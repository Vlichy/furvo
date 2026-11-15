using Furvo.Core.Market;
using Godot;
using System.Globalization;
namespace Furvo.Presentation.Components;

/// <summary>Presentación de cláusulas existentes. Conserva condiciones anteriores hasta reemplazo expreso.</summary>
public partial class TransferRightsCards : VBoxContainer
{
    private readonly TransferTerms? _old;
    private readonly DateOnly _date;
    private readonly TransferMoneyPolicy _policy;
    private readonly string _currency;
    private readonly CheckButton _buy,_match;
    private readonly TransferMoneyField _price;
    private readonly OfferChoices _buyYears,_matchYears,_response;
    private readonly VBoxContainer _buyDetails,_matchDetails,_buyControls,_matchControls;
    private readonly Label _buySummary,_matchSummary;
    private bool _keepBuy,_keepMatching;
    public event Action? Changed;
    public TransferRightsCards(Theme theme,TransferTerms? old,DateOnly date,TransferMoneyPolicy policy,string currency)
    {
        Name="OfferRightsCards";_old=old;_date=date;_policy=policy;_currency=currency;
        var buy=ClubUi.Card(this,"OfferBuyback");buy.AddChild(ClubUi.Text("OfferBuybackDescription",UiText.Get("OfferBuybackDescription"),14));
        _buy=new CheckButton{Name="TransferBuybackEnabled",ButtonPressed=old?.Buyback is not null,SizeFlagsHorizontal=SizeFlags.ShrinkBegin};buy.AddChild(_buy);
        _buyDetails=new VBoxContainer{Name="OfferBuybackDetails"};_buyDetails.AddThemeConstantOverride("separation",8);buy.AddChild(_buyDetails);
        _buyControls=new VBoxContainer();_buyDetails.AddChild(_buyControls);var buyRow=Row(_buyControls);
        _price=new TransferMoneyField("TransferBuybackAmount",old?.Buyback?.Amount,policy,currency);buyRow.AddChild(_price);
        var years=Box(buyRow,"OfferBuybackYears");_buyYears=Years(years,"OfferBuybackYears",OfferConditionOptions.BuybackYears,old?.Buyback?.DurationYears??1);
        _buySummary=ClubUi.Text("OfferBuybackSummary","",14,"f1b96b");_buyDetails.AddChild(_buySummary);
        _keepBuy=old?.Buyback is not null&&old.Buyback.StandardCode!=TransferOfferPresets.BuybackCode;
        if(_keepBuy){var replace=WindowUi.Button("OfferReplaceBuyback",()=>{_keepBuy=false;Notify();});replace.SizeFlagsHorizontal=SizeFlags.ShrinkBegin;_buyDetails.AddChild(replace);}
        _buyDetails.AddChild(Help("OfferBuybackHelp",()=>old?.Buyback is {} previous&&_keepBuy?previous.Conditions+" · "+previous.From.ToString("dd/MM/yyyy")+" — "+previous.Until.ToString("dd/MM/yyyy"):TransferOfferPresets.BuybackConditions,theme));
        var match=ClubUi.Card(this,"OfferMatching");match.AddChild(ClubUi.Text("OfferMatchingDescription",UiText.Get("OfferMatchingDescription"),14));
        _match=new CheckButton{Name="TransferMatchingEnabled",ButtonPressed=old?.Matching is not null,SizeFlagsHorizontal=SizeFlags.ShrinkBegin};match.AddChild(_match);
        _matchDetails=new VBoxContainer{Name="OfferMatchingDetails"};_matchDetails.AddThemeConstantOverride("separation",8);match.AddChild(_matchDetails);
        _matchControls=new VBoxContainer();_matchDetails.AddChild(_matchControls);var matchRow=Row(_matchControls);
        _matchYears=Years(Box(matchRow,"OfferMatchingYears"),"OfferMatchingYears",OfferConditionOptions.MatchingYears,old?.Matching?.DurationYears??1);
        var days=OfferConditionOptions.Preserve(OfferConditionOptions.MatchingDays,old?.Matching?.ResponseDays);
        var responseBox=Box(matchRow,"OfferResponseDays");_response=new OfferChoices("OfferResponseDays",days.Select(d=>new OfferChoice(d,Days(d),Previous:!OfferConditionOptions.MatchingDays.Contains(d))),old?.Matching?.ResponseDays??7);responseBox.AddChild(_response);
        _matchSummary=ClubUi.Text("OfferMatchingSummary","",14,"f1b96b");_matchDetails.AddChild(_matchSummary);
        _keepMatching=old?.Matching is not null&&old.Matching.StandardCode!=TransferOfferPresets.MatchingCode;
        if(_keepMatching){var replace=WindowUi.Button("OfferReplaceMatching",()=>{_keepMatching=false;Notify();});replace.SizeFlagsHorizontal=SizeFlags.ShrinkBegin;_matchDetails.AddChild(replace);}
        _matchDetails.AddChild(Help("OfferMatchingHelp",()=>old?.Matching is {} previous&&_keepMatching?previous.Notification+((char)10)+previous.MatchingCriteria:TransferOfferPresets.MatchingNotification+((char)10)+TransferOfferPresets.MatchingCriteria,theme));
        _price.Changed+=Notify;_buy.Toggled+=_=>Notify();_match.Toggled+=_=>Notify();_buyYears.Changed+=Notify;_matchYears.Changed+=Notify;_response.Changed+=Notify;Refresh();
    }
    internal static string Years(int n)=>UiText.Format(OfferConditionOptions.YearsKey(n),n);
    internal static string Days(int n)=>UiText.Format(OfferConditionOptions.DaysKey(n),n);
    private static HFlowContainer Row(Control parent){var row=new HFlowContainer();row.AddThemeConstantOverride("h_separation",16);row.AddThemeConstantOverride("v_separation",8);parent.AddChild(row);return row;}
    private static VBoxContainer Box(Control parent,string title){var box=new VBoxContainer();box.AddChild(ClubUi.Text(title+"Label",UiText.Get(title),13));parent.AddChild(box);return box;}
    private static OfferChoices Years(Control parent,string name,IReadOnlyList<int> options,int previous){var choices=new OfferChoices(name,OfferConditionOptions.Preserve(options,previous).Select(n=>new OfferChoice(n,Years(n),Previous:!options.Contains(n))),previous);parent.AddChild(choices);return choices;}
    internal static Button Help(string key,Func<string> text,Theme theme)
    {
        var button=WindowUi.Button(key,()=>{});button.SizeFlagsHorizontal=SizeFlags.ShrinkBegin;button.TooltipText=text();button.MouseEntered+=()=>button.TooltipText=text();
        button.Pressed+=()=>{var dialog=new AcceptDialog{Name=key+"Dialog",Theme=theme,Title=UiText.Get(key),DialogText=text(),DialogAutowrap=true,Exclusive=true,Transient=true,Unresizable=true,OkButtonText=UiText.Get("OfferHelpClose")};button.AddChild(dialog);dialog.Confirmed+=()=>dialog.QueueFree();dialog.Canceled+=()=>dialog.QueueFree();var root=button.GetTree().Root.Size;dialog.PopupCentered(new Vector2I(Math.Min(720,root.X-48),Math.Min(360,root.Y-64)));};return button;
    }
    private void Notify(){Refresh();Changed?.Invoke();}
    private void Refresh()
    {
        _buy.Text=UiText.Get(_buy.ButtonPressed?"OfferClauseOn":"OfferClauseOff");_match.Text=UiText.Get(_match.ButtonPressed?"OfferClauseOn":"OfferClauseOff");
        _buyDetails.Visible=_buy.ButtonPressed;_matchDetails.Visible=_match.ButtonPressed;_buyControls.Visible=!_keepBuy;_matchControls.Visible=!_keepMatching;
        if(_buy.ButtonPressed){try{_buySummary.Text=BuySummary(ReadBuy(false));}catch{_buySummary.Text=UiText.Get("OfferClauseIncomplete");}}
        if(_match.ButtonPressed){try{_matchSummary.Text=MatchingSummary(ReadMatching());}catch{_matchSummary.Text=UiText.Get("OfferClauseIncomplete");}}
    }
    public BuybackClause? ReadBuy(bool errors=true)
    {
        if(!_buy.ButtonPressed)return null;if(_keepBuy)return _old!.Buyback;
        var amount=_price.Read(errors);if(amount<=0){if(errors)_price.ReportError(UiText.Get("OfferBuybackPositive"));throw new InvalidDataException(UiText.Get("OfferBuybackPositive"));}
        var origin=_old?.Buyback?.StandardCode==TransferOfferPresets.BuybackCode?_old.Buyback.From:_date;
        return TransferOfferPresets.Buyback(amount,origin,_buyYears.SelectedValue,_policy);
    }
    public MatchingClause? ReadMatching()
    {
        if(!_match.ButtonPressed)return null;if(_keepMatching)return _old!.Matching;
        var previous=_old?.Matching;
        if(previous?.StandardCode==TransferOfferPresets.MatchingCode&&previous.DurationYears==_matchYears.SelectedValue&&previous.ResponseDays==_response.SelectedValue)return previous;
        var origin=previous?.StandardCode==TransferOfferPresets.MatchingCode&&previous.DurationYears is {} years?previous.Until.AddDays(1).AddYears(-years):_date;
        return TransferOfferPresets.Matching(origin,_matchYears.SelectedValue,_response.SelectedValue);
    }
    public string BuySummary(BuybackClause? buy)=>buy is null?UiText.Get("OfferClauseAbsent"):buy.StandardCode!=TransferOfferPresets.BuybackCode?UiText.Get("OfferPreviousConditions"):UiText.Format("OfferBuybackDynamic",buy.Amount.ToString("N2",CultureInfo.GetCultureInfo("es-ES"))+" "+_currency,Years(buy.DurationYears!.Value));
    public string MatchingSummary(MatchingClause? match)=>match is null?UiText.Get("OfferClauseAbsent"):match.StandardCode!=TransferOfferPresets.MatchingCode?UiText.Get("OfferPreviousConditions"):UiText.Format("OfferMatchingDynamic",Years(match.DurationYears!.Value),Days(match.ResponseDays));
}
