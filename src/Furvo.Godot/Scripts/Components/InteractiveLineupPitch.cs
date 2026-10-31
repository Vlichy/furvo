using Furvo.Core.Lineups;
using Furvo.Core.Tactics;
using Furvo.Presentation;
using Godot;
namespace Furvo.Presentation.Components;
/// <summary>Círculo interactivo, activable con clic o teclado; hover no es necesario para completar una selección.</summary>
public partial class LineupPosition:LineupDragButton
{
    public bool Pinned {get;set;}
    private readonly Color _suitability;
    private readonly bool _vertical;
    private readonly Label _nameLabel;
    public void FitName(float width){if(!_vertical)return;_nameLabel.Size=new Vector2(width,14);_nameLabel.Position=new Vector2(18-width/2,30);}
    public LineupPosition(FormationSlot slot,string player,int compatibility=0,bool vertical=false)
    {
        _vertical=vertical;_suitability=new Color(compatibility switch{0=>"27754e",1=>"9a601f",_=>"a33644"});
        SetMeta("suitability_color",_suitability.ToHtml(false));
        Name="LineupSlot_"+slot.Id;SetMeta("compatibility",compatibility);SetMeta("slot_id",slot.Id);CustomMinimumSize=vertical?new Vector2(36,48):new Vector2(112,58);Size=CustomMinimumSize;TooltipText=UiText.Get(slot.TacticalCode is null?"Position_"+slot.Role:"TacticalRole_"+slot.TacticalCode)+" · "+player+" · "+UiText.Get("Suitability_"+compatibility);
        foreach(var state in new[]{"normal","hover","pressed","focus","disabled"})AddThemeStyleboxOverride(state,new StyleBoxEmpty());
        var role=new Label{Name="TacticalRoleLabel",Text=slot.TacticalCode??UiText.Get("PositionCode_"+slot.Role),HorizontalAlignment=HorizontalAlignment.Center,MouseFilter=MouseFilterEnum.Ignore,Position=new Vector2(0,vertical?1:5),Size=new Vector2(vertical?36:112,25)};
        role.AddThemeFontSizeOverride("font_size",13);AddChild(role);
        var words=player.Split(' ',StringSplitOptions.RemoveEmptyEntries);var shortName=words.Length>1?words[0][0]+". "+words[^1]:player;
        var name=new Label{Text=shortName,HorizontalAlignment=HorizontalAlignment.Center,MouseFilter=MouseFilterEnum.Ignore,Position=new Vector2(vertical?-16:0,vertical?30:38),Size=new Vector2(vertical?68:112,18),TextOverrunBehavior=TextServer.OverrunBehavior.TrimEllipsis};
        _nameLabel=name;name.AddThemeFontSizeOverride("font_size",vertical?11:12);name.AddThemeColorOverride("font_color",Colors.White);AddChild(name);
        MouseEntered+=QueueRedraw;MouseExited+=QueueRedraw;FocusEntered+=QueueRedraw;FocusExited+=QueueRedraw;
    }
    public override void _Draw()
    {
        base._Draw();var center=new Vector2(_vertical?18:56,_vertical?14:20);var radius=_vertical?16:19;DrawCircle(center,radius,_suitability);
        DrawArc(center,radius,0,Mathf.Tau,36,new Color(Pinned||HasFocus()?"f1b96b":"c5d9c7"),2,true);
    }
}
/// <summary>Reutiliza las líneas del campo existente y las coordenadas de datos de la formación, no posiciones sueltas de UI.</summary>
public partial class InteractiveLineupPitch:Control
{
    private readonly List<(FormationSlot Slot,LineupPosition Button)> _positions=[];
    private readonly bool _vertical;
    private readonly CareerLineup _state;
    public string DropScope {get;set;}="";
    public Func<string,double,double,bool>? CanMove {get;set;}
    public Action<string,double,double>? MovePlayer {get;set;}
    public FormationDefinition? Ghost {get;set;}
    private Vector2? _movePreview;
    private bool _moveValid;
    private sealed partial class EditorOverlay(InteractiveLineupPitch owner):Control
    {public override void _Draw()=>owner.PaintOverlay(this);}
    private EditorOverlay? _overlay;
    public InteractiveLineupPitch(CareerLineup state,IReadOnlyList<LineupPlayer> players,Action<string,bool> select,string? pinned,bool vertical=false)
    {
        _state=state;_vertical=vertical;Name="LineupPitch";SizeFlagsHorizontal=SizeFlags.ExpandFill;SizeFlagsVertical=SizeFlags.ExpandFill;CustomMinimumSize=vertical?new Vector2(250,260):new Vector2(480,310);
        var background=new FootballPitch(vertical);AddChild(background);background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach(var slot in state.Formation.Slots)
        {
            var member=state.Members.Single(m=>m.SlotId==slot.Id);var name=players.Single(p=>p.Id==member.PlayerId).Name;
            var circle=new LineupPosition(slot,name,LineupSelection.Compatibility(players.Single(p=>p.Id==member.PlayerId),slot.Role),vertical){Pinned=pinned==slot.Id};circle.SetMeta("player_id",member.PlayerId.ToString());circle.SetMeta("tactical_x",slot.X);circle.SetMeta("tactical_y",slot.Y);circle.SetMeta("tactical_role",slot.Role.ToString());var natural=players.Single(p=>p.Id==member.PlayerId);circle.SetMeta("natural_role",natural.Primary.ToString());circle.SetMeta("secondary_roles",natural.Secondary.Select(r=>r.ToString()).ToArray());AddChild(circle);_positions.Add((slot,circle));
            circle.MouseEntered+=()=>select(slot.Id,false);circle.FocusEntered+=()=>select(slot.Id,false);circle.Pressed+=()=>select(slot.Id,true);
        }
        _overlay=new EditorOverlay(this){MouseFilter=MouseFilterEnum.Ignore};AddChild(_overlay);_overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Resized+=Arrange;Ready+=Arrange;
    }
    private void Arrange()
    {
        var rect=FootballPitch.FieldRect(Size,_vertical);SetMeta("field_rect",rect);
        foreach(var (slot,button) in _positions)
        {
            button.Position=rect.Position+rect.Size*(_vertical?new Vector2((float)slot.Y,1-(float)slot.X):new Vector2((float)slot.X,(float)slot.Y))-(_vertical?new Vector2(18,14):new Vector2(56,20));
            if(_vertical)
            {
                var nearest=_positions.Where(p=>p.Slot.X==slot.X&&p.Slot.Id!=slot.Id).Select(p=>Math.Abs(p.Slot.Y-slot.Y)).DefaultIfEmpty(1).Min();
                button.FitName(Mathf.Clamp((float)nearest*rect.Size.X-4,30,108));
            }
        }
    }
    private string? Source(Variant data)
    {
        if(data.VariantType!=Variant.Type.Dictionary)return null;var d=data.AsGodotDictionary();
        return d.ContainsKey("scope")&&d["scope"].AsString()==DropScope&&d.ContainsKey("furvo_lineup_player")?d["furvo_lineup_player"].AsString():null;
    }
    private (double X,double Y) Coordinates(Vector2 at)
    {var rect=FootballPitch.FieldRect(Size,_vertical);return _vertical?(1-(at.Y-rect.Position.Y)/rect.Size.Y,(at.X-rect.Position.X)/rect.Size.X):((at.X-rect.Position.X)/rect.Size.X,(at.Y-rect.Position.Y)/rect.Size.Y);}
    public override bool _CanDropData(Vector2 at,Variant data)
    {
        var source=Source(data);var (x,y)=Coordinates(at);
        _moveValid=source is not null&&CanMove?.Invoke(source,x,y)==true;SetMeta("move_checked",true);SetMeta("move_source",source??"");SetMeta("move_x",x);SetMeta("move_y",y);SetMeta("move_valid",_moveValid);_movePreview=at;_overlay?.QueueRedraw();return _moveValid;
    }
    public override void _DropData(Vector2 at,Variant data)
    {var source=Source(data);var (x,y)=Coordinates(at);SetMeta("move_dropped",true);if(source is not null&&CanMove?.Invoke(source,x,y)==true)Callable.From(()=>MovePlayer?.Invoke(source,x,y)).CallDeferred();}
    public override void _Notification(int what){if(what==NotificationDragEnd){_movePreview=null;_overlay?.QueueRedraw();}}
    private void PaintOverlay(Control canvas)
    {
        var rect=FootballPitch.FieldRect(Size,_vertical);
        if(Ghost is not null)foreach(var slot in Ghost.Slots)
        {
            var at=rect.Position+rect.Size*(_vertical?new Vector2((float)slot.Y,1-(float)slot.X):new Vector2((float)slot.X,(float)slot.Y));
            canvas.DrawArc(at,16,0,Mathf.Tau,32,new Color(.8f,.9f,1,.28f),2,true);
        }
        if(_movePreview is {} target&&GetViewport().GuiIsDragging())
        {canvas.DrawCircle(target,16,new Color(_moveValid?.2f:.8f,_moveValid?.8f:.2f,.3f,.5f));canvas.DrawArc(target,16,0,Mathf.Tau,32,new Color(_moveValid?"68dfac":"ec6879"),2,true);}
    }
    public void Pin(string? slot)
    {foreach(var (position,button) in _positions){button.Pinned=position.Id==slot;button.QueueRedraw();}}
}
