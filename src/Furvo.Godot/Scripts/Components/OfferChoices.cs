using Godot;
namespace Furvo.Presentation.Components;
public sealed record OfferChoice(int Value,string Title,string? Description=null,bool Previous=false);
/// <summary>Selección exclusiva accesible por teclado, con marca textual y color; nunca depende solo del color.</summary>
public partial class OfferChoices : VBoxContainer
{
    private readonly List<(Button Button,OfferChoice Choice)> _buttons=new();
    public int SelectedValue {get;private set;}
    public event Action? Changed;
    public OfferChoices(string name,IEnumerable<OfferChoice> options,int selected,bool cards=false)
    {
        Name=name;SelectedValue=selected;var values=options.ToArray();
        // Los grupos breves conservan su ancho natural dentro de filas adaptables, sin apilar cada botón.
        if(!cards)CustomMinimumSize=new Vector2(values.Length*120+Math.Max(0,values.Length-1)*10,0);
        var flow=new HFlowContainer();flow.AddThemeConstantOverride("h_separation",10);flow.AddThemeConstantOverride("v_separation",8);AddChild(flow);
        var group=new ButtonGroup{AllowUnpress=false};
        foreach(var choice in values){
            var button=new Button{Name=name+"_"+choice.Value,ToggleMode=true,ButtonGroup=group,Alignment=HorizontalAlignment.Left,AutowrapMode=cards?TextServer.AutowrapMode.WordSmart:TextServer.AutowrapMode.Off,CustomMinimumSize=new Vector2(cards?350:120,cards?86:40),SizeFlagsHorizontal=SizeFlags.ShrinkBegin};
            button.SetMeta("offer_choice_value",choice.Value);button.SetMeta("offer_choice_group",name);button.TooltipText=choice.Description??choice.Title;flow.AddChild(button);_buttons.Add((button,choice));
            button.Pressed+=()=>{if(SelectedValue==choice.Value)return;SelectedValue=choice.Value;Refresh();Changed?.Invoke();};
        }
        Refresh();
    }
    private void Refresh()
    {
        foreach(var (button,choice) in _buttons){
            var active=choice.Value==SelectedValue;button.SetPressedNoSignal(active);button.Text=(active?"● ":"○ ")+choice.Title+(choice.Previous?" · "+UiText.Get("OfferPreviousValue"):"")+(choice.Description is {} description?((char)10)+description:"");
            StyleBoxFlat Box(string background){var style=new StyleBoxFlat{BgColor=new Color(background),BorderColor=new Color(active?"f1b96b":"40556d"),ContentMarginLeft=12,ContentMarginRight=12,ContentMarginTop=8,ContentMarginBottom=8};style.SetBorderWidthAll(active?2:1);style.SetCornerRadiusAll(4);return style;}
            button.AddThemeStyleboxOverride("normal",Box(active?"30445c":"203249"));button.AddThemeStyleboxOverride("pressed",Box("30445c"));button.AddThemeStyleboxOverride("hover",Box("3a506a"));button.AddThemeColorOverride("font_color",new Color(active?"f1b96b":"e5ecf5"));
        }
    }
}
