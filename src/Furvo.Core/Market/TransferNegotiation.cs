using System.Globalization;
using Furvo.Core.Domain;
namespace Furvo.Core.Market;

public enum NegotiationState { Draft,Sent,PendingResponse,CounterReceived,AcceptedBetweenClubs,Rejected,Withdrawn,Expired,PendingPlayers,Cancelled }
public enum BonusObjective { OfficialAppearances,Starts,Goals,Assists,CleanSheets,InternationalCaps,Promotion,Survival,EuropeanQualification,LeagueTitle,CupTitle,InternationalTitle }
public enum FutureFeeBasis { FutureTransferFee,ContractualProfit }
public enum ArrivalKind { Immediate,NextWindow,FutureDate }
public enum TransferPaymentState { Proposed,PendingExecution,Paid,Cancelled }
public sealed record TransferPayment(Guid Id,int Order,decimal Amount,DateOnly Due,TransferPaymentState State=TransferPaymentState.Proposed);
public sealed record TransferBonus(Guid Id,BonusObjective Objective,int Threshold,decimal Amount,DateOnly From,DateOnly Until,int MaximumActivations,bool Cumulative,string Scope);
public sealed record FutureFeeClause(Guid Id,ClubId Beneficiary,PlayerId Player,decimal Percentage,FutureFeeBasis Basis,string Calculation,DateOnly From,DateOnly Until,string Activation,string? StandardCode=null,decimal? AcquisitionFixedBasis=null);
public sealed record BuybackClause(decimal Amount,DateOnly From,DateOnly Until,string Conditions,string? StandardCode=null,int? DurationYears=null);
public sealed record MatchingClause(DateOnly Until,int ResponseDays,string Notification,string MatchingCriteria,string? StandardCode=null,int? DurationYears=null);
public sealed record TransferTerms(string Currency,decimal FixedAmount,IReadOnlyList<TransferPayment> Payments,IReadOnlyList<PlayerId> OfferedPlayers,
    IReadOnlyList<TransferBonus> Bonuses,IReadOnlyList<FutureFeeClause> FutureFees,BuybackClause? Buyback,MatchingClause? Matching,
    ArrivalKind Arrival,DateOnly? IntendedArrival,DateOnly Expires,bool ExplicitFreeProposal=false,TransferPaymentPlan? PaymentPlan=null)
{
    public decimal Immediate(DateOnly date)=>Payments.Where(p=>p.Due<=date).Sum(p=>p.Amount);
    public decimal MaximumBonuses=>Bonuses.Sum(b=>checked(b.Amount*b.MaximumActivations));
    public void Validate(DateOnly date,ClubId seller,PlayerId target,bool sending,TransferMoneyPolicy? policy=null,DateOnly? proposalDate=null)
    {
        TransferRules.Money(FixedAmount,policy);if(Currency.Length!=3||Currency.Any(c=>c is <'A' or >'Z'))throw new InvalidDataException("Moneda no válida.");
        if(!Enum.IsDefined(Arrival)||Arrival==ArrivalKind.FutureDate&&IntendedArrival is null||IntendedArrival is {} arrival&&arrival<date||Expires<date)throw new InvalidDataException("Fechas de propuesta no válidas.");
        if(OfferedPlayers.Distinct().Count()!=OfferedPlayers.Count||OfferedPlayers.Any(p=>p.Value==Guid.Empty||p==target))throw new InvalidDataException("Jugadores incluidos inválidos o duplicados.");
        TransferRules.Unique(Payments.Select(p=>p.Id));TransferRules.Unique(Bonuses.Select(p=>p.Id));TransferRules.Unique(FutureFees.Select(p=>p.Id));
        var referenceDate=proposalDate??date;
        var earliestPayment=Arrival==ArrivalKind.FutureDate?IntendedArrival!.Value:referenceDate;
        if(Payments.Select(p=>p.Order).Distinct().Count()!=Payments.Count||Payments.Any(p=>p.Order<1||p.Due<earliestPayment||p.State!=TransferPaymentState.Proposed))throw new InvalidDataException("Plazos o vencimientos inválidos.");
        foreach(var p in Payments){TransferRules.Money(p.Amount,policy);if(p.Amount<=0)throw new InvalidDataException("Un plazo debe tener importe positivo.");}
        var ordered=Payments.OrderBy(p=>p.Order).ToArray();
        for(var i=1;i<ordered.Length;i++)if(ordered[i].Due<ordered[i-1].Due)throw new InvalidDataException("Los vencimientos deben respetar el orden de pago.");
        if(Payments.Sum(p=>p.Amount)!=FixedAmount)throw new InvalidDataException("La suma de plazos debe coincidir con el importe fijo.");
        foreach(var b in Bonuses){
            TransferRules.Money(b.Amount,policy);
            if(!Enum.IsDefined(b.Objective)||b.Threshold<1||b.Amount<=0||b.From<referenceDate||b.Until<b.From||b.MaximumActivations<1||!b.Cumulative&&b.MaximumActivations!=1||string.IsNullOrWhiteSpace(b.Scope))throw new InvalidDataException("Variable incompleta o incompatible.");
        }
        TransferPaymentPlans.ValidateTerms(this,referenceDate,policy);
        _=MaximumBonuses;
        foreach(var f in FutureFees){
            if(f.Beneficiary!=seller||f.Player!=target)throw new InvalidDataException("La cláusula propuesta solo puede beneficiar al vendedor por el jugador solicitado.");
            TransferRules.Percentage(f.Percentage);TransferOfferPresets.ValidateStandard(f,FixedAmount,policy);
            if(!Enum.IsDefined(f.Basis)||string.IsNullOrWhiteSpace(f.Calculation)||string.IsNullOrWhiteSpace(f.Activation)||f.From<referenceDate||f.Until<f.From)throw new InvalidDataException("Base, activación o vigencia de futura venta incompletas.");
        }
        foreach(var group in FutureFees.GroupBy(f=>(f.Basis,f.Calculation.Trim().ToUpperInvariant())))if(group.Sum(f=>f.Percentage)>100)throw new InvalidDataException("Porcentaje de la misma base superior a 100.");
        if(Buyback is {} buy){TransferRules.Money(buy.Amount,policy);if(buy.Amount<=0||buy.From<referenceDate||buy.Until<buy.From||string.IsNullOrWhiteSpace(buy.Conditions))throw new InvalidDataException("Recompra incompleta.");}
        if(Matching is {} match&&(match.Until<date||match.ResponseDays<1||string.IsNullOrWhiteSpace(match.Notification)||string.IsNullOrWhiteSpace(match.MatchingCriteria)))throw new InvalidDataException("Tanteo incompleto.");
        if(Buyback is {StandardCode:not null} standardBuy&&standardBuy!=TransferOfferPresets.Buyback(standardBuy.Amount,standardBuy.From,standardBuy.DurationYears??0,policy))throw new InvalidDataException("Recompra estándar incoherente.");
        if(Matching is {StandardCode:not null} standardMatch&&(standardMatch.DurationYears is null||standardMatch!=TransferOfferPresets.Matching(standardMatch.Until.AddDays(1).AddYears(-standardMatch.DurationYears.Value),standardMatch.DurationYears.Value,standardMatch.ResponseDays)))throw new InvalidDataException("Tanteo estándar incoherente.");
        if(FixedAmount==0&&OfferedPlayers.Count==0&&!ExplicitFreeProposal)throw new InvalidDataException("Una propuesta gratuita sin intercambio debe declararse expresamente; su viabilidad sigue pendiente.");
    }
}
public sealed record NegotiationVersion(Guid Id,Guid Career,ClubId Buyer,ClubId Seller,PlayerId Target,int Revision,DateOnly Date,
    NegotiationState State,TransferTerms Terms,string Action,string? Reason);
