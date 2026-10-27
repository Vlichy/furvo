using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Presentación de campos futuros: no calcula cifras ni introduce un sistema económico provisional.</summary>
public partial class ClubEconomyPage : VBoxContainer
{
    public ClubEconomyPage(string clubId)
    {
        Name = "ClubEconomyPage"; SetMeta("club_id", clubId);
        var body = ClubUi.ScrollBody(this, "ClubEconomyScroll");
        body.AddChild(ClubUi.Text("EconomyPending", UiText.Get("EconomyPending"), 15, "a6b6ca"));
        var columns = ClubUi.ResponsiveColumns(body);
        var budgets = ClubUi.Card(columns, "EconomyBudgets");
        foreach (var key in new[] { "EconomyCash", "EconomyTransferBudget", "EconomyWageBudget", "EconomyCommittedWages" }) ClubUi.Field(budgets, key, null, unavailable: true);
        var balance = ClubUi.Card(columns, "EconomyAccounts");
        foreach (var key in new[] { "EconomyIncome", "EconomyExpenses", "EconomyBalance", "EconomyDebt", "EconomySituation" }) ClubUi.Field(balance, key, null, unavailable: true);
        body.AddChild(ClubUi.Text("EconomyDefinitions", UiText.Get("EconomyDefinitions"), 14, "a6b6ca"));
    }
}
