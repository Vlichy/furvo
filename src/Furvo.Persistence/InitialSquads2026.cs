using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Furvo.Core.Domain;
using Furvo.Core.Generation;
using Furvo.Core.Worlds;
using Microsoft.Data.Sqlite;

namespace Furvo.Persistence;

/// <summary>Identifica los parámetros reproducibles de una publicación, sin añadir una valoración general al jugador.</summary>
public sealed record SquadGenerationInfo(string GeneratorVersion, string Seed, string ParameterHash, string CatalogHash, WorldId SourceWorldId);

/// <summary>Publicación v3 independiente: reutiliza los clubes v2 y añade futbolistas sin escribir sobre archivos anteriores.</summary>
public static class InitialSquads2026
{
    /// <summary>Recupera parámetros editoriales embebidos; nunca consulta nombres o plantillas de clubes reales.</summary>
    public static SquadGenerationSettings Settings()
    {
        using var stream = typeof(InitialSquads2026).Assembly.GetManifestResourceStream(
            "Furvo.Persistence.Data.initial-squads-2026-27.json")
            ?? throw new InvalidOperationException("Faltan los parámetros de plantillas.");
        var definition = JsonSerializer.Deserialize<Definition>(stream)
            ?? throw new InvalidDataException("Parámetros vacíos.");
        var (worldId, _) = InitialWorld2026.ReadDefinition();
        if (definition.SourceWorldId != worldId.Value || definition.SourceDatasetVersion != "2026-27.entrega2.v2")
            throw new InvalidDataException("El generador no corresponde a la publicación de clubes v2.");
        return new SquadGenerationSettings(definition.Version, definition.Seed,
            definition.Clubs.Select(x => new ClubGenerationProfile(new ClubId(x.ClubId), x.AttributeBaseline,
                x.SpanishPlayers, x.AgeBias, x.Style, x.SpotlightSlot)),
            definition.Cultures.Select(x => new NameCulture(x.Country, x.GivenNames, x.FamilyNames, x.TwoSurnames, x.FamilyFirst, x.Weight)));
    }

    /// <summary>Genera datos puros sin tocar disco; facilita verificar semilla, orden y determinismo en pruebas.</summary>
    public static WorldData Generate(SquadGenerationSettings? settings = null)
    {
        var (_, source) = InitialWorld2026.ReadDefinition();
        return InitialSquadGenerator.Generate(source, settings ?? Settings());
    }

    /// <summary>Crea o verifica la v3; un destino que contenga otra publicación o una partida se rechaza sin cambiarlo.</summary>
    public static SqliteWorldStore LoadOrCreate(string destination)
    {
        var path = WorldDatabase.PathFor(destination);
        var settings = Settings();
        var data = Generate(settings);
        var info = Provenance(settings);
        var worldId = PublicationId(info);
        if (!File.Exists(path))
        {
            try
            {
                return SqliteWorldFiles.CreateInitialWorldWithGeneration(path, worldId, data, info);
            }
            catch (IOException) when (File.Exists(path)) { /* Otra ejecución publicó antes: se compara, nunca se sobrescribe. */ }
        }
        var existing = new SqliteWorldStore(path);
        var metadata = existing.ReadInfo();
        if (metadata.Kind != WorldFileKind.InitialWorld || metadata.WorldId != worldId || metadata.SaveId is not null
            || ReadGenerationInfo(path) != info || Fingerprint(existing.Load()) != Fingerprint(data))
            throw new InvalidOperationException("El destino no coincide con la publicación de plantillas v3; utiliza otro archivo.");
        return existing;
    }

    /// <summary>Lee la trazabilidad conservada al copiar una partida desde v3.</summary>
    public static SquadGenerationInfo? ReadGenerationInfo(string databasePath)
    {
        using var connection = WorldDatabase.Open(databasePath);
        using var transaction = connection.BeginTransaction(deferred: true);
        WorldDatabase.RequireCurrent(connection, transaction);
        using var command = WorldDatabase.Command(connection, transaction, """
            SELECT generator_version, seed, parameter_hash, catalog_hash, source_world_id
            FROM generation_metadata WHERE singleton = 1;
            """);
        using var reader = command.ExecuteReader();
        return reader.Read() ? new(reader.GetString(0), reader.GetString(1), reader.GetString(2),
            reader.GetString(3), new WorldId(Guid.Parse(reader.GetString(4)))) : null;
    }

