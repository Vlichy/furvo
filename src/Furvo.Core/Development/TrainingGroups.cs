using Furvo.Core.Domain;
namespace Furvo.Core.Development;
public readonly record struct TrainingGroupContext(Guid Career,Guid Team);
public enum TrainingGroupExit { Save,Discard,Cancel }
/// <summary>Borrador portable: su edición nunca escribe el plan ni contiene atributos del jugador.</summary>
public sealed record TrainingGroupDraft(Guid? EditingId,string Name,string Objective,IReadOnlyList<PlayerId> Members,Guid? Responsible,TrainingGroup? Original=null)
{
    public bool Dirty=>Original is null?Name.Length>0||Objective.Length>0||Members.Count>0||Responsible is not null:
        Name!=Original.Name||Objective!=Original.Objective||Responsible!=Original.Responsible||!Members.ToHashSet().SetEquals(Original.Members);
    public TrainingGroup Value(Guid newId)=>new(EditingId??newId,Name,Members.ToArray(),Objective,Responsible);
    public static TrainingGroupDraft From(TrainingGroup? group)=>group is null?new(null,"","",Array.Empty<PlayerId>(),null):new(group.Id,group.Name,group.Objective,group.Members.ToArray(),group.Responsible,group with{Members=group.Members.ToArray()});
}
public sealed class TrainingGroupDraftBook
{
    private readonly Dictionary<TrainingGroupContext,TrainingGroupDraft> _values=new();
    public TrainingGroupDraft? Get(TrainingGroupContext context)=>_values.GetValueOrDefault(context);
    public void Put(TrainingGroupContext context,TrainingGroupDraft value){if(context.Career==Guid.Empty||context.Team==Guid.Empty)throw new ArgumentException("Contexto de grupo inválido.");_values[context]=value with{Members=value.Members.ToArray()};}
    public void Begin(TrainingGroupContext context,TrainingGroup? group=null)=>Put(context,TrainingGroupDraft.From(group));
    public void Close(TrainingGroupContext context)=>_values.Remove(context);
    public void Clear()=>_values.Clear();
    public bool TryLeave(TrainingGroupContext context,TrainingGroupExit choice,Func<TrainingGroupDraft,bool> save)
    {if(choice==TrainingGroupExit.Cancel)return false;if(choice==TrainingGroupExit.Save&&Get(context) is {} draft&&!save(draft))return false;Close(context);return true;}
}
public sealed record TrainingGroupReference(string Kind,Guid Id,DateOnly Date,string Description);
public sealed class TrainingGroupInUseException(IReadOnlyList<TrainingGroupReference> references):InvalidOperationException("El grupo está en uso y no puede eliminarse.")
{public IReadOnlyList<TrainingGroupReference> References {get;}=references;}
public static class TrainingGroups
{
    public static IReadOnlyList<TrainingGroupReference> References(TrainingPlan plan,Guid group,IReadOnlyList<AnalysisReport>? reports=null)=>
        plan.Sessions.Concat((plan.LegacyRests??Array.Empty<LegacyTrainingRest>()).Select(r=>r.Original)).Where(s=>s.Target.Group==group)
        .Select(s=>new TrainingGroupReference("Activity",s.Id,s.Date,s.Objective??s.Exercise))
        .Concat((reports??Array.Empty<AnalysisReport>()).Where(r=>r.Context?.Selection.Group==group).Select(r=>new TrainingGroupReference("Report",r.Id,r.Date,r.Context!.GroupName??""))).DistinctBy(r=>(r.Kind,r.Id)).OrderBy(r=>r.Date).ThenBy(r=>r.Id).ToArray();
    /// <summary>La composición actual solo se aplica a ejercicios no realizados desde la fecha de carrera.</summary>
    public static TrainingPlan Update(TrainingPlan plan,TrainingGroup group,IReadOnlyList<TrainingCandidate> squad,DateOnly today,IReadOnlySet<Guid> completed)
    {
        var groups=plan.Groups.Where(g=>g.Id!=group.Id).Append(group).ToArray();
        return plan with{Groups=groups,Sessions=plan.Sessions.Select(s=>s.Target.Group==group.Id&&s.Date>=today&&!completed.Contains(s.Id)?s with{Participants=TrainingRules.Resolve(s.Target,squad,groups)}:s).ToArray()};
    }
    public static TrainingPlan Delete(TrainingPlan plan,Guid group,IReadOnlyList<AnalysisReport>? reports=null)
    {if(!plan.Groups.Any(g=>g.Id==group))throw new InvalidOperationException("El grupo ya no existe.");var references=References(plan,group,reports);if(references.Count>0)throw new TrainingGroupInUseException(references);return plan with{Groups=plan.Groups.Where(g=>g.Id!=group).ToArray()};}
}
