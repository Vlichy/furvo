using Godot;
namespace Furvo.Presentation.Components;
/// <summary>Indicador y texto separados: los márgenes del tema no pueden superponerlos.</summary>
public partial class PriorityChoice : HBoxContainer
{
    public CheckBox Indicator { get; } = new(){CustomMinimumSize=new Vector2(44,36)};
    public bool Checked { get=>Indicator.ButtonPressed; set=>Indicator.ButtonPressed=value; }
    public PriorityChoice(string text,bool selected=false)
    {
        SizeFlagsHorizontal=SizeFlags.ExpandFill;AddThemeConstantOverride("separation",12);
        AddChild(Indicator);var label=new Label{Text=text,AutowrapMode=TextServer.AutowrapMode.WordSmart,SizeFlagsHorizontal=SizeFlags.ExpandFill,MouseFilter=MouseFilterEnum.Stop};
        AddChild(label);Checked=selected;Indicator.TooltipText=text;
        label.GuiInput+=e=>{if(e is InputEventMouseButton{Pressed:true,ButtonIndex:MouseButton.Left}&&!Indicator.Disabled){Checked=!Checked;Indicator.GrabFocus();AcceptEvent();}};
    }
}
