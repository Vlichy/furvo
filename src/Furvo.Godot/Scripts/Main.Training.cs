using System.Globalization;
using Furvo.Core.Domain;
using Furvo.Core.Careers;
using Furvo.Core.Development;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Godot;
namespace Furvo.Presentation;
public partial class Main
{
    private Guid? _trainingTeam;
    private int _trainingTab;
    private DateOnly? _trainingWeek;
    private string SportsPath=>_careers!.PathFor(_workspace!.Career.Id);
    private SqliteSportsStore Sports()=>new(SportsPath);
    private IReadOnlyList<ClubTeam> SportsTeams()=>new SqliteClubSquadStore(SportsPath).Teams();
    private Guid SportsTeam=>_trainingTeam??_workspace!.Career.ClubId!.Value.Value;
    private void SportsTeamSelector(VBoxContainer body,Action refresh)
    {
        var teams=SportsTeams();if(!teams.Any(t=>t.Id==SportsTeam))_trainingTeam=teams[0].Id;var selector=new OptionButton{Name="SportsTeamSelector",SizeFlagsHorizontal=SizeFlags.ExpandFill,CustomMinimumSize=new Vector2(0,34)};foreach(var team in teams)selector.AddItem(team.Name);selector.Selected=teams.ToList().FindIndex(t=>t.Id==SportsTeam);body.AddChild(selector);selector.ItemSelected+=i=>{_trainingTeam=teams[(int)i].Id;refresh();};
    }
    private static VBoxContainer SportsCard(Control parent,string key)
    {
        var body=ClubUi.Card(parent,key);var generated=body.GetChild(0);body.RemoveChild(generated);generated.QueueFree();return body;
    }
    private void SportsError(Exception error,Label status){GD.PushWarning(error.ToString());status.Text=error.Message;}
    /// <summary>Planificar no ejecuta sesiones ni avanza la fecha. La misma pantalla sirve a todos los equipos del club.</summary>
    private void ShowTraining()
    {
        try
        {
            _workspace!.Open(CareerPage.Training);var service=Sports();var team=SportsTeam;var plan=service.Plan(team);var players=new SqliteClubSquadStore(SportsPath).Candidates(team);var staff=service.Staff(team);var today=service.CareerDate();
            var body=Frame("CareerSection",UiText.Get("NavPage_Training"),UiText.Get("TrainingClockNotice"));body.Name="TrainingPage";SportsTeamSelector(body,ShowTraining);
            var tabs=new HFlowContainer{Name="TrainingTabs"};body.AddChild(tabs);for(var i=0;i<7;i++){var index=i;var b=WindowUi.Button("TrainingTab_"+i,()=>{_trainingTab=index;ShowTraining();});if(i==_trainingTab)b.AddThemeColorOverride("font_color",_accent);tabs.AddChild(b);}
            var content=ClubUi.ScrollBody(body,"TrainingScroll");var status=ClubUi.Text("TrainingStatus",plan.RequiresReview?UiText.Get("TrainingReviewRequired"):UiText.Get("TrainingProvisional"),13,"f1b96b");content.AddChild(status);
            bool Save(TrainingPlan next){try{service.SavePlan(next with{RequiresReview=false});ShowTraining();return true;}catch(Exception e){SportsError(e,status);return false;}}
            RenderTrainingPlanner(content,status,team,plan,players,staff,service,today);
            if(_trainingTab==0){RenderTrainingGroups(content,plan,players,staff,Save);RenderTrainingDelegation(content,plan,staff,Save);}
        }
        catch(Exception e){GD.PushError(e.ToString());ShowError("TrainingLoadError",()=>NavigateCareer(CareerPage.Training),()=>NavigateCareer(CareerPage.Desktop));}
    }
    private void RenderTrainingGroups(VBoxContainer content,TrainingPlan plan,IReadOnlyList<TrainingCandidate> players,IReadOnlyList<TrainingStaff> staff,Func<TrainingPlan,bool> save)
    {
        var card=SportsCard(content,"TrainingGroups");card.AddChild(ClubUi.Text("TrainingGroupsTitle",UiText.Get("TrainingGroups"),18,"f1b96b"));var choose=new OptionButton{Name="TrainingGroupEdit"};choose.AddItem(UiText.Get("TrainingNewGroup"));foreach(var g in plan.Groups)choose.AddItem(g.Name);card.AddChild(choose);var name=new LineEdit{Name="TrainingGroupName",PlaceholderText=UiText.Get("Name")};card.AddChild(name);var objective=new LineEdit{Name="TrainingGroupObjective",PlaceholderText=UiText.Get("TrainingObjective")};card.AddChild(objective);var list=new ItemList{Name="TrainingGroupMembers",SelectMode=ItemList.SelectModeEnum.Multi,CustomMinimumSize=new Vector2(0,170)};foreach(var p in players)list.AddItem(p.Name);card.AddChild(list);
        var groupStaff=staff.Where(p=>p.Responsibilities.Contains(TrainingResponsibility.Group)).ToArray();var owner=new OptionButton{Name="TrainingGroupResponsible"};owner.AddItem(UiText.Get("TrainingUnassignedResponsible"));foreach(var person in groupStaff)owner.AddItem(person.Name=="TrainingHeadCoach"?UiText.Get(person.Name):person.Name);card.AddChild(owner);
        choose.ItemSelected+=i=>{list.DeselectAll();if(i==0){name.Text="";objective.Text="";return;}var selected=plan.Groups[(int)i-1];owner.Selected=selected.Responsible is {} responsible?Array.FindIndex(groupStaff,p=>p.Id==responsible)+1:0;name.Text=selected.Name;objective.Text=selected.Objective;for(var j=0;j<players.Count;j++)if(selected.Members.Contains(players[j].Id))list.Select(j,false);};
        card.AddChild(WindowUi.Button("TrainingSaveGroup",()=>{try{var old=choose.Selected==0?null:plan.Groups[choose.Selected-1];var value=new TrainingGroup(old?.Id??Guid.NewGuid(),name.Text,list.GetSelectedItems().Select(i=>players[i].Id).ToArray(),objective.Text,owner.Selected==0?null:groupStaff[owner.Selected-1].Id);var groups=plan.Groups.Where(g=>g.Id!=value.Id).Append(value).ToArray();save(plan with{Groups=groups,Sessions=plan.Sessions.Select(s=>s.Target.Scope==TrainingScope.Group&&s.Target.Group==value.Id?s with{Participants=TrainingRules.Resolve(s.Target,players,groups)}:s).ToArray()});}catch(Exception error){if(FindChild("TrainingStatus",true,false) is Label message)SportsError(error,message);}}));
    }
    private TrainingResponsibility _trainingDelegationArea=TrainingResponsibility.Collective;
    private void RenderTrainingDelegation(VBoxContainer content,TrainingPlan plan,IReadOnlyList<TrainingStaff> staff,Func<TrainingPlan,bool> save)
    {
        var card=SportsCard(content,"TrainingDelegations");card.AddChild(ClubUi.Text("TrainingManagement",UiText.Get("TrainingManagement_"+plan.Management),18,"f1b96b"));
        var table=new DataTable([new(UiText.Get("TrainingDelegationArea"),210,1,HeaderAlignment:HorizontalAlignment.Left),new(UiText.Get("TrainingResponsibleLabel"),270,2,HeaderAlignment:HorizontalAlignment.Left)],selectable:false,rowHeight:36,constrainColumns:true){Name="TrainingDelegationSummary",CustomMinimumSize=new Vector2(0,180)};card.AddChild(table);
        var areas=Enum.GetValues<TrainingResponsibility>();
        table.SetRows(areas.Select(area=>{var assigned=plan.Delegations.SingleOrDefault(d=>d.Area==area)?.Responsible;var person=staff.SingleOrDefault(s=>s.Id==assigned);return new TableRow(area.ToString(),new[]{UiText.Get("TrainingResponsibility_"+area),person is null?UiText.Get("TrainingManual"):person.Name+" — "+UiText.Get("AnalysisStaff_"+person.Role)});}));
        var row=new GridContainer{Columns=2,SizeFlagsHorizontal=SizeFlags.ExpandFill};row.AddThemeConstantOverride("h_separation",12);card.AddChild(row);var choose=new OptionButton{Name="TrainingDelegationArea",ClipText=true,FitToLongestItem=false,CustomMinimumSize=new Vector2(230,0)};foreach(var area in areas)choose.AddItem(UiText.Get("TrainingResponsibility_"+area));choose.Selected=Array.IndexOf(areas,_trainingDelegationArea);choose.SizeFlagsHorizontal=SizeFlags.ExpandFill;row.AddChild(choose);choose.ItemSelected+=i=>{_trainingDelegationArea=areas[(int)i];ShowTraining();};
        var eligible=staff.Where(s=>s.Id!=_workspace!.Career.Coach.Id.Value&&s.Responsibilities.Contains(_trainingDelegationArea)).ToArray();var owner=new OptionButton{Name="TrainingDelegateResponsible",ClipText=true,CustomMinimumSize=new Vector2(290,0)};owner.AddItem(UiText.Get("TrainingManual"));foreach(var person in eligible)owner.AddItem(person.Name+" — "+UiText.Get("AnalysisStaff_"+person.Role));var current=plan.Delegations.SingleOrDefault(d=>d.Area==_trainingDelegationArea)?.Responsible;owner.Selected=current is null?0:Math.Max(0,Array.FindIndex(eligible,s=>s.Id==current)+1);owner.SizeFlagsHorizontal=SizeFlags.ExpandFill;row.AddChild(owner);
        if(eligible.Length==0)card.AddChild(ClubUi.Text("TrainingNoQualified",UiText.Get("TrainingNoQualifiedStaff"),14));
        card.AddChild(WindowUi.Button("TrainingApplyDelegation",()=>save(plan with{Delegations=plan.Delegations.Where(d=>d.Area!=_trainingDelegationArea).Concat(owner.Selected==0?Array.Empty<TrainingDelegation>():new[]{new TrainingDelegation(_trainingDelegationArea,eligible[owner.Selected-1].Id)}).ToArray()})));
        card.AddChild(WindowUi.Button("TrainingAllManual",()=>TrainingConfirm(UiText.Get("TrainingConfirmManual"),()=>save(plan with{Delegations=Array.Empty<TrainingDelegation>()}))));
        card.AddChild(ClubUi.Text("TrainingDelegationNotice",UiText.Get("TrainingDelegationNotice"),13));
    }
}
