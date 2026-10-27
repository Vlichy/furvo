using System.Globalization;
using Furvo.Core.Careers;
using Furvo.Core.Queries;
using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Documento de oferta/confirmación desde una instantánea; no vuelve a inventar presupuestos ni compromisos.</summary>
public partial class ManagerOfferDocument:VBoxContainer
{
    public ManagerOfferDocument(ManagerOffer offer,CoachProfile profile,ClubDetails club,SeasonSummary season)
    {
        Name="ManagerOfferDocument";SetMeta("club_id",offer.ClubId.ToString());SetMeta("offer_id",offer.Id.ToString("D"));SizeFlagsVertical=SizeFlags.ExpandFill;
        var body=ClubUi.ScrollBody(this,"ManagerOfferScroll");body.AddChild(new ClubHeader(club));
        var terms=ClubUi.Card(body,"OfferEmploymentTerms");
        ClubUi.Field(terms,"CoachFullName",profile.FullName);ClubUi.Field(terms,"OfferHiringClub",club.Club.Name);ClubUi.Field(terms,"OfferIncorporationSeason",season.Name);
        ClubUi.Field(terms,"OfferStart",Date(offer.Terms.StartDate));ClubUi.Field(terms,"OfferEnd",EndDateText(offer.Terms));
        ClubUi.Field(terms,"OfferAnnualSalary",offer.Terms.GrossAnnualSalary?.ToString("N2",CultureInfo.GetCultureInfo("es-ES")));
        ClubUi.Field(terms,"OfferCurrency",offer.Terms.Currency);
        ClubUi.Field(terms,"OfferTermination",offer.Terms.TerminationKind==TerminationClauseKind.NoSpecificEconomicClause?UiText.Get("NoSpecificEconomicClause"):offer.Terms.TerminationKind==TerminationClauseKind.Defined?offer.Terms.TerminationConditions:null);
        var commitments=ClubUi.ResponsiveColumns(body);var philosophy=ClubUi.Card(commitments,"ClubPhilosophy");var objectives=ClubUi.Card(commitments,"ClubBoardExpectations");
        if(offer.Commitments.Approval!=CommitmentApproval.Approved)
            foreach(var panel in new[]{philosophy,objectives})panel.AddChild(ClubUi.Text("OfferDraftNotice",UiText.Get("OfferPreferencesNotice"),12,"f1b96b"));
        foreach(var p in offer.Commitments.Principles)
        {
            var row=new VBoxContainer();row.SetMeta("principle_code",p.Code);philosophy.AddChild(row);
            row.AddChild(ClubUi.Text("PrincipleTitle",p.Title,14));row.AddChild(ClubUi.Text("PrincipleDescription",p.Description,12,"a6b6ca"));
        }
        if(offer.Commitments.Principles.Count==0)philosophy.AddChild(ClubUi.Text("PendingPhilosophy",UiText.Get("PendingDefinition")));
        foreach(var o in offer.Commitments.Objectives)
        {
            var row=new VBoxContainer();row.SetMeta("objective_code",o.Code);objectives.AddChild(row);
            row.AddChild(ClubUi.Text("ObjectiveDescription",o.Description,13));
            row.AddChild(ClubUi.Text("ObjectiveConditions",UiText.Format("OfferObjectiveConditions",UiText.Get("DirectionHorizon_"+o.Horizon),o.Priority is {} priority?UiText.Get("DirectionPriority_"+priority):UiText.Get("PendingDefinition"),o.Deadline?.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture)??UiText.Get("PendingDefinition"),UiText.Get(o.Mandatory is null?"PendingDefinition":o.Mandatory.Value?"MandatoryObjective":"AdvisoryObjective")),12,"a6b6ca"));
        }
        if(offer.Commitments.ContractualPhilosophy.Count>0)
        {
            var declared=ClubUi.Card(body,"OfferDeclaredPhilosophy");
            foreach(var p in offer.Commitments.ContractualPhilosophy)
            {
                declared.AddChild(ClubUi.Text("ContractualPrinciple",p.Description,13));
                declared.AddChild(ClubUi.Text("ContractualPrincipleKind",UiText.Format("OfferPrincipleConditions",p.Priority is {} priority?UiText.Get("DirectionPriority_"+priority):UiText.Get("PendingDefinition"),UiText.Get(p.Mandatory==true?"MandatoryObjective":p.Mandatory==false?"AdvisoryObjective":"PendingDefinition")),12,"a6b6ca"));
            }
        }
        var tactical=ClubUi.Card(body,"OfferTacticalProject");ClubUi.Field(tactical,"OfferFormation",offer.TacticalProject.Formation);ClubUi.Field(tactical,"OfferPlayingStyle",offer.TacticalProject.ExpectedStyle);ClubUi.Field(tactical,"OfferTacticalImportance",offer.TacticalProject.Importance);
        ClubUi.Field(tactical,"OfferTacticalPriority",offer.TacticalProject.Priority is {} tacticalPriority?UiText.Get("DirectionPriority_"+tacticalPriority):null);
        ClubUi.Field(tactical,"OfferTacticalNature",UiText.Get(offer.TacticalProject.Mandatory==true?"MandatoryObjective":offer.TacticalProject.Mandatory==false?"AdvisoryObjective":"PendingDefinition"));
        tactical.AddChild(ClubUi.Text("OfferTacticsNotice",UiText.Get("OfferTacticsNotice"),12,"a6b6ca"));
        var resources=ClubUi.Card(body,"OfferResources");ClubUi.Field(resources,"EconomySeason",season.Name);
        foreach(var item in new[]{("EconomyTransferBudget",offer.Resources.TransferBudget),
            ("EconomyWageBudget",offer.Resources.PlayerWageBudget),("EconomyRemainingWages",offer.Resources.PlayerWageRemaining),("EconomyStaffBudget",offer.Resources.StaffBudget),("EconomyYouthBudget",offer.Resources.YouthBudget)})
            ClubUi.Field(resources,item.Item1,Money(item.Item2,offer.Resources.Currency));
        var blocks=offer.SigningIssues();SetMeta("signing_block_count",blocks.Count);
        if(blocks.Count>0)
        {
            var pending=ClubUi.Card(body,"OfferSignatureBlocked");foreach(var reason in blocks)pending.AddChild(ClubUi.Text(reason.Code,SigningIssueText(reason),13,"f1b96b"));
        }
    }
    public static string SigningIssueText(ManagerSigningIssue issue)=>string.IsNullOrWhiteSpace(issue.Subject)?UiText.Get(issue.Code):UiText.Format("OfferSigningIssueSubject",UiText.Get(issue.Code),issue.Subject);
    public static string? Money(decimal? amount,string? currency)=>amount.HasValue && currency is not null?amount.Value.ToString("N2",CultureInfo.GetCultureInfo("es-ES"))+" "+currency:null;
    public static string? EndDateText(ManagerEmploymentTerms terms)
    {
        var date=Date(terms.EndDate);
        if(terms.StartDate is {} start && start.Year<9999 && terms.EndDate==start.AddYears(1).AddDays(-1))
            return UiText.Format("OfferEndOneYear",date!);
        return date;
    }
    private static string? Date(DateOnly? date)=>date?.ToString("dd/MM/yyyy",CultureInfo.InvariantCulture);
}
