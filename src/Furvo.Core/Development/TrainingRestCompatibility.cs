namespace Furvo.Core.Development;
public enum LegacyRestResolution { Review=1,Converted=2 }
/// <summary>Registro histórico íntegro de un bloque obsoleto; nunca se ejecuta ni se cuenta como ejercicio.</summary>
public sealed record LegacyTrainingRest(TrainingSession Original,LegacyRestResolution Resolution=LegacyRestResolution.Review,bool WasExecuted=false);
public static class TrainingRestCompatibility
{
    /// <summary>Proyección de solo lectura. Solo un descanso de jornada ya confirmado y sin ejercicios permite conversión inequívoca.</summary>
    public static TrainingPlan Project(TrainingPlan plan,IReadOnlySet<Guid>? executed=null)
    {
        var records=(plan.LegacyRests??Array.Empty<LegacyTrainingRest>()).ToDictionary(r=>r.Original.Id);
        foreach(var block in plan.Sessions.Where(s=>s.Exercise=="rest")){
            if(!records.ContainsKey(block.Id))records.Add(block.Id,new(block,WasExecuted:executed?.Contains(block.Id)==true));
        }
        var active=plan.Sessions.Where(s=>s.Exercise!="rest").ToArray();
        var history=records.Values.Select(record=>{
            var observed=record.WasExecuted||executed?.Contains(record.Original.Id)==true;
            var confirmed=plan.Days?.Any(d=>d.Date==record.Original.Date&&d.Kind==TrainingDayKind.Rest)==true&&!active.Any(s=>s.Date==record.Original.Date);
            return record with{WasExecuted=observed,Resolution=observed?LegacyRestResolution.Review:confirmed?LegacyRestResolution.Converted:record.Resolution};
        }).OrderBy(r=>r.Original.Date).ThenBy(r=>r.Original.Id).ToArray();
        return plan with{Sessions=active,LegacyRests=history};
    }
}
/// <summary>Puerto para futuros informes basados en observaciones verificadas; no impone límites ni inventa riesgos.</summary>
public interface ITrainingAdvisorySource
{
    IReadOnlyList<TrainingAdvisory> Review(TrainingPlan plan,DateOnly date);
}
public sealed record TrainingAdvisory(string Topic,string Message,string Evidence);
