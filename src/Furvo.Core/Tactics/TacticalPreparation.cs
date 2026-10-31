using Furvo.Core.Domain;
namespace Furvo.Core.Tactics;

public enum SpecialistKind { Captain=1,Penalty=2,DirectFreeKick=3,IndirectFreeKick=4,CornerLeft=5,CornerRight=6 }
public sealed record TeamPersonnel(IReadOnlyDictionary<SpecialistKind,IReadOnlyList<PlayerId>> Priorities,IReadOnlyList<PlayerId?>? CaptainSlots=null)
{
    public static TeamPersonnel Empty()=>new(new Dictionary<SpecialistKind,IReadOnlyList<PlayerId>>());
    public IReadOnlyList<PlayerId?> Captains=>CaptainSlots??Enumerable.Range(0,2).Select(i=>Priorities.GetValueOrDefault(SpecialistKind.Captain)?.Skip(i).Select(p=>(PlayerId?)p).FirstOrDefault()).ToArray();
    public IReadOnlyList<PlayerId> Order(SpecialistKind kind)=>kind==SpecialistKind.Captain?Captains.Where(p=>p is not null).Select(p=>p!.Value).ToArray():Priorities.GetValueOrDefault(kind)??Array.Empty<PlayerId>();
    public PlayerId? Available(SpecialistKind kind,IReadOnlySet<PlayerId> onPitch)=>Order(kind).Where(onPitch.Contains).Select(p=>(PlayerId?)p).FirstOrDefault();
    public void Validate(IReadOnlySet<PlayerId> squad)
    {if(Captains.Count!=2||Captains.Where(p=>p is not null).Distinct().Count()!=Captains.Count(p=>p is not null)||Captains.Any(p=>p is {} id&&!squad.Contains(id)))throw new InvalidDataException("Se requieren dos puestos de capitán, sin duplicados ni jugadores ajenos.");if(Priorities.Any(p=>!Enum.IsDefined(p.Key)||p.Value is null||p.Value.Count>25||p.Value.Distinct().Count()!=p.Value.Count||p.Value.Any(id=>!squad.Contains(id))))throw new InvalidDataException("Jerarquía repetida o futbolista ajeno al equipo.");}
}
public sealed record OpponentOrder(Guid MatchId,ClubId Opponent,PlayerId Player,bool ExtraPressure,bool TightMarking,bool DenySpace,bool WeakFoot,string Tackling)
{
    public void Validate(Guid match,ClubId opponent,IReadOnlySet<PlayerId> opponentSquad)
    {if(match==Guid.Empty||MatchId!=match||Opponent!=opponent||opponent.Value==Guid.Empty||!opponentSquad.Contains(Player)||Tackling is not ("cautious" or "normal" or "aggressive"))throw new InvalidDataException("Orden rival fuera de su encuentro, equipo o plantilla.");}
}
/// <summary>El futuro calendario verifica encuentro y adversario y devuelve su plantilla elegible; no hay proveedor ficticio de producción.</summary>
public interface IMatchPreparationContext
{
    IReadOnlySet<PlayerId> OpponentSquad(Guid match,ClubId ownClub,ClubId opponent);
}
public static class SpecialistSelection
{
    public static PlayerId? Resolve(TacticalSettings settings,SpecialistKind kind,IReadOnlySet<PlayerId> onPitch)
    {
        var personnel=settings.Personnel??TeamPersonnel.Empty();if(personnel.Order(kind).Count>0)return personnel.Available(kind,onPitch);
        var legacy=kind switch{SpecialistKind.Penalty=>settings.SetPieces[SetPieceKind.Penalty].Taker,SpecialistKind.DirectFreeKick=>settings.SetPieces[SetPieceKind.DirectFreeKick].Taker,SpecialistKind.IndirectFreeKick=>settings.SetPieces[SetPieceKind.IndirectFreeKick].Taker,SpecialistKind.CornerLeft or SpecialistKind.CornerRight=>settings.SetPieces[SetPieceKind.AttackingCorner].Taker,_=>null};
        return legacy is {} player&&onPitch.Contains(player)?player:null;
    }
}
public enum FamiliarityAspect { Formation=1,Role=2,Collective=3,Coordination=4 }
/// <summary>Sin observación no hay porcentaje. Los valores normalizados son un contrato de almacenamiento, no una fórmula de aprendizaje.</summary>
public sealed record FamiliarityObservation(PlayerId Player,FamiliarityAspect Aspect,string Configuration,decimal Value,DateOnly ObservedAt,string Evidence)
{
    public void Validate(IReadOnlySet<PlayerId> squad)
    {if(!squad.Contains(Player)||!Enum.IsDefined(Aspect)||string.IsNullOrWhiteSpace(Configuration)||Value is <0 or >1||ObservedAt==default||string.IsNullOrWhiteSpace(Evidence))throw new InvalidDataException("Observación de familiaridad inválida o sin evidencia.");}
}
public interface IFamiliarityObservations
{
    IReadOnlyList<FamiliarityObservation> Read();
    void Record(FamiliarityObservation observation);
}
/// <summary>El motor futuro aporta el plan explícito y la posesión real; abrir o guardar nunca activa cambios automáticos.</summary>
public sealed record TacticalMatchView(TacticalPlan Plan,TacticalDisposition Disposition,TeamPersonnel Personnel)
{
    public static TacticalMatchView Read(TacticalPlanBook book,TacticalPlanKind active,PossessionPhase phase,TacticalSettings settings)=>new(book.Get(active),book.Get(active).Disposition(phase),settings.Personnel??TeamPersonnel.Empty());
}
