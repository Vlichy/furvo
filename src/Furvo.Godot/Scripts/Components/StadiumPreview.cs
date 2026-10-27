using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Imagen vectorial provisional del estadio; no representa instalaciones o fotografías reales.</summary>
public partial class StadiumPreview : Control
{
    public StadiumPreview()
    {
        Name = "StadiumPreview"; CustomMinimumSize = new Vector2(0, 155); MouseFilter = MouseFilterEnum.Ignore;
        TooltipText = UiText.Get("StadiumPlaceholder"); Resized += QueueRedraw;
    }
    public override void _Draw()
    {
        if (Size.X < 40 || Size.Y < 40) return;
        var size = new Vector2(Mathf.Min(Size.X - 24, 360), 130);
        var rect = new Rect2((Size - size) / 2, size);
        DrawRect(rect, new Color("263d55"));
        DrawRect(rect.Grow(-9), new Color("47617b"), false, 7);
        DrawRect(rect.Grow(-20), new Color("f1b96b"), false, 2);
        var pitch = rect.Grow(-30);
        DrawRect(pitch, new Color("1b403b")); DrawRect(pitch, new Color("91b7aa"), false, 1);
        DrawLine(new Vector2(pitch.GetCenter().X, pitch.Position.Y), new Vector2(pitch.GetCenter().X, pitch.End.Y), new Color("91b7aa"));
        DrawArc(pitch.GetCenter(), pitch.Size.Y * .20f, 0, Mathf.Tau, 32, new Color("91b7aa"));
    }
}
