using Furvo.Core.Careers;
using Furvo.Core.Development;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Godot;
namespace Furvo.Presentation;
public partial class Main
{
    private int _analysisTab;
    private void ShowSportsAnalysis()
    {
        try{
            _workspace!.Open(CareerPage.Analysis);var service=Sports();var body=Frame("CareerSection",UiText.Get("NavPage_Analysis"),UiText.Get("AnalysisDataNotice"));body.Name="SportsAnalysisPage";SportsTeamSelector(body,ShowSportsAnalysis);var tabs=new HFlowContainer();body.AddChild(tabs);for(var i=0;i<4;i++){var index=i;var button=WindowUi.Button("AnalysisTab_"+i,()=>{_analysisTab=index;ShowSportsAnalysis();});if(i==_analysisTab)button.AddThemeColorOverride("font_color",_accent);tabs.AddChild(button);}
            var content=ClubUi.ScrollBody(body,"SportsAnalysisScroll");var status=ClubUi.Text("AnalysisStatus","",13,"f1b96b");content.AddChild(status);var team=SportsTeam;var players=new SqliteClubSquadStore(SportsPath).Details(team);var plan=service.Plan(team);
            if(_analysisTab==0){var summary=SportsCard(content,"AnalysisTeam");ClubUi.Field(summary,"TrainingPlayers",players.Count.ToString());ClubUi.Field(summary,"TrainingSessionsPlanned",plan.Sessions.Count.ToString());summary.AddChild(ClubUi.Text("AnalysisMatchPending",UiText.Get("AnalysisNoMatches"),16));foreach(var metric in new[]{"Possession","Chances","Shots","Goals","ExpectedGoals","Recoveries","Losses","Pressing","Zones"})ClubUi.Field(summary,"AnalysisMetric_"+metric,UiText.Get("NotAvailable"));}
            if(_analysisTab==1)RenderAnalysisPlayers(content,team,players,plan,service);
            if(_analysisTab==2){content.AddChild(ClubUi.Text("AnalysisOpponentUnavailable",UiText.Get("AnalysisOpponentUnavailable"),17));var subject=new LineEdit{Name="AnalysisRequestSubject",PlaceholderText=UiText.Get("AnalysisRequestSubject")};content.AddChild(subject);content.AddChild(WindowUi.Button("AnalysisRequest",()=>{try{service.RequestAnalysis(new(Guid.NewGuid(),team,subject.Text,service.CareerDate(),null,"PendingData"));ShowSportsAnalysis();}catch(Exception e){SportsError(e,status);}}));foreach(var request in service.AnalysisRequests(team))content.AddChild(ClubUi.Text("AnalysisRequest_"+request.Id,request.Subject+" · "+UiText.Get("AnalysisPendingData"),14));}
            if(_analysisTab==3)RenderAnalysisReports(content,status,team);
        }catch(Exception e){GD.PushError(e.ToString());ShowError("AnalysisLoadError",()=>NavigateCareer(CareerPage.Analysis),()=>NavigateCareer(CareerPage.Desktop));}
    }
    private void ShowSportsTechnology()
    {
        try{
            _workspace!.Open(CareerPage.Facilities);var service=Sports();var facility=service.Facility();var body=Frame("CareerSection",UiText.Get("NavPage_Facilities"),UiText.Get("SportsTechnologyTitle"));body.Name="SportsTechnologyPage";var content=ClubUi.ScrollBody(body,"SportsTechnologyScroll");var status=ClubUi.Text("SportsTechnologyStatus","",13,"f1b96b");content.AddChild(status);var card=SportsCard(content,"FootbonautCard");card.AddChild(ClubUi.Text("FootbonautTitle",UiText.Get("FootbonautTitle"),23,"f1b96b"));ClubUi.Field(card,"FacilityOwnership",UiText.Get("FacilityOwnership_"+facility.Ownership));ClubUi.Field(card,"FacilityCapacity",facility.Capacity.ToString());card.AddChild(ClubUi.Text("FootbonautDescription",UiText.Get("FootbonautDescription"),15));card.AddChild(ClubUi.Text("FootbonautAcquisitionNotice",UiText.Get("FootbonautAcquisitionNotice"),14));
            var requests=service.Acquisitions();var button=WindowUi.Button("FootbonautRequestQuote",()=>{try{service.RequestFacility(new(Guid.NewGuid(),facility.Club,"footbonaut",service.CareerDate(),null,null,null,null,null,null,null,"PendingQuote",null));ShowSportsTechnology();}catch(Exception e){SportsError(e,status);}});button.Disabled=requests.Any(r=>r.Status=="PendingQuote")||facility.Ownership is FacilityOwnership.Installed or FacilityOwnership.Ordered;card.AddChild(button);foreach(var request in requests)card.AddChild(ClubUi.Text("FacilityRequest_"+request.Id,request.RequestedAt.ToString("dd/MM/yyyy")+" · "+UiText.Get("FacilityPendingQuote"),14));
            var reservations=SportsCard(content,"FootbonautReservations");reservations.AddChild(ClubUi.Text("FootbonautReservationsTitle",UiText.Get("FootbonautReservations"),18,"f1b96b"));var count=0;foreach(var team in SportsTeams())foreach(var session in service.Plan(team.Id).Sessions.Where(s=>s.Facility==facility.Id).OrderBy(s=>s.Begins)){count++;reservations.AddChild(ClubUi.Text("FacilityReservation_"+session.Id,session.Begins.ToString("dd/MM/yyyy HH:mm")+" · "+team.Name+" · "+session.Minutes+" min",14));}if(count==0)reservations.AddChild(ClubUi.Text("FacilityNoReservations",UiText.Get("FacilityNoReservations"),14));
        }catch(Exception e){GD.PushError(e.ToString());ShowError("SportsTechnologyError",()=>NavigateCareer(CareerPage.Facilities),()=>NavigateCareer(CareerPage.Desktop));}
    }
}
