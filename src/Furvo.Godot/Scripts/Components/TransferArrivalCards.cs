using Furvo.Core.Market;
using Godot;
namespace Furvo.Presentation.Components;
/// <summary>Incorporación y validez independientes. Todas las fechas proceden de la carrera, nunca del ordenador.</summary>
public partial class TransferArrivalCards : VBoxContainer
{
    private readonly DateOnly _date;
    private readonly OfferChoices _arrival,_validity;
    private readonly SpinBox _day,_month,_year;
    private readonly Control _calendar;
    private readonly Label _dateError,_arrivalSummary,_validitySummary,_validityContext;
    private bool _updating,_validityEdited;
    public event Action? Changed;
    public ArrivalKind Kind=>(ArrivalKind)_arrival.SelectedValue;
    public DateOnly Expires=>_date.AddDays(_validity.SelectedValue);
    public TransferArrivalCards(Theme theme,TransferTerms? old,DateOnly date)
    {
        Name="OfferArrivalCards";_date=date;_validityEdited=old is null;
        var arrival=ClubUi.Card(this,"OfferArrivalQuestion");
        _arrival=new OfferChoices("OfferArrival",new[]{new OfferChoice((int)ArrivalKind.Immediate,UiText.Get("OfferArrivalSoon"),UiText.Get("OfferArrivalSoonDescription")),new OfferChoice((int)ArrivalKind.NextWindow,UiText.Get("OfferArrivalNext"),UiText.Get("OfferArrivalNextDescription")),new OfferChoice((int)ArrivalKind.FutureDate,UiText.Get("OfferArrivalFuture"),UiText.Get("OfferArrivalFutureDescription"))},(int)(old?.Arrival??ArrivalKind.Immediate),true);arrival.AddChild(_arrival);
        var future=new VBoxContainer{Name="OfferArrivalCalendar"};arrival.AddChild(future);_calendar=future;future.AddChild(ClubUi.Text("OfferFutureDateLabel",UiText.Get("OfferFutureDateLabel"),13));var row=new HFlowContainer();row.AddThemeConstantOverride("h_separation",12);future.AddChild(row);var intended=old?.IntendedArrival??date;
        _day=Part(row,"OfferArrivalDay",1,31,intended.Day);_month=Part(row,"OfferArrivalMonth",1,12,intended.Month);_year=Part(row,"OfferArrivalYear",1,9999,intended.Year);
        _dateError=ClubUi.Text("OfferArrivalDateError","",13,"ec8e8e");future.AddChild(_dateError);_dateError.Visible=false;
        _arrivalSummary=ClubUi.Text("OfferArrivalSummary","",14,"f1b96b");arrival.AddChild(_arrivalSummary);arrival.AddChild(ClubUi.Text("OfferRegistrationPending",UiText.Get("OfferRegistrationPending"),12,"a6b6ca"));
        arrival.AddChild(TransferRightsCards.Help("OfferArrivalHelp",()=>UiText.Get("TransferArrivalWarning"),theme));
        var validity=ClubUi.Card(this,"OfferValidityQuestion");var remaining=old is null?7:old.Expires.DayNumber-date.DayNumber;
        _validity=new OfferChoices("OfferValidity",OfferConditionOptions.Preserve(OfferConditionOptions.ValidityDays,remaining).Select(d=>new OfferChoice(d,TransferRightsCards.Days(d),Previous:!OfferConditionOptions.ValidityDays.Contains(d))),remaining);validity.AddChild(_validity);
        _validitySummary=ClubUi.Text("OfferValiditySummary","",14,"f1b96b");validity.AddChild(_validitySummary);
        _validityContext=ClubUi.Text("OfferValidityContext","",12,"a6b6ca");validity.AddChild(_validityContext);
        _arrival.Changed+=Notify;_validity.Changed+=()=>{_validityEdited=true;Notify();};_day.ValueChanged+=_=>Notify();_month.ValueChanged+=_=>Calendar();_year.ValueChanged+=_=>Calendar();Refresh();
    }
    private static SpinBox Part(Control parent,string key,int min,int max,int value){var box=new VBoxContainer();parent.AddChild(box);box.AddChild(ClubUi.Text(key+"Label",UiText.Get(key),12));var input=new SpinBox{Name=key,MinValue=min,MaxValue=max,Step=1,Value=value,CustomMinimumSize=new Vector2(100,36)};box.AddChild(input);return input;}
    private void Calendar(){if(_updating)return;_updating=true;_day.MaxValue=DateTime.DaysInMonth((int)_year.Value,(int)_month.Value);_updating=false;Notify();}
    private void Notify(){if(_updating)return;Refresh();Changed?.Invoke();}
    private void Refresh(){_calendar.Visible=Kind==ArrivalKind.FutureDate;try{_arrivalSummary.Text=ArrivalSummary(ReadDate(false));}catch{_arrivalSummary.Text=UiText.Get("OfferInvalidArrivalDate");}_validitySummary.Text=UiText.Format(_validityEdited?"OfferValidityDynamic":"OfferValidityRemaining",TransferRightsCards.Days(_validity.SelectedValue),Expires.ToString("dd/MM/yyyy"));_validityContext.Text=UiText.Get(_validityEdited?"OfferValidityContext":"OfferValidityPreserved");}
    public DateOnly? ReadDate(bool errors=true)
    {
        if(Kind!=ArrivalKind.FutureDate){_dateError.Visible=false;return null;}
        try{var result=new DateOnly((int)_year.Value,(int)_month.Value,(int)_day.Value);if(result<_date)throw new InvalidDataException(UiText.Get("OfferInvalidArrivalDate"));_dateError.Visible=false;return result;}
        catch{if(errors){_dateError.Text=UiText.Get("OfferInvalidArrivalDate");_dateError.Visible=true;}throw new InvalidDataException(UiText.Get("OfferInvalidArrivalDate"));}
    }
    public string ArrivalSummary(DateOnly? date)=>Kind switch{ArrivalKind.Immediate=>UiText.Get("OfferArrivalSoonDescription"),ArrivalKind.NextWindow=>UiText.Get("OfferArrivalNextDescription"),_=>UiText.Format("OfferArrivalDateSummary",date?.ToString("dd/MM/yyyy"))};
    public string CompactSummary=>UiText.Get(Kind==ArrivalKind.Immediate?"OfferArrivalSoon":Kind==ArrivalKind.NextWindow?"OfferArrivalNext":"OfferArrivalFuture")+(Kind==ArrivalKind.FutureDate?" · "+ReadDate(false)?.ToString("dd/MM/yyyy"):"");
    public string ValiditySummary=>UiText.Format("OfferValidityCompact",TransferRightsCards.Days(_validity.SelectedValue),Expires.ToString("dd/MM/yyyy"));
}
