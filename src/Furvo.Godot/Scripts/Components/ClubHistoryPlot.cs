using Furvo.Core.Queries;

namespace Furvo.Presentation.Components;

/// <summary>Coordenada de un resultado conocido; los vacíos mantienen su hueco y no se convierten en clasificaciones.</summary>
public sealed record ClubHistoryPlotPoint(ClubSeasonStanding Standing, double X, double Y);

/// <summary>Preparación visual del gráfico, sin generar temporadas, resultados, ascensos o rankings históricos.</summary>
public static class ClubHistoryPlot
{
    public static IReadOnlyList<ClubHistoryPlotPoint> Build(IEnumerable<ClubSeasonStanding> standings)
    {
        var rows = standings.OrderBy(s => s.StartYear).ThenBy(s => s.Season, StringComparer.Ordinal).ToArray();
        if (rows.Select(s => s.StartYear).Distinct().Count() != rows.Length) throw new ArgumentException("Dos filas de liga para una temporada.");
        if (rows.Any(s => s.Division is < 1 or > 2 || s.FinalPosition is < 1 or > 20))
            throw new ArgumentOutOfRangeException(nameof(standings));
        return Array.AsReadOnly(rows.Select((s, i) => (Standing: s, Index: i)).Where(x => x.Standing.FinalPosition.HasValue)
            .Select(x => new ClubHistoryPlotPoint(x.Standing, rows.Length == 1 ? 0.5 : (double)x.Index / (rows.Length - 1),
                (x.Standing.Division == 1 ? 0 : 0.55) + (x.Standing.FinalPosition!.Value - 1) / 19.0 * 0.45)).ToArray());
    }

    public static bool CanConnect(ClubHistoryPlotPoint previous, ClubHistoryPlotPoint next) =>
        previous.Standing.StartYear + 1 == next.Standing.StartYear;
}
