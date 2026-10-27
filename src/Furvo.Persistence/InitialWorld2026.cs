using System.Text.Json;
using Furvo.Core.Domain;
using Furvo.Core.Worlds;

namespace Furvo.Persistence;

// Solo datos iniciales de clubes: nunca se aplica sobre una partida.
/// <summary>Publica el universo ficticio corregido desde un recurso embebido; las referencias reales solo existen en desarrollo.</summary>
public static class InitialWorld2026
{
    /// <summary>Crea un archivo nuevo o verifica uno idéntico. Rechaza partidas y datos distintos para no sobrescribir publicaciones.</summary>
    public static SqliteWorldStore LoadOrCreate(string destination)
    {
        var path = WorldDatabase.PathFor(destination);
        var (worldId, data) = ReadDefinition();
        var clubs = data.Clubs;
        if (!File.Exists(path))
        {
            try { return SqliteWorldFiles.CreateInitialWorld(path, worldId, data); }
            catch (IOException) when (File.Exists(path)) { /* Otra carga pudo publicar primero: verificar sin escribir. */ }
        }
        var existing = new SqliteWorldStore(path);
        var info = existing.ReadInfo();
        var current = existing.Load();
        if (info.Kind != WorldFileKind.InitialWorld || info.WorldId != worldId || info.SaveId is not null
            || !current.Clubs.OrderBy(x => x.Id.Value).SequenceEqual(clubs.OrderBy(x => x.Id.Value))
            || !current.Competitions.SequenceEqual(data.Competitions)
            || !current.Seasons.SequenceEqual(data.Seasons)
            || !current.Registrations.OrderBy(x => x.ClubId.Value)
                .SequenceEqual(data.Registrations.OrderBy(x => x.ClubId.Value))
            || current.Players.Count != 0 || current.Contracts.Count != 0 || current.Coaches.Count != 0)
            throw new InvalidOperationException("El destino publicado no coincide con el universo 2026/27. No se modifica; utiliza otra ruta.");
        return existing;
    }

    /// <summary>Proporciona los mismos clubes v2 al generador sin crear ni abrir ningún SQLite de usuario.</summary>
    internal static (WorldId WorldId, WorldData Data) ReadDefinition()
    {
        using var stream = typeof(InitialWorld2026).Assembly.GetManifestResourceStream(
            "Furvo.Persistence.Data.initial-world-2026-27.v2.json")
            ?? throw new InvalidOperationException("Faltan los datos iniciales embebidos.");
        var seed = JsonSerializer.Deserialize<SeedDocument>(stream)
            ?? throw new InvalidDataException("Datos iniciales vacíos.");
        if (seed.DatasetVersion != "2026-27.entrega2.v2")
            throw new InvalidDataException("Versión de datos iniciales desconocida.");
        var worldId = new WorldId(seed.WorldId);
        var competition = new Competition(new CompetitionId(seed.Competition.Id), seed.Competition.Name, seed.Competition.Country);
        var season = new Season(new SeasonId(seed.Season.Id), seed.Season.StartDate, seed.Season.EndDate, seed.Season.Name);
        var clubs = seed.Clubs.Select(x => new Club(new ClubId(x.Id), x.Name, x.City, x.Country, x.Reputation,
            new ClubProfile(x.Profile.ShortName, x.Profile.Region, x.Profile.FoundationYear, x.Profile.SportingIdentity,
                x.Profile.PrimaryColor, x.Profile.SecondaryColor, x.Profile.StadiumName,
                x.Profile.StadiumCapacity, x.Profile.InitialCategory))).ToArray();
        if (clubs.Length != 20 || clubs.Select(x => x.Id).Distinct().Count() != 20
            || clubs.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 20
            || clubs.Any(x => x.Country != "España" || x.Profile?.InitialCategory != "Primera categoría"))
            throw new InvalidDataException("El universo debe tener exactamente 20 clubes españoles distintos.");
        var data = new WorldData(clubs: clubs, competitions: [competition], seasons: [season],
            registrations: clubs.Select(x => new Registration(x.Id, competition.Id, season.Id)));
        return (worldId, data);
    }

    private sealed record SeedDocument(string DatasetVersion, Guid WorldId, CompetitionRow Competition, SeasonRow Season, ClubRow[] Clubs);
    private sealed record CompetitionRow(Guid Id, string Name, string Country);
    private sealed record SeasonRow(Guid Id, string Name, DateOnly StartDate, DateOnly EndDate);
    private sealed record ClubRow(Guid Id, string Name, string City, string Country, int Reputation, ProfileRow Profile);
    private sealed record ProfileRow(string ShortName, string Region, int FoundationYear, string SportingIdentity,
        string PrimaryColor, string SecondaryColor, string StadiumName, int StadiumCapacity, string InitialCategory);
}
