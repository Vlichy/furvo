using Furvo.Core.Careers;
using Microsoft.Data.Sqlite;

namespace Furvo.Persistence;

public sealed class SqliteStaffOffers(string path, CareerSummary career)
{
    private static bool Exists(SqliteConnection c, SqliteTransaction t)
    {
        using var q = WorldDatabase.Command(c,t,"SELECT count(*) FROM sqlite_master WHERE name='staff_offer_versions'");
        return Convert.ToInt32(q.ExecuteScalar()) == 1;
    }

    private static IReadOnlyList<StaffOffer> Read(SqliteConnection c, SqliteTransaction t, Guid? id)
    {
        if (!Exists(c,t)) return [];
        using var q = WorldDatabase.Command(c,t,"SELECT payload FROM staff_offer_versions WHERE $id IS NULL OR id=$id ORDER BY revision",
            ("$id",id?.ToString("D")));
        using var r = q.ExecuteReader();
        var offers = new List<StaffOffer>();
        while (r.Read()) offers.Add(CareerJson.Read<StaffOffer>(r.GetString(0)));
        return offers;
    }

    public IReadOnlyList<StaffOffer> History(Guid? id = null)
    {
        using var c = WorldDatabase.Open(path);
        using var t = c.BeginTransaction(deferred:true);
        CheckOwner(c,t);
        return Read(c,t,id);
    }

    private void CheckOwner(SqliteConnection c, SqliteTransaction t)
    {
        var owner = SqliteLineupStore.Owner(c,t);
        if (owner.Save != career.Id || owner.Club is null || owner.Club != career.ClubId || owner.Date != career.CurrentDate)
            throw new InvalidOperationException("La carrera o su fecha cambió.");
    }

    public StaffOffer Draft(Guid employee, StaffOfferTerms terms)
    {
        var candidate = Candidate(employee);
        var phase = candidate.ContractStatus(career.CurrentDate) == StaffContractStatus.Contracted
            ? StaffOfferPhase.Club : StaffOfferPhase.Personal;
        if (phase == StaffOfferPhase.Club && candidate.Employment is null)
            throw new InvalidOperationException("Faltan condiciones del contrato de origen.");
        if (phase == StaffOfferPhase.Personal && terms.Compensation != 0)
            throw new InvalidOperationException("Un empleado libre no requiere compensación a un club.");
        var offer = new StaffOffer(Guid.NewGuid(), 1, employee, career.ClubId!.Value,
            phase == StaffOfferPhase.Club ? candidate.Club : null,
            phase, StaffOfferState.Draft, career.CurrentDate, career.CurrentDate.AddDays(14), terms);
        Validate(offer);
        return Persist(offer, null);
    }

    public StaffOffer Move(StaffOffer expected, string action, StaffOfferTerms? revised = null)
    {
        var date = career.CurrentDate;
        StaffOffer next;
        if (expected.State is not (StaffOfferState.Draft or StaffOfferState.Sent or StaffOfferState.Counter or StaffOfferState.ClubAgreed or StaffOfferState.Conditional))
            throw new InvalidOperationException("La negociación está cerrada.");
        if (action == "Withdraw") next = expected with { State = StaffOfferState.Withdrawn, Reason = "Retirada por el mánager." };
        else if (date > expected.Expires) next = expected with { State = StaffOfferState.Expired, Reason = "Vigencia superada." };
        else
        {
            var candidate = Candidate(expected.Employee);
            var origin = candidate.ContractStatus(date) == StaffContractStatus.Contracted ? candidate.Club : null;
            if (origin != expected.Origin) throw new InvalidOperationException("Cambió la vinculación del empleado.");
            next = (expected.State,action) switch
            {
                (StaffOfferState.Draft,"Send") => expected with { State = StaffOfferState.Sent, Reason = "" },
                (StaffOfferState.Counter or StaffOfferState.Draft,"Revise") when revised is not null => expected with { State = StaffOfferState.Draft, Terms = revised, Reason = "" },
                (StaffOfferState.ClubAgreed,"Personal") => expected with { Phase = StaffOfferPhase.Personal, State = StaffOfferState.Draft, Reason = "" },
                (StaffOfferState.Sent,"Evaluate") => Evaluate(expected,candidate,date),
                _ => throw new InvalidOperationException("La acción no corresponde al estado actual.")
            };
            if (next.State is StaffOfferState.Draft or StaffOfferState.Sent) Validate(next);
        }
        if (revised is not null && expected.Phase == StaffOfferPhase.Personal && revised.Compensation != expected.Terms.Compensation)
            throw new InvalidOperationException("La compensación acordada se conserva.");
        return Persist(next with { Revision = checked(expected.Revision + 1), Date = date }, expected);
    }

