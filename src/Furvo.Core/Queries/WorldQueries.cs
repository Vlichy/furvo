using Furvo.Core.Domain;
using Furvo.Core.Worlds;

namespace Furvo.Core.Queries;

/// <summary>Orden de la consulta de plantilla; los desempates siempre usan nombre e identidad estable.</summary>
public enum SquadSort { Name, Position, Age }

/// <summary>Proyección de club para presentación, sin acceso de escritura al dominio.</summary>
public sealed record SeasonSummary(SeasonId Id, string? Name, DateOnly StartDate, DateOnly EndDate);

public sealed record ClubSummary(ClubId Id, string Name, string? City, int? VisibleReputation);
/// <summary>Datos visibles de una fila de plantilla; no contiene atributos ni referencias a Player.</summary>
public sealed record PlayerSummary(PlayerId Id, string Name, string? Nationality, int? Age,
    PlayerPosition? Group, PlayerRole? Position);
/// <summary>Atributo público identificado por código; el texto se resuelve en recursos de la interfaz.</summary>
public sealed record VisibleAttribute(string Code, int Value);
/// <summary>Colección de una categoría visible; nunca se construye para Hidden.</summary>
public sealed record VisibleAttributeGroup(AttributeCategory Category, IReadOnlyList<VisibleAttribute> Values);
/// <summary>Ficha pública completa sin entidades ni diccionarios de atributos internos.</summary>
public sealed record PlayerDetails(PlayerSummary Player, ClubSummary Club, DateOnly? BirthDate,
    IReadOnlyList<PlayerRole> SecondaryPositions, IReadOnlyList<VisibleAttributeGroup> AttributeGroups);

/// <summary>Puerto de consultas de solo lectura que puede consumir la presentación sin conocer SQL.</summary>
public interface IWorldQueries
{
    DateOnly ReferenceDate { get; }
    IReadOnlyList<ClubSummary> GetClubs();
    IReadOnlyList<SeasonSummary> GetSeasons();
    ClubFinancialResources GetFinancialResources(ClubId clubId, SeasonId seasonId);
    ClubDetails GetClubDetails(ClubId clubId);
    ClubHistoryDetails GetClubHistory(ClubId clubId);
    IReadOnlyList<PlayerSummary> GetSquad(ClubId clubId, SquadSort sort = SquadSort.Name, bool descending = false);
    PlayerDetails GetPlayer(ClubId clubId, PlayerId playerId);
}

/// <summary>Consultas sobre una instantánea y una fecha explícita; filtra ocultos antes de salir de Core.</summary>
public sealed class WorldQueries : IWorldQueries
{
    private readonly WorldData _world;
    private readonly IReadOnlyList<ClubSummary> _clubs;
    private readonly IReadOnlyList<ClubFinancialResources> _economics;
    public DateOnly ReferenceDate { get; }

