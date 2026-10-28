using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Furvo.Core.Queries;

namespace Furvo.Presentation.Components;

/// <summary>Adapta el estado económico aprobado del club a la oferta; los catálogos históricos sin salarios siguen pendientes.</summary>
public sealed class InitialManagerOfferSource(IWorldQueries queries,InitialManagerSalaryRule? salaryRule=null):IManagerOfferSource
{
    public ManagerOffer Create(CoachProfile profile,ClubId clubId)
    {
        var club=queries.GetClubDetails(clubId);
        var country=ContractCountryPolicies.For(club.Identity.Country);
        var resources=queries.GetFinancialResources(clubId,queries.GetSeasons().Single(s=>s.StartDate<=queries.ReferenceDate && queries.ReferenceDate<=s.EndDate).Id);
        var rule=salaryRule??(resources.EconomicPlan is {} economic
            ?new InitialManagerSalaryRule("initial-economy-2026-27-v1",economic.ProposedManagerGrossAnnualSalary-country.MinimumGrossAnnualSalary)
            :InitialManagerContractPolicy.PendingSalaryRule);
        var season=queries.GetSeasons().Single(s=>s.StartDate<=queries.ReferenceDate && queries.ReferenceDate<=s.EndDate);
        var proposal=ClubDirectionProposalCatalog.Find(clubId.ToString());
        var commitments=new ManagerCommitments(proposal?.Version??"pending",CommitmentApproval.Draft,
            Array.AsReadOnly(proposal?.Principles.ToArray()??[]),Array.AsReadOnly(proposal?.Objectives.Select(o=>new ContractObjective(o.Code,o.Title+": "+o.Description,o.Horizon,o.Priority,o.Deadline,null)).ToArray()??[]));
        return new ManagerOffer(Guid.NewGuid(),profile.Id,clubId,season.Id,new ManagerEmploymentTerms(InitialManagerContractPolicy.Start,InitialManagerContractPolicy.End,rule.Calculate(country),country.Currency,null),commitments,
            new ManagerTacticalProject(null,null,null,null),resources){SalaryPolicy=new(country,rule)};
    }
}
