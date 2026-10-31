using Furvo.Core.Lineups;
using Furvo.Core.Tactics;
using Godot;
namespace Furvo.Presentation.Components;
/// <summary>Pinta un grafo con extremos enlazados a símbolos. No hay simulación ni trayectorias a puntos anónimos.</summary>
public partial class TacticalPreview:Control
{
    private readonly FootballPitch _background;
    private readonly FormationDefinition _formation;private readonly CareerLineup? _lineup;private readonly IReadOnlyList<LineupPlayer> _players;
    public FreeKickLocation Location {get;set;}=FreeKickLocation.Edge;
    private TacticalSettings _settings;private readonly int _tab;private string _focus="";private SetPieceKind _kind;
    public TacticalPreview(FormationDefinition formation,TacticalSettings settings,int tab,CareerLineup? lineup=null,IReadOnlyList<LineupPlayer>? players=null)
    {
        _formation=formation;_settings=settings;_tab=tab;_lineup=lineup;_players=players??Array.Empty<LineupPlayer>();
        CustomMinimumSize=new Vector2(310,200);MouseFilter=MouseFilterEnum.Ignore;
        _background=new FootballPitch(true);AddChild(_background);ClipContents=true;
        Resized+=()=>{LayoutPitch();GetNodeOrNull<Control>("PreviewOverlay")?.QueueRedraw();};
    }
    public void Update(TacticalSettings settings,string focus,SetPieceKind kind)
    {
        _settings=settings;_focus=focus;_kind=kind;SetMeta("preview_focus",focus);
        SetMeta("preview_value",focus=="set_piece"?settings.SetPieces[kind].Routine??"":settings.Instructions.GetValueOrDefault(focus)??"");
        LayoutPitch();GetNodeOrNull<Control>("PreviewOverlay")?.QueueRedraw();
    }
    public override void _Ready(){var overlay=new PreviewOverlay(this);AddChild(overlay);overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);LayoutPitch();}
    private sealed partial class PreviewOverlay(TacticalPreview owner):Control
    {public override void _Ready(){Name="PreviewOverlay";MouseFilter=MouseFilterEnum.Ignore;}public override void _Draw()=>owner.Paint(this);}
    private void LayoutPitch()
    {
        var zoom=1f;var width=.5f;var advance=.5f;
        if(_tab==3&&_kind!=SetPieceKind.Penalty&&Enum.IsDefined(_kind)&&Size.Y>0)
        {
            var scene=TacticDiagramBuilder.Build(_lineup,_formation,_settings,_tab,_focus,_kind,true,Location);
            if(scene.Points.Count>0)
            {
                var min=scene.Points.Min(p=>p.Advance);var max=scene.Points.Max(p=>p.Advance);
                var baseHeight=FootballPitch.FieldRect(Size,true).Size.Y;
                zoom=Mathf.Clamp((Size.Y-50)/Mathf.Max(1,(float)(max-min+.14)*baseHeight),1,1.8f);
                advance=(float)((min+max)/2);
                width=(float)((scene.Points.Min(p=>p.Width)+scene.Points.Max(p=>p.Width))/2);
            }
        }
        _background.Size=Size*zoom;var field=FootballPitch.FieldRect(_background.Size,true);
        _background.Position=zoom==1?Vector2.Zero:Size/2-field.Position-field.Size*new Vector2(width,1-advance);

    }
    private void Paint(Control canvas)
    {
        if(_tab==3&&!Enum.IsDefined(_kind))return;
        var scene=TacticDiagramBuilder.Build(_lineup,_formation,_settings,_tab,_focus,_kind,true,Location);
        SetMeta("diagram_points",scene.Points.Count);SetMeta("diagram_arrows",scene.Arrows.Count);SetMeta("diagram_zones",scene.Zones.Count);
        SetMeta("diagram_marked",scene.Points.Count(p=>p.Marked));SetMeta("diagram_lying",scene.Points.Count(p=>p.Lying));
        var rect=FootballPitch.FieldRect(_background.Size,true);rect.Position+=_background.Position;Vector2 P(DiagramPoint p)=>rect.Position+rect.Size*new Vector2((float)p.Width,1-(float)p.Advance);
        var ink=new Color("f1b96b");var white=new Color("eef4f3");var font=ThemeDB.FallbackFont;const float radius=7;
        foreach(var zone in scene.Zones)
        {
            var a=rect.Position+rect.Size*new Vector2((float)zone.Width,1-(float)(zone.Advance+zone.Length));
            var box=new Rect2(a,rect.Size*new Vector2((float)zone.Breadth,(float)zone.Length));
            canvas.DrawRect(box,new Color(.3f,.65f,.8f,.22f));canvas.DrawRect(box,new Color("73b7d7"),false,1);
        }
        foreach(var arrow in scene.Arrows)
        {
            var a=P(scene.Points.Single(p=>p.Id==arrow.From));var b=P(scene.Points.Single(p=>p.Id==arrow.To));var direction=(b-a).Normalized();
            var from=a+direction*radius;var to=b-direction*radius;if(a.DistanceTo(b)<=radius*2+4)continue;
            var routeInk=arrow.Highlight?new Color("78d9b0"):ink;
            if(arrow.Short==true||arrow.Short is null&&arrow.Movement)canvas.DrawDashedLine(from,to,routeInk,2,5,true);else canvas.DrawLine(from,to,routeInk,2,true);
            canvas.DrawLine(to,to-direction.Rotated(.5f)*7,ink,2,true);canvas.DrawLine(to,to-direction.Rotated(-.5f)*7,ink,2,true);
        }
        var occupied=new List<Rect2>();
        foreach(var point in scene.Points)
        {
            var at=P(point);var color=point.Pending?new Color("96a5b8"):point.Symbol==DiagramSymbol.Opponent?new Color("e98b99"):white;
            if(point.Lying){canvas.DrawLine(at-new Vector2(10,0),at+new Vector2(10,0),color,5,true);}
            else if(point.Symbol!=DiagramSymbol.Player||point.Pending)canvas.DrawArc(at,radius,0,Mathf.Tau,24,color,2,true);
            else canvas.DrawCircle(at,radius,new Color("287852"));
            if(point.Marked)canvas.DrawArc(at,radius+3,0,Mathf.Tau,24,new Color("ef9aaf"),2,true);
            if(point.Id=="ball"){canvas.DrawCircle(at,3,new Color("f1b96b"));continue;}
            string label=(point.Pending?"? ":"")+UiText.Get(point.Caption);
            if(_tab==3&&point.Player is {} id)
            {
                var p=_players.SingleOrDefault(p=>p.Id==id);if(p is not null){var words=p.Name.Split(' ',StringSplitOptions.RemoveEmptyEntries);label=words[0][0]+". "+words[^1]+(point.Pending?" *":"");}
            }
            if(_tab==3&&point.Pending){var roleId=point.Id;label=(roleId.StartsWith("Zone",StringComparison.Ordinal)?"Z"+roleId[4..]:roleId.StartsWith("Marker",StringComparison.Ordinal)?"M"+roleId[6..]:roleId switch{"NearPost"=>"P1","FarPost"=>"P2","Rebound"=>"R","Prone"=>"TB","Receiver"=>"AP","StayBack"=>"ATR","Finisher"=>"REM","taker"=>"LAN",_=>label})+" ?";}
            if(point.Id.StartsWith("rival_wall_",StringComparison.Ordinal))label="R"+point.Id[11..];
            if(point.Id.StartsWith("penalty",StringComparison.Ordinal))label=(int.Parse(point.Id[7..])+1)+". "+label;
            if(point.Id.StartsWith("Wall",StringComparison.Ordinal))label="B"+point.Id[4..]+(point.Pending?"?":"");
            var size=_tab==3?11:10;var width=font.GetStringSize(label,fontSize:size).X;
            var origin=at+new Vector2(-width/2,point.Id.StartsWith("Wall",StringComparison.Ordinal)?-12:point.Lying?16:21);
            // Etiquetas compactas y reubicación local: nunca se recortan en los bordes ni se superponen entre sí.
            origin.X=Math.Clamp(origin.X,2,Math.Max(2,Size.X-width-2));origin.Y=Math.Clamp(origin.Y,14,Math.Max(14,Size.Y-4));
            foreach(var offset in new[]{0,-16,16,-32,32,-48,48}){var candidate=new Vector2(origin.X,Math.Clamp(origin.Y+offset,14,Math.Max(14,Size.Y-4)));var bounds=new Rect2(candidate-new Vector2(1,size),new Vector2(width+2,size+3));if(occupied.All(r=>!r.Intersects(bounds))){origin=candidate;break;}}
            occupied.Add(new Rect2(origin-new Vector2(1,size),new Vector2(width+2,size+3)));
            font.DrawString(canvas.GetCanvasItem(),origin,label,fontSize:size,modulate:color);
        }
    }
}
