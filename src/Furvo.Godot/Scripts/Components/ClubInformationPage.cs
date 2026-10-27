using Furvo.Core.Queries;
using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Identidad y estadio existentes; los campos sin fuente aprobada permanecen explícitamente pendientes.</summary>
public partial class ClubInformationPage : VBoxContainer
{
    public ClubInformationPage(ClubDetails details)
    {
        Name = "ClubInformationPage"; SetMeta("club_id", details.Club.Id.ToString());
        var body = ClubUi.ScrollBody(this, "ClubInfoScroll"); var columns = ClubUi.ResponsiveColumns(body);
        var identity = ClubUi.Card(columns, "ClubInstitution");
        ClubUi.Field(identity, "ClubFoundationYear", details.Identity.FoundationYear);
        ClubUi.Field(identity, "ClubNickname", details.Identity.Nickname);
        ClubUi.Field(identity, "ClubSupporters", details.Identity.SupportersName);
        var colors = details.Identity.PrimaryColor is null || details.Identity.SecondaryColor is null ? null :
            UiText.Format("ClubColorsFormat", details.Identity.PrimaryColor, details.Identity.SecondaryColor);
        ClubUi.Field(identity, "ClubColors", colors);
        ClubUi.Field(identity, "ClubRivalries", details.Identity.Rivalries is null ? null : string.Join(UiText.Get("ListSeparator"), details.Identity.Rivalries.Select(c => c.Name)));
        ClubUi.Field(identity, "ClubSubscribers", details.Identity.Subscribers);
        ClubUi.Field(identity, "ClubTradition", details.Identity.Tradition);
        var stadium = ClubUi.Card(columns, "ClubStadium");
        stadium.AddChild(new StadiumPreview());
        stadium.AddChild(ClubUi.Text("StadiumImageStatus", UiText.Get("StadiumPlaceholder"), 13, "a6b6ca"));
        ClubUi.Field(stadium, "StadiumName", details.Stadium.Name);
        ClubUi.Field(stadium, "StadiumCity", details.Stadium.City);
        ClubUi.Field(stadium, "StadiumCapacity", details.Stadium.Capacity);
        ClubUi.Field(stadium, "StadiumAverageAttendance", details.Stadium.AverageAttendance);
        ClubUi.Field(stadium, "StadiumRecordAttendance", details.Stadium.RecordAttendance);
        ClubUi.Field(stadium, "StadiumOccupancy", details.Stadium.OccupancyPercent);
    }
}
