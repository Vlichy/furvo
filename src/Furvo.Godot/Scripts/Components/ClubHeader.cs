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
        var line = new HBoxContainer(); line.AddThemeConstantOverride("separation", 16); AddChild(line); line.AddChild(new ClubEmblem());
        var content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; content.AddThemeConstantOverride("separation", 2); line.AddChild(content);
        content.AddChild(ClubUi.Text("ClubFullName", details.Club.Name, 24));
        content.AddChild(ClubUi.Text("ClubLocation", UiText.Format("ClubLocationFormat", ClubUi.Value(details.Club.City),
            ClubUi.Value(details.Identity.Region), ClubUi.Value(details.Identity.Country)), 14, "a6b6ca"));
        var competition = details.Competitions.Count == 0 ? UiText.Get("PendingDefinition") : string.Join(UiText.Get("ListSeparator"), details.Competitions.Select(c => c.Competition));
        content.AddChild(ClubUi.Text("ClubCompetitionReputation", UiText.Format("ClubCompetitionFormat", competition,
            ClubUi.Value(details.Club.VisibleReputation)), 15, "f1b96b"));
    }
}
