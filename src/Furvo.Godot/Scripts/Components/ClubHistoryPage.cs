using Furvo.Core.Queries;
using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Estructura histórica sin resultados inventados. Solo admite mostrar datos aprobados del contrato de consulta.</summary>
public partial class ClubHistoryPage : VBoxContainer
{
    public ClubHistoryPage(ClubHistoryDetails history)
    {
        Name = "ClubHistoryPage"; SetMeta("club_id", history.ClubId.ToString());
        var body = ClubUi.ScrollBody(this, "ClubHistoryScroll");
        if (!history.IsApprovedDataAvailable) body.AddChild(ClubUi.Text("HistoryPendingNotice", UiText.Get("HistoryPendingNotice"), 15, "a6b6ca"));
        var trajectory = ClubUi.Card(body, "HistoryTrajectory");
        var known = history.IsApprovedDataAvailable;
        ClubUi.Field(trajectory, "HistoryFirstSeasons", known ? history.FirstDivisionSeasons : null);
        ClubUi.Field(trajectory, "HistorySecondSeasons", known ? history.SecondDivisionSeasons : null);
        ClubUi.Field(trajectory, "HistoryBest", known && history.BestFirstPosition.HasValue ? UiText.Format("HistoryPositionSeason", history.BestFirstPosition, history.BestFirstSeason) : null);
        ClubUi.Field(trajectory, "HistoryWorst", known && history.WorstFirstPosition.HasValue ? UiText.Format("HistoryPositionSeason", history.WorstFirstPosition, history.WorstFirstSeason) : null);
        ClubUi.Field(trajectory, "HistoryAllTime", known ? history.AllTimeRank : null);
        trajectory.AddChild(ClubUi.Text("HistoryRankingCriterion", UiText.Get("HistoryRankingCriterion"), 13, "a6b6ca"));
        ClubUi.Field(trajectory, "HistoryPromotions", known ? history.Promotions : null);
        ClubUi.Field(trajectory, "HistoryRelegations", known ? history.Relegations : null);
        ClubUi.Field(trajectory, "HistoryLastPromotion", known ? history.LastPromotion : null);
        ClubUi.Field(trajectory, "HistoryLastRelegation", known ? history.LastRelegation : null);
        var rows = known ? history.Standings : Array.Empty<ClubSeasonStanding>();
        var standings = ClubUi.Card(body, "HistoryStandings");
        var table = new DataTable([new(UiText.Get("HistorySeason"), 100, 2), new(UiText.Get("HistoryDivision"), 100, 2), new(UiText.Get("HistoryPosition"), 90)], selectable: false)
            { Name = "HistoryStandingsTable", CustomMinimumSize = new Vector2(0, 180) };
        standings.AddChild(table);
        table.SetRows(rows.OrderBy(r => r.StartYear).Select(r => new TableRow(r.StartYear.ToString(),
            [r.Season, UiText.Get(r.Division == 1 ? "HistoryFirstDivision" : "HistorySecondDivision"), ClubUi.Value(r.FinalPosition)])));
        if (rows.Count == 0) standings.AddChild(ClubUi.Text("HistoryEmptyRows", UiText.Get("HistoryNoResults"), 14, "a6b6ca"));
        var evolution = ClubUi.Card(body, "HistoryEvolution"); evolution.AddChild(new ClubHistoryChart(rows));
        evolution.AddChild(ClubUi.Text("HistoryChartLegend", UiText.Get("HistoryChartLegend"), 13, "a6b6ca"));
        var honours = ClubUi.Card(body, "HistoryHonours");
        if (!known || history.Honours.Count == 0) honours.AddChild(ClubUi.Text("HistoryHonoursPending", UiText.Get("PendingDefinition")));
        else foreach (var honour in history.Honours) honours.AddChild(ClubUi.Text("Honour", UiText.Format("HistoryHonourFormat", honour.Competition, honour.Seasons.Count, string.Join(UiText.Get("ListSeparator"), honour.Seasons))));
        var legends = ClubUi.Card(body, "HistoryLegends");
        ClubUi.Field(legends, "HistoryPlayers", known && history.Players.Count > 0 ? string.Join(UiText.Get("ListSeparator"), history.Players.Select(p => p.Name)) : null);
        ClubUi.Field(legends, "HistoryCoaches", known && history.Coaches.Count > 0 ? string.Join(UiText.Get("ListSeparator"), history.Coaches.Select(p => p.Name)) : null);
        ClubUi.Field(legends, "HistoryTopScorer", known ? history.TopScorer : null);
        ClubUi.Field(legends, "HistoryMostAppearances", known ? history.MostAppearances : null);
        ClubUi.Field(legends, "HistoryCoachAppearances", known ? history.LongestServingCoach : null);
        ClubUi.Field(legends, "HistoryRecords", known ? history.Records : null);
    }
}