public enum ClubDecisionKind { Counter,Accept,Reject }
public sealed record ClubDecision(Guid Negotiation,int Revision,ClubId RespondingClub,ClubDecisionKind Kind,TransferTerms? CounterTerms,string Reason);
/// <summary>Punto de integración, sin implementación automática en esta entrega. Solo decisiones verificadas del club vendedor.</summary>
public interface IClubNegotiationDecisions { ClubDecision Decide(NegotiationVersion proposal); }
public static class TransferRules
{
    public static void Money(decimal value,TransferMoneyPolicy? policy=null){var limits=policy??TransferMoneyPolicy.Default;limits.Validate();if(value<0||decimal.Round(value,2)!=value||value>limits.MaximumIndividual)throw new InvalidDataException("Introduce un importe entre 0 y "+limits.MaximumIndividual.ToString("N2",CultureInfo.GetCultureInfo("es-ES"))+", con hasta dos decimales. Límite técnico, no presupuesto.");}
    public static decimal ParseMoney(string text,TransferMoneyPolicy? policy=null)
    {
        var normalized=text.Trim().Replace(',','.');if(normalized.StartsWith('-'))throw new InvalidDataException("No se admiten importes negativos.");var separator=normalized.IndexOf('.');
        if(separator>=0&&normalized.Length-separator-1>2)throw new InvalidDataException("Introduce como máximo dos decimales y no utilices separadores de miles.");
        if(!decimal.TryParse(normalized,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var value))throw new InvalidDataException("Introduce un importe sin separadores de miles, con hasta dos decimales.");
        Money(value,policy);return value;
    }
    public static void Percentage(decimal value){if(value<=0||value>100||decimal.Round(value,2)!=value)throw new InvalidDataException("El porcentaje debe ser mayor que cero y no superar 100, con dos decimales como máximo.");}
    public static void Unique(IEnumerable<Guid> ids){var a=ids.ToArray();if(a.Any(g=>g==Guid.Empty)||a.Distinct().Count()!=a.Length)throw new InvalidDataException("Identificadores de condiciones vacíos o duplicados.");}
    public static void Transition(NegotiationState from,NegotiationState to)
    {
        var valid=from switch{
            NegotiationState.Draft=>to is NegotiationState.Sent or NegotiationState.Cancelled,
            NegotiationState.Sent=>to is NegotiationState.PendingResponse or NegotiationState.CounterReceived or NegotiationState.AcceptedBetweenClubs or NegotiationState.Rejected or NegotiationState.Withdrawn or NegotiationState.Expired,
            NegotiationState.PendingResponse=>to is NegotiationState.CounterReceived or NegotiationState.AcceptedBetweenClubs or NegotiationState.Rejected or NegotiationState.Withdrawn or NegotiationState.Expired,
            NegotiationState.CounterReceived=>to is NegotiationState.Draft or NegotiationState.Withdrawn or NegotiationState.Expired,
            NegotiationState.AcceptedBetweenClubs=>to is NegotiationState.PendingPlayers,
            NegotiationState.PendingPlayers=>to is NegotiationState.Cancelled,
            _=>false};
        if(!valid)throw new InvalidOperationException("Transición de negociación no permitida.");
    }
}

