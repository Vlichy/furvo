using Furvo.Core.Queries;
using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Cabecera común institucional; solo recibe datos de consulta procedentes del mundo.</summary>
public partial class ClubHeader : PanelContainer
{
    public ClubHeader(ClubDetails details)
    {
        Name = "ClubHeader"; SetMeta("club_id", details.Club.Id.ToString());
        var style = new StyleBoxFlat { BgColor = new Color("17283d"), ContentMarginLeft = 10, ContentMarginRight = 10,
            ContentMarginTop = 8, ContentMarginBottom = 8 }; style.SetCornerRadiusAll(5); AddThemeStyleboxOverride("panel", style);
        var line = new HBoxContainer(); line.AddThemeConstantOverride("separation", 16); AddChild(line); var visual = ClubVisualCatalog.Get(details.Club.Id.ToString()); line.AddChild(new ClubEmblem(visual));
        var content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; content.AddThemeConstantOverride("separation", 2); line.AddChild(content);
        content.AddChild(ClubUi.Text("ClubFullName", details.Club.Name, 24));
        content.AddChild(ClubUi.Text("ClubLocation", UiText.Format("ClubLocationFormat", ClubUi.Value(details.Club.City),
            ClubUi.Value(details.Identity.Region), ClubUi.Value(details.Identity.Country)), 14, "a6b6ca"));
        var competition = details.Competitions.Count == 0 ? UiText.Get("PendingDefinition") : string.Join(UiText.Get("ListSeparator"), details.Competitions.Select(c => c.Competition));
        content.AddChild(ClubUi.Text("ClubCompetitionReputation", UiText.Format("ClubCompetitionFormat", competition,
            ClubUi.Value(details.Club.VisibleReputation)), 15, "f1b96b"));
        var kits = new VBoxContainer { Name = "ClubKits" }; line.AddChild(kits);
        var shirts = new HBoxContainer(); shirts.AddThemeConstantOverride("separation", 8); kits.AddChild(shirts);
        for (var i = 0; i < 3; i++)
        {
            var item = new VBoxContainer(); item.AddThemeConstantOverride("separation", 0); shirts.AddChild(item);
            item.AddChild(new ClubShirt(visual.Kits[i]) { Name = "ClubShirt" + i });
            var label = ClubUi.Text("KitLabel" + i, UiText.Get("ClubKit" + i), 12);
            label.HorizontalAlignment = HorizontalAlignment.Center; item.AddChild(label);
        }
        kits.AddChild(ClubUi.Text("KitProvisional", UiText.Get("ClubKitsProvisional"), 11, "a6b6ca"));
    }
}
