using Furvo.Core.Domain;
using Furvo.Core.Queries;

namespace Furvo.Core.Careers;

public enum CommitmentApproval { Draft, Approved }
public enum TerminationClauseKind { Unknown, Defined, NoSpecificEconomicClause }
public sealed record ContractObjective(string Code,string Description,BoardObjectiveHorizon Horizon,BoardObjectivePriority? Priority,DateOnly? Deadline,bool? Mandatory);
/// <summary>Obligación filosófica explícita, separada del catálogo institucional orientativo.</summary>
public sealed record ContractualPrinciple(string Code,string Description,BoardObjectivePriority? Priority,bool? Mandatory);
public sealed record ManagerCommitments(string Version,CommitmentApproval Approval,IReadOnlyList<ClubPrincipleProposal> Principles,IReadOnlyList<ContractObjective> Objectives)
{
    public IReadOnlyList<ContractualPrinciple> ContractualPhilosophy { get; init; } = Array.Empty<ContractualPrinciple>();
}
public sealed record ManagerTacticalProject(string? Formation,string? ExpectedStyle,string? Importance,bool? Mandatory)
{
    public BoardObjectivePriority? Priority { get; init; }
}

/// <summary>Fechas y salario son datos; la duración se calcula y no se serializa ni persiste por separado.</summary>
public sealed record ManagerEmploymentTerms(DateOnly? StartDate,DateOnly? EndDate,decimal? GrossAnnualSalary,string? Currency,
    string? TerminationConditions,TerminationClauseKind TerminationKind=TerminationClauseKind.Unknown)
{
    public decimal? CalculateDurationMonths()
    {
        if(StartDate is not {} start || EndDate is not {} end || end<=start)return null;
        var whole=(end.Year-start.Year)*12+end.Month-start.Month;
        var exclusiveEnd=(long)end.DayNumber+1;
        if(start.AddMonths(whole).DayNumber>exclusiveEnd)whole--;
        var anchor=start.AddMonths(whole);
        var lastMonth=(9999-start.Year)*12+12-start.Month;
        var next=whole+1<=lastMonth ? start.AddMonths(whole+1).DayNumber : (long)DateOnly.MaxValue.DayNumber+start.Day;
        return Math.Round(whole+(decimal)(exclusiveEnd-anchor.DayNumber)/(next-anchor.DayNumber),2,MidpointRounding.AwayFromZero);
    }
}
public static class ContractCurrencies
{
    // Catálogo inicial admitido, sin conversiones ni selección de una moneda predeterminada para las ofertas.
    public static IReadOnlyList<string> Codes {get;}=Array.AsReadOnly(new[]{"EUR","USD","GBP","CHF","JPY","CAD","AUD"});
    public static bool IsSupported(string? code)=>code is not null && Codes.Contains(code,StringComparer.Ordinal);
}
public sealed record ManagerSigningIssue(string Code,string? Subject=null);