/// <summary>Derecho de cobro entre clubes, nunca propiedad del jugador ni de su inscripción.</summary>
public enum ContractRightState { PendingActivation,Active,Extinguished }
public sealed record ContractualTransferRight(Guid Id,Guid Career,Guid OriginOperation,PlayerId Player,ClubId Debtor,ClubId Beneficiary,FutureFeeBasis Basis,
    decimal Percentage,string Calculation,DateOnly From,DateOnly Until,int Revision=1,ContractRightState State=ContractRightState.PendingActivation)
{
    public ContractualTransferRight Reduce(RightAmendmentAgreement agreement,DateOnly date,TransferMoneyPolicy? policy=null)
    {
        if(State!=ContractRightState.Active||Id==Guid.Empty||OriginOperation==Guid.Empty||agreement.Id==Guid.Empty||agreement.Right!=Id||agreement.Revision!=Revision||agreement.Debtor!=Debtor||agreement.Beneficiary!=Beneficiary||!agreement.DebtorAccepted||!agreement.BeneficiaryAccepted||Beneficiary==Debtor||Revision<1||string.IsNullOrWhiteSpace(Calculation)||!Enum.IsDefined(Basis)||Until<From||date<From||date>Until)throw new InvalidDataException("Derecho o acuerdo de modificación no válido.");
        TransferRules.Money(agreement.Price,policy);if(agreement.Currency.Length!=3)throw new InvalidDataException("Moneda de modificación inválida.");
        var extinguished=agreement.ExtinguishedPercentage;
        TransferRules.Percentage(Percentage);TransferRules.Percentage(extinguished);
        if(extinguished>Percentage)throw new InvalidDataException("No puede extinguirse más porcentaje que el vigente.");
        return this with{Percentage=Percentage-extinguished,Revision=checked(Revision+1),State=Percentage==extinguished?ContractRightState.Extinguished:ContractRightState.Active};
    }
}
public sealed record RightAmendmentProposal(Guid Id,Guid Right,int RightRevision,ClubId Debtor,ClubId Beneficiary,decimal ExtinguishedPercentage,decimal Price,string Currency);

public sealed record RightAmendmentAgreement(Guid Id,Guid Right,int Revision,ClubId Debtor,ClubId Beneficiary,decimal ExtinguishedPercentage,decimal Price,string Currency,bool DebtorAccepted,bool BeneficiaryAccepted);
