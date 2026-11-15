using System.Globalization;
using Furvo.Core.Domain;
using Furvo.Core.Market;
using Furvo.Core.Queries;
using Furvo.Core.Careers;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Godot;
namespace Furvo.Presentation;

/// <summary>Solo negociación entre clubes. Ninguna acción ejecuta pagos, inscripciones o contratos de jugadores.</summary>
public partial class Main
{
    private SqliteTransferStore Transfers()=>new(SportsPath);
    private string TransferMoney(decimal? value,string currency)=>value is {} v?v.ToString("N2",CultureInfo.GetCultureInfo("es-ES"))+" "+currency:UiText.Get("NotAvailable");
    private string TransferSummary(TransferTerms terms,DateOnly date,bool detailed=true)
    {
        var lines=new List<string>{UiText.Format("TransferSummary",TransferMoney(terms.Immediate(date),terms.Currency),TransferMoney(terms.FixedAmount-terms.Immediate(date),terms.Currency),TransferMoney(terms.FixedAmount,terms.Currency),TransferMoney(terms.MaximumBonuses,terms.Currency),terms.OfferedPlayers.Count,terms.FutureFees.Count)};
        if(detailed){
            foreach(var b in terms.Bonuses)lines.Add(UiText.Get("Transfer_"+b.Objective)+" ≥ "+b.Threshold+": "+TransferMoney(b.Amount,terms.Currency)+" · "+b.From+" — "+b.Until+" · "+UiText.Get("TransferPendingEvaluation"));
            foreach(var f in terms.FutureFees)lines.Add(f.Percentage+"% · "+UiText.Get("Transfer_"+f.Basis)+" · "+f.Calculation+" · "+f.Activation+" · "+f.From+" — "+f.Until);
            if(terms.Buyback is {} buy)lines.Add(UiText.Get("TransferBuybackEnabled")+": "+TransferMoney(buy.Amount,terms.Currency)+" · "+buy.From+" — "+buy.Until+" · "+buy.Conditions);
            if(terms.Matching is {} match)lines.Add(UiText.Get("TransferMatchingEnabled")+": "+match.Until+" · "+match.ResponseDays+" · "+match.Notification+" · "+match.MatchingCriteria);
        }
        return string.Join(System.Environment.NewLine,lines);
    }
    private void ShowTransferForm(PlayerId target,NegotiationVersion? existing=null)
    {
        try{
            _workspace!.Open(CareerPage.MarketOffers);var store=Transfers();var data=store.Participants();var buyer=data.ManagedClub??throw new InvalidOperationException(UiText.Get("TransferNoClub"));
            var player=MarketQueries.Player(data,target);var seller=player.Details.Club!.Id;
            if(existing is not null&&(existing.Career!=data.Career||existing.Buyer!=buyer||existing.Seller!=seller||existing.Target!=target||existing.State is not(NegotiationState.Draft or NegotiationState.CounterReceived)))throw new InvalidOperationException(UiText.Get("TransferNotEditable"));
            var resources=SqliteEconomyStore.ReadResources(SportsPath).FirstOrDefault(r=>r.ClubId==buyer);var currency=store.Currency();
            var seasonEnd=new SqliteWorldStore(SportsPath).Load().Seasons.Single(s=>s.Id==_workspace.Career.SeasonId).EndDate;
            var id=existing?.Id??Guid.NewGuid();var revision=existing?.Revision??0;
            var body=Frame("CareerSection","","",false);body.Name="TransferForm";body.SetMeta("transfer_target",target.ToString());
            TransferOfferEditor? editor=null;
            void Back(){
                if(editor is null||!editor.Dirty){ShowMarket();return;}
                var discard=new ConfirmationDialog{Name="TransferDiscard",DialogAutowrap=true,Exclusive=true,Transient=true,Unresizable=true,DialogText=UiText.Get("TransferDiscard"),OkButtonText=UiText.Get("TransferDiscard"),CancelButtonText=UiText.Get("TransferColumnsCancel")};
                AddChild(discard);discard.Confirmed+=()=>{ShowMarket();discard.QueueFree();};discard.Canceled+=()=>discard.QueueFree();discard.PopupCentered();
            }
            editor=new TransferOfferEditor(Theme,data,player,existing?.Terms,resources,currency,store.Competitions(),seasonEnd,store.MoneyPolicy,(proposed,send)=>{
                var saved=store.Draft(id,revision,target,proposed,Guid.NewGuid());revision=saved.Revision;editor!.MarkSaved();
                if(!send){ShowTransferDetail(id);return;}
                var confirm=new TransferSendWindow(Theme,editor.Summary(proposed)+System.Environment.NewLine+UiText.Get("TransferNoExecution"),()=>{
                    try{store.Send(id,revision,Guid.NewGuid());ShowTransferDetail(id);}catch(Exception e){editor.ShowError(e.Message);}
                });AddChild(confirm);confirm.Open();
            },Back);body.AddChild(editor);
        }catch(Exception e){GD.PushWarning(e.ToString());ShowError("TransferLoadError",()=>ShowTransferList(CareerPage.MarketOffers),ShowMarket);}
    }
    private void ShowTransferList(CareerPage page)
    {
        _workspace!.Open(page);var body=Frame("CareerSection",UiText.Get(page==CareerPage.MarketOffers?"NavPage_MarketOffers":"NavPage_MarketNegotiations"),UiText.Get("TransferNoAutomaticResponses"));body.Name="TransferList";
        try{
            var store=Transfers();var all=store.Current();var data=store.Participants();var filter=new OptionButton{Name="TransferStateFilter",SizeFlagsHorizontal=Control.SizeFlags.ShrinkBegin,CustomMinimumSize=new Vector2(260,40)};filter.AddItem(UiText.Get("TransferAllStates"));foreach(var s in Enum.GetValues<NegotiationState>())filter.AddItem(UiText.Get("TransferState_"+s));body.AddChild(filter);
            // Tree comparte la geometría de cabeceras y celdas. El recorte y los tooltips impiden que
            // los nombres largos ensanchen columnas; los pesos distribuyen todo el ancho disponible.
            var table=new DataTable([
                new(UiText.Get("TransferPlayerColumn"),1,28,HeaderAlignment:HorizontalAlignment.Left),
                new(UiText.Get("Club"),1,27,HeaderAlignment:HorizontalAlignment.Left),
                new(UiText.Get("TransferAmountColumn"),1,20,HeaderAlignment:HorizontalAlignment.Right,CellAlignment:HorizontalAlignment.Right),
                new(UiText.Get("TransferState"),1,17,HeaderAlignment:HorizontalAlignment.Left),
                new(UiText.Get("TransferRevision"),1,8,CellAlignment:HorizontalAlignment.Center)
            ],rowHeight:34,rowSpacing:4,constrainColumns:true,tabularNumbers:true,proportionalColumns:true){Name="TransferNegotiationsTable"};body.AddChild(table);
            void Render(){var rows=all.Where(v=>(filter.Selected==0||v.State==Enum.GetValues<NegotiationState>()[filter.Selected-1])&&(page!=CareerPage.MarketNegotiations||v.State!=NegotiationState.Draft));table.SetRows(rows.Select(v=>new TableRow(v.Id.ToString("D"),new[]{data.Players.FirstOrDefault(p=>p.Details.Player.Id==v.Target)?.Details.Player.Name??v.Target.ToString(),data.Clubs.Single(c=>c.Id==v.Seller).Name,TransferMoney(v.Terms.FixedAmount,v.Terms.Currency),UiText.Get("TransferState_"+v.State),v.Revision.ToString()})));}
            filter.ItemSelected+=_=>Render();Render();table.RowActivated+=id=>ShowTransferDetail(Guid.Parse(id));var open=WindowUi.Button("TransferOpen",()=>{if(table.SelectedId is {} id)ShowTransferDetail(Guid.Parse(id));});open.Disabled=true;table.RowSelected+=_=>open.Disabled=false;body.AddChild(open);
            if(all.Count==0)body.AddChild(ClubUi.Text("TransferEmpty",UiText.Get("TransferEmpty"),15));
        }catch(Exception e){GD.PushWarning(e.ToString());body.AddChild(ClubUi.Text("TransferLoadError",UiText.Get("TransferLoadError"),15));}
    }
    private void ShowTransferDetail(Guid id)
    {
        _workspace!.Open(CareerPage.MarketOffers);var body=Frame("CareerSection",UiText.Get("TransferDetail"),UiText.Get("TransferNoExecution"));body.Name="TransferDetail";
        try{
            var store=Transfers();var history=store.History(id);var latest=history.LastOrDefault()??throw new InvalidOperationException(UiText.Get("TransferUnknown"));var content=ClubUi.ScrollBody(body,"TransferHistoryScroll");
            content.AddChild(ClubUi.Text("TransferNoAutomaticResponses",UiText.Get("TransferNoAutomaticResponses"),14,"f1b96b"));
            content.AddChild(ClubUi.Text("TransferCurrentState",UiText.Get("TransferState_"+latest.State),19,"f1b96b"));
            var participants=store.Participants();var scopes=store.Competitions().ToDictionary(c=>c.Id.ToString(),c=>c.Name);content.AddChild(ClubUi.Text("TransferIdentity",participants.Players.FirstOrDefault(p=>p.Details.Player.Id==latest.Target)?.Details.Player.Name??latest.Target.ToString(),19));
            if(latest.State is NegotiationState.AcceptedBetweenClubs or NegotiationState.PendingPlayers)ClubUi.Field(content,"TransferAgreedAmount",TransferMoney(latest.Terms.FixedAmount,latest.Terms.Currency));
            content.AddChild(ClubUi.Text("TransferFinalSummary",TransferSummary(latest.Terms,latest.Date),15));
            foreach(var version in history){
                var section=ClubUi.Card(content,"TransferVersion");section.AddChild(ClubUi.Text("TransferVersionHeading",version.Revision+" · "+version.Date.ToString("dd/MM/yyyy")+" · "+UiText.Get("TransferState_"+version.State),15,"f1b96b"));
                section.AddChild(ClubUi.Text("TransferVersionSummary",TransferSummary(version.Terms,version.Date),14));
                foreach(var p in version.Terms.Payments)section.AddChild(ClubUi.Text("TransferPayment",p.Order+" · "+p.Due.ToString("dd/MM/yyyy")+" · "+TransferMoney(p.Amount,version.Terms.Currency)+" · "+UiText.Get("TransferPaymentState_"+p.State),14));
                foreach(var player in version.Terms.OfferedPlayers)section.AddChild(ClubUi.Text("TransferSwap",UiText.Get("TransferInclude")+": "+(participants.Players.FirstOrDefault(p=>p.Details.Player.Id==player)?.Details.Player.Name??player.ToString()),14));
                foreach(var bonus in version.Terms.Bonuses)section.AddChild(ClubUi.Text("TransferBonus",UiText.Get("Transfer_"+bonus.Objective)+" ≥ "+bonus.Threshold+" · "+TransferMoney(bonus.Amount,version.Terms.Currency)+" · "+bonus.From+" — "+bonus.Until+" · "+scopes.GetValueOrDefault(bonus.Scope,UiText.Get("NotAvailable"))+" · "+UiText.Get("TransferPendingEvaluation"),14));
                foreach(var f in version.Terms.FutureFees)section.AddChild(ClubUi.Text("TransferFee",f.Percentage+"% · "+UiText.Get("Transfer_"+f.Basis)+" · "+f.Calculation+" · "+f.Activation+" · "+f.From+" — "+f.Until,14));
                if(version.Terms.Buyback is {} buy)section.AddChild(ClubUi.Text("TransferBuyback",UiText.Get("TransferBuybackEnabled")+": "+TransferMoney(buy.Amount,version.Terms.Currency)+" · "+buy.From+" — "+buy.Until+" · "+buy.Conditions,14));
                if(version.Terms.Matching is {} match)section.AddChild(ClubUi.Text("TransferMatching",UiText.Get("TransferMatchingEnabled")+": "+match.Until+" · "+match.ResponseDays+" · "+match.Notification+" · "+match.MatchingCriteria,14));
                section.AddChild(ClubUi.Text("TransferArrivalDetail",UiText.Get("Transfer_"+version.Terms.Arrival)+" · "+(version.Terms.IntendedArrival?.ToString("dd/MM/yyyy")??UiText.Get("TransferDateNotResolved")),14));
                if(version.Reason is {} reason)section.AddChild(ClubUi.Text("TransferReason",reason,14));
            }
            var feedback=ClubUi.Text("TransferDetailError","",14,"f1b96b");body.AddChild(feedback);var buttons=new HFlowContainer();buttons.AddThemeConstantOverride("h_separation",8);body.AddChild(buttons);
            if(latest.State is NegotiationState.Draft or NegotiationState.CounterReceived)buttons.AddChild(WindowUi.Button("TransferEdit",()=>ShowTransferForm(latest.Target,latest)));
            if(latest.State is NegotiationState.Sent or NegotiationState.PendingResponse or NegotiationState.CounterReceived)buttons.AddChild(WindowUi.Button("TransferWithdraw",()=>{try{store.Withdraw(id,latest.Revision,Guid.NewGuid());ShowTransferDetail(id);}catch(Exception e){feedback.Text=e.Message;}}));
            if(latest.State is NegotiationState.Draft or NegotiationState.PendingPlayers)buttons.AddChild(WindowUi.Button("TransferCancel",()=>{try{store.Cancel(id,latest.Revision,Guid.NewGuid());ShowTransferDetail(id);}catch(Exception e){feedback.Text=e.Message;}}));
            buttons.AddChild(WindowUi.Button("TransferBackList",()=>ShowTransferList(CareerPage.MarketOffers)));buttons.AddChild(WindowUi.Button("MarketBackSearch",ShowMarket));
        }catch(Exception e){GD.PushWarning(e.ToString());body.AddChild(ClubUi.Text("TransferLoadError",UiText.Get("TransferLoadError"),15));body.AddChild(WindowUi.Button("TransferBackList",()=>ShowTransferList(CareerPage.MarketOffers)));}
    }
    private void ShowTransferRights()
    {
        _workspace!.Open(CareerPage.MarketRights);var body=Frame("CareerSection",UiText.Get("NavPage_MarketRights"),UiText.Get("TransferRightsWarning"));body.Name="TransferRightsPage";
        body.AddChild(ClubUi.Text("TransferNoActiveRights",UiText.Get("TransferNoActiveRights"),16));
    }
}
