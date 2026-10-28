using Furvo.Core.Domain;
using Furvo.Core.Queries;
namespace Furvo.Presentation.Components;
public enum SquadTrackingSort { Assignment,Position,Player,Physical,Rhythm,Morale,Played,Goals,Assists,Average }
public sealed record SquadTrackingRow(PlayerSummary Player,SquadAssignment Assignment,PlayerSportingStatus Status);
/// <summary>Ordena datos públicos disponibles; no calcula calidad, forma ni estadísticas deportivas.</summary>
public static class SquadTrackingRows
{
    public static IReadOnlyList<SquadTrackingRow> Sort(IEnumerable<SquadTrackingRow> rows,SquadTrackingSort sort=SquadTrackingSort.Assignment,bool descending=false)
    {
        IOrderedEnumerable<SquadTrackingRow> ordered;
        if(sort==SquadTrackingSort.Player)ordered=descending?rows.OrderByDescending(r=>r.Player.Name,StringComparer.OrdinalIgnoreCase):rows.OrderBy(r=>r.Player.Name,StringComparer.OrdinalIgnoreCase);
        else
        {
            decimal? Key(SquadTrackingRow r)=>sort switch{
                SquadTrackingSort.Assignment=>(int)r.Assignment,SquadTrackingSort.Position=>Position(r),SquadTrackingSort.Physical=>r.Status.Physical.Percentage,
                SquadTrackingSort.Rhythm=>r.Status.Rhythm.Percentage,SquadTrackingSort.Morale=>r.Status.Morale is {} morale?4-(int)morale:null,
                SquadTrackingSort.Played=>r.Status.Season?.Played,SquadTrackingSort.Goals=>r.Status.Season?.Goals,SquadTrackingSort.Assists=>r.Status.Season?.Assists,
                SquadTrackingSort.Average=>r.Status.Season?.Average,_=>throw new ArgumentOutOfRangeException(nameof(sort))};
            // Desconocidos al final en ambos sentidos, sin transformarlos en ceros.
            var known=rows.OrderBy(r=>Key(r) is null);
            ordered=descending?known.ThenByDescending(Key):known.ThenBy(Key);
        }
        return Array.AsReadOnly(ordered.ThenBy(Position).ThenBy(r=>r.Player.Name,StringComparer.OrdinalIgnoreCase).ThenBy(r=>r.Player.Id.ToString(),StringComparer.Ordinal).ToArray());
    }
    private static int Position(SquadTrackingRow row)=>row.Player.Position is {} role?CareerSquadRules.PositionOrder(role):int.MaxValue;
}
