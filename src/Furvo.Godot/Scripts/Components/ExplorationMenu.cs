using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Menú de exploración único: Continuar oculta la ventana sin reconstruir el club ni la tabla.</summary>
public partial class ExplorationMenu : Window
{
    public ExplorationMenu(Theme theme, Action home, Action options, Action exit)
    {
        Name = "ExplorationMenu"; Title = UiText.Get("ExplorationMenuTitle");
        var body = WindowUi.Body(this, theme);
        body.AddChild(ClubUi.Text("ExplorationMenuHeading", Title, 22, "f1b96b"));
        body.AddChild(WindowUi.Button("ContinueExploration", Hide));
        body.AddChild(WindowUi.Button("ReturnMainMenu", () => { Hide(); home(); }));
        body.AddChild(WindowUi.Button("Options", () => { Hide(); options(); }));
        body.AddChild(WindowUi.Button("ExitGame", exit));
        CloseRequested += Hide;
        WindowInput += input => { if (WindowUi.IsEscape(input)) { SetInputAsHandled(); Hide(); } };
    }
    public void Open() => PopupCentered(new Vector2I(440, 290));
}
