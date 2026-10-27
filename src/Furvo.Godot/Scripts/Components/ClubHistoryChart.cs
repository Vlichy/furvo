using Furvo.Core.Queries;
using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Gráfico preparado para resultados aprobados: primero arriba, divisiones diferenciadas y huecos sin líneas ficticias.</summary>
public partial class ClubHistoryChart : Control
{
    private readonly IReadOnlyList<ClubHistoryPlotPoint> _points;
    public ClubHistoryChart(IReadOnlyList<ClubSeasonStanding> standings)
    {
        Name = "ClubHistoryChart"; CustomMinimumSize = new Vector2(0, 220); SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _points = ClubHistoryPlot.Build(standings); SetMeta("result_count", _points.Count);
        Resized += RebuildLabels;
        RebuildLabels();
    }
    private Rect2 Plot() => new(new Vector2(55, 12), new Vector2(Mathf.Max(1, Size.X - 80), Mathf.Max(1, Size.Y - 52)));
    private Vector2 Point(ClubHistoryPlotPoint p) => Plot().Position + Plot().Size * new Vector2((float)p.X, (float)p.Y);
    private static Color DivisionColor(int division) => new(division == 1 ? "f1b96b" : "8ccce2");
    private void RebuildLabels()
    {
        foreach (var child in GetChildren()) { RemoveChild(child); child.QueueFree(); }
        var area = Plot();
        foreach (var tick in new[] { ("ChartFirstTop", 0f), ("ChartFirstBottom", 0.45f), ("ChartSecondTop", 0.55f), ("ChartSecondBottom", 1f) })
        {
            var label = ClubUi.Text("Tick_" + tick.Item1, UiText.Get(tick.Item1), 11, "a6b6ca");
            label.Position = new Vector2(0, area.Position.Y + area.Size.Y * tick.Item2 - 8); label.Size = new Vector2(50, 18); AddChild(label);
        }
        if (_points.Count == 0)
        {
            var empty = ClubUi.Text("HistoryChartEmpty", UiText.Get("HistoryNoResults"), 15, "a6b6ca");
            empty.HorizontalAlignment = HorizontalAlignment.Center; empty.Position = area.Position; empty.Size = area.Size; AddChild(empty);
        }
        var step = Math.Max(1, (int)Math.Ceiling(_points.Count / 6.0));
        for (var i = 0; i < _points.Count; i++)
        {
            var data = _points[i]; var position = Point(data);
            var marker = ClubUi.Text("HistoryPoint" + i, UiText.Get("ChartPointSymbol"), 14, data.Standing.Division == 1 ? "f1b96b" : "8ccce2");
            marker.Position = position - new Vector2(6, 10); marker.Size = new Vector2(16, 20);
            marker.TooltipText = UiText.Format("HistoryPointTooltip", data.Standing.Season, data.Standing.Division,
                data.Standing.FinalPosition, UiText.Get("HistoryOrigin_" + data.Standing.Origin)); AddChild(marker);
            if (i % step == 0 || i == _points.Count - 1)
            {
                var label = ClubUi.Text("HistorySeason" + i, data.Standing.Season, 11, "a6b6ca");
                label.Position = new Vector2(position.X - 24, area.End.Y + 10); label.Size = new Vector2(55, 20); AddChild(label);
            }
            if (i > 0 && ClubHistoryPlot.CanConnect(_points[i - 1], data) && _points[i - 1].Standing.Division != data.Standing.Division)
            {
                var promotion = data.Standing.Division < _points[i - 1].Standing.Division;
                var label = ClubUi.Text("HistoryTransition" + i, UiText.Get(promotion ? "ChartPromotionSymbol" : "ChartRelegationSymbol"), 17, "e5ecf5");
                label.Position = (position + Point(_points[i - 1])) / 2; label.TooltipText = UiText.Get(promotion ? "HistoryPromotion" : "HistoryRelegation"); AddChild(label);
            }
        }
        QueueRedraw();
    }
    public override void _Draw()
    {
        var area = Plot(); DrawRect(area, new Color("102032"));
        DrawRect(new Rect2(new Vector2(area.Position.X, area.Position.Y + area.Size.Y * 0.55f), new Vector2(area.Size.X, area.Size.Y * 0.45f)), new Color("172c40"));
        DrawRect(area, new Color("40566d"), false, 1);
        for (var i = 1; i < _points.Count; i++)
            if (ClubHistoryPlot.CanConnect(_points[i - 1], _points[i])) DrawLine(Point(_points[i - 1]), Point(_points[i]), DivisionColor(_points[i].Standing.Division), 2, true);
    }
}
