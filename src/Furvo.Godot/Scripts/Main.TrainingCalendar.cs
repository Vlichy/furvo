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
    private readonly Dictionary<string,TrainingDraft> _trainingDrafts=new();
    private DateOnly? _trainingSelectedDay;
    private string? _trainingSelectionContext;
    private bool _trainingChooseArea;
    private bool _trainingFormCollapsed,_trainingExclusionsOpen;
    /// <summary>Une la navegación a la agenda de carrera/equipo y protege los cambios todavía no guardados.</summary>
    private void TrainingNavigate(Action action,bool discardDelegation=false)
    {
        if(discardDelegation&&TrainingGroupDirty){TrainingGroupGuard(()=>TrainingNavigate(action,discardDelegation));return;}
        var key=SportsPath+"|"+SportsTeam;
        void Go(){_trainingDrafts.Remove(key);if(discardDelegation)_trainingDelegationDrafts.Remove(key);action();}
        if((_trainingDrafts.TryGetValue(key,out var pending)&&pending.Dirty)||(discardDelegation&&TrainingDelegationDirty))TrainingConfirm(UiText.Get("TrainingDiscardChanges"),Go);else Go();
    }
    private void TrainingConfirm(string message,Action action)
    {
        var dialog=new ConfirmationDialog{Name="TrainingConfirmDialog",Title=UiText.Get("Brand"),DialogText=message,Exclusive=true,OkButtonText=UiText.Get("PlanConfirm"),CancelButtonText=UiText.Get("PlanCancel")};
        dialog.Confirmed+=()=>{dialog.QueueFree();action();};dialog.Canceled+=()=>dialog.QueueFree();AddChild(dialog);dialog.PopupCentered(new Vector2I(520,180));
    }
    private void RenderTrainingPlanner(VBoxContainer content,Label status,Guid team,TrainingPlan plan,IReadOnlyList<TrainingCandidate> players,IReadOnlyList<TrainingStaff> staff,SqliteSportsStore service,DateOnly today)
    {
        var area=_trainingTab switch{1=>TrainingArea.Field,2=>TrainingArea.Gym,3=>TrainingArea.Goalkeepers,4=>TrainingArea.SetPieces,5=>TrainingArea.Recovery,6=>TrainingArea.Technology,_=>(TrainingArea?)null};
        var policy=_trainingSchedulePolicy;var week=TrainingCalendarWeek.Create(plan,_trainingWeek??today,today).Start;_trainingWeek=week;
        var key=SportsPath+"|"+team;
        var availability=new SqliteTrainingAvailabilityStore(SportsPath).Restrictions();
        if(_trainingSelectionContext!=key){_trainingSelectionContext=key;_trainingSelectedDay=null;}
        if(_trainingSelectedDay is {} outside&&(outside<week||outside>week.AddDays(6)))_trainingSelectedDay=null;
        TrainingDraft Empty(DateOnly day,TrainingTurn? preferred=null){
            var setting=TrainingSchedule.Day(plan,day,policy);var shift=preferred??setting?.SingleTurn??TrainingTurn.Morning;
            var length=45;var suggested=TrainingDailyPlanning.Suggest(plan,day,shift,length,policy);
            var start=suggested?.Start??new TimeOnly(shift==TrainingTurn.Morning?policy.OperatingStartHour:policy.AfternoonHour,0);
            return new(null,day.ToString("yyyy-MM-dd"),start.ToString("HH:mm:ss.fffffff"),length,TrainingCatalog.Exercises.First(e=>area is null||e.Area==area).Code,TrainingLoad.Medium,area==TrainingArea.Goalkeepers?TrainingScope.Position:TrainingScope.Team,null,area==TrainingArea.Goalkeepers?PlayerRole.Goalkeeper:null,null,Array.Empty<PlayerId>(),"",1,null,false,null,DayKind:setting?.Kind??TrainingDayKind.Single,Turn:shift);
        }
        void Guard(Action action){if(_trainingDrafts.TryGetValue(key,out var pending)&&pending.Dirty)TrainingConfirm(UiText.Get("TrainingDiscardChanges"),action);else action();}
        void Replace(TrainingDraft next){Guard(()=>{_trainingDrafts[key]=next;_trainingSelectedDay=DateOnly.ParseExact(next.Date,"yyyy-MM-dd",CultureInfo.InvariantCulture);_trainingFormCollapsed=false;ShowTraining();});}
        void OpenDate(DateOnly day){
            if(_trainingDrafts.TryGetValue(key,out var pending)&&pending.Date==day.ToString("yyyy-MM-dd")){_trainingSelectedDay=day;ShowTraining();return;}
            Replace(Empty(day));
        }
        void OpenBlock(TrainingSession block){
            var destination=TrainingAgenda.TabFor(block);
            if(destination!=_trainingTab){Guard(()=>{_trainingTab=destination;_trainingDrafts.Remove(key);OpenBlock(block);});return;}
            if(_trainingDrafts.TryGetValue(key,out var current)&&current.Editing==block.Id){_trainingSelectedDay=block.Date;_trainingFormCollapsed=false;ShowTraining();return;}
            Replace(new(block.Id,block.Date.ToString("yyyy-MM-dd"),block.Start.ToString("HH:mm:ss.fffffff"),block.Minutes,block.Exercise,block.Load,block.Target.Scope,block.Target.Player,block.Target.Position,block.Target.Group,block.Target.Excluded?.ToArray()??Array.Empty<PlayerId>(),block.Objective??"",block.FrequencyWeeks,block.Responsible,block.Opposition,block.Routine,DayKind:TrainingSchedule.Day(plan,block.Date,policy)?.Kind??TrainingDayKind.Single,Turn:TrainingSchedule.Classify(block,policy)));
        }
        void DeleteBlock(TrainingSession block)=>TrainingConfirm(UiText.Get("TrainingConfirmDelete"),()=>{try{
            service.SavePlan(plan with{Sessions=plan.Sessions.Where(s=>s.Id!=block.Id).ToArray()});
            if(_trainingDrafts.TryGetValue(key,out var editing)&&editing.Editing==block.Id)_trainingDrafts[key]=Empty(block.Date,TrainingSchedule.Classify(block,policy));
            _trainingSelectedDay=block.Date;ShowTraining();
        }catch(Exception error){SportsError(error,status);}});
        void ChangeWeek(int offset)=>Guard(()=>{_trainingDrafts.Remove(key);_trainingSelectedDay=null;_trainingWeek=week.AddDays(offset);ShowTraining();});
        var weekRow=new HFlowContainer{Name="TrainingWeekNavigation"};weekRow.AddThemeConstantOverride("h_separation",8);content.AddChild(weekRow);
        weekRow.AddChild(WindowUi.Button("TrainingPreviousWeek",()=>ChangeWeek(-7)));weekRow.AddChild(WindowUi.Button("TrainingNextWeek",()=>ChangeWeek(7)));
        weekRow.AddChild(ClubUi.Text("TrainingWeek",week.ToString("dd/MM/yyyy")+" – "+week.AddDays(6).ToString("dd/MM/yyyy"),16));
        // La pestaña limita el formulario, nunca la agenda visible ni sus validaciones.
        var completed=new SqliteTrainingExecutionStore(SportsPath).CompletedSessions(team);
        var calendar=TrainingCalendarWeek.Create(plan,week,today,completed:completed);
        string Tooltip(TrainingCalendarEntry entry){
            var block=entry.Session;var groupName=block.Target.Group is {} id?plan.Groups.SingleOrDefault(g=>g.Id==id)?.Name:null;
            return UiText.Get("Exercise_"+block.Exercise)+"\n"+block.Start.ToString("HH:mm")+"–"+block.Ends.ToString("HH:mm")+" · "+block.Minutes+" min · "+UiText.Get("TrainingLoad_"+block.Load)+"\n"+UiText.Get("TrainingArea_"+entry.Area)+" · "+UiText.Get("TrainingSessionState_"+entry.State)+"\n"+UiText.Get("TrainingParticipantsLabel")+": "+block.Participants.Count+(groupName is null?"":" · "+groupName);
        }
        content.AddChild(new TrainingWeekCalendar(calendar,Tooltip,OpenDate,OpenBlock,_trainingSelectedDay));
        if(_trainingTab==0)RenderTrainingAgendaSummary(content,TrainingAgenda.ReadWeek(plan,week));
        content.AddChild(ClubUi.Text("TrainingEmptyDaysNotice",UiText.Get("TrainingEmptyDaysNotice"),13));
        if(_trainingSelectedDay is not {} selectedDay)return;
        if(_trainingTab==0){
            var summaryDay=TrainingDailyPlanning.Read(plan,selectedDay,null,policy);
            RenderTrainingDayPanel(content,summaryDay,plan,players,staff,OpenBlock,DeleteBlock,()=>{_trainingChooseArea=true;ShowTraining();});
            var controls=SportsCard(content,"TrainingSummaryActions");controls.Name="TrainingSummaryActions";
            if(_trainingChooseArea){
                var category=new OptionButton{Name="TrainingSummaryCategory",SizeFlagsHorizontal=SizeFlags.ExpandFill};
                var areas=Enum.GetValues<TrainingArea>();foreach(var choice in areas)category.AddItem(UiText.Get("TrainingArea_"+choice));controls.AddChild(category);
                controls.AddChild(WindowUi.Button("TrainingSummaryOpenCategory",()=>{_trainingTab=(int)areas[category.Selected];_trainingChooseArea=false;_trainingDrafts.Remove(key);ShowTraining();}));
            }
            controls.AddChild(WindowUi.Button("TrainingConfirmRestDay",()=>TrainingConfirm(UiText.Get(summaryDay.GlobalCount>0?"TrainingConfirmRestReplacement":"TrainingConfirmRestEmpty"),()=>{
                try{service.SavePlan(TrainingSchedule.SetDay(plan,new(selectedDay,TrainingDayKind.Rest),true) with{RequiresReview=false});_trainingDrafts.Remove(key);_trainingChooseArea=false;ShowTraining();}catch(Exception error){SportsError(error,status);}
            })));
            return;
        }
        content.AddChild(WindowUi.Button("TrainingBackSummary",()=>TrainingNavigate(()=>{_trainingTab=0;ShowTraining();})));
        if(!_trainingDrafts.TryGetValue(key,out var draft)||draft.Date!=selectedDay.ToString("yyyy-MM-dd"))draft=Empty(selectedDay);
        else if(!draft.Dirty&&draft.Editing is null)draft=draft with{Exercise=TrainingCatalog.Exercises.First(e=>area is null||e.Area==area).Code};
        _trainingDrafts[key]=draft;
        var daily=TrainingDailyPlanning.Read(plan,selectedDay,policy:policy);
        RenderTrainingDayPanel(content,daily,plan,players,staff,OpenBlock,DeleteBlock,()=>Replace(Empty(selectedDay,_trainingDrafts[key].Turn) with{DayKind=daily.Configuration?.Kind==TrainingDayKind.Rest?TrainingDayKind.Single:daily.Configuration?.Kind??TrainingDayKind.Single}));
        var section=SportsCard(content,"TrainingCreate");section.Name="TrainingSessionForm";
        section.AddChild(ClubUi.Text("TrainingCreateTitle",UiText.Get(draft.Editing is null?"TrainingDailyAddExercise":"TrainingEditing"),18,"f1b96b"));
        var fields=new VBoxContainer{Name="TrainingFormFields",Visible=!_trainingFormCollapsed};
        var toggle=WindowUi.Button(_trainingFormCollapsed?"TrainingExpandForm":"TrainingCollapseForm",()=>{_trainingFormCollapsed=!_trainingFormCollapsed;ShowTraining();});section.AddChild(toggle);section.AddChild(fields);
        GridContainer FormGrid(int wideColumns){var created=new GridContainer{Columns=GetViewportRect().Size.X<1400?2:wideColumns,SizeFlagsHorizontal=SizeFlags.ExpandFill};created.Resized+=()=>{var count=GetViewportRect().Size.X<1400?2:wideColumns;if(created.Columns!=count)created.Columns=count;};created.AddThemeConstantOverride("h_separation",12);created.AddThemeConstantOverride("v_separation",8);fields.AddChild(created);return created;}
        var grid=FormGrid(3);
        VBoxContainer Field(string label,Control control,GridContainer? target=null){var box=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};box.AddChild(ClubUi.Text(label+"Label",UiText.Get(label),14));control.SizeFlagsHorizontal=SizeFlags.ExpandFill;box.AddChild(control);(target??grid).AddChild(box);return box;}
        var date=new LineEdit{Name="TrainingDate",Text=draft.Date,Editable=false};Field("TrainingDateLabel",date);
        var dayKind=new OptionButton{Name="TrainingDayKind"};foreach(var kind in Enum.GetValues<TrainingDayKind>())dayKind.AddItem(UiText.Get("TrainingDayKind_"+kind));dayKind.Selected=(int)draft.DayKind-1;Field("TrainingDayKindLabel",dayKind);
        var turn=new OptionButton{Name="TrainingTurn"};turn.AddItem(UiText.Get("TrainingTurnPending"));turn.AddItem(UiText.Get("TrainingTurn_Morning"));turn.AddItem(UiText.Get("TrainingTurn_Afternoon"));turn.Selected=draft.Turn is {} shift?(int)shift:0;turn.SetItemDisabled(0,draft.Editing is null||draft.Turn is not null);var turnBox=Field("TrainingTurnLabel",turn);
        OptionButton Clock(string name,TimeSpan value){
            var control=new OptionButton{Name=name,FitToLongestItem=false,CustomMinimumSize=new Vector2(160,0)};TrainingUi.LimitPopup(control);var values=Enumerable.Range(0,(policy.OperatingEndHour-policy.OperatingStartHour)*60/policy.TimeStepMinutes+1).Select(i=>TimeSpan.FromMinutes(policy.OperatingStartHour*60+i*policy.TimeStepMinutes)).Append(value).Distinct().Order().ToArray();
            foreach(var item in values){control.AddItem(item==TimeSpan.FromDays(1)?"24:00":item.ToString(item.Seconds==0?@"hh\:mm":@"hh\:mm\:ss"));control.SetItemMetadata(control.ItemCount-1,item.Ticks);}control.Selected=Array.IndexOf(values,value);return control;
        }
        var exerciseFields=new VBoxContainer{Name="TrainingExerciseFields"};fields.AddChild(exerciseFields);
        var clocks=FormGrid(2);clocks.Name="TrainingClockRow";clocks.SizeFlagsHorizontal=SizeFlags.ShrinkBegin;fields.RemoveChild(clocks);exerciseFields.AddChild(clocks);
        var initial=TimeOnly.Parse(draft.Time,CultureInfo.InvariantCulture);var time=Clock("TrainingTime",initial.ToTimeSpan());var startBox=Field("TrainingStartLabel",time,clocks);
        var end=Clock("TrainingEndTime",initial.ToTimeSpan()+TimeSpan.FromMinutes(draft.Minutes));var endBox=Field("TrainingEndLabel",end,clocks);
        var duration=new Label{Name="TrainingCalculatedDuration"};var durationBox=new VBoxContainer();durationBox.AddChild(ClubUi.Text("TrainingDurationLabel",UiText.Get("TrainingDuration"),13));durationBox.AddChild(duration);exerciseFields.AddChild(durationBox);
        var timeError=ClubUi.Text("TrainingTimeError","",14,"e3a09b");exerciseFields.AddChild(timeError);Button? saveBlock=null;TrainingRoutinePicker? routinePicker=null;bool availabilityInvalid=false;
        int Minutes()=>(int)(TimeSpan.FromTicks(end.GetItemMetadata(end.Selected).AsInt64())-TimeSpan.FromTicks(time.GetItemMetadata(time.Selected).AsInt64())).TotalMinutes;
        void Duration(){var invalid=Minutes()<=0;duration.Text=invalid?"—":Minutes()+" min";timeError.Text=invalid&&dayKind.Selected!=0?UiText.Get("TrainingDailyInvalidRange"):"";if(saveBlock is not null)saveBlock.Disabled=dayKind.Selected!=0&&(invalid||availabilityInvalid||routinePicker is not null&&!routinePicker.Valid);}Duration();
        Label? proposalLabel=null;
        void UpdateProposal(){if(proposalLabel is not null)proposalLabel.Visible=false;}
        if(draft.Editing is null&&!draft.Dirty){
            var proposal=TrainingDailyPlanning.Suggest(plan,selectedDay,draft.Turn??TrainingTurn.Morning,Math.Max(1,draft.Minutes),policy);
            proposalLabel=ClubUi.Text("TrainingDailyProposal",proposal is null?UiText.Get("TrainingDailyNoProposal"):UiText.Format("TrainingDailyProposal",proposal.Start.ToString("HH:mm"),proposal.End.ToString(@"hh\:mm")),13);exerciseFields.AddChild(proposalLabel);
        }
        grid=FormGrid(4);fields.RemoveChild(grid);exerciseFields.AddChild(grid);
        var exercises=TrainingCatalog.Exercises.Where(e=>area is null||e.Area==area).ToArray();var exercise=new OptionButton{Name="TrainingExercise",ClipText=true,FitToLongestItem=false};foreach(var e in exercises)exercise.AddItem(UiText.Get("Exercise_"+e.Code));exercise.Selected=Array.FindIndex(exercises,e=>e.Code==draft.Exercise);var exerciseBox=Field("TrainingExerciseLabel",exercise);
        // Los ejercicios específicos de porteros admiten solo selección individual o por demarcación.
        var scopes=area==TrainingArea.Goalkeepers?new[]{TrainingScope.Individual,TrainingScope.Position}:Enum.GetValues<TrainingScope>();
        var scope=new OptionButton{Name="TrainingScope"};foreach(var item in scopes){scope.AddItem(UiText.Get("TrainingScope_"+item));scope.SetItemMetadata(scope.ItemCount-1,(int)item);}scope.Selected=Array.IndexOf(scopes,draft.Scope);
        TrainingScope SelectedScope()=>scope.Selected>=0?scopes[scope.Selected]:draft.Scope;var scopeBox=Field("TrainingParticipantsLabel",scope);
        var responsible=new OptionButton{Name="TrainingResponsible",ClipText=true};var staffBox=Field("TrainingResponsibleLabel",responsible);
        void Staff(Guid? selected){
            responsible.Clear();responsible.AddItem(UiText.Get("TrainingUnassignedResponsible"));responsible.SetItemMetadata(0,"");
            var test=new TrainingSession(Guid.Empty,team,today,new(9,0),1,exercises[exercise.Selected].Code,TrainingLoad.Medium,new(SelectedScope()),Array.Empty<PlayerId>());
            foreach(var member in TrainingSchedule.Eligible(staff,test)){responsible.AddItem(member.Name+" — "+UiText.Get("AnalysisStaff_"+member.Role));responsible.SetItemMetadata(responsible.ItemCount-1,member.Id.ToString());}
            var index=Enumerable.Range(1,Math.Max(0,responsible.ItemCount-1)).FirstOrDefault(i=>responsible.GetItemMetadata(i).AsString()==selected?.ToString());
            if(selected is {} missing&&index==0){responsible.AddItem((staff.SingleOrDefault(p=>p.Id==missing)?.Name??UiText.Get("Unknown"))+" — "+UiText.Get("TrainingStaffIneligible"));index=responsible.ItemCount-1;responsible.SetItemMetadata(index,missing.ToString());responsible.SetItemDisabled(index,true);}responsible.Selected=index;
        }
        Guid? Responsible()=>responsible.Selected>0&&Guid.TryParse(responsible.GetItemMetadata(responsible.Selected).AsString(),out var id)?id:null;Staff(draft.Responsible);
        var intensity=new OptionButton{Name="TrainingIntensity"};var loads=Enum.GetValues<TrainingLoad>().Where(l=>l!=TrainingLoad.Rest).ToArray();foreach(var load in loads)intensity.AddItem(UiText.Get("TrainingLoad_"+load));intensity.Selected=Math.Max(0,Array.IndexOf(loads,draft.Load));var intensityBox=Field("TrainingIntensityLabel",intensity);
        var frequency=new SpinBox{Name="TrainingFrequency",MinValue=1,MaxValue=52,Step=1,Value=draft.Frequency};Field("TrainingFrequency",frequency);
        var candidates=players.Where(p=>area!=TrainingArea.Goalkeepers||p.Positions?.Primary==PlayerRole.Goalkeeper).ToArray();
        var person=new OptionButton{Name="TrainingPlayer",ClipText=true,FitToLongestItem=false};foreach(var p in candidates){person.AddItem(TrainingUi.PlayerCaption(p));person.SetItemMetadata(person.ItemCount-1,p.Id.ToString());}TrainingUi.LimitPopup(person);person.Selected=Math.Max(0,candidates.ToList().FindIndex(p=>p.Id==draft.Player));var personBox=Field("TrainingPlayerLabel",person);grid.RemoveChild(personBox);exerciseFields.AddChild(personBox);
        var role=new OptionButton{Name="TrainingPosition",ClipText=true};var roles=area==TrainingArea.Goalkeepers?new[]{PlayerRole.Goalkeeper}:Enum.GetValues<PlayerRole>();foreach(var p in roles)role.AddItem(UiText.Get("Position_"+p));role.Selected=draft.Position is {} position?Array.IndexOf(roles,position):0;var roleBox=Field("TrainingPositionLabel",role);
        var group=new OptionButton{Name="TrainingGroup",ClipText=true};foreach(var g in plan.Groups)group.AddItem(g.Name);group.Selected=plan.Groups.Count==0?-1:draft.Group is {} requestedGroup?plan.Groups.ToList().FindIndex(g=>g.Id==requestedGroup):0;var groupBox=Field("TrainingGroupLabel",group);
        var tacticalSettings=service.TrainingTactics(team);routinePicker=new TrainingRoutinePicker();routinePicker.Configure(draft.Exercise,tacticalSettings,draft.Routine);var routineBox=Field("TrainingRoutineLabel",routinePicker);
        void Scope(){var active=dayKind.Selected!=0;exerciseFields.Visible=active;if(saveBlock is not null)saveBlock.Text=UiText.Get(active?"TrainingSaveSession":"TrainingConfirmRestDay");personBox.Visible=active&&SelectedScope()==TrainingScope.Individual;roleBox.Visible=active&&SelectedScope()==TrainingScope.Position;groupBox.Visible=active&&SelectedScope()==TrainingScope.Group;routineBox.Visible=active&&TrainingCatalog.Get(exercises[exercise.Selected].Code).Area==TrainingArea.SetPieces;foreach(var box in new[]{turnBox,startBox,endBox,durationBox,exerciseBox,scopeBox,staffBox,intensityBox})box.Visible=active;}Scope();
        var exclude=new ItemList{Name="TrainingExcluded",SelectMode=ItemList.SelectModeEnum.Multi,CustomMinimumSize=new Vector2(0,90),Visible=_trainingExclusionsOpen};TrainingUi.AddPlayers(exclude,players);for(var i=0;i<players.Count;i++)if(draft.Excluded.Contains(players[i].Id))exclude.Select(i,false);
        var exclusionToggle=WindowUi.Button("TrainingToggleExclusions",()=>{_trainingExclusionsOpen=!_trainingExclusionsOpen;exclude.Visible=_trainingExclusionsOpen;});
        exerciseFields.AddChild(ClubUi.Text("TrainingObjectiveLabel",UiText.Get("TrainingObjective"),14));var objective=new LineEdit{Name="TrainingObjective",Text=draft.Objective};exerciseFields.AddChild(objective);exerciseFields.AddChild(exclusionToggle);exerciseFields.AddChild(exclude);
        var availabilityPreview=ClubUi.Text("TrainingAvailabilityPreview","",13,"f1b96b");exerciseFields.AddChild(availabilityPreview);
        var opposition=new PriorityChoice(UiText.Get("TrainingOpposition"),draft.Opposition){Name="TrainingOppositionRow"};opposition.Indicator.Name="TrainingOpposition";exerciseFields.AddChild(opposition);
        TrainingDraft Capture()=>new(draft.Editing,date.Text,TimeOnly.FromTimeSpan(TimeSpan.FromTicks(time.GetItemMetadata(time.Selected).AsInt64())).ToString("HH:mm:ss.fffffff"),Minutes(),exercises[exercise.Selected].Code,loads[intensity.Selected],SelectedScope(),
            SelectedScope()==TrainingScope.Individual&&person.Selected>=0?candidates[person.Selected].Id:null,SelectedScope()==TrainingScope.Position?roles[role.Selected]:null,SelectedScope()==TrainingScope.Group&&group.Selected>=0?plan.Groups[group.Selected].Id:null,
            exclude.GetSelectedItems().Select(i=>players[i].Id).ToArray(),objective.Text,(int)frequency.Value,Responsible(),opposition.Checked,
            routineBox.Visible?routinePicker.Reference:null,true,(TrainingDayKind)(dayKind.Selected+1),turn.Selected==0?null:(TrainingTurn)turn.Selected);
        void UpdateAvailability(){
            var code=exercises[exercise.Selected].Code;
            for(var i=0;i<candidates.Length;i++){var decision=TrainingAvailability.Check(candidates[i].Id,selectedDay,code,availability);person.SetItemText(i,TrainingUi.PlayerCaption(candidates[i])+(decision.State==TrainingAvailabilityState.Available?"":" — "+AvailabilityCaption(decision)));person.SetItemDisabled(i,!decision.Allowed);}
            for(var i=0;i<players.Count;i++){var decision=TrainingAvailability.Check(players[i].Id,selectedDay,code,availability);exclude.SetItemText(i,TrainingUi.PlayerCaption(players[i])+(decision.State==TrainingAvailabilityState.Available?"":" — "+AvailabilityCaption(decision)));}
            try{
                var value=Capture();var target=new TrainingTarget(value.Scope,value.Player,value.Position,value.Group,value.Excluded);
                var selection=TrainingAvailability.Select(target,players,plan.Groups,selectedDay,code,availability);
                availabilityInvalid=selection.Included.Count==0||value.Scope==TrainingScope.Individual&&selection.Excluded.Count>0;
                availabilityPreview.Text=selection.Excluded.Count==0?"":UiText.Get("TrainingAvailabilityExcluded")+"\n"+string.Join("\n",selection.Excluded.Select(pair=>TrainingUi.PlayerCaption(players.Single(p=>p.Id==pair.Key))+" — "+AvailabilityCaption(pair.Value)))+"\n"+UiText.Get("TrainingAvailabilityConfirmExclusions");
            }catch(InvalidDataException){availabilityInvalid=false;availabilityPreview.Text="";}
            Duration();
        }
        void Changed(){_trainingDrafts[key]=Capture();UpdateAvailability();}
        date.TextChanged+=_=>Changed();time.ItemSelected+=_=>{Duration();UpdateProposal();Changed();};end.ItemSelected+=_=>{Duration();UpdateProposal();Changed();};dayKind.ItemSelected+=_=>{Scope();Duration();Changed();};turn.ItemSelected+=_=>{UpdateProposal();Changed();};intensity.ItemSelected+=_=>Changed();exercise.ItemSelected+=_=>{var owner=Responsible();routinePicker.Configure(exercises[exercise.Selected].Code,tacticalSettings,routinePicker.Reference);Scope();Staff(owner);Duration();Changed();};scope.ItemSelected+=_=>{var owner=Responsible();Scope();Staff(owner);Changed();};responsible.ItemSelected+=_=>Changed();frequency.ValueChanged+=_=>Changed();person.ItemSelected+=_=>Changed();role.ItemSelected+=_=>Changed();group.ItemSelected+=_=>Changed();routinePicker.Changed+=()=>{Duration();Changed();};exclude.MultiSelected+=(_,_)=>Changed();objective.TextChanged+=_=>Changed();opposition.Indicator.Toggled+=_=>Changed();
        if(draft.Editing is {} legacyId&&plan.Sessions.Any(s=>s.Id==legacyId&&TrainingSchedule.Classify(s) is null))exerciseFields.AddChild(ClubUi.Text("TrainingLegacyNotice",UiText.Get("TrainingLegacyNotice"),14,"f1b96b"));
        var formStatus=ClubUi.Text("TrainingFormStatus","",14,"f1b96b");fields.AddChild(formStatus);
        var actions=new HFlowContainer{Name="TrainingFormActions"};fields.AddChild(actions);
        saveBlock=WindowUi.Button("TrainingSaveSession",()=>{try{
            if(scope.Selected<0)throw new InvalidDataException(UiText.Get("TrainingChooseKeeperScope"));
            var value=Capture();if(!DateOnly.TryParseExact(value.Date,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var day)||!TimeOnly.TryParse(value.Time,CultureInfo.InvariantCulture,DateTimeStyles.None,out var start))throw new InvalidDataException(UiText.Get("TrainingInvalidDate"));
            if(day!=selectedDay)throw new InvalidDataException(UiText.Get("TrainingDailyDateFixed"));
            if(value.Editing is {} missing&&!plan.Sessions.Any(s=>s.Id==missing))throw new InvalidOperationException(UiText.Get("TrainingMissingSession"));
            TrainingPlan candidate=plan;var config=new TrainingDayPlan(day,value.DayKind,value.DayKind==TrainingDayKind.Single?value.Turn:null);
            if(value.DayKind!=TrainingDayKind.Rest){
                TrainingSchedule.Duration(start,TimeSpan.FromTicks(end.GetItemMetadata(end.Selected).AsInt64()));
                var target=new TrainingTarget(value.Scope,value.Player,value.Position,value.Group,value.Excluded);var e=TrainingCatalog.Get(value.Exercise);
                var selection=TrainingAvailability.Select(target,players,plan.Groups,day,value.Exercise,availability);
                if(selection.Included.Count==0||value.Scope==TrainingScope.Individual&&selection.Excluded.Count>0)throw new InvalidDataException(UiText.Get("TrainingAvailabilityBlocked"));
                // Confirmación explícita del formulario: la exclusión se aplica a este ejercicio, nunca al grupo persistido.
                target=target with{Excluded=value.Excluded.Concat(selection.Excluded.Keys).Distinct().ToArray()};
                var session=new TrainingSession(value.Editing??Guid.NewGuid(),team,day,start,value.Minutes,value.Exercise,value.Load,target,TrainingRules.Resolve(target,players,plan.Groups),value.Responsible,value.Opposition,value.Routine,e.Area==TrainingArea.Technology?service.Facility().Id:null,string.IsNullOrWhiteSpace(value.Objective)?UiText.Get("Exercise_"+e.Code):value.Objective,value.Frequency,value.Turn);
                candidate=plan with{Sessions=plan.Sessions.Where(s=>s.Id!=session.Id).Append(session).ToArray()};
            }
            void Commit(bool remove){try{var legacyUnclassified=value.Editing is {} id&&value.Turn is null&&value.DayKind==TrainingDayKind.Single&&plan.Sessions.Any(s=>s.Id==id&&s.Date==day&&s.Start==start&&s.Minutes==value.Minutes&&TrainingSchedule.Classify(s) is null);var next=legacyUnclassified?candidate:TrainingSchedule.SetDay(candidate,config,remove);var saved=service.SavePlan(next with{RequiresReview=false});var fresh=Empty(day,value.Turn) with{DayKind=value.DayKind};var proposed=TrainingDailyPlanning.Suggest(saved,day,value.Turn??TrainingTurn.Morning,fresh.Minutes,policy);_trainingDrafts[key]=fresh with{Time=(proposed?.Start??TimeOnly.Parse(fresh.Time,CultureInfo.InvariantCulture)).ToString("HH:mm:ss.fffffff")};ShowTraining();}catch(Exception error){SportsError(error,formStatus);}}
            var incompatible=TrainingSchedule.Incompatible(candidate,config);
            if(incompatible.Count>0)TrainingConfirm(value.DayKind==TrainingDayKind.Rest?UiText.Get("TrainingConfirmRestReplacement"):UiText.Format("TrainingConfirmDayChange",incompatible.Count),()=>Commit(true));else Commit(false);
        }catch(Exception error){SportsError(error,formStatus);}});actions.AddChild(saveBlock);Scope();UpdateAvailability();
        actions.AddChild(WindowUi.Button("TrainingCancelSession",()=>Replace(Empty(selectedDay,_trainingDrafts[key].Turn))));
        if(draft.Editing is {} editing)actions.AddChild(WindowUi.Button("TrainingDeleteSession",()=>DeleteBlock(plan.Sessions.Single(s=>s.Id==editing))));
    }
}
