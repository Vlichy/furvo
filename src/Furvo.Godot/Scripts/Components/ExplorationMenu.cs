using Godot;
namespace Furvo.Presentation.Components;
/// <summary>Menú único de pausa. No existe un guardado pendiente: las pantallas actuales son consultas y la creación se confirma en SQLite.</summary>
public partial class ExplorationMenu:Window
{
    private readonly bool _career;
    public ExplorationMenu(Theme theme,Action home,Action options,Action exit,bool career=false)
    {
        _career=career;Name="ExplorationMenu";Title=UiText.Get(career?"CareerGameMenu":"ExplorationMenuTitle");var body=WindowUi.Body(this,theme);
        body.AddChild(ClubUi.Text("ExplorationMenuHeading",Title,22,"f1b96b"));
        var resume=WindowUi.Button("ContinueExploration",Hide);if(career)resume.Text=UiText.Get("CareerResume");body.AddChild(resume);
        if(career)
        {
            foreach(var key in new[]{"CareerSaveUnavailable","CareerLoadUnavailable"})
                body.AddChild(new Button{Name=key,Text=UiText.Get(key),Disabled=true,TooltipText=UiText.Get("CareerPersistenceUnavailable"),CustomMinimumSize=new Vector2(0,44)});
        }
        var settings=WindowUi.Button("Options",()=>{Hide();options();});if(career)settings.Text=UiText.Get("GameSettings");body.AddChild(settings);
        body.AddChild(WindowUi.Button("ReturnMainMenu",()=>{Hide();home();}));body.AddChild(WindowUi.Button("ExitGame",exit));
        CloseRequested+=Hide;WindowInput+=input=>{if(WindowUi.IsEscape(input)){SetInputAsHandled();Hide();}};
    }
    public void Open()=>PopupCentered(new Vector2I(480,_career?450:290));
}
