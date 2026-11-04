using System.Text.Json;
using Furvo.Core.Development;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Godot;
namespace Furvo.Presentation;
public partial class Main
{
    private readonly TrainingGroupDraftBook _trainingGroupDrafts=new();
    private TrainingGroupContext GroupContext=>new(_workspace!.Career.Id.Value,SportsTeam);
    private bool TrainingGroupDirty=>_workspace is not null&&_careers is not null&&_workspace.HasClub&&_trainingGroupDrafts.Get(GroupContext)?.Dirty==true;
    private TrainingGroup SaveTrainingGroupDraft(TrainingGroupContext context,TrainingGroupDraft draft)
    {
        if(_workspace?.Career.Id.Value!=context.Career)throw new InvalidOperationException(UiText.Get("TrainingGroupWrongCareer"));
        var service=Sports();var plan=service.Plan(context.Team);
        if(draft.EditingId is {} id){var stored=plan.Groups.SingleOrDefault(g=>g.Id==id)??throw new InvalidOperationException(UiText.Get("TrainingGroupMissing"));if(JsonSerializer.Serialize(stored)!=JsonSerializer.Serialize(draft.Original))throw new InvalidOperationException(UiText.Get("TrainingGroupChanged"));}
        if(string.IsNullOrWhiteSpace(draft.Name))throw new InvalidDataException(UiText.Get("TrainingGroupNameRequired"));
        if(string.IsNullOrWhiteSpace(draft.Objective))throw new InvalidDataException(UiText.Get("TrainingGroupObjectiveRequired"));
        if(draft.Members.Count==0)throw new InvalidDataException(UiText.Get("TrainingGroupMembersRequired"));
        var value=draft.Value(Guid.NewGuid());var saved=service.SaveGroup(context.Team,value,plan.Revision).Groups.Single(g=>g.Id==value.Id);_trainingGroupDrafts.Begin(context,saved);return saved;
    }
    /// <summary>Guardar/descartar/cancelar comparten el mismo borrador que el editor; nunca se guarda implícitamente.</summary>
    private void TrainingGroupGuard(Action action,TrainingGroupContext? requested=null)
    {
        var context=requested??GroupContext;var draft=_trainingGroupDrafts.Get(context);if(draft?.Dirty!=true){action();return;}
        var dialog=new ConfirmationDialog{Name="TrainingGroupExitDialog",Title=UiText.Get("TrainingGroups"),DialogText=UiText.Get("TrainingGroupLeaveQuestion"),Exclusive=true,DialogHideOnOk=false,OkButtonText=UiText.Get("TrainingGroupSaveChanges"),CancelButtonText=UiText.Get("TrainingGroupKeepEditing")};
        var error=dialog.GetLabel();error.Name="TrainingGroupExitError";error.AutowrapMode=TextServer.AutowrapMode.WordSmart;error.CustomMinimumSize=new Vector2(480,0);dialog.AddButton(UiText.Get("TrainingGroupDiscard"),false,"discard");
        void Finish(TrainingGroupExit choice){try{if(_trainingGroupDrafts.TryLeave(context,choice,d=>{SaveTrainingGroupDraft(context,d);return true;})){dialog.QueueFree();action();}}catch(Exception problem){dialog.DialogText=UiText.Get("TrainingGroupLeaveQuestion")+"\n\n"+problem.Message;}}
        dialog.Confirmed+=()=>Finish(TrainingGroupExit.Save);dialog.CustomAction+=choice=>{if(choice=="discard")Finish(TrainingGroupExit.Discard);};dialog.Canceled+=()=>{_trainingGroupDrafts.TryLeave(context,TrainingGroupExit.Cancel,_=>false);dialog.QueueFree();};AddChild(dialog);var viewport=GetViewport();void Center(){if(GodotObject.IsInstanceValid(dialog)&&dialog.Visible)dialog.PopupCentered(new Vector2I(570,245));}viewport.SizeChanged+=Center;dialog.TreeExiting+=()=>viewport.SizeChanged-=Center;dialog.PopupCentered(new Vector2I(570,245));
    }
    private static string TrainingGroupProblem(Exception error)=>error is TrainingGroupInUseException used?
        UiText.Get("TrainingGroupInUse")+"\n"+string.Join("\n",used.References.Select(r=>r.Date.ToString("dd/MM/yyyy")+" · "+UiText.Get("TrainingGroupReference_"+r.Kind)+" · "+r.Description)):error.Message;
    private void RenderTrainingGroups(VBoxContainer content,TrainingPlan plan,IReadOnlyList<TrainingCandidate> players,IReadOnlyList<TrainingStaff> staff,Action refresh,Action<Guid?>? selectedGroup=null)
    {
        var context=new TrainingGroupContext(_workspace!.Career.Id.Value,plan.Team);if(_trainingGroupDrafts.Get(context) is null)_trainingGroupDrafts.Begin(context);var draft=_trainingGroupDrafts.Get(context)!;
        var card=SportsCard(content,"TrainingGroups");card.AddChild(ClubUi.Text("TrainingGroupsTitle",UiText.Get("TrainingGroups"),18,"f1b96b"));
        var state=ClubUi.Text("TrainingGroupDraftState","",14,"f1b96b");card.AddChild(state);
        var choose=new OptionButton{Name="TrainingGroupEdit"};choose.AddItem(UiText.Get("TrainingNewGroup"));foreach(var g in plan.Groups){choose.AddItem(g.Name);choose.SetItemMetadata(choose.ItemCount-1,g.Id.ToString());}choose.Selected=draft.EditingId is {} editing?Math.Max(0,plan.Groups.ToList().FindIndex(g=>g.Id==editing)+1):0;card.AddChild(choose);
        var name=new LineEdit{Name="TrainingGroupName",PlaceholderText=UiText.Get("Name"),Text=draft.Name};card.AddChild(name);var objective=new LineEdit{Name="TrainingGroupObjective",PlaceholderText=UiText.Get("TrainingObjective"),Text=draft.Objective};card.AddChild(objective);
        var list=new ItemList{Name="TrainingGroupMembers",SelectMode=ItemList.SelectModeEnum.Multi,CustomMinimumSize=new Vector2(0,170)};TrainingUi.AddPlayers(list,players);for(var i=0;i<players.Count;i++)if(draft.Members.Contains(players[i].Id))list.Select(i,false);card.AddChild(list);
        var eligible=staff.Where(p=>p.Responsibilities.Contains(TrainingResponsibility.Group)).ToArray();var owner=new OptionButton{Name="TrainingGroupResponsible",FitToLongestItem=false,ClipText=true};owner.AddItem(UiText.Get("TrainingUnassignedResponsible"));foreach(var person in eligible)owner.AddItem(person.Name+" — "+UiText.Get("AnalysisStaff_"+person.Role));owner.Selected=draft.Responsible is {} responsible?Math.Max(0,Array.FindIndex(eligible,p=>p.Id==responsible)+1):0;card.AddChild(owner);TrainingUi.LimitPopup(owner);
        var status=ClubUi.Text("TrainingGroupStatus","",14,"f1b96b");card.AddChild(status);
        void Caption(){var value=_trainingGroupDrafts.Get(context)!;state.Text=UiText.Get(value.EditingId is null?value.Dirty?"TrainingGroupNewDirty":"TrainingNewGroup":value.Dirty?"TrainingGroupExistingDirty":"TrainingGroupSavedState");}
        void Capture(){_trainingGroupDrafts.Put(context,draft with{Name=name.Text,Objective=objective.Text,Members=list.GetSelectedItems().Select(i=>players[i].Id).ToArray(),Responsible=owner.Selected>0?eligible[owner.Selected-1].Id:null});Caption();}
        name.TextChanged+=_=>Capture();objective.TextChanged+=_=>Capture();owner.ItemSelected+=_=>Capture();list.MultiSelected+=(_,_)=>Capture();Caption();
        choose.ItemSelected+=index=>{Capture();var next=index==0?null:plan.Groups[(int)index-1];choose.Selected=draft.EditingId is {} current?Math.Max(0,plan.Groups.ToList().FindIndex(g=>g.Id==current)+1):0;TrainingGroupGuard(()=>{_trainingGroupDrafts.Begin(context,next);refresh();},context);};
        var actions=new HFlowContainer();card.AddChild(actions);
        actions.AddChild(WindowUi.Button("TrainingSaveGroup",()=>{try{Capture();var saved=SaveTrainingGroupDraft(context,_trainingGroupDrafts.Get(context)!);selectedGroup?.Invoke(saved.Id);refresh();if(FindChild("TrainingGroupStatus",true,false) is Label message)message.Text=UiText.Get("TrainingGroupSavedState");}catch(Exception error){status.Text=TrainingGroupProblem(error);}}));
        actions.AddChild(WindowUi.Button("TrainingCancelGroup",()=>{Capture();var id=draft.EditingId;TrainingGroupGuard(()=>{var stored=id is {} value?Sports().Plan(plan.Team).Groups.SingleOrDefault(g=>g.Id==value):null;_trainingGroupDrafts.Begin(context,stored);refresh();},context);}));
        var delete=WindowUi.Button("TrainingDeleteGroup",()=>{Capture();var id=draft.EditingId!.Value;TrainingGroupGuard(()=>{try{
            var service=Sports();var references=service.GroupReferences(plan.Team,id);if(references.Count>0)throw new TrainingGroupInUseException(references);
            TrainingConfirm(UiText.Format("TrainingGroupConfirmDelete",plan.Groups.Single(g=>g.Id==id).Name),()=>{try{service.DeleteGroup(plan.Team,id,service.Plan(plan.Team).Revision);_trainingGroupDrafts.Begin(context);selectedGroup?.Invoke(null);refresh();}catch(Exception error){if(FindChild("TrainingGroupStatus",true,false) is Label label)label.Text=TrainingGroupProblem(error);}});
        }catch(Exception error){_trainingGroupDrafts.Begin(context,Sports().Plan(plan.Team).Groups.SingleOrDefault(g=>g.Id==id));refresh();if(FindChild("TrainingGroupStatus",true,false) is Label label)label.Text=TrainingGroupProblem(error);}},context);});delete.Disabled=draft.EditingId is null;actions.AddChild(delete);
    }
}
