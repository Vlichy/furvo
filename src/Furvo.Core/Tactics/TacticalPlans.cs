using System.Security.Cryptography;
using System.Text;
using Furvo.Core.Domain;
using Furvo.Core.Lineups;
namespace Furvo.Core.Tactics;
public enum TacticalPlanKind { Initial=1,Winning=2,Losing=3 }
public enum PossessionPhase { WithBall=1,WithoutBall=2 }
public sealed record PlanRoster(string Seed,int BenchLimit,IReadOnlyList<LineupMember> Members);
public sealed record TacticalDisposition(FormationDefinition Reference,IReadOnlyList<FormationSlot> Positions,bool IsCustom,IReadOnlyDictionary<string,SlotBehaviour>? Behaviours=null)
{
    public FormationDefinition Formation=>new(Reference.Id,Positions);
    public SlotBehaviour Behaviour(string slot)=>Behaviours?.GetValueOrDefault(slot)??IndividualTactics.Initial(Positions.Single(s=>s.Id==slot).Role);
}
/// <summary>Un solo mapa puesto→futbolista por plan. Las fases guardan geometría/demarcación y no otro once contradictorio.</summary>
public sealed record TacticalPlan(Guid Id,TacticalPlanKind Kind,PlanRoster Roster,TacticalDisposition WithBall,
    TacticalDisposition WithoutBall,TeamMentality? Mentality,IReadOnlyDictionary<string,string?> Instructions)
{
    public TacticalDisposition Disposition(PossessionPhase phase)=>phase switch{PossessionPhase.WithBall=>WithBall,PossessionPhase.WithoutBall=>WithoutBall,_=>throw new ArgumentOutOfRangeException(nameof(phase))};
    public TacticalPlan WithDisposition(PossessionPhase phase,TacticalDisposition disposition)=>phase switch{PossessionPhase.WithBall=>this with{WithBall=disposition},PossessionPhase.WithoutBall=>this with{WithoutBall=disposition},_=>throw new ArgumentOutOfRangeException(nameof(phase))};
    public CareerLineup View(ClubId club,int revision,PossessionPhase phase)=>new(club,Roster.Seed,Disposition(phase).Formation,Roster.BenchLimit,revision,Roster.Members);
}
public sealed record TacticalPlanBook(ClubId ClubId,int Revision,IReadOnlyList<TacticalPlan> Plans)
{
    public TacticalPlan Get(TacticalPlanKind kind)=>Plans.Single(p=>p.Kind==kind);
    public TacticalPlanBook Replace(TacticalPlan plan)=>this with{Plans=Array.AsReadOnly(Plans.Select(p=>p.Kind==plan.Kind?plan:p).ToArray())};
}
public static class TacticalPlanRules
{
    public static TacticalPlanBook FromLegacy(Guid career,CareerLineup lineup,TacticalSettings settings)
    {
        Guid Id(TacticalPlanKind kind)=>new(SHA256.HashData(Encoding.UTF8.GetBytes("FURVO/plan/v1/"+career.ToString("D")+"/"+(int)kind)).Take(16).ToArray());
        var known=FormationCatalog.All.SingleOrDefault(f=>f.Id==lineup.Formation.Id);
        var phase=new TacticalDisposition(Clone(known??lineup.Formation),Array.AsReadOnly(lineup.Formation.Slots.ToArray()),known is null||!known.Slots.SequenceEqual(lineup.Formation.Slots));
        var source=new TacticalPlan(Id(TacticalPlanKind.Initial),TacticalPlanKind.Initial,new(lineup.Seed,lineup.BenchLimit,Array.AsReadOnly(lineup.Members.ToArray())),phase,phase,settings.Mentality,settings.Instructions.ToDictionary());
        return new(lineup.ClubId,0,Array.AsReadOnly(Enum.GetValues<TacticalPlanKind>().Select(kind=>DeepCopy(source,Id(kind),kind)).ToArray()));
    }
    public static FormationDefinition Clone(FormationDefinition formation)=>new(formation.Id,Array.AsReadOnly(formation.Slots.ToArray()));
    public static TacticalDisposition Clone(TacticalDisposition phase)=>new(Clone(phase.Reference),Array.AsReadOnly(phase.Positions.ToArray()),phase.IsCustom,phase.Behaviours?.ToDictionary(p=>p.Key,p=>p.Value with{Overrides=p.Value.Overrides.ToDictionary()}));
    public static TacticalPlan DeepCopy(TacticalPlan source,Guid id,TacticalPlanKind kind)=>new(id,kind,new(source.Roster.Seed,source.Roster.BenchLimit,Array.AsReadOnly(source.Roster.Members.ToArray())),Clone(source.WithBall),Clone(source.WithoutBall),source.Mentality,source.Instructions.ToDictionary());
    public static void Validate(TacticalPlanBook book,IReadOnlyList<LineupPlayer> players)
    {
        if(book.Revision<0||book.ClubId.Value==Guid.Empty||book.Plans.Count!=3||book.Plans.Select(p=>p.Id).Distinct().Count()!=3||book.Plans.Any(p=>p.Id==Guid.Empty)||!Enum.GetValues<TacticalPlanKind>().All(k=>book.Plans.Count(p=>p.Kind==k)==1))throw new InvalidDataException("Se requieren los tres planes, sin identidades repetidas.");
        foreach(var plan in book.Plans)
        {
            var settings=TacticalSettings.Undefined(book.ClubId) with{Mentality=plan.Mentality,Instructions=plan.Instructions};TacticalCatalog.Validate(settings,players.Select(p=>p.Id).ToHashSet());
            foreach(var phase in Enum.GetValues<PossessionPhase>())
            {
                var d=plan.Disposition(phase);FormationCatalog.Validate(d.Reference);LineupSelection.Validate(plan.View(book.ClubId,book.Revision,phase),players);
                if(d.Behaviours is not null)foreach(var entry in d.Behaviours){var slot=d.Positions.SingleOrDefault(s=>s.Id==entry.Key)??throw new InvalidDataException("Comportamiento en puesto inexistente.");IndividualTactics.Validate(entry.Value,slot.Role,phase);}
                if(!d.Reference.Slots.Select(s=>s.Id).Order().SequenceEqual(d.Positions.Select(s=>s.Id).Order())||
                    d.Positions.Any(s=>s.TacticalCode is not null&&!(s.TacticalCode=="CAD"&&s.Role==PlayerRole.RightBack||s.TacticalCode=="CAI"&&s.Role==PlayerRole.LeftBack)))
                    throw new InvalidDataException("Puestos o demarcaciones tácticas desconocidos.");
                for(var i=0;i<d.Positions.Count;i++)for(var j=i+1;j<d.Positions.Count;j++)
                    if(Distance(d.Positions[i].X,d.Positions[i].Y,d.Positions[j].X,d.Positions[j].Y)<.055)throw new InvalidDataException("Dos puestos se superponen. Separa sus posiciones.");
                if(!d.IsCustom&&!d.Positions.SequenceEqual(d.Reference.Slots))throw new InvalidDataException("La disposición modificada debe identificarse como personalizada.");
            }
        }
    }
    private static double Distance(double x,double y,double xx,double yy)=>Math.Sqrt(Math.Pow(x-xx,2)+Math.Pow(y-yy,2));
    public static TacticalPlan Move(TacticalPlan plan,PossessionPhase phase,string slot,double x,double y)
    {
        if(!double.IsFinite(x)||!double.IsFinite(y)||x is <0 or >1||y is <0 or >1)throw new InvalidDataException("El puesto debe quedar dentro del campo.");
        var d=plan.Disposition(phase);if(!d.Positions.Any(s=>s.Id==slot))throw new InvalidDataException("Puesto inexistente.");
        if(d.Positions.Any(s=>s.Id!=slot&&Distance(x,y,s.X,s.Y)<.055))throw new InvalidDataException("Destino demasiado próximo a otro jugador.");
        return plan.WithDisposition(phase,d with{Positions=Array.AsReadOnly(d.Positions.Select(s=>s.Id==slot?s with{X=x,Y=y}:s).ToArray()),IsCustom=true});
    }
    public static TacticalPlan SetRole(TacticalPlan plan,PossessionPhase phase,string slot,PlayerRole role,string? code=null)
    {
        var d=plan.Disposition(phase);var old=d.Positions.Single(s=>s.Id==slot);
        if(!Enum.IsDefined(role)||(old.Role==PlayerRole.Goalkeeper)!=(role==PlayerRole.Goalkeeper))throw new InvalidDataException("El portero debe conservar su demarcación.");
        var behaviours=d.Behaviours?.ToDictionary();
        if(behaviours?.TryGetValue(slot,out var behaviour)==true&&!IndividualTactics.Roles.Any(r=>r.Code==behaviour.Role&&r.Positions.Contains(role)))behaviours[slot]=IndividualTactics.Initial(role);
        else if(behaviours?.TryGetValue(slot,out behaviour)==true)behaviours[slot]=behaviour with{Overrides=behaviour.Overrides.Where(v=>IndividualTactics.Available(role,phase).Any(i=>i.Code==v.Key)).ToDictionary()};
        return plan.WithDisposition(phase,d with{Positions=Array.AsReadOnly(d.Positions.Select(s=>s.Id==slot?s with{Role=role,TacticalCode=code}:s).ToArray()),IsCustom=true,Behaviours=behaviours});
    }
    /// <summary>Una única edición atómica: geometría y demarcación de la fase activa; no cambia posiciones naturales.</summary>
    public static TacticalPlan MoveAndAssign(TacticalPlan plan,PossessionPhase phase,string slot,double x,double y)
    {
        var before=plan.Disposition(phase).Positions.Single(s=>s.Id==slot);var moved=Move(plan,phase,slot,x,y);
        var role=Propose(before,x,y,false);return SetRole(moved,phase,slot,role.Role,role.Code);
    }
    public static (PlayerRole Role,string? Code) Propose(FormationSlot before,double x,double y,bool retainNearby=true)
    {
        if(before.Role==PlayerRole.Goalkeeper||retainNearby&&Distance(before.X,before.Y,x,y)<.08)return(before.Role,before.TacticalCode);
        if(y<.25||y>.75)
        {
            var left=y<.25;
            if(x<.34)return(left?PlayerRole.LeftBack:PlayerRole.RightBack,null);
            if(x<.48)return(left?PlayerRole.LeftBack:PlayerRole.RightBack,left?"CAI":"CAD");
            return(x<.62?(left?PlayerRole.LeftMidfielder:PlayerRole.RightMidfielder):(left?PlayerRole.LeftWinger:PlayerRole.RightWinger),null);
        }
        return(x<.32?PlayerRole.CentreBack:x<.46?PlayerRole.DefensiveMidfielder:x<.62?PlayerRole.CentralMidfielder:x<.78?PlayerRole.AttackingMidfielder:PlayerRole.Striker,null);
    }
    /// <summary>Aplica composición y geometría sin duplicar el editor entre primer equipo y cantera.</summary>
    public static TacticalPlan ApplyLineup(TacticalPlan plan,PossessionPhase phase,CareerLineup proposed)
    {
        var before=plan.Disposition(phase);var d=before;
        if(before.Reference.Id!=proposed.Formation.Id){var reference=FormationCatalog.All.SingleOrDefault(f=>f.Id==proposed.Formation.Id)??proposed.Formation;d=new(TacticalPlanRules.Clone(reference),Array.AsReadOnly(proposed.Formation.Slots.ToArray()),!reference.Slots.SequenceEqual(proposed.Formation.Slots),AdaptBehaviours(before,proposed.Formation.Slots,phase));}
        else if(!before.Positions.SequenceEqual(proposed.Formation.Slots))d=before with{Positions=Array.AsReadOnly(proposed.Formation.Slots.ToArray()),IsCustom=true};
        var oldStarters=plan.Roster.Members.Where(m=>m.Role==LineupRole.Starter).ToArray();
        var newStarters=proposed.Members.Where(m=>m.Role==LineupRole.Starter).ToArray();
        if(oldStarters.Select(m=>m.PlayerId).ToHashSet().SetEquals(newStarters.Select(m=>m.PlayerId))&&oldStarters.Any(m=>newStarters.Single(n=>n.PlayerId==m.PlayerId).SlotId!=m.SlotId))
        {
            // Entre titulares se intercambia la ubicación SOLO en esta fase; las anclas de identidad permanecen comunes.
            var positions=oldStarters.Select(member=>d.Positions.Single(s=>s.Id==newStarters.Single(n=>n.PlayerId==member.PlayerId).SlotId) with{Id=member.SlotId!}).ToArray();
            d=d with{Positions=Array.AsReadOnly(positions),IsCustom=true};
            plan=plan with{Roster=plan.Roster with{Members=Array.AsReadOnly(oldStarters.Concat(proposed.Members.Where(m=>m.Role!=LineupRole.Starter)).OrderBy(m=>m.PlayerId.ToString(),StringComparer.Ordinal).ToArray())}};
        }
        else plan=plan with{Roster=plan.Roster with{Members=Array.AsReadOnly(proposed.Members.ToArray())}};
        plan=plan.WithDisposition(phase,d);
        return plan;
    }
    private static IReadOnlyDictionary<string,SlotBehaviour>? AdaptBehaviours(TacticalDisposition source,IReadOnlyList<FormationSlot> slots,PossessionPhase phase)
    {
        if(source.Behaviours is null)return null;var result=new Dictionary<string,SlotBehaviour>();
        foreach(var slot in slots){if(!source.Behaviours.TryGetValue(slot.Id,out var value))continue;if(!IndividualTactics.Roles.Any(r=>r.Code==value.Role&&r.Positions.Contains(slot.Role)))value=IndividualTactics.Initial(slot.Role);result[slot.Id]=value with{Overrides=value.Overrides.Where(v=>IndividualTactics.Available(slot.Role,phase).Any(i=>i.Code==v.Key)).ToDictionary()};}return result;
    }
    public static TacticalPlan SetReference(TacticalPlan plan,PossessionPhase phase,FormationDefinition reference)=>plan.WithDisposition(phase,new(Clone(reference),Array.AsReadOnly(reference.Slots.ToArray()),false,AdaptBehaviours(plan.Disposition(phase),reference.Slots,phase)));
    public static TacticalPlan Reset(TacticalPlan plan,PossessionPhase phase)=>SetReference(plan,phase,plan.Disposition(phase).Reference);
    public static TacticalPlan CopyDisposition(TacticalPlan target,PossessionPhase phase,TacticalPlan source,PossessionPhase sourcePhase)=>target.WithDisposition(phase,Clone(source.Disposition(sourcePhase)) with{Behaviours=AdaptBehaviours(source.Disposition(sourcePhase),source.Disposition(sourcePhase).Positions,phase)});
}
