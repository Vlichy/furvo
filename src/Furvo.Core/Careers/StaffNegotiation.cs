using Furvo.Core.Domain;

namespace Furvo.Core.Careers;

public enum StaffOfferState { Draft, Sent, Counter, ClubAgreed, Conditional, Rejected, Withdrawn, Expired }
public enum StaffOfferPhase { Club, Personal }
public sealed record StaffOfferTerms(Guid Team, string Role, DateOnly Start, DateOnly End, decimal Salary,
    string Currency, decimal SigningBonus, decimal Compensation, IReadOnlyList<StaffClause> Clauses);
public sealed record StaffOffer(Guid Id, int Revision, Guid Employee, ClubId Buyer, ClubId? Origin,
    StaffOfferPhase Phase, StaffOfferState State, DateOnly Date, DateOnly Expires, StaffOfferTerms Terms,
    string Reason = "");

public static class StaffOfferRules
{
    public static Furvo.Core.Market.TransferMoneyPolicy MoneyPolicy { get; } = new(100_000_000m);
    public const int MaximumYears = 5;

    // El aniversario marca el primer día fuera del contrato. SQLite guarda el último día incluido.
    public static DateOnly Anniversary(DateOnly start, int years) => start.AddYears(years);
    public static DateOnly LastContractDay(DateOnly start, int years) => Anniversary(start, years).AddDays(-1);

    public static void ValidateIncorporation(DateOnly start, DateOnly careerDate)
    {
        if (start < careerDate)
            throw new InvalidDataException("La fecha de incorporación es anterior a la fecha actual de la carrera.");
        if (start.Year > 9994)
            throw new InvalidDataException("La fecha de incorporación no permite calcular un vencimiento válido.");
    }

    public static decimal EstimateCompensation(StaffProfile candidate, DateOnly date)
    {
        if (candidate.Employment is not { } employment || date < employment.Start || date > employment.End)
            return 0m;
        var remaining = (employment.End.DayNumber - date.DayNumber + 1) / 365m;
        var youth = candidate.Profession?.Attributes.GetValueOrDefault("YouthDevelopment", 11) ?? 11;
        var retention = 1m + (youth - 11) / 40m + ((candidate.Reputation ?? 1) - 1) / 20m;
        return decimal.Ceiling(employment.AnnualGross * remaining * retention);
    }

    public static void Validate(StaffOffer offer, DateOnly date)
    {
        var terms = offer.Terms;
        ValidateIncorporation(terms.Start, date);
        if (terms.Team == Guid.Empty || terms.Role != "AcademyCoach" ||
            terms.End <= terms.Start || terms.End > LastContractDay(terms.Start, MaximumYears))
            throw new InvalidDataException("Comprueba el equipo, las fechas y los importes de la propuesta.");
        foreach (var amount in new[] { terms.Salary, terms.SigningBonus, terms.Compensation }
            .Concat(terms.Clauses.Select(clause => clause.Amount).OfType<decimal>()))
            Furvo.Core.Market.TransferRules.Money(amount, MoneyPolicy);
        new StaffEmployment(offer.Id, offer.Employee, offer.Buyer, terms.Start, terms.End, terms.Salary, terms.Currency, terms.Clauses)
            .Validate(offer.Employee, offer.Buyer);
    }

    public static StaffOffer Evaluate(StaffOffer offer, StaffProfile candidate, DateOnly date, int buyerReputation, int originReputation,
        decimal freeSalaryReference)
    {
        if (offer.State != StaffOfferState.Sent) throw new InvalidOperationException("La propuesta no está pendiente.");
        if (date > offer.Expires) return offer with { State = StaffOfferState.Expired, Reason = "Vigencia superada." };
        var terms = offer.Terms;
        ValidateIncorporation(terms.Start, date);
        var employment = candidate.Employment;
        if (offer.Phase == StaffOfferPhase.Club)
        {
            if (employment is null || employment.End < date)
                return offer with { State = StaffOfferState.Rejected, Reason = "El vínculo de origen cambió." };
            var reserve = EstimateCompensation(candidate, date);
            if (terms.Compensation > 0 && terms.Compensation < reserve / 4m)
                return offer with { State = StaffOfferState.Rejected, Reason = "La compensación queda muy por debajo del coste del vínculo restante." };
            return terms.Compensation >= reserve
                ? offer with { State = StaffOfferState.ClubAgreed, Reason = "Compensación suficiente para el coste de perder al técnico." }
                : offer with { State = StaffOfferState.Counter, Terms = terms with { Compensation = reserve }, Reason = "El club pide cubrir el vínculo restante y la capacidad del técnico." };
        }
        var current = employment?.AnnualGross ?? freeSalaryReference;
        var minimum = decimal.Ceiling(current * (1m - (buyerReputation - originReputation) * .025m));
        var reduction = terms.Clauses.FirstOrDefault(clause => clause.Kind == StaffClauseKind.RelegationReduction)?.Percentage ?? 0;
        var adjusted = terms.Salary * (1m - reduction / 400m);
        if (adjusted < minimum * .65m)
            return offer with { State = StaffOfferState.Rejected, Reason = "La remuneración y las cláusulas quedan lejos de las condiciones actuales." };
        if (adjusted < minimum || employment is not null && terms.End < employment.End)
            return offer with { State = StaffOfferState.Counter, Terms = terms with
            {
                Salary = decimal.Ceiling(minimum / (1m - reduction / 400m)),
                End = employment is not null && terms.End < employment.End ? employment.End : terms.End
            }, Reason = "El técnico pide mantener su remuneración ajustada y su estabilidad contractual." };
        return offer with { State = StaffOfferState.Conditional, Reason = "Conformidad personal. Formalización pendiente de validar compromisos, baja y alta." };
    }
}
