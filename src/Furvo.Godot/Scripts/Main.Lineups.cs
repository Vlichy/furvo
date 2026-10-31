using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Furvo.Core.Lineups;
using Furvo.Core.Tactics;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Godot;
namespace Furvo.Presentation;
public partial class Main
{
    private string? _lineupSlot;
    private bool _lineupPinned;
    private ILineupEditorStore Lineups()=>_academyTeam is {} team?new SqliteAcademyLineupStore(_careers!.PathFor(_workspace!.Career.Id),team,_planKind,EditingPhase):_workspace?.Page==CareerPage.Lineup?
        new SqlitePlanLineupStore(_careers!.PathFor(_workspace.Career.Id),_planKind,EditingPhase):
        new SqliteLineupStore(_careers!.PathFor(_workspace!.Career.Id));
    /// <summary>Los cambios válidos se guardan al soltar o seleccionar; no hay un borrador silencioso al salir de la pantalla.</summary>
    private void RenderTacticLineup(VBoxContainer body,HBoxContainer heading)
    {
        try
        {
            var club=_workspace!.Career.ClubId??throw new InvalidOperationException("Sin club.");_workspace.RequireManagement(club);_workspace.Open(_academyTeam is null?CareerPage.Lineup:CareerPage.Academy);
            var store=Lineups();var state=store.Load();
            if(state is null)
            {
                body.AddChild(ClubUi.Text("LineupLegacyNotice",UiText.Get("LineupLegacyNotice"),17));
                body.AddChild(Button("InitializeLineup",()=>{try{store.InitializeMissing();ShowTactics();}catch(Exception e){GD.PushError(e.ToString());ShowError("LineupError",ShowTactics,()=>NavigateCareer(CareerPage.Desktop));}}));return;
            }
            body.Name="LineupPage";body.SetMeta("lineup_revision",state.Revision);body.SetMeta("lineup_seed",state.Seed);
            var players=store.Players();var assignmentsReadOnly=AcademyAssignmentsReadOnly;var allowBenchExchange=_academyTeam is not null&&!assignmentsReadOnly;var toolbar=new HBoxContainer{Name="LineupToolbar"};heading.AddChild(toolbar);

            var picker=new OptionButton{Name="LineupFormation",CustomMinimumSize=new Vector2(150,40)};toolbar.AddChild(picker);
            foreach(var f in FormationCatalog.All){picker.AddItem(f.Id);if(f.Id==state.Formation.Id)picker.Selected=picker.ItemCount-1;}
            if(!FormationCatalog.All.Any(f=>f.Id==state.Formation.Id)){picker.AddItem(state.Formation.Id);picker.Selected=picker.ItemCount-1;}
            var status=ClubUi.Text("LineupSaveStatus",UiText.Format("LineupSaved",state.Revision),14,"a6b6ca");status.SizeFlagsHorizontal=SizeFlags.Fill;status.CustomMinimumSize=new Vector2(160,0);
            CompactTactic(picker);
            var scroll=new ScrollContainer{Name="TacticLineupScroll",SizeFlagsVertical=SizeFlags.ExpandFill,SizeFlagsHorizontal=SizeFlags.ExpandFill,HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};body.AddChild(scroll);
            var center=new CenterContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill,SizeFlagsVertical=SizeFlags.ExpandFill};scroll.AddChild(center);
            var main=new HBoxContainer{Name="LineupLayout",SizeFlagsVertical=_academyTeam is null?SizeFlags.ExpandFill:SizeFlags.ShrinkCenter,CustomMinimumSize=new Vector2(700,260)};main.AddThemeConstantOverride("separation",22);center.AddChild(main);

            var candidates=new VBoxContainer{Name="LineupCandidates",CustomMinimumSize=new Vector2(310,0),SizeFlagsHorizontal=SizeFlags.ExpandFill};
            var candidateScroll=new ScrollContainer{Name="LineupCandidateScroll",HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled,CustomMinimumSize=new Vector2(390,0),SizeFlagsHorizontal=SizeFlags.ExpandFill,SizeFlagsVertical=SizeFlags.ExpandFill};var panel=new VBoxContainer{Name="LineupSidePanel",SizeFlagsHorizontal=SizeFlags.ExpandFill};panel.AddThemeConstantOverride("separation",12);candidateScroll.AddChild(panel);
            var actions=new HFlowContainer{Name="TacticsActions"};actions.AddThemeConstantOverride("h_separation",8);actions.AddThemeConstantOverride("v_separation",8);panel.AddChild(actions);AddPlanActions(actions,Plans().Load());WireFormationEditor(toolbar,picker,state,status,actions);panel.AddChild(status);if(allowBenchExchange)panel.AddChild(ClubUi.Text("AcademyCallupHelp",UiText.Get("AcademyCallupHelp"),12,"a6b6ca"));panel.AddChild(candidates);
            // Las responsabilidades individuales quedan a la izquierda; candidatos y acciones permanecen a la derecha.
            var individualScroll=new ScrollContainer{Name="IndividualSideScroll",CustomMinimumSize=new Vector2(260,0),HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled,SizeFlagsVertical=SizeFlags.ExpandFill};
            var individualPanel=new VBoxContainer{Name="IndividualSidePanel",CustomMinimumSize=new Vector2(260,0),SizeFlagsHorizontal=SizeFlags.ExpandFill};individualPanel.AddThemeConstantOverride("separation",7);individualScroll.AddChild(individualPanel);main.AddChild(individualScroll);
            void Captains(){var planStore=Plans();var captainBook=planStore.Load()!;var settings=planStore.Settings(captainBook,_planKind);RenderPersonnel(individualPanel,()=>settings,next=>{try{settings=planStore.SaveSettings(_planKind,next);return true;}catch(Exception e){GD.PushWarning(e.Message);status.Text=e.Message;return false;}},players,true);}
            Captains();
            InteractiveLineupPitch? field=null;
            void ShowCandidates(string slotId,bool pin)
            {
                if(GetViewport().GuiIsDragging()||_lineupPinned&&!pin)return;
                _lineupSlot=slotId;if(pin)_lineupPinned=true;
                foreach(var node in candidates.GetChildren()){candidates.RemoveChild(node);node.QueueFree();}
                var slot=state.Formation.Slots.Single(s=>s.Id==slotId);field?.Pin(_lineupPinned?slotId:null);
                candidates.AddChild(ClubUi.Text("LineupCandidatePosition",UiText.Get(slot.TacticalCode is null?"Position_"+slot.Role:"TacticalRole_"+slot.TacticalCode),18,"f1b96b"));
                foreach(var child in individualPanel.GetChildren()){individualPanel.RemoveChild(child);child.QueueFree();}
                RenderIndividualEditor(individualPanel,state,players,slot);Captains();
                candidates.AddChild(ClubUi.Text("LineupCandidateOrder",UiText.Get("LineupCandidateOrder"),12,"a6b6ca"));

                AddLineupChoices(slotId,slot);
                candidates.AddChild(Button("LineupUnpin",()=>{_lineupPinned=false;field?.Pin(null);}));
            }
            void ConfigureDrag(LineupDragButton button,string target)
            {
                button.PlayerKey=target;button.Scope=EditorScope(state);button.SetMeta("player_id",target);
                button.AcceptPlayer=source=>!assignmentsReadOnly&&Guid.TryParse(source,out var id)&&LineupSelection.CanExchange(state,new PlayerId(id),new PlayerId(Guid.Parse(target)),players,allowBenchExchange);
                button.ReceivePlayer=source=>{try{store.Save(LineupSelection.Exchange(state,new PlayerId(Guid.Parse(source)),new PlayerId(Guid.Parse(target)),players,allowBenchExchange));var origin=state.Members.Single(m=>m.PlayerId.ToString()==source);var destination=state.Members.Single(m=>m.PlayerId.ToString()==target);_lineupSlot=origin.Role==LineupRole.Starter&&destination.Role==LineupRole.Starter?origin.SlotId:destination.SlotId??origin.SlotId;_lineupPinned=_lineupSlot is not null;ShowTactics();}
                    catch(Exception e){GD.PushWarning(e.Message);status.Text=UiText.Get("LineupChangeError");}};
            }
            void AddLineupChoices(string? selected,FormationSlot? slot)
            {
                // Titulares solo aparecen sobre el césped; el panel ofrece banquillo/reservas y un intercambio contextual.
                foreach(var role in new[]{LineupRole.Substitute,LineupRole.Reserve})
                {
                    candidates.AddChild(ClubUi.Text("LineupRole_"+role,UiText.Get(role==LineupRole.Substitute?"TacticBench":"TacticReserves"),17,"f1b96b"));
                    var available=players.Where(p=>state.Members.Single(m=>m.PlayerId==p.Id).Role==role);
                    if(slot is not null)available=available.OrderBy(p=>LineupSelection.Compatibility(p,slot.Role)).ThenByDescending(p=>LineupSelection.ProvisionalScore(p,slot.Role)).ThenBy(p=>p.Name,StringComparer.Ordinal);
                    else available=available.OrderBy(p=>p.Name,StringComparer.Ordinal);
                    foreach(var player in available)
                    {
                        var candidate=player;var rank=slot is null?-1:LineupSelection.Compatibility(player,slot.Role);
                        var positions=UiText.Format("BenchNatural",UiText.Get("PositionCode_"+player.Primary))+(player.Secondary.Count==0?"":" · "+UiText.Format("BenchSecondary",string.Join(", ",player.Secondary.Select(p=>UiText.Get("PositionCode_"+p)))));
                        var label=player.Name+"\n"+positions+(slot is null?"":" · "+UiText.Format("BenchDestination",UiText.Get("Suitability_"+rank)));
                        var button=new LineupDragButton{Name="LineupCandidate_"+player.Id,Text=label,CustomMinimumSize=new Vector2(300,52),Alignment=HorizontalAlignment.Left,TextOverrunBehavior=TextServer.OverrunBehavior.TrimEllipsis,TooltipText=label};
                        button.Disabled=assignmentsReadOnly;button.AddThemeFontSizeOverride("font_size",14);button.SetMeta("eligibility",rank);button.SetMeta("assignment",role.ToString());ConfigureDrag(button,player.Id.ToString());candidates.AddChild(button);
                        button.Pressed+=()=>{if(selected is null){status.Text=UiText.Get("LineupChooseSlot");return;}try{store.Save(LineupSelection.Exchange(state,state.Members.Single(m=>m.SlotId==selected).PlayerId,candidate.Id,players));_lineupSlot=selected;_lineupPinned=true;ShowTactics();}
                            catch(InvalidOperationException e){GD.PushWarning(e.Message);status.Text=UiText.Get("LineupSwapError");}
                            catch(Exception e){GD.PushError(e.ToString());status.Text=UiText.Get("LineupChangeError");}};
                    }
                }

            }
            field=new InteractiveLineupPitch(state,players,ShowCandidates,_lineupPinned?_lineupSlot:null,true);field.SizeFlagsHorizontal=SizeFlags.Fill;main.AddChild(field);main.AddChild(candidateScroll);
            void ArrangeLineup(){var available=scroll.Size.Y;if(_academyTeam is not null&&FindChild("AcademyTacticsScroll",true,false) is ScrollContainer academyViewport)available=Mathf.Min(available,academyViewport.Size.Y-20);var height=Mathf.Max(260,available);var width=(height-18)*68/111+80;field.CustomMinimumSize=new Vector2(width,260);main.CustomMinimumSize=new Vector2(width+636,height);}
            scroll.Resized+=ArrangeLineup;center.Resized+=ArrangeLineup;center.Ready+=ArrangeLineup;foreach(var circle in field.GetChildren().OfType<LineupPosition>())ConfigureDrag(circle,circle.GetMeta("player_id").AsString());
            WireFreeEditor(field,state,status);
            candidates.AddChild(ClubUi.Text("LineupChooseSlot",UiText.Get("LineupChooseSlot"),14,"a6b6ca"));AddLineupChoices(null,null);
            if(_lineupSlot is not null&&state.Formation.Slots.Any(s=>s.Id==_lineupSlot)){var pinned=_lineupPinned;_lineupPinned=false;ShowCandidates(_lineupSlot,pinned);}
            var outOfPosition=state.Members.Where(m=>m.Role==LineupRole.Starter).Count(m=>LineupSelection.Compatibility(players.Single(p=>p.Id==m.PlayerId),state.Formation.Slots.Single(s=>s.Id==m.SlotId).Role)==2);
            body.AddChild(ClubUi.Text("LineupSummary",UiText.Format("LineupSummary",state.BenchLimit,state.Members.Count(m=>m.Role==LineupRole.Reserve),outOfPosition),13,"a6b6ca"));
        }
        catch(Exception error){GD.PushError(error.ToString());ShowError("LineupError",ShowTactics,()=>NavigateCareer(CareerPage.Desktop));}
    }
}
