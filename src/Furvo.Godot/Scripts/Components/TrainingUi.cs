using Godot;
using Furvo.Core.Development;
namespace Furvo.Presentation.Components;
/// <summary>Presentación de Entrenamiento; no modifica selecciones ni reglas deportivas.</summary>
internal static class TrainingUi
{
    internal static string PlayerCaption(TrainingCandidate player)=>player.Name+" — "+(player.Positions is {} positions?UiText.Get("Position_"+positions.Primary):UiText.Get("Unknown"));
    internal static void Tab(Button button,bool active)
    {
        var ink=new Color("f1b96b");
        StyleBoxFlat Box(string color)=>new(){BgColor=new Color(color),BorderColor=active?ink:new Color("40556d"),BorderWidthBottom=3,CornerRadiusTopLeft=4,CornerRadiusTopRight=4,CornerRadiusBottomLeft=4,CornerRadiusBottomRight=4,ContentMarginLeft=16,ContentMarginRight=16,ContentMarginTop=7,ContentMarginBottom=7};
        button.AddThemeStyleboxOverride("normal",Box(active?"30445c":"203249"));
        button.AddThemeStyleboxOverride("hover",Box(active?"3a506a":"2a4059"));
        button.AddThemeStyleboxOverride("pressed",Box(active?"30445c":"182b43"));
        button.AddThemeColorOverride("font_color",active?ink:new Color("e5ecf5"));
        button.AddThemeColorOverride("font_hover_color",active?ink:new Color("e5ecf5"));
        button.AddThemeColorOverride("font_pressed_color",active?ink:new Color("e5ecf5"));
        var bold=new FontVariation{BaseFont=button.GetThemeFont("font"),VariationEmbolden=.7f};
        var fontSize=button.GetThemeFontSize("font_size");
        // Se reserva también el ancho de la fuente destacada para evitar saltos al navegar.
        button.CustomMinimumSize=new Vector2(Mathf.Ceil(bold.GetStringSize(button.Text,fontSize:fontSize).X)+36,44);
        if(active)button.AddThemeFontOverride("font",bold);
        button.SetMeta("training_active",active);
    }
    internal static void LimitPopup(OptionButton control,int rows=7)
    {
        // Godot requiere ambos límites no nulos; con anchura cero ignora la altura máxima.
        var popup=control.GetPopup();popup.PreferNativeMenu=false;popup.WrapControls=true;
        void Size(){var row=popup.GetThemeFont("font").GetHeight(popup.GetThemeFontSize("font_size"))+popup.GetThemeConstant("v_separation");var height=Mathf.CeilToInt(row*rows)+20;popup.MaxSize=new Vector2I(Mathf.Clamp(Mathf.CeilToInt(control.Size.X),160,720),height);popup.Size=new Vector2I(popup.MaxSize.X,height);}
        control.Ready+=Size;popup.AboutToPopup+=Size;
    }
    internal static void AddPlayers(ItemList list,IReadOnlyList<TrainingCandidate> players)
    {
        foreach(var player in players){list.AddItem(PlayerCaption(player));list.SetItemMetadata(list.ItemCount-1,player.Id.ToString());}
    }
}
