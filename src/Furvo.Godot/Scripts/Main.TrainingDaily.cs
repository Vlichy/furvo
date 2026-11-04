using System.Globalization;
using Furvo.Core.Development;
using Furvo.Presentation.Components;
using Godot;
namespace Furvo.Presentation;
public partial class Main
{
    private static void RenderTrainingAgendaSummary(VBoxContainer content,TrainingAgendaSummary summary)
    {
        var box=SportsCard(content,"TrainingAgendaSummary");box.Name="TrainingAgendaSummary";
        box.AddChild(ClubUi.Text("TrainingAgendaMetrics",UiText.Format("TrainingAgendaMetrics",summary.ExerciseCount,summary.PlannedDays,summary.ActivityMinutes,summary.RestDays),14,"f1b96b"));
        var categories=new HFlowContainer();box.AddChild(categories);
        foreach(var area in summary.CategoryMinutes)categories.AddChild(ClubUi.Text("TrainingAgendaCategory",UiText.Get("TrainingArea_"+area.Key)+": "+area.Value+" min",13));
        box.AddChild(ClubUi.Text("TrainingAgendaTurns",UiText.Format("TrainingAgendaTurns",summary.TurnMinutes[TrainingTurn.Morning],summary.TurnMinutes[TrainingTurn.Afternoon]),13));
        box.AddChild(ClubUi.Text("TrainingAgendaNotice",UiText.Get("TrainingDailyGlobalNotice"),13));
    }
    private void RenderTrainingDayPanel(VBoxContainer content,TrainingDailyView day,TrainingPlan plan,IReadOnlyList<TrainingCandidate> players,IReadOnlyList<TrainingStaff> staff,Action<TrainingSession> edit,Action<TrainingSession> delete,Action add)
    {
        var detail=SportsCard(content,"TrainingSelectedDay");detail.Name="TrainingDailyPanel";detail.SetMeta("selected_date",day.Date.ToString("yyyy-MM-dd"));
        var culture=CultureInfo.GetCultureInfo("es-ES");
        detail.AddChild(ClubUi.Text("TrainingSelectedDayTitle",UiText.Format("TrainingDailyTitle",culture.TextInfo.ToTitleCase(day.Date.ToString("dddd dd/MM/yyyy",culture))),18,"f1b96b"));
        detail.AddChild(ClubUi.Text("TrainingDailyKind",day.Configuration is {} setting?UiText.Get("TrainingDayKind_"+setting.Kind):UiText.Get(day.GlobalCount==0?"TrainingDayUnplanned":"TrainingLegacyDayPending"),14));
        detail.AddChild(ClubUi.Text("TrainingDailyCount",UiText.Format("TrainingDailyCount",day.VisibleCount,day.GlobalCount),14));
        var resting=day.Configuration?.Kind==TrainingDayKind.Rest&&day.GlobalCount==0;
        if(resting)detail.AddChild(ClubUi.Text("TrainingRestConfirmed",UiText.Get("TrainingRestConfirmed"),17,"f1b96b"));
        foreach(var legacy in day.LegacyRests??Array.Empty<LegacyTrainingRest>())if(legacy.Resolution==LegacyRestResolution.Review)detail.AddChild(ClubUi.Text("TrainingLegacyRestReview",UiText.Format("TrainingLegacyRestReview",legacy.Original.Start.ToString("HH:mm"),legacy.Original.Ends.ToString("HH:mm")),14,"f1b96b"));
        if(day.GlobalCount==0&&!resting)detail.AddChild(ClubUi.Text("TrainingDailyEmpty",UiText.Get("TrainingDailyEmpty"),15));
        foreach(var shift in day.Turns){
            detail.AddChild(ClubUi.Text("TrainingDailyTurn",shift.Turn is {} known?UiText.Get("TrainingTurn_"+known):UiText.Get("TrainingTurnPending"),16,"f1b96b"));
            if(shift.Blocks.Count==0)detail.AddChild(ClubUi.Text("TrainingNoBlocks",UiText.Get("TrainingNoBlocks"),14));
            foreach(var block in shift.Blocks){
                var target=block.Target.Scope switch{
                    TrainingScope.Individual=>players.SingleOrDefault(p=>p.Id==block.Target.Player)?.Name??UiText.Get("Unknown"),
                    TrainingScope.Position=>UiText.Get("Position_"+block.Target.Position),
                    TrainingScope.Group=>plan.Groups.SingleOrDefault(g=>g.Id==block.Target.Group)?.Name??UiText.Get("TrainingGroupLabel"),
                    _=>UiText.Get("TrainingScope_Team")};
                var participants=target+" · "+UiText.Format("TrainingDailyPlayers",block.Participants.Count);
                var owner=staff.SingleOrDefault(s=>s.Id==block.Responsible);
                var responsible=owner is not null?owner.Name+" — "+UiText.Get("AnalysisStaff_"+owner.Role):UiText.Get(block.Responsible is null?"TrainingUnassignedResponsible":"TrainingDailyStaffUnavailable");
                detail.AddChild(new TrainingDailyExerciseRow(block,participants,responsible,()=>edit(block),()=>delete(block)));
            }
        }
        detail.AddChild(ClubUi.Text("TrainingDailyGlobalNotice",UiText.Get("TrainingDailyGlobalNotice"),13));
        var totals=new GridContainer{Name="TrainingDailyTotals",Columns=3,SizeFlagsHorizontal=SizeFlags.ExpandFill};totals.AddThemeConstantOverride("h_separation",12);detail.AddChild(totals);
        void Total(string key,int minutes){var box=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};box.AddChild(ClubUi.Text(key+"Label",UiText.Get(key),14));box.AddChild(ClubUi.Text(key,minutes+" min",19,"f1b96b"));totals.AddChild(box);}
        Total("TrainingDailyMorning",day.GlobalTotals.MorningActivityMinutes);Total("TrainingDailyAfternoon",day.GlobalTotals.AfternoonActivityMinutes);Total("TrainingDailyTotal",day.GlobalTotals.ActivityMinutes);
        if(day.GlobalTotals.PendingActivityMinutes>0)detail.AddChild(ClubUi.Text("TrainingDailyPending",UiText.Format("TrainingDailyPending",day.GlobalTotals.PendingActivityMinutes),14));
        var maximum=day.GlobalTotals.IndividualMinutes.Values.DefaultIfEmpty(0).Max();
        detail.AddChild(ClubUi.Text("TrainingDailyIndividual",UiText.Format("TrainingDailyIndividual",day.GlobalTotals.EffectiveMinutes,maximum),13));
        foreach(var warning in TrainingSchedule.Warnings(plan,day.Date,_trainingSchedulePolicy))detail.AddChild(ClubUi.Text("TrainingDayWarning",UiText.Get("TrainingWarning_"+warning),14,"f1b96b"));
        detail.AddChild(WindowUi.Button(resting?"TrainingChangeRestToTraining":"TrainingDailyAddAnother",add));
    }
}
