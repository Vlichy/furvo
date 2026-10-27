using System.Globalization;
using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Bloques compactos compartidos de las pestañas de club; distingue datos ausentes de cifras cero.</summary>
internal static class ClubUi
{
    internal static string Value(object? value) => value is null ? UiText.Get("PendingDefinition") :
        value is IFormattable format ? format.ToString(null, CultureInfo.InvariantCulture) : value.ToString()!;
    internal static Label Text(string name, string text, int size = 15, string color = "e5ecf5")
    {
        var label = new Label { Name = name, Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        label.AddThemeFontSizeOverride("font_size", size); label.AddThemeColorOverride("font_color", new Color(color));
        return label;
    }
    internal static VBoxContainer Card(Control parent, string title)
    {
        var panel = new PanelContainer { Name = title + "Panel", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var style = new StyleBoxFlat { BgColor = new Color("14243a"), BorderColor = new Color("2c4058"),
            ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 10, ContentMarginBottom = 10 };
        style.SetBorderWidthAll(1); style.SetCornerRadiusAll(5); panel.AddThemeStyleboxOverride("panel", style);
        parent.AddChild(panel); var body = new VBoxContainer(); body.AddThemeConstantOverride("separation", 4); panel.AddChild(body);
        body.AddChild(Text(title + "Heading", UiText.Get(title), 16, "f1b96b")); return body;
    }
    internal static void Field(Control parent, string key, object? value, bool unavailable = false)
    {
        var row = new HBoxContainer(); row.AddThemeConstantOverride("separation", 12); parent.AddChild(row);
        row.AddChild(Text(key + "Label", UiText.Get(key), 12, "a6b6ca"));
        var display = Text(key + "Value", unavailable ? UiText.Get("NotAvailable") : Value(value), 12);
        if (unavailable) display.SetMeta("economic_unavailable", true);
        row.AddChild(display);
    }
    internal static VBoxContainer ScrollBody(Control parent, string name)
    {
        var scroll = new ScrollContainer { Name = name, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        parent.AddChild(scroll); var body = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 12); scroll.AddChild(body); return body;
    }
    internal static BoxContainer ResponsiveRow(Control parent, string name)
    {
        var row = new BoxContainer { Name = name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 10); parent.AddChild(row);
        row.Resized += () => { var vertical = row.Size.X < 1000; if (row.Vertical != vertical) row.Vertical = vertical; };
        return row;
    }
    internal static GridContainer ResponsiveColumns(Control parent, float breakpoint = 1000)
    {
        var grid = new GridContainer { Columns = 2, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation", 12); grid.AddThemeConstantOverride("v_separation", 12);
        parent.AddChild(grid); grid.Resized += () => { var count = grid.Size.X < breakpoint ? 1 : 2; if (grid.Columns != count) grid.Columns = count; };
        return grid;
    }
}
