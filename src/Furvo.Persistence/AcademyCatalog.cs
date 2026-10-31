using System.Text.Json;
using System.Security.Cryptography;
using Furvo.Core.Academy;
using Furvo.Core.Domain;
namespace Furvo.Persistence;
/// <summary>Solo identidades ficticias y metadatos acreditados; las fuentes reales viven en documentación interna.</summary>
public static class AcademyCatalog
{
    private sealed record Row(Guid ClubId,string Code,string Name,string Category,int Level,int Size,string? Competition);
    private sealed record Definition(string Version,Row[] Teams);
    private static readonly byte[] Bytes=ReadBytes();
    private static byte[] ReadBytes(){using var s=typeof(AcademyCatalog).Assembly.GetManifestResourceStream("Furvo.Persistence.Data.academy-2026-27.v1.json")??throw new InvalidDataException("Falta catálogo de cantera.");using var m=new MemoryStream();s.CopyTo(m);return m.ToArray();}
    public static string Hash=>Convert.ToHexString(SHA256.HashData(Bytes)).ToLowerInvariant();
    public static IReadOnlyList<AcademyTeam> Teams{get;}=Read();
    private static IReadOnlyList<AcademyTeam> Read(){var d=JsonSerializer.Deserialize<Definition>(Bytes)??throw new InvalidDataException("Catálogo vacío.");if(d.Version!=AcademyGenerator.Version)throw new NotSupportedException("Versión de catálogo de cantera desconocida.");var teams=d.Teams.Select(r=>new AcademyTeam(AcademyGenerator.Identity(r.ClubId+"/team/"+r.Code),new ClubId(r.ClubId),r.Name,r.Category,r.Level,r.Size,r.Competition)).OrderBy(t=>t.Id).ToArray();if(teams.Select(t=>t.Id).Distinct().Count()!=teams.Length||teams.Select(t=>t.ClubId).Distinct().Count()!=20)throw new InvalidDataException("Inventario de cantera incompleto o repetido.");return Array.AsReadOnly(teams);}
}
