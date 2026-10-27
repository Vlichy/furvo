using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Contenedores compartidos de ventanas modales; ningún menú modifica estado deportivo.</summary>
internal static class WindowUi
{
    internal static VBoxContainer Body(Window window, Theme theme)
    {
        window.Visible = false; window.Theme = theme; window.Exclusive = true; window.Transient = true; window.Unresizable = true;
        var background = new ColorRect { Color = new Color("0d192b"), MouseFilter = Control.MouseFilterEnum.Ignore };
        window.AddChild(background); background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var margin = new MarginContainer(); window.AddChild(margin); margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        foreach (var side in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, 18);
        var body = new VBoxContainer(); body.AddThemeConstantOverride("separation", 10); margin.AddChild(body); return body;
    }
    internal static Button Button(string key, Action action)
    {
        var button = new Button { Name = key, Text = UiText.Get(key), CustomMinimumSize = new Vector2(0, 42) };
        button.Pressed += action; return button;
    }
    internal static bool IsEscape(InputEvent input) => input is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape };
}
