namespace Furvo.Core.Domain;

public static class MonetaryLimits { public const decimal DefaultMaximum=1_000_000_000m; }
public enum ReleaseClausePresence { Unknown=0,Absent=1,Present=2 }
public enum ReleaseClauseDisclosure { ClubOnly=0,PublicConfirmed=1 }
public enum ReleaseClauseScope { AnyClub=0,Domestic=1,International=2 }
public enum ReleaseConditionKind { BuyerClub=1,Competition=2 }
public sealed record ReleaseClauseCondition(ReleaseConditionKind Kind,Guid Reference);
/// <summary>Dato del contrato, no del jugador. La ausencia del registro equivale a desconocido, nunca a sin cláusula.</summary>
public sealed record ContractReleaseClause(ContractId ContractId,ReleaseClausePresence Presence,decimal? Amount=null,string? Currency=null,
    DateOnly? From=null,DateOnly? Until=null,ReleaseClauseScope Scope=ReleaseClauseScope.AnyClub,IReadOnlyList<ReleaseClauseCondition>? Conditions=null,
    ReleaseClauseDisclosure Disclosure=ReleaseClauseDisclosure.ClubOnly,string? Evidence=null,DateOnly? ConfirmedOn=null);
public static class ContractReleaseRules
{
    public static void Validate(ContractReleaseClause clause,Contract contract)
    {
        if(clause.ContractId!=contract.Id||!Enum.IsDefined(clause.Presence)||!Enum.IsDefined(clause.Scope)||!Enum.IsDefined(clause.Disclosure))throw new InvalidDataException("Cláusula o referencia contractual inválida.");
        var conditions=clause.Conditions??Array.Empty<ReleaseClauseCondition>();
        if(clause.Presence!=ReleaseClausePresence.Present){if(clause.Amount is not null||clause.Currency is not null||clause.From is not null||clause.Until is not null||conditions.Count!=0||clause.Scope!=ReleaseClauseScope.AnyClub)throw new InvalidDataException("Sin cláusula o desconocida no admite importes ni condiciones de ejercicio.");}
        else{
            if(clause.Amount is not {} amount||amount<=0||amount>MonetaryLimits.DefaultMaximum||decimal.Round(amount,2)!=amount)throw new InvalidDataException("El importe de rescisión debe ser positivo, con dos decimales y dentro del máximo técnico.");
            if(clause.Currency is not {Length:3} currency||currency.Any(c=>c is <'A' or >'Z'))throw new InvalidDataException("Moneda de rescisión no válida.");
            if(clause.From is {} from&&(contract.StartDate is {} start&&from<start||contract.EndDate is {} end&&from>end)||clause.Until is {} until&&(contract.StartDate is {} start2&&until<start2||contract.EndDate is {} end2&&until>end2)||clause.From is {} a&&clause.Until is {} b&&b<a)throw new InvalidDataException("La vigencia de rescisión debe ser coherente con el contrato.");
            if(conditions.Any(c=>!Enum.IsDefined(c.Kind)||c.Reference==Guid.Empty)||conditions.Distinct().Count()!=conditions.Count)throw new InvalidDataException("Condiciones de rescisión inválidas o duplicadas.");
        }
        if(clause.Disclosure==ReleaseClauseDisclosure.PublicConfirmed&&(string.IsNullOrWhiteSpace(clause.Evidence)||clause.ConfirmedOn is null||clause.Presence==ReleaseClausePresence.Unknown))throw new InvalidDataException("La información pública exige confirmación documentada y fecha.");
    }
}
/// <summary>Puerto preparatorio: una rescisión no es una oferta aceptada ni dispone de ejecución en esta entrega.</summary>
public enum PlayerAcquisitionProcedure { ClubNegotiation=1,ContractRelease=2 }
public enum ReleaseProcedureRequirement { ApplicableClause,SpecialConditions,AvailableFunds,PaymentObligations,Registration,PlayerAgreement,TransactionalHistory }
public interface IReleaseProcedureChecks { IReadOnlyDictionary<ReleaseProcedureRequirement,bool?> Assess(ContractId contract,ClubId buyer,DateOnly date); }
