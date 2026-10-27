using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Emblema abstracto original y provisional; no copia escudos reales ni atribuye una identidad gráfica definitiva.</summary>
public partial class ClubEmblem : Control
{
    public ClubEmblem()
    {
        Name = "ClubEmblem"; CustomMinimumSize = new Vector2(62, 74); MouseFilter = MouseFilterEnum.Ignore;
        TooltipText = UiText.Get("ClubEmblemPlaceholder");
        var letter = new Label { Text = UiText.Get("ClubEmblemLetter"), HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
        letter.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        letter.AddThemeFontSizeOverride("font_size", 28); letter.AddThemeColorOverride("font_color", new Color("f1b96b"));
        AddChild(letter); Resized += QueueRedraw;
    }
    public override void _Draw()
    {
        var points = new[] { new Vector2(5, 5), new Vector2(Size.X - 5, 5), new Vector2(Size.X - 7, Size.Y * .65f),
            new Vector2(Size.X / 2, Size.Y - 5), new Vector2(7, Size.Y * .65f) };
        DrawColoredPolygon(points, new Color("1c344c"));
        DrawPolyline(points.Append(points[0]).ToArray(), new Color("f1b96b"), 2, true);
    }
}
