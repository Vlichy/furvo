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
        var timeline = ClubUi.Card(body, "HistoryTimeline");
        if (!history.IsApprovedDataAvailable || history.Timeline.Count == 0) timeline.AddChild(ClubUi.Text("TimelinePending", UiText.Get("PendingDefinition")));
        else foreach (var entry in history.Timeline) ClubUi.Field(timeline, "HistoryTimelineEntry", UiText.Format("HistoryTimelineFormat", entry.Period, entry.Description));
        ClubHistorySections.Trajectory(body, history);
        var known = history.IsApprovedDataAvailable;
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
        ClubHistorySections.Honours(body, history);
        ClubHistorySections.Legends(body, history);
        ClubHistorySections.Records(body, history);
        ClubUi.Field(body, "HistoryCoachAppearances", known ? history.LongestServingCoach : null);
        ClubUi.Field(body, "HistoryAdditionalRecords", known ? history.Records : null);
    }
}
