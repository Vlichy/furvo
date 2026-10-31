using Furvo.Core.Domain;
using Furvo.Core.Lineups;
using Furvo.Core.Tactics;
namespace Furvo.Core.Academy;
public enum AcademyManagement { Direct=1,Delegated=2 }
public enum AcademyTacticMode { Synchronized=1,Own=2 }
public sealed record AcademyTeam(Guid Id,ClubId ClubId,string Name,string Category,int Level,int Size,string? Competition);
public sealed record AcademyStaff(Guid Id,Guid TeamId,string Name,string Role,string LinkState);
/// <summary>Miembros propios y demarcaciones de referencia; nunca guarda coordenadas heredadas.</summary>
public sealed record AcademyAssignments(PlanRoster Roster,IReadOnlyDictionary<string,PlayerRole> Roles);
public sealed record AcademyTeamState(AcademyTeam Team,AcademyStaff Coach,AcademyManagement Management,AcademyTacticMode TacticMode,int Revision,
    IReadOnlyDictionary<TacticalPlanKind,AcademyAssignments> Rosters,TacticalPlanBook? OwnBook,TacticalSettings Common);
/// <summary>Relación unidireccional: solo el esquema se hereda; los miembros siempre pertenecen al equipo inferior.</summary>
public static class AcademyTactics
{
    public static TacticalPlanBook Resolve(AcademyTeamState state,TacticalPlanBook source,IReadOnlyList<LineupPlayer> players)
    {
        if(state.TacticMode==AcademyTacticMode.Own){var own=state.OwnBook! with{Revision=state.Revision,Plans=state.OwnBook!.Plans.Select(p=>p with{Instructions=TacticalCatalog.UpgradeInstructions(p.Instructions)}).ToArray()};TacticalPlanRules.Validate(own,players);return own;}
        if(source.ClubId!=state.Team.ClubId)throw new InvalidDataException("El origen táctico no corresponde al club matriz.");
        var plans=new List<TacticalPlan>();
        foreach(var kind in Enum.GetValues<TacticalPlanKind>()){
            var origin=source.Get(kind);var roster=Reconcile(state.Team.ClubId,state.Rosters[kind].Roster,origin.WithoutBall.Formation,players,state.Rosters[kind].Roles);
            plans.Add(new(AcademyGenerator.Identity(state.Team.Id+"/plan/"+(int)kind),kind,roster,TacticalPlanRules.Clone(origin.WithBall),TacticalPlanRules.Clone(origin.WithoutBall),origin.Mentality,origin.Instructions.ToDictionary()));
        }
        var book=new TacticalPlanBook(state.Team.ClubId,checked(state.Revision+source.Revision),plans.AsReadOnly());TacticalPlanRules.Validate(book,players);return book;
    }
    public static PlanRoster Reconcile(ClubId club,PlanRoster previous,FormationDefinition formation,IReadOnlyList<LineupPlayer> players,IReadOnlyDictionary<string,PlayerRole>? previousRoles=null)
    {
        if(players.Count<11)throw new InvalidDataException("Cantera sin once disponible. No se crean jugadores para completar puestos.");
        var assigned=new List<LineupMember>();var used=new HashSet<PlayerId>();
        foreach(var slot in formation.Slots){var old=previous.Members.FirstOrDefault(m=>m.SlotId==slot.Id);if(old is not null&&players.Any(p=>p.Id==old.PlayerId&&LineupSelection.Compatibility(p,slot.Role)<99&&(previousRoles?.GetValueOrDefault(slot.Id)==slot.Role||LineupSelection.Compatibility(p,slot.Role)<2))&&used.Add(old.PlayerId))assigned.Add(old);}
        foreach(var slot in formation.Slots.Where(s=>assigned.All(m=>m.SlotId!=s.Id)).OrderBy(s=>s.Role==PlayerRole.Goalkeeper?0:1).ThenBy(s=>s.Id,StringComparer.Ordinal)){
            var player=players.Where(p=>!used.Contains(p.Id)&&LineupSelection.Compatibility(p,slot.Role)<99).OrderBy(p=>LineupSelection.Compatibility(p,slot.Role)).ThenByDescending(p=>LineupSelection.ProvisionalScore(p,slot.Role)).ThenBy(p=>p.Id.ToString(),StringComparer.Ordinal).FirstOrDefault()??throw new InvalidDataException("Sin futbolista elegible para un puesto sincronizado.");
            assigned.Add(new(player.Id,LineupRole.Starter,slot.Id));used.Add(player.Id);
        }
        var remaining=players.Where(p=>!used.Contains(p.Id)).OrderByDescending(p=>previous.Members.Any(m=>m.PlayerId==p.Id&&m.Role==LineupRole.Substitute)).ThenBy(p=>p.Id.ToString(),StringComparer.Ordinal).ToArray();
        var bench=Math.Min(previous.BenchLimit,remaining.Length);assigned.AddRange(remaining.Select((p,i)=>new LineupMember(p.Id,i<bench?LineupRole.Substitute:LineupRole.Reserve,null)));
        var result=previous with{Members=assigned.OrderBy(m=>m.PlayerId.ToString(),StringComparer.Ordinal).ToArray(),BenchLimit=bench};LineupSelection.Validate(new(club,result.Seed,formation,bench,0,result.Members),players);return result;
    }
}
