using Furvo.Core.Domain;
using Furvo.Core.Queries;
namespace Furvo.Presentation.Components;
/// <summary>Estado efímero de consulta; no contiene entidades internas ni escribe preferencias en SQLite.</summary>
public sealed class MarketSearchContext(Guid career,ClubId? managedClub=null)
{
    public Guid Career {get;}=career;
    public ClubId? ManagedClub {get;}=managedClub;
    public bool MatchesScope(Guid career,ClubId? managedClub)=>Career==career&&ManagedClub==managedClub;
    public MarketFilter Applied {get;set;}=new();
    public MarketSort Sort {get;set;}
    public string? FilterError {get;set;}
    public string? SortAttribute {get;set;}
    public int HorizontalScroll {get;set;}
    public string MinimumValueText {get;set;}="1";
    public string MaximumValueText {get;set;}="20";
    public void ReconcileSort(IReadOnlyList<string> columns){if(Sort==MarketSort.Attribute&&!columns.Contains(SortAttribute)){Sort=MarketSort.Name;SortAttribute=null;Descending=false;}}
    public bool Descending {get;set;}
    public PlayerId? Selected {get;set;}
    public int Scroll {get;set;}
    public string Name {get;set;}="";
    public ClubId? Club {get;set;}
    public PlayerRole? Position {get;set;}
    public string MinimumAge {get;set;}="";
    public string MaximumAge {get;set;}="";
    public string? Attribute {get;set;}
    public void Reconcile(IReadOnlyList<MarketPlayer> rows){if(!rows.Any(p=>p.Details.Player.Id==Selected))Selected=null;}
}
