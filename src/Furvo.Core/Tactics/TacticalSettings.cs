using Furvo.Core.Domain;
namespace Furvo.Core.Tactics;
public enum TeamMentality { VeryDefensive=1,Defensive=2,Balanced=3,Attacking=4,VeryAttacking=5 }
public enum SetPieceKind { AttackingCorner=1,DefendingCorner=2,DirectFreeKick=3,IndirectFreeKick=4,DefendingFreeKick=5,Penalty=6,ThrowIn=7 }
public enum FreeKickLocation { Left=1,Right=2,Distant=3,Edge=4 }
public sealed record InstructionDefinition(string Code,string Group,IReadOnlyList<string> Options);
public enum SetPieceRole { Receiver=1,Finisher=2,Header=3,StayBack=4,Zone1=5,Zone2=6,Zone3=7,Marker1=8,Marker2=9,Marker3=10,Wall1=11,Wall2=12,Wall3=13,Wall4=14,Prone=15,NearPost=16,FarPost=17,Rebound=18 }
public sealed record SetPieceDetails(string? CrossTarget,string? Movement,string? Variant,int? WallSize,bool? Lying,
    IReadOnlyDictionary<SetPieceRole,PlayerId?> Assignments)
{
    public static SetPieceDetails Undefined()=>new(null,null,null,null,null,Enum.GetValues<SetPieceRole>().ToDictionary(r=>r,_=>(PlayerId?)null));
}
public sealed record SetPiecePlan(string? Routine,PlayerId? Taker,SetPieceDetails? Details=null)
{public SetPieceDetails Configuration=>Details??SetPieceDetails.Undefined();}
/// <summary>Una configuración activa por club de carrera. Null significa pendiente, nunca una preferencia inventada.</summary>
public sealed record TacticalSettings(ClubId ClubId,int Revision,TeamMentality? Mentality,
    IReadOnlyDictionary<string,string?> Instructions,IReadOnlyDictionary<SetPieceKind,SetPiecePlan> SetPieces,IReadOnlyList<PlayerId?>? PenaltyTakers=null,IReadOnlyDictionary<string,SetPiecePlan>? FreeKicks=null,TeamPersonnel? Personnel=null)
{
    /// <summary>Las ubicaciones nuevas empiezan pendientes; el legado se conserva en su contexto original.</summary>
    public SetPiecePlan FreeKick(SetPieceKind kind,FreeKickLocation location)=>FreeKicks?.GetValueOrDefault(FreeKickKey(kind,location))??
        (location==DefaultLocation(kind)?SetPieces[kind]:new SetPiecePlan(null,null));
    public static string FreeKickKey(SetPieceKind kind,FreeKickLocation location)=>$"{(int)kind}:{(int)location}";
    public static FreeKickLocation DefaultLocation(SetPieceKind kind)=>kind==SetPieceKind.IndirectFreeKick?FreeKickLocation.Distant:FreeKickLocation.Edge;
    public TacticalSettings AtLocation(SetPieceKind kind,FreeKickLocation location){var pieces=SetPieces.ToDictionary();pieces[kind]=FreeKick(kind,location);return this with{SetPieces=pieces};}
    public IReadOnlyList<PlayerId?> ShootoutOrder=>PenaltyTakers??Array.AsReadOnly(new PlayerId?[5]);
    public static TacticalSettings Undefined(ClubId club)=>new(club,0,null,
        TacticalCatalog.Instructions.ToDictionary(i=>i.Code,_=>(string?)null),
        Enum.GetValues<SetPieceKind>().ToDictionary(k=>k,_=>new SetPiecePlan(null,null)));
}
public static class TacticalCatalog
{
    public const int DataVersion=1;
    public static IReadOnlyList<InstructionDefinition> Instructions {get;}=Array.AsReadOnly(new InstructionDefinition[]{
        new("passing","possession",Array.AsReadOnly(new[]{"short","mixed","long"})),
        new("tempo","possession",Array.AsReadOnly(new[]{"slow","normal","fast"})),
        new("attack_width","possession",Array.AsReadOnly(new[]{"narrow","normal","wide"})),
        new("freedom","possession",Array.AsReadOnly(new[]{"disciplined","balanced","free"})),
        new("movement","possession",Array.AsReadOnly(new[]{"hold","mixed","exchange"})),
        new("progression","possession",Array.AsReadOnly(new[]{"flanks","mixed","centre"})),
        new("pressing","defending",Array.AsReadOnly(new[]{"low","medium","high"})),
        new("press_direction","defending",new[]{"none","left","right","centre","flanks"}),
        new("opponent_keeper","defending",new[]{"allow","press","selective"}),
        new("offside_trap","defending",new[]{"off","on"}),
        new("press_beaten","defending",new[]{"positions","structure","repress"}),
        new("defensive_line","defending",Array.AsReadOnly(new[]{"low","medium","high"})),
        new("marking","defending",Array.AsReadOnly(new[]{"zonal","mixed","man"})),
        new("defend_width","defending",Array.AsReadOnly(new[]{"narrow","normal","wide"})),
        new("organization","defending",Array.AsReadOnly(new[]{"hold","follow"})),
        new("tackling","defending",Array.AsReadOnly(new[]{"cautious","normal","aggressive"})),
        new("recovery","transition",Array.AsReadOnly(new[]{"counter","balance","keep"})),
        new("counter_route","transition",Array.AsReadOnly(new[]{"right","left","both","centre","long"})),
        new("loss","transition",Array.AsReadOnly(new[]{"counterpress","balance","regroup"})),
        new("keeper","transition",Array.AsReadOnly(new[]{"short","mixed","long"})),
        new("keeper_target","transition",Array.AsReadOnly(new[]{"defenders","flanks","forwards"}))
    });
    /// <summary>Completa únicamente claves incorporadas por esta versión, sin inventar preferencias.</summary>
    public static Dictionary<string,string?> UpgradeInstructions(IReadOnlyDictionary<string,string?> source){var result=source.ToDictionary();foreach(var definition in Instructions)result.TryAdd(definition.Code,null);return result;}
    public static SetPiecePlan UpgradePlan(SetPiecePlan plan){if(plan.Details is null)return plan;var a=plan.Configuration.Assignments.ToDictionary();foreach(var r in Enum.GetValues<SetPieceRole>())a.TryAdd(r,null);return plan with{Details=plan.Configuration with{Assignments=a}};}
    public static IReadOnlyList<string> FreeKickRoutines(SetPieceKind kind,FreeKickLocation location)=>kind==SetPieceKind.DefendingFreeKick?new[]{"zonal","mixed","man"}:
        location is FreeKickLocation.Left or FreeKickLocation.Right?new[]{"cross","short","rehearsed"}:location==FreeKickLocation.Distant?new[]{"direct","short","area","rehearsed"}:new[]{"direct","short","rehearsed"};
    public static IReadOnlyList<string> Routines(SetPieceKind kind)=>kind switch{
        SetPieceKind.AttackingCorner=>Array.AsReadOnly(new[]{"near_post","far_post","short","mixed"}),
        SetPieceKind.DefendingCorner or SetPieceKind.DefendingFreeKick=>Array.AsReadOnly(new[]{"zonal","mixed","man"}),
        SetPieceKind.DirectFreeKick=>Array.AsReadOnly(new[]{"direct","short","cross","rehearsed"}),
        SetPieceKind.Penalty=>Array.AsReadOnly(new[]{"direct"}),
        SetPieceKind.IndirectFreeKick=>Array.AsReadOnly(new[]{"direct","short","cross","rehearsed","area","mixed"}),
        SetPieceKind.ThrowIn=>Array.AsReadOnly(new[]{"short","long","flick","space","back","rehearsed","mixed"}),_=>throw new ArgumentOutOfRangeException(nameof(kind))};
    public static bool HasTaker(SetPieceKind kind)=>kind is not (SetPieceKind.DefendingCorner or SetPieceKind.DefendingFreeKick);
    /// <summary>Solo las funciones activas son simultáneas. Las preferencias inactivas se conservan.</summary>
    public static IReadOnlyList<SetPieceRole> ActiveRoles(SetPieceKind kind,SetPiecePlan plan)
    {
        var roles=new List<SetPieceRole>();var c=plan.Configuration;
        if(kind is SetPieceKind.DefendingCorner or SetPieceKind.DefendingFreeKick)
        {
            if(plan.Routine is "zonal" or "mixed")roles.AddRange(plan.Routine=="mixed"?new[]{SetPieceRole.Zone1,SetPieceRole.Zone2}:new[]{SetPieceRole.Zone1,SetPieceRole.Zone2,SetPieceRole.Zone3});
            if(plan.Routine is "man" or "mixed")roles.AddRange(plan.Routine=="mixed"?new[]{SetPieceRole.Marker1,SetPieceRole.Marker2}:new[]{SetPieceRole.Marker1,SetPieceRole.Marker2,SetPieceRole.Marker3});
            roles.AddRange(new[]{SetPieceRole.NearPost,SetPieceRole.FarPost,SetPieceRole.Rebound});
            if(kind==SetPieceKind.DefendingFreeKick&&c.WallSize is >0){roles.AddRange(new[]{SetPieceRole.Wall1,SetPieceRole.Wall2,SetPieceRole.Wall3,SetPieceRole.Wall4}.Take(c.WallSize.Value));if(c.Lying==true)roles.Add(SetPieceRole.Prone);}
        }
        else if(kind!=SetPieceKind.Penalty&&plan.Routine is not null)
        {
            if(plan.Routine is "short" or "back" or "space" or "flick" or "rehearsed")roles.Add(SetPieceRole.Receiver);
            if(plan.Routine=="flick")roles.Add(SetPieceRole.Header);
            if(plan.Routine is "cross" or "area" or "mixed" or "near_post" or "far_post" or "long" or "rehearsed")roles.Add(SetPieceRole.Finisher);
            if(c.Movement=="combined"&&(plan.Routine is "cross" or "area" or "mixed" or "near_post" or "far_post")&&!roles.Contains(SetPieceRole.Receiver))roles.Add(SetPieceRole.Receiver);
            roles.Add(SetPieceRole.StayBack);
        }
        return roles.AsReadOnly();
    }
    /// <summary>Valida únicamente configuración y pertenencia; no calcula ventajas ni calidad.</summary>
    public static void Validate(TacticalSettings state,IReadOnlySet<PlayerId> squad)
    {
        state.Personnel?.Validate(squad);
        if(state.ClubId.Value==Guid.Empty||state.Revision<0||state.Mentality is {} m&&!Enum.IsDefined(m))
            throw new InvalidDataException("Identidad, revisión o mentalidad táctica inválida.");
        if(state.Instructions is null||state.Instructions.Count!=Instructions.Count||
            Instructions.Any(d=>!state.Instructions.TryGetValue(d.Code,out var value)||value is not null&&!d.Options.Contains(value)))
            throw new InvalidDataException("Instrucciones tácticas incompletas o desconocidas.");
        if(state.SetPieces is null||state.SetPieces.Count!=Enum.GetValues<SetPieceKind>().Length)
            throw new InvalidDataException("Configuración de balón parado incompleta.");
        var order=state.ShootoutOrder;
        if(order.Count!=5||order.Where(p=>p is not null).Distinct().Count()!=order.Count(p=>p is not null)||order.Any(p=>p is {} id&&!squad.Contains(id)))
            throw new InvalidDataException("La tanda requiere cinco puestos, sin lanzadores repetidos ni ajenos al club.");
        if(state.FreeKicks is not null)foreach(var (key,plan) in state.FreeKicks){var parts=key.Split(':');if(parts.Length!=2||!int.TryParse(parts[0],out var k)||!int.TryParse(parts[1],out var l)||!Enum.IsDefined((FreeKickLocation)l)||k is not (3 or 4 or 5))throw new InvalidDataException("Ubicación de falta desconocida.");
            var kind=(SetPieceKind)k;var location=(FreeKickLocation)l;if(key!=TacticalSettings.FreeKickKey(kind,location)||plan is null)throw new InvalidDataException("Clave de falta no canónica.");if(plan.Routine is {} routine&&!FreeKickRoutines(kind,location).Contains(routine))throw new InvalidDataException("Jugada no válida para esta ubicación.");
            if(location is FreeKickLocation.Left or FreeKickLocation.Right&&plan.Configuration.Lying==true)throw new InvalidDataException("El jugador tumbado solo se configura en faltas centrales.");
            var pieces=state.SetPieces.ToDictionary();pieces[kind]=plan;Validate(state with{SetPieces=pieces,FreeKicks=null},squad);}
        foreach(var kind in Enum.GetValues<SetPieceKind>())
        {
            if(!state.SetPieces.TryGetValue(kind,out var plan)||plan is null||plan.Routine is {} routine&&!Routines(kind).Contains(routine)||
                plan.Taker is {} taker&&(!HasTaker(kind)||!squad.Contains(taker)))
                throw new InvalidDataException("Jugada o lanzador no válido para este club.");
            var c=plan.Configuration;
            if(c.CrossTarget is {} target&&!new[]{"near_post","far_post","centre","mixed"}.Contains(target)||
                c.Movement is {} movement&&!new[]{"run","late","combined"}.Contains(movement)||
                c.Variant is {} variant&&variant!="one_two"||
                c.WallSize is {} wall&&!new[]{0,1,2,3,4}.Contains(wall)||c.Lying==true&&kind!=SetPieceKind.DefendingFreeKick||
                c.Assignments is null||c.Assignments.Count!=Enum.GetValues<SetPieceRole>().Length||
                Enum.GetValues<SetPieceRole>().Any(r=>!c.Assignments.ContainsKey(r))||c.Assignments.Values.Any(p=>p is {} id&&!squad.Contains(id)))
                throw new InvalidDataException("Detalles o asignaciones de jugada no válidos.");
            var active=ActiveRoles(kind,plan).Select(r=>c.Assignments[r]).Where(p=>p is not null).Select(p=>p!.Value).ToList();
            if(kind!=SetPieceKind.Penalty&&plan.Taker is {} launcher)active.Add(launcher);
            if(active.Count!=active.Distinct().Count())throw new InvalidDataException("Un futbolista no puede asumir dos funciones simultáneas en la misma jugada.");
        }
    }
}
