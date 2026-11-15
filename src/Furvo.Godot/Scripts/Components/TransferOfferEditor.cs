using System.Globalization;
using Furvo.Core.Domain;
using Furvo.Core.Market;
using Furvo.Core.Queries;
using Godot;
namespace Furvo.Presentation.Components;

/// <summary>Pantalla única de presentación. Los controles no escriben en SQLite ni ejecutan operaciones deportivas.</summary>
public partial class TransferOfferEditor : VBoxContainer
{
    public bool Dirty {get;private set;}
    private Action _refresh=()=>{};
    private readonly MarketSnapshot _data;
    private readonly MarketPlayer _player;
    private readonly TransferMoneyPolicy _policy;
    private readonly string _currency;
    private readonly IReadOnlyList<Competition> _competitions;
    private readonly TransferMoneyField _amount;
    private readonly CheckBox _free;
    private readonly TransferRightsCards _rights;
    private readonly TransferArrivalCards _incorporation;
    private readonly Label _additional;
    private readonly Label _releaseNotice;
    private readonly TransferPlayerPicker _players;
    private readonly Label _quick,_summary,_amountRuleError,_feedback;
    private readonly List<TransferBonus> _bonuses;
    private readonly List<FutureFeeClause> _fees;
    private readonly List<Func<FutureFeeClause?>> _standardFees=new();
    private readonly TransferPaymentEditor _payments;
    private readonly ClubFinancialResources? _resources;
    private bool _bonusPending;
    private int? _editingBonus;
    private readonly VBoxContainer _scrollBody;
    public TransferOfferEditor(Theme theme,MarketSnapshot data,MarketPlayer player,TransferTerms? old,ClubFinancialResources? resources,string currency,
        IReadOnlyList<Competition> competitions,DateOnly seasonEnd,TransferMoneyPolicy policy,Action<TransferTerms,bool> save,Action back)
    {
        Name="TransferOfferEditor";SizeFlagsVertical=SizeFlags.ExpandFill;_data=data;_player=player;_policy=policy;_currency=currency;_competitions=competitions;
        _resources=resources;_bonuses=old?.Bonuses.ToList()??new();_fees=old?.FutureFees.ToList()??new();
        var header=ClubUi.Card(this,"OfferHeader");header.AddChild(ClubUi.Text("OfferPlayerHeading",player.Details.Player.Name+" · "+UiText.Position(player.Details.Player.Position,player.Details.Player.Group)+" · "+(player.Details.Player.Age?.ToString()??UiText.Get("NotAvailable")),19,"f1b96b"));
        header.AddChild(ClubUi.Text("OfferScopeHint",UiText.Get("OfferScopeHint"),12));
        header.AddChild(ClubUi.Text("OfferClubs",UiText.Get("OfferSeller")+": "+player.Details.Club!.Name+"   ·   "+UiText.Get("OfferBuyer")+": "+data.Clubs.Single(c=>c.Id==data.ManagedClub).Name,14));
        header.AddChild(new PlayerContractPanel(new(player.Contract?.Id,player.Contract?.Club.Name??player.Details.Club?.Name,player.Contract?.End,player.Contract?.Release??ReleaseClauseInfo.Unknown),true));
        _quick=ClubUi.Text("OfferQuickSummary","",14);header.AddChild(_quick);
        _scrollBody=ClubUi.ScrollBody(this,"OfferScroll");
        var info=Section("OfferPlayerResources",false);
        ClubUi.Field(info,"TransferBudget",Money(resources?.TransferRemaining));ClubUi.Field(info,"TransferCash",Money(resources?.Cash));
        ClubUi.Field(info,"TransferValue",UiText.Get("NotAvailable"));ClubUi.Field(info,"MarketContractEnd",player.Contract?.End?.ToString("dd/MM/yyyy"));
        info.AddChild(ClubUi.Text("OfferResourceWarning",UiText.Get("TransferResourcesWarning"),13));
        var financial=Section("OfferFinancial",true);
        var moneyRow=Row(financial);_amount=MoneyField(moneyRow,"TransferAmount",old?.FixedAmount);_free=Check(moneyRow,"TransferExplicitFree",old?.ExplicitFreeProposal??false);
        _amountRuleError=Error(financial,"OfferAmountRuleError");
        _releaseNotice=ClubUi.Text("ReleaseOfferNotice",ReleaseClauseText.Notice(player.Contract?.Release,old?.FixedAmount??0,currency,UiText.Get),13,"a6b6ca");financial.AddChild(_releaseNotice);
        _amount.Changed+=()=>{try{_releaseNotice.Text=ReleaseClauseText.Notice(player.Contract?.Release,_amount.Read(false),currency,UiText.Get);}catch{_releaseNotice.Text=UiText.Get("ReleaseComparisonPending");}};
        _payments=new TransferPaymentEditor(old,policy,currency);financial.AddChild(_payments);_payments.Changed+=Changed;
        financial.AddChild(ClubUi.Text("OfferFormalizationHint",UiText.Get("OfferFormalizationHint"),12));
        var swaps=Section("OfferPlayers",false);
        _players=new TransferPlayerPicker(theme,TransferExchangeSelection.Candidates(data,data.ManagedClub!.Value,player.Details.Club!.Id,player.Details.Player.Id),old?.OfferedPlayers??Array.Empty<PlayerId>());
        swaps.AddChild(_players);_players.Changed+=Changed;
        swaps.AddChild(ClubUi.Text("OfferInterestHint",UiText.Get("OfferInterestHint"),12));
        var bonusSection=Section("OfferBonuses",false);var bonusRows=new VBoxContainer();bonusSection.AddChild(bonusRows);
        var builder=ClubUi.Card(bonusSection,"OfferBonusBuilder");var first=Row(builder);
        var objective=Options(first,"TransferBonusType",Enum.GetValues<BonusObjective>().Select(t=>UiText.Get("Transfer_"+t)).ToArray(),0,190);
        foreach(var t in Enum.GetValues<BonusObjective>())if(t is BonusObjective.InternationalCaps or BonusObjective.EuropeanQualification or BonusObjective.CupTitle or BonusObjective.InternationalTitle||t==BonusObjective.CleanSheets&&player.Details.Player.Position!=PlayerRole.Goalkeeper)objective.SetItemDisabled(Array.IndexOf(Enum.GetValues<BonusObjective>(),t),true);
        var count=Text(first,"TransferThreshold","1",90);var bonusAmount=MoneyField(first,"TransferBonusAmount",null);var second=Row(builder);
        var periodLabels=new List<string>{UiText.Get("OfferCurrentSeason")};periodLabels.AddRange(TransferOfferPresets.RightYears.Select(TransferRightsCards.Years));
        var period=Options(second,"OfferBonusPeriod",periodLabels,0,180);
        var scope=Options(second,"TransferScope",competitions.Select(c=>c.Name??UiText.Get("NotAvailable")).ToArray(),0,230);
        var bonusError=Error(builder,"OfferBonusError");
        void SetBonusPending(){_bonusPending=true;Changed();}
        objective.ItemSelected+=_=>SetBonusPending();count.TextChanged+=_=>SetBonusPending();bonusAmount.Changed+=SetBonusPending;period.ItemSelected+=_=>SetBonusPending();scope.ItemSelected+=_=>SetBonusPending();
        TransferBonus? editing=null;
        void RenderBonuses(){
            Clear(bonusRows);
            for(var i=0;i<_bonuses.Count;i++){
                var index=i;var b=_bonuses[i];var row=Row(bonusRows);row.SetMeta("offer_bonus_id",b.Id.ToString("D"));
                row.AddChild(ClubUi.Text("OfferBonusRow",UiText.Get("Transfer_"+b.Objective)+" ≥ "+b.Threshold+" · "+Money(b.Amount)+" · "+b.From.ToString("dd/MM/yyyy")+" — "+b.Until.ToString("dd/MM/yyyy")+(b.Cumulative?" · "+UiText.Format("OfferMaximumActivations",b.MaximumActivations):""),14));
                row.AddChild(ActionButton("OfferEditBonus",()=>{
                    editing=b;_editingBonus=index;objective.Selected=Array.IndexOf(Enum.GetValues<BonusObjective>(),b.Objective);count.Text=b.Threshold.ToString();bonusAmount.SetAmount(b.Amount);
                    if(period.ItemCount>periodLabels.Count)period.RemoveItem(period.ItemCount-1);
                    period.AddItem(UiText.Get("OfferPreviousPeriod")+" · "+b.From.ToString("dd/MM/yyyy")+" — "+b.Until.ToString("dd/MM/yyyy"));period.Selected=period.ItemCount-1;
                    scope.Selected=Math.Max(0,competitions.ToList().FindIndex(c=>c.Id.ToString()==b.Scope));_bonusPending=true;
                }));
                row.AddChild(ActionButton("TransferRemove",()=>{_bonuses.RemoveAt(index);_editingBonus=null;editing=null;_bonusPending=false;RenderBonuses();Changed();}));
            }
        }
        builder.AddChild(ActionButton("OfferAddBonus",()=>{
            try{
                if(competitions.Count==0)throw new InvalidDataException(UiText.Get("OfferNoScope"));
                if(!int.TryParse(count.Text,out var threshold)||threshold<1)throw new InvalidDataException(UiText.Get("TransferInvalidCount"));
                var from=data.Date;var until=seasonEnd;
                if(period.Selected>0&&period.Selected<=TransferOfferPresets.RightYears.Count)until=data.Date.AddYears(TransferOfferPresets.RightYears[period.Selected-1]).AddDays(-1);
                else if(period.Selected>TransferOfferPresets.RightYears.Count&&editing is {} legacy){from=legacy.From;until=legacy.Until;}
                var bonus=new TransferBonus(editing?.Id??Guid.NewGuid(),Enum.GetValues<BonusObjective>()[objective.Selected],threshold,bonusAmount.Read(),from,until,editing?.MaximumActivations??1,editing?.Cumulative??false,competitions[scope.Selected].Id.ToString());
                if(bonus.Amount<=0)throw new InvalidDataException(UiText.Get("OfferPositiveBonus"));
                if(_editingBonus is {} index)_bonuses[index]=bonus;else _bonuses.Add(bonus);
                _editingBonus=null;editing=null;_bonusPending=false;bonusAmount.Clear();bonusError.Text="";RenderBonuses();Changed();
            }catch(Exception e){bonusError.Text=e.Message;}
        }));RenderBonuses();bonusSection.AddChild(ClubUi.Text("OfferBonusHint",UiText.Get("TransferBonusWarning"),12));
        var future=Section("OfferFuture",false);
        foreach(var basis in Enum.GetValues<FutureFeeBasis>()){
            var box=ClubUi.Card(future,basis==FutureFeeBasis.FutureTransferFee?"OfferSale":"OfferProfit");var original=_fees.Where(f=>f.Basis==basis).ToArray();
            var expectedCode=basis==FutureFeeBasis.FutureTransferFee?TransferOfferPresets.SaleCode:TransferOfferPresets.ProfitCode;
            var standard=original.Length==1&&original[0].StandardCode==expectedCode&&original[0].Percentage%10==0;
            var legacy=original.Length>0&&!standard;var legacyRows=new VBoxContainer();box.AddChild(legacyRows);
            foreach(var f in original.Where(_=>legacy))legacyRows.AddChild(ClubUi.Text("OfferPreviousClause",f.Percentage+"% · "+f.Calculation+" · "+f.Activation,13));
            var controls=Row(box);controls.Visible=!legacy;var enabled=Check(controls,basis==FutureFeeBasis.FutureTransferFee?"OfferEnableSale":"OfferEnableProfit",standard);
            var percentages=Options(controls,basis==FutureFeeBasis.FutureTransferFee?"OfferSalePercent":"OfferProfitPercent",Enumerable.Range(1,10).Select(i=>i*10+" %").ToArray(),standard?(int)original[0].Percentage/10-1:0,120);
            var keepLegacy=legacy;var clauseId=original.Length==1?original[0].Id:Guid.NewGuid();
            var explanation=ClubUi.Text("OfferBaseExplanation",UiText.Get(basis==FutureFeeBasis.FutureTransferFee?"OfferSaleExplanation":"OfferProfitExplanation"),13);explanation.Visible=!legacy;box.AddChild(explanation);
            if(legacy)box.AddChild(ActionButton("OfferReplaceClause",()=>{keepLegacy=false;legacyRows.Visible=false;controls.Visible=true;explanation.Visible=true;enabled.ButtonPressed=true;Changed();}));
            _standardFees.Add(()=>{
                if(keepLegacy)return null;
                _fees.RemoveAll(f=>f.Basis==basis);
                return enabled.ButtonPressed?TransferOfferPresets.FutureFee(basis,(percentages.Selected+1)*10,player.Details.Club.Id,player.Details.Player.Id,standard?original[0].From:data.Date,_amount.Read(false),clauseId,policy):null;
            });
        }
        var rights=Section("OfferOptions",false);_rights=new TransferRightsCards(theme,old,data.Date,policy,currency);rights.AddChild(_rights);_rights.Changed+=Changed;
        var dates=Section("OfferDates",false);_incorporation=new TransferArrivalCards(theme,old,data.Date);dates.AddChild(_incorporation);_incorporation.Changed+=Changed;
        var additional=ClubUi.Card(dates,"OfferAdditionalSummary");_additional=ClubUi.Text("OfferAdditionalSummaryText","",14);additional.AddChild(_additional);
        var final=Section("OfferSummary",true);_summary=ClubUi.Text("TransferPreview","",14);final.AddChild(_summary);
        _feedback=Error(this,"TransferFeedback");_feedback.Visible=false;var actions=new HFlowContainer();actions.AddThemeConstantOverride("h_separation",10);AddChild(actions);
        void Save(bool send){
            try{var proposal=Read();proposal.Validate(data.Date,player.Details.Club.Id,player.Details.Player.Id,send,policy);ShowError("");save(proposal,send);}
            catch(Exception e){ShowError(e.Message);}
        }
        actions.AddChild(ActionButton("TransferSaveDraft",()=>Save(false)));actions.AddChild(ActionButton("TransferSend",()=>Save(true)));actions.AddChild(ActionButton("MarketBackSearch",back));
        _refresh=()=>{
            try{var proposal=Read(false);_quick.Text=UiText.Format("OfferCompactSummary",Money(proposal.FixedAmount),proposal.OfferedPlayers.Count,proposal.Bonuses.Count);_summary.Text=Summary(proposal,false);_additional.Text=AdditionalSummary(proposal);}
            catch{_quick.Text=UiText.Get("OfferIncomplete");_summary.Text=UiText.Get("OfferIncomplete");_additional.Text=UiText.Get("OfferAdditionalIncomplete");}
        };
        _refresh();Dirty=false;
    }
    public void MarkSaved()=>Dirty=false;
    public void ShowError(string message){_feedback.Text=message;_feedback.Visible=!string.IsNullOrEmpty(message);}
    private void Changed(){Dirty=true;if(_feedback is not null)ShowError("");_refresh();}
    private string Money(decimal? value)=>value is {} v?v.ToString("N2",CultureInfo.GetCultureInfo("es-ES"))+" "+_currency:UiText.Get("NotAvailable");
    private static void Clear(Node node){foreach(var c in node.GetChildren()){node.RemoveChild(c);c.QueueFree();}}
    private VBoxContainer Section(string key,bool expanded)
    {
        var card=ClubUi.Card(_scrollBody,key);var heading=card.GetChild<Label>(0);card.RemoveChild(heading);heading.QueueFree();
        var content=new VBoxContainer{Visible=expanded};content.AddThemeConstantOverride("separation",8);
        var toggle=WindowUi.Button(key,()=>{});toggle.Name="Toggle_"+key;toggle.Alignment=HorizontalAlignment.Left;toggle.AddThemeColorOverride("font_color",new Color("f1b96b"));card.AddChild(toggle);card.AddChild(content);
        void Title()=>toggle.Text=(content.Visible?"▾ ":"▸ ")+UiText.Get(key);toggle.Pressed+=()=>{content.Visible=!content.Visible;Title();};Title();return content;
    }
    private HFlowContainer Row(Control parent){var row=new HFlowContainer();row.AddThemeConstantOverride("h_separation",14);row.AddThemeConstantOverride("v_separation",8);parent.AddChild(row);return row;}
    private TransferMoneyField MoneyField(Control parent,string key,decimal? initial){var f=new TransferMoneyField(key,initial,_policy,_currency);parent.AddChild(f);f.Changed+=Changed;return f;}
    private Button ActionButton(string key,Action action){var b=WindowUi.Button(key,action);b.SizeFlagsHorizontal=SizeFlags.ShrinkBegin;return b;}
    private CheckBox Check(Control parent,string key,bool enabled){var c=new CheckBox{Name=key,Text=UiText.Get(key),ButtonPressed=enabled,SizeFlagsHorizontal=SizeFlags.ShrinkBegin,SizeFlagsVertical=SizeFlags.ShrinkCenter};parent.AddChild(c);c.Toggled+=_=>Changed();return c;}
    private LineEdit Text(Control parent,string key,string value,int width){var box=new VBoxContainer();parent.AddChild(box);box.AddChild(ClubUi.Text(key+"Label",UiText.Get(key),13));var f=new LineEdit{Name=key,Text=value,CustomMinimumSize=new Vector2(width,36)};box.AddChild(f);f.TextChanged+=_=>Changed();return f;}
    private OptionButton Options(Control parent,string key,IEnumerable<string> labels,int selected,int width){var box=new VBoxContainer();parent.AddChild(box);box.AddChild(ClubUi.Text(key+"Label",UiText.Get(key),13));var o=new OptionButton{Name=key,FitToLongestItem=false,ClipText=true,CustomMinimumSize=new Vector2(width,36)};foreach(var l in labels)o.AddItem(l);o.Selected=o.ItemCount==0?-1:selected;TrainingUi.LimitPopup(o);box.AddChild(o);o.ItemSelected+=_=>Changed();return o;}
    private Label Error(Control parent,string key){var e=ClubUi.Text(key,"",13,"ec8e8e");parent.AddChild(e);return e;}
    public TransferTerms Read(bool errors=true)
    {
        var fixedAmount=_amount.Read(errors);
        var intended=_incorporation.ReadDate(errors);var reference=intended??_data.Date;
        var payment=_payments.Read(fixedAmount,reference,errors);
        if(_bonusPending){if(errors)throw new InvalidDataException(UiText.Get("OfferAddBonusFirst"));}
        if(fixedAmount==0&&_players.Selected.Count==0&&!_free.ButtonPressed){if(errors)_amountRuleError.Text=UiText.Get("OfferZeroReason");if(errors)throw new InvalidDataException(UiText.Get("OfferZeroReason"));}else _amountRuleError.Text="";
        var standard=_standardFees.Select(f=>f()).Where(f=>f is not null).Cast<FutureFeeClause>().ToArray();var allFees=_fees.Concat(standard).ToArray();
        var buy=_rights.ReadBuy(errors);var match=_rights.ReadMatching();
        return new(_currency,fixedAmount,payment.Payments,_players.Selected,_bonuses.ToArray(),allFees,buy,match,_incorporation.Kind,intended,_incorporation.Expires,_free.ButtonPressed,payment.Plan);
    }
    public string AdditionalSummary(TransferTerms t)=>string.Join(((char)10).ToString(),new[]{
        UiText.Get("OfferBuyback")+": "+_rights.BuySummary(t.Buyback),UiText.Get("OfferMatching")+": "+_rights.MatchingSummary(t.Matching),
        UiText.Get("OfferArrivalLabel")+": "+UiText.Get(t.Arrival==ArrivalKind.Immediate?"OfferArrivalSoon":t.Arrival==ArrivalKind.NextWindow?"OfferArrivalNext":"OfferArrivalFuture")+(t.IntendedArrival is {} date?" · "+date.ToString("dd/MM/yyyy"):""),
        UiText.Get("OfferValidity")+": "+UiText.Format("OfferValidityCompact",TransferRightsCards.Days(t.Expires.DayNumber-_data.Date.DayNumber),t.Expires.ToString("dd/MM/yyyy"))});
    public string Summary(TransferTerms t,bool detailed=true)
    {
        var lines=new List<string>{_player.Details.Player.Name+" · "+_player.Details.Club!.Name,UiText.Get("TransferAmount")+": "+Money(t.FixedAmount),UiText.Get("OfferPaymentMode")+": "+_payments.ModeText,UiText.Get("OfferPlayers")+": "+(_players.Selected.Count==0?UiText.Get("OfferNone"):string.Join(", ",_players.Selected.Select(id=>_data.Players.FirstOrDefault(p=>p.Details.Player.Id==id)?.Details.Player.Name??UiText.Get("TransferUnavailablePlayers"))))};
        var review=TransferFinancialReview.Assess(t,_resources,t.PaymentPlan?.ReferenceDate??_data.Date);
        lines.Add(UiText.Get("OfferFinancialPending"));
        lines.Add(UiText.Get("TransferCash")+": "+UiText.Get("OfferFinancial_"+review.Liquidity)+" · "+UiText.Get("TransferBudget")+": "+UiText.Get("OfferFinancial_"+review.Budget));
        foreach(var payment in t.Payments)lines.Add(payment.Order+". "+Money(payment.Amount)+" · "+payment.Due.ToString("dd/MM/yyyy")+" · "+UiText.Get("TransferPaymentState_Proposed"));
        foreach(var b in t.Bonuses)lines.Add(UiText.Get("Transfer_"+b.Objective)+" ≥ "+b.Threshold+" · "+Money(b.Amount)+" · "+b.From.ToString("dd/MM/yyyy")+" — "+b.Until.ToString("dd/MM/yyyy")+" · "+(_competitions.FirstOrDefault(c=>c.Id.ToString()==b.Scope)?.Name??UiText.Get("NotAvailable"))+" · "+UiText.Get("TransferPendingEvaluation"));
        foreach(var f in t.FutureFees)lines.Add(UiText.Get(f.Basis==FutureFeeBasis.FutureTransferFee?"OfferSale":"OfferProfit")+": "+f.Percentage+" % · "+f.Calculation+" · "+f.Activation+(f.Until==DateOnly.MaxValue?"":" · "+f.From.ToString("dd/MM/yyyy")+" — "+f.Until.ToString("dd/MM/yyyy")));
        if(detailed){
        lines.Add(UiText.Get("OfferBuyback")+": "+(t.Buyback is {} buy?Money(buy.Amount)+" · "+buy.From.ToString("dd/MM/yyyy")+" — "+buy.Until.ToString("dd/MM/yyyy")+" · "+buy.Conditions:UiText.Get("OfferNone")));
        lines.Add(UiText.Get("OfferMatching")+": "+(t.Matching is {} match?match.Until.ToString("dd/MM/yyyy")+" · "+TransferRightsCards.Days(match.ResponseDays)+" · "+match.Notification+" · "+match.MatchingCriteria:UiText.Get("OfferNone")));
        lines.Add(UiText.Get("TransferArrival")+": "+UiText.Get("Transfer_"+t.Arrival)+" · "+(t.IntendedArrival?.ToString("dd/MM/yyyy")??UiText.Get("TransferDateNotResolved")));
        lines.Add(UiText.Get("OfferValidity")+": "+t.Expires.ToString("dd/MM/yyyy"));
        }else lines.Add(AdditionalSummary(t));
        return string.Join(System.Environment.NewLine,lines);
    }
}
