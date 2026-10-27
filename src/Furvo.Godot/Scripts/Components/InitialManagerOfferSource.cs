using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Furvo.Core.Queries;

namespace Furvo.Presentation.Components;

/// <summary>Adapta las mismas fuentes del explorador a ofertas pendientes; aplica fechas aprobadas y configuración económica explícita.</summary>
public sealed class InitialManagerOfferSource(IWorldQueries queries,InitialManagerSalaryRule? salaryRule=null):IManagerOfferSource
{
    public ManagerOffer Create(CoachProfile profile,ClubId clubId)
    {
        var club=queries.GetClubDetails(clubId);
        var country=ContractCountryPolicies.For(club.Identity.Country);
        var rule=salaryRule??InitialManagerContractPolicy.PendingSalaryRule;
        var season=queries.GetSeasons().Single(s=>s.StartDate<=queries.ReferenceDate && queries.ReferenceDate<=s.EndDate);
        var proposal=ClubDirectionProposalCatalog.Find(clubId.ToString());
        var commitments=new ManagerCommitments(proposal?.Version??"pending",CommitmentApproval.Draft,
            Array.AsReadOnly(proposal?.Principles.ToArray()??[]),Array.AsReadOnly(proposal?.Objectives.Select(o=>new ContractObjective(o.Code,o.Title+": "+o.Description,o.Horizon,o.Priority,o.Deadline,null)).ToArray()??[]));
        return new ManagerOffer(Guid.NewGuid(),profile.Id,clubId,season.Id,new ManagerEmploymentTerms(InitialManagerContractPolicy.Start,InitialManagerContractPolicy.End,rule.Calculate(country),country.Currency,null),commitments,
            new ManagerTacticalProject(null,null,null,null),queries.GetFinancialResources(clubId,season.Id)){SalaryPolicy=new(country,rule)};
    }
}
