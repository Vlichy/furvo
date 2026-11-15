using Furvo.Core.Academy;
using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Furvo.Core.Tactics;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Godot;
namespace Furvo.Presentation;
public partial class Main
{
    private Guid? _academyTeam,_academyListSelection;
    private readonly AcademyRosterContexts _academyContexts=new();
    private AcademyRosterContext AcademyContext(Guid team)=>_academyContexts.For(_workspace!.Career.Id.Value,team);
    private int _academyTab;
    private int _academyTacticsScrollOffset;
    private SqliteAcademyStore Academy()=>new(_careers!.PathFor(_workspace!.Career.Id));
    private bool AcademyTacticsReadOnly=>_academyTeam is {} id&&Academy().Get(id).TacticMode==AcademyTacticMode.Synchronized;
    private bool AcademyAssignmentsReadOnly=>_academyTeam is {} id&&Academy().Get(id).EffectiveManagement==AcademyManagement.Delegated;
    private void OpenAcademyTeam(Guid id,int? requestedTab=null){var tab=requestedTab??AcademyContext(id).Section;AcademyContext(id).Section=tab;_academyListSelection=id;if(_academyTeam!=id){_planKind=TacticalPlanKind.Initial;_layoutPhase=PossessionPhase.WithoutBall;_stylePhase="defending";}if(_academyTeam!=id||tab!=2)_academyTacticsScrollOffset=0;if(tab==2&&(_academyTeam!=id||_academyTab!=2))_tacticTab=0;_academyTeam=id;_academyTab=tab;_lineupSlot=null;_lineupPinned=false;_workspace!.Open(CareerPage.Academy);if(tab==2)ShowTactics();else ShowAcademyTeam();}
    private void ShowAcademy()
    {
        _academyTeam=null;_academyTab=0;_academyTacticsScrollOffset=0;_workspace!.Open(CareerPage.Academy);_workspace.RequireManagement(_workspace.Career.ClubId!.Value);
        try{
            var store=Academy();var body=Frame("CareerSection",UiText.Get("AcademyTitle"),UiText.Get("AcademyIntroduction"));body.Name="AcademyPage";body.AddThemeConstantOverride("separation",14);
            if(!store.IsInitialized){body.AddChild(ClubUi.Text("AcademyLegacyNotice",UiText.Get("AcademyLegacyNotice"),16));body.AddChild(WindowUi.Button("InitializeAcademy",()=>{try{store.InitializeMissing();ShowAcademy();}catch(Exception e){GD.PushError(e.ToString());ShowError("AcademyLoadError",ShowAcademy,()=>NavigateCareer(CareerPage.Desktop));}}));return;}
            var teams=store.List();body.AddChild(ClubUi.Text("AcademyClub",_queries!.GetClubDetails(_workspace.Career.ClubId.Value).Club.Name,21,"f1b96b"));body.AddChild(ClubUi.Text("AcademyResearchNotice",UiText.Get("AcademyResearchNotice"),13,"a6b6ca"));
            var table=new DataTable([new(UiText.Get("AcademyTeam"),250,35,HeaderAlignment:HorizontalAlignment.Left,Wrap:true),new(UiText.Get("AcademyCategory"),95,12),new(UiText.Get("AcademyPlayers"),65,8),new(UiText.Get("AcademyCoach"),170,25,Wrap:true),new(UiText.Get("AcademyManagement"),95,10),new(UiText.Get("AcademyTacticMode"),110,12)]){Name="AcademyTeamsTable"};body.AddChild(table);
            table.SetRows(teams.Select(s=>new TableRow(s.Team.Id.ToString("D"),new[]{s.Team.Name,UiText.Get("AcademyCategory_"+s.Team.Category),store.PlayerCount(s.Team.Id).ToString(),s.Coach.Name,UiText.Get("AcademyManagement_"+s.EffectiveManagement),UiText.Get("AcademyMode_"+s.TacticMode)})),_academyListSelection?.ToString());
            var open=WindowUi.Button("OpenAcademyTeam",()=>{if(Guid.TryParse(table.SelectedId,out var id))OpenAcademyTeam(id);});open.Disabled=table.SelectedId is null;body.AddChild(open);table.RowSelected+=id=>{_academyListSelection=Guid.Parse(id);open.Disabled=false;};table.RowActivated+=id=>OpenAcademyTeam(Guid.Parse(id));
        }catch(Exception e){GD.PushError(e.ToString());ShowError("AcademyLoadError",ShowAcademy,()=>NavigateCareer(CareerPage.Desktop));}
    }
    private void AcademyHeader(VBoxContainer body,AcademyTeamState state)
    {
        body.AddThemeConstantOverride("separation",12);body.AddChild(ClubUi.Text("AcademyTeamTitle",state.Team.Name,21,"f1b96b"));
        var policies=new HFlowContainer{Name="AcademyPolicies"};policies.AddThemeConstantOverride("h_separation",18);policies.AddThemeConstantOverride("v_separation",10);body.AddChild(policies);
        void Policy(string name,string title,string[] labels,int selected,Action<int> apply){var group=new HBoxContainer();group.AddThemeConstantOverride("separation",8);policies.AddChild(group);var label=ClubUi.Text(name+"Label",UiText.Get(title),14,"f1b96b");label.AutowrapMode=TextServer.AutowrapMode.Off;group.AddChild(label);var picker=new OptionButton{Name=name,CustomMinimumSize=new Vector2(210,36)};foreach(var value in labels)picker.AddItem(UiText.Get(value));picker.Selected=selected;group.AddChild(picker);picker.ItemSelected+=i=>{try{apply((int)i);}catch(Exception e){GD.PushWarning(e.ToString());picker.Selected=selected;ShowError("AcademyPolicyError",()=>OpenAcademyTeam(state.Team.Id,_academyTab),ShowAcademy);}};}
        Policy("AcademyManagementSelector","AcademyManagement",["AcademyManagement_Direct","AcademyManagement_Delegated"],(int)state.EffectiveManagement-1,i=>{var current=Academy().Get(state.Team.Id);Academy().SetManagement(state.Team.Id,(AcademyManagement)(i+1),current.Revision);OpenAcademyTeam(state.Team.Id,_academyTab);});
        Policy("AcademyTacticSelector","AcademyTacticMode",["AcademyMode_Synchronized","AcademyMode_Own"],(int)state.TacticMode-1,i=>{var mode=(AcademyTacticMode)(i+1);var current=Academy().Get(state.Team.Id);if(mode==current.TacticMode)return;void Apply(){try{Academy().SetTacticMode(state.Team.Id,mode,current.Revision,true);OpenAcademyTeam(state.Team.Id,_academyTab);}catch(Exception e){GD.PushWarning(e.ToString());ShowError("AcademyPolicyError",()=>OpenAcademyTeam(state.Team.Id,_academyTab),ShowAcademy);}}if(mode==AcademyTacticMode.Synchronized){var selector=FindChild("AcademyTacticSelector",true,false) as OptionButton;if(selector is not null)selector.Selected=(int)current.TacticMode-1;ConfirmPlan(UiText.Get("AcademySynchronizationWarning"),Apply);}else Apply();});
        var tabs=new HFlowContainer{Name="AcademySections"};tabs.AddThemeConstantOverride("h_separation",8);body.AddChild(tabs);for(var i=0;i<4;i++){var index=i;var b=WindowUi.Button("AcademySection_"+i,()=>OpenAcademyTeam(state.Team.Id,index));if(i==_academyTab)b.AddThemeColorOverride("font_color",_accent);tabs.AddChild(b);}
        if(state.TacticMode==AcademyTacticMode.Synchronized)body.AddChild(ClubUi.Text("AcademySynchronizationNotice",UiText.Get("AcademySynchronizationNotice"),13,"a6b6ca"));
    }
    private void ShowAcademyTeam()
    {
        try{
            var id=_academyTeam!.Value;var store=Academy();var state=store.Get(id);var body=Frame("CareerSection","","",false);body.Name="AcademyTeamPage";AcademyHeader(body,state);
            if(_academyTab==0){body.AddChild(WindowUi.Button("AcademyOpenTraining",()=>{_trainingTeam=id;NavigateCareer(CareerPage.Training);}));var content=ClubUi.ScrollBody(body,"AcademySummaryScroll");ClubUi.Field(content,"AcademyCategory",UiText.Get("AcademyCategory_"+state.Team.Category));ClubUi.Field(content,"AcademyLevel",state.Team.Level.ToString());ClubUi.Field(content,"AcademyCoach",state.Coach.Name);ClubUi.Field(content,"AcademyStaffLink",UiText.Get("AcademyStaffInstitutional"));ClubUi.Field(content,"AcademyPlayers",store.PlayerCount(id).ToString());ClubUi.Field(content,"AcademyCompetition",state.Team.Competition??UiText.Get("AcademyCompetitionPending"));content.AddChild(ClubUi.Text("AcademyDelegationNotice",UiText.Get("AcademyDelegationNotice"),15));}
            else RenderAcademyRoster(body,state,_academyTab==3);
            body.AddChild(WindowUi.Button("BackAcademy",ShowAcademy));
        }catch(Exception e){GD.PushError(e.ToString());ShowError("AcademyLoadError",ShowAcademy,()=>NavigateCareer(CareerPage.Desktop));}
    }
    private void RenderAcademyRoster(VBoxContainer body,AcademyTeamState state,bool development)
    {
        var rows=Academy().Details(state.Team.Id);var context=AcademyContext(state.Team.Id);if(development)body.AddChild(ClubUi.Text("AcademyDevelopmentNotice",UiText.Get("AcademyDevelopmentNotice"),14,"a6b6ca"));
        var filters=new HBoxContainer{Name="AcademyRosterFilters"};body.AddChild(filters);
        var name=new LineEdit{Name="AcademyNameFilter",Text=context.NameFilter,PlaceholderText=UiText.Get("AcademyFilterName"),SizeFlagsHorizontal=SizeFlags.ExpandFill};filters.AddChild(name);
        var position=new OptionButton{Name="AcademyPositionFilter",CustomMinimumSize=new Vector2(220,0),FitToLongestItem=false};position.AddItem(UiText.Get("AcademyAllPositions"));var roles=Enum.GetValues<PlayerRole>();foreach(var role in roles)position.AddItem(UiText.Get("Position_"+role));position.Selected=context.PositionFilter is {} selected?Array.IndexOf(roles,selected)+1:0;filters.AddChild(position);
        var columns=new List<TableColumn>{new(UiText.Get("Name"),220,40,true,HeaderAlignment:HorizontalAlignment.Left),new(UiText.Get("Position"),140,25,true),new(UiText.Get("Age"),65,10,true),new(UiText.Get("Nationality"),110,25)};if(development)columns.Add(new(UiText.Get("AcademySituation"),180,25,Wrap:true));
        var table=new DataTable(columns){Name="AcademyRosterTable"};body.AddChild(table);
        var view=WindowUi.Button("ViewAcademyPlayer",()=>{if(context.Selected is {} id)ShowAcademyPlayer(state.Team.Id,id);});body.AddChild(view);
        void Refresh(){var ordered=context.Rows(rows);table.SetRows(ordered.Select(p=>new TableRow(p.Player.Id.ToString(),new[]{p.Player.Name,UiText.Get("Position_"+p.Player.Position),p.Player.Age?.ToString()??"—",p.Player.Nationality??"—"}.Concat(development?new[]{UiText.Get("AcademySituationAssigned")}:Array.Empty<string>()).ToArray())),context.Selected?.ToString());table.MarkSort(context.Sort,context.Descending);view.Disabled=context.Selected is null;}
        table.RowSelected+=id=>{context.Selected=new PlayerId(Guid.Parse(id));view.Disabled=false;};table.RowActivated+=id=>{context.Selected=new PlayerId(Guid.Parse(id));ShowAcademyPlayer(state.Team.Id,context.Selected.Value);};
        table.SortRequested+=column=>{if(context.Sort==column)context.Descending=!context.Descending;else{context.Sort=column;context.Descending=false;}Refresh();};
        name.TextChanged+=text=>{context.NameFilter=text;Refresh();};position.ItemSelected+=i=>{context.PositionFilter=i==0?null:roles[(int)i-1];Refresh();};Refresh();
    }

    private void ShowAcademyPlayer(Guid team,PlayerId player)
    {
        try{var details=Academy().Player(team,player);var body=Frame("PlayerSection","","",false);body.Name="AcademyPlayerPage";body.AddChild(new PlayerSheet(details));body.AddChild(WindowUi.Button("AcademyOpenFollowup",()=>ShowAcademyFollowup(team,player)));body.AddChild(WindowUi.Button("MovePlayerTeam",()=>ShowPlayerMovement(player)));body.AddChild(WindowUi.Button("BackAcademyPlayer",()=>OpenAcademyTeam(team,_academyTab)));}catch(Exception e){GD.PushError(e.ToString());ShowError("AcademyLoadError",()=>OpenAcademyTeam(team,_academyTab),ShowAcademy);}
    }
}
