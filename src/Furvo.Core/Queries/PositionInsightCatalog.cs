using Furvo.Core.Domain;
using Furvo.Core.Queries;

namespace Furvo.Core.Queries;

/// <summary>Descripción individual de atributos visibles; no contiene medias, pesos, valoraciones ni resultados deportivos.</summary>
public sealed record PlayerPositionInsights(IReadOnlyList<VisibleAttribute> Strengths, IReadOnlyList<VisibleAttribute> Weaknesses);

/// <summary>Configuración descriptiva de presentación, separada de cualquier futura valoración interna por posición.</summary>
public static class PositionInsightCatalog
{
    public const int StrengthMinimum = 14;
    public const int WeaknessMaximum = 7;
    public const int MaximumItems = 3;

    private static readonly IReadOnlyList<string> Goalkeeper = Codes("reflexes handling punching one_on_ones aerial_claims rushing_out goalkeeping_positioning command_of_area hand_distribution defensive_communication");
    private static readonly IReadOnlyList<string> CentreBack = Codes("marking tackling anticipation defensive_positioning heading strength concentration jumping bravery decisions");
    private static readonly IReadOnlyList<string> FullBack = Codes("tackling marking defensive_positioning anticipation pace acceleration stamina crossing short_passing decisions");
    private static readonly IReadOnlyList<string> DefensiveMidfielder = Codes("defensive_positioning anticipation tackling marking short_passing decisions teamwork tactical_discipline concentration stamina");
    private static readonly IReadOnlyList<string> CentralMidfielder = Codes("short_passing long_passing vision decisions ball_control first_touch teamwork stamina tactical_understanding");
    private static readonly IReadOnlyList<string> AttackingMidfielder = Codes("vision short_passing creativity decisions technique first_touch ball_control dribbling off_the_ball long_shots composure");
    private static readonly IReadOnlyList<string> WideMidfielder = Codes("crossing short_passing dribbling pace acceleration stamina work_rate off_the_ball ball_control");
    private static readonly IReadOnlyList<string> Striker = Codes("finishing off_the_ball composure heading anticipation first_touch shot_power strength");
    private static readonly IReadOnlyList<string> Winger = Codes("dribbling pace acceleration off_the_ball crossing technique first_touch finishing creativity");
    private static readonly IReadOnlyList<string> SecondStriker = Codes("off_the_ball finishing creativity short_passing vision first_touch dribbling composure decisions");

    /// <summary>El catálogo solo declara pertenencia; el orden de estos códigos no expresa pesos ni prioridad deportiva.</summary>
    public static IReadOnlyList<string> RelevantFor(PlayerRole role) => role switch
    {
        PlayerRole.Goalkeeper => Goalkeeper,
        PlayerRole.CentreBack => CentreBack,
        PlayerRole.RightBack or PlayerRole.LeftBack => FullBack,
        PlayerRole.DefensiveMidfielder => DefensiveMidfielder,
        PlayerRole.CentralMidfielder => CentralMidfielder,
        PlayerRole.AttackingMidfielder => AttackingMidfielder,
        PlayerRole.RightMidfielder or PlayerRole.LeftMidfielder => WideMidfielder,
        PlayerRole.Striker => Striker,
        PlayerRole.RightWinger or PlayerRole.LeftWinger => Winger,
        PlayerRole.SecondStriker => SecondStriker,
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };

    /// <summary>Filtra categorías/códigos públicos, aplica relevancia y límites descriptivos; desempata por código ordinal.</summary>
    public static PlayerPositionInsights Describe(PlayerRole? primary, IEnumerable<VisibleAttributeGroup> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);
        if (primary is null) return new PlayerPositionInsights(Array.Empty<VisibleAttribute>(), Array.Empty<VisibleAttribute>());
        var relevant = RelevantFor(primary.Value).ToHashSet(StringComparer.Ordinal);
        var publicDefinitions = PlayerAttributeCatalog.All.Where(d => !d.IsHidden).ToDictionary(d => d.Code, StringComparer.Ordinal);
        var candidates = groups.Where(g => g.Category != AttributeCategory.Hidden)
            .SelectMany(g => g.Values.Where(v => publicDefinitions.TryGetValue(v.Code, out var d) && d.Category == g.Category))
            .Where(v => relevant.Contains(v.Code)).ToArray();
        if (candidates.Select(v => v.Code).Distinct(StringComparer.Ordinal).Count() != candidates.Length)
            throw new InvalidDataException("Atributos públicos repetidos en la descripción.");
        if (candidates.Any(v => v.Value is < 1 or > 20)) throw new ArgumentOutOfRangeException(nameof(groups));
        var strengths = candidates.Where(v => v.Value >= StrengthMinimum).OrderByDescending(v => v.Value)
            .ThenBy(v => v.Code, StringComparer.Ordinal).Take(MaximumItems).ToArray();
        var weaknesses = candidates.Where(v => v.Value <= WeaknessMaximum).OrderBy(v => v.Value)
            .ThenBy(v => v.Code, StringComparer.Ordinal).Take(MaximumItems).ToArray();
        return new PlayerPositionInsights(Array.AsReadOnly(strengths), Array.AsReadOnly(weaknesses));
    }

    private static IReadOnlyList<string> Codes(string codes) => Array.AsReadOnly(codes.Split(' '));
}
