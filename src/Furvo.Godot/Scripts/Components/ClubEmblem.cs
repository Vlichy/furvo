using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Emblema abstracto provisional por ClubId y monograma; sin copiar insignias comerciales.</summary>
public partial class ClubEmblem : Control
{
    private readonly ClubVisual _visual;
    public ClubEmblem(ClubVisual visual)
    {
        _visual = visual; Name = "ClubEmblem"; CustomMinimumSize = new Vector2(68, 76); MouseFilter = MouseFilterEnum.Ignore;
        TooltipText = UiText.Get("ClubVisualProvisional"); SetMeta("club_id", visual.ClubId);
        var letter = new Label { Text = visual.Monogram, HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
        letter.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        letter.AddThemeFontSizeOverride("font_size", 22); letter.AddThemeColorOverride("font_color", new Color("e5ecf5"));
        AddChild(letter); Resized += QueueRedraw;
    }
    public override void _Draw()
    {
        var points = _visual.EmblemMotif switch
        {
            0 => new[] { new Vector2(5, 5), new Vector2(Size.X - 5, 5), new Vector2(Size.X - 8, Size.Y * .7f), new Vector2(Size.X / 2, Size.Y - 5), new Vector2(8, Size.Y * .7f) },
            1 => new[] { new Vector2(Size.X/2, 3), new Vector2(Size.X-4, Size.Y/2), new Vector2(Size.X/2, Size.Y-3), new Vector2(4, Size.Y/2) },
            2 => Enumerable.Range(0, 8).Select(i => Size/2 + new Vector2(Mathf.Cos(i*Mathf.Tau/8), Mathf.Sin(i*Mathf.Tau/8)) * (Size/2-new Vector2(5, 5))).ToArray(),
            _ => new[] { new Vector2(12, 4), new Vector2(Size.X-12, 4), new Vector2(Size.X-4, Size.Y/2), new Vector2(Size.X-12, Size.Y-4), new Vector2(12, Size.Y-4), new Vector2(4, Size.Y/2) }
        };
        DrawColoredPolygon(points, new Color(_visual.Kits[0].Primary));
        DrawPolyline(points.Append(points[0]).ToArray(), new Color(_visual.Kits[0].Secondary), 3, true);
    }
}
