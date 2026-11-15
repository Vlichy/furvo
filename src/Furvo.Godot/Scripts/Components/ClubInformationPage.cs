using Furvo.Core.Queries;
using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Composición institucional compacta; las propuestas visuales no sustituyen datos aprobados del mundo.</summary>
public partial class ClubInformationPage : VBoxContainer
{
    public ClubInformationPage(ClubDetails details, ClubHistoryDetails history, Action? manageStadium = null)
    {
        Name = "ClubInformationPage"; SetMeta("club_id", details.Club.Id.ToString());
        var body = ClubUi.ScrollBody(this, "ClubInfoScroll");
        var top = ClubUi.ResponsiveRow(body, "ClubOverviewColumns");
        var identity = ClubUi.Card(top, "ClubInstitution"); identity.GetParent<Control>().SizeFlagsStretchRatio = 36;
        ClubUi.Field(identity, "ClubIdentityName", details.Club.Name);
        ClubUi.Field(identity, "City", details.Club.City);
        ClubUi.Field(identity, "ClubRegion", details.Identity.Region);
        ClubUi.Field(identity, "ClubCountry", details.Identity.Country);
        ClubUi.Field(identity, "ClubFoundationYear", details.Identity.FoundationYear);
        ClubUi.Field(identity, "ClubNickname", details.Identity.Nickname);
        ClubUi.Field(identity, "ClubSupporters", details.Identity.SupportersName);
        ClubUi.Field(identity, "ClubSubscribers", details.Identity.Subscribers);
        ClubUi.Field(identity, "ClubAssociations", details.Identity.SupporterAssociations.Count > 0 ? string.Join(UiText.Get("ListSeparator"), details.Identity.SupporterAssociations) : null);
        ClubUi.Field(identity, "ClubRivalries", details.Identity.Rivalries is { Count: > 0 } ? string.Join(UiText.Get("ListSeparator"), details.Identity.Rivalries.Select(c => c.Name)) : null);
        ClubUi.Field(identity, "ClubOwnership", details.Identity.Ownership is { } ownership ? UiText.Get("ClubOwnership_" + ownership) : null);
        ClubUi.Field(identity, "ClubPresident", details.Identity.President);
        ClubUi.Field(identity, "ClubOwner", details.Identity.MajorityOwner);
        ClubUi.Field(identity, "ClubGroup", details.Identity.Affiliation == ClubAffiliationKind.Independent ? UiText.Get("ClubIndependent") : details.Identity.GroupName);
        ClubUi.Field(identity, "ClubColors", details.Identity.PrimaryColor is null ? null : UiText.Format("ClubColorsFormat", details.Identity.PrimaryColor, ClubUi.Value(details.Identity.SecondaryColor)));
        ClubUi.Field(identity, "ClubTradition", details.Identity.Tradition);
        var stadium = ClubUi.Card(top, "ClubStadium");
        stadium.GetParent<Control>().SizeFlagsStretchRatio = 64;
        ClubStadiumPage.AddOverview(stadium, details, compact: true);
        if (manageStadium is not null)
            stadium.AddChild(WindowUi.Button("ManageClubInfrastructure", manageStadium));
        body.AddChild(new ClubDirectionPanels(details.Club.Id.ToString()));
        var bottom = ClubUi.ResponsiveRow(body, "ClubHistorySummary");
        ClubHistorySections.Trajectory(bottom, history);
        ClubHistorySections.Legends(bottom, history);
        ClubHistorySections.Records(bottom, history);
    }
}
