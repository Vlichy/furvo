using Furvo.Core.Domain;

namespace Furvo.Core.Queries;

/// <summary>Única fuente pública de recursos ofrecidos y mostrados por Economía; null no equivale a cero.</summary>
public sealed record ClubFinancialResources(ClubId ClubId, SeasonId SeasonId)
{
    public ClubEconomicPlan? EconomicPlan { get; init; }
    public decimal? Cash { get; init; }
    public decimal? ManagerCommittedAnnualCost { get; init; }
    public decimal? TransferBudget { get; init; }
    public decimal? TransferRemaining { get; init; }
    public decimal? PlayerWageBudget { get; init; }
    public decimal? PlayerWageRemaining { get; init; }
    public decimal? StaffBudget { get; init; }
    public decimal? YouthBudget { get; init; }
    public string? Currency { get; init; }
}
