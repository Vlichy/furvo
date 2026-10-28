using Furvo.Core.Domain;
using Furvo.Core.Queries;
namespace Furvo.Presentation.Components;
public enum SquadAssignment { Starter=1,Substitute=2,Reserve=3,Unassigned=4 }
public enum CareerSquadSort { Assignment,Position,Player,Attribute1,Attribute2,Attribute3 }
public sealed record CareerSquadRow(PlayerSummary Player,SquadAssignment Assignment,PlayerRole Position,IReadOnlyList<VisibleAttribute> Highlights);
/// <summary>Selección contextual descriptiva, sin calidad global, condición ni asignaciones inventadas.</summary>
public static class CareerSquadRules
{
    public static IReadOnlyList<string> CodesFor(PlayerRole role)=>Array.AsReadOnly((role switch{
        PlayerRole.Goalkeeper=>"reflexes one_on_ones goalkeeping_positioning",
        PlayerRole.CentreBack=>"marking tackling defensive_positioning",
        PlayerRole.RightBack or PlayerRole.LeftBack=>"tackling pace crossing",
        PlayerRole.DefensiveMidfielder=>"anticipation defensive_positioning short_passing",
        PlayerRole.CentralMidfielder=>"short_passing vision decisions",
        PlayerRole.AttackingMidfielder=>"vision technique creativity",
        PlayerRole.RightMidfielder or PlayerRole.LeftMidfielder=>"crossing short_passing stamina",
        PlayerRole.RightWinger or PlayerRole.LeftWinger=>"dribbling acceleration off_the_ball",
        PlayerRole.Striker=>"finishing off_the_ball composure",
        PlayerRole.SecondStriker=>"first_touch short_passing finishing",
        _=>throw new ArgumentOutOfRangeException(nameof(role))}).Split(' '));
    public static int PositionOrder(PlayerRole role)=>role switch{
        PlayerRole.Goalkeeper=>1,PlayerRole.CentreBack=>2,PlayerRole.RightBack=>3,PlayerRole.LeftBack=>4,
        PlayerRole.DefensiveMidfielder=>5,PlayerRole.CentralMidfielder=>6,PlayerRole.AttackingMidfielder=>7,
        PlayerRole.RightMidfielder=>8,PlayerRole.LeftMidfielder=>9,PlayerRole.RightWinger=>10,PlayerRole.LeftWinger=>11,
        PlayerRole.SecondStriker=>12,PlayerRole.Striker=>13,_=>throw new ArgumentOutOfRangeException(nameof(role))};
    public static CareerSquadRow Create(PlayerDetails player,PlayerRole? context=null,SquadAssignment assignment=SquadAssignment.Unassigned)
    {
        if(!Enum.IsDefined(assignment))throw new ArgumentOutOfRangeException(nameof(assignment));
        var role=context??player.Player.Position??throw new InvalidDataException("Falta demarcación.");
        var visible=PlayerAttributeCatalog.All.Where(d=>!d.IsHidden).ToDictionary(d=>d.Code,StringComparer.Ordinal);
        var values=player.AttributeGroups.Where(g=>g.Category!=AttributeCategory.Hidden).SelectMany(g=>g.Values.Where(v=>visible.TryGetValue(v.Code,out var d)&&d.Category==g.Category)).ToDictionary(v=>v.Code,StringComparer.Ordinal);
        var selected=CodesFor(role).Select(code=>values.TryGetValue(code,out var value)&&value.Value is >=1 and <=20?value:throw new InvalidDataException("Falta un atributo visible válido: "+code)).ToArray();
        return new(player.Player,assignment,role,Array.AsReadOnly(selected));
    }
    public static IReadOnlyList<CareerSquadRow> Sort(IEnumerable<CareerSquadRow> rows,CareerSquadSort sort=CareerSquadSort.Assignment,bool descending=false)
    {
        IOrderedEnumerable<CareerSquadRow> ordered;
        if(sort==CareerSquadSort.Player)ordered=descending?rows.OrderByDescending(r=>r.Player.Name,StringComparer.OrdinalIgnoreCase):rows.OrderBy(r=>r.Player.Name,StringComparer.OrdinalIgnoreCase);
        else
        {
            int Key(CareerSquadRow r)=>sort switch{CareerSquadSort.Assignment=>(int)r.Assignment,CareerSquadSort.Position=>PositionOrder(r.Position),
                CareerSquadSort.Attribute1=>r.Highlights[0].Value,CareerSquadSort.Attribute2=>r.Highlights[1].Value,CareerSquadSort.Attribute3=>r.Highlights[2].Value,_=>throw new ArgumentOutOfRangeException(nameof(sort))};
            ordered=descending?rows.OrderByDescending(Key):rows.OrderBy(Key);
        }
        return Array.AsReadOnly(ordered.ThenBy(r=>PositionOrder(r.Position)).ThenBy(r=>r.Player.Name,StringComparer.OrdinalIgnoreCase).ThenBy(r=>r.Player.Id.ToString(),StringComparer.Ordinal).ToArray());
    }
}
