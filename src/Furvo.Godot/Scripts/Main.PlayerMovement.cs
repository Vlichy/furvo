using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Furvo.Core.Development;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Godot;
namespace Furvo.Presentation;
public partial class Main
{
    private void ShowPlayerMovement(PlayerId id)
    {
        try{
            var squads=new SqliteClubSquadStore(SportsPath);var teams=squads.Teams();var actor=squads.Locations()[id];var destinations=teams.Where(t=>t.Id!=actor.Team).ToArray();var service=new SqliteAcademyMobilityStore(SportsPath);var body=Frame("CareerSection",UiText.Get("MovePlayerTeam"),actor.Details.Player.Name);body.Name="PlayerMovementPage";var content=ClubUi.ScrollBody(body,"PlayerMovementScroll");var status=ClubUi.Text("MovementStatus",UiText.Get("MovementRegulationNotice"),15,"f1b96b");content.AddChild(status);var destination=new OptionButton{Name="MovementDestination"};foreach(var team in destinations)destination.AddItem(team.Name);content.AddChild(destination);var kind=new OptionButton{Name="MovementKind"};foreach(var value in Enum.GetValues<MobilityKind>())kind.AddItem(UiText.Get("MovementKind_"+value));content.AddChild(kind);
            var execute=WindowUi.Button("MovementExecute",()=>{try{service.Move(new(id,actor.Team,destinations[destination.Selected].Id,(MobilityKind)(kind.Selected+1),Sports().CareerDate()),_workspace!.Career.Coach.Id.Value);NavigateCareer(CareerPage.OwnSquad);}catch(Exception e){SportsError(e,status);}});content.AddChild(execute);
            void Validate(){var result=service.Check(new(id,actor.Team,destinations[destination.Selected].Id,(MobilityKind)(kind.Selected+1),Sports().CareerDate()));execute.Disabled=!result.Allowed;status.Text=result.Allowed?UiText.Get("MovementReady"):string.Join(" ",result.Reasons);};destination.ItemSelected+=_=>Validate();kind.ItemSelected+=_=>Validate();Validate();
            if(actor.Team!=teams[0].Id){var reason=new LineEdit{Name="MovementRecommendationReason",PlaceholderText=UiText.Get("MovementRecommendationReason")};content.AddChild(reason);content.AddChild(WindowUi.Button("MovementRecommend",()=>{try{var coach=Academy().Get(actor.Team).Coach;service.Recommend(new(Guid.NewGuid(),coach.Id,id,actor.Team,destinations[destination.Selected].Id,(MobilityKind)(kind.Selected+1),reason.Text,Sports().CareerDate(),RecommendationStatus.Pending,RelevantAttributes:actor.Details.AttributeGroups.SelectMany(g=>g.Values).Where(v=>PositionInsightRules.RelevantFor(actor.Details.Player.Position!.Value).Contains(v.Code)).ToDictionary(v=>v.Code,v=>v.Value)));status.Text=UiText.Get("MovementProposalSaved");}catch(Exception e){SportsError(e,status);}}));}
            foreach(var proposal in service.Recommendations().Where(p=>p.Player==id)){var card=SportsCard(content,"MovementProposal_"+proposal.Id);card.AddChild(ClubUi.Text("MovementProposalText",proposal.Reason+" · "+UiText.Get("RecommendationStatus_"+proposal.Status)+" · "+proposal.Age+" · "+teams.Single(t=>t.Id==proposal.Source).Name+" → "+teams.Single(t=>t.Id==proposal.Destination).Name,15));card.AddChild(ClubUi.Text("MovementRegulatorySummary",proposal.RegulatorySummary??UiText.Get("MovementRegulationNotice"),13));foreach(var attribute in proposal.RelevantAttributes??new Dictionary<string,int>())card.AddChild(ClubUi.Text("MovementAttribute_"+attribute.Key,UiText.Get("Attribute_"+attribute.Key)+": "+attribute.Value,12));if(proposal.Status==RecommendationStatus.Pending){card.AddChild(WindowUi.Button("MovementAccept",()=>{try{service.Accept(proposal.Id,_workspace!.Career.Coach.Id.Value);ShowPlayerMovement(id);}catch(Exception e){SportsError(e,status);}}));card.AddChild(WindowUi.Button("MovementReject",()=>{try{service.Reject(proposal.Id,_workspace!.Career.Coach.Id.Value);ShowPlayerMovement(id);}catch(Exception e){SportsError(e,status);}}));}}
            content.AddChild(WindowUi.Button("MovementBack",()=>{if(actor.Team==teams[0].Id)NavigateCareer(CareerPage.OwnSquad);else OpenAcademyTeam(actor.Team,1);}));
        }catch(Exception e){GD.PushError(e.ToString());ShowError("MovementError",()=>NavigateCareer(CareerPage.Academy),()=>NavigateCareer(CareerPage.Desktop));}
    }
}
