using System.Globalization;
using Furvo.Core.Market;
using Godot;
namespace Furvo.Presentation.Components;

/// <summary>Señala la entrada inválida y conserva el texto para corregirlo. Formatea importes válidos al salir.</summary>
public partial class TransferMoneyField : VBoxContainer
{
    public LineEdit Input {get;}
    private readonly Label _error;
    private readonly TransferMoneyPolicy _policy;
    private bool _updating,_formatted;
    private decimal? _value;
    public event Action? Changed;
    internal static MenuButton Suggestions(LineEdit input, TransferMoneyPolicy policy, IEnumerable<decimal> amounts, string currency)
    {
        var menu = new MenuButton { Name = input.Name.ToString() + "Suggestions", Text = "▾",
            TooltipText = "Importes sugeridos", CustomMinimumSize = new Vector2(38,36) };
        var choices = amounts.Where(amount => amount <= policy.MaximumIndividual).Distinct().ToArray();
        var popup = menu.GetPopup();
        var culture = CultureInfo.GetCultureInfo("es-ES");
        for (var i = 0; i < choices.Length; i++) popup.AddItem(choices[i].ToString("N2",culture) + " " + currency,i);
        popup.IdPressed += id =>
        {
            if (!input.Editable) return;
            input.Text = choices[(int)id].ToString("0.##",culture);
            input.EmitSignal(LineEdit.SignalName.TextChanged,input.Text);
        };
        return menu;
    }
    public TransferMoneyField(string key,decimal? initial,TransferMoneyPolicy policy,string currency)
    {
        Name=key+"Field";_policy=policy;CustomMinimumSize=new Vector2(225,0);SizeFlagsHorizontal=SizeFlags.ShrinkBegin;
        AddChild(ClubUi.Text(key+"Label",UiText.Get(key),13));
        Input=new LineEdit{Name=key,Text=initial?.ToString("0.00",CultureInfo.GetCultureInfo("es-ES"))??"",PlaceholderText=UiText.Get("OfferMoneyHint"),CustomMinimumSize=new Vector2(225,36)};var row=new HBoxContainer();AddChild(row);row.AddChild(Input);var unit=ClubUi.Text(key+"Currency",currency,14);unit.AutowrapMode=TextServer.AutowrapMode.Off;unit.CustomMinimumSize=new Vector2(42,0);unit.SizeFlagsHorizontal=SizeFlags.ShrinkBegin;unit.SizeFlagsVertical=SizeFlags.ShrinkCenter;Input.SizeFlagsVertical=SizeFlags.ShrinkCenter;row.AddChild(unit);
        _error=ClubUi.Text(key+"Error","",12,"ec8e8e");_error.CustomMinimumSize=new Vector2(0,0);AddChild(_error);_error.Visible=false;
        row.AddChild(Suggestions(Input,policy,[0,100_000,500_000,1_000_000,5_000_000,10_000_000],currency));
        if(initial is {} old){try{TransferRules.Money(old,_policy);_value=old;}catch(Exception e){SetError(e.Message);}}
        Input.TextChanged+=text=>{
            if(_updating)return;_formatted=false;
            try{_value=TransferRules.ParseMoney(text,_policy);SetError("");}
            catch(Exception e){_value=null;SetError(e.Message);}
            Changed?.Invoke();
        };
        Input.FocusEntered+=()=>{if(_formatted&&_value is {} value){_updating=true;Input.Text=value.ToString("0.00",CultureInfo.GetCultureInfo("es-ES"));_updating=false;_formatted=false;}};
        Input.FocusExited+=()=>Format();
        Format();
    }
    private void SetError(string message){_error.Text=message;_error.Visible=!string.IsNullOrEmpty(message);}
    public void ReportError(string message)=>SetError(message);
    public void Format(){if(_value is not {} value)return;_updating=true;Input.Text=value.ToString("N2",CultureInfo.GetCultureInfo("es-ES"));_updating=false;_formatted=true;}
    public void Clear(){_updating=true;Input.Text="";_updating=false;_value=null;_formatted=false;SetError("");}
    public void SetAmount(decimal amount){_updating=true;Input.Text=amount.ToString("0.00",CultureInfo.GetCultureInfo("es-ES"));_updating=false;_formatted=false;_value=amount;try{TransferRules.Money(amount,_policy);SetError("");}catch(Exception e){_value=null;SetError(e.Message);}Changed?.Invoke();}
    public decimal Read(bool showError=true)
    {
        try{var value=_formatted&&_value is {} cached?cached:TransferRules.ParseMoney(Input.Text,_policy);TransferRules.Money(value,_policy);return value;}
        catch(Exception e){if(showError&&string.IsNullOrEmpty(_error.Text))SetError(e.Message);throw;}
    }
}
