using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Furvo.Core.Domain;
using Furvo.Core.Worlds;

namespace Furvo.Persistence;

/// <summary>Dato manual de publicación; la escala visible se deriva y no se introduce como otro valor de autoría.</summary>
public sealed record InitialClubReputation
{
    public ClubId ClubId { get; }
    public string Name { get; }
    public int Internal { get; }
    public int Visible => Math.Min(5, Internal / 20 + 1);
    public InitialClubReputation(ClubId clubId, string name, int internalValue)
    {
        if (clubId.Value == Guid.Empty) throw new ArgumentException("Club sin identidad.", nameof(clubId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (internalValue is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(internalValue));
        ClubId = clubId; Name = name; Internal = internalValue;
    }
}

/// <summary>Publica v4 desde la v3 de solo lectura; corrige reputación sin regenerar futbolistas ni alterar el esquema.</summary>
public static class AdjustedInitialWorld2026
{
    private const string Resource = "Furvo.Persistence.Data.club-reputations-2026-27.v2.json";
    private static Definition ReadDefinition()
    {
        using var stream = typeof(AdjustedInitialWorld2026).Assembly.GetManifestResourceStream(Resource)
            ?? throw new InvalidDataException("Falta el catálogo corregido de reputaciones.");
        return JsonSerializer.Deserialize<Definition>(stream) ?? throw new InvalidDataException("Catálogo vacío.");
    }

    /// <summary>La autoría interna vive en el catálogo versionado; SQLite sigue almacenando la reputación visible 1–5.</summary>
    public static IReadOnlyList<InitialClubReputation> ReadReputations() => ReadReputations(ReadDefinition());

    private static IReadOnlyList<InitialClubReputation> ReadReputations(Definition definition)
    {
        var values = definition.Clubs.Select(x => new InitialClubReputation(new ClubId(x.ClubId), x.Name, x.Internal)).ToArray();
        if (values.Length != 20 || values.Select(x => x.ClubId).Distinct().Count() != 20)
            throw new InvalidDataException("El catálogo debe contener veinte ClubId distintos.");
        return Array.AsReadOnly(values);
    }

    /// <summary>Solo crea un destino nuevo o verifica v4; nunca crea, migra o escribe el archivo de origen.</summary>
    public static SqliteWorldStore LoadOrCreate(string sourceV3, string destination)
    {
        var sourcePath = WorldDatabase.PathFor(sourceV3);
        var path = WorldDatabase.PathFor(destination);
        var definition = ReadDefinition();
        var source = new SqliteWorldStore(sourcePath);
        var sourceInfo = source.ReadInfo();
        var original = source.Load();
        if (sourceInfo.Kind != WorldFileKind.InitialWorld || sourceInfo.WorldId.Value != definition.SourceWorldId ||
            InitialSquads2026.Fingerprint(original) != InitialSquads2026.Fingerprint(InitialSquads2026.Generate()))
            throw new InvalidDataException("El origen no coincide con la publicación v3 aprobada.");
        var reputations = ReadReputations(definition).ToDictionary(x => x.ClubId);
        if (!original.Clubs.All(c => reputations.ContainsKey(c.Id))) throw new InvalidDataException("ClubId del catálogo incompatible.");
        // Se reutilizan todas las entidades; solo se construye otro Club si cambia su reputación visible.
        var clubs = original.Clubs.Select(c => c.Reputation == reputations[c.Id].Visible ? c :
            new Club(c.Id, c.Name, c.City, c.Country, reputations[c.Id].Visible, c.Profile)).ToArray();
        var data = new WorldData(clubs, original.Players, original.Contracts, original.Coaches,
            original.Competitions, original.Seasons, original.Registrations);
        var generation = InitialSquads2026.ReadGenerationInfo(sourcePath)
            ?? throw new InvalidDataException("Falta procedencia de la publicación v3.");
        var parameterHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(definition)))).ToLowerInvariant();
        var provenance = new SquadGenerationInfo(definition.Version, generation.Seed, parameterHash,
            generation.CatalogHash, sourceInfo.WorldId);
        var worldId = new WorldId(definition.WorldId);
        if (worldId == sourceInfo.WorldId) throw new InvalidDataException("v4 requiere otro WorldId.");
        if (!File.Exists(path))
        {
            try { return SqliteWorldFiles.CreateInitialWorldWithGeneration(path, worldId, data, provenance); }
            catch (IOException) when (File.Exists(path)) { /* Otra instancia publicó: verificar, nunca sobrescribir. */ }
        }
        var existing = new SqliteWorldStore(path);
        var info = existing.ReadInfo();
        if (info.Kind != WorldFileKind.InitialWorld || info.WorldId != worldId || info.SaveId is not null ||
            InitialSquads2026.ReadGenerationInfo(path) != provenance ||
            InitialSquads2026.Fingerprint(existing.Load()) != InitialSquads2026.Fingerprint(data))
            throw new InvalidOperationException("El destino no coincide con v4; utiliza una ruta nueva.");
        return existing;
    }

    private sealed record Definition(string Version, Guid SourceWorldId, Guid WorldId, Row[] Clubs);
    private sealed record Row(Guid ClubId, string Name, int Internal);
}
