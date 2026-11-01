using Furvo.Core.Queries;
using Godot;
namespace Furvo.Presentation.Components;
/// <summary>Una fila pública con números y barras 1–20. El color solo complementa el dato numérico.</summary>
public partial class AnalysisAttributeRow : HBoxContainer
{
    public AnalysisAttributeRow(AttributeAnalysisRow value)
    {
        Name="AnalysisAttribute_"+value.Code;SetMeta("analysis_attribute",value.Code);
        SizeFlagsHorizontal=SizeFlags.ExpandFill;CustomMinimumSize=new Vector2(0,30);AddThemeConstantOverride("separation",12);
        var label=ClubUi.Text("AttributeName",UiText.Get("Attribute_"+value.Code),14);
        label.SizeFlagsStretchRatio=2;AddChild(label);
        AddChild(Meter(value.Value,"Selected"));
        if(value.ComparisonValue is {} other)AddChild(Meter(other,"Comparison"));
        TooltipText=value.Relevant?UiText.Get("AnalysisRelevantAttribute"):UiText.Get("AnalysisOtherAttribute");
    }
    private static HBoxContainer Meter(int value,string name)
    {
        var body=new HBoxContainer{Name=name,SizeFlagsHorizontal=SizeFlags.ExpandFill,SizeFlagsStretchRatio=2};body.AddThemeConstantOverride("separation",8);
        var bar=new ProgressBar{Name="Bar",MinValue=0,MaxValue=20,Value=value,ShowPercentage=false,MouseFilter=MouseFilterEnum.Ignore,
            SizeFlagsHorizontal=SizeFlags.ExpandFill,SizeFlagsVertical=SizeFlags.ShrinkCenter,CustomMinimumSize=new Vector2(0,12)};
        var background=new StyleBoxFlat{BgColor=new Color("263950")};background.SetCornerRadiusAll(3);bar.AddThemeStyleboxOverride("background",background);
        var fill=new StyleBoxFlat{BgColor=new Color(AttributeValueStyle.ColorHex(value))};fill.SetCornerRadiusAll(3);bar.AddThemeStyleboxOverride("fill",fill);
        body.AddChild(bar);body.AddChild(new Label{Name="Number",Text=value.ToString()+" / 20",HorizontalAlignment=HorizontalAlignment.Right,CustomMinimumSize=new Vector2(60,0)});
        return body;
    }
}
