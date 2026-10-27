using System.Globalization;
using Furvo.Core.Queries;
using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Fortalezas y debilidades descriptivas de la posición principal, sin valoración global ni atributos ocultos.</summary>
public partial class PlayerInsightsPanel : PanelContainer
{
    public PlayerInsightsPanel(PlayerDetails details)
    {
        Name = "InsightsPanel";
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        var style = new StyleBoxFlat { BgColor = new Color("17283d"), BorderColor = new Color("34465c"),
            ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 10, ContentMarginBottom = 10 };
        style.SetCornerRadiusAll(5); style.SetBorderWidthAll(1);
        AddThemeStyleboxOverride("panel", style);
        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 10);
        AddChild(body);
        body.AddChild(Title("InsightsHeading", 17));
        var selection = PositionInsightRules.Describe(details.Player.Position, details.AttributeGroups);
        var sections = new GridContainer { Columns = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        sections.AddThemeConstantOverride("h_separation", 16);
        body.AddChild(sections);
        sections.AddChild(Section("strength", "StrengthsHeading", "NoStrengths", selection.Strengths));
        sections.AddChild(Section("weakness", "WeaknessesHeading", "NoWeaknesses", selection.Weaknesses));
    }

    private static VBoxContainer Section(string kind, string heading, string emptyKey, IReadOnlyList<VisibleAttribute> values)
    {
        var section = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        section.AddThemeConstantOverride("separation", 8);
        section.AddChild(Title(heading, 15));
        if (values.Count == 0)
        {
            var empty = new Label { Text = UiText.Get(emptyKey), AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = SizeFlags.ExpandFill };
            empty.AddThemeFontSizeOverride("font_size", 14);
            empty.AddThemeColorOverride("font_color", new Color("a6b6ca"));
            section.AddChild(empty);
        }
        foreach (var attribute in values)
        {
            var row = new HBoxContainer { Name = kind + "_" + attribute.Code, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            row.SetMeta("insight_code", attribute.Code);
            row.SetMeta("insight_kind", kind);
            row.SetMeta("insight_value", attribute.Value);
            var name = new Label { Text = UiText.Get("Attribute_" + attribute.Code), AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = SizeFlags.ExpandFill };
            name.AddThemeFontSizeOverride("font_size", 14);
            var value = new Label { Text = attribute.Value.ToString(CultureInfo.InvariantCulture),
                HorizontalAlignment = HorizontalAlignment.Right, CustomMinimumSize = new Vector2(25, 0) };
            value.AddThemeFontSizeOverride("font_size", 15);
            value.AddThemeColorOverride("font_color", new Color(AttributeValueStyle.ColorHex(attribute.Value)));
            row.AddChild(name); row.AddChild(value); section.AddChild(row);
        }
        return section;
    }

    private static Label Title(string key, int size)
    {
        var label = new Label { Text = UiText.Get(key) };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", new Color("f1b96b"));
        return label;
    }
}