    private StaffOffer Evaluate(StaffOffer offer, StaffProfile candidate, DateOnly date)
    {
        Validate(offer);
        var clubs = new SqliteWorldStore(path).Load().Clubs;
        var reference = SqliteEconomyStore.ReadResources(path).Single(r => r.ClubId == offer.Buyer && r.SeasonId == career.SeasonId).EconomicPlan!;
        var response = StaffOfferRules.Evaluate(offer,candidate,date,
            clubs.Single(club => club.Id == offer.Buyer).Reputation ?? 1,
            clubs.SingleOrDefault(club => club.Id == offer.Origin)?.Reputation ?? 1,
            Math.Max(ContractCountryPolicies.For("España").MinimumGrossAnnualSalary,
                reference.ProposedManagerGrossAnnualSalary * (.075m + ((candidate.Reputation ?? 1) - 1) * .015m)));
        return response.State == StaffOfferState.Counter &&
            History(offer.Id).Count(o => o.State == StaffOfferState.Counter && o.Phase == offer.Phase) >= 3
            ? offer with { State = StaffOfferState.Rejected, Reason = "Se agotaron las tres contraofertas de esta fase." }
            : response;
    }

    private StaffProfile Candidate(Guid id)
    {
        var candidate = SqliteStaffDirectory.Read(path,career).SingleOrDefault(p => p.Id == id);
        if (candidate is null || candidate.Club is { } club && club == career.ClubId ||
            candidate.ContractStatus(career.CurrentDate) == StaffContractStatus.Unknown)
            throw new InvalidOperationException("El empleado carece de una situación contractual apta para negociar.");
        return candidate;
    }

    private void Validate(StaffOffer offer)
    {
        StaffOfferRules.Validate(offer,career.CurrentDate);
        if (offer.Buyer != career.ClubId || !new SqliteClubSquadStore(path).Teams().Any(t => t.Id == offer.Terms.Team && t.Category != "First"))
            throw new InvalidOperationException("Selecciona un equipo inferior de tu club.");
        var plan = SqliteEconomyStore.ReadResources(path).SingleOrDefault(r => r.ClubId == offer.Buyer && r.SeasonId == career.SeasonId)?.EconomicPlan;
        if (plan is null || offer.Terms.Currency != plan.Currency ||
            offer.Terms.Salary < ContractCountryPolicies.For("España").MinimumGrossAnnualSalary || offer.Terms.Salary > plan.StaffBudget)
            throw new InvalidOperationException("El salario o la moneda quedan fuera del plan económico disponible.");
    }

    private StaffOffer Persist(StaffOffer next, StaffOffer? expected)
    {
        using var c = WorldDatabase.Open(path,SqliteOpenMode.ReadWrite);
        using var t = c.BeginTransaction();
        CheckOwner(c,t);
        if (!Exists(c,t)) CareerJson.Migrate(c,t,"StaffOffers001_Initial.sql");
        var latest = Read(c,t,next.Id).LastOrDefault();
        if (expected is null ? latest is not null : latest is null || CareerJson.Write(latest) != CareerJson.Write(expected))
            throw new InvalidOperationException("La negociación cambió. Vuelve a abrirla.");
        if (expected is null && Read(c,t,null).GroupBy(o => o.Id).Select(g => g.Last()).Any(o => o.Employee == next.Employee &&
            o.Expires >= career.CurrentDate && o.State is not (StaffOfferState.Rejected or StaffOfferState.Withdrawn or StaffOfferState.Expired)))
            throw new InvalidOperationException("Ya existe una negociación activa para este empleado.");
        using var q = WorldDatabase.Command(c,t,"INSERT INTO staff_offer_versions VALUES($id,$revision,$employee,$buyer,$payload)",
            ("$id",next.Id.ToString("D")),("$revision",next.Revision),("$employee",next.Employee.ToString("D")),
            ("$buyer",next.Buyer.ToString()),("$payload",CareerJson.Write(next)));
        q.ExecuteNonQuery();
        t.Commit();
        return next;
    }
}
