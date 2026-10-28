using Furvo.Core.Queries;
using Godot;
namespace Furvo.Presentation.Components;
public enum SportingIndicatorKind { Quality,Physical,Rhythm,Morale,Form }
public sealed record SportingIndicatorModel(SportingIndicatorKind Kind,PlayerSportingStatus Status);
/// <summary>Componente gráfico reutilizable; el mismo pintor se utiliza en controles independientes y celdas Tree.</summary>
public partial class SportingIndicator:Control
{
    private SportingIndicatorModel _model=new(SportingIndicatorKind.Quality,new());
    public SportingIndicatorModel Model {get=>_model;set{_model=value??throw new ArgumentNullException(nameof(value));TooltipText=Describe(value);QueueRedraw();}}
    public static string Describe(SportingIndicatorModel model)
    {
        if(model.Kind==SportingIndicatorKind.Morale && model.Status.Morale is {} morale)return UiText.Get("Morale_"+morale);
        if(model.Kind==SportingIndicatorKind.Form)
        {
            var matches=model.Status.LastFive().Where(m=>m is not null).Select(m=>UiText.Format("IndicatorAppearance",m!.Date.ToString("dd/MM/yyyy"),m.Rating?.ToString("0.00",System.Globalization.CultureInfo.GetCultureInfo("es-ES"))??UiText.Get("PendingDefinition"))).ToArray();
            return matches.Length==0?UiText.Get("IndicatorNoData_Form"):string.Join("\n",matches);
        }
        var value=model.Kind switch{SportingIndicatorKind.Quality=>model.Status.Quality,SportingIndicatorKind.Physical=>model.Status.Physical,_=>model.Status.Rhythm};
        if(value.Percentage is {} percent)return UiText.Format("IndicatorPercentage",percent);
        if(value.Segments is {} segments)return UiText.Format("IndicatorSegments",segments);
        return UiText.Get("IndicatorNoData_"+model.Kind);
    }
    public override void _Draw()=>Paint(GetCanvasItem(),new Rect2(Vector2.Zero,Size),Model);
    public const string InactiveColor="3c4b62";
    public static void Paint(Rid canvas,Rect2 rect,SportingIndicatorModel model)
    {
        var font=ThemeDB.FallbackFont;const int size=13;var x=rect.Position.X+6;var y=rect.GetCenter().Y;
        void Text(string text,float at,Color color)=>font.DrawString(canvas,new Vector2(at,y+font.GetAscent(size)/2-2),text,fontSize:size,modulate:color);
        Color Tone(SportingTone tone)=>new(tone switch{SportingTone.Positive=>"36b98a",SportingTone.Intermediate=>"edb151",SportingTone.Negative=>"db6878",_=>InactiveColor});
        if(model.Kind==SportingIndicatorKind.Form)
        {
            x=rect.GetCenter().X-39;
            var matches=model.Status.LastFive();
            for(var i=0;i<5;i++)RenderingServer.CanvasItemAddCircle(canvas,new Vector2(x+5+i*17,y),5,matches[i]?.Performance is {} tone?Tone(tone):new Color(InactiveColor));
            return;
        }
        if(model.Kind==SportingIndicatorKind.Morale)
        {
            var morale=model.Status.Morale;var color=morale switch{PlayerMorale.VeryHigh or PlayerMorale.High=>Tone(SportingTone.Positive),PlayerMorale.Low or PlayerMorale.VeryLow=>Tone(SportingTone.Negative),PlayerMorale.Normal=>new Color("b1a2c9"),_=>new Color(InactiveColor)};
            var moralText=morale is {} state?UiText.Get("Morale_"+state):UiText.Get("IndicatorNoDataShort");x=rect.GetCenter().X-(16+font.GetStringSize(moralText,fontSize:size).X)/2;
            RenderingServer.CanvasItemAddCircle(canvas,new Vector2(x+5,y),5,color);Text(morale is {} value?UiText.Get("Morale_"+value):UiText.Get("IndicatorNoDataShort"),x+16,new Color("dce5f0"));return;
        }
        var status=model.Kind switch{SportingIndicatorKind.Quality=>model.Status.Quality,SportingIndicatorKind.Physical=>model.Status.Physical,_=>model.Status.Rhythm};
        var active=model.Kind switch{SportingIndicatorKind.Quality=>new Color("efb65c"),SportingIndicatorKind.Rhythm=>new Color("7dbfe5"),_=>Tone(status.Tone)};
        var caption=status.Percentage is {} percent?percent+" %":status.Segments is null?"—":"";
        x=rect.GetCenter().X-(caption.Length>0?58+font.GetStringSize(caption,fontSize:size).X:52)/2;
        for(var i=0;i<5;i++)RenderingServer.CanvasItemAddRect(canvas,new Rect2(new Vector2(x+i*11,y-7),new Vector2(8,14)),i<status.Segments?active:new Color(InactiveColor));
        if(caption.Length>0)Text(caption,x+58,new Color("dce5f0"));
    }
}
