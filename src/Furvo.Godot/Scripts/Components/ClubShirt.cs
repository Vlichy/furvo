using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Camiseta vectorial original intercambiable; no incorpora fabricantes, patrocinadores ni marcas.</summary>
public partial class ClubShirt : Control
{
    private readonly ClubKitVisual _kit;
    public ClubShirt(ClubKitVisual kit)
    {
        _kit = kit; CustomMinimumSize = new Vector2(60, 56); MouseFilter = MouseFilterEnum.Ignore;
        TooltipText = UiText.Get("ClubVisualProvisional"); Resized += QueueRedraw;
    }
    public override void _Draw()
    {
        var scale = Mathf.Min(Size.X / 60, Size.Y / 56);
        Vector2 P(float x, float y) => (Size - new Vector2(60, 56) * scale) / 2 + new Vector2(x, y) * scale;
        var points = new[] { P(19, 5), P(24, 8), P(36, 8), P(41, 5), P(56, 18), P(47, 27), P(43, 23), P(43, 51), P(17, 51), P(17, 23), P(13, 27), P(4, 18) };
        var accent = new Color(_kit.Secondary); DrawColoredPolygon(points, new Color(_kit.Primary));
        if (_kit.Pattern == 0) DrawLine(P(18, 29), P(42, 29), accent, 6 * scale, true);
        else if (_kit.Pattern == 1) DrawLine(P(30, 13), P(30, 49), accent, 7 * scale, true);
        else DrawLine(P(19, 18), P(41, 41), accent, 5 * scale, true);
        DrawPolyline(points.Append(points[0]).ToArray(), new Color("a6b6ca"), scale, true);
        DrawLine(P(24, 8), P(30, 12), accent, 2 * scale, true); DrawLine(P(30, 12), P(36, 8), accent, 2 * scale, true);
    }
}
