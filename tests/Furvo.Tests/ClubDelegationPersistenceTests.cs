using Furvo.Core.Development;
using Furvo.Persistence;
using Xunit;

namespace Furvo.Tests;

public sealed partial class CareerCreationTests
{
    [Fact]
    public void CentralTrainingDelegationUpdatesTheExistingPlan()
    {
        var (_, file, _) = TacticalCareer();
        var team = new SqliteAcademyStore(file).List().First();
        var store = new SqliteSportsStore(file);
        var original = store.Plan(team.Team.Id);
        var area = TrainingResponsibility.Weekly;
        Assert.Throws<InvalidDataException>(() => store.SaveDelegation(team.Team.Id,
            original.Revision, area, new(area, Guid.NewGuid())));
        foreach (var mode in Enum.GetValues<TrainingDelegationMode>())
        {
            var current = store.Plan(team.Team.Id);
            store.SaveDelegation(team.Team.Id, current.Revision, area, new(area, team.Coach.Id, mode));
            var reopened = new SqliteSportsStore(file).Plan(team.Team.Id);
            Assert.Equal(new TrainingDelegation(area, team.Coach.Id, mode), Assert.Single(reopened.Delegations));
            Assert.Equal(original.Sessions, reopened.Sessions);
        }
        var saved = store.Plan(team.Team.Id);
        store.SaveDelegation(team.Team.Id, saved.Revision, area, null);
        Assert.Empty(store.Plan(team.Team.Id).Delegations);
    }
}
