using Furvo.Core.Queries;
using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Composición institucional compacta; las propuestas visuales no sustituyen datos aprobados del mundo.</summary>
public partial class ClubInformationPage : VBoxContainer
{
    public ClubInformationPage(ClubDetails details, ClubHistoryDetails history)
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
        var stadium = ClubUi.Card(top, "ClubStadium"); stadium.GetParent<Control>().SizeFlagsStretchRatio = 64;
        var views = new HBoxContainer { Name = "StadiumViews" }; views.AddThemeConstantOverride("separation", 6); stadium.AddChild(views);
        AddView(views, new FootballPitch(), "StadiumPitchCaption");
        AddView(views, new StadiumPreview(), "StadiumExteriorCaption");
        AddView(views, new StadiumPreview(interior: true), "StadiumInteriorCaption");
        stadium.AddChild(ClubUi.Text("StadiumImageStatus", UiText.Get("StadiumPlaceholder"), 11, "a6b6ca"));
        var stadiumFields = ClubUi.ResponsiveColumns(stadium, breakpoint: 450);
        // Los datos se reorganizan en dos grupos; ninguna dimensión ilustrativa se presenta como medida del estadio.
        var left = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; stadiumFields.AddChild(left); left.AddThemeConstantOverride("separation", 4);
        var right = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; stadiumFields.AddChild(right); right.AddThemeConstantOverride("separation", 4);
        ClubUi.Field(left, "StadiumName", details.Stadium.Name); ClubUi.Field(left, "StadiumCity", details.Stadium.City);
        ClubUi.Field(left, "StadiumCapacity", details.Stadium.Capacity); ClubUi.Field(left, "StadiumDimensions", details.Stadium.PitchDimensions);
        ClubUi.Field(left, "StadiumSurface", details.Stadium.Surface); ClubUi.Field(left, "StadiumCategory", details.Stadium.Category is { } category ? UiText.Format("StadiumCategoryFormat", category) : null);
        ClubUi.Field(left, "StadiumOpening", details.Stadium.OpeningYear);
        ClubUi.Field(right, "StadiumAverageAttendance", details.Stadium.AverageAttendance); ClubUi.Field(right, "StadiumRecordAttendance", details.Stadium.RecordAttendance);
        ClubUi.Field(right, "StadiumOccupancy", details.Stadium.OccupancyPercent); ClubUi.Field(right, "StadiumTraining", details.Stadium.TrainingFacilities);
        ClubUi.Field(right, "StadiumYouth", details.Stadium.YouthFacilities); ClubUi.Field(right, "StadiumTrainingGround", details.Stadium.TrainingGround);
        body.AddChild(new ClubDirectionPanels(details.Club.Id.ToString()));
        var bottom = ClubUi.ResponsiveRow(body, "ClubHistorySummary");
        ClubHistorySections.Trajectory(bottom, history);
        ClubHistorySections.Legends(bottom, history);
        ClubHistorySections.Records(bottom, history);
    }
    private static void AddView(Control parent, Control image, string caption)
    {
        var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; column.AddThemeConstantOverride("separation", 2); parent.AddChild(column);
        image.SizeFlagsHorizontal = SizeFlags.ExpandFill; column.AddChild(image);
        var label = ClubUi.Text(caption, UiText.Get(caption), 11, "a6b6ca"); label.HorizontalAlignment = HorizontalAlignment.Center; column.AddChild(label);
    }
}
