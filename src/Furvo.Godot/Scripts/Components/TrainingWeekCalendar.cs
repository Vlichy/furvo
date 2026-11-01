using System.Globalization;
using Furvo.Core.Development;
using Godot;
namespace Furvo.Presentation.Components;
/// <summary>Calendario común para todas las pestañas. Solo emite selección de fechas y sesiones reales.</summary>
public partial class TrainingWeekCalendar : GridContainer
{
    public TrainingWeekCalendar(TrainingCalendarWeek week,Func<TrainingCalendarEntry,string> description,Action<DateOnly> add,Action<TrainingSession> edit)
    {
        Name="TrainingWeeklyCalendar";Columns=7;SizeFlagsHorizontal=SizeFlags.ExpandFill;AddThemeConstantOverride("h_separation",6);
        foreach(var day in week.Days){
            var panel=new PanelContainer{Name="TrainingDay_"+day.Date.ToString("yyyyMMdd"),SizeFlagsHorizontal=SizeFlags.ExpandFill,SizeFlagsStretchRatio=1};
            panel.SetMeta("calendar_date",day.Date.ToString("yyyy-MM-dd"));panel.SetMeta("is_current",day.IsCurrent);
            var style=new StyleBoxFlat{BgColor=new Color(day.IsCurrent?"20344c":"14243a"),BorderColor=new Color(day.IsCurrent?"f1b96b":"2c4058"),ContentMarginLeft=5,ContentMarginRight=5,ContentMarginTop=6,ContentMarginBottom=6};style.SetBorderWidthAll(day.IsCurrent?2:1);style.SetCornerRadiusAll(4);panel.AddThemeStyleboxOverride("panel",style);AddChild(panel);
            var body=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};panel.AddChild(body);
            var heading=new Button{Name="TrainingDateHeading",Text=day.Date.ToString("dddd",CultureInfo.GetCultureInfo("es-ES"))+"\n"+day.Date.ToString("dd/MM"),SizeFlagsHorizontal=SizeFlags.ExpandFill,ClipText=true,TooltipText=UiText.Get("TrainingAddSession")};
            heading.AddThemeFontSizeOverride("font_size",14);heading.AddThemeConstantOverride("outline_size",0);heading.Pressed+=()=>add(day.Date);body.AddChild(heading);body.AddChild(ClubUi.Text("TrainingDayType",day.Configuration is {} setting?UiText.Get("TrainingDayKind_"+setting.Kind):UiText.Get(day.Entries.Count>0?"TrainingLegacyDayPending":"TrainingDayUnplanned"),12,"f1b96b"));
            if(day.Configuration?.Kind==TrainingDayKind.Rest&&day.Entries.Count==0)body.AddChild(new TrainingActivityIcon(TrainingArea.Recovery,true){SizeFlagsHorizontal=SizeFlags.ShrinkCenter});
            if(day.Configuration?.Kind==TrainingDayKind.Double&&!day.Entries.Any(e=>e.Turn==TrainingTurn.Morning))body.AddChild(ClubUi.Text("TrainingMorningEmpty",UiText.Get("TrainingTurn_Morning")+" · —",12));
            var extra=new VBoxContainer{Name="TrainingDayOverflow"};var index=0;TrainingTurn? lastTurn=null;bool first=true;
            foreach(var entry in day.Entries){
                if(first||entry.Turn!=lastTurn){body.AddChild(ClubUi.Text("TrainingTurnHeading",entry.Turn is {} shift?UiText.Get("TrainingTurn_"+shift):UiText.Get("TrainingTurnPending"),12));lastTurn=entry.Turn;first=false;}
                var button=new Button{Name="TrainingSession_"+entry.Session.Id,CustomMinimumSize=new Vector2(0,46),TooltipText=description(entry),SizeFlagsHorizontal=SizeFlags.ExpandFill};
                button.SetMeta("session_id",entry.Session.Id.ToString());button.SetMeta("session_state",entry.State.ToString());button.AccessibilityName=description(entry);
                var line=new HBoxContainer{MouseFilter=MouseFilterEnum.Ignore,Alignment=BoxContainer.AlignmentMode.Center};button.AddChild(line);line.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
                line.AddChild(new TrainingActivityIcon(entry.Area,entry.Session.Exercise=="rest"));var hours=new Label{Text=entry.Session.Start.ToString("HH:mm")+"–"+entry.Session.Ends.ToString("HH:mm")+(entry.State==TrainingCalendarState.Completed?" ✓":""),MouseFilter=MouseFilterEnum.Ignore};hours.AddThemeFontSizeOverride("font_size",11);line.AddChild(hours);
                button.Pressed+=()=>edit(entry.Session);if(index++<3)body.AddChild(button);else extra.AddChild(button);
            }
            if(day.Configuration?.Kind==TrainingDayKind.Double&&!day.Entries.Any(e=>e.Turn==TrainingTurn.Afternoon))body.AddChild(ClubUi.Text("TrainingAfternoonEmpty",UiText.Get("TrainingTurn_Afternoon")+" · —",12));
            if(day.Entries.Count>3){var more=new Button{Name="TrainingMoreSessions",Text="+"+(day.Entries.Count-3),TooltipText=UiText.Get("TrainingMoreSessions")};body.AddChild(more);extra.Visible=false;body.AddChild(extra);more.Pressed+=()=>{extra.Visible=!extra.Visible;more.Text=extra.Visible?"−":"+"+(day.Entries.Count-3);};}else extra.Free();
            var plus=new Button{Name="TrainingAddDay",Text="+",TooltipText=UiText.Get("TrainingAddSession"),CustomMinimumSize=new Vector2(0,32)};plus.Pressed+=()=>add(day.Date);body.AddChild(plus);
        }
    }
}