    /// <summary>Huella canónica de todos los datos, incluidos ocultos y posiciones: no depende del orden de SQLite.</summary>
    public static string Fingerprint(WorldData data)
    {
        var canonical = new
        {
            Clubs = data.Clubs.OrderBy(x => x.Id.ToString(), StringComparer.Ordinal).Select(x => new { Id = x.Id.ToString(), x.Name, x.City, x.Country, x.Reputation, x.Profile }),
            Players = data.Players.OrderBy(x => x.Id.ToString(), StringComparer.Ordinal).Select(x => new
            {
                Id = x.Id.ToString(), x.Name, x.BirthDate, x.Nationality, x.MainPosition,
                Primary = x.Positions?.Primary,
                Secondary = x.Positions?.Secondary.OrderBy(x => (int)x).ToArray(),
                Values = x.Attributes.AllValues.OrderBy(v => v.Key, StringComparer.Ordinal).Select(v => new { Code = v.Key, v.Value })
            }),
            Contracts = data.Contracts.OrderBy(x => x.Id.ToString(), StringComparer.Ordinal).Select(x =>
                new { Id = x.Id.ToString(), Club = x.ClubId.ToString(), Player = x.PlayerId.ToString(), x.StartDate, x.EndDate }),
            Coaches = data.Coaches.OrderBy(x => x.Id.ToString(), StringComparer.Ordinal).Select(x =>
                new { Id = x.Id.ToString(), x.Name, Club = x.ClubId.ToString() }),
            Competitions = data.Competitions.OrderBy(x => x.Id.ToString(), StringComparer.Ordinal).Select(x => new { Id = x.Id.ToString(), x.Name, x.Country }),
            Seasons = data.Seasons.OrderBy(x => x.Id.ToString(), StringComparer.Ordinal).Select(x => new { Id = x.Id.ToString(), x.Name, x.StartDate, x.EndDate }),
            Registrations = data.Registrations.OrderBy(x => x.ClubId.ToString(), StringComparer.Ordinal)
                .ThenBy(x => x.CompetitionId.ToString(), StringComparer.Ordinal).ThenBy(x => x.SeasonId.ToString(), StringComparer.Ordinal)
                .Select(x => new { Club = x.ClubId.ToString(), Competition = x.CompetitionId.ToString(), Season = x.SeasonId.ToString() })
        };
        return Hash(canonical);
    }

    internal static SquadGenerationInfo Provenance(SquadGenerationSettings settings) => new(
        settings.Version, settings.Seed.ToString(CultureInfo.InvariantCulture), Hash(settings),
        Hash(PlayerAttributeCatalog.All.OrderBy(x => x.Code, StringComparer.Ordinal)),
        InitialWorld2026.ReadDefinition().WorldId);

    private static WorldId PublicationId(SquadGenerationInfo info) => new(Guid.ParseExact(
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("FURVO-WORLD-V3|" +
            info.GeneratorVersion + "|" + info.Seed + "|" + info.ParameterHash + "|" + info.CatalogHash + "|" + info.SourceWorldId))
            .AsSpan(0, 16)), "N"));

    private static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value))).ToLowerInvariant();

    private sealed record Definition(string Version, long Seed, string SourceDatasetVersion, Guid SourceWorldId, ProfileRow[] Clubs, CultureRow[] Cultures);
    private sealed record ProfileRow(Guid ClubId, int AttributeBaseline, int SpanishPlayers, int AgeBias, int Style, int? SpotlightSlot);
    private sealed record CultureRow(string Country, string[] GivenNames, string[] FamilyNames, bool TwoSurnames, bool FamilyFirst, int Weight);
}
