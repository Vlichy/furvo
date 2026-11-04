using Furvo.Core.Tactics;
namespace Furvo.Core.Development;
/// <summary>Compatibilidad de ejercicios y consulta de configuraciones reales; no crea ni modifica tácticas.</summary>
public static class TrainingRoutines
{
    public static IReadOnlyList<SetPieceKind> Compatible(string exercise)=>exercise switch{
        "corner_practice"=>new[]{SetPieceKind.AttackingCorner,SetPieceKind.DefendingCorner},
        "free_kick_practice"=>new[]{SetPieceKind.DirectFreeKick,SetPieceKind.IndirectFreeKick,SetPieceKind.DefendingFreeKick},
        "penalty_practice"=>new[]{SetPieceKind.Penalty},"throw_practice"=>new[]{SetPieceKind.ThrowIn},_=>Array.Empty<SetPieceKind>()};
    public static bool Located(SetPieceKind kind)=>kind is SetPieceKind.DirectFreeKick or SetPieceKind.IndirectFreeKick or SetPieceKind.DefendingFreeKick;
    public static SetPiecePlan Resolve(TacticalSettings settings,RoutineReference reference)
    {if(!Enum.IsDefined(reference.Kind)||reference.Location is {} location&&(!Enum.IsDefined(location)||!Located(reference.Kind)))throw new InvalidDataException("Referencia o ubicación de jugada incompatible.");return reference.Location is {} value?settings.FreeKick(reference.Kind,value):settings.SetPieces[reference.Kind];}
    public static bool Designed(TacticalSettings? settings,RoutineReference reference){if(settings is null)return false;var plan=Resolve(settings,reference);return reference.Kind==SetPieceKind.Penalty||plan.Routine is not null;}
    public static void Validate(string exercise,TacticalSettings? settings,RoutineReference reference)
    {if(!Compatible(exercise).Contains(reference.Kind))throw new InvalidDataException("La jugada no corresponde al ejercicio seleccionado.");if(!Designed(settings,reference))throw new InvalidDataException("No hay ninguna jugada diseñada para esta ubicación. Configúrala primero en Táctica.");}
}
