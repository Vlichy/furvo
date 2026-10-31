using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Campo vectorial horizontal reutilizable. Todas las áreas pertenecen a los extremos izquierdo y derecho.</summary>
public partial class FootballPitch : Control
{
    public bool Vertical {get;}
    public FootballPitch(bool vertical=false)
    {
        Vertical=vertical;Name = "FootballPitch"; CustomMinimumSize = new Vector2(0, 116); MouseFilter = MouseFilterEnum.Ignore;
        Resized += QueueRedraw; SetMeta("end_area_count", FootballPitchGeometry.EndAreas.Count);
    }
    public static Rect2 FieldRect(Vector2 size,bool vertical=false)
    {
        var scale=Mathf.Max(0,vertical?Mathf.Min((size.X-10)/68,(size.Y-18)/111):Mathf.Min((size.X-18)/111,(size.Y-10)/68));var extent=(vertical?new Vector2(68,105):new Vector2(105,68))*scale;
        return new Rect2((size-extent)/2,extent);
    }
    public override void _Draw()
    {
        if (Size.X < 20 || Size.Y < 20) return;
        var rect=FieldRect(Size,Vertical);var scale=(Vertical?rect.Size.Y:rect.Size.X)/105;var offset=rect.Position;
        Vector2 P(double x, double y) => offset + (Vertical?new Vector2((float)y,105-(float)x):new Vector2((float)x,(float)y))*scale;
        var ink = new Color("c5d9c7"); var stroke = Mathf.Max(1, scale * .35f);
        void Box(double x,double y,double w,double h){var a=P(x,y);var b=P(x+w,y+h);DrawRect(new Rect2(new Vector2(Mathf.Min(a.X,b.X),Mathf.Min(a.Y,b.Y)),new Vector2(Mathf.Abs(a.X-b.X),Mathf.Abs(a.Y-b.Y))),ink,false,stroke);}
        void Arc(double x, double y, double radius, double from, double to) => DrawArc(P(x,y), (float)radius*scale, (float)(from-(Vertical?Math.PI/2:0)), (float)(to-(Vertical?Math.PI/2:0)), 32, ink, stroke, true);
        DrawRect(rect, new Color("244c42"));
        Box(0,0,105,68); DrawLine(P(52.5,0),P(52.5,68),ink,stroke,true);
        Arc(52.5,34,9.15,0,Math.Tau); DrawCircle(P(52.5,34), Mathf.Max(1.3f, scale*.4f), ink);
        foreach (var area in FootballPitchGeometry.EndAreas) Box(area.X,area.Y,area.Width,area.Height);
        foreach (var x in new[] {11,94}) DrawCircle(P(x,34),Mathf.Max(1.2f,scale*.4f),ink);
        var angle = FootballPitchGeometry.PenaltyArcHalfAngle;
        Arc(11,34,9.15,-angle,angle); Arc(94,34,9.15,Math.PI-angle,Math.PI+angle);
        Arc(0,0,1,0,Math.PI/2); Arc(105,0,1,Math.PI/2,Math.PI);
        Arc(0,68,1,-Math.PI/2,0); Arc(105,68,1,Math.PI,Math.PI*1.5);
        // Las porterías están fuera del perímetro y centradas en cada línea de meta.
        Box(-2,(68-7.32)/2,2,7.32); Box(105,(68-7.32)/2,2,7.32);
    }
}
