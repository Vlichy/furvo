namespace Furvo.Core.Development;
public sealed record TrainingAgendaSummary(int ExerciseCount,int PlannedDays,int ActivityMinutes,int RestDays,
    IReadOnlyDictionary<TrainingArea,int> CategoryMinutes,IReadOnlyDictionary<TrainingTurn,int> TurnMinutes);
/// <summary>Resumen semanal de la agenda única. Son minutos de actividades, nunca fatiga ni suma de carga individual.</summary>
public static class TrainingAgenda
{
    public static TrainingAgendaSummary ReadWeek(TrainingPlan source,DateOnly reference)
    {
        var plan=TrainingRestCompatibility.Project(source);var week=TrainingCalendarWeek.Create(plan,reference,reference);
        var end=week.Start.AddDays(7);var blocks=week.Days.SelectMany(d=>d.Entries).Select(e=>e.Session).ToArray();
        var rests=week.Days.Count(d=>d.Configuration?.Kind==TrainingDayKind.Rest&&d.Entries.Count==0);
        var dates=blocks.Select(s=>s.Date).Concat((plan.Days??Array.Empty<TrainingDayPlan>()).Where(d=>d.Date>=week.Start&&d.Date<end).Select(d=>d.Date)).Distinct().Count();
        return new(blocks.Length,dates,blocks.Sum(s=>s.Minutes),rests,
            Enum.GetValues<TrainingArea>().ToDictionary(a=>a,a=>blocks.Where(s=>TrainingCatalog.Get(s.Exercise).Area==a).Sum(s=>s.Minutes)),
            Enum.GetValues<TrainingTurn>().ToDictionary(t=>t,t=>blocks.Where(s=>TrainingSchedule.Classify(s)==t).Sum(s=>s.Minutes)));
    }
    public static int TabFor(TrainingSession session)=>(int)TrainingCatalog.Get(session.Exercise).Area;
}
