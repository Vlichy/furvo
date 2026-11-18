using Furvo.Core.Academy;
using Furvo.Core.Domain;
using Furvo.Core.Generation;
using Furvo.Core.Queries;

namespace Furvo.Core.Careers;

public enum StaffLanguageLevel { Basic, Working, Fluent, Native }
public sealed record StaffLanguage(string Code, StaffLanguageLevel Level);
public sealed record StaffCareerStage(ClubId Club, string Role, DateOnly Start, DateOnly End,
    IReadOnlyList<string> Achievements, IReadOnlyList<string> Training);
public enum StaffClauseKind { RelegationReduction, PromotionIncrease, PromotionRestoration, ObjectiveBonus, Severance, Release }
public sealed record StaffClause(StaffClauseKind Kind, decimal? Percentage, decimal? Amount, string? ObjectiveCode);
public sealed record StaffEmployment(Guid Id, Guid EmployeeId, ClubId Club, DateOnly Start, DateOnly End,
    decimal AnnualGross, string Currency, IReadOnlyList<StaffClause> Clauses)
{
    public void Validate(Guid employee, ClubId club)
    {
        if (Id == Guid.Empty || employee == Guid.Empty || EmployeeId != employee || club.Value == Guid.Empty ||
            Club != club || End <= Start || AnnualGross <= 0 || decimal.Round(AnnualGross, 2) != AnnualGross ||
            !ContractCurrencies.IsSupported(Currency) || Clauses.Count != Clauses.Select(c => c.Kind).Distinct().Count() ||
            Clauses.Any(c => !ValidClause(c)))
            throw new InvalidDataException("Contrato profesional incoherente.");
    }

    private static bool ValidClause(StaffClause clause) => clause.Kind switch
    {
        StaffClauseKind.RelegationReduction or StaffClauseKind.PromotionIncrease =>
            clause.Percentage is > 0 and <= 100 && clause.Amount is null && clause.ObjectiveCode is null,
        StaffClauseKind.PromotionRestoration => clause.Percentage is (null or 25 or 50 or 75 or 100) && clause.Amount is null && clause.ObjectiveCode is null,
        StaffClauseKind.ObjectiveBonus => clause.Percentage is null && clause.Amount is > 0 &&
            !string.IsNullOrWhiteSpace(clause.ObjectiveCode),
        StaffClauseKind.Severance or StaffClauseKind.Release =>
            clause.Percentage is null && clause.Amount is > 0 && clause.ObjectiveCode is null,
        _ => false
    };

    public ManagerEmploymentTerms DisplayTerms() => new(Start, End, AnnualGross, Currency, null);
}

public sealed record StaffProfession(string Version, DateOnly BirthDate, string Nationality, string Specialty,
    int Reputation, int ExperienceYears, IReadOnlyDictionary<string, int> Attributes, IReadOnlyList<StaffLanguage> Languages,
    IReadOnlyList<StaffCareerStage>? History = null)
{
    public int AgeOn(DateOnly date)
    {
        var years = date.Year - BirthDate.Year;
        return years - (BirthDate.AddYears(years) > date ? 1 : 0);
    }

    public void Validate(DateOnly date)
    {
        var required = StaffProfessionalCatalog.For(Specialty);
        var age = AgeOn(date);
        if (Version != InitialStaffProfession.Version || age is < 18 or > 100 || Reputation is < 1 or > 5 ||
            ExperienceYears < 0 || ExperienceYears > age - 18 ||
            !CoachNationalities.Codes.Contains(Nationality) ||
            Attributes.Count != required.Count || required.Any(key => !Attributes.TryGetValue(key, out var value) || value is < 1 or > 20) ||
            Languages.Count == 0 || Languages.Any(l => string.IsNullOrWhiteSpace(l.Code) || !Enum.IsDefined(l.Level)) ||
            Languages.Select(l => l.Code).Distinct(StringComparer.Ordinal).Count() != Languages.Count)
            throw new InvalidDataException("Perfil profesional incoherente.");
        var stages = History ?? [];
        if (stages.Any(stage => stage.Club.Value == Guid.Empty || string.IsNullOrWhiteSpace(stage.Role) ||
                stage.Start < BirthDate.AddYears(18) || stage.End < stage.Start || stage.End > date) ||
            stages.Zip(stages.Skip(1), (earlier, later) => earlier.Start > later.Start).Any(outOfOrder => outOfOrder))
            throw new InvalidDataException("Trayectoria profesional incoherente.");
    }

    public IReadOnlyList<KeyValuePair<string, int>> Strengths() => Highlights(strength: true);
    public IReadOnlyList<KeyValuePair<string, int>> Weaknesses() => Highlights(strength: false);

    private IReadOnlyList<KeyValuePair<string, int>> Highlights(bool strength) =>
        StaffProfessionalCatalog.For(Specialty).Where(Attributes.ContainsKey)
            .Select(key => (Key: key, Value: Attributes[key], Expected: StaffProfessionalCatalog.Expectation(Specialty, key)))
            .Where(row => strength ? row.Value >= 13 && row.Value >= row.Expected + 2 : row.Value <= 8 && row.Value <= row.Expected - 2)
            .OrderByDescending(row => strength ? row.Value - row.Expected : row.Expected - row.Value)
            .ThenBy(row => row.Key, StringComparer.Ordinal).Take(3)
            .Select(row => KeyValuePair.Create(row.Key, row.Value)).ToArray();
}

