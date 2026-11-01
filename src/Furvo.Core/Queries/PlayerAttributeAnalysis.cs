using System.Globalization;
using Furvo.Core.Domain;
namespace Furvo.Core.Queries;

public enum AttributeAnalysisSort { Name, Value, Relevance }
public sealed record AttributeAnalysisRow(string Code,int Value,int? ComparisonValue,bool Relevant);
public sealed record AttributeAnalysisView(PlayerSummary Player,PlayerSummary? Comparison,
    AttributeCategory Category,IReadOnlyList<AttributeAnalysisRow> Attributes);
public sealed record RecordedPlayerAttributes(Guid Record,DateOnly Date,PlayerId Player,IReadOnlyList<VisibleAttributeGroup> Groups);
public sealed record AttributeHistoryChange(string Code,DateOnly From,DateOnly To,int Before,int After);

/// <summary>Selección y comparación públicas de solo lectura; relevancia significa pertenencia, nunca una valoración global.</summary>
public static class PlayerAttributeAnalysis
{
    public static AttributeAnalysisView Select(IReadOnlyList<PlayerDetails> squad,PlayerId selected,PlayerId? comparison,
        AttributeCategory category,AttributeAnalysisSort sort=AttributeAnalysisSort.Name,bool descending=false)
    {
        if(category==AttributeCategory.Hidden||!Enum.IsDefined(category)||!Enum.IsDefined(sort))throw new ArgumentException("Categoría u orden de análisis no válido.");
        var player=Require(squad,selected);
        if(comparison==selected)throw new ArgumentException("Selecciona dos futbolistas distintos.");
        var other=comparison is {} id?Require(squad,id):null;
        var relevant=player.Player.Position is {} role?PositionInsightCatalog.RelevantFor(role).ToHashSet(StringComparer.Ordinal):new HashSet<string>(StringComparer.Ordinal);
        var values=PublicValues(player.AttributeGroups,category);var otherValues=other is null?null:PublicValues(other.AttributeGroups,category);
        var rows=values.Select(a=>new AttributeAnalysisRow(a.Key,a.Value,otherValues is not null&&otherValues.TryGetValue(a.Key,out var value)?value:null,relevant.Contains(a.Key)));
        var names=StringComparer.Create(CultureInfo.GetCultureInfo("es-ES"),true);
        string Name(AttributeAnalysisRow row)=>PlayerAttributeCatalog.Get(row.Code).Name;
        // Los empates siempre se resuelven por nombre y código; no existen pesos deportivos implícitos.
        IOrderedEnumerable<AttributeAnalysisRow> ordered=sort switch{
            AttributeAnalysisSort.Value=>descending?rows.OrderByDescending(a=>a.Value):rows.OrderBy(a=>a.Value),
            AttributeAnalysisSort.Relevance=>descending?rows.OrderBy(a=>a.Relevant):rows.OrderByDescending(a=>a.Relevant),
            _=>descending?rows.OrderByDescending(Name,names):rows.OrderBy(Name,names)};
        return new(player.Player,other?.Player,category,ordered.ThenBy(Name,names).ThenBy(a=>a.Code,StringComparer.Ordinal).ToArray());
    }
    /// <summary>Solo diferencias entre instantáneas registradas en días distintos; no extrapola ni convierte el acumulador en historial.</summary>
    public static IReadOnlyList<AttributeHistoryChange> History(PlayerId player,AttributeCategory category,IReadOnlyList<RecordedPlayerAttributes> records)
    {
        if(category==AttributeCategory.Hidden||!Enum.IsDefined(category))throw new ArgumentException("Categoría no visible.");
        var days=records.Where(r=>r.Player==player&&r.Date!=default).OrderBy(r=>r.Date).ThenBy(r=>r.Record).GroupBy(r=>r.Date)
            .Select(g=>(Date:g.Key,Values:g.Select(r=>PublicValues(r.Groups,category)).ToArray()))
            // Sin hora de observación no se elige arbitrariamente entre valores contradictorios del mismo día.
            .Where(g=>g.Values.All(v=>v.OrderBy(p=>p.Key).SequenceEqual(g.Values[0].OrderBy(p=>p.Key))))
            .Select(g=>(g.Date,Values:g.Values[0])).ToArray();
        var result=new List<AttributeHistoryChange>();
        for(var i=1;i<days.Length;i++)foreach(var value in days[i].Values)
            if(days[i-1].Values.TryGetValue(value.Key,out var before)&&before!=value.Value)result.Add(new(value.Key,days[i-1].Date,days[i].Date,before,value.Value));
        return result;
    }
    private static PlayerDetails Require(IReadOnlyList<PlayerDetails> squad,PlayerId player)=>
        squad.SingleOrDefault(p=>p.Player.Id==player)??throw new KeyNotFoundException("El futbolista no pertenece a esta plantilla.");
    private static Dictionary<string,int> PublicValues(IReadOnlyList<VisibleAttributeGroup> groups,AttributeCategory category)
    {
        var allowed=PlayerAttributeCatalog.All.Where(d=>!d.IsHidden&&d.Category==category).Select(d=>d.Code).ToHashSet(StringComparer.Ordinal);
        var values=groups.Where(g=>g.Category==category).SelectMany(g=>g.Values).Where(a=>allowed.Contains(a.Code)).ToArray();
        if(values.Any(a=>a.Value is <1 or >20)||values.Select(a=>a.Code).Distinct().Count()!=values.Length)throw new InvalidDataException("Atributos públicos inválidos.");
        return values.ToDictionary(a=>a.Code,a=>a.Value,StringComparer.Ordinal);
    }
}
