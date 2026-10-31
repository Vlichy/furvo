using Godot;
namespace Furvo.Presentation.Components;
/// <summary>Drag and drop nativo de Godot. El payload solo identifica jugador/revisión; nunca escribe antes de soltar.</summary>
public partial class LineupDragButton:Button
{
    public string PlayerKey {get;set;}="";
    public string Scope {get;set;}="";
    public Func<string,bool>? AcceptPlayer {get;set;}
    public Action<string>? ReceivePlayer {get;set;}
    private bool? _dropHint;
    public LineupDragButton(){MouseExited+=()=>{_dropHint=null;QueueRedraw();};}
    public override Variant _GetDragData(Vector2 position)
    {
        if(string.IsNullOrEmpty(PlayerKey))return default;
        var preview=new PanelContainer{Modulate=new Color(1,1,1,.72f)};
        preview.AddChild(new Label{Text=TooltipText,MouseFilter=MouseFilterEnum.Ignore});
        SetDragPreview(preview);SetMeta("drag_started",true);
        return new Godot.Collections.Dictionary<string,Variant>{{"furvo_lineup_player",PlayerKey},{"scope",Scope}};
    }
    private string? Source(Variant data)
    {
        if(data.VariantType!=Variant.Type.Dictionary)return null;
        var d=data.AsGodotDictionary();
        return d.ContainsKey("scope")&&d["scope"].AsString()==Scope&&d.ContainsKey("furvo_lineup_player")?d["furvo_lineup_player"].AsString():null;
    }
    public override bool _CanDropData(Vector2 position,Variant data)
    {var source=Source(data);var valid=source is not null&&AcceptPlayer?.Invoke(source)==true;_dropHint=valid;QueueRedraw();return valid;}
    public override void _DropData(Vector2 position,Variant data)
    {
        var source=Source(data);if(source is null||AcceptPlayer?.Invoke(source)!=true)return;
        // Se difiere la reconstrucción visual para no eliminar controles dentro de la entrega nativa del drop.
        Callable.From(()=>ReceivePlayer?.Invoke(source)).CallDeferred();
    }
    public override void _Notification(int what){if(what==NotificationDragEnd){_dropHint=null;QueueRedraw();}}
    public override void _Input(InputEvent e)
    {
        if(e is InputEventKey{Pressed:true,Keycode:Key.Escape}&&GetViewport().GuiIsDragging())
        {GetViewport().GuiCancelDrag();GetViewport().SetInputAsHandled();}
    }
    public override void _Draw()
    {
        if(_dropHint is {} valid&&GetViewport().GuiIsDragging())
            DrawRect(new Rect2(Vector2.Zero,Size),new Color(valid?"63d59b":"ec6879"),false,3);
    }
}