public static class StaffProfessionalCatalog
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Attributes =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["AcademyCoach"] = Array.AsReadOnly(new[] { "Tactics", "Technical", "Attacking", "Defending", "YouthDevelopment",
                "Fitness", "Motivation", "Discipline", "GroupManagement", "TacticalAdaptability" }),
            ["GoalkeeperCoach"] = Array.AsReadOnly(new[] { "KeeperTechnique", "Positioning", "Aerial", "Distribution", "KeeperDevelopment", "SpecificPreparation" }),
            ["FitnessCoach"] = Array.AsReadOnly(new[] { "Fitness", "InjuryPrevention", "Recovery", "LoadManagement", "PhysicalDevelopment" }),
            ["Scout"] = Array.AsReadOnly(new[] { "CurrentAbility", "Potential", "TalentIdentification", "Observation", "PerformanceAnalysis", "MarketKnowledge" }),
            ["SportingDirector"] = Array.AsReadOnly(new[] { "Negotiation", "SquadPlanning", "PlayerEvaluation", "ContractManagement", "MarketKnowledge", "SportingOrganization" }),
            ["AcademyDirector"] = Array.AsReadOnly(new[] { "YouthDevelopment", "CoachCoordination", "DevelopmentPlanning", "TalentEvaluation", "AgeGroupOrganization", "Leadership" }),
            ["Medical"] = Array.AsReadOnly(new[] { "Diagnosis", "InjuryPrevention", "Treatment", "Rehabilitation", "InjuryFollowup" })
        };

    public static IReadOnlyList<string> For(string specialty) => Attributes.TryGetValue(specialty, out var keys)
        ? keys : throw new InvalidDataException("Especialidad profesional desconocida.");

    public static int Expectation(string specialty, string key) => (specialty, key) switch
    {
        ("AcademyCoach", "YouthDevelopment" or "Technical" or "Motivation") => 12,
        ("AcademyCoach", "Fitness" or "TacticalAdaptability") => 8,
        _ => 10
    };
}

public static class InitialStaffProfession
{
    public const string Version = "staff-profession-v1";

    public static StaffProfession Generate(AcademyStaff coach, AcademyTeam team, DateOnly date)
    {
        if (coach.Id == Guid.Empty || coach.TeamId != team.Id || coach.Role != "HeadCoach")
            throw new InvalidDataException("Identidad de técnico incompatible.");
        var rng = new DeterministicRandom(Version + "/" + coach.Id.ToString("D"));
        var age = rng.Next(27, 59);
        var birth = date.AddYears(-age).AddDays(-rng.Next(0, 365));
        var experience = Math.Clamp(age - rng.Next(21, 32), 0, 30);
        var senior = team.Category == "Senior";
        var reputation = Math.Clamp(1 + experience / 12 + (senior && team.Level == 1 ? 1 : 0), 1, 4);
        var baseline = 8 + Math.Min(3, experience / 7) + (senior ? 1 : 0);
        var attributes = StaffProfessionalCatalog.For("AcademyCoach").ToDictionary(key => key, key =>
            Math.Clamp(baseline + rng.Next(-5, 5) + (key == "YouthDevelopment" ? 2 : 0), 3, 18));
        var languages = new List<StaffLanguage> { new("es", StaffLanguageLevel.Native) };
        if (rng.Next(0, 4) == 0) languages.Add(new("en", (StaffLanguageLevel)rng.Next(0, 3)));
        var result = new StaffProfession(Version, birth, "ES", "AcademyCoach", reputation, experience, attributes, languages);
        result.Validate(date);
        return result;
    }

    public static StaffEmployment? Employment(Guid employeeId, AcademyTeam team, StaffProfession profession,
        DateOnly start, DateOnly end, ClubEconomicPlan? plan, int coaches)
    {
        if (plan is null || end <= start || coaches <= 0) return null;
        if (plan.ClubId != team.ClubId.Value) throw new InvalidDataException("Presupuesto de otro club.");
        var policy = ContractCountryPolicies.For("España");
        var employerFactor = plan.PlanningManagerEmployerCost / plan.ProposedManagerGrossAnnualSalary;
        var perCoachCap = decimal.Floor((plan.ForecastExpense.SportingPersonnel - plan.PlanningManagerEmployerCost) / employerFactor / coaches);
        if (perCoachCap < policy.MinimumGrossAnnualSalary) return null;
        var share = (team.Category == "Senior" ? .13m : .075m) + .015m * (profession.Reputation - 1) +
            .001m * profession.ExperienceYears + .002m * (profession.Attributes["YouthDevelopment"] - 10);
        var gross = Math.Clamp(decimal.Floor(plan.ProposedManagerGrossAnnualSalary * share), policy.MinimumGrossAnnualSalary, perCoachCap);
        var result = new StaffEmployment(DeterministicRandom.Identity(Version + "/employment/" + employeeId), employeeId,
            team.ClubId, start, end, gross, policy.Currency, []);
        result.Validate(employeeId, team.ClubId);
        return result;
    }
}
