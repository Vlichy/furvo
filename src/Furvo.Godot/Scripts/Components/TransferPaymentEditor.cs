using Furvo.Core.Market;
using Godot;
using System.Globalization;
namespace Furvo.Presentation.Components;

/// <summary>Editor porcentual compacto. Solo construye una propuesta relativa a la futura formalización.</summary>
public partial class TransferPaymentEditor : VBoxContainer
{
    private readonly OptionButton _mode=new(){Name="OfferPaymentMode",CustomMinimumSize=new Vector2(190,40)};
    private readonly OptionButton _count=new(){Name="OfferPaymentCount",CustomMinimumSize=new Vector2(150,40)};
    private readonly VBoxContainer _fields=new(){Name="OfferPaymentRows"};
    private readonly Label _summary=ClubUi.Text("OfferPaymentSchedule","",14);
    private readonly Label _error=ClubUi.Text("OfferPaymentError","",13,"ec8e8e");
    private readonly List<(SpinBox Months,SpinBox Percentage)> _inputs=new();
    private readonly TransferTerms? _old;
    private readonly TransferMoneyPolicy _policy;
    private readonly string _currency;
    private IReadOnlyList<TransferPayment> _previous;
    private bool _legacy,_loading;
    public event Action? Changed;
    public TransferPaymentEditor(TransferTerms? old,TransferMoneyPolicy policy,string currency)
    {
        Name="OfferPaymentEditor";_old=old;_policy=policy;_currency=currency;_previous=old?.Payments??Array.Empty<TransferPayment>();_legacy=old is not null&&old.PaymentPlan is null;
        var controls=new HFlowContainer();controls.AddThemeConstantOverride("h_separation",12);AddChild(controls);
        var modeBox=new VBoxContainer();controls.AddChild(modeBox);modeBox.AddChild(ClubUi.Text("OfferPaymentModeLabel",UiText.Get("OfferPaymentMode"),13));modeBox.AddChild(_mode);
        foreach(var key in new[]{"OfferSingle","OfferDeferred","OfferCustomPlan"})_mode.AddItem(UiText.Get(key));
        var countBox=new VBoxContainer();controls.AddChild(countBox);countBox.AddChild(ClubUi.Text("OfferPaymentCountLabel",UiText.Get("OfferPaymentCount"),13));countBox.AddChild(_count);
        foreach(var n in new[]{2,3,4})_count.AddItem(UiText.Format("OfferPaymentsCount",n));
        AddChild(_fields);AddChild(_summary);AddChild(_error);_error.Visible=false;
        var legacy=ClubUi.Text("OfferLegacySchedule",UiText.Get("OfferLegacySchedule"),13,"f1b96b");AddChild(legacy);legacy.Visible=_legacy;controls.Visible=!_legacy;
        if(_legacy){var replace=WindowUi.Button("OfferReplaceSchedule",()=>{_legacy=false;legacy.Visible=false;controls.Visible=true;Build(TransferPaymentPlans.Preset(1));Changed?.Invoke();});replace.SizeFlagsHorizontal=SizeFlags.ShrinkBegin;AddChild(replace);}
        void Reset(){if(_loading)return;countBox.Visible=_mode.Selected!=0;Build(TransferPaymentPlans.Preset(_mode.Selected==0?1:_count.Selected+2));Changed?.Invoke();}
        _mode.ItemSelected+=_=>Reset();_count.ItemSelected+=_=>Reset();
        var shares=old?.PaymentPlan?.Shares??TransferPaymentPlans.Preset(1);_mode.Selected=shares.Count==1?0:2;_count.Selected=Math.Max(0,shares.Count-2);countBox.Visible=_mode.Selected!=0;Build(shares);_fields.Visible=!_legacy;
    }
    private void Build(IReadOnlyList<PaymentShare> shares)
    {
        _loading=true;foreach(var child in _fields.GetChildren()){_fields.RemoveChild(child);child.QueueFree();}_inputs.Clear();_fields.Visible=true;
        for(var i=0;i<shares.Count;i++){
            var index=i;var row=new HFlowContainer();row.AddThemeConstantOverride("h_separation",12);_fields.AddChild(row);
            var heading=ClubUi.Text("OfferPaymentRowLabel",i==0?UiText.Get("OfferInitialPayment"):UiText.Format("OfferPaymentNumber",i+1),14);heading.CustomMinimumSize=new Vector2(100,36);heading.SizeFlagsHorizontal=SizeFlags.ShrinkBegin;row.AddChild(heading);
            var months=new SpinBox{Name="OfferPaymentMonth"+i,MinValue=0,MaxValue=36,Step=1,Value=shares[i].Months,Editable=i!=0,CustomMinimumSize=new Vector2(110,36)};
            var percentage=new SpinBox{Name="OfferPaymentShare"+i,MinValue=0,MaxValue=100,Step=.01,Value=(double)shares[i].Percentage,Editable=i!=shares.Count-1,CustomMinimumSize=new Vector2(120,36),Suffix="%"};
            row.AddChild(months);var dueLabel=ClubUi.Text("OfferMonthsAfterLabel",UiText.Get(i==0?"OfferAtFormalization":"OfferMonthsAfter"),13);dueLabel.SizeFlagsHorizontal=SizeFlags.ShrinkBegin;dueLabel.CustomMinimumSize=new Vector2(200,36);row.AddChild(dueLabel);row.AddChild(percentage);_inputs.Add((months,percentage));
            void Update(){if(_loading)return;_loading=true;var remainder=100-_inputs.Take(_inputs.Count-1).Sum(p=>(decimal)p.Percentage.Value);_inputs[^1].Percentage.Value=(double)Math.Clamp(remainder,0,100);_loading=false;Changed?.Invoke();}
            months.ValueChanged+=_=>Update();percentage.ValueChanged+=_=>Update();
        }
        _loading=false;
    }
    public string ModeText=>UiText.Get(_legacy?"OfferLegacySchedule":_mode.Selected==0?"OfferSingle":_mode.Selected==1?"OfferDeferred":"OfferCustomPlan");
    public (IReadOnlyList<TransferPayment> Payments,TransferPaymentPlan? Plan) Read(decimal amount,DateOnly reference,bool errors=true)
    {
        try{
            TransferPaymentPlan? plan=_legacy?null:new(TransferPaymentPlans.Version,reference,_inputs.Select(p=>new PaymentShare((int)p.Months.Value,(decimal)p.Percentage.Value)).ToArray());
            var payments=_legacy?_old!.Payments:TransferPaymentPlans.Generate(amount,plan!,_previous,_policy);_previous=payments;
            if(_legacy&&payments.Sum(p=>p.Amount)!=amount)throw new InvalidDataException(UiText.Get("OfferLegacyAmountMismatch"));
            _error.Visible=false;_error.Text="";
            var advance=payments.Where(p=>p.Due==reference).Sum(p=>p.Amount);
            var lines=new List<string>{UiText.Format("OfferPaymentTotals",Money(amount),Money(advance),Money(amount-advance),payments.Count)};
            foreach(var p in payments){var relative=plan is null?"":plan.Shares.First(s=>reference.AddMonths(s.Months)==p.Due).Months==0?UiText.Get("OfferAtFormalization"):UiText.Format("OfferAfterMonths",plan.Shares.First(s=>reference.AddMonths(s.Months)==p.Due).Months);lines.Add(p.Order+" · "+relative+" · "+p.Due.ToString("dd/MM/yyyy")+" · "+Money(p.Amount)+" · "+(amount==0?0:p.Amount/amount*100).ToString("0.##",CultureInfo.GetCultureInfo("es-ES"))+" %");}
            _summary.Text=string.Join(((char)10).ToString(),lines);
            return(payments,plan);
        }catch(Exception e){_error.Text=e.Message;_error.Visible=errors;throw;}
    }
    private string Money(decimal amount)=>amount.ToString("N2",CultureInfo.GetCultureInfo("es-ES"))+" "+_currency;
}
