using Furvo.Core.Domain;
using Furvo.Core.Queries;
namespace Furvo.Core.Market;

public sealed record PaymentShare(int Months,decimal Percentage);
public sealed record TransferPaymentPlan(string Version,DateOnly ReferenceDate,IReadOnlyList<PaymentShare> Shares);
/// <summary>Propuesta relativa a formalización. La fecha de referencia es estimada, nunca un cobro ejecutado.</summary>
public static class TransferPaymentPlans
{
    public const string Version="formalization-shares-v1";
    public static IReadOnlyList<PaymentShare> Preset(int count)=>count switch{
        1=>new[]{new PaymentShare(0,100)},2=>new[]{new PaymentShare(0,50),new PaymentShare(12,50)},
        3=>new[]{new PaymentShare(0,50),new PaymentShare(12,25),new PaymentShare(24,25)},
        4=>new[]{new PaymentShare(0,40),new PaymentShare(12,20),new PaymentShare(24,20),new PaymentShare(36,20)},
        _=>throw new InvalidDataException("Selecciona entre uno y cuatro pagos.")};
    public static void Validate(TransferPaymentPlan plan)
    {
        if(plan.Version!=Version||plan.Shares.Count is <1 or >4)throw new InvalidDataException("Plan de pagos no compatible.");
        if(plan.Shares[0].Months!=0||plan.Shares.Any(s=>s.Months is <0 or >36||s.Percentage<=0||s.Percentage>100||decimal.Round(s.Percentage,2)!=s.Percentage)||plan.Shares.Sum(s=>s.Percentage)!=100)
            throw new InvalidDataException("El reparto debe sumar 100 %, con anticipo positivo y vencimientos entre 0 y 36 meses.");
        for(var i=1;i<plan.Shares.Count;i++)if(plan.Shares[i].Months<=plan.Shares[i-1].Months)throw new InvalidDataException("Los vencimientos deben ser posteriores y estar ordenados.");
        if(plan.ReferenceDate.Year>9996)throw new InvalidDataException("Fecha de referencia fuera de rango.");
    }
    public static IReadOnlyList<TransferPayment> Generate(decimal total,TransferPaymentPlan plan,IReadOnlyList<TransferPayment>? previous=null,TransferMoneyPolicy? policy=null)
    {
        TransferRules.Money(total,policy);Validate(plan);if(total==0)return Array.Empty<TransferPayment>();
        var rows=new List<TransferPayment>();decimal assigned=0;
        for(var i=0;i<plan.Shares.Count;i++){
            var share=plan.Shares[i];var amount=i==plan.Shares.Count-1?total-assigned:decimal.Floor(total*share.Percentage)/100;assigned+=amount;
            // Un importe mínimo no impone precio mínimo ni inventa pagos de cero; el residuo vence al final.
            if(amount==0)continue;
            var order=rows.Count+1;rows.Add(new(previous?.FirstOrDefault(p=>p.Order==order)?.Id??Guid.NewGuid(),order,amount,plan.ReferenceDate.AddMonths(share.Months)));
        }
        return rows.AsReadOnly();
    }
    public static void ValidateTerms(TransferTerms terms,DateOnly careerDate,TransferMoneyPolicy? policy=null)
    {
        if(terms.PaymentPlan is not {} plan)return;Validate(plan);
        var reference=terms.Arrival==ArrivalKind.FutureDate?terms.IntendedArrival:careerDate;
        if(reference is null||plan.ReferenceDate!=reference||plan.ReferenceDate<careerDate)throw new InvalidDataException("La referencia de pagos no coincide con la incorporación propuesta.");
        if(!TransferOfferPresets.SameSchedule(terms.Payments,Generate(terms.FixedAmount,plan,policy:policy)))throw new InvalidDataException("El calendario no coincide con su reparto porcentual.");
    }
}
public enum FinancialCheckState { ConsistentWithKnownData,ExceedsKnownAmount,Pending }
public sealed record TransferFinancialReview(FinancialCheckState Liquidity,FinancialCheckState Budget,decimal Advance,decimal Outstanding,int Months)
{
    /// <summary>Comparación informativa; no reserva fondos ni inventa compromisos o preferencias del vendedor.</summary>
    public static TransferFinancialReview Assess(TransferTerms terms,ClubFinancialResources? resources,DateOnly reference)
    {
        var advance=terms.Immediate(reference);FinancialCheckState Compare(decimal amount,decimal? limit)=>limit is null?FinancialCheckState.Pending:amount>limit?FinancialCheckState.ExceedsKnownAmount:FinancialCheckState.ConsistentWithKnownData;
        var usable=resources?.Currency==terms.Currency?resources:null;
        return new(Compare(advance,usable?.Cash),Compare(terms.FixedAmount,usable?.TransferRemaining),advance,terms.FixedAmount-advance,terms.PaymentPlan?.Shares.Max(s=>s.Months)??0);
    }
}
public enum ExchangeTeamScope { All,First,Reserve,Academy }
public static class TransferExchangeFilters
{
    public static ExchangeTeamScope? Scope(MarketPlayer player)=>player.Category switch{"First"=>ExchangeTeamScope.First,"Senior"=>ExchangeTeamScope.Reserve,"Juvenil"=>ExchangeTeamScope.Academy,_=>null};
    public static IReadOnlyList<TransferExchangeCandidate> Search(IReadOnlyList<TransferExchangeCandidate> candidates,string name,PlayerPosition? group=null,ExchangeTeamScope scope=ExchangeTeamScope.All)
    {
        if(group is {} g&&!Enum.IsDefined(g)||!Enum.IsDefined(scope))throw new InvalidDataException("Filtro deportivo desconocido.");
        var key=MarketQueries.NormalizeName(name);
        return candidates.Where(c=>MarketQueries.NormalizeName(c.Player.Details.Player.Name).Contains(key,StringComparison.Ordinal)&&(group is null||c.Player.Details.Player.Group==group)&&(scope==ExchangeTeamScope.All||Scope(c.Player)==scope)).ToArray();
    }
}
/// <summary>Selección transitoria: filtrar no pierde IDs y cancelar nunca modifica la selección confirmada.</summary>
public sealed class TransferExchangeDraft
{
    private readonly HashSet<PlayerId> _ids;
    public TransferExchangeDraft(IEnumerable<PlayerId> selected)=>_ids=selected.ToHashSet();
    public IReadOnlyList<PlayerId> Selected=>_ids.OrderBy(p=>p.ToString(),StringComparer.Ordinal).ToArray();
    public void Toggle(PlayerId id){if(!_ids.Remove(id))_ids.Add(id);}
}
