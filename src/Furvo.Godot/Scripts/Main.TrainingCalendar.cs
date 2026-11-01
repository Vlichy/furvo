using System.Globalization;
using Furvo.Core.Domain;
using Furvo.Core.Development;
using Furvo.Core.Tactics;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Godot;
namespace Furvo.Presentation;
public partial class Main
{
    private sealed record TrainingDraft(Guid? Editing,string Date,string Time,int Minutes,string Exercise,TrainingLoad Load,TrainingScope Scope,
        PlayerId? Player,PlayerRole? Position,Guid? Group,PlayerId[] Excluded,string Objective,int Frequency,Guid? Responsible,bool Opposition,RoutineReference? Routine,bool Dirty=false,TrainingDayKind DayKind=TrainingDayKind.Single,TrainingTurn? Turn=TrainingTurn.Morning);
    // Los borradores son exclusivamente de interfaz y están aislados por archivo de carrera/equipo.
    private readonly Dictionary<string,TrainingDraft> _trainingDrafts=new();
    private DateOnly? _trainingSelectedDay;
    private bool _trainingFormCollapsed,_trainingCategoryOnly,_trainingExclusionsOpen;
    private void TrainingConfirm(string message,Action action)
    {
        var dialog=new ConfirmationDialog{Name="TrainingConfirmDialog",Title=UiText.Get("Brand"),DialogText=message,Exclusive=true,OkButtonText=UiText.Get("PlanConfirm"),CancelButtonText=UiText.Get("PlanCancel")};
        dialog.Confirmed+=()=>{dialog.QueueFree();action();};dialog.Canceled+=()=>dialog.QueueFree();AddChild(dialog);dialog.PopupCentered(new Vector2I(520,180));
    }
    private void RenderTrainingPlanner(VBoxContainer content,Label status,Guid team,TrainingPlan plan,IReadOnlyList<TrainingCandidate> players,IReadOnlyList<TrainingStaff> staff,SqliteSportsStore service,DateOnly today)
    {
        var area=_trainingTab switch{1=>TrainingArea.Field,2=>TrainingArea.Gym,3=>TrainingArea.Goalkeepers,4=>TrainingArea.SetPieces,5=>TrainingArea.Recovery,6=>TrainingArea.Technology,_=>(TrainingArea?)null};
        var week=TrainingCalendarWeek.Create(plan,_trainingWeek??today,today).Start;_trainingWeek=week;
        var key=SportsPath+"|"+team;
        TrainingDraft Empty(DateOnly day){var setting=TrainingSchedule.Day(plan,day);var turn=setting?.SingleTurn??TrainingTurn.Morning;return new(null,day.ToString("yyyy-MM-dd"),turn==TrainingTurn.Morning?"09:00":"16:00",45,TrainingCatalog.Exercises.First(e=>area is null||e.Area==area).Code,TrainingLoad.Medium,TrainingScope.Team,null,null,null,Array.Empty<PlayerId>(),"",1,null,false,null,DayKind:setting?.Kind??TrainingDayKind.Single,Turn:turn);}
        if(!_trainingDrafts.TryGetValue(key,out var draft))draft=Empty(week<today?today:week);
        else if(!draft.Dirty&&draft.Editing is null)draft=draft with{Exercise=Empty(today).Exercise};
        _trainingDrafts[key]=draft;
        var section=SportsCard(content,"TrainingCreate");section.Name="TrainingSessionForm";
        section.AddChild(ClubUi.Text("TrainingCreateTitle",UiText.Get("TrainingCreateTitle")+(draft.Editing is null?"":" · "+UiText.Get("TrainingEditing")),18,"f1b96b"));
        var fields=new VBoxContainer{Name="TrainingFormFields",Visible=!_trainingFormCollapsed};
        var toggle=WindowUi.Button(_trainingFormCollapsed?"TrainingExpandForm":"TrainingCollapseForm",()=>{_trainingFormCollapsed=!_trainingFormCollapsed;ShowTraining();});section.AddChild(toggle);section.AddChild(fields);
        var grid=new GridContainer{Columns=GetViewportRect().Size.X<1400?2:4,SizeFlagsHorizontal=SizeFlags.ExpandFill};grid.Resized+=()=>{var count=GetViewportRect().Size.X<1400?2:4;if(grid.Columns!=count)grid.Columns=count;};grid.AddThemeConstantOverride("h_separation",12);grid.AddThemeConstantOverride("v_separation",8);fields.AddChild(grid);
        VBoxContainer Field(string label,Control control){var box=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};box.AddChild(ClubUi.Text(label+"Label",UiText.Get(label),14));control.SizeFlagsHorizontal=SizeFlags.ExpandFill;box.AddChild(control);grid.AddChild(box);return box;}
        var date=new LineEdit{Name="TrainingDate",Text=draft.Date};Field("TrainingDateLabel",date);
        var dayKind=new OptionButton{Name="TrainingDayKind"};foreach(var kind in Enum.GetValues<TrainingDayKind>())dayKind.AddItem(UiText.Get("TrainingDayKind_"+kind));dayKind.Selected=(int)draft.DayKind-1;Field("TrainingDayKindLabel",dayKind);
        var turn=new OptionButton{Name="TrainingTurn"};turn.AddItem(UiText.Get("TrainingTurnPending"));turn.AddItem(UiText.Get("TrainingTurn_Morning"));turn.AddItem(UiText.Get("TrainingTurn_Afternoon"));turn.Selected=draft.Turn is {} shift?(int)shift:0;turn.SetItemDisabled(0,draft.Editing is null||draft.Turn is not null);var turnBox=Field("TrainingTurnLabel",turn);
        OptionButton Clock(string name,TimeSpan value){
            var control=new OptionButton{Name=name};var values=Enumerable.Range(0,name=="TrainingTime"?96:97).Select(i=>TimeSpan.FromMinutes(i*15)).Append(value).Distinct().Order().ToArray();
            foreach(var item in values){control.AddItem(item==TimeSpan.FromDays(1)?"24:00":item.ToString(item.Seconds==0?@"hh\:mm":@"hh\:mm\:ss"));control.SetItemMetadata(control.ItemCount-1,item.Ticks);}control.Selected=Array.IndexOf(values,value);return control;
        }
        var initial=TimeOnly.Parse(draft.Time,CultureInfo.InvariantCulture);var time=Clock("TrainingTime",initial.ToTimeSpan());var startBox=Field("TrainingStartLabel",time);
        var end=Clock("TrainingEndTime",initial.ToTimeSpan()+TimeSpan.FromMinutes(draft.Minutes));var endBox=Field("TrainingEndLabel",end);
        var duration=new Label{Name="TrainingCalculatedDuration"};var durationBox=Field("TrainingDuration",duration);
        int Minutes()=>(int)(TimeSpan.FromTicks(end.GetItemMetadata(end.Selected).AsInt64())-TimeSpan.FromTicks(time.GetItemMetadata(time.Selected).AsInt64())).TotalMinutes;
        void Duration(){duration.Text=Minutes()+" min";}Duration();
        var exercises=TrainingCatalog.Exercises.ToArray();var exercise=new OptionButton{Name="TrainingExercise",ClipText=true,FitToLongestItem=false};foreach(var e in exercises)exercise.AddItem(UiText.Get("Exercise_"+e.Code));exercise.Selected=Array.FindIndex(exercises,e=>e.Code==draft.Exercise);var exerciseBox=Field("TrainingExerciseLabel",exercise);
        var scope=new OptionButton{Name="TrainingScope"};foreach(var item in Enum.GetValues<TrainingScope>())scope.AddItem(UiText.Get("TrainingScope_"+item));scope.Selected=(int)draft.Scope-1;var scopeBox=Field("TrainingParticipantsLabel",scope);
        var responsible=new OptionButton{Name="TrainingResponsible",ClipText=true};var staffBox=Field("TrainingResponsibleLabel",responsible);
        void Staff(Guid? selected){
            responsible.Clear();responsible.AddItem(UiText.Get("TrainingUnassignedResponsible"));responsible.SetItemMetadata(0,"");
            var test=new TrainingSession(Guid.Empty,team,today,new(9,0),15,exercises[exercise.Selected].Code,TrainingLoad.Medium,new((TrainingScope)(scope.Selected+1)),Array.Empty<PlayerId>());
            foreach(var member in TrainingSchedule.Eligible(staff,test)){responsible.AddItem(member.Name+" — "+UiText.Get("AnalysisStaff_"+member.Role));responsible.SetItemMetadata(responsible.ItemCount-1,member.Id.ToString());}
            var index=Enumerable.Range(1,Math.Max(0,responsible.ItemCount-1)).FirstOrDefault(i=>responsible.GetItemMetadata(i).AsString()==selected?.ToString());
            if(selected is {} missing&&index==0){responsible.AddItem((staff.SingleOrDefault(p=>p.Id==missing)?.Name??UiText.Get("Unknown"))+" — "+UiText.Get("TrainingStaffIneligible"));index=responsible.ItemCount-1;responsible.SetItemMetadata(index,missing.ToString());responsible.SetItemDisabled(index,true);}responsible.Selected=index;
        }
        Guid? Responsible()=>responsible.Selected>0&&Guid.TryParse(responsible.GetItemMetadata(responsible.Selected).AsString(),out var id)?id:null;Staff(draft.Responsible);
        var intensity=new OptionButton{Name="TrainingIntensity"};foreach(var load in Enum.GetValues<TrainingLoad>())intensity.AddItem(UiText.Get("TrainingLoad_"+load));intensity.Selected=(int)draft.Load;var intensityBox=Field("TrainingIntensityLabel",intensity);
        var frequency=new SpinBox{Name="TrainingFrequency",MinValue=1,MaxValue=52,Step=1,Value=draft.Frequency};Field("TrainingFrequency",frequency);
        var person=new OptionButton{Name="TrainingPlayer",ClipText=true};foreach(var p in players)person.AddItem(p.Name);person.Selected=Math.Max(0,players.ToList().FindIndex(p=>p.Id==draft.Player));var personBox=Field("TrainingPlayerLabel",person);
        var role=new OptionButton{Name="TrainingPosition",ClipText=true};var roles=Enum.GetValues<PlayerRole>();foreach(var p in roles)role.AddItem(UiText.Get("Position_"+p));role.Selected=draft.Position is {} position?Array.IndexOf(roles,position):0;var roleBox=Field("TrainingPositionLabel",role);
        var group=new OptionButton{Name="TrainingGroup",ClipText=true};foreach(var g in plan.Groups)group.AddItem(g.Name);group.Selected=plan.Groups.Count==0?-1:Math.Max(0,plan.Groups.ToList().FindIndex(g=>g.Id==draft.Group));var groupBox=Field("TrainingGroupLabel",group);
        var routine=new OptionButton{Name="TrainingRoutine",ClipText=true};var routines=Enum.GetValues<SetPieceKind>();foreach(var kind in routines)routine.AddItem(UiText.Get("SetPiece_"+kind));routine.Selected=draft.Routine is {} configured?Array.IndexOf(routines,configured.Kind):0;var routineBox=Field("TrainingRoutineLabel",routine);
        void Scope(){var active=dayKind.Selected!=0;personBox.Visible=active&&scope.Selected==1;roleBox.Visible=active&&scope.Selected==2;groupBox.Visible=active&&scope.Selected==3;routineBox.Visible=active&&TrainingCatalog.Get(exercises[exercise.Selected].Code).Area==TrainingArea.SetPieces;foreach(var box in new[]{turnBox,startBox,endBox,durationBox,exerciseBox,scopeBox,staffBox,intensityBox})box.Visible=active;}Scope();
        var exclude=new ItemList{Name="TrainingExcluded",SelectMode=ItemList.SelectModeEnum.Multi,CustomMinimumSize=new Vector2(0,90),Visible=_trainingExclusionsOpen};foreach(var p in players)exclude.AddItem(p.Name);for(var i=0;i<players.Count;i++)if(draft.Excluded.Contains(players[i].Id))exclude.Select(i,false);
        var exclusionToggle=WindowUi.Button("TrainingToggleExclusions",()=>{_trainingExclusionsOpen=!_trainingExclusionsOpen;exclude.Visible=_trainingExclusionsOpen;});
        fields.AddChild(ClubUi.Text("TrainingObjectiveLabel",UiText.Get("TrainingObjective"),14));var objective=new LineEdit{Name="TrainingObjective",Text=draft.Objective};fields.AddChild(objective);fields.AddChild(exclusionToggle);fields.AddChild(exclude);
        var opposition=new PriorityChoice(UiText.Get("TrainingOpposition"),draft.Opposition){Name="TrainingOppositionRow"};opposition.Indicator.Name="TrainingOpposition";fields.AddChild(opposition);
        TrainingDraft Capture()=>new(draft.Editing,date.Text,TimeOnly.FromTimeSpan(TimeSpan.FromTicks(time.GetItemMetadata(time.Selected).AsInt64())).ToString("HH:mm:ss.fffffff"),Minutes(),exercises[exercise.Selected].Code,(TrainingLoad)intensity.Selected,(TrainingScope)(scope.Selected+1),
            scope.Selected==1&&person.Selected>=0?players[person.Selected].Id:null,scope.Selected==2?roles[role.Selected]:null,scope.Selected==3&&group.Selected>=0?plan.Groups[group.Selected].Id:null,
            exclude.GetSelectedItems().Select(i=>players[i].Id).ToArray(),objective.Text,(int)frequency.Value,Responsible(),opposition.Checked,
            routineBox.Visible?new(routines[routine.Selected],draft.Routine?.Kind==routines[routine.Selected]?draft.Routine.Location:null):null,true,(TrainingDayKind)(dayKind.Selected+1),turn.Selected==0?null:(TrainingTurn)turn.Selected);
        void Changed(){_trainingDrafts[key]=Capture();}
        date.TextChanged+=_=>Changed();time.ItemSelected+=_=>{Duration();Changed();};end.ItemSelected+=_=>{Duration();Changed();};dayKind.ItemSelected+=_=>{Scope();Changed();};turn.ItemSelected+=_=>Changed();intensity.ItemSelected+=_=>Changed();exercise.ItemSelected+=_=>{var owner=Responsible();Scope();Staff(owner);Changed();};scope.ItemSelected+=_=>{var owner=Responsible();Scope();Staff(owner);Changed();};responsible.ItemSelected+=_=>Changed();frequency.ValueChanged+=_=>Changed();person.ItemSelected+=_=>Changed();role.ItemSelected+=_=>Changed();group.ItemSelected+=_=>Changed();routine.ItemSelected+=_=>Changed();exclude.MultiSelected+=(_,_)=>Changed();objective.TextChanged+=_=>Changed();opposition.Indicator.Toggled+=_=>Changed();
        void Replace(TrainingDraft next){void Apply(){_trainingDrafts[key]=next;_trainingFormCollapsed=false;ShowTraining();}if(_trainingDrafts[key].Dirty)TrainingConfirm(UiText.Get("TrainingDiscardChanges"),Apply);else Apply();}
        if(draft.Editing is {} legacyId&&plan.Sessions.Any(s=>s.Id==legacyId&&TrainingSchedule.Classify(s) is null))fields.AddChild(ClubUi.Text("TrainingLegacyNotice",UiText.Get("TrainingLegacyNotice"),14,"f1b96b"));
        var actions=new HFlowContainer{Name="TrainingFormActions"};fields.AddChild(actions);
        actions.AddChild(WindowUi.Button("TrainingSaveSession",()=>{try{
            var value=Capture();if(!DateOnly.TryParseExact(value.Date,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var day)||!TimeOnly.TryParse(value.Time,CultureInfo.InvariantCulture,DateTimeStyles.None,out var start))throw new InvalidDataException(UiText.Get("TrainingInvalidDate"));
            if(value.Editing is {} missing&&!plan.Sessions.Any(s=>s.Id==missing))throw new InvalidOperationException(UiText.Get("TrainingMissingSession"));
            TrainingPlan candidate=plan;var config=new TrainingDayPlan(day,value.DayKind,value.DayKind==TrainingDayKind.Single?value.Turn:null);
            if(value.DayKind!=TrainingDayKind.Rest){
                TrainingSchedule.Duration(start,TimeSpan.FromTicks(end.GetItemMetadata(end.Selected).AsInt64()));
                var target=new TrainingTarget(value.Scope,value.Player,value.Position,value.Group,value.Excluded);var e=TrainingCatalog.Get(value.Exercise);
                var session=new TrainingSession(value.Editing??Guid.NewGuid(),team,day,start,value.Minutes,value.Exercise,value.Load,target,TrainingRules.Resolve(target,players,plan.Groups),value.Responsible,value.Opposition,value.Routine,e.Area==TrainingArea.Technology?service.Facility().Id:null,string.IsNullOrWhiteSpace(value.Objective)?UiText.Get("Exercise_"+e.Code):value.Objective,value.Frequency,value.Turn);
                candidate=plan with{Sessions=plan.Sessions.Where(s=>s.Id!=session.Id).Append(session).ToArray()};
            }
            void Commit(bool remove){try{var legacyUnclassified=value.Editing is {} id&&value.Turn is null&&value.DayKind==TrainingDayKind.Single&&plan.Sessions.Any(s=>s.Id==id&&s.Date==day&&s.Start==start&&s.Minutes==value.Minutes&&TrainingSchedule.Classify(s) is null);var next=legacyUnclassified?candidate:TrainingSchedule.SetDay(candidate,config,remove);service.SavePlan(next with{RequiresReview=false});_trainingDrafts.Remove(key);ShowTraining();}catch(Exception error){SportsError(error,status);}}
            var incompatible=TrainingSchedule.Incompatible(candidate,config);
            if(incompatible.Count>0)TrainingConfirm(UiText.Format("TrainingConfirmDayChange",incompatible.Count),()=>Commit(true));else Commit(false);
        }catch(Exception error){SportsError(error,status);}}));
        actions.AddChild(WindowUi.Button("TrainingCancelSession",()=>Replace(Empty(today))));
        if(draft.Editing is {} editing)actions.AddChild(WindowUi.Button("TrainingDeleteSession",()=>TrainingConfirm(UiText.Get("TrainingConfirmDelete"),()=>{try{service.SavePlan(plan with{Sessions=plan.Sessions.Where(s=>s.Id!=editing).ToArray()});_trainingDrafts.Remove(key);ShowTraining();}catch(Exception e){SportsError(e,status);}})));
        if(_trainingSelectedDay is {} selectedDay){
            var detail=SportsCard(content,"TrainingSelectedDay");detail.AddChild(ClubUi.Text("TrainingSelectedDayTitle",UiText.Get("TrainingShowDay")+" · "+selectedDay.ToString("dd/MM/yyyy"),17,"f1b96b"));
            var settings=TrainingSchedule.Day(plan,selectedDay);var turns=TrainingSchedule.Turns(plan,selectedDay);
            foreach(var dayTurn in new TrainingTurn?[]{TrainingTurn.Morning,TrainingTurn.Afternoon,null}){
                var turnData=turns.SingleOrDefault(t=>t.Turn==dayTurn);if(turnData is null&&!(settings?.Kind==TrainingDayKind.Double&&dayTurn is not null))continue;
                var title=dayTurn is {} known?UiText.Get("TrainingTurn_"+known):UiText.Get("TrainingTurnPending");
                detail.AddChild(ClubUi.Text("TrainingDayTurn",UiText.Format("TrainingEffectiveElapsed",title,turnData?.EffectiveMinutes??0,turnData?.ElapsedMinutes??0),14,"f1b96b"));
                if(turnData is null)detail.AddChild(ClubUi.Text("TrainingNoBlocks",UiText.Get("TrainingNoBlocks"),14));
                else foreach(var block in turnData.Blocks){var button=new Button{Name="TrainingDayBlock_"+block.Id,Text=block.Start.ToString("HH:mm")+"–"+block.Ends.ToString("HH:mm")+" · "+UiText.Get("Exercise_"+block.Exercise),SizeFlagsHorizontal=SizeFlags.ExpandFill};button.Pressed+=()=>Replace(new(block.Id,block.Date.ToString("yyyy-MM-dd"),block.Start.ToString("HH:mm:ss.fffffff"),block.Minutes,block.Exercise,block.Load,block.Target.Scope,block.Target.Player,block.Target.Position,block.Target.Group,block.Target.Excluded?.ToArray()??Array.Empty<PlayerId>(),block.Objective??"",block.FrequencyWeeks,block.Responsible,block.Opposition,block.Routine,DayKind:settings?.Kind??TrainingDayKind.Single,Turn:TrainingSchedule.Classify(block)));detail.AddChild(button);}
            }
            foreach(var warning in TrainingSchedule.Warnings(plan,selectedDay))detail.AddChild(ClubUi.Text("TrainingDayWarning",UiText.Get("TrainingWarning_"+warning),14,"f1b96b"));
        }
        var weekRow=new HFlowContainer{Name="TrainingWeekNavigation"};content.AddChild(weekRow);weekRow.AddChild(WindowUi.Button("TrainingPreviousWeek",()=>{_trainingWeek=week.AddDays(-7);ShowTraining();}));weekRow.AddChild(ClubUi.Text("TrainingWeek",week.ToString("dd/MM/yyyy")+" – "+week.AddDays(6).ToString("dd/MM/yyyy"),17));weekRow.AddChild(WindowUi.Button("TrainingNextWeek",()=>{_trainingWeek=week.AddDays(7);ShowTraining();}));
        var filter=new OptionButton{Name="TrainingCalendarFilter"};filter.AddItem(UiText.Get("TrainingAllSessions"));filter.AddItem(UiText.Get("TrainingCategorySessions"));filter.SetItemDisabled(1,area is null);filter.Selected=_trainingCategoryOnly&&area is not null?1:0;weekRow.AddChild(filter);filter.ItemSelected+=i=>{_trainingCategoryOnly=i==1;ShowTraining();};
        var completed=new SqliteTrainingExecutionStore(SportsPath).CompletedSessions(team);
        var calendar=TrainingCalendarWeek.Create(plan,week,today,_trainingCategoryOnly?area:null,completed);
        foreach(var visibleDay in calendar.Days)foreach(var warning in TrainingSchedule.Warnings(plan,visibleDay.Date))content.AddChild(ClubUi.Text("TrainingWeekWarning",visibleDay.Date.ToString("dd/MM")+" · "+UiText.Get("TrainingWarning_"+warning),14,"f1b96b"));
        string Tooltip(TrainingCalendarEntry entry){
            var s=entry.Session;var names=string.Join(", ",players.Where(p=>s.Participants.Contains(p.Id)).Take(5).Select(p=>p.Name));var groupName=s.Target.Group is {} id?plan.Groups.SingleOrDefault(g=>g.Id==id)?.Name:null;
            return UiText.Get("Exercise_"+s.Exercise)+"\n"+UiText.Get("TrainingArea_"+entry.Area)+" · "+UiText.Get("TrainingSessionState_"+entry.State)+"\n"+s.Start.ToString("HH:mm")+" · "+s.Minutes+" min · "+UiText.Get("TrainingLoad_"+s.Load)+"\n"+(groupName is null?"":groupName+" · ")+UiText.Get("TrainingParticipantsLabel")+": "+s.Participants.Count+" · "+names+(s.Participants.Count>5?"…":"");
        }
        content.AddChild(new TrainingWeekCalendar(calendar,Tooltip,day=>{_trainingSelectedDay=day;Replace(Empty(day));},session=>Replace(new(session.Id,session.Date.ToString("yyyy-MM-dd"),session.Start.ToString("HH:mm:ss.fffffff"),session.Minutes,session.Exercise,session.Load,session.Target.Scope,session.Target.Player,session.Target.Position,session.Target.Group,session.Target.Excluded?.ToArray()??Array.Empty<PlayerId>(),session.Objective??"",session.FrequencyWeeks,session.Responsible,session.Opposition,session.Routine,DayKind:TrainingSchedule.Day(plan,session.Date)?.Kind??TrainingDayKind.Single,Turn:TrainingSchedule.Classify(session)))));
        content.AddChild(ClubUi.Text("TrainingEmptyDaysNotice",UiText.Get("TrainingEmptyDaysNotice"),13));
    }
}
