using Furvo.Core.Domain;
namespace Furvo.Core.Development;

public enum TrainingAvailabilityState { Available=0, Unavailable=1, Partial=2 }
public enum TrainingRestrictionReason { Injury=1, Illness=2, InternationalDuty=3, PersonalLeave=4, MedicalRestriction=5, Other=6 }
/// <summary>Registro administrativo; no representa un diagnóstico ni calcula recuperación.</summary>
public sealed record TrainingRestriction(Guid Id,PlayerId Player,TrainingAvailabilityState State,TrainingRestrictionReason Reason,
    DateOnly Start,DateOnly? End=null,string? Notes=null,string? Origin=null,IReadOnlyList<string>? AllowedExercises=null,int Revision=0)
{
    public void Validate()
    {
        if(Id==Guid.Empty||Player.Value==Guid.Empty||!Enum.IsDefined(State)||State==TrainingAvailabilityState.Available||!Enum.IsDefined(Reason)||Start==default||End is {} end&&(end==default||end<Start)||Revision<0)
            throw new InvalidDataException("Identidad, estado, motivo o fechas de la restricción no válidos.");
        var allowed=AllowedExercises??Array.Empty<string>();
        if(allowed.Distinct(StringComparer.Ordinal).Count()!=allowed.Count||allowed.Any(code=>!TrainingCatalog.Exercises.Any(e=>e.Code==code))||State!=TrainingAvailabilityState.Partial&&allowed.Count>0)
            throw new InvalidDataException("Las actividades autorizadas deben ser explícitas, válidas y exclusivas de una restricción parcial.");
    }
    public bool Active(DateOnly date)=>date>=Start&&(End is null||date<=End);
}
public sealed record TrainingAvailabilityDecision(TrainingAvailabilityState State,bool Allowed,IReadOnlyList<TrainingRestriction> Restrictions);
public sealed record TrainingAvailabilityConflict(Guid Session,DateOnly Date,string Exercise,PlayerId Player,TrainingAvailabilityDecision Decision);
public sealed record TrainingParticipantAvailability(IReadOnlyList<PlayerId> Included,IReadOnlyDictionary<PlayerId,TrainingAvailabilityDecision> Excluded);
/// <summary>Regla compartida por selectores, persistencia y futuros consumidores; fechas inclusivas y ninguna inferencia médica.</summary>
public static class TrainingAvailability
{
    public static TrainingAvailabilityDecision Check(PlayerId player,DateOnly date,string exercise,IReadOnlyList<TrainingRestriction> restrictions)
    {
        _=TrainingCatalog.Get(exercise);
        var active=restrictions.Where(r=>r.Player==player&&r.Active(date)).OrderBy(r=>r.Start).ThenBy(r=>r.Id).ToArray();
        foreach(var entry in active)entry.Validate();
        var state=active.Any(r=>r.State==TrainingAvailabilityState.Unavailable)?TrainingAvailabilityState.Unavailable:active.Length>0?TrainingAvailabilityState.Partial:TrainingAvailabilityState.Available;
        return new(state,active.All(r=>r.State==TrainingAvailabilityState.Partial&&r.AllowedExercises?.Contains(exercise)==true),active);
    }
    public static TrainingParticipantAvailability Select(TrainingTarget target,IReadOnlyList<TrainingCandidate> squad,IReadOnlyList<TrainingGroup> groups,DateOnly date,string exercise,IReadOnlyList<TrainingRestriction> restrictions)
    {
        var ids=TrainingRules.Resolve(target,squad,groups);var decisions=ids.ToDictionary(id=>id,id=>Check(id,date,exercise,restrictions));
        return new(ids.Where(id=>decisions[id].Allowed).ToArray(),decisions.Where(p=>!p.Value.Allowed).ToDictionary());
    }
    public static IReadOnlyList<TrainingAvailabilityConflict> Conflicts(IEnumerable<TrainingSession> sessions,IReadOnlyList<TrainingRestriction> restrictions,DateOnly from,IReadOnlySet<Guid>? completed=null)
        =>sessions.Where(s=>s.Date>=from&&completed?.Contains(s.Id)!=true).SelectMany(s=>s.Participants.Select(p=>new TrainingAvailabilityConflict(s.Id,s.Date,s.Exercise,p,Check(p,s.Date,s.Exercise,restrictions))).Where(c=>!c.Decision.Allowed)).OrderBy(c=>c.Date).ThenBy(c=>c.Session).ThenBy(c=>c.Player.ToString(),StringComparer.Ordinal).ToArray();
    public static void Validate(TrainingSession session,IReadOnlyList<TrainingRestriction> restrictions)
    {
        var conflicts=Conflicts(new[]{session},restrictions,session.Date);
        if(conflicts.Count>0){var error=new InvalidDataException("Hay participantes no autorizados para este ejercicio en su fecha. Revisa sus restricciones de disponibilidad.");error.Data[nameof(TrainingAvailabilityConflict)]=conflicts;throw error;}
    }
}
