using Furvo.Core.Queries;
using Godot;

namespace Furvo.Presentation.Components;

public partial class ClubStadiumPage : VBoxContainer
{
    public ClubStadiumPage(ClubDetails details, bool management = false)
    {
        Name = "ClubStadiumPage";
        SetMeta("club_id", details.Club.Id.ToString());
        if (management)
        {
            var navigation = new HFlowContainer { Name = "StadiumSectionsNavigation" };
            navigation.AddThemeConstantOverride("h_separation", 6);
            AddChild(navigation);
            foreach (var key in new[] { "StadiumCurrent", "StadiumReforms", "StadiumNew", "StadiumCampus", "StadiumProjects" })
            {
                var current = key == "StadiumCurrent";
                var button = new Button
                {
                    Name = key, Text = UiText.Get(key), Disabled = true,
                    TooltipText = current ? UiText.Get(key) : UiText.Get("StadiumManagementPending")
                };
                if (current) button.AddThemeColorOverride("font_disabled_color", new Color("f1b96b"));
                navigation.AddChild(button);
            }
            AddChild(ClubUi.Text("StadiumManagementStatus", UiText.Get("StadiumManagementPending"), 13, "a6b6ca"));
        }
        AddOverview(ClubUi.ScrollBody(this, "ClubStadiumScroll"), details);
    }

    internal static void AddOverview(Control body, ClubDetails details, bool compact = false)
    {
        var stadium = details.Stadium;
        var fields = compact ? body : ClubUi.ResponsiveRow(body, "StadiumSections");
        var identity = compact ? body : ClubUi.Card(fields, "StadiumIdentitySection");
        ClubUi.Field(identity, "StadiumName", stadium.Name);
        ClubUi.Field(identity, "StadiumCity", stadium.City);
        ClubUi.Field(identity, "StadiumOpening", stadium.OpeningYear);
        ClubUi.Field(identity, "StadiumCategory", stadium.Category is { } category ? UiText.Format("StadiumCategoryFormat", category) : null);
        var pitch = compact ? body : ClubUi.Card(fields, "StadiumPitchSection");
        ClubUi.Field(pitch, "StadiumDimensions", stadium.PitchDimensions);
        ClubUi.Field(pitch, "StadiumSurface", stadium.Surface);
        var attendance = compact ? body : ClubUi.Card(fields, "StadiumAttendanceSection");
        ClubUi.Field(attendance, "StadiumCapacity", stadium.Capacity);
        ClubUi.Field(attendance, "StadiumAverageAttendance", stadium.AverageAttendance);
        ClubUi.Field(attendance, "StadiumRecordAttendance", stadium.RecordAttendance);
        ClubUi.Field(attendance, "StadiumOccupancy", stadium.OccupancyPercent);

        var illustrations = compact ? body : ClubUi.Card(body, "StadiumIllustrationsSection");
        var views = new HBoxContainer { Name = "StadiumViews" };
        views.AddThemeConstantOverride("separation", 8);
        illustrations.AddChild(views);
        AddView(views, new FootballPitch(), "StadiumPitchCaption");
        AddView(views, new StadiumPreview(), "StadiumExteriorCaption");
        AddView(views, new StadiumPreview(interior: true), "StadiumInteriorCaption");
        illustrations.AddChild(ClubUi.Text("StadiumImageStatus", UiText.Get("StadiumPlaceholder"), 12, "a6b6ca"));

        if (compact) return;
        var facilities = ClubUi.Card(body, "StadiumFacilitiesSection");
        ClubUi.Field(facilities, "StadiumTraining", stadium.TrainingFacilities);
        ClubUi.Field(facilities, "StadiumYouth", stadium.YouthFacilities);
        ClubUi.Field(facilities, "StadiumTrainingGround", stadium.TrainingGround);
    }

    private static void AddView(Control parent, Control image, string caption)
    {
        var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        parent.AddChild(column);
        image.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        column.AddChild(image);
        var label = ClubUi.Text(caption, UiText.Get(caption), 12, "a6b6ca");
        label.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(label);
    }
}
