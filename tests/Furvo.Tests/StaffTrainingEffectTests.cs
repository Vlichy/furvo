using Furvo.Core.Development;
using Furvo.Core.Domain;
using Furvo.Persistence;
using Xunit;

namespace Furvo.Tests;

public sealed partial class CareerCreationTests
{
    [Fact]
    public void VerifiedYouthTrainingUsesTheAssignedProfessionalAndPreservesItsEvidence()
    {
        var low = Run(6, TrainingDelegationMode.Delegated);
        var high = Run(16, TrainingDelegationMode.Delegated);
        var supervised = Run(16, TrainingDelegationMode.Mixed);
        Assert.True(low.Progress < supervised.Progress && supervised.Progress < high.Progress);
        Assert.Equal(low.Physical, high.Physical);
        Assert.Equal(.95m, low.Factor);
        Assert.Equal(1.05m, high.Factor);
        Assert.Null(supervised.Factor);
        Assert.InRange(high.Progress / low.Progress, 1m, 1.11m);
        Assert.Equal(high, Run(16, TrainingDelegationMode.Delegated));

        (decimal Progress, PhysicalObservation? Physical, decimal? Factor) Run(int attribute, TrainingDelegationMode mode)
        {
            var (_, file, _) = TacticalCareer();
            var youth = new SqliteAcademyStore(file).List().First(state => state.Team.Category == "Juvenil");
            using (var connection = Connection(file))
            using (var update = connection.CreateCommand())
            {
                update.CommandText = "UPDATE staff_professions SET profile_json=json_set(profile_json,'$.Attributes.YouthDevelopment',$value) WHERE employee_id=$id";
                update.Parameters.AddWithValue("$value", attribute);
                update.Parameters.AddWithValue("$id", youth.Coach.Id.ToString("D"));
                Assert.Equal(1, update.ExecuteNonQuery());
            }
            var squad = new SqliteClubSquadStore(file).Players(youth.Team.Id);
            var player = squad.First(p => p.AgeOn(SportsDate) < 23);
            var session = Session(youth.Team.Id, squad, target: new(TrainingScope.Individual, player.Id));
            var plan = new SqliteSportsStore(file);
            plan.SavePlan(plan.Plan(youth.Team.Id) with
            {
                Sessions = [session],
                Delegations = [new(TrainingResponsibility.Individual, youth.Coach.Id, mode)]
            });
            var evidence = new TrainingEvidence(session.Id, session.Date, [player.Id], "fixture-only");
            var physical = new Dictionary<PlayerId, PhysicalObservation> { [player.Id] = new(25, 50, .1m, session.Date) };
            new SqliteTrainingExecutionStore(file).Process(youth.Team.Id, session.Date, [evidence], physical, new FixtureClock(session.Ends));
            var reopened = new SqliteTrainingExecutionStore(file).State(player.Id);
            Assert.Empty(reopened.AttributeChanges);
            using var check = Connection(file);
            using var query = check.CreateCommand();
            query.CommandText = "SELECT json_extract(evidence_json,'$.Staff.Employee'),json_extract(evidence_json,'$.Staff.Attribute'),json_extract(evidence_json,'$.Staff.Factor') FROM training_attendance";
            using var result = query.ExecuteReader();
            Assert.True(result.Read());
            if (mode == TrainingDelegationMode.Delegated)
            {
                Assert.Equal(youth.Coach.Id, Guid.Parse(result.GetString(0)));
                Assert.Equal("YouthDevelopment", result.GetString(1));
            }
            return (reopened.Accumulated["ball_control"], reopened.Physical, result.IsDBNull(2) ? null : result.GetDecimal(2));
        }
    }
}
