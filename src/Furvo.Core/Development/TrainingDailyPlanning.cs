using Furvo.Core.Domain;
namespace Furvo.Core.Development;
public sealed record TrainingDailyTurn(TrainingTurn? Turn,IReadOnlyList<TrainingSession> Blocks);
public sealed record TrainingDailyTotals(int MorningActivityMinutes,int AfternoonActivityMinutes,int PendingActivityMinutes,int ActivityMinutes,
    int EffectiveMinutes,IReadOnlyDictionary<PlayerId,int> IndividualMinutes);
public sealed record TrainingDailyView(DateOnly Date,TrainingDayPlan? Configuration,int GlobalCount,int VisibleCount,
    IReadOnlyList<TrainingDailyTurn> Turns,TrainingDailyTotals GlobalTotals,IReadOnlyList<LegacyTrainingRest>? LegacyRests=null);
public sealed record TrainingTimeProposal(TimeOnly Start,TimeSpan End);
/// <summary>Una proyección del plan existente: nunca genera ni guarda actividades y no contiene atributos ni fatiga observada.</summary>
public static class TrainingDailyPlanning
{
    public static TrainingDailyView Read(TrainingPlan plan,DateOnly date,TrainingArea? filter=null,TrainingSchedulePolicy? policy=null)
    {
        plan=TrainingRestCompatibility.Project(plan);
        policy??=new();policy.Validate();if(date==default||filter is {} area&&!Enum.IsDefined(area))throw new ArgumentException("Fecha o filtro inválido.");
        var all=plan.Sessions.Where(b=>b.Date==date).OrderBy(b=>b.Start).ThenBy(b=>b.Id).ToArray();
        if(all.Select(b=>b.Id).Distinct().Count()!=all.Length)throw new InvalidDataException("El día contiene bloques con identidad duplicada.");
        var configuration=TrainingSchedule.Day(plan,date,policy);var visible=all.Where(b=>filter is null||TrainingCatalog.Get(b.Exercise).Area==filter).ToArray();
        var shifts=new List<TrainingTurn?>();
        if(configuration?.Kind==TrainingDayKind.Single)shifts.Add(configuration.SingleTurn);
        else if(configuration?.Kind==TrainingDayKind.Double){shifts.Add(TrainingTurn.Morning);shifts.Add(TrainingTurn.Afternoon);}
        else if(configuration?.Kind!=TrainingDayKind.Rest)shifts.Add(TrainingTurn.Morning);
        foreach(var shift in all.Select(b=>TrainingSchedule.Classify(b,policy)))if(!shifts.Contains(shift))shifts.Add(shift);
        var turns=shifts.Select(shift=>new TrainingDailyTurn(shift,visible.Where(b=>TrainingSchedule.Classify(b,policy)==shift).ToArray())).ToArray();
        var work=all.Where(b=>b.Exercise!="rest").ToArray();
        int Sum(TrainingTurn? shift)=>work.Where(b=>TrainingSchedule.Classify(b,policy)==shift).Sum(b=>b.Minutes);
        var individual=work.SelectMany(b=>b.Participants.Distinct().Select(p=>(Player:p,b.Minutes))).GroupBy(p=>p.Player).ToDictionary(g=>g.Key,g=>g.Sum(p=>p.Minutes));
        return new(date,configuration,all.Length,visible.Length,turns,new(Sum(TrainingTurn.Morning),Sum(TrainingTurn.Afternoon),Sum(null),work.Sum(b=>b.Minutes),TrainingSchedule.EffectiveMinutes(all),individual),(plan.LegacyRests??Array.Empty<LegacyTrainingRest>()).Where(r=>r.Original.Date==date).ToArray());
    }
    /// <summary>Propone un hueco libre conservador para el equipo completo; los grupos pueden elegir manualmente horarios simultáneos válidos.</summary>
    public static TrainingTimeProposal? Suggest(TrainingPlan plan,DateOnly date,TrainingTurn turn,int minutes,TrainingSchedulePolicy? policy=null)
    {
        policy??=new();policy.Validate();
        if(!Enum.IsDefined(turn)||minutes<=0)throw new ArgumentException("Turno o duración de propuesta inválido.");
        var start=(turn==TrainingTurn.Morning?policy.OperatingStartHour:policy.AfternoonHour)*60;var end=(turn==TrainingTurn.Morning?policy.AfternoonHour:policy.OperatingEndHour)*60;
        var blocks=plan.Sessions.Where(b=>b.Date==date&&b.Exercise!="rest").ToArray();
        var last=blocks.Where(b=>TrainingSchedule.Classify(b,policy)==turn).Select(b=>b.Start.ToTimeSpan().TotalMinutes+b.Minutes).DefaultIfEmpty(start).Max();
        var step=policy.TimeStepMinutes;var next=(int)Math.Ceiling(last/step)*step;
        var candidates=Enumerable.Range(0,(end-start)/step+1).Select(i=>start+i*step).Where(m=>m+minutes<=end).OrderBy(m=>m<next).ThenBy(m=>m);
        foreach(var candidate in candidates){
            var finish=candidate+minutes;
            if(blocks.Any(b=>candidate<b.Start.ToTimeSpan().TotalMinutes+b.Minutes&&b.Start.ToTimeSpan().TotalMinutes<finish))continue;
            return new(TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(candidate)),TimeSpan.FromMinutes(finish));
        }
        return null;
    }
}
