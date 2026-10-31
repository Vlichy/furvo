using Furvo.Core.Domain;
namespace Furvo.Core.Development;
public enum MobilityKind { Permanent=1,Temporary=2 }
public enum RecommendationStatus { Pending=1,Accepted=2,Rejected=3,Invalidated=4 }
public sealed record AcademyRuleKey(string Country,string Federation,string Season,string Category,string Competition);
public sealed record EligibilityDecision(bool Allowed,IReadOnlyList<string> Reasons,string EvidenceVersion);
public sealed record MobilityRequest(PlayerId Player,Guid Source,Guid Destination,MobilityKind Kind,DateOnly Date);
public sealed record EligibilityContext(ClubId Club,Player Player,Guid Source,Guid Destination,string DestinationCategory,string? Competition,DateOnly Date,int DestinationCount,string Country="",string Federation="",string Season="");
/// <summary>La licencia y los periodos deben proceder de normativa verificada. Desconocido nunca significa autorizado.</summary>
public interface IAcademyEligibility
{EligibilityDecision Check(EligibilityContext context,MobilityKind kind);}
public sealed class PendingAcademyEligibility:IAcademyEligibility
{
    public EligibilityDecision Check(EligibilityContext context,MobilityKind kind)=>new(false,new[]{"Falta verificar normativa específica de la competición, licencia del jugador y periodo de inscripción para 2026/27."},"ES-2026-27-pending");
}
public sealed record MovementRecommendation(Guid Id,Guid Coach,PlayerId Player,Guid Source,Guid Destination,MobilityKind Kind,string Reason,DateOnly Date,RecommendationStatus Status,string? Decision=null,int? Age=null,IReadOnlyDictionary<string,int>? RelevantAttributes=null,string? RegulatorySummary=null,string? Readiness=null);
public sealed record AcademyRule( AcademyRuleKey Key,DateOnly? EarliestBirth,DateOnly? LatestBirth,int? RegistrationLimit,string Source,DateOnly VerifiedAt,bool Complete);
public sealed class ConfiguredAcademyEligibility(AcademyRule rule,Func<EligibilityContext,bool?> licenseAndPeriod):IAcademyEligibility
{
    public EligibilityDecision Check(EligibilityContext c,MobilityKind kind)
    {
        var reasons=new List<string>();if(!rule.Complete||rule.Key.Country!=c.Country||rule.Key.Federation!=c.Federation||rule.Key.Season!=c.Season||rule.Key.Category!=c.DestinationCategory||rule.Key.Competition!=c.Competition||string.IsNullOrWhiteSpace(rule.Source))reasons.Add("Normativa incompleta o fuera de su ámbito.");
        if(c.Player.BirthDate is not {} birth)reasons.Add("Fecha de nacimiento desconocida.");else if(rule.EarliestBirth is {} min&&birth<min||rule.LatestBirth is {} max&&birth>max)reasons.Add("Edad no admitida por la categoría.");
        if(rule.RegistrationLimit is {} limit&&c.DestinationCount>=limit)reasons.Add("No hay plaza de inscripción.");if(licenseAndPeriod(c)!=true)reasons.Add("Licencia, vínculo, descensos o periodo de inscripción no autorizados.");if(!Enum.IsDefined(kind))reasons.Add("Operación desconocida.");
        return new(reasons.Count==0,reasons,rule.Source+"/"+rule.VerifiedAt.ToString("yyyy-MM-dd"));
    }
}
