using Furvo.Core.Domain;
using Furvo.Core.Queries;
namespace Furvo.Core.Market;

/// <summary>Límite técnico de captura de propuestas, independiente de presupuestos o reglas de viabilidad.</summary>
public sealed record TransferMoneyPolicy(decimal MaximumIndividual)
{
    public static TransferMoneyPolicy Default {get;}=new(MonetaryLimits.DefaultMaximum);
    public void Validate(){if(MaximumIndividual<=0||MaximumIndividual>long.MaxValue/100m||decimal.Round(MaximumIndividual,2)!=MaximumIndividual)throw new InvalidDataException("Configuración monetaria inválida.");}
}
public static class TransferOfferPresets
{
    public static IReadOnlyList<int> PaymentYears {get;}=Array.AsReadOnly(new[]{2,6,10});
    public static IReadOnlyList<int> RightYears {get;}=Array.AsReadOnly(new[]{1,2,3,5,10});
    public static IReadOnlyList<int> ResponseDays {get;}=Array.AsReadOnly(new[]{3,7,14,30});
    public const string SaleCode="gross-fixed-cash-v1",ProfitCode="positive-fixed-cash-surplus-v1",BuybackCode="fixed-buyback-v1",MatchingCode="equal-terms-notice-v1";
    public const string SaleDefinition="Importe fijo monetario bruto pactado en la próxima transferencia completada, sin deducciones; excluye variables y valoraciones de jugadores intercambiados.";
    public const string ProfitDefinition="Máximo entre cero y el importe fijo monetario bruto de la próxima transferencia menos el importe fijo de adquisición de esta operación; sin otras deducciones.";
    public const string Activation="Primera transferencia futura completada, con contraprestación monetaria y cobro verificables; sin ejecución automática.";
    public const string BuybackConditions="Precio fijo pactado. Ejercicio sujeto al acuerdo del futbolista, elegibilidad e inscripción verificadas; nunca automático.";
    public const string MatchingNotification="Notificación íntegra y verificable de la oferta futura. Días naturales desde su recepción acreditada.";
    public const string MatchingCriteria="Igualar íntegramente importes fijos, calendario de pagos, condiciones contingentes y contraprestaciones deportivas; sujeto a validación y consentimiento del futbolista.";
    /// <summary>Hasta N cuotas anuales al final de cada año. Redondea hacia abajo y ajusta la última; omite importes cero.</summary>
    public static IReadOnlyList<TransferPayment> Payments(decimal amount,DateOnly date,int? years=null,IReadOnlyList<TransferPayment>? previous=null,TransferMoneyPolicy? policy=null)
    {
        TransferRules.Money(amount,policy);if(years is {} n&&!PaymentYears.Contains(n))throw new InvalidDataException("Duración de pago no configurada.");
        if(amount==0)return Array.Empty<TransferPayment>();
        var count=years??1;var regular=decimal.Floor(amount*100/count)/100;var result=new List<TransferPayment>();decimal assigned=0;
        for(var i=0;i<count;i++){
            var value=i==count-1?amount-assigned:regular;assigned+=value;if(value==0)continue;
            var order=result.Count+1;var due=years is null?date:date.AddYears(i+1);
            result.Add(new(previous?.FirstOrDefault(p=>p.Order==order)?.Id??Guid.NewGuid(),order,value,due));
        }
        return result.AsReadOnly();
    }
    public static bool SameSchedule(IReadOnlyList<TransferPayment> a,IReadOnlyList<TransferPayment> b)=>a.Count==b.Count&&a.Zip(b).All(p=>p.First.Amount==p.Second.Amount&&p.First.Due==p.Second.Due&&p.First.Order==p.Second.Order&&p.First.State==p.Second.State);
    public static FutureFeeClause FutureFee(FutureFeeBasis basis,decimal percentage,ClubId seller,PlayerId player,DateOnly date,decimal acquisitionFixed,Guid? id=null,TransferMoneyPolicy? policy=null)
    {
        if(!Enum.IsDefined(basis))throw new InvalidDataException("Base desconocida.");TransferRules.Percentage(percentage);TransferRules.Money(acquisitionFixed,policy);
        return new(id??Guid.NewGuid(),seller,player,percentage,basis,basis==FutureFeeBasis.FutureTransferFee?SaleDefinition:ProfitDefinition,date,DateOnly.MaxValue,Activation,
            basis==FutureFeeBasis.FutureTransferFee?SaleCode:ProfitCode,basis==FutureFeeBasis.ContractualProfit?acquisitionFixed:null);
    }
    public static BuybackClause Buyback(decimal amount,DateOnly date,int years,TransferMoneyPolicy? policy=null)
    {if(!RightYears.Contains(years))throw new InvalidDataException("Duración no configurada.");TransferRules.Money(amount,policy);return new(amount,date,date.AddYears(years).AddDays(-1),BuybackConditions,BuybackCode,years);}
    public static MatchingClause Matching(DateOnly date,int years,int responseDays)
    {if(!RightYears.Contains(years)||!ResponseDays.Contains(responseDays))throw new InvalidDataException("Duración o plazo no configurado.");return new(date.AddYears(years).AddDays(-1),responseDays,MatchingNotification,MatchingCriteria,MatchingCode,years);}
    public static void ValidateStandard(FutureFeeClause clause,decimal fixedAmount,TransferMoneyPolicy? policy=null)
    {
        if(clause.StandardCode is null)return;
        var expected=FutureFee(clause.Basis,clause.Percentage,clause.Beneficiary,clause.Player,clause.From,fixedAmount,clause.Id,policy);
        if(clause!=expected)throw new InvalidDataException("La cláusula estándar no coincide con su definición versionada.");
    }
}
/// <summary>Entrada futura de un registro real de interés. No crea ni estima interés a partir de atributos.</summary>
public sealed record RecordedTransferInterest(ClubId Club,PlayerId Player,DateOnly RecordedOn,string Source);
public sealed record TransferExchangeCandidate(MarketPlayer Player,bool Interested);
public static class TransferExchangeSelection
{
    public static IReadOnlyList<TransferExchangeCandidate> Candidates(MarketSnapshot snapshot,ClubId buyer,ClubId seller,PlayerId target,IReadOnlyList<RecordedTransferInterest>? interests=null)
    {
        var known=(interests??Array.Empty<RecordedTransferInterest>()).Where(i=>i.Club==seller&&i.RecordedOn<=snapshot.Date&&!string.IsNullOrWhiteSpace(i.Source)).Select(i=>i.Player).ToHashSet();
        return snapshot.Players.Where(p=>p.Details.Club?.Id==buyer&&p.Details.Player.Id!=target).GroupBy(p=>p.Details.Player.Id).Select(g=>new TransferExchangeCandidate(g.First(),known.Contains(g.Key)))
            .OrderByDescending(p=>p.Interested).ThenBy(p=>p.Player.Details.Player.Name,StringComparer.OrdinalIgnoreCase).ThenBy(p=>p.Player.Details.Player.Id.ToString(),StringComparer.Ordinal).ToArray();
    }
}
