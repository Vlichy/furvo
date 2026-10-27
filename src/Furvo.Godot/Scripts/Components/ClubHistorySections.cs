using Furvo.Core.Queries;
using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Bloques compartidos entre síntesis y detalle. Solo la historia explícitamente aprobada puede poblarlos.</summary>
internal static class ClubHistorySections
{
    internal static void Trajectory(Control parent, ClubHistoryDetails h)
    {
        var panel = ClubUi.Card(parent, "HistoryTrajectory"); var known = h.IsApprovedDataAvailable;
        ClubUi.Field(panel, "HistoryFirstSeasons", known ? h.FirstDivisionSeasons : null);
        ClubUi.Field(panel, "HistorySecondSeasons", known ? h.SecondDivisionSeasons : null);
        ClubUi.Field(panel, "HistoryBest", known && h.BestFirstPosition.HasValue ? UiText.Format("HistoryPositionSeason", h.BestFirstPosition, h.BestFirstSeason) : null);
        ClubUi.Field(panel, "HistoryWorst", known && h.WorstFirstPosition.HasValue ? UiText.Format("HistoryPositionSeason", h.WorstFirstPosition, h.WorstFirstSeason) : null);
        ClubUi.Field(panel, "HistoryPromotions", known ? h.Promotions : null);
        ClubUi.Field(panel, "HistoryRelegations", known ? h.Relegations : null);
        ClubUi.Field(panel, "HistoryLastPromotion", known ? h.LastPromotion : null);
        ClubUi.Field(panel, "HistoryLastRelegation", known ? h.LastRelegation : null);
        ClubUi.Field(panel, "HistoryAllTime", known ? h.AllTimeRank : null);
        panel.AddChild(ClubUi.Text("HistoryRankingCriterion", UiText.Get("HistoryRankingCriterion"), 11, "a6b6ca"));
    }
    internal static void Honours(Control parent, ClubHistoryDetails h)
    {
        var panel = ClubUi.Card(parent, "HistoryHonours");
        var header = new HBoxContainer(); panel.AddChild(header);
        foreach (var key in new[] { "HistoryHonourCompetition", "HistoryHonourCount", "HistoryHonourSeasons" })
            header.AddChild(ClubUi.Text(key, UiText.Get(key), 12, "a6b6ca"));
        if (!h.IsApprovedDataAvailable || h.Honours.Count == 0) panel.AddChild(ClubUi.Text("HistoryHonoursPending", UiText.Get("PendingDefinition"), 12));
        else foreach (var honour in h.Honours)
        {
            var row = new HBoxContainer(); panel.AddChild(row);
            row.AddChild(ClubUi.Text("Competition", honour.Competition, 12));
            row.AddChild(ClubUi.Text("Titles", honour.Seasons.Count.ToString(), 12));
            row.AddChild(ClubUi.Text("Seasons", string.Join(UiText.Get("ListSeparator"), honour.Seasons), 12));
        }
    }
    internal static void Legends(Control parent, ClubHistoryDetails h, bool compact = false)
    {
        var panel = ClubUi.Card(parent, "HistoryLegends");
        foreach (var category in new[] { ("HistoryPlayers", h.Players), ("HistoryCoaches", h.Coaches) })
        {
            panel.AddChild(ClubUi.Text(category.Item1 + "Heading", UiText.Get(category.Item1), 13, "a6b6ca"));
            var people = h.IsApprovedDataAvailable ? category.Item2 : Array.Empty<ClubHistoricalPerson>();
            if (people.Count == 0) panel.AddChild(ClubUi.Text(category.Item1 + "Pending", UiText.Get("PendingDefinition"), 12));
            foreach (var person in compact ? people.Take(2) : people)
            {
                panel.AddChild(ClubUi.Text("HistoricalPerson", person.Name, 13));
                panel.AddChild(ClubUi.Text("HistoricalPersonDetails", UiText.Format("HistoricalPersonFormat", ClubUi.Value(person.Period), ClubUi.Value(person.Position), person.Description), 12, "a6b6ca"));
            }
        }
    }
    internal static void Records(Control parent, ClubHistoryDetails h)
    {
        var panel = ClubUi.Card(parent, "HistoryRecords");
        foreach (var field in new[] { ("HistoryTopScorer",h.TopScorer), ("HistoryMostAppearances",h.MostAppearances),
            ("HistoryBiggestWin",h.BiggestWin), ("HistoryBiggestLoss",h.BiggestLoss), ("HistoryWinningStreak",h.WinningStreak),
            ("HistoryUnbeatenStreak",h.UnbeatenStreak), ("HistoryMostGoalsSeason",h.MostGoalsSeason), ("HistoryFewestGoalsSeason",h.FewestGoalsSeason),
            ("HistoryFewestConcededSeason",h.FewestConcededSeason), ("HistoryCleanSheetStreak",h.CleanSheetStreak),
            ("HistoryCoachAppearances",h.LongestServingCoach), ("HistoryAdditionalRecords",h.Records) })
            ClubUi.Field(panel, field.Item1, h.IsApprovedDataAvailable ? field.Item2 : null);
    }
}
