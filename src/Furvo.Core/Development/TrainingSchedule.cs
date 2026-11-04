namespace Furvo.Core.Development;
public enum TrainingTurn { Morning=1,Afternoon=2 }
public enum TrainingDayKind { Rest=1,Single=2,Double=3 }
public sealed record TrainingDayPlan(DateOnly Date,TrainingDayKind Kind,TrainingTurn? SingleTurn=null);
public sealed record TrainingSchedulePolicy(int MinimumTurnBreakMinutes=60,int AfternoonHour=14,int OperatingStartHour=7,int OperatingEndHour=20,int TimeStepMinutes=15)
{
    public void Validate(){if(MinimumTurnBreakMinutes<0||AfternoonHour is <1 or >23||OperatingStartHour<0||OperatingEndHour>24||OperatingStartHour>=AfternoonHour||OperatingEndHour<=AfternoonHour||TimeStepMinutes<1||60%TimeStepMinutes!=0)throw new ArgumentException("Parámetros horarios inválidos.");}
}
public sealed record TrainingTurnSummary(TrainingTurn? Turn,int EffectiveMinutes,int ElapsedMinutes,IReadOnlyList<TrainingSession> Blocks);
public static class TrainingSchedule
{
    public static TrainingTurn? Classify(TrainingSession block,TrainingSchedulePolicy? policy=null)
    {
        policy??=new();var boundary=new TimeSpan(policy.AfternoonHour,0,0);
        return block.Turn??(block.Start.ToTimeSpan()+TimeSpan.FromMinutes(block.Minutes)<=boundary?TrainingTurn.Morning:block.Start.ToTimeSpan()>=boundary?TrainingTurn.Afternoon:null);
    }
    public static int Duration(TimeOnly start,TimeSpan end)
    {
        var minutes=(end-start.ToTimeSpan()).TotalMinutes;
        if(end>TimeSpan.FromDays(1)||minutes<=0||minutes!=Math.Truncate(minutes))throw new InvalidDataException("La finalización debe ser posterior al inicio y la duración debe expresarse en minutos completos.");return checked((int)minutes);
    }
    public static TrainingDayPlan? Day(TrainingPlan plan,DateOnly date,TrainingSchedulePolicy? policy=null)
    {
        var saved=plan.Days?.SingleOrDefault(d=>d.Date==date);if(saved is not null)return saved;
        var blocks=plan.Sessions.Where(s=>s.Date==date).ToArray();if(blocks.Length==0)return null;
        if(blocks.Any(b=>b.Exercise=="rest"))return null;
        var shifts=blocks.Select(b=>Classify(b,policy)).Distinct().ToArray();if(shifts.Any(s=>s is null))return null;
        return shifts.Length==1&&shifts[0] is {} shift?new(date,TrainingDayKind.Single,shift):new(date,TrainingDayKind.Double);
    }
    /// <summary>Unión de intervalos: los huecos no cuentan; los grupos simultáneos no duplican el tiempo del turno.</summary>
    public static int EffectiveMinutes(IEnumerable<TrainingSession> blocks)
    {
        var intervals=blocks.Where(b=>b.Exercise!="rest").OrderBy(b=>b.Start).Select(b=>(Start:b.Start.ToTimeSpan().TotalMinutes,End:b.Start.ToTimeSpan().TotalMinutes+b.Minutes)).ToArray();
        if(intervals.Length==0)return 0;double total=0,start=intervals[0].Start,end=intervals[0].End;
        foreach(var next in intervals.Skip(1)){if(next.Start<=end)end=Math.Max(end,next.End);else{total+=end-start;start=next.Start;end=next.End;}}return checked((int)(total+end-start));
    }
    public static IReadOnlyList<TrainingTurnSummary> Turns(TrainingPlan plan,DateOnly date,TrainingSchedulePolicy? policy=null)=>plan.Sessions.Where(s=>s.Date==date).GroupBy(s=>Classify(s,policy)).OrderBy(g=>g.Key).Select(g=>{
        var blocks=g.OrderBy(b=>b.Start).ThenBy(b=>b.Id).ToArray();return new TrainingTurnSummary(g.Key,EffectiveMinutes(blocks),(int)(blocks.Max(b=>b.Ends)-blocks.Min(b=>b.Begins)).TotalMinutes,blocks);
    }).ToArray();
    public static IReadOnlyList<Guid> Incompatible(TrainingPlan plan,TrainingDayPlan day,TrainingSchedulePolicy? policy=null)=>plan.Sessions.Where(b=>b.Date==day.Date&&
        (day.Kind==TrainingDayKind.Rest?true:day.Kind==TrainingDayKind.Single&&Classify(b,policy)!=day.SingleTurn)).Select(b=>b.Id).ToArray();
    public static TrainingPlan SetDay(TrainingPlan plan,TrainingDayPlan day,bool confirmRemoval=false,TrainingSchedulePolicy? policy=null)
    {
        ValidateDay(day);var incompatible=Incompatible(plan,day,policy);if(incompatible.Count>0&&!confirmRemoval)throw new InvalidOperationException("El cambio de jornada requiere confirmar los bloques incompatibles.");
        return plan with{Days=(plan.Days??Array.Empty<TrainingDayPlan>()).Where(d=>d.Date!=day.Date).Append(day).ToArray(),Sessions=plan.Sessions.Where(s=>!incompatible.Contains(s.Id)).ToArray()};
    }
    private static void ValidateDay(TrainingDayPlan day)
    {if(day.Date==default||!Enum.IsDefined(day.Kind)||day.Kind==TrainingDayKind.Single&&(day.SingleTurn is null||!Enum.IsDefined(day.SingleTurn.Value))||day.Kind!=TrainingDayKind.Single&&day.SingleTurn is not null)throw new InvalidDataException("Tipo de jornada o turno inválido.");}
    public static void ValidateChanges(TrainingPlan plan,TrainingPlan previous,TrainingSchedulePolicy? policy=null)
    {
        policy??=new();policy.Validate();var days=plan.Days??Array.Empty<TrainingDayPlan>();if(days.Select(d=>d.Date).Distinct().Count()!=days.Count)throw new InvalidDataException("Jornadas duplicadas.");
        foreach(var day in days){ValidateDay(day);if(Incompatible(plan,day,policy).Count>0)throw new InvalidDataException("Existen bloques incompatibles con el tipo de jornada.");}
        foreach(var block in plan.Sessions){
            var old=previous.Sessions.SingleOrDefault(s=>s.Id==block.Id);var sameTiming=old is not null&&old.Date==block.Date&&old.Start==block.Start&&old.Minutes==block.Minutes&&(old.Turn==block.Turn||old.Turn is null&&Classify(old,policy)==block.Turn);
            if(!sameTiming&&(block.Start.ToTimeSpan()<TimeSpan.FromHours(policy.OperatingStartHour)||block.Start.ToTimeSpan()+TimeSpan.FromMinutes(block.Minutes)>TimeSpan.FromHours(policy.OperatingEndHour)))throw new InvalidDataException($"El horario operativo para bloques nuevos es {policy.OperatingStartHour:00}:00–{policy.OperatingEndHour:00}:00.");
            if(block.Exercise=="rest")throw new InvalidDataException("El descanso se confirma como jornada completa, no como ejercicio.");
            if(block.Minutes<=0||block.Start.ToTimeSpan()+TimeSpan.FromMinutes(block.Minutes)>TimeSpan.FromDays(1))throw new InvalidDataException("La duración debe ser positiva y terminar dentro de la misma jornada.");
            if(old is null&&Classify(block,policy) is null)throw new InvalidDataException("El bloque nuevo debe pertenecer a mañana o tarde.");
            if(block.Turn is {} turn&&(!Enum.IsDefined(turn)||turn==TrainingTurn.Morning&&block.Start.ToTimeSpan().TotalMinutes+block.Minutes>policy.AfternoonHour*60||turn==TrainingTurn.Afternoon&&block.Start.Hour<policy.AfternoonHour))throw new InvalidDataException("El horario no corresponde al turno seleccionado.");
        }

    }
    public static IReadOnlyList<string> Warnings(TrainingPlan plan,DateOnly date,TrainingSchedulePolicy? policy=null)
    {
        policy??=new();var blocks=plan.Sessions.Where(b=>b.Date==date&&b.Exercise!="rest").ToArray();var result=new List<string>();
        if(blocks.Any(b=>Classify(b,policy) is null))result.Add("LegacyTurnPending");
        var morning=blocks.Where(b=>Classify(b,policy)==TrainingTurn.Morning).ToArray();var afternoon=blocks.Where(b=>Classify(b,policy)==TrainingTurn.Afternoon).ToArray();
        if(morning.Length>0&&afternoon.Length>0&&(afternoon.Min(b=>b.Begins)-morning.Max(b=>b.Ends)).TotalMinutes<policy.MinimumTurnBreakMinutes)result.Add("ShortTurnBreak");return result;
    }
    public static IReadOnlyList<TrainingStaff> Eligible(IReadOnlyList<TrainingStaff> staff,TrainingSession block)=>staff.Where(s=>s.Team==block.Team&&s.Responsibilities.Contains(TrainingRules.Responsibility(block))).ToArray();
}
