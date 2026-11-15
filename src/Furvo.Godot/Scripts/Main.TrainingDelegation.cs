using System.Text.Json;
using Furvo.Core.Development;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Godot;
namespace Furvo.Presentation;
public partial class Main
{
    private sealed class TrainingDelegationDraft(IReadOnlyList<TrainingDelegation> saved)
    {
        internal TrainingDelegation[] Original=saved.ToArray();
        internal Dictionary<TrainingResponsibility,TrainingDelegation> Assignments=saved.ToDictionary(d=>d.Area);
        internal bool Expanded,Dirty;
    }
    private readonly Dictionary<string,TrainingDelegationDraft> _trainingDelegationDrafts=new();
    private bool TrainingDelegationDirty=>_trainingDelegationDrafts.TryGetValue(SportsPath+"|"+SportsTeam,out var state)&&state.Dirty;
    private void RenderTrainingDelegation(VBoxContainer content,TrainingPlan plan,IReadOnlyList<TrainingStaff> staff,SqliteSportsStore service)
    {
        var key=SportsPath+"|"+plan.Team;
        if(!_trainingDelegationDrafts.TryGetValue(key,out var state))_trainingDelegationDrafts[key]=state=new(plan.Delegations);
        if(!state.Dirty){state.Original=plan.Delegations.ToArray();state.Assignments=plan.Delegations.ToDictionary(d=>d.Area);}
        var card=SportsCard(content,"TrainingDelegationPanel");card.Name="TrainingDelegationPanel";
        var toggle=WindowUi.Button("TrainingDelegationToggle",()=>{});toggle.Alignment=HorizontalAlignment.Left;card.AddChild(toggle);
        var details=new VBoxContainer{Name="TrainingDelegationDetails",Visible=state.Expanded};card.AddChild(details);
        var scroll=new ScrollContainer{Name="TrainingDelegationScroll",HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled,CustomMinimumSize=new Vector2(0,Math.Clamp(GetViewportRect().Size.Y*.13f,100,130))};details.AddChild(scroll);scroll.Resized+=()=>scroll.CustomMinimumSize=new Vector2(0,Math.Clamp(GetViewportRect().Size.Y*.13f,100,130));
        var table=new GridContainer{Name="TrainingDelegationTable",Columns=3,SizeFlagsHorizontal=SizeFlags.ExpandFill};table.AddThemeConstantOverride("h_separation",12);table.AddThemeConstantOverride("v_separation",6);scroll.AddChild(table);
        table.AddChild(ClubUi.Text("TrainingDelegationAreaHeader",UiText.Get("TrainingDelegationArea"),13,"f1b96b"));table.AddChild(ClubUi.Text("TrainingDelegationModeHeader",UiText.Get("TrainingDelegationModeLabel"),13,"f1b96b"));table.AddChild(ClubUi.Text("TrainingDelegationOwnerHeader",UiText.Get("TrainingResponsibleLabel"),13,"f1b96b"));
        ((Control)table.GetChild(0)).SizeFlagsHorizontal=SizeFlags.Fill;((Control)table.GetChild(1)).SizeFlagsHorizontal=SizeFlags.Fill;
        var status=ClubUi.Text("TrainingDelegationStatus","",13,"f1b96b");Button? save=null;
        var manualOwner=_workspace!.Career.Coach.ProfileSnapshot.FullName+" — "+UiText.Get("AnalysisStaff_HeadCoach");
        TrainingStaff[] Eligible(TrainingResponsibility area)=>staff.Where(s=>s.Id!=_workspace!.Career.Coach.Id.Value&&s.Responsibilities.Contains(area)).ToArray();
        bool Valid()=>state.Assignments.Values.All(d=>d.Responsible is {} id&&Eligible(d.Area).Any(p=>p.Id==id));
        void Header(){toggle.Text=(state.Expanded?"▾ ":"▸ ")+UiText.Get("TrainingDelegationTitle")+" — "+UiText.Get("TrainingDelegationState_"+plan.Management)+(state.Dirty?" · "+UiText.Get("TrainingDelegationPending"):"");details.Visible=state.Expanded;}
        void Changed(){state.Dirty=true;Header();status.Text=Valid()?UiText.Get("TrainingDelegationPending"):UiText.Get("TrainingDelegationOwnerRequired");if(save is not null)save.Disabled=!Valid();}
        foreach(var area in Enum.GetValues<TrainingResponsibility>()){
            var label=ClubUi.Text("TrainingDelegationArea_"+area,UiText.Get("TrainingResponsibility_"+area),13);label.SizeFlagsHorizontal=SizeFlags.Fill;label.CustomMinimumSize=new Vector2(210,32);table.AddChild(label);
            var current=state.Assignments.GetValueOrDefault(area);var eligible=Eligible(area);
            var mode=new OptionButton{Name="TrainingDelegationMode_"+area,FitToLongestItem=false,CustomMinimumSize=new Vector2(145,32)};
            foreach(var name in new[]{"Manual","Delegated","Mixed","PersonalSupport"})mode.AddItem(UiText.Get("TrainingDelegationMode_"+name));mode.Selected=current is null?0:(int)current.Mode;table.AddChild(mode);
            var owner=new OptionButton{Name="TrainingDelegationOwner_"+area,FitToLongestItem=false,ClipText=true,CustomMinimumSize=new Vector2(240,32),SizeFlagsHorizontal=SizeFlags.ExpandFill,Disabled=current is null};owner.AddItem(eligible.Length==0?UiText.Get("TrainingDelegationNoStaff"):current is null?manualOwner:UiText.Get("TrainingUnassignedResponsible"));owner.SetItemMetadata(0,"");
            foreach(var person in eligible){owner.AddItem(person.Name+" — "+UiText.Get("AnalysisStaff_"+person.Role));owner.SetItemMetadata(owner.ItemCount-1,person.Id.ToString());}
            owner.Selected=current?.Responsible is {} assigned?Math.Max(0,Array.FindIndex(eligible,p=>p.Id==assigned)+1):0;
            if(current?.Responsible is {} missing&&owner.Selected==0){owner.AddItem((staff.SingleOrDefault(p=>p.Id==missing)?.Name??UiText.Get("Unknown"))+" — "+UiText.Get("TrainingStaffIneligible"));owner.SetItemMetadata(owner.ItemCount-1,missing.ToString());owner.SetItemDisabled(owner.ItemCount-1,true);owner.Selected=owner.ItemCount-1;}
            owner.TooltipText=eligible.Length==0?UiText.Get("TrainingDelegationNoStaff"):"";TrainingUi.LimitPopup(owner);table.AddChild(owner);
            if(eligible.Length==0){mode.SetItemDisabled(1,true);mode.SetItemDisabled(2,true);mode.SetItemDisabled(3,true);}
            Guid? SelectedOwner()=>owner.Selected>0&&Guid.TryParse(owner.GetItemMetadata(owner.Selected).AsString(),out var id)?id:null;
            mode.ItemSelected+=index=>{owner.Disabled=index==0;owner.SetItemText(0,eligible.Length==0?UiText.Get("TrainingDelegationNoStaff"):index==0?manualOwner:UiText.Get("TrainingUnassignedResponsible"));if(index==0)owner.Selected=0;if(index==0)state.Assignments.Remove(area);else state.Assignments[area]=new(area,SelectedOwner(),(TrainingDelegationMode)index);Changed();};
            owner.ItemSelected+=_=>{if(mode.Selected>0)state.Assignments[area]=new(area,SelectedOwner(),(TrainingDelegationMode)mode.Selected);Changed();};
        }
        details.AddChild(ClubUi.Text("TrainingDelegationNotice",UiText.Get("TrainingDelegationNotice"),12));details.AddChild(status);
        var actions=new HFlowContainer();details.AddChild(actions);
        save=WindowUi.Button("TrainingSaveDelegation",()=>{try{
            if(!Valid())throw new InvalidDataException(UiText.Get("TrainingDelegationOwnerRequired"));
            // Se relee la agenda para conservar los ejercicios guardados mientras el panel tenía un borrador.
            var latest=service.Plan(plan.Team);
            if(JsonSerializer.Serialize(latest.Delegations.OrderBy(d=>d.Area))!=JsonSerializer.Serialize(state.Original.OrderBy(d=>d.Area)))throw new InvalidOperationException(UiText.Get("TrainingDelegationConflict"));
            var saved=service.SavePlan(latest with{Delegations=state.Assignments.Values.OrderBy(d=>d.Area).ToArray()});state.Original=saved.Delegations.ToArray();state.Dirty=false;ShowTraining();if(FindChild("TrainingDelegationStatus",true,false) is Label result)result.Text=UiText.Get("TrainingDelegationSaved");
        }catch(Exception error){SportsError(error,status);}});save.Disabled=!state.Dirty||!Valid();actions.AddChild(save);
        actions.AddChild(WindowUi.Button("TrainingDiscardDelegation",()=>TrainingConfirm(UiText.Get("TrainingDiscardChanges"),()=>{var fresh=service.Plan(plan.Team);_trainingDelegationDrafts[key]=new(fresh.Delegations){Expanded=true};ShowTraining();})));
        actions.AddChild(WindowUi.Button("TrainingAllManual",()=>TrainingConfirm(UiText.Get("TrainingConfirmManual"),()=>{state.Assignments.Clear();state.Dirty=true;ShowTraining();if(FindChild("TrainingSaveDelegation",true,false) is Button button)button.Disabled=false;if(FindChild("TrainingDelegationStatus",true,false) is Label result)result.Text=UiText.Get("TrainingDelegationPending");})));
        toggle.Pressed+=()=>{void Toggle(){if(state.Expanded&&state.Dirty){var fresh=service.Plan(plan.Team);state.Original=fresh.Delegations.ToArray();state.Assignments=fresh.Delegations.ToDictionary(d=>d.Area);state.Dirty=false;state.Expanded=false;ShowTraining();}else{state.Expanded=!state.Expanded;Header();}}if(state.Expanded&&state.Dirty)TrainingConfirm(UiText.Get("TrainingDiscardChanges"),Toggle);else Toggle();};Header();
    }
}
