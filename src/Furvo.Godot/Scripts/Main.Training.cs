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
    private readonly TrainingSchedulePolicy _trainingSchedulePolicy=new();
    private SqliteSportsStore Sports()=>new(SportsPath,_trainingSchedulePolicy);
    private IReadOnlyList<ClubTeam> SportsTeams()=>new SqliteClubSquadStore(SportsPath).Teams();
    private Guid SportsTeam=>_trainingTeam??_workspace!.Career.ClubId!.Value.Value;
    private void SportsTeamSelector(VBoxContainer body,Action refresh)
    {
        var teams=SportsTeams();if(!teams.Any(t=>t.Id==SportsTeam))_trainingTeam=teams[0].Id;var selector=new OptionButton{Name="SportsTeamSelector",SizeFlagsHorizontal=SizeFlags.ExpandFill,CustomMinimumSize=new Vector2(0,34)};foreach(var team in teams)selector.AddItem(team.Name);selector.Selected=teams.ToList().FindIndex(t=>t.Id==SportsTeam);body.AddChild(selector);selector.ItemSelected+=i=>{var chosen=teams[(int)i].Id;selector.Selected=teams.ToList().FindIndex(t=>t.Id==SportsTeam);void Change(){_trainingTeam=chosen;refresh();}if(_workspace?.Page==CareerPage.Training)TrainingNavigate(Change,true);else if(TrainingGroupDirty)TrainingGroupGuard(Change);else Change();};
    }
    private static VBoxContainer SportsCard(Control parent,string key)
    {
        var body=ClubUi.Card(parent,key);var generated=body.GetChild(0);body.RemoveChild(generated);generated.QueueFree();return body;
    }
    private void SportsError(Exception error,Label status){GD.PushWarning(error.ToString());status.Text=error.Data[nameof(TrainingConflict)] is TrainingConflict conflict?UiText.Format("TrainingConflictDetail",UiText.Get("Exercise_"+conflict.First.Exercise),conflict.First.Start.ToString("HH:mm"),conflict.First.Ends.ToString("HH:mm"),UiText.Get("Exercise_"+conflict.Second.Exercise),conflict.Second.Start.ToString("HH:mm"),conflict.Second.Ends.ToString("HH:mm")):error.Message;}
    /// <summary>Planificar no ejecuta sesiones ni avanza la fecha. La misma pantalla sirve a todos los equipos del club.</summary>
    private void ShowTraining()
    {
        try
        {
            _workspace!.Open(CareerPage.Training);var service=Sports();var team=SportsTeam;var plan=service.Plan(team);var players=new SqliteClubSquadStore(SportsPath).Candidates(team);var staff=service.Staff(team);var today=service.CareerDate();
            var body=Frame("CareerSection",UiText.Get("NavPage_Training"),UiText.Get("TrainingClockNotice"));body.Name="TrainingPage";SportsTeamSelector(body,ShowTraining);
            var tabs=new HFlowContainer{Name="TrainingTabs"};body.AddChild(tabs);for(var i=0;i<7;i++){var index=i;var b=WindowUi.Button("TrainingTab_"+i,()=>{if(index!=_trainingTab)TrainingNavigate(()=>{_trainingTab=index;ShowTraining();});});TrainingUi.Tab(b,i==_trainingTab);tabs.AddChild(b);}
            var content=ClubUi.ScrollBody(body,"TrainingScroll");RenderTrainingDelegation(content,plan,staff,service);var status=ClubUi.Text("TrainingStatus",plan.RequiresReview?UiText.Get("TrainingReviewRequired"):UiText.Get("TrainingProvisional"),13,"f1b96b");content.AddChild(status);
            RenderTrainingAvailability(content,team,players);
            RenderTrainingPlanner(content,status,team,plan,players,staff,service,today);
            if(_trainingTab==0){RenderTrainingGroups(content,plan,players,staff,ShowTraining);}
        }
        catch(Exception e){GD.PushError(e.ToString());ShowError("TrainingLoadError",()=>NavigateCareer(CareerPage.Training),()=>NavigateCareer(CareerPage.Desktop));}
    }
}
