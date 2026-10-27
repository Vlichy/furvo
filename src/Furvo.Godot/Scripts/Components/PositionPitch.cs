using Furvo.Core.Domain;
using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Campo de solo presentación reutilizable; dibuja posiciones registradas, sin entradas, alineaciones ni decisiones tácticas.</summary>
public partial class PositionPitch : Control
{
    private IReadOnlyList<PitchPositionMarker> _markers = Array.Empty<PitchPositionMarker>();
    private readonly List<(PitchPositionMarker Marker, Label Label)> _labels = [];
    private readonly Label _unknown;
    private readonly Color _primary = new("f1b96b");
    private readonly Color _secondary = new("8ccce2");

    public PositionPitch()
    {
        Name = "PositionPitch";
        CustomMinimumSize = new Vector2(280, 190);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        MouseFilter = MouseFilterEnum.Ignore;
        _unknown = new Label { Text = UiText.Get("PitchUnknownPosition"), HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore };
        _unknown.AddThemeFontSizeOverride("font_size", 14);
        AddChild(_unknown);
        Resized += UpdateLayout;
    }

    /// <summary>Conserva exactamente la principal y las secundarias; las marcas no representan aptitud ni nuevas posiciones.</summary>
    public void SetPositions(PlayerRole? primary, IEnumerable<PlayerRole> secondary)
    {
        _markers = PitchPositionLayout.Create(primary, secondary);
        SetMeta("primary_role", primary is { } role ? (int)role : 0);
        foreach (var entry in _labels) { RemoveChild(entry.Label); entry.Label.QueueFree(); }
        _labels.Clear();
        foreach (var marker in _markers)
        {
            var label = new Label { Name = "PitchMarker_" + marker.Role, Text = UiText.Get("PitchCode_" + marker.Role),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore };
            label.AddThemeFontSizeOverride("font_size", 11);
            label.AddThemeColorOverride("font_color", new Color("102032"));
            label.SetMeta("position_role", (int)marker.Role);
            label.SetMeta("position_primary", marker.IsPrimary);
            AddChild(label);
            _labels.Add((marker, label));
        }
        _unknown.Visible = _markers.Count == 0;
        UpdateLayout();
    }

    private Rect2 FieldRect()
    {
        var available = new Vector2(Mathf.Max(1, Size.X - 28), Mathf.Max(1, Size.Y - 28));
        var width = Mathf.Min(available.X, available.Y * 105f / 68f);
        var size = new Vector2(width, width * 68f / 105f);
        return new Rect2((Size - size) / 2, size);
    }

    private static Vector2 Point(Rect2 field, PitchPositionMarker marker) =>
        field.Position + field.Size * new Vector2((float)marker.X, (float)marker.Y);

    private void UpdateLayout()
    {
        var field = FieldRect();
        _unknown.Position = field.Position;
        _unknown.Size = field.Size;
        foreach (var entry in _labels)
        {
            entry.Label.Position = Point(field, entry.Marker) - new Vector2(14, 14);
            entry.Label.Size = new Vector2(28, 28);
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Size.X < 30 || Size.Y < 30) return;
        var field = FieldRect();
        var lines = new Color("87b0a6");
        var center = field.GetCenter();
        DrawRect(field, new Color("1b403b"));
        DrawRect(field, lines, false, 1.5f);
        DrawLine(new Vector2(center.X, field.Position.Y), new Vector2(center.X, field.End.Y), lines, 1.5f);
        DrawArc(center, field.Size.Y * 0.14f, 0, Mathf.Tau, 48, lines, 1.5f, true);
        DrawCircle(center, 2, lines);
        foreach (var right in new[] { false, true })
        {
            var areaWidth = field.Size.X * 0.16f;
            var smallWidth = field.Size.X * 0.055f;
            var edge = right ? field.End.X : field.Position.X;
            DrawRect(new Rect2(new Vector2(right ? edge - areaWidth : edge, field.Position.Y + field.Size.Y * 0.20f),
                new Vector2(areaWidth, field.Size.Y * 0.60f)), lines, false, 1.2f);
            DrawRect(new Rect2(new Vector2(right ? edge - smallWidth : edge, field.Position.Y + field.Size.Y * 0.36f),
                new Vector2(smallWidth, field.Size.Y * 0.28f)), lines, false, 1.2f);
            DrawRect(new Rect2(new Vector2(right ? edge : edge - 5, center.Y - field.Size.Y * 0.09f),
                new Vector2(5, field.Size.Y * 0.18f)), lines, false, 1.2f);
        }
        foreach (var marker in _markers)
        {
            var point = Point(field, marker);
            DrawCircle(point, 14, new Color("102032"));
            DrawCircle(point, 12, marker.IsPrimary ? _primary : _secondary);
        }
    }
}
