using System.Globalization;
using Furvo.Core.Domain;
using Furvo.Core.Development;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Godot;
namespace Furvo.Presentation;
public partial class Main
{
    private static string AvailabilityCaption(TrainingAvailabilityDecision decision)=>UiText.Get("TrainingAvailability_"+decision.State)+
        (decision.Restrictions.Count==0?"":" · "+string.Join("; ",decision.Restrictions.Select(r=>UiText.Get("TrainingRestriction_"+r.Reason))));
    private static string AvailabilityConflictCaption(TrainingAvailabilityConflict conflict,IReadOnlyList<TrainingCandidate> players)
        =>conflict.Date.ToString("dd/MM/yyyy")+" · "+UiText.Get("Exercise_"+conflict.Exercise)+" · "+(players.SingleOrDefault(p=>p.Id==conflict.Player) is {} p?TrainingUi.PlayerCaption(p):conflict.Player.ToString())+" — "+AvailabilityCaption(conflict.Decision);
    private void RenderTrainingAvailability(VBoxContainer content,Guid team,IReadOnlyList<TrainingCandidate> players)
    {
        var store=new SqliteTrainingAvailabilityStore(SportsPath);var conflicts=store.Conflicts(team);
        // El acceso compacto conserva el espacio del calendario, especialmente al contraer Delegación.
        var box=new VBoxContainer{Name="TrainingAvailabilityPanel"};content.AddChild(box);
        var open=WindowUi.Button("TrainingAvailabilityOpen",()=>OpenTrainingAvailability(players));open.TooltipText=UiText.Get("TrainingAvailabilityNotice");box.AddChild(open);
        if(conflicts.Count>0){
            box.AddChild(ClubUi.Text("TrainingAvailabilityWarnings",UiText.Get("TrainingAvailabilityWarnings"),14,"f1b96b"));
            var scroll=new ScrollContainer{Name="TrainingAvailabilityWarningsScroll",HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled,CustomMinimumSize=new Vector2(0,85)};box.AddChild(scroll);
            var rows=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};scroll.AddChild(rows);
            foreach(var conflict in conflicts)rows.AddChild(ClubUi.Text("TrainingAvailabilityConflict",AvailabilityConflictCaption(conflict,players),13,"f1b96b"));
        }
    }
    /// <summary>Edición administrativa explícita, en una ventana modal; ninguna restricción produce efectos médicos.</summary>
    private void OpenTrainingAvailability(IReadOnlyList<TrainingCandidate> players)
    {
        var store=new SqliteTrainingAvailabilityStore(SportsPath);var all=store.Restrictions();TrainingRestriction? editing=null;
        var dialog=new ConfirmationDialog{Name="TrainingAvailabilityDialog",Title=UiText.Get("TrainingAvailabilityTitle"),Exclusive=true,DialogHideOnOk=false,OkButtonText=UiText.Get("TrainingAvailabilitySave"),CancelButtonText=UiText.Get("PlanCancel")};
        AddChild(dialog);var scroll=new ScrollContainer{Name="TrainingAvailabilityEditorScroll",HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled,CustomMinimumSize=new Vector2(650,390)};dialog.AddChild(scroll);
        var body=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};body.AddThemeConstantOverride("separation",8);scroll.AddChild(body);
        body.AddChild(ClubUi.Text("TrainingAvailabilityNotice",UiText.Get("TrainingAvailabilityNotice"),12));
        void Field(string key,Control control){body.AddChild(ClubUi.Text(key+"Label",UiText.Get(key),13));control.SizeFlagsHorizontal=SizeFlags.ExpandFill;body.AddChild(control);}
        var player=new OptionButton{Name="AvailabilityPlayer",FitToLongestItem=false,ClipText=true};foreach(var p in players){player.AddItem(TrainingUi.PlayerCaption(p));player.SetItemMetadata(player.ItemCount-1,p.Id.ToString());}TrainingUi.LimitPopup(player);Field("TrainingPlayerLabel",player);
        var entries=new OptionButton{Name="AvailabilityEntries",FitToLongestItem=false,ClipText=true};TrainingUi.LimitPopup(entries);Field("TrainingAvailabilityRecords",entries);
        var state=new OptionButton{Name="AvailabilityState"};foreach(var value in new[]{TrainingAvailabilityState.Unavailable,TrainingAvailabilityState.Partial})state.AddItem(UiText.Get("TrainingAvailability_"+value));Field("TrainingAvailabilityState",state);
        var reason=new OptionButton{Name="AvailabilityReason"};foreach(var value in Enum.GetValues<TrainingRestrictionReason>())reason.AddItem(UiText.Get("TrainingRestriction_"+value));Field("TrainingAvailabilityReason",reason);
        var dates=new GridContainer{Columns=2,SizeFlagsHorizontal=SizeFlags.ExpandFill};body.AddChild(dates);
        LineEdit DateField(string name,string key,string value){var box=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};dates.AddChild(box);box.AddChild(ClubUi.Text(key+"Label",UiText.Get(key),13));var field=new LineEdit{Name=name,Text=value,PlaceholderText="AAAA-MM-DD"};box.AddChild(field);return field;}
        var start=DateField("AvailabilityStart","TrainingAvailabilityStart",Sports().CareerDate().ToString("yyyy-MM-dd"));var end=DateField("AvailabilityEnd","TrainingAvailabilityEnd","");
        var notes=new LineEdit{Name="AvailabilityNotes"};Field("TrainingAvailabilityNotes",notes);var origin=new LineEdit{Name="AvailabilityOrigin"};Field("TrainingAvailabilityOrigin",origin);
        var allowed=new ItemList{Name="AvailabilityAllowedExercises",SelectMode=ItemList.SelectModeEnum.Multi,CustomMinimumSize=new Vector2(0,105)};
        foreach(var e in TrainingCatalog.Exercises){allowed.AddItem(UiText.Get("TrainingArea_"+e.Area)+" · "+UiText.Get("Exercise_"+e.Code));allowed.SetItemMetadata(allowed.ItemCount-1,e.Code);}
        Field("TrainingAvailabilityAllowed",allowed);var allowedLabel=(Control)body.GetChild(body.GetChildCount()-2);var notice=ClubUi.Text("AvailabilityPartialNotice",UiText.Get("TrainingAvailabilityPartialNotice"),12,"f1b96b");body.AddChild(notice);
        var status=ClubUi.Text("AvailabilityStatus","",14,"f1b96b");body.AddChild(status);
        var finish=WindowUi.Button("TrainingAvailabilityFinish",()=>{});finish.Name="AvailabilityFinish";body.AddChild(finish);
        PlayerId Selected()=>players[player.Selected].Id;
        void Mode(){allowedLabel.Visible=state.Selected==1;allowed.Visible=state.Selected==1;notice.Visible=state.Selected==1;}
        void Load(){
            editing=entries.Selected>0?all.Single(r=>r.Id.ToString()==entries.GetItemMetadata(entries.Selected).AsString()):null;
            state.Selected=editing?.State==TrainingAvailabilityState.Partial?1:0;reason.Selected=editing is null?0:(int)editing.Reason-1;
            start.Text=(editing?.Start??Sports().CareerDate()).ToString("yyyy-MM-dd");end.Text=editing?.End?.ToString("yyyy-MM-dd")??"";notes.Text=editing?.Notes??"";origin.Text=editing?.Origin??"";
            allowed.DeselectAll();for(var i=0;i<allowed.ItemCount;i++)if(editing?.AllowedExercises?.Contains(allowed.GetItemMetadata(i).AsString())==true)allowed.Select(i,false);
            finish.Disabled=editing is null;status.Text="";Mode();
        }
        void Records(){entries.Clear();entries.AddItem(UiText.Get("TrainingAvailabilityNew"));foreach(var r in all.Where(r=>r.Player==Selected())){entries.AddItem(r.Start.ToString("dd/MM/yyyy")+" – "+(r.End?.ToString("dd/MM/yyyy")??UiText.Get("TrainingAvailabilityNoEnd"))+" · "+UiText.Get("TrainingRestriction_"+r.Reason));entries.SetItemMetadata(entries.ItemCount-1,r.Id.ToString());}entries.Selected=0;Load();}
        DateOnly Parse(string text){if(!DateOnly.TryParseExact(text,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var result))throw new InvalidDataException(UiText.Get("TrainingAvailabilityInvalidDate"));return result;}
        dialog.Confirmed+=()=>{try{
            var value=new TrainingRestriction(editing?.Id??Guid.NewGuid(),Selected(),state.Selected==1?TrainingAvailabilityState.Partial:TrainingAvailabilityState.Unavailable,(TrainingRestrictionReason)(reason.Selected+1),Parse(start.Text),string.IsNullOrWhiteSpace(end.Text)?null:Parse(end.Text),notes.Text,origin.Text,
                state.Selected==1?allowed.GetSelectedItems().Select(i=>allowed.GetItemMetadata(i).AsString()).ToArray():null,editing?.Revision??0);
            store.Save(value);dialog.QueueFree();ShowTraining();
        }catch(Exception error){status.Text=error.Message;}};
        finish.Pressed+=()=>{try{if(editing is null)return;store.Finish(editing.Id,Parse(end.Text),editing.Revision);dialog.QueueFree();ShowTraining();}catch(Exception error){status.Text=error.Message;}};
        dialog.Canceled+=()=>dialog.QueueFree();player.ItemSelected+=_=>Records();entries.ItemSelected+=_=>Load();state.ItemSelected+=_=>Mode();Records();
        void Size(){var viewport=GetViewportRect().Size;scroll.CustomMinimumSize=new Vector2(Math.Min(720,viewport.X-80),Math.Max(280,Math.Min(540,viewport.Y-150)));dialog.PopupCentered();}
        GetViewport().SizeChanged+=Size;dialog.TreeExiting+=()=>GetViewport().SizeChanged-=Size;Size();
    }
}
