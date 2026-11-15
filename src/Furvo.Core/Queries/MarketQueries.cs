using Furvo.Core.Domain;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
namespace Furvo.Core.Queries;
public enum MarketSort { Name,Age,Club,Position,ContractEnd,Attribute }
/// <summary>Registro contractual público, sin salario, valor, cláusulas ni disponibilidad inferida.</summary>
public sealed record MarketContract(ContractId Id,ClubSummary Club,DateOnly? Start,DateOnly? End,bool? Active,ReleaseClauseInfo? Release=null);
public sealed record MarketPlayer(PlayerDetails Details,Guid? Team,string? TeamName,string? Category,MarketContract? Contract);
public sealed record MarketSnapshot(Guid Career,DateOnly Date,IReadOnlyList<ClubSummary> Clubs,IReadOnlyList<MarketPlayer> Players,ClubId? ManagedClub=null);
public sealed record MarketFilter(string Name="",ClubId? Club=null,PlayerRole? Position=null,int? MinimumAge=null,int? MaximumAge=null,string? Attribute=null,int MinimumValue=1,int MaximumValue=20)
{
    public void Validate()
    {
        if(MinimumAge is <0||MaximumAge is <0||MinimumAge is {} min&&MaximumAge is {} max&&min>max||Position is {} role&&!Enum.IsDefined(role))throw new InvalidDataException("El rango de edad o la posición no es válido.");
        if(Attribute is {} code){var definition=PlayerAttributeCatalog.All.SingleOrDefault(a=>a.Code==code);if(definition is null||definition.IsHidden)throw new InvalidDataException("Solo se permite filtrar atributos públicos.");if(MinimumValue is <1 or >20||MaximumValue is <1 or >20||MinimumValue>MaximumValue)throw new InvalidDataException("El rango del atributo debe estar entre 1 y 20 y ordenado.");}
    }
}
public static class MarketQueries
{
    /// <summary>Intersección de filtros públicos. Sin escritura, potencial, puntuación global ni atributos ocultos.</summary>
    public static IReadOnlyList<MarketPlayer> Search(MarketSnapshot snapshot,MarketFilter filter,MarketSort sort=MarketSort.Name,bool descending=false,string? sortAttribute=null)
    {
        filter.Validate();if(!Enum.IsDefined(sort))throw new ArgumentOutOfRangeException(nameof(sort));if(filter.Club is {} club&&!snapshot.Clubs.Any(c=>c.Id==club))throw new InvalidDataException("Club ajeno a esta carrera.");
        if(sort==MarketSort.Attribute)RequirePublicAttribute(sortAttribute);
        var name=NormalizeName(filter.Name);
        var rows=snapshot.Players.Where(p=>IsExternal(snapshot,p)&&NormalizeName(p.Details.Player.Name).Contains(name,StringComparison.Ordinal)&&(filter.Club is null||p.Details.Club?.Id==filter.Club)&&(filter.Position is null||p.Details.Player.Position==filter.Position)&&
            (filter.MinimumAge is null||p.Details.Player.Age>=filter.MinimumAge)&&(filter.MaximumAge is null||p.Details.Player.Age<=filter.MaximumAge)&&
            (filter.Attribute is null||p.Details.AttributeGroups.SelectMany(g=>g.Values).Any(v=>v.Code==filter.Attribute&&v.Value>=filter.MinimumValue&&v.Value<=filter.MaximumValue)));
        var ordered=sort switch{
            MarketSort.Attribute=>descending?rows.OrderBy(p=>AttributeValue(p,sortAttribute!) is null).ThenByDescending(p=>AttributeValue(p,sortAttribute!)):rows.OrderBy(p=>AttributeValue(p,sortAttribute!) is null).ThenBy(p=>AttributeValue(p,sortAttribute!)),
            MarketSort.Age=>descending?rows.OrderBy(p=>p.Details.Player.Age is null).ThenByDescending(p=>p.Details.Player.Age):rows.OrderBy(p=>p.Details.Player.Age is null).ThenBy(p=>p.Details.Player.Age),
            MarketSort.Club=>descending?rows.OrderBy(p=>p.Details.Club is null).ThenByDescending(p=>p.Details.Club?.Name,StringComparer.OrdinalIgnoreCase):rows.OrderBy(p=>p.Details.Club is null).ThenBy(p=>p.Details.Club?.Name,StringComparer.OrdinalIgnoreCase),
            MarketSort.Position=>descending?rows.OrderBy(p=>p.Details.Player.Position is null).ThenByDescending(p=>p.Details.Player.Position):rows.OrderBy(p=>p.Details.Player.Position is null).ThenBy(p=>p.Details.Player.Position),
            MarketSort.ContractEnd=>descending?rows.OrderBy(p=>p.Contract?.End is null).ThenByDescending(p=>p.Contract?.End):rows.OrderBy(p=>p.Contract?.End is null).ThenBy(p=>p.Contract?.End),
            _=>descending?rows.OrderByDescending(p=>p.Details.Player.Name,StringComparer.OrdinalIgnoreCase):rows.OrderBy(p=>p.Details.Player.Name,StringComparer.OrdinalIgnoreCase)};
        return ordered.ThenBy(p=>p.Details.Player.Name,StringComparer.OrdinalIgnoreCase).ThenBy(p=>p.Details.Player.Id.ToString(),StringComparer.Ordinal).ToArray();
    }
    // Se normaliza solo la clave de consulta: el nombre original nunca cambia.
    public static string NormalizeName(string name)
    {
        var decomposed=name.Normalize(NormalizationForm.FormD);
        var plain=new string(decomposed.Where(c=>CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark).ToArray());
        return Regex.Replace(plain.Normalize(NormalizationForm.FormC).ToUpperInvariant().Trim(),@"\s+"," ");
    }
    public static void RequirePublicAttribute(string? code)
    {
        if(!PlayerAttributeCatalog.All.Any(a=>a.Code==code&&!a.IsHidden))throw new InvalidDataException("Solo se permiten atributos públicos existentes.");
    }
    public static int? AttributeValue(MarketPlayer player,string code)
    {
        RequirePublicAttribute(code);
        return player.Details.AttributeGroups.Where(g=>g.Category!=AttributeCategory.Hidden).SelectMany(g=>g.Values).Where(v=>v.Code==code).Select(v=>(int?)v.Value).SingleOrDefault();
    }
    /// <summary>Vacío significa límite abierto de la escala. No redondea ni recorta entradas inválidas.</summary>
    public static (int Minimum,int Maximum) ParseAttributeRange(string minimum,string maximum)
    {
        int Parse(string text,int fallback){
            if(string.IsNullOrWhiteSpace(text))return fallback;
            if(!int.TryParse(text,NumberStyles.None,CultureInfo.InvariantCulture,out var value)||value is <1 or >20)throw new InvalidDataException("Los límites del atributo deben ser enteros entre 1 y 20.");
            return value;
        }
        var min=Parse(minimum.Trim(),1);var max=Parse(maximum.Trim(),20);
        if(min>max)throw new InvalidDataException("El valor mínimo no puede superar al máximo.");
        return(min,max);
    }
    // Defensa adicional para consultas por ID o instantáneas construidas por otros consumidores.
    private static bool IsExternal(MarketSnapshot snapshot,MarketPlayer player)=>player.Details.Club is {} club&&club.Id!=snapshot.ManagedClub;
    public static MarketPlayer Player(MarketSnapshot snapshot,PlayerId id)=>snapshot.Players.SingleOrDefault(p=>IsExternal(snapshot,p)&&p.Details.Player.Id==id)??throw new KeyNotFoundException("Futbolista no disponible en esta carrera.");
}
