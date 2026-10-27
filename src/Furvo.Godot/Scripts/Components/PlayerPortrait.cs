using Furvo.Core.Domain;
using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Retrato reutilizable 3:4, con recursos locales por PlayerId y silueta provisional cuando no hay imagen individual.</summary>
public partial class PlayerPortrait : PanelContainer
{
    private readonly TextureRect _image;
    private readonly Label _fallback;
    public PlayerPortrait()
    {
        Name = "PlayerPortrait";
        CustomMinimumSize = new Vector2(78, 104);
        SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        SizeFlagsVertical = SizeFlags.ShrinkBegin;
        var frame = new StyleBoxFlat { BgColor = new Color("14243a"), BorderColor = new Color("44556b"),
            ContentMarginLeft = 2, ContentMarginRight = 2, ContentMarginTop = 2, ContentMarginBottom = 2 };
        frame.SetBorderWidthAll(1);
        frame.SetCornerRadiusAll(6);
        AddThemeStyleboxOverride("panel", frame);
        _image = new TextureRect { Name = "PortraitImage", ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(74, 100), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_image);
        _fallback = new Label { Text = UiText.Get("PortraitUnavailable"), Visible = false,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_fallback);
    }

    /// <summary>Solo busca bajo res:// con nombres derivados de GUID; ante ausencia o recurso inválido conserva el avatar.</summary>
    public void SetPlayer(PlayerId id)
    {
        var candidates = PortraitResourcePaths.ForPlayer(id);
        Texture2D? texture = null;
        foreach (var resource in candidates)
        {
            texture = TryLoad(resource);
            if (texture is not null) break;
        }
        var individual = texture is not null;
        _image.Texture = texture ?? TryLoad(PortraitResourcePaths.Placeholder);
        TooltipText = UiText.Get(individual ? "PortraitIndividual" : "PortraitPlaceholder");
        // Si faltase también el recurso instalado, un marcador textual mantiene la ficha utilizable sin excepciones.
        _fallback.Visible = _image.Texture is null;
    }

    private static Texture2D? TryLoad(string resource)
    {
        try
        {
            if (!ResourceLoader.Exists(resource, "Texture2D")) return null;
            var texture = ResourceLoader.Load<Texture2D>(resource);
            return texture is not null && texture.GetWidth() is > 0 and <= 4096 && texture.GetHeight() is > 0 and <= 4096
                ? texture : null;
        }
        catch (Exception error)
        {
            GD.PushWarning("No se pudo cargar un recurso de retrato: " + error.GetType().Name);
            return null;
        }
    }
}
