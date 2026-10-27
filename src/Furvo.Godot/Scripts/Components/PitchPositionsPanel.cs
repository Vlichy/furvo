using Furvo.Core.Domain;
using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Panel de campo y leyenda; el campo se mantiene independiente para su reutilización en otras pantallas.</summary>
public partial class PitchPositionsPanel : PanelContainer
{
    public PitchPositionsPanel(PlayerRole? primary, IReadOnlyList<PlayerRole> secondary)
    {
        Name = "PositionsPanel";
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        var style = new StyleBoxFlat { BgColor = new Color("14243a"), BorderColor = new Color("2c4058"),
            ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 10, ContentMarginBottom = 10 };
        style.SetCornerRadiusAll(5); style.SetBorderWidthAll(1);
        AddThemeStyleboxOverride("panel", style);
        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 6);
        AddChild(content);
        var title = new Label { Text = UiText.Get("PitchHeading") };
        title.AddThemeFontSizeOverride("font_size", 17);
        title.AddThemeColorOverride("font_color", new Color("f1b96b"));
        content.AddChild(title);
        var pitch = new PositionPitch();
        pitch.SetPositions(primary, secondary);
        content.AddChild(pitch);
        var legend = new HBoxContainer();
        legend.AddThemeConstantOverride("separation", 14);
        content.AddChild(legend);
        legend.AddChild(Legend("PitchLegendPrimary", "f1b96b"));
        legend.AddChild(Legend("PitchLegendSecondary", "8ccce2"));
        var direction = Legend("PitchAttackDirection", "a6b6ca");
        direction.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        direction.HorizontalAlignment = HorizontalAlignment.Right;
        legend.AddChild(direction);
    }

    private static Label Legend(string key, string color)
    {
        var label = new Label { Text = UiText.Get(key) };
        label.AddThemeFontSizeOverride("font_size", 13);
        label.AddThemeColorOverride("font_color", new Color(color));
        return label;
    }
}
