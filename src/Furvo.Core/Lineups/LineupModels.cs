using System.Text.Json;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
namespace Furvo.Core.Lineups;
public sealed record FormationSlot(string Id,PlayerRole Role,double X,double Y);
public sealed record FormationDefinition(string Id,IReadOnlyList<FormationSlot> Slots);
public enum LineupRole { Starter=1,Substitute=2,Reserve=3 }
public sealed record LineupMember(PlayerId PlayerId,LineupRole Role,string? SlotId);
public sealed record CareerLineup(ClubId ClubId,string Seed,FormationDefinition Formation,int BenchLimit,int Revision,IReadOnlyList<LineupMember> Members);
public sealed record LineupPlayer(PlayerId Id,string Name,PlayerRole Primary,IReadOnlyList<PlayerRole> Secondary,IReadOnlyDictionary<string,int> Visible)
{
    public static LineupPlayer From(PlayerDetails p)=>new(p.Player.Id,p.Player.Name,p.Player.Position??throw new InvalidDataException("Sin posición."),p.SecondaryPositions,
        p.AttributeGroups.Where(g=>g.Category!=AttributeCategory.Hidden).SelectMany(g=>g.Values).Where(v=>PlayerAttributeCatalog.All.Any(d=>d.Code==v.Code&&!d.IsHidden)).ToDictionary(v=>v.Code,v=>v.Value,StringComparer.Ordinal));
}
public static class FormationCatalog
{
    private sealed record ClubRow(Guid ClubId,string FormationId);
    private sealed record Data(string Version,int BenchLimit,FormationDefinition[] Formations,ClubRow[] Clubs);
    private static readonly Data Catalog=Read();
    private static Data Read()
    {
        using var s=typeof(FormationCatalog).Assembly.GetManifestResourceStream("Furvo.Formations")??throw new InvalidDataException("Falta catálogo de formaciones.");
        var d=JsonSerializer.Deserialize<Data>(s)??throw new InvalidDataException("Catálogo vacío.");
        foreach(var f in d.Formations)Validate(f);
        if(d.Clubs.Length!=20||d.Clubs.Select(c=>c.ClubId).Distinct().Count()!=20||d.Clubs.Any(c=>!d.Formations.Any(f=>f.Id==c.FormationId)))throw new InvalidDataException("Configuración de clubes incompleta.");
        return d;
    }
    public static IReadOnlyList<FormationDefinition> All=>Array.AsReadOnly(Catalog.Formations);
    public static IReadOnlyList<Guid> Clubs=>Array.AsReadOnly(Catalog.Clubs.Select(c=>c.ClubId).ToArray());
    public static int BenchLimit=>Catalog.BenchLimit;
    public static FormationDefinition Get(string id)=>All.Single(f=>f.Id==id);
    public static FormationDefinition Initial(ClubId club)=>Get(Catalog.Clubs.Single(c=>c.ClubId==club.Value).FormationId);
    public static void Validate(FormationDefinition f)
    {
        if(string.IsNullOrWhiteSpace(f.Id)||f.Slots.Count!=11||f.Slots.Select(s=>s.Id).Distinct().Count()!=11||f.Slots.Count(s=>s.Role==PlayerRole.Goalkeeper)!=1||f.Slots.Select(s=>(s.X,s.Y)).Distinct().Count()!=11||
           f.Slots.Any(s=>!Enum.IsDefined(s.Role)||string.IsNullOrWhiteSpace(s.Id)||s.X is <0 or >1||s.Y is <0 or >1 || !double.IsFinite(s.X)||!double.IsFinite(s.Y)))throw new InvalidDataException("Formación no válida.");
    }
}
