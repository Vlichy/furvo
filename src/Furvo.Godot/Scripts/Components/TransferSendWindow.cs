using Godot;
namespace Furvo.Presentation.Components;

/// <summary>Resumen de envío desplazable; los botones permanecen fuera del contenido variable.</summary>
public partial class TransferSendWindow : Window
{
    private bool _closing;
    public TransferSendWindow(Theme theme,string summary,Action confirmed)
    {
        Name="TransferSendConfirmation";Title=UiText.Get("TransferSend");
        var body=WindowUi.Body(this,theme);
        body.AddChild(ClubUi.Text("TransferConfirmHeading",Title,20,"f1b96b"));
        var content=ClubUi.ScrollBody(body,"TransferConfirmationScroll");
        content.AddChild(ClubUi.Text("TransferConfirmSummary",summary,15));
        var footer=new HBoxContainer();footer.AddThemeConstantOverride("separation",12);body.AddChild(footer);
        footer.AddChild(WindowUi.Button("TransferFinalSend",()=>{if(_closing)return;_closing=true;Hide();confirmed();if(!IsQueuedForDeletion())QueueFree();}));
        footer.AddChild(WindowUi.Button("TransferCancelSend",Close));
        CloseRequested+=Close;WindowInput+=input=>{if(WindowUi.IsEscape(input)){SetInputAsHandled();Close();}};
    }
    private void Close(){if(_closing)return;_closing=true;Hide();QueueFree();}
    private void Fit(){var size=GetTree().Root.Size;PopupCentered(new Vector2I(Math.Min(900,size.X-40),Math.Min(540,size.Y-48)));}
    public void Open(){var root=GetTree().Root;void Resize(){if(Visible)Fit();}root.SizeChanged+=Resize;TreeExiting+=()=>root.SizeChanged-=Resize;Fit();}
}