    /// <summary>No consulta el reloj ni persiste cambios; la afiliación se determina exclusivamente por contratos vigentes.</summary>
    public WorldQueries(WorldData world, DateOnly referenceDate,IReadOnlyList<ClubFinancialResources>? economics=null)
    {
        ArgumentNullException.ThrowIfNull(world);
        _world = world;
        _economics=Array.AsReadOnly((economics??Array.Empty<ClubFinancialResources>()).ToArray());
        ReferenceDate = referenceDate;
        _clubs = Array.AsReadOnly(world.Clubs.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Id.ToString(), StringComparer.Ordinal)
            .Select(x => new ClubSummary(x.Id, x.Name, x.City, x.Reputation)).ToArray());
    }

    public IReadOnlyList<ClubSummary> GetClubs() => _clubs;

    /// <summary>Solo temporadas presentes en la instantánea, sin generar ejercicios financieros.</summary>
    public IReadOnlyList<SeasonSummary> GetSeasons() => Array.AsReadOnly(_world.Seasons.OrderBy(s => s.StartDate)
        .ThenBy(s => s.Id.ToString(), StringComparer.Ordinal)
        .Select(s => new SeasonSummary(s.Id, s.Name, s.StartDate, s.EndDate)).ToArray());

    /// <summary>Oferta y Economía comparten el mismo estado aprobado; los mundos históricos conservan sus desconocidos.</summary>
    public ClubFinancialResources GetFinancialResources(ClubId clubId, SeasonId seasonId)
    {
        _ = RequireClub(clubId);
        if (!_world.Seasons.Any(s=>s.Id==seasonId)) throw new KeyNotFoundException("Temporada desconocida.");
        return _economics.SingleOrDefault(e=>e.ClubId==clubId && e.SeasonId==seasonId)??new ClubFinancialResources(clubId,seasonId);
    }

    /// <summary>Proyecta los datos ya almacenados; campos nuevos permanecen desconocidos hasta autoría y persistencia aprobadas.</summary>
    public ClubDetails GetClubDetails(ClubId clubId)
    {
        var summary = RequireClub(clubId);
        var club = _world.Clubs.Single(c => c.Id == clubId);
        var profile = club.Profile;
        var competitions = (from registration in _world.Registrations
            join competition in _world.Competitions on registration.CompetitionId equals competition.Id
            join season in _world.Seasons on registration.SeasonId equals season.Id
            where registration.ClubId == clubId && season.StartDate <= ReferenceDate && season.EndDate >= ReferenceDate
            select new ClubCompetitionSummary(competition.Id, season.Id, competition.Name, season.Name)).ToArray();
        competitions = competitions.OrderBy(c => c.Competition, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.CompetitionId.ToString(), StringComparer.Ordinal).ToArray();
        return new ClubDetails(summary, new ClubIdentityDetails(profile?.Region, club.Country, profile?.FoundationYear,
            profile?.PrimaryColor, profile?.SecondaryColor, profile?.SportingIdentity),
            new ClubStadiumDetails(profile?.StadiumName, null, profile?.StadiumCapacity), Array.AsReadOnly(competitions));
    }

    /// <summary>No hay resultados históricos aprobados en el esquema actual. No trata inscripciones como clasificaciones.</summary>
    public ClubHistoryDetails GetClubHistory(ClubId clubId)
    {
        _ = RequireClub(clubId);
        return new ClubHistoryDetails(clubId);
    }

    /// <summary>Ordena sin cultura del proceso; la dirección afecta a la clave principal y los desempates son ascendentes.</summary>
    public IReadOnlyList<PlayerSummary> GetSquad(ClubId clubId, SquadSort sort = SquadSort.Name, bool descending = false)
    {
        RequireClub(clubId);
        if (!Enum.IsDefined(sort)) throw new ArgumentOutOfRangeException(nameof(sort));
        var members = ActiveContracts().Where(x => x.ClubId == clubId).Select(x => x.PlayerId).ToHashSet();
        var rows = _world.Players.Where(x => members.Contains(x.Id)).Select(Summary);
        IOrderedEnumerable<PlayerSummary> ordered = sort switch
        {
            SquadSort.Name => descending ? rows.OrderByDescending(x => x.Name, StringComparer.OrdinalIgnoreCase)
                : rows.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase),
            SquadSort.Position => descending ? rows.OrderByDescending(PositionOrder) : rows.OrderBy(PositionOrder),
            SquadSort.Age => descending ? rows.OrderByDescending(x => x.Age) : rows.OrderBy(x => x.Age),
            _ => throw new ArgumentOutOfRangeException(nameof(sort))
        };
        return Array.AsReadOnly(ordered.ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Id.ToString(), StringComparer.Ordinal).ToArray());
    }

    /// <summary>Exige que la ficha pertenezca al club consultado y proyecta únicamente VisibleValues.</summary>
    public PlayerDetails GetPlayer(ClubId clubId, PlayerId playerId)
    {
        var club = RequireClub(clubId);
        if (!ActiveContracts().Any(x => x.ClubId == clubId && x.PlayerId == playerId))
            throw new KeyNotFoundException("El futbolista no pertenece a la plantilla consultada.");
        var player = _world.Players.SingleOrDefault(x => x.Id == playerId)
            ?? throw new KeyNotFoundException("Futbolista desconocido.");
        return ProjectPlayer(player,club,ReferenceDate);
    }
    public static PlayerDetails ProjectPlayer(Player player,ClubSummary club,DateOnly date)
    {
        var visible = player.Attributes.VisibleValues;
        // Una ficha de consulta incompleta no debe aparentar que contiene los 51 atributos aprobados.
        if (visible.Count != PlayerAttributeCatalog.All.Count(x => !x.IsHidden))
            throw new InvalidDataException("No se puede recuperar la ficha completa de atributos visibles.");
        var groups = PlayerAttributeCatalog.All.Where(x => !x.IsHidden)
            .GroupBy(x => x.Category).OrderBy(x => (int)x.Key)
            .Select(g => new VisibleAttributeGroup(g.Key, Array.AsReadOnly(g.Where(x => visible.ContainsKey(x.Code))
                .Select(x => new VisibleAttribute(x.Code, visible[x.Code])).ToArray()))).ToArray();
        return new PlayerDetails(new PlayerSummary(player.Id,player.Name,player.Nationality,player.BirthDate is {} birth&&birth<=date?player.AgeOn(date):null,player.MainPosition,player.Positions?.Primary), club, player.BirthDate,
            Array.AsReadOnly(player.Positions?.Secondary.ToArray() ?? []), Array.AsReadOnly(groups));
    }

    private ClubSummary RequireClub(ClubId id) => _clubs.SingleOrDefault(x => x.Id == id)
        ?? throw new KeyNotFoundException("Club desconocido.");
    private IEnumerable<Contract> ActiveContracts() => _world.Contracts.Where(x =>
        x.StartDate is { } start && start <= ReferenceDate && x.EndDate is { } end && end >= ReferenceDate);
    private PlayerSummary Summary(Player p) => new(p.Id, p.Name, p.Nationality,
        p.BirthDate is { } birth && birth <= ReferenceDate ? p.AgeOn(ReferenceDate) : null, p.MainPosition, p.Positions?.Primary);
    private static int PositionOrder(PlayerSummary p)
    {
        // Primero el grupo deportivo; una posición genérica también permanece dentro de su grupo.
        var group = p.Position is { } role ? PlayerPositions.GroupOf(role) : p.Group;
        return group is { } value ? (int)value * 100 + (p.Position is { } position ? (int)position : 99) : int.MaxValue;
    }
}
