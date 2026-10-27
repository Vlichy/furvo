using Furvo.Core.Domain;

namespace Furvo.Core.Queries;

/// <summary>Competición/temporada con inscripción vigente en la fecha explícita de consulta.</summary>
public sealed record ClubCompetitionSummary(CompetitionId CompetitionId, SeasonId SeasonId, string Competition, string? Season);

/// <summary>Datos institucionales públicos; null significa desconocido, no cero ni un dato generado.</summary>
public sealed record ClubIdentityDetails(string? Region, string? Country, int? FoundationYear,
    string? PrimaryColor, string? SecondaryColor, string? Tradition)
{
    public string? Nickname { get; init; }
    public string? SupportersName { get; init; }
    public IReadOnlyList<ClubSummary>? Rivalries { get; init; }
    public int? Subscribers { get; init; }
}

/// <summary>Estadio conocido y campos futuros opcionales; la ciudad del club no se atribuye sin datos al estadio.</summary>
public sealed record ClubStadiumDetails(string? Name, string? City, int? Capacity)
{
    public int? AverageAttendance { get; init; }
    public int? RecordAttendance { get; init; }
    public decimal? OccupancyPercent { get; init; }
}

/// <summary>Ficha de club de solo lectura, sin jugadores, atributos internos ni cifras económicas inventadas.</summary>
public sealed record ClubDetails(ClubSummary Club, ClubIdentityDetails Identity, ClubStadiumDetails Stadium,
    IReadOnlyList<ClubCompetitionSummary> Competitions);

/// <summary>Separa historia editorial aprobada de resultados generados en una partida; no incorpora propuestas a datos del juego.</summary>
public enum ClubHistoryOrigin { AuthoredBefore2026, SimulatedSave }

/// <summary>Fila futura de clasificación: posición ausente no es un resultado; división 1/2 distingue las categorías previstas.</summary>
public sealed record ClubSeasonStanding(string Season, int StartYear, int Division, int? FinalPosition, ClubHistoryOrigin Origin);
public sealed record ClubHonourSummary(string Competition, IReadOnlyList<string> Seasons);
public sealed record ClubHistoricalPerson(string Name, string Description);

/// <summary>Contrato de lectura preparado para historia aprobada. Actualmente se devuelve vacío y con campos desconocidos.</summary>
public sealed record ClubHistoryDetails(ClubId ClubId)
{
    public bool IsApprovedDataAvailable { get; init; }
    public IReadOnlyList<ClubSeasonStanding> Standings { get; init; } = Array.Empty<ClubSeasonStanding>();
    public IReadOnlyList<ClubHonourSummary> Honours { get; init; } = Array.Empty<ClubHonourSummary>();
    public IReadOnlyList<ClubHistoricalPerson> Players { get; init; } = Array.Empty<ClubHistoricalPerson>();
    public IReadOnlyList<ClubHistoricalPerson> Coaches { get; init; } = Array.Empty<ClubHistoricalPerson>();
    public int? FirstDivisionSeasons { get; init; }
    public int? SecondDivisionSeasons { get; init; }
    public int? BestFirstPosition { get; init; }
    public string? BestFirstSeason { get; init; }
    public int? WorstFirstPosition { get; init; }
    public string? WorstFirstSeason { get; init; }
    public int? AllTimeRank { get; init; }
    public int? Promotions { get; init; }
    public int? Relegations { get; init; }
    public string? LastPromotion { get; init; }
    public string? LastRelegation { get; init; }
    public string? TopScorer { get; init; }
    public string? MostAppearances { get; init; }
    public string? LongestServingCoach { get; init; }
    public string? Records { get; init; }
}
