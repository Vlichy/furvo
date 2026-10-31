using Furvo.Core.Domain;
using Furvo.Core.Lineups;
using Furvo.Core.Tactics;
using Furvo.Presentation.Components;
using Godot;
namespace Furvo.Presentation;
public partial class Main
{
    private bool _individualExpanded;
    private bool _personnelExpanded;
    private string _individualGroup="attack";
    private void RenderIndividualEditor(VBoxContainer panel,CareerLineup lineup,IReadOnlyList<LineupPlayer> players,FormationSlot slot)
    {
        var book=Plans().Load()!;var plan=book.Get(_planKind);var phase=EditingPhase;var disposition=plan.Disposition(phase);var behaviour=disposition.Behaviour(slot.Id);
        var member=lineup.Members.Single(m=>m.SlotId==slot.Id);var player=players.Single(p=>p.Id==member.PlayerId);
        panel.AddChild(ClubUi.Text("IndividualPlayer",player.Name,16,"f1b96b"));
        panel.AddChild(ClubUi.Text("IndividualSuitability",UiText.Get("Suitability_"+LineupSelection.Compatibility(player,slot.Role)),12,"a6b6ca"));
        var title=UiText.Get("Behaviour_"+behaviour.Role)+(behaviour.Overrides.Count>0?" · "+UiText.Get("IndividualCustomized"):"");
        panel.AddChild(ClubUi.Text("IndividualRoleLabel",title,14));
        var roles=IndividualTactics.Roles.Where(r=>r.Positions.Contains(slot.Role)).ToArray();
        var picker=new OptionButton{Name="IndividualRole",FitToLongestItem=false,ClipText=true,SizeFlagsHorizontal=SizeFlags.ExpandFill,CustomMinimumSize=new Vector2(0,32),Disabled=AcademyTacticsReadOnly};panel.AddChild(picker);
        foreach(var role in roles)picker.AddItem(UiText.Get("Behaviour_"+role.Code));picker.Selected=Array.FindIndex(roles,r=>r.Code==behaviour.Role);
        var status=ClubUi.Text("IndividualSaveStatus","",12,"a6b6ca");panel.AddChild(status);
        void Apply(SlotBehaviour next)
        {
            try{if(AcademyTacticsReadOnly)return;var current=Plans().Load()!;if(current.Revision!=book.Revision)throw new InvalidOperationException(UiText.Get("PlanSaveError"));var p=current.Get(_planKind);var d=p.Disposition(phase);var values=d.Behaviours?.ToDictionary()??new Dictionary<string,SlotBehaviour>();values[slot.Id]=next;Plans().Save(current.Replace(p.WithDisposition(phase,d with{Behaviours=values})));ShowTactics();}
            catch(Exception e){GD.PushWarning(e.ToString());status.Text=UiText.Get("PlanSaveError")+" "+e.Message;}
        }
        picker.ItemSelected+=i=>{var chosen=roles[(int)i].Code;picker.Selected=Array.FindIndex(roles,r=>r.Code==behaviour.Role);void Replace()=>Apply(IndividualTactics.SelectRole(behaviour,chosen,slot.Role,phase,true));if(behaviour.Overrides.Count>0)ConfirmPlan(UiText.Get("IndividualReplaceWarning"),Replace);else Replace();};
        var expand=new CheckButton{Name="IndividualExpand",ClipText=true,Text=UiText.Get("IndividualInstructions"),ButtonPressed=_individualExpanded};expand.AddThemeFontSizeOverride("font_size",14);panel.AddChild(expand);
        var controls=new VBoxContainer{Name="IndividualControls",Visible=_individualExpanded};controls.AddThemeConstantOverride("separation",5);panel.AddChild(controls);expand.Toggled+=v=>{_individualExpanded=v;controls.Visible=v;};
        var availableInstructions=IndividualTactics.Available(slot.Role,phase);var groups=availableInstructions.Select(i=>i.Group).Distinct().ToArray();if(!groups.Contains(_individualGroup))_individualGroup=groups[0];
        var groupPicker=new OptionButton{Name="IndividualGroup",FitToLongestItem=false,ClipText=true,CustomMinimumSize=new Vector2(0,30),SizeFlagsHorizontal=SizeFlags.ExpandFill};controls.AddChild(groupPicker);foreach(var group in groups)groupPicker.AddItem(UiText.Get("IndividualGroup_"+group));groupPicker.Selected=Array.IndexOf(groups,_individualGroup);groupPicker.ItemSelected+=i=>{_individualGroup=groups[(int)i];ShowTactics();};
        var effective=IndividualTactics.Effective(behaviour,slot.Role,phase);
        foreach(var instruction in availableInstructions.Where(i=>i.Group==_individualGroup))
        {
            controls.AddChild(ClubUi.Text("IndividualLabel_"+instruction.Code,UiText.Get("Individual_"+instruction.Code),13));
            var choice=new OptionButton{Name="Individual_"+instruction.Code,FitToLongestItem=false,ClipText=true,CustomMinimumSize=new Vector2(0,30),SizeFlagsHorizontal=SizeFlags.ExpandFill,Disabled=AcademyTacticsReadOnly};controls.AddChild(choice);
            foreach(var option in instruction.Options)choice.AddItem(UiText.Get("IndividualOption_"+option));choice.Selected=instruction.Options.ToList().IndexOf(effective[instruction.Code]);
            choice.ItemSelected+=i=>Apply(IndividualTactics.Customize(behaviour,instruction.Code,instruction.Options[(int)i],slot.Role,phase));
        }
        foreach(var warning in IndividualTactics.Warnings(behaviour,slot.Role,phase,plan.Instructions))panel.AddChild(ClubUi.Text(warning,UiText.Get(warning),12,"f1b96b"));
        panel.AddChild(ClubUi.Text("IndividualGeometryNotice",UiText.Get("IndividualGeometryNotice"),11,"a6b6ca"));
    }
    private void RenderPersonnel(VBoxContainer controls,Func<TacticalSettings> get,Func<TacticalSettings,bool> save,IReadOnlyList<LineupPlayer> squad,bool captainsOnly=false)
    {
        var toggle=new CheckButton{Name="PersonnelExpand",ClipText=true,Text=UiText.Get(captainsOnly?"CaptainTitle":"PersonnelTitle"),ButtonPressed=_personnelExpanded};toggle.AddThemeFontSizeOverride("font_size",14);controls.AddChild(toggle);
        var body=new VBoxContainer{Name="PersonnelControls",Visible=_personnelExpanded};body.AddThemeConstantOverride("separation",5);controls.AddChild(body);toggle.Toggled+=v=>{_personnelExpanded=v;body.Visible=v;};
        body.AddChild(ClubUi.Text("PersonnelNotice",UiText.Get("PersonnelNotice"),12,"a6b6ca"));
        foreach(var kind in Enum.GetValues<SpecialistKind>().Where(k=>captainsOnly?k==SpecialistKind.Captain:k!=SpecialistKind.Captain))
        {
            body.AddChild(ClubUi.Text("PersonnelLabel_"+kind,UiText.Get("Specialist_"+kind),15,"f1b96b"));
            var current=(get().Personnel??TeamPersonnel.Empty()).Order(kind);var captainSlots=(get().Personnel??TeamPersonnel.Empty()).Captains;
            var onPitch=Lineups().Load()!.Members.Where(m=>m.Role==LineupRole.Starter).Select(m=>m.PlayerId).ToHashSet();var available=SpecialistSelection.Resolve(get(),kind,onPitch);
            body.AddChild(ClubUi.Text("PersonnelAvailable_"+kind,UiText.Get("PersonnelAvailable")+": "+(available is {} actor?squad.Single(p=>p.Id==actor).Name:UiText.Get("TacticUnassigned")),12,"a6b6ca"));
            for(var ordinal=0;ordinal<(kind==SpecialistKind.Captain?2:5);ordinal++)
            {
                var index=ordinal;var old=kind==SpecialistKind.Captain?captainSlots[index]:current.Count>index?(PlayerId?)current[index]:null;
                var options=squad.Where(p=>!current.Contains(p.Id)||p.Id==old).OrderBy(p=>p.Name,StringComparer.Ordinal).ToArray();
                var box=new OptionButton{Name="Personnel_"+kind+"_"+index,CustomMinimumSize=new Vector2(0,30),SizeFlagsHorizontal=SizeFlags.ExpandFill,FitToLongestItem=false,ClipText=true};body.AddChild(box);box.AddItem((index+1)+" · "+UiText.Get("TacticUnassigned"));foreach(var p in options)box.AddItem((index+1)+" · "+p.Name);box.Selected=old is null?0:Array.FindIndex(options,p=>p.Id==old)+1;
                box.ItemSelected+=i=>{var personnel=get().Personnel??TeamPersonnel.Empty();if(kind==SpecialistKind.Captain){var slots=personnel.Captains.ToArray();slots[index]=i==0?null:options[(int)i-1].Id;if(save(get() with{Personnel=personnel with{CaptainSlots=slots}}))ShowTactics();return;}var values=personnel.Order(kind).Select(p=>(PlayerId?)p).ToList();while(values.Count<=index)values.Add(null);values[index]=i==0?null:options[(int)i-1].Id;var orders=personnel.Priorities.ToDictionary();orders[kind]=values.Where(p=>p is not null).Select(p=>p!.Value).ToArray();if(save(get() with{Personnel=new(orders)}))ShowTactics();};
            }
        }
    }
}
