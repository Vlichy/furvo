using Furvo.Core.Development;
using Furvo.Core.Tactics;
using Godot;
namespace Furvo.Presentation.Components;
/// <summary>Selectores dependientes sobre la táctica real. Una ubicación pendiente se identifica y no permite guardar.</summary>
public partial class TrainingRoutinePicker:VBoxContainer
{
    private readonly OptionButton _kind=new(){Name="TrainingRoutine",FitToLongestItem=false,ClipText=true};
    private readonly OptionButton _location=new(){Name="TrainingRoutineLocation",FitToLongestItem=false,ClipText=true};
    private readonly Label _description=ClubUi.Text("TrainingRoutineDescription","",13,"f1b96b");
    private SetPieceKind[] _kinds=Array.Empty<SetPieceKind>();private FreeKickLocation?[] _locations=Array.Empty<FreeKickLocation?>();private TacticalSettings? _settings;
    public event Action? Changed;
    public RoutineReference? Reference=>_kind.Selected<0||_kinds.Length==0?null:new(_kinds[_kind.Selected],_location.Visible&&_location.Selected>=0?_locations[_location.Selected]:null);
    public bool Valid=>_kinds.Length==0||Reference is {} reference&&TrainingRoutines.Designed(_settings,reference);
    public TrainingRoutinePicker(){AddChild(_kind);AddChild(_location);AddChild(_description);_kind.ItemSelected+=_=>{Locations(null,false);Describe();Changed?.Invoke();};_location.ItemSelected+=_=>{Describe();Changed?.Invoke();};}
    public void Configure(string exercise,TacticalSettings? settings,RoutineReference? preferred)
    {
        _settings=settings;_kinds=TrainingRoutines.Compatible(exercise).ToArray();_kind.Clear();
        foreach(var kind in _kinds){var available=TrainingRoutines.Located(kind)?Enum.GetValues<FreeKickLocation>().Any(l=>TrainingRoutines.Designed(settings,new(kind,l)))||preferred?.Kind==kind&&preferred.Location is null&&TrainingRoutines.Designed(settings,preferred):TrainingRoutines.Designed(settings,new(kind));_kind.AddItem(UiText.Get("SetPiece_"+kind)+(available?"":" — "+UiText.Get("TrainingRoutinePending")));_kind.SetItemDisabled(_kind.ItemCount-1,!available);}
        if(_kinds.Length==0){_kind.Selected=-1;_location.Visible=false;_description.Text="";return;}
        var desired=preferred is {} value?Array.IndexOf(_kinds,value.Kind):-1;_kind.Selected=desired>=0?desired:Enumerable.Range(0,_kinds.Length).FirstOrDefault(i=>!_kind.IsItemDisabled(i));
        Locations(preferred?.Location,preferred is {} old&&old.Kind==_kinds[_kind.Selected]&&old.Location is null);Describe();
    }
    private void Locations(FreeKickLocation? preferred,bool legacy)
    {
        var kind=_kinds[_kind.Selected];_location.Visible=TrainingRoutines.Located(kind);_location.Clear();if(!_location.Visible){_locations=Array.Empty<FreeKickLocation?>();return;}
        _locations=(legacy?new FreeKickLocation?[]{null}:Array.Empty<FreeKickLocation?>()).Concat(Enum.GetValues<FreeKickLocation>().Select(l=>(FreeKickLocation?)l)).ToArray();
        foreach(var location in _locations){var designed=TrainingRoutines.Designed(_settings,new(kind,location));_location.AddItem((location is {} place?UiText.Get("FreeKickLocation_"+place):UiText.Get("TrainingRoutineLegacy"))+(designed?"":" — "+UiText.Get("TrainingRoutinePending")));}
        var selected=legacy?0:preferred is {} known?Array.IndexOf(_locations,known):-1;
        if(selected<0)selected=Array.FindIndex(_locations,l=>TrainingRoutines.Designed(_settings,new(kind,l)));_location.Selected=Math.Max(0,selected);
    }
    private void Describe()
    {
        if(Reference is not {} reference){_description.Text="";return;}
        if(!TrainingRoutines.Designed(_settings,reference)){_description.Text=UiText.Get(TrainingRoutines.Located(reference.Kind)?"TrainingRoutineMissingLocation":"TrainingRoutineMissing");return;}
        var plan=TrainingRoutines.Resolve(_settings!,reference);
        _description.Text=reference.Kind==SetPieceKind.Penalty?UiText.Get("TrainingRoutinePenalty"):UiText.Format("TrainingRoutineDesigned",UiText.Get((reference.Kind==SetPieceKind.ThrowIn?"ThrowRoutine_":"Routine_")+plan.Routine));
    }
}
