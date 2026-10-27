using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Ilustraciones geométricas provisionales, sin fotografías ni instalaciones reales.</summary>
public partial class StadiumPreview : Control
{
    private readonly bool _interior;
    public StadiumPreview(bool interior = false)
    {
        _interior = interior; Name = interior ? "StadiumInterior" : "StadiumExterior";
        CustomMinimumSize = new Vector2(0, 116); MouseFilter = MouseFilterEnum.Ignore;
        TooltipText = UiText.Get("StadiumPlaceholder"); Resized += QueueRedraw;
    }
    public override void _Draw()
    {
        if (Size.X < 20 || Size.Y < 20) return;
        var r = new Rect2(new Vector2(5,12), new Vector2(Size.X-10,Size.Y-24));
        DrawRect(r,new Color("1b3047"));
        if (_interior)
        {
            for (var i=0;i<5;i++)
                DrawLine(r.Position + new Vector2(0, i*9), new Vector2(r.End.X,r.Position.Y+i*9), new Color(i%2==0 ? "73869a" : "3e586f"), 5);
            var pitch = new Rect2(new Vector2(r.Position.X+8,r.Position.Y+r.Size.Y*.58f),new Vector2(r.Size.X-16,r.Size.Y*.35f));
            DrawRect(pitch,new Color("2b5a49")); DrawRect(pitch,new Color("b4c9bd"),false,1);
        }
        else
        {
            var front = new Rect2(r.Position+new Vector2(5,22),r.Size-new Vector2(10,24));
            DrawRect(front,new Color("496177"));
            DrawLine(front.Position-new Vector2(4,2),new Vector2(front.End.X+4,front.Position.Y-2),new Color("f1b96b"),4);
            for (var i=1;i<6;i++)
            {
                var x=front.Position.X+front.Size.X*i/6;
                DrawRect(new Rect2(new Vector2(x-3,front.Position.Y+12),new Vector2(6,front.Size.Y-12)),new Color("1b3047"));
            }
        }
    }
}
