using Furvo.Core.Queries;
using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Terminología y consulta visual previa a la elección: sin estado financiero inferido ni contabilidad implementada.</summary>
public partial class ClubEconomyPage : VBoxContainer
{
    public ClubEconomyPage(string clubId, IReadOnlyList<SeasonSummary> seasons, DateOnly referenceDate, ClubFinancialResources resources)
    {
        Name = "ClubEconomyPage"; SetMeta("club_id", clubId);
        var body = ClubUi.ScrollBody(this, "ClubEconomyScroll");
        body.AddChild(new SeasonSelector(seasons, referenceDate));
        var indicators = new GridContainer { Name = "EconomyIndicators", Columns = 4, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        indicators.AddThemeConstantOverride("h_separation", 10); indicators.AddThemeConstantOverride("v_separation", 10); body.AddChild(indicators);
        indicators.Resized += () => { var count = indicators.Size.X < 1000 ? 2 : 4; if (indicators.Columns != count) indicators.Columns = count; };
        foreach (var key in new[] { "EconomySituation", "EconomyTransferBudget", "EconomyRemainingWages", "EconomyDebt" })
        {
            var panel = ClubUi.Card(indicators, key); panel.SetMeta("economic_indicator", key);
            var amount = key switch { "EconomyTransferBudget"=>resources.TransferBudget, "EconomyRemainingWages"=>resources.PlayerWageRemaining, _=>(decimal?)null };
            var display = key=="EconomySituation"?UiText.Get("NotEvaluated"):ManagerOfferDocument.Money(amount,resources.Currency)??UiText.Get("NotAvailable");
            var value = ClubUi.Text(key + "IndicatorValue", display, 16);
            panel.AddChild(value);
        }
        body.AddChild(ClubUi.Text("EconomyPending", UiText.Get("EconomyPending"), 14, "a6b6ca"));
        var columns = ClubUi.ResponsiveColumns(body);
        Block(columns, "EconomyBudgets", ["EconomyCash", "EconomyTransferBudget", "EconomyWageBudget", "EconomyCommittedWages", "EconomyRemainingBudget", "EconomyRemainingWages", "EconomyStaffBudget", "EconomyYouthBudget"], resources);
        Block(columns, "EconomyIncome", ["EconomyTelevision", "EconomySubscribers", "EconomyTickets", "EconomySponsors", "EconomyMerchandising", "EconomyTransferIncome", "EconomyOtherIncome"]);
        var expenses = Block(columns, "EconomyExpenses", ["EconomySalaries", "EconomySignings", "EconomyStadiumMaintenance", "EconomyFacilities", "EconomyStaff", "EconomyOtherExpenses"]);
        var financial = ClubUi.Card(expenses, "EconomyFinancialExpenses");
        ClubUi.Field(financial, "EconomyInterest", null, unavailable: true);
        Block(columns, "EconomyObligations", ["EconomyDebt", "EconomyShortDebt", "EconomyLongDebt", "EconomyInterestRate", "EconomyPrincipalRepayment", "EconomyDueDates", "EconomyPendingPayments"]);
        var evolution = Block(columns, "EconomyFinancialEvolution", ["EconomyBalance", "EconomyOperatingResult", "EconomyEvolution"]);
        ClubUi.Field(evolution, "EconomySituation", UiText.Get("NotEvaluated"));
        body.AddChild(ClubUi.Text("EconomyDefinitions", UiText.Get("EconomyDefinitions"), 12, "a6b6ca"));
    }
    private static VBoxContainer Block(Control parent, string title, string[] fields, ClubFinancialResources? resources=null)
    {
        var panel = ClubUi.Card(parent, title);
        if (title is "EconomyIncome" or "EconomyExpenses") ClubUi.Field(panel, title, null, unavailable: true);
        foreach (var key in fields)
        {
            var amount=key switch { "EconomyTransferBudget"=>resources?.TransferBudget,"EconomyRemainingBudget"=>resources?.TransferRemaining,
                "EconomyWageBudget"=>resources?.PlayerWageBudget,"EconomyRemainingWages"=>resources?.PlayerWageRemaining,
                "EconomyStaffBudget"=>resources?.StaffBudget,"EconomyYouthBudget"=>resources?.YouthBudget,_=>(decimal?)null };
            var display=ManagerOfferDocument.Money(amount,resources?.Currency);ClubUi.Field(panel,key,display,unavailable:display is null);
        }
        return panel;
    }
}
