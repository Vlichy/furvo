using System.Globalization;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Presentación compacta de la ficha pública; recibe solo PlayerDetails y no consulta ni modifica el mundo.</summary>
public partial class PlayerSheet : VBoxContainer
{
    public PlayerSheet(PlayerDetails details,Control? supplementary=null,PlayerContractInfo? contract=null)
    {
        Name = "PlayerSheet";
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        AddThemeConstantOverride("separation", 10);
        AddChild(BuildHeader(details));
        var overview = CreateScroll("AttributesOverview");
        AddChild(overview);
        var columns = new GridContainer { Name = "AttributeColumns", Columns = 3, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        columns.AddThemeConstantOverride("h_separation", 12);
        var content = new VBoxContainer { Name = "PlayerScrollableContent", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        content.AddThemeConstantOverride("separation", 14);
        overview.AddChild(content);
        if(contract is not null)content.AddChild(new PlayerContractPanel(contract));
        if(supplementary is not null)content.AddChild(supplementary);
        content.AddChild(columns);
        columns.AddChild(new AttributeSection(details.AttributeGroups.Single(g => g.Category == AttributeCategory.Technical)));
        columns.AddChild(new AttributeSection(details.AttributeGroups.Single(g => g.Category == AttributeCategory.Mental)));
        var thirdColumn = new VBoxContainer { Name = "PhysicalAndGoalkeeper", SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin };
        thirdColumn.AddThemeConstantOverride("separation", 10);
        columns.AddChild(thirdColumn);
        foreach (var category in new[] { AttributeCategory.Physical, AttributeCategory.Goalkeeper })
            thirdColumn.AddChild(new AttributeSection(details.AttributeGroups.Single(g => g.Category == category)));
        // Los paneles nuevos comparten el desplazamiento de atributos, sin mover cabecera ni botón de regreso.
        var lower = new GridContainer { Name = "PlayerExtraPanels", Columns = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        lower.AddThemeConstantOverride("h_separation", 12);
        lower.AddThemeConstantOverride("v_separation", 12);
        content.AddChild(lower);
        lower.AddChild(new PitchPositionsPanel(details.Player.Position, details.SecondaryPositions));
        lower.AddChild(new PlayerInsightsPanel(details));
        lower.Resized += () =>
        {
            var count = lower.Size.X < 1000 ? 1 : 2;
            if (lower.Columns != count) lower.Columns = count;
        };
    }

    private static ScrollContainer CreateScroll(string name) => new()
    {
        Name = name, SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill,
        HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true
    };

    private static PanelContainer BuildHeader(PlayerDetails details)
    {
        var header = new PanelContainer { Name = "PlayerHeader", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var style = new StyleBoxFlat { BgColor = new Color("17283d"), BorderColor = new Color("34465c"),
            ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 8, ContentMarginBottom = 8 };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(5);
        header.AddThemeStyleboxOverride("panel", style);
        var line = new HBoxContainer();
        line.AddThemeConstantOverride("separation", 16);
        header.AddChild(line);
        var portrait = new PlayerPortrait();
        portrait.SetPlayer(details.Player.Id);
        line.AddChild(portrait);
        var identity = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        identity.AddThemeConstantOverride("separation", 2);
        line.AddChild(identity);
        identity.AddChild(Text("PlayerName", details.Player.Name, 25, "e5ecf5"));
        identity.AddChild(Text("PlayerClub", details.Club?.Name??UiText.Get("NotAvailable"), 15, "f1b96b"));
        var facts = new GridContainer { Columns = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        facts.AddThemeConstantOverride("h_separation", 20);
        facts.AddThemeConstantOverride("v_separation", 2);
        identity.AddChild(facts);
        facts.AddChild(Text("PlayerPosition", UiText.Format("PlayerPrimaryLine",
            UiText.Position(details.Player.Position, details.Player.Group)), 14, "dce5f0"));
        facts.AddChild(Text("PlayerNationalityAge", UiText.Format("PlayerNationalityAge",
            details.Player.Nationality ?? UiText.Get("Unknown"),
            details.Player.Age?.ToString(CultureInfo.InvariantCulture) ?? UiText.Get("Unknown")), 14, "a6b6ca"));
        var secondary = details.SecondaryPositions.Count == 0 ? UiText.Get("None") :
            string.Join(UiText.Get("ListSeparator"), details.SecondaryPositions.Select(x => UiText.Position(x, null)));
        facts.AddChild(Text("PlayerSecondaryPositions", UiText.Format("PlayerSecondaryLine", secondary), 14, "a6b6ca"));
        facts.AddChild(Text("PlayerBirthDate", UiText.Format("PlayerBirthLine",
            details.BirthDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? UiText.Get("Unknown")), 14, "a6b6ca"));
        return header;
    }

    private static Label Text(string name, string text, int size, string color)
    {
        var label = new Label { Name = name, Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", new Color(color));
        return label;
    }
}
