namespace Furvo.Core.Domain;

// Null solo permite representar datos desconocidos de las partidas del esquema 1.
/// <summary>Club con identidad estable; su perfil y reputación son institucionales, no una valoración de plantilla.</summary>
public sealed record Club
{
    public ClubId Id { get; }
    public string Name { get; }
    public string? City { get; }
    public string? Country { get; }
    public int? Reputation { get; }
    public ClubProfile? Profile { get; }

    /// <summary>Valida datos disponibles y conserva null históricos; el almacén exige los campos básicos para nuevas escrituras.</summary>
    public Club(ClubId id, string name, string? city = null, string? country = null, int? reputation = null, ClubProfile? profile = null)
    {
        Id = DomainRules.Id(id, id.Value);
        Name = DomainRules.Text(name);
        City = DomainRules.OptionalText(city);
        Country = DomainRules.OptionalText(country);
        Reputation = DomainRules.ClubReputation(reputation);
        Profile = profile;
    }
}

/// <summary>Posición principal general; no introduce ponderaciones ni valoraciones deportivas derivadas.</summary>
public enum PlayerPosition { Goalkeeper = 1, Defender = 2, Midfielder = 3, Forward = 4 }

/// <summary>Jugador y atributos individuales. Su club se obtiene de contratos y fechas, sin duplicar la afiliación.</summary>
public sealed record Player
{
    public PlayerId Id { get; }
    public string Name { get; }
    public DateOnly? BirthDate { get; }
    public string? Nationality { get; }
    public PlayerPosition? MainPosition { get; }
    public PlayerAttributes Attributes { get; }
    public PlayerPositions? Positions { get; }

    /// <summary>Conserva datos históricos desconocidos y valida la posición; no asigna puntuaciones automáticamente.</summary>
    public Player(PlayerId id, string name, DateOnly? birthDate = null, string? nationality = null,
        PlayerPosition? mainPosition = null, PlayerAttributes? attributes = null, PlayerPositions? positions = null)
    {
        Id = DomainRules.Id(id, id.Value);
        Name = DomainRules.Text(name);
        BirthDate = birthDate;
        Nationality = DomainRules.OptionalText(nationality);
        if (mainPosition.HasValue && !Enum.IsDefined(mainPosition.Value))
            throw new ArgumentOutOfRangeException(nameof(mainPosition));
        MainPosition = mainPosition;
        Attributes = attributes ?? PlayerAttributes.Empty;
        if (positions is not null && mainPosition != PlayerPositions.GroupOf(positions.Primary))
            throw new ArgumentException("El desglose no coincide con el grupo principal.", nameof(positions));
        Positions = positions;
    }
    /// <summary>Calcula la edad en una fecha explícita; no depende del reloj ni almacena una edad que pueda quedar obsoleta.</summary>
    public int AgeOn(DateOnly date)
    {
        if (BirthDate is not { } birth) throw new InvalidOperationException("Fecha de nacimiento desconocida.");
        if (date < birth) throw new ArgumentOutOfRangeException(nameof(date));
        var age = date.Year - birth.Year;
        return birth.AddYears(age) > date ? age - 1 : age;
    }
}

/// <summary>Vínculo temporal jugador-club; las fechas son inclusivas y SQLite impide solapamientos del mismo jugador.</summary>
public sealed record Contract
{
    public ContractId Id { get; }
    public ClubId ClubId { get; }
    public PlayerId PlayerId { get; }
    public DateOnly? StartDate { get; }
    public DateOnly? EndDate { get; }

    /// <summary>Exige identidades válidas y fechas completas y ordenadas, salvo un intervalo histórico totalmente desconocido.</summary>
    public Contract(ContractId id, ClubId clubId, PlayerId playerId, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        Id = DomainRules.Id(id, id.Value);
        ClubId = DomainRules.Id(clubId, clubId.Value);
        PlayerId = DomainRules.Id(playerId, playerId.Value);
        DomainRules.Dates(startDate, endDate);
        StartDate = startDate;
        EndDate = endDate;
    }
}

/// <summary>Entrenador identificado y club elegido; no incluye comportamiento de inteligencia artificial.</summary>
public sealed record Coach
{
    public CoachId Id { get; }
    public string Name { get; }
    public ClubId ClubId { get; }

    /// <summary>Valida identidad, nombre y referencia del club sin resolver persistencia desde el dominio.</summary>
    public Coach(CoachId id, string name, ClubId clubId)
    {
        Id = DomainRules.Id(id, id.Value);
        Name = DomainRules.Text(name);
        ClubId = DomainRules.Id(clubId, clubId.Value);
    }
}

/// <summary>Identidad y datos descriptivos de la competición, sin calendario, resultados ni reglas de torneo.</summary>
public sealed record Competition
{
    public CompetitionId Id { get; }
    public string Name { get; }
    public string Country { get; }

    /// <summary>Requiere nombre y país para que la competición pueda identificarse sin depender de su etiqueta.</summary>
    public Competition(CompetitionId id, string name, string country)
    {
        Id = DomainRules.Id(id, id.Value);
        Name = DomainRules.Text(name);
        Country = DomainRules.Text(country);
    }
}

/// <summary>Periodo de temporada independiente del calendario de partidos; el nombre admite desconocidos históricos.</summary>
public sealed record Season
{
    public string? Name { get; }
    public SeasonId Id { get; }
    public DateOnly StartDate { get; }
    public DateOnly EndDate { get; }

    /// <summary>Valida las fechas sin inventar jornadas ni modificar intervalos de temporadas existentes.</summary>
    public Season(SeasonId id, DateOnly startDate, DateOnly endDate, string? name = null)
    {
        Id = DomainRules.Id(id, id.Value);
        Name = DomainRules.OptionalText(name);
        DomainRules.Dates(startDate, endDate);
        StartDate = startDate;
        EndDate = endDate;
    }
}

// Identidad compuesta: una sola inscripción por club, competición y temporada.
/// <summary>Relación club-competición-temporada con identidad compuesta; una inscripción repetida no crea otra entidad.</summary>
public sealed record Registration
{
    public ClubId ClubId { get; }
    public CompetitionId CompetitionId { get; }
    public SeasonId SeasonId { get; }

    /// <summary>Valida las tres identidades; la persistencia garantiza que sus entidades existan.</summary>
    public Registration(ClubId clubId, CompetitionId competitionId, SeasonId seasonId)
    {
        ClubId = DomainRules.Id(clubId, clubId.Value);
        CompetitionId = DomainRules.Id(competitionId, competitionId.Value);
        SeasonId = DomainRules.Id(seasonId, seasonId.Value);
    }
}
