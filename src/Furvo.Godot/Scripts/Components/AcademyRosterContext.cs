using Furvo.Core.Domain;
using Furvo.Core.Queries;
namespace Furvo.Presentation.Components;
/// <summary>Contexto efímero por carrera/equipo; no pertenece al esquema de partida.</summary>
public sealed class AcademyRosterContext
{
    public PlayerId? Selected {get;set;}
    public int Sort {get;set;}
    public bool Descending {get;set;}
    public string NameFilter {get;set;}="";
    public PlayerRole? PositionFilter {get;set;}
    public int Section {get;set;}
    public IReadOnlyList<PlayerDetails> Rows(IReadOnlyList<PlayerDetails> squad)
    {
        var filtered=squad.Where(p=>p.Player.Name.Contains(NameFilter,StringComparison.OrdinalIgnoreCase)&&(PositionFilter is null||p.Player.Position==PositionFilter)).ToArray();
        if(!filtered.Any(p=>p.Player.Id==Selected))Selected=null;
        var ordered=Sort switch{1=>Descending?filtered.OrderByDescending(p=>p.Player.Position):filtered.OrderBy(p=>p.Player.Position),2=>Descending?filtered.OrderByDescending(p=>p.Player.Age):filtered.OrderBy(p=>p.Player.Age),_=>Descending?filtered.OrderByDescending(p=>p.Player.Name,StringComparer.OrdinalIgnoreCase):filtered.OrderBy(p=>p.Player.Name,StringComparer.OrdinalIgnoreCase)};
        return ordered.ThenBy(p=>p.Player.Id.ToString(),StringComparer.Ordinal).ToArray();
    }
}
public sealed class AcademyRosterContexts
{
    private readonly Dictionary<(Guid Career,Guid Team),AcademyRosterContext> _items=new();
    public AcademyRosterContext For(Guid career,Guid team){if(career==Guid.Empty||team==Guid.Empty)throw new ArgumentException("Contexto de cantera inválido.");if(!_items.TryGetValue((career,team),out var state))_items[(career,team)]=state=new();return state;}
    public void Clear()=>_items.Clear();
}
