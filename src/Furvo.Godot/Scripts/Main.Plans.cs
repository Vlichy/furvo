using Furvo.Core.Domain;
using Furvo.Core.Lineups;
using Furvo.Core.Tactics;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Godot;
namespace Furvo.Presentation;
public partial class Main
{
    private TacticalPlanKind _planKind=TacticalPlanKind.Initial;
    private PossessionPhase _layoutPhase=PossessionPhase.WithoutBall;
    private bool _showPhaseReference;
    private PossessionPhase EditingPhase=>_tacticTab==1?(_stylePhase=="possession"?PossessionPhase.WithBall:PossessionPhase.WithoutBall):_layoutPhase;
    private ITacticalPlanStore Plans()=>_academyTeam is {} team?new SqliteAcademyPlanStore(_careers!.PathFor(_workspace!.Career.Id),team):new SqliteTacticalPlanStore(_careers!.PathFor(_workspace!.Career.Id));
    private string EditorScope(CareerLineup state)=>_workspace!.Career.Id+":"+_academyTeam+":"+_planKind+":"+EditingPhase+":"+state.Revision;
    private void CompactTactic(Control control)
    {
        control.CustomMinimumSize=new Vector2(control.CustomMinimumSize.X,32);control.AddThemeFontSizeOverride("font_size",14);
        foreach(var state in new[]{"normal","hover","pressed","disabled"})
        {var box=(StyleBox)Theme.GetStylebox(state,"Button").Duplicate();box.ContentMarginTop=4;box.ContentMarginBottom=4;box.ContentMarginLeft=10;box.ContentMarginRight=10;control.AddThemeStyleboxOverride(state,box);}
    }
    private void ConfirmPlan(string message,Action action)
    {
        var dialog=new ConfirmationDialog{Name="PlanOverwriteDialog",Title=UiText.Get("Brand"),DialogText=message,Exclusive=true};
        dialog.OkButtonText=UiText.Get("PlanConfirm");dialog.CancelButtonText=UiText.Get("PlanCancel");
        dialog.Confirmed+=()=>{dialog.QueueFree();action();};dialog.Canceled+=()=>dialog.QueueFree();AddChild(dialog);dialog.PopupCentered(new Vector2I(520,180));
    }
    private void AddPlanSelectors(Container row)
    {
        var planGroup=new HBoxContainer{Name="TacticalPlanGroup"};planGroup.AddThemeConstantOverride("separation",8);row.AddChild(planGroup);
        var phaseGroup=new HBoxContainer{Name="TacticalPhaseGroup"};phaseGroup.AddThemeConstantOverride("separation",8);row.AddChild(phaseGroup);
        Label Caption(Container group,string name,string key){var label=ClubUi.Text(name,UiText.Get(key),14,"f1b96b");label.SizeFlagsHorizontal=SizeFlags.Fill;label.AutowrapMode=TextServer.AutowrapMode.Off;group.AddChild(label);return label;}
        Caption(planGroup,"PlanLabel","PlanLabel");var plans=new OptionButton{Name="TacticalPlanSelector",CustomMinimumSize=new Vector2(125,32)};CompactTactic(plans);planGroup.AddChild(plans);
        foreach(var kind in Enum.GetValues<TacticalPlanKind>())plans.AddItem(UiText.Get("Plan_"+kind));plans.Selected=(int)_planKind-1;
        plans.ItemSelected+=i=>{_planKind=(TacticalPlanKind)(i+1);_lineupSlot=null;_lineupPinned=false;ShowTactics();};
        Caption(phaseGroup,"PhaseLabel","PlanPhase");var phases=new OptionButton{Name="TacticalPhaseSelector",CustomMinimumSize=new Vector2(135,32)};CompactTactic(phases);phaseGroup.AddChild(phases);
        foreach(var phase in Enum.GetValues<PossessionPhase>())phases.AddItem(UiText.Get("Phase_"+phase));phases.Selected=(int)EditingPhase-1;
        phases.ItemSelected+=i=>{_layoutPhase=(PossessionPhase)(i+1);_stylePhase=_layoutPhase==PossessionPhase.WithBall?"possession":"defending";_lineupSlot=null;_lineupPinned=false;ShowTactics();};
    }
    private void AddPlanActions(Container heading,TacticalPlanBook? book)
    {
        var save=Button("SaveTacticalPlans",()=>{try{if(Plans().Load() is {} current){Plans().Save(current);ShowTactics();}}catch(Exception e){GD.PushWarning(e.ToString());ShowError("PlanSaveError",ShowTactics,()=>NavigateCareer(Furvo.Core.Careers.CareerPage.Desktop));}});
        CompactTactic(save);save.Disabled=book is null||AcademyTacticsReadOnly;heading.AddChild(save);
        var copy=new OptionButton{Name="CopyTacticalPlan",CustomMinimumSize=new Vector2(170,32)};CompactTactic(copy);heading.AddChild(copy);copy.AddItem(UiText.Get("CopyTacticalPlan"));
        var choices=Enum.GetValues<TacticalPlanKind>().Where(k=>k!=_planKind).ToArray();foreach(var kind in choices)copy.AddItem(UiText.Get("Plan_"+kind));copy.Disabled=book is null||AcademyTacticsReadOnly;
        copy.ItemSelected+=i=>{if(i==0)return;var source=choices[(int)i-1];var target=_planKind;var expected=Plans().Load()!.Revision;copy.Selected=0;
            ConfirmPlan(UiText.Format("PlanCopyWarning",UiText.Get("Plan_"+target)),()=>{try{var current=Plans().Load()!;if(current.Revision!=expected)throw new InvalidOperationException("Los planes cambiaron.");var old=current.Get(target);Plans().Save(current.Replace(TacticalPlanRules.DeepCopy(current.Get(source),old.Id,target)));ShowTactics();}catch(Exception e){GD.PushWarning(e.ToString());ShowError("PlanSaveError",ShowTactics,()=>NavigateCareer(Furvo.Core.Careers.CareerPage.Desktop));}});};
    }
    private void WireFormationEditor(HBoxContainer toolbar,OptionButton picker,CareerLineup state,Label status,Container actions)
    {
        picker.Disabled=AcademyTacticsReadOnly;
        var book=Plans().Load()!;var kind=_planKind;var phase=EditingPhase;var d=book.Get(kind).Disposition(phase);
        if(d.IsCustom){var customized=ClubUi.Text("CustomFormation",UiText.Get("PersonalizedFormation"),12,"f1b96b");customized.AutowrapMode=TextServer.AutowrapMode.Off;customized.SizeFlagsHorizontal=SizeFlags.Fill;toolbar.AddChild(customized);}
        void Apply(Func<TacticalPlan,TacticalPlan> edit)
        {try{var current=Plans().Load()!;if(current.Revision!=state.Revision)throw new InvalidOperationException("Los planes cambiaron.");Plans().Save(current.Replace(edit(current.Get(kind))));_lineupSlot=null;_lineupPinned=false;ShowTactics();}catch(Exception e){GD.PushWarning(e.ToString());status.Text=UiText.Get("PlanSaveError");}}
        picker.ItemSelected+=i=>{var reference=FormationCatalog.All.SingleOrDefault(f=>f.Id==picker.GetItemText((int)i));if(reference is null)return;void Set()=>Apply(p=>TacticalPlanRules.SetReference(p,phase,reference));if(d.IsCustom){picker.Selected=FormationCatalog.All.ToList().FindIndex(f=>f.Id==state.Formation.Id);ConfirmPlan(UiText.Get("DispositionOverwriteWarning"),Set);}else Set();};
        var reset=Button("ResetDisposition",()=>ConfirmPlan(UiText.Get("DispositionOverwriteWarning"),()=>Apply(p=>TacticalPlanRules.Reset(p,phase))));CompactTactic(reset);reset.Disabled=AcademyTacticsReadOnly;actions.AddChild(reset);
        var copy=new OptionButton{Name="CopyDisposition",CustomMinimumSize=new Vector2(160,32)};CompactTactic(copy);actions.AddChild(copy);copy.AddItem(UiText.Get("CopyDisposition"));copy.Disabled=AcademyTacticsReadOnly;
        var options=Enum.GetValues<TacticalPlanKind>().SelectMany(k=>Enum.GetValues<PossessionPhase>().Select(p=>(Kind:k,Phase:p))).Where(v=>v.Kind!=kind||v.Phase!=phase).ToArray();
        foreach(var option in options)copy.AddItem(UiText.Get("Plan_"+option.Kind)+" · "+UiText.Get("Phase_"+option.Phase));
        copy.ItemSelected+=i=>{if(i==0)return;var selected=options[(int)i-1];copy.Selected=0;ConfirmPlan(UiText.Get("DispositionCopyWarning"),()=>Apply(p=>TacticalPlanRules.CopyDisposition(p,phase,Plans().Load()!.Get(selected.Kind),selected.Phase)));};
        var ghosts=new CheckBox{Name="ShowPhaseReference",Text=UiText.Get("ShowPhaseReference"),ButtonPressed=_showPhaseReference};CompactTactic(ghosts);actions.AddChild(ghosts);ghosts.Toggled+=v=>{_showPhaseReference=v;ShowTactics();};
    }
    private void WireFreeEditor(InteractiveLineupPitch field,CareerLineup state,Label status)
    {
        var kind=_planKind;var phase=EditingPhase;var readOnly=AcademyTacticsReadOnly;field.DropScope=EditorScope(state);
        var plan=Plans().Load()!.Get(kind);field.SetMeta("reference_positions",_showPhaseReference?11:0);field.Ghost=_showPhaseReference?plan.Disposition(phase==PossessionPhase.WithBall?PossessionPhase.WithoutBall:PossessionPhase.WithBall).Formation:null;
        field.CanMove=(source,x,y)=>{if(readOnly)return false;try{var member=state.Members.Single(m=>m.PlayerId.ToString()==source);if(member.Role!=LineupRole.Starter){field.SetMeta("move_error","No es titular");return false;}TacticalPlanRules.MoveAndAssign(plan,phase,member.SlotId!,x,y);return true;}catch(Exception e) when(e is InvalidDataException or InvalidOperationException){field.SetMeta("move_error",e.Message);return false;}};
        field.MovePlayer=(source,x,y)=>
        {
            try
            {
                if(readOnly)return;
                var member=state.Members.Single(m=>m.PlayerId.ToString()==source);var slot=state.Formation.Slots.Single(s=>s.Id==member.SlotId);
                var current=Plans().Load()!;if(current.Revision!=state.Revision)throw new InvalidOperationException("Los planes cambiaron.");
                Plans().Save(current.Replace(TacticalPlanRules.MoveAndAssign(current.Get(kind),phase,slot.Id,x,y)));
                _lineupSlot=slot.Id;_lineupPinned=true;ShowTactics();
            }
            catch(Exception e){GD.PushWarning(e.ToString());status.Text=UiText.Get("PlanMoveError");}
        };
    }
}
