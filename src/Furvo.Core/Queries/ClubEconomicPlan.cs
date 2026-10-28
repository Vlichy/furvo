using Furvo.Core.Domain;
namespace Furvo.Core.Queries;

/// <summary>Previsiones aprobadas, separadas de movimientos y compromisos contractuales reales.</summary>
public sealed record EconomicIncome(decimal Television,decimal Subscriptions,decimal Tickets,decimal Sponsorship,decimal Commercial,decimal? PlayerSales)
{ public decimal RecurringTotal=>Television+Subscriptions+Tickets+Sponsorship+Commercial; }
public sealed record EconomicExpense(decimal PlayerWages,decimal SportingPersonnel,decimal YouthDevelopment,decimal StadiumMaintenance,
    decimal SportingOperations,decimal DebtInterest,decimal? PlayerPurchaseCashDisbursement,decimal? PlayerPurchaseAccountingExpense)
{ public decimal RecurringTotal=>PlayerWages+SportingPersonnel+YouthDevelopment+StadiumMaintenance+SportingOperations+DebtInterest; }
public sealed record ClubEconomicPlan
{
    public Guid ClubId {get;init;}
    public string Name {get;init;}="";
    public string Currency {get;init;}="";
    public decimal InitialCash {get;init;}
    public decimal TransferBudget {get;init;}
    public decimal PlayerWageBudget {get;init;}
    public decimal? CommittedPlayerWages {get;init;}
    public decimal? PlayerWageRemaining {get;init;}
    public decimal PlannedPlayerWageExpense {get;init;}
    public decimal PlannedUnallocatedWageCapacity {get;init;}
    public decimal StaffBudget {get;init;}
    public decimal YouthBudget {get;init;}
    public decimal ShortTermDebt {get;init;}
    public decimal LongTermDebt {get;init;}
    public decimal TotalDebt {get;init;}
    public decimal ProposedManagerGrossAnnualSalary {get;init;}
    public decimal PlanningManagerEmployerCost {get;init;}
    public EconomicIncome ForecastIncome {get;init;}=null!;
    public EconomicExpense ForecastExpense {get;init;}=null!;
    public void Validate()
    {
        if(ClubId==Guid.Empty || Currency!="EUR" || string.IsNullOrWhiteSpace(Name) || ForecastIncome is null || ForecastExpense is null)
            throw new InvalidDataException("Identidad o previsión económica incompleta.");
        var values=new[]{InitialCash,TransferBudget,PlayerWageBudget,PlannedPlayerWageExpense,PlannedUnallocatedWageCapacity,StaffBudget,YouthBudget,
            ShortTermDebt,LongTermDebt,TotalDebt,ProposedManagerGrossAnnualSalary,PlanningManagerEmployerCost,
            ForecastIncome.Television,ForecastIncome.Subscriptions,ForecastIncome.Tickets,ForecastIncome.Sponsorship,ForecastIncome.Commercial,
            ForecastExpense.PlayerWages,ForecastExpense.SportingPersonnel,ForecastExpense.YouthDevelopment,ForecastExpense.StadiumMaintenance,ForecastExpense.SportingOperations,ForecastExpense.DebtInterest};
        if(values.Any(v=>v<0 || decimal.Round(v,2)!=v) || TotalDebt!=ShortTermDebt+LongTermDebt ||
           PlannedUnallocatedWageCapacity!=PlayerWageBudget-PlannedPlayerWageExpense || PlannedUnallocatedWageCapacity>PlayerWageBudget ||
           PlanningManagerEmployerCost>ForecastExpense.SportingPersonnel || ForecastExpense.SportingPersonnel>StaffBudget ||
           ForecastExpense.PlayerWages!=PlannedPlayerWageExpense || ForecastExpense.YouthDevelopment!=YouthBudget ||
           CommittedPlayerWages is not null || PlayerWageRemaining is not null ||
           ForecastIncome.PlayerSales is not null || ForecastExpense.PlayerPurchaseCashDisbursement is not null || ForecastExpense.PlayerPurchaseAccountingExpense is not null)
            throw new InvalidDataException("Magnitudes económicas iniciales incoherentes.");
    }
    public ClubFinancialResources Resources(SeasonId season,decimal cash,decimal transferRemaining,decimal? managerCost)=>new(new ClubId(ClubId),season)
    {
        TransferBudget=TransferBudget,TransferRemaining=transferRemaining,PlayerWageBudget=PlayerWageBudget,PlayerWageRemaining=PlayerWageRemaining,
        StaffBudget=StaffBudget,YouthBudget=YouthBudget,Currency=Currency,EconomicPlan=this,Cash=cash,ManagerCommittedAnnualCost=managerCost
    };
}