/// <summary>Los datos laborales y las obligaciones expresas se validan separadamente de preferencias y borradores.</summary>
public sealed record ManagerOffer(Guid Id,CoachProfileId ProfileId,ClubId ClubId,SeasonId SeasonId,
    ManagerEmploymentTerms Terms,ManagerCommitments Commitments,ManagerTacticalProject TacticalProject,ClubFinancialResources Resources)
{
    public ManagerSalaryPolicySnapshot? SalaryPolicy { get; init; }
    public IReadOnlyList<ManagerSigningIssue> SigningIssues()
    {
        var issues=new List<ManagerSigningIssue>();
        if(Id==Guid.Empty)issues.Add(new("OfferIdInvalid"));
        if(ProfileId.Value==Guid.Empty)issues.Add(new("OfferCoachInvalid"));
        if(ClubId.Value==Guid.Empty)issues.Add(new("OfferClubInvalid"));
        if(SeasonId.Value==Guid.Empty)issues.Add(new("OfferSeasonInvalid"));
        if(Terms.StartDate is null)issues.Add(new("OfferStartMissing"));
        if(Terms.EndDate is null)issues.Add(new("OfferEndMissing"));
        else if(Terms.StartDate is {} start && Terms.EndDate<=start)issues.Add(new("OfferEndInvalid"));
        if(Terms.GrossAnnualSalary is null)issues.Add(new("OfferSalaryMissing"));
        else if(Terms.GrossAnnualSalary<=0)issues.Add(new("OfferSalaryInvalid"));
        if(SalaryPolicy is {} policy && Terms.GrossAnnualSalary is {} salary)
        {
            if(policy.Rule.Calculate(policy.Country) is not {} expected)issues.Add(new("OfferSalaryRulePending"));
            else if(salary!=expected)issues.Add(new("OfferSalaryRuleMismatch"));
            if(salary<=policy.Country.MinimumGrossAnnualSalary)issues.Add(new("OfferSalaryBelowMinimum"));
            if(Terms.Currency!=policy.Country.Currency)issues.Add(new("OfferSalaryCurrencyMismatch"));
        }
        if(string.IsNullOrWhiteSpace(Terms.Currency))issues.Add(new("OfferCurrencyMissing"));
        else if(!ContractCurrencies.IsSupported(Terms.Currency))issues.Add(new("OfferCurrencyInvalid"));
        if(!Enum.IsDefined(Terms.TerminationKind))issues.Add(new("OfferTerminationKindInvalid"));
        else if(Terms.TerminationKind==TerminationClauseKind.Defined && string.IsNullOrWhiteSpace(Terms.TerminationConditions))issues.Add(new("OfferTerminationTextMissing"));
        var objectives=Commitments.Objectives.Where(o=>o.Mandatory==true).ToArray();
        foreach(var objective in objectives)
        {
            if(string.IsNullOrWhiteSpace(objective.Code)||string.IsNullOrWhiteSpace(objective.Description))issues.Add(new("OfferObjectiveContentMissing",objective.Code));
            if(objective.Priority is not {} priority || !Enum.IsDefined(priority))issues.Add(new("OfferObjectivePriorityMissing",objective.Code));
            if(!Enum.IsDefined(objective.Horizon))issues.Add(new("OfferObjectiveHorizonInvalid",objective.Code));
        }
        var philosophy=Commitments.ContractualPhilosophy.Where(p=>p.Mandatory==true).ToArray();
        foreach(var principle in philosophy)
        {
            if(string.IsNullOrWhiteSpace(principle.Code)||string.IsNullOrWhiteSpace(principle.Description))issues.Add(new("OfferPhilosophyContentMissing",principle.Code));
            if(principle.Priority is not {} priority || !Enum.IsDefined(priority))issues.Add(new("OfferPhilosophyPriorityMissing",principle.Code));
        }
        if(objectives.Select(o=>o.Code).Distinct(StringComparer.Ordinal).Count()!=objectives.Length || philosophy.Select(p=>p.Code).Distinct(StringComparer.Ordinal).Count()!=philosophy.Length)
            issues.Add(new("OfferObligationCodesRepeated"));
        if(TacticalProject.Mandatory==true)
        {
            if(string.IsNullOrWhiteSpace(TacticalProject.Formation) && string.IsNullOrWhiteSpace(TacticalProject.ExpectedStyle))issues.Add(new("OfferTacticalContentMissing"));
            if(TacticalProject.Priority is not {} priority || !Enum.IsDefined(priority))issues.Add(new("OfferTacticalPriorityMissing"));
        }
        if(Resources.ClubId!=ClubId || Resources.SeasonId!=SeasonId)issues.Add(new("OfferResourcesMismatch"));
        return issues.AsReadOnly();
    }
    public IReadOnlyList<string> SigningBlocks()=>Array.AsReadOnly(SigningIssues().Select(i=>i.Code).ToArray());
    public ManagerOffer Freeze()=>this with{Commitments=Commitments with
    {Principles=Array.AsReadOnly(Commitments.Principles.ToArray()),Objectives=Array.AsReadOnly(Commitments.Objectives.ToArray()),
     ContractualPhilosophy=Array.AsReadOnly(Commitments.ContractualPhilosophy.ToArray())},Resources=Resources with{},Terms=Terms with{},TacticalProject=TacticalProject with{}};
    /// <summary>Solo las obligaciones expresamente exigibles se aceptan; los borradores no se promueven a compromisos.</summary>
    public ManagerOffer ForSignature()
    {
        if(SigningIssues().Count!=0)throw new InvalidOperationException("Condiciones obligatorias incompletas.");
        var frozen=Freeze();
        return frozen with{Terms=frozen.Terms with{TerminationConditions=frozen.Terms.TerminationKind==TerminationClauseKind.Defined?frozen.Terms.TerminationConditions:null},
            Commitments=frozen.Commitments with{Principles=Array.Empty<ClubPrincipleProposal>(),
                Objectives=Array.AsReadOnly(frozen.Commitments.Objectives.Where(o=>o.Mandatory==true).ToArray()),
                ContractualPhilosophy=Array.AsReadOnly(frozen.Commitments.ContractualPhilosophy.Where(p=>p.Mandatory==true).ToArray())},
            TacticalProject=frozen.TacticalProject.Mandatory==true?frozen.TacticalProject:new ManagerTacticalProject(null,null,null,false)};
    }
}
public interface IManagerOfferSource { ManagerOffer Create(CoachProfile profile,ClubId clubId); }
