using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Estructura financiera exclusivamente visual: ningún importe, operación ni fórmula económica.</summary>
public partial class ClubEconomyPage : VBoxContainer
{
    public ClubEconomyPage(string clubId)
    {
        Name = "ClubEconomyPage"; SetMeta("club_id", clubId);
        var body = ClubUi.ScrollBody(this, "ClubEconomyScroll");
        body.AddChild(ClubUi.Text("EconomyPending", UiText.Get("EconomyPending"), 14, "a6b6ca"));
        var columns = ClubUi.ResponsiveColumns(body);
        Block(columns, "EconomyBudgets", ["EconomyCash", "EconomyTransferBudget", "EconomyWageBudget", "EconomyCommittedWages", "EconomyRemainingBudget"]);
        Block(columns, "EconomyIncome", ["EconomyTelevision", "EconomySubscribers", "EconomyTickets", "EconomySponsors", "EconomyMerchandising", "EconomyTransferIncome", "EconomyOtherIncome"]);
        Block(columns, "EconomyExpenses", ["EconomySalaries", "EconomySignings", "EconomyStadiumMaintenance", "EconomyFacilities", "EconomyStaff", "EconomyOtherExpenses"]);
        Block(columns, "EconomyObligations", ["EconomyDebt", "EconomyShortDebt", "EconomyLongDebt", "EconomyInterest", "EconomyDueDates", "EconomyPendingPayments"]);
        Block(columns, "EconomyFinancialStatus", ["EconomyBalance", "EconomyOperatingResult", "EconomySituation", "EconomyEvolution"]);
        body.AddChild(ClubUi.Text("EconomyDefinitions", UiText.Get("EconomyDefinitions"), 12, "a6b6ca"));
    }
    private static void Block(Control parent, string title, string[] fields)
    {
        var panel = ClubUi.Card(parent, title);
        // El encabezado también identifica el total como no disponible; jamás se representa un importe desconocido con cero.
        if (title is "EconomyIncome" or "EconomyExpenses") ClubUi.Field(panel, title, null, unavailable: true);
        foreach (var key in fields) ClubUi.Field(panel, key, null, unavailable: true);
    }
}
