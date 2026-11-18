using Furvo.Core.Domain;
namespace Furvo.Core.Development;
public sealed record PhysicalObservation(decimal Fatigue,decimal Preparation,decimal RelativeRisk,DateOnly Date);
public sealed record DevelopmentState(PlayerId Player,DateOnly? LastDay,PhysicalObservation? Physical,IReadOnlyDictionary<string,decimal> Accumulated,IReadOnlyDictionary<string,int> AttributeChanges,IReadOnlyList<Guid> Processed)
{public static DevelopmentState Empty(PlayerId id)=>new(id,null,null,new Dictionary<string,decimal>(),new Dictionary<string,int>(),Array.Empty<Guid>());}
public sealed record TrainingEvidence(Guid Session,DateOnly Date,IReadOnlyList<PlayerId> Attended,string Evidence);
public interface ITrainingExecutionClock
{DateTime CareerTime {get;} bool Verified(Guid team,TrainingEvidence evidence);}
/// <summary>Banco experimental determinista v0. No se ejecuta al planificar ni permite actuar como alta médica.</summary>
public static class DevelopmentEngine
{
    public const string Version="development-experimental-v0";
    public static DevelopmentState Process(Player player,DevelopmentState previous,DateOnly day,IReadOnlyList<TrainingSession> sessions,IReadOnlyList<TrainingEvidence> evidence,
        PhysicalObservation physical,int? CompetitiveMinutes=null,IReadOnlyDictionary<Guid, TrainingStaffContribution>? staff=null)
    {
        if(previous.Player!=player.Id||previous.LastDay is {} last&&day<=last||physical.Date>day||day==default||CompetitiveMinutes is <0 or >180||physical.Fatigue is <0 or >100||physical.Preparation is <0 or >100||physical.RelativeRisk is <0 or >1)throw new InvalidDataException("Estado, fecha u observación física no válidos.");
        var known=sessions.Where(s=>s.Date==day&&s.Participants.Contains(player.Id)).ToDictionary(s=>s.Id);var observations=evidence.Where(e=>e.Date==day&&e.Attended.Contains(player.Id)).ToArray();
        if(observations.Select(e=>e.Session).Distinct().Count()!=observations.Length||observations.Any(e=>!known.ContainsKey(e.Session)||string.IsNullOrWhiteSpace(e.Evidence)||e.Attended.Distinct().Count()!=e.Attended.Count||e.Attended.Any(id=>!known[e.Session].Participants.Contains(id))))throw new InvalidDataException("Asistencia duplicada, ajena o sin evidencia.");
        var processed=previous.Processed.ToHashSet();if(observations.Any(e=>processed.Contains(e.Session)))throw new InvalidDataException("La sesión ya fue contabilizada.");
        var attended=observations.Select(e=>known[e.Session]).OrderBy(s=>s.Start).ThenBy(s=>s.Id).ToArray();var load=attended.Sum(TrainingCatalog.PlannedLoad)+(CompetitiveMinutes??0)*1.2m;
        var rest=attended.Any(s=>TrainingCatalog.Get(s.Exercise).Area==TrainingArea.Recovery);var recovery=rest?12m:8m;
        var fatigue=Math.Clamp(physical.Fatigue+load*.18m-recovery,0,100);var useful=Math.Max(0,1-physical.Fatigue/100m)*Math.Max(0,1-load/220m);var age=player.AgeOn(day);
        var ageFactor=age<23?1m:age<29?.7m:age<34?.35m:.1m;var learning=(player.Attributes.AllValues.GetValueOrDefault("learning_ability",10)+10)/30m;
        var progress=previous.Accumulated.ToDictionary();var changes=previous.AttributeChanges.ToDictionary();
        foreach(var session in attended){processed.Add(session.Id);var exercise=TrainingCatalog.Get(session.Exercise);foreach(var code in exercise.Attributes){var baseValue=player.Attributes.VisibleValues.GetValueOrDefault(code);if(baseValue==0)continue;var current=baseValue+changes.GetValueOrDefault(code);var dose=Math.Min(1.2m,TrainingCatalog.PlannedLoad(session)/session.MinutesPerPlayer);var staffFactor=staff?.GetValueOrDefault(session.Id)?.Factor??1m;var stimulus=Math.Min(.03m,session.MinutesPerPlayer/3000m*dose*Math.Clamp(staffFactor,.95m,1.05m))*useful*ageFactor*learning*(21-current)/20m;progress[code]=Math.Clamp(progress.GetValueOrDefault(code)+stimulus,-1,1);}}
        // El declive es pequeño y requiere días realmente procesados; no existe un potencial global.
        if(age>=32)foreach(var attribute in PlayerAttributeCatalog.All.Where(a=>a.Category==AttributeCategory.Physical&&player.Attributes.VisibleValues.ContainsKey(a.Code)))progress[attribute.Code]=Math.Max(-1,progress.GetValueOrDefault(attribute.Code)-.0008m*(age-31));
        foreach(var code in progress.Keys.ToArray()){var value=player.Attributes.VisibleValues.GetValueOrDefault(code)+changes.GetValueOrDefault(code);if(progress[code]>=1&&value<20){changes[code]=changes.GetValueOrDefault(code)+1;progress[code]-=1;}else if(progress[code]<=-1&&value>1){changes[code]=changes.GetValueOrDefault(code)-1;progress[code]+=1;}}
        return new(player.Id,day,new(fatigue,Math.Clamp(physical.Preparation+load*.015m*(useful-.25m),0,100),Math.Clamp(fatigue/100m+Math.Max(0,load-90)/200m,0,1),day),progress,changes,observations.Length==0?previous.Processed:processed.Order().ToArray());
    }
}
