using System.Security.Cryptography;
using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Furvo.Persistence;
using Xunit;

namespace Furvo.Tests;

public sealed partial class CareerCreationTests
{
    [Fact]
    public void DirectoryReadsExistingIdentitiesWithoutChangingTheCareer()
    {
        var (store, file, _) = TacticalCareer();
        var career = store.Load(new SaveId(Guid.Parse(Path.GetFileNameWithoutExtension(file))));
        var hash = SHA256.HashData(File.ReadAllBytes(file));
        var people = SqliteStaffDirectory.Read(file, career);
        var manager = Assert.Single(people, p => p.Id == career.Coach.Id.Value);
        Assert.Equal(StaffContractStatus.Contracted, manager.ContractStatus(career.CurrentDate));
        foreach (var team in new SqliteAcademyStore(file).List())
        {
            var coach = Assert.Single(people, p => p.Id == team.Coach.Id);
            Assert.Equal(StaffContractStatus.Contracted, coach.ContractStatus(career.CurrentDate));
            Assert.Equal(team.Team.Name, coach.Team);
            var profile = Assert.IsType<StaffProfession>(coach.Profession);
            Assert.InRange(profile.AgeOn(career.CurrentDate), 27, 58);
            Assert.InRange(profile.Reputation, 1, 5);
            Assert.Equal(10, profile.Attributes.Count);
            Assert.All(profile.Attributes.Values, value => Assert.InRange(value, 1, 20));
            Assert.Contains(profile.Languages, language => language.Code == "es");
            Assert.All(profile.Strengths(), pair => Assert.True(pair.Value >= 13));
            Assert.All(profile.Weaknesses(), pair => Assert.True(pair.Value <= 8));
            Assert.Equal(profile.Attributes, InitialStaffProfession.Generate(team.Coach, team.Team, career.CurrentDate).Attributes);
            var employment = Assert.IsType<StaffEmployment>(coach.Employment);
            employment.Validate(coach.Id, team.Team.ClubId);
            Assert.True(employment.AnnualGross >= ContractCountryPolicies.For("España").MinimumGrossAnnualSalary);
        }
        Assert.Equal(68, people.Count(p => p.Employment is not null));
        Assert.Equal(hash, SHA256.HashData(File.ReadAllBytes(file)));
    }

    [Fact]
    public void LegacyDirectoryKeepsUnknownTermsAndDoesNotBackfill()
    {
        var (store, file, _) = TacticalCareer();
        var career = store.Load(new SaveId(Guid.Parse(Path.GetFileNameWithoutExtension(file))));
        using (var connection = Connection(file))
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "DROP TABLE staff_employments; DROP TABLE staff_professions;";
            command.ExecuteNonQuery();
        }
        var hash = SHA256.HashData(File.ReadAllBytes(file));
        var staff = SqliteStaffDirectory.Read(file, career).Where(p => p.Role == "AcademyCoach").ToArray();
        Assert.Equal(68, staff.Length);
        Assert.All(staff, p => { Assert.Null(p.Profession); Assert.Equal(StaffContractStatus.Unknown, p.ContractStatus(career.CurrentDate)); });
        Assert.Equal(hash, SHA256.HashData(File.ReadAllBytes(file)));
    }

    [Fact]
    public void StructuredEmploymentTermsRejectInvalidClausesAndDates()
    {
        var date = new DateOnly(2026, 7, 1);
        var employee = Guid.NewGuid();
        var club = new ClubId(Guid.NewGuid());
        var valid = new StaffEmployment(Guid.NewGuid(), employee, club, date, date.AddYears(1).AddDays(-1),
            120000m, "EUR", [new(StaffClauseKind.RelegationReduction, 25, null, null),
                new(StaffClauseKind.ObjectiveBonus, null, 3000, "promotion")]);
        valid.Validate(employee, club);
        Assert.Equal(12m, valid.DisplayTerms().CalculateDurationMonths());
        Assert.Throws<InvalidDataException>(() => (valid with { End = date }).Validate(employee, club));
        Assert.Throws<InvalidDataException>(() => (valid with
        {
            Clauses = [new(StaffClauseKind.RelegationReduction, 120, null, null)]
        }).Validate(employee, club));
    }

    [Fact]
    public void ExpiryFiltersContractedEmployeesWithoutTreatingUnknownAsFree()
    {
        var date = new DateOnly(2026, 7, 1);
        var employee = new StaffProfile(Guid.NewGuid(), "Ángela Pérez", null, "Manager", null,
            Contract: new(date.AddMonths(-1), date.AddMonths(4), 120000, "EUR", null));
        var former = employee with { Id = Guid.NewGuid(), Contract = employee.Contract! with { EndDate = date.AddDays(-1) } };
        var unknown = employee with { Id = Guid.NewGuid(), Contract = null };
        StaffProfile[] people = [employee, former, unknown];
        Assert.Empty(StaffSearch.Find(people, date, "angela", StaffContractStatus.Contracted, 3));
        Assert.Equal(employee, Assert.Single(StaffSearch.Find(people, date, "angela perez", StaffContractStatus.Contracted, 6)));
        Assert.Equal(former, Assert.Single(StaffSearch.Find(people, date, "", StaffContractStatus.Free)));
        Assert.Empty(StaffSearch.Find(people, date, "", StaffContractStatus.Free, 12));
    }
}
