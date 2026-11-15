using Furvo.Core.Domain;
using Furvo.Core.Market;
using Furvo.Core.Queries;
using Godot;
namespace Furvo.Presentation.Components;

/// <summary>Selección múltiple transitoria por ID: confirmar aplica; cancelar y Escape descartan el borrador.</summary>
public partial class TransferPlayerPicker : VBoxContainer
{
    private readonly IReadOnlyList<TransferExchangeCandidate> _candidates;
    private readonly HashSet<PlayerId> _selected;
    private readonly VBoxContainer _rows=new(),_results=new(){Name="OfferPlayerResults"};
    private readonly Window _popup=new(){Name="OfferPlayerPopup",Borderless=true};
    private readonly LineEdit _search=new(){Name="OfferPlayerSearch",PlaceholderText=UiText.Get("OfferPlayerSearchHint")};
    private readonly OptionButton _position=new(){Name="OfferPlayerPosition",CustomMinimumSize=new Vector2(200,40)};
    private readonly OptionButton _team=new(){Name="OfferPlayerTeam",CustomMinimumSize=new Vector2(180,40)};
    private readonly Label _counter=ClubUi.Text("OfferSelectedCount","",14,"f1b96b");
    private TransferExchangeDraft _draft;
    public event Action? Changed;
    public IReadOnlyList<PlayerId> Selected=>_selected.OrderBy(p=>p.ToString(),StringComparer.Ordinal).ToArray();
    public TransferPlayerPicker(Theme theme,IReadOnlyList<TransferExchangeCandidate> candidates,IEnumerable<PlayerId> selected)
    {
        Name="OfferPlayerPicker";_candidates=candidates;_selected=selected.ToHashSet();_draft=new(Selected);
        var open=WindowUi.Button("OfferChoosePlayers",()=>{
            _draft=new(Selected);Refresh();var root=GetTree().Root.Size;var size=new Vector2I(Math.Min(1000,root.X-48),Math.Min(650,root.Y-64));
            _popup.MaxSize=size;_popup.PopupCentered(size);Callable.From(()=>{if(GodotObject.IsInstanceValid(_popup)&&_popup.IsInsideTree()){_popup.Size=size;_popup.MoveToCenter();}}).CallDeferred();_search.GrabFocus();
        });open.SizeFlagsHorizontal=SizeFlags.ShrinkBegin;AddChild(open);AddChild(_rows);AddChild(_popup);
        var content=WindowUi.Body(_popup,theme);content.AddChild(ClubUi.Text("OfferPickerTitle",UiText.Get("OfferPickerTitle"),20,"f1b96b"));content.AddChild(ClubUi.Text("OfferPickerSubtitle",UiText.Get("OfferPickerSubtitle"),14));content.AddChild(_search);
        // Las etiquetas recién creadas aún no tienen ancho al calcular su altura. No permitir que
        // ese mínimo transitorio agrande el modal: la lista dispone de desplazamiento propio.
        _popup.WrapControls=false;
        Ready+=()=>{
            var window=GetTree().Root;
            void Fit(){if(!_popup.Visible)return;var size=new Vector2I(Math.Min(1000,window.Size.X-48),Math.Min(650,window.Size.Y-64));_popup.MaxSize=size;_popup.Size=size;_popup.MoveToCenter();}
            window.SizeChanged+=Fit;TreeExiting+=()=>window.SizeChanged-=Fit;
        };
        var filters=new HFlowContainer();filters.AddThemeConstantOverride("h_separation",12);content.AddChild(filters);filters.AddChild(_position);filters.AddChild(_team);
        foreach(var key in new[]{"OfferFilterAll","OfferFilterGoalkeepers","OfferFilterDefenders","OfferFilterMidfielders","OfferFilterForwards"})_position.AddItem(UiText.Get(key));
        foreach(var key in new[]{"OfferFilterAllTeams","OfferFilterFirst","OfferFilterReserve","OfferFilterAcademy"})_team.AddItem(UiText.Get(key));
        content.AddChild(ClubUi.Text("OfferExchangeEligibilityHint",UiText.Get("OfferExchangeEligibilityHint"),12,"a6b6ca"));
        var scroll=new ScrollContainer{Name="OfferPlayerScroll",SizeFlagsVertical=SizeFlags.ExpandFill,HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};content.AddChild(scroll);_results.SizeFlagsHorizontal=SizeFlags.ExpandFill;_results.AddThemeConstantOverride("separation",6);scroll.AddChild(_results);
        content.AddChild(_counter);var buttons=new HBoxContainer();buttons.AddThemeConstantOverride("separation",12);content.AddChild(buttons);
        buttons.AddChild(WindowUi.Button("OfferAddSelected",()=>{_selected.Clear();_selected.UnionWith(_draft.Selected);_popup.Hide();Render();Changed?.Invoke();}));buttons.AddChild(WindowUi.Button("TransferColumnsCancel",()=>_popup.Hide()));
        _popup.WindowInput+=input=>{if(WindowUi.IsEscape(input))_popup.Hide();};_popup.CloseRequested+=()=>_popup.Hide();_search.TextChanged+=_=>Refresh();_position.ItemSelected+=_=>Refresh();_team.ItemSelected+=_=>Refresh();Render();
    }
    public void Remove(PlayerId id){_selected.Remove(id);Render();Changed?.Invoke();}
    private string Secondary(TransferExchangeCandidate candidate,bool team)
    {
        var p=candidate.Player.Details.Player;return UiText.Position(p.Position,p.Group)+" · "+UiText.Format("OfferPlayerAge",p.Age?.ToString()??UiText.Get("NotAvailable"))+(team?" · "+(candidate.Player.TeamName??UiText.Get("NotAvailable")):"");
    }
    private static void Clear(Node parent){foreach(var c in parent.GetChildren()){parent.RemoveChild(c);c.QueueFree();}}
    private void Count()=>_counter.Text=UiText.Format("OfferSelectedCount",_draft.Selected.Count);
    private void Refresh()
    {
        Clear(_results);var candidates=TransferExchangeFilters.Search(_candidates,_search.Text,_position.Selected==0?null:(PlayerPosition)_position.Selected,(ExchangeTeamScope)_team.Selected);
        foreach(var candidate in candidates){
            var id=candidate.Player.Details.Player.Id;var card=Card(_results,id,_draft.Selected.Contains(id));
            var row=new HBoxContainer();row.AddThemeConstantOverride("separation",10);card.AddChild(row);
            var toggle=new CheckBox{Name="OfferSelectPlayer",ButtonPressed=_draft.Selected.Contains(id),CustomMinimumSize=new Vector2(36,40),TooltipText=UiText.Get("OfferTogglePlayer")};toggle.SetMeta("offer_candidate_id",id.ToString());row.AddChild(toggle);
            var text=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};row.AddChild(text);text.AddChild(ClubUi.Text("OfferPlayerName",candidate.Player.Details.Player.Name,16));text.AddChild(ClubUi.Text("OfferPlayerInfo",Secondary(candidate,true),13,"a6b6ca"));
            var contract=candidate.Player.Contract;card.TooltipText=contract?.End is {} end?UiText.Get("MarketContractEnd")+": "+end.ToString("dd/MM/yyyy"):UiText.Get("NotAvailable");
            if(candidate.Interested)text.AddChild(ClubUi.Text("OfferRecordedInterest",UiText.Get("OfferRecordedInterest"),12,"f1b96b"));
            toggle.Toggled+=active=>{_draft.Toggle(id);Style(card,active);Count();};
        }
        if(candidates.Count==0)_results.AddChild(ClubUi.Text("OfferNoCandidates",UiText.Get("OfferNoCandidates"),14));Count();
    }
    private static PanelContainer Card(Control parent,PlayerId id,bool active)
    {
        var card=new PanelContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};card.SetMeta("offer_player_card",id.ToString());Style(card,active);parent.AddChild(card);return card;
    }
    private static void Style(PanelContainer card,bool active)
    {
        var box=new StyleBoxFlat{BgColor=new Color(active?"30445c":"182b42"),BorderColor=new Color(active?"f1b96b":"2c4058"),ContentMarginLeft=10,ContentMarginRight=10,ContentMarginTop=7,ContentMarginBottom=7};box.SetBorderWidthAll(1);box.SetCornerRadiusAll(4);card.AddThemeStyleboxOverride("panel",box);
    }
    private void Render()
    {
        Clear(_rows);
        foreach(var id in Selected){
            var card=Card(_rows,id,true);card.SetMeta("offer_selected_player",id.ToString());var row=new HBoxContainer();row.AddThemeConstantOverride("separation",10);card.AddChild(row);
            var candidate=_candidates.FirstOrDefault(p=>p.Player.Details.Player.Id==id);var text=new VBoxContainer{SizeFlagsHorizontal=SizeFlags.ExpandFill};row.AddChild(text);
            text.AddChild(ClubUi.Text("OfferSelectedPlayer",candidate?.Player.Details.Player.Name??UiText.Get("TransferUnavailablePlayers")+" · "+id,16));if(candidate is not null)text.AddChild(ClubUi.Text("OfferSelectedPlayerInfo",Secondary(candidate,false),13,"a6b6ca"));row.AddChild(WindowUi.Button("OfferRemovePlayer",()=>Remove(id)));
        }
    }
}
