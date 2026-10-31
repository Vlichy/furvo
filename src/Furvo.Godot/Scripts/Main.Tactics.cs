using Furvo.Core.Domain;
using Furvo.Core.Lineups;
using Furvo.Core.Tactics;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Godot;
namespace Furvo.Presentation;
public partial class Main
{
    private int _tacticTab;
    private string _stylePhase="possession";
    private FreeKickLocation _freeKickLocation=FreeKickLocation.Edge;
    private SetPieceKind _setPieceKind=SetPieceKind.AttackingCorner;
    private void ShowTactics()
    {
        try
        {
            
            var club=_workspace!.Career.ClubId??throw new InvalidOperationException("Sin club.");
            _workspace.RequireManagement(club);_workspace.Open(_academyTeam is null?Furvo.Core.Careers.CareerPage.Lineup:Furvo.Core.Careers.CareerPage.Academy);
            var store=Plans();
            var book=store.Load();var settings=book is null?TacticalSettings.Undefined(club):store.Settings(book,_planKind);
            var lineup=Lineups().Load();
            if(_academyTeam is not null&&FindChild("AcademyTacticsScroll",true,false) is ScrollContainer previousAcademyScroll)_academyTacticsScrollOffset=previousAcademyScroll.ScrollVertical;
            var frame=Frame("CareerSection","","",false);var body=frame;
            if(_academyTeam is {} team){var academyState=Academy().Get(team);AcademyHeader(frame,academyState);body=ClubUi.ScrollBody(frame,"AcademyTacticsScroll");body.CustomMinimumSize=new Vector2(0,520);
                var academyScroll=body.GetParent<ScrollContainer>();var offset=_academyTacticsScrollOffset;async void RestoreScroll(){await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);if(GodotObject.IsInstanceValid(academyScroll))academyScroll.ScrollVertical=offset;}RestoreScroll();
                frame.AddChild(WindowUi.Button("BackAcademy",ShowAcademy));}
            body.Name="TacticsPage";body.SetMeta("tactics_revision",settings.Revision);body.AddThemeConstantOverride("separation",0);
            // Márgenes entre niveles; los grupos interiores se redistribuyen sin coordenadas rígidas.
            void Level(Control row,int top){var margin=new MarginContainer{Name=row.Name+"Margin",SizeFlagsHorizontal=SizeFlags.ExpandFill};margin.AddThemeConstantOverride("margin_top",top);body.AddChild(margin);margin.AddChild(row);}
            
            var heading=new HBoxContainer{Name="TacticsHeading"};Level(heading,6);
            if(_academyTeam is null)heading.AddChild(ClubUi.Text("TacticsClub",_queries!.GetClubDetails(club).Club.Name,19,"f1b96b"));
            if(_tacticTab!=0)AddPlanActions(heading,book);
            var tabs=new HBoxContainer{Name="TacticsTabs"};Level(tabs,16);
            for(var i=0;i<4;i++){var index=i;var tab=Button("TacticTab_"+i,()=>{_tacticTab=index;ShowTactics();});CompactTactic(tab);tab.Disabled=false;if(tab.Disabled)tab.TooltipText=UiText.Get("AcademySetPiecesNotInherited");tab.SetMeta("active",i==_tacticTab);if(i==_tacticTab)tab.AddThemeColorOverride("font_color",_accent);tabs.AddChild(tab);}
            var formationRow=new HFlowContainer{Name="TacticsFormationRow"};formationRow.AddThemeConstantOverride("h_separation",18);formationRow.AddThemeConstantOverride("v_separation",10);Level(formationRow,20);
            AddPlanSelectors(formationRow);
            var formationGroup=new HBoxContainer{Name="TacticalFormationGroup"};formationGroup.AddThemeConstantOverride("separation",8);formationRow.AddChild(formationGroup);
            var formationTitle=ClubUi.Text("FormationLabel",UiText.Get("TacticFormation"),16,"f1b96b");formationTitle.SizeFlagsHorizontal=SizeFlags.Fill;formationTitle.CustomMinimumSize=new Vector2(90,0);formationGroup.AddChild(formationTitle);
            if(_tacticTab!=0){var formationValue=ClubUi.Text("TacticsFormation",lineup?.Formation.Id??UiText.Get("TacticUndefined"),16);formationValue.AutowrapMode=TextServer.AutowrapMode.Off;formationValue.SizeFlagsHorizontal=SizeFlags.Fill;formationValue.CustomMinimumSize=new Vector2(85,0);formationGroup.AddChild(formationValue);}
            var mental=new HFlowContainer{Name="TacticsMentalityRow"};mental.AddThemeConstantOverride("h_separation",18);mental.AddThemeConstantOverride("v_separation",8);Level(mental,16);
            var mentalLabel=ClubUi.Text("MentalityLabel",UiText.Get("TacticMentality"),16,"f1b96b");mentalLabel.SizeFlagsHorizontal=SizeFlags.Fill;mentalLabel.CustomMinimumSize=new Vector2(90,0);mental.AddChild(mentalLabel);
            var select=new OptionButton{Name="TacticsMentality",CustomMinimumSize=new Vector2(185,36)};mental.AddChild(select);select.AddItem(UiText.Get("TacticUndefined"));
            foreach(var value in Enum.GetValues<TeamMentality>())select.AddItem(UiText.Get("Mentality_"+value));
            CompactTactic(select);select.Disabled=AcademyTacticsReadOnly;select.Selected=settings.Mentality is {} m?(int)m:0;select.AddThemeColorOverride("font_color",new Color(select.Selected==0?"9bacbf":"f1b96b"));
            var explanation=ClubUi.Text("TacticsMentalityDescription",UiText.Get("MentalityDescription_"+(settings.Mentality?.ToString()??"Undefined")),13,"a6b6ca");explanation.SizeFlagsHorizontal=SizeFlags.Fill;explanation.CustomMinimumSize=new Vector2(340,0);mental.AddChild(explanation);
            var status=ClubUi.Text("TacticsSaveState",settings.Revision==0?UiText.Get("TacticNotSaved"):UiText.Format("TacticSaved",settings.Revision),12,"a6b6ca");status.SizeFlagsHorizontal=SizeFlags.Fill;status.CustomMinimumSize=new Vector2(210,0);mental.AddChild(status);
            bool Save(TacticalSettings proposed)
            {
                try{if(AcademyTacticsReadOnly&&_tacticTab!=3)throw new InvalidOperationException(UiText.Get("AcademySynchronizationNotice"));settings=store.SaveSettings(_planKind,proposed);body.SetMeta("tactics_revision",settings.Revision);status.Text=UiText.Format("TacticSaved",settings.Revision);return true;}
                catch(Exception e){GD.PushWarning(e.ToString());status.Text=UiText.Get("TacticSaveError")+(e is InvalidDataException or InvalidOperationException?" "+e.Message:"");return false;}
            }
            select.ItemSelected+=i=>{var previous=settings.Mentality;if(Save(settings with{Mentality=i==0?null:(TeamMentality)i})){explanation.Text=UiText.Get("MentalityDescription_"+(settings.Mentality?.ToString()??"Undefined"));select.AddThemeColorOverride("font_color",new Color(i==0?"9bacbf":"f1b96b"));}else select.Selected=previous is {} old?(int)old:0;};
            if(_tacticTab==0)
            {
                var inner=new VBoxContainer{Name="TacticLineupContent",SizeFlagsVertical=SizeFlags.ExpandFill,SizeFlagsHorizontal=SizeFlags.ExpandFill};Level(inner,24);inner.GetParent<MarginContainer>().SizeFlagsVertical=SizeFlags.ExpandFill;RenderTacticLineup(inner,formationGroup);return;
            }
            Level(ClubUi.Text("TacticsPreviewNotice",UiText.Get("TacticPreviewNotice"),12,"a6b6ca"),24);
            var layout=new HBoxContainer{Name="TacticsInstructionLayout",SizeFlagsVertical=SizeFlags.ExpandFill};layout.AddThemeConstantOverride("separation",16);body.AddChild(layout);
            var left=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};layout.AddChild(left);
            var preview=new TacticalPreview(lineup?.Formation??FormationCatalog.Initial(club),settings,_tacticTab,lineup,Lineups().Players()){Name="TacticsPreview",SizeFlagsVertical=SizeFlags.ExpandFill,SizeFlagsHorizontal=SizeFlags.ExpandFill};left.AddChild(preview);
            left.AddChild(ClubUi.Text("TacticsLegend",UiText.Get("TacticDiagramLegend"),11,"a6b6ca"));
            var caption=ClubUi.Text("TacticsPreviewCaption",UiText.Get("TacticPreviewHelp"),13,"a6b6ca");
            var scroll=new ScrollContainer{Name="TacticsInstructionScroll",SizeFlagsHorizontal=SizeFlags.ExpandFill,SizeFlagsVertical=SizeFlags.ExpandFill,HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};layout.AddChild(scroll);
            var controls=new VBoxContainer{Name="TacticsControls",SizeFlagsHorizontal=SizeFlags.ExpandFill,CustomMinimumSize=new Vector2(420,0)};controls.AddThemeConstantOverride("separation",9);scroll.AddChild(controls);controls.AddChild(caption);
            void Focus(string code)
            {
                preview.Update(settings,code,_setPieceKind);
                var value=settings.Instructions.GetValueOrDefault(code);
                caption.Text=UiText.Get("Instruction_"+code)+" · "+(value is null?UiText.Get("TacticUndefined"):UiText.Get("Option_"+code+"_"+value))+"\n"+UiText.Get("InstructionDescription_"+code);
            }
            void Instruction(string code)
            {
                var def=TacticalCatalog.Instructions.Single(d=>d.Code==code);
                controls.AddChild(ClubUi.Text("InstructionLabel_"+code,UiText.Get("Instruction_"+code),15));
                var picker=new OptionButton{Name="Instruction_"+code,CustomMinimumSize=new Vector2(0,36),SizeFlagsHorizontal=SizeFlags.ExpandFill};controls.AddChild(picker);picker.Disabled=AcademyTacticsReadOnly;picker.AddItem(UiText.Get("TacticUndefined"));
                foreach(var option in def.Options)picker.AddItem(UiText.Get("Option_"+code+"_"+option));
                var value=settings.Instructions[code];picker.Selected=value is null?0:def.Options.ToList().IndexOf(value)+1;picker.AddThemeColorOverride("font_color",new Color(value is null?"9bacbf":"f1b96b"));
                picker.FocusEntered+=()=>Focus(code);
                picker.ItemSelected+=i=>{var next=settings.Instructions.ToDictionary();next[code]=i==0?null:def.Options[(int)i-1];
                    if(!Save(settings with{Instructions=next})){var old=settings.Instructions[code];picker.Selected=old is null?0:def.Options.ToList().IndexOf(old)+1;}picker.AddThemeColorOverride("font_color",new Color(settings.Instructions[code] is null?"9bacbf":"f1b96b"));Focus(code);if(code=="recovery")ShowTactics();};
            }
            if(_tacticTab is 1 or 2)
            {
                foreach(var group in (_tacticTab==1?new[]{_stylePhase}:new[]{"transition"}))
                {
                    controls.AddChild(ClubUi.Text("TacticGroup_"+group,UiText.Get("TacticGroup_"+group),18,"f1b96b"));
                    foreach(var definition in TacticalCatalog.Instructions.Where(d=>d.Group==group&&(d.Code!="counter_route"||settings.Instructions.GetValueOrDefault("recovery")=="counter")))Instruction(definition.Code);
                }
                Focus(_tacticTab==1?(_stylePhase=="possession"?"attack_width":"defensive_line"):"recovery");
            }
            else
            {
                controls.AddChild(ClubUi.Text("SetPieceLabel",UiText.Get("TacticSetPiece"),17,"f1b96b"));
                var kindPicker=new OptionButton{Name="SetPieceKind",CustomMinimumSize=new Vector2(0,38)};controls.AddChild(kindPicker);
                foreach(var choice in Enum.GetValues<SetPieceKind>())kindPicker.AddItem(UiText.Get("SetPiece_"+choice));
                kindPicker.Selected=(int)_setPieceKind-1;
                kindPicker.ItemSelected+=i=>{_setPieceKind=(SetPieceKind)(i+1);_freeKickLocation=TacticalSettings.DefaultLocation(_setPieceKind);ShowTactics();};
                var kind=_setPieceKind;
                var located=kind is SetPieceKind.DirectFreeKick or SetPieceKind.IndirectFreeKick or SetPieceKind.DefendingFreeKick;
                if(located){controls.AddChild(ClubUi.Text("FreeKickLocationLabel",UiText.Get("FreeKickLocation"),15,"f1b96b"));var locations=new OptionButton{Name="FreeKickLocation",CustomMinimumSize=new Vector2(0,36)};foreach(var location in Enum.GetValues<FreeKickLocation>())locations.AddItem(UiText.Get("FreeKickLocation_"+location));locations.Selected=(int)_freeKickLocation-1;controls.AddChild(locations);locations.ItemSelected+=i=>{_freeKickLocation=(FreeKickLocation)(i+1);ShowTactics();};}
                TacticalSettings Context()=>located?settings.AtLocation(kind,_freeKickLocation):settings;
                bool SavePiece(TacticalSettings next){if(!located)return Save(next);var locations=settings.FreeKicks?.ToDictionary()??new Dictionary<string,SetPiecePlan>();locations[TacticalSettings.FreeKickKey(kind,_freeKickLocation)]=next.SetPieces[kind];return Save(settings with{FreeKicks=locations});}
                void ExplainPiece(){var current=Context().SetPieces[kind];preview.Location=_freeKickLocation;preview.Update(Context(),"set_piece",kind);caption.Text=UiText.Get("SetPiece_"+kind)+" · "+(kind==SetPieceKind.Penalty?UiText.Get("ShootoutOnly"):current.Routine is null?UiText.Get("TacticUndefined"):RoutineCaption(kind,current.Routine));}
                RenderPersonnel(controls,()=>settings,Save,Lineups().Players());
                RenderSetPieceEditor(controls,Context,SavePiece,ExplainPiece,lineup,Lineups().Players());ExplainPiece();

            }
        }
        catch(Exception error){GD.PushError(error.ToString());ShowError("TacticLoadError",ShowTactics,()=>NavigateCareer(Furvo.Core.Careers.CareerPage.Desktop));}
    }
}
