namespace Furvo.Core.Development;
public enum TrainingCalendarState { Planned, Rest, Completed }
public sealed record TrainingCalendarEntry(TrainingSession Session,TrainingArea Area,TrainingCalendarState State,TrainingTurn? Turn=null);
public sealed record TrainingCalendarDay(DateOnly Date,bool IsCurrent,IReadOnlyList<TrainingCalendarEntry> Entries,TrainingDayPlan? Configuration=null,int LegacyRestReviewCount=0);
public sealed record TrainingCalendarWeek(DateOnly Start,IReadOnlyList<TrainingCalendarDay> Days)
{
    /// <summary>Una sola planificación, semana de lunes a domingo y orden estable; el filtro nunca duplica ni modifica sesiones.</summary>
    public static TrainingCalendarWeek Create(TrainingPlan plan,DateOnly reference,DateOnly careerDate,TrainingArea? area=null,IReadOnlySet<Guid>? completed=null)
    {
        plan=TrainingRestCompatibility.Project(plan,completed);
        if(reference==default||careerDate==default||area is {} category&&!Enum.IsDefined(category))throw new ArgumentException("Fecha o categoría de calendario inválida.");
        var monday=reference.AddDays(-((7+(int)reference.DayOfWeek-1)%7));
        var days=Enumerable.Range(0,7).Select(n=>monday.AddDays(n)).Select(day=>new TrainingCalendarDay(day,day==careerDate,
            plan.Sessions.Where(s=>s.Date==day&&(area is null||TrainingCatalog.Get(s.Exercise).Area==area))
            .OrderBy(s=>s.Start).ThenBy(s=>s.Id).Select(s=>new TrainingCalendarEntry(s,TrainingCatalog.Get(s.Exercise).Area,
                completed?.Contains(s.Id)==true?TrainingCalendarState.Completed:TrainingCalendarState.Planned,TrainingSchedule.Classify(s))).ToArray(),TrainingSchedule.Day(plan,day),(plan.LegacyRests??Array.Empty<LegacyTrainingRest>()).Count(r=>r.Original.Date==day&&r.Resolution==LegacyRestResolution.Review))).ToArray();
        return new(monday,days);
    }
}
