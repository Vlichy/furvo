using Furvo.Core.Domain;
using Furvo.Core.Development;
namespace Furvo.Core.Academy;
/// <summary>Observación humana, sin atributos ni evaluación automática; el autor queda fijado al crearla.</summary>
public sealed record AcademyObservation(Guid Id,PlayerId Player,Guid Team,DateOnly RegisteredOn,Guid Author,string AuthorName,string Text,DateOnly? ModifiedOn=null,int Revision=1)
{
    public void Validate(){if(Id==Guid.Empty||Player.Value==Guid.Empty||Team==Guid.Empty||Author==Guid.Empty||RegisteredOn==default||string.IsNullOrWhiteSpace(AuthorName)||string.IsNullOrWhiteSpace(Text)||Revision<1||ModifiedOn is {} modified&&modified<RegisteredOn)throw new InvalidDataException("Observación sin identidad, autor, texto o fechas válidos.");}
    public bool CanEdit(Guid actor,Guid principal)=>actor!=Guid.Empty&&(actor==Author||actor==principal);
}
public sealed record AcademyAttributeComparison(string Code,int First,int Second);
public static class AcademyFollowup
{
    public static bool ContainsPlayer(AnalysisReport report,PlayerId player)=>report.Context?.Players.Any(p=>p.Details.Player.Id==player)==true||report.Context is null&&report.Player==player;
    /// <summary>Compara instantáneas registradas, incluso del mismo día; no calcula progresión ni escribe informes.</summary>
    public static IReadOnlyList<AcademyAttributeComparison> Compare(AnalysisReport first,AnalysisReport second,PlayerId player)
    {
        if(first.Id==second.Id)throw new InvalidDataException("Selecciona informes distintos.");AnalysisReportRules.ValidateSnapshot(first);AnalysisReportRules.ValidateSnapshot(second);
        var a=first.Context!.Players.SingleOrDefault(p=>p.Details.Player.Id==player)??throw new InvalidDataException("El primer informe no contiene al jugador.");var b=second.Context!.Players.SingleOrDefault(p=>p.Details.Player.Id==player)??throw new InvalidDataException("El segundo informe no contiene al jugador.");
        var values=b.Details.AttributeGroups.SelectMany(g=>g.Values).ToDictionary(v=>v.Code,v=>v.Value);
        return a.Details.AttributeGroups.SelectMany(g=>g.Values).Select(v=>new AcademyAttributeComparison(v.Code,v.Value,values[v.Code])).ToArray();
    }
}
