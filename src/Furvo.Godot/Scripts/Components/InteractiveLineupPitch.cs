using Furvo.Core.Lineups;
using Furvo.Presentation;
using Godot;
namespace Furvo.Presentation.Components;
/// <summary>Círculo interactivo, activable con clic o teclado; hover no es necesario para completar una selección.</summary>
public partial class LineupPosition:Button
{
    public bool Pinned {get;set;}
    public LineupPosition(FormationSlot slot,string player)
    {
        Name="LineupSlot_"+slot.Id;SetMeta("slot_id",slot.Id);CustomMinimumSize=new Vector2(112,58);Size=CustomMinimumSize;TooltipText=UiText.Get("Position_"+slot.Role)+" · "+player;
        foreach(var state in new[]{"normal","hover","pressed","focus","disabled"})AddThemeStyleboxOverride(state,new StyleBoxEmpty());
        var role=new Label{Text=UiText.Get("PositionCode_"+slot.Role),HorizontalAlignment=HorizontalAlignment.Center,MouseFilter=MouseFilterEnum.Ignore,Position=new Vector2(0,5),Size=new Vector2(112,25)};
        role.AddThemeFontSizeOverride("font_size",13);AddChild(role);
        var words=player.Split(' ',StringSplitOptions.RemoveEmptyEntries);var shortName=words.Length>1?words[0][0]+". "+words[^1]:player;
        var name=new Label{Text=shortName,HorizontalAlignment=HorizontalAlignment.Center,MouseFilter=MouseFilterEnum.Ignore,Position=new Vector2(0,38),Size=new Vector2(112,18),TextOverrunBehavior=TextServer.OverrunBehavior.TrimEllipsis};
        name.AddThemeFontSizeOverride("font_size",12);AddChild(name);
        MouseEntered+=QueueRedraw;MouseExited+=QueueRedraw;FocusEntered+=QueueRedraw;FocusExited+=QueueRedraw;
    }
    public override void _Draw()
    {
        var center=new Vector2(56,20);DrawCircle(center,19,new Color(Pinned?"b87428":IsHovered()?"406650":"183d34"));
        DrawArc(center,19,0,Mathf.Tau,36,new Color(Pinned||HasFocus()?"f1b96b":"c5d9c7"),2,true);
    }
}
/// <summary>Reutiliza las líneas del campo existente y las coordenadas de datos de la formación, no posiciones sueltas de UI.</summary>
public partial class InteractiveLineupPitch:Control
{
    private readonly List<(FormationSlot Slot,LineupPosition Button)> _positions=[];
    public InteractiveLineupPitch(CareerLineup state,IReadOnlyList<LineupPlayer> players,Action<string,bool> select,string? pinned)
    {
        Name="LineupPitch";SizeFlagsHorizontal=SizeFlags.ExpandFill;SizeFlagsVertical=SizeFlags.ExpandFill;CustomMinimumSize=new Vector2(480,310);
        var background=new FootballPitch();AddChild(background);background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach(var slot in state.Formation.Slots)
        {
            var member=state.Members.Single(m=>m.SlotId==slot.Id);var name=players.Single(p=>p.Id==member.PlayerId).Name;
            var circle=new LineupPosition(slot,name){Pinned=pinned==slot.Id};circle.SetMeta("player_id",member.PlayerId.ToString());AddChild(circle);_positions.Add((slot,circle));
            circle.MouseEntered+=()=>select(slot.Id,false);circle.FocusEntered+=()=>select(slot.Id,false);circle.Pressed+=()=>select(slot.Id,true);
        }
        Resized+=Arrange;Ready+=Arrange;
    }
    private void Arrange()
    {
        var rect=FootballPitch.FieldRect(Size);
        foreach(var (slot,button) in _positions)button.Position=rect.Position+rect.Size*new Vector2((float)slot.X,(float)slot.Y)-new Vector2(56,20);
    }
    public void Pin(string? slot)
    {foreach(var (position,button) in _positions){button.Pinned=position.Id==slot;button.QueueRedraw();}}
}
