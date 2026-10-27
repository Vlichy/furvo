using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Opciones de sesión. Solo Aplicar cambia la ventana; no escribe configuración ni preferencias del sistema.</summary>
public partial class DisplayOptionsWindow : Window
{
    private readonly OptionButton _mode;
    private readonly OptionButton _resolution;
    private readonly List<Vector2I> _resolutions = [];
    private readonly Label _feedback;
    private readonly Action _closed;
    private bool _closing;
    public DisplayOptionsWindow(Theme theme, Action closed)
    {
        Name = "DisplayOptions"; Title = UiText.Get("Options"); _closed = closed;
        var body = WindowUi.Body(this, theme);
        body.AddChild(ClubUi.Text("OptionsHeading", Title, 22, "f1b96b"));
        body.AddChild(ClubUi.Text("DisplayModeLabel", UiText.Get("DisplayMode"), 15));
        _mode = new OptionButton { Name = "DisplayModeSelection", CustomMinimumSize = new Vector2(0, 38) };
        _mode.AddItem(UiText.Get("WindowedMode")); _mode.AddItem(UiText.Get("FullscreenMode")); body.AddChild(_mode);
        var currentMode = DisplayServer.WindowGetMode();
        _mode.Select(currentMode is DisplayServer.WindowMode.Fullscreen or DisplayServer.WindowMode.ExclusiveFullscreen ? 1 : 0);
        body.AddChild(ClubUi.Text("ResolutionLabel", UiText.Get("Resolution"), 15));
        _resolution = new OptionButton { Name = "ResolutionSelection", CustomMinimumSize = new Vector2(0, 38) }; body.AddChild(_resolution);
        var usable = DisplayServer.GetName() == "headless" ? new Vector2I(1920,1080) : DisplayServer.ScreenGetUsableRect().Size;
        var currentSize = DisplayServer.GetName() == "headless" ? new Vector2I(1280,720) : DisplayServer.WindowGetSize();
        foreach (var size in new[] { new Vector2I(960,640), new Vector2I(1280,720), new Vector2I(1600,900), new Vector2I(1920,1080), currentSize }
            .Where(s => s.X >= 960 && s.Y >= 640 && s.X <= usable.X - 16 && s.Y <= usable.Y - 40).Distinct().OrderBy(s=>s.X).ThenBy(s=>s.Y))
        {
            _resolutions.Add(size); _resolution.AddItem(UiText.Format("ResolutionFormat", size.X, size.Y));
        }
        if (_resolutions.Count > 0)
        {
            var selected = _resolutions.IndexOf(currentSize); _resolution.Select(selected >= 0 ? selected : 0);
        }
        else { _resolution.AddItem(UiText.Get("ResolutionUnavailable")); _resolution.Select(0); }
        void UpdateResolution() => _resolution.Disabled = _mode.Selected == 1 || _resolutions.Count == 0;
        _mode.ItemSelected += _ => UpdateResolution(); UpdateResolution();
        body.AddChild(ClubUi.Text("OptionsSessionNotice", UiText.Get("OptionsSessionNotice"), 12, "a6b6ca"));
        _feedback = ClubUi.Text("OptionsFeedback", "", 12, "f1b96b"); body.AddChild(_feedback);
        var buttons = new HFlowContainer(); buttons.AddThemeConstantOverride("h_separation", 12); body.AddChild(buttons);
        buttons.AddChild(WindowUi.Button("ApplyOptions", Apply)); buttons.AddChild(WindowUi.Button("CancelOptions", Close));
        CloseRequested += Close;
        WindowInput += input => { if (WindowUi.IsEscape(input)) { SetInputAsHandled(); Close(); } };
    }
    private void Apply()
    {
        if (DisplayServer.GetName() == "headless") { _feedback.Text = UiText.Get("DisplayChangeUnavailable"); return; }
        if (_mode.Selected == 1) DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
        else
        {
            if (_resolutions.Count == 0) { _feedback.Text = UiText.Get("ResolutionUnavailable"); return; }
            var size = _resolutions[_resolution.Selected];
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed); DisplayServer.WindowSetSize(size);
            var usable = DisplayServer.ScreenGetUsableRect(); DisplayServer.WindowSetPosition(usable.Position + (usable.Size - size)/2);
        }
        Close();
    }
    private void Close()
    {
        if (_closing) return; _closing = true;
        Hide(); _closed(); QueueFree();
    }
    public void Open() => PopupCentered(new Vector2I(540, 390));
}
