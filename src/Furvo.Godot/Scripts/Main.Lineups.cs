using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Furvo.Core.Lineups;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Godot;
namespace Furvo.Presentation;
public partial class Main
{
    private string? _lineupSlot;
    private bool _lineupPinned;
    private SqliteLineupStore Lineups()=>new(_careers!.PathFor(_workspace!.Career.Id));
    /// <summary>Los cambios se guardan realmente al confirmarlos; no hay un borrador silencioso al salir de la pantalla.</summary>
    private void ShowLineup()
    {
        try
        {
            var club=_workspace!.Career.ClubId??throw new InvalidOperationException("Sin club.");_workspace.RequireManagement(club);_workspace.Open(CareerPage.Lineup);
            var store=Lineups();var state=store.Load();var body=Frame("CareerSection","","",false);
            if(state is null)
            {
                body.AddChild(ClubUi.Text("LineupLegacyNotice",UiText.Get("LineupLegacyNotice"),17));
                body.AddChild(Button("InitializeLineup",()=>{try{store.InitializeMissing();ShowLineup();}catch(Exception e){GD.PushError(e.ToString());ShowError("LineupError",ShowLineup,()=>NavigateCareer(CareerPage.Desktop));}}));return;
            }
            body.Name="LineupPage";body.SetMeta("lineup_revision",state.Revision);body.SetMeta("lineup_seed",state.Seed);
            var players=store.Players();var toolbar=new HFlowContainer{Name="LineupToolbar"};body.AddChild(toolbar);
            toolbar.AddChild(ClubUi.Text("LineupClub",_queries!.GetClubDetails(club).Club.Name,19,"f1b96b"));
            var picker=new OptionButton{Name="LineupFormation",CustomMinimumSize=new Vector2(150,40)};toolbar.AddChild(picker);
            foreach(var f in FormationCatalog.All){picker.AddItem(f.Id);if(f.Id==state.Formation.Id)picker.Selected=picker.ItemCount-1;}
            var status=ClubUi.Text("LineupSaveStatus",UiText.Format("LineupSaved",state.Revision),14,"a6b6ca");toolbar.AddChild(status);
            picker.ItemSelected+=index=>
            {
                try{var next=LineupSelection.Generate(club,state.Seed,FormationCatalog.Get(picker.GetItemText((int)index)),players,state.BenchLimit,state);store.Save(next);_lineupSlot=null;_lineupPinned=false;ShowLineup();}
                catch(Exception e){GD.PushError(e.ToString());status.Text=UiText.Get("LineupChangeError");}
            };
            var main=new HBoxContainer{Name="LineupLayout",SizeFlagsVertical=SizeFlags.ExpandFill};main.AddThemeConstantOverride("separation",12);body.AddChild(main);
            var candidates=new VBoxContainer{Name="LineupCandidates",CustomMinimumSize=new Vector2(310,0)};
            var candidateScroll=new ScrollContainer{Name="LineupCandidateScroll",CustomMinimumSize=new Vector2(310,0),SizeFlagsVertical=SizeFlags.ExpandFill};candidateScroll.AddChild(candidates);
            InteractiveLineupPitch? field=null;
            void ShowCandidates(string slotId,bool pin)
            {
                if(_lineupPinned&&!pin)return;
                _lineupSlot=slotId;if(pin)_lineupPinned=true;
                foreach(var node in candidates.GetChildren()){candidates.RemoveChild(node);node.QueueFree();}
                var slot=state.Formation.Slots.Single(s=>s.Id==slotId);field?.Pin(_lineupPinned?slotId:null);
                candidates.AddChild(ClubUi.Text("LineupCandidatePosition",UiText.Get("Position_"+slot.Role),18,"f1b96b"));
                candidates.AddChild(ClubUi.Text("LineupCandidateOrder",UiText.Get("LineupCandidateOrder"),12,"a6b6ca"));
                foreach(var rank in new[]{0,1})
                {
                    candidates.AddChild(ClubUi.Text("LineupCandidateGroup"+rank,UiText.Get(rank==0?"LineupNatural":"LineupSecondary"),15,"f1b96b"));
                    var group=LineupSelection.Candidates(players,slot.Role).Where(p=>LineupSelection.Compatibility(p,slot.Role)==rank).ToArray();
                    if(group.Length==0)candidates.AddChild(ClubUi.Text("NoLineupCandidates",UiText.Get("LineupNoCandidates"),13,"a6b6ca"));
                    foreach(var player in group)
                    {
                        var candidate=player;var button=new Button{Name="LineupCandidate_"+player.Id,Text=player.Name,CustomMinimumSize=new Vector2(300,40),TextOverrunBehavior=TextServer.OverrunBehavior.TrimEllipsis,TooltipText=player.Name};
                        button.SetMeta("player_id",player.Id.ToString());button.SetMeta("eligibility",rank);candidates.AddChild(button);
                        button.Pressed+=()=>
                        {
                            try{var changed=LineupSelection.Replace(state,slotId,candidate.Id,players);store.Save(changed);_lineupSlot=slotId;_lineupPinned=true;ShowLineup();}
                            catch(InvalidOperationException e){GD.PushWarning(e.Message);status.Text=UiText.Get("LineupSwapError");}
                            catch(Exception e){GD.PushError(e.ToString());status.Text=UiText.Get("LineupChangeError");}
                        };
                    }
                }
                candidates.AddChild(Button("LineupUnpin",()=>{_lineupPinned=false;field?.Pin(null);}));
            }
            field=new InteractiveLineupPitch(state,players,ShowCandidates,_lineupPinned?_lineupSlot:null);main.AddChild(field);main.AddChild(candidateScroll);
            candidates.AddChild(ClubUi.Text("LineupChooseSlot",UiText.Get("LineupChooseSlot"),16,"a6b6ca"));
            if(_lineupSlot is not null&&state.Formation.Slots.Any(s=>s.Id==_lineupSlot)){var pinned=_lineupPinned;_lineupPinned=false;ShowCandidates(_lineupSlot,pinned);}
            var outOfPosition=state.Members.Where(m=>m.Role==LineupRole.Starter).Count(m=>LineupSelection.Compatibility(players.Single(p=>p.Id==m.PlayerId),state.Formation.Slots.Single(s=>s.Id==m.SlotId).Role)==2);
            body.AddChild(ClubUi.Text("LineupSummary",UiText.Format("LineupSummary",state.BenchLimit,state.Members.Count(m=>m.Role==LineupRole.Reserve),outOfPosition),13,"a6b6ca"));
        }
        catch(Exception error){GD.PushError(error.ToString());ShowError("LineupError",ShowLineup,()=>NavigateCareer(CareerPage.Desktop));}
    }
}
