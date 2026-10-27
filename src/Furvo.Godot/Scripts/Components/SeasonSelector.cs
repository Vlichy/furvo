using System.Globalization;
using Furvo.Core.Queries;
using Godot;

namespace Furvo.Presentation.Components;

/// <summary>Selector reutilizable de temporadas existentes; no inventa ejercicios ni presupone disponibilidad contable.</summary>
public partial class SeasonSelector : HBoxContainer
{
    private readonly IReadOnlyList<SeasonSummary> _seasons;
    private readonly OptionButton _selector;
    public event Action<SeasonSummary>? SeasonSelected;
    public SeasonSummary? SelectedSeason => _selector.Selected >= 0 && _selector.Selected < _seasons.Count ? _seasons[_selector.Selected] : null;
    public SeasonSelector(IReadOnlyList<SeasonSummary> seasons, DateOnly referenceDate)
    {
        Name = "EconomySeasonSelector"; _seasons = seasons;
        AddChild(ClubUi.Text("EconomySeasonLabel", UiText.Get("EconomySeason"), 14, "a6b6ca"));
        _selector = new OptionButton { Name = "EconomySeasonSelection", Disabled = seasons.Count <= 1, CustomMinimumSize = new Vector2(180, 38) };
        AddChild(_selector);
        foreach (var season in seasons)
        {
            _selector.AddItem(season.Name ?? UiText.Format("SeasonDateRange", season.StartDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), season.EndDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)));
            _selector.SetItemMetadata(_selector.ItemCount - 1, season.Id.ToString());
        }
        if (seasons.Count == 0) { _selector.AddItem(UiText.Get("NoSeasonsAvailable")); _selector.Select(0); }
        else
        {
            var index = seasons.Select((s,i)=>(s,i)).FirstOrDefault(x=>x.s.StartDate<=referenceDate && referenceDate<=x.s.EndDate).i;
            _selector.Select(index); SetMeta("selected_season_id", seasons[index].Id.ToString());
        }
        _selector.ItemSelected += index =>
        {
            if (index < 0 || index >= seasons.Count) return;
            SetMeta("selected_season_id", seasons[(int)index].Id.ToString()); SeasonSelected?.Invoke(seasons[(int)index]);
        };
    }
}
