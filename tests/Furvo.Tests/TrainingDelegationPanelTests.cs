using Furvo.Core.Development;
using Furvo.Persistence;
using Xunit;
using System.Text.Json;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    [Theory]
    [InlineData(TrainingDelegationMode.Delegated)]
    [InlineData(TrainingDelegationMode.Mixed)]
    public void DelegationPanelModesPersistInTheExistingPlan(TrainingDelegationMode mode)
    {
        var (_,file,_)=TacticalCareer();var team=new SqliteClubSquadStore(file).Teams().First(t=>t.Category=="Senior").Id;var store=new SqliteSportsStore(file);var coach=new SqliteAcademyStore(file).Get(team).Coach;
        store.SavePlan(store.Plan(team) with{Delegations=new[]{new TrainingDelegation(TrainingResponsibility.Collective,coach.Id,mode)}});var saved=Assert.Single(new SqliteSportsStore(file).Plan(team).Delegations);Assert.Equal(mode,saved.Mode);Assert.Equal(coach.Id,saved.Responsible);
    }
    [Fact] public void LegacyDelegationsWithoutModeRetainDelegatedMeaning()
    {
        var (_,file,_)=TacticalCareer();var team=new SqliteClubSquadStore(file).Teams().First(t=>t.Category=="Senior").Id;var store=new SqliteSportsStore(file);var coach=new SqliteAcademyStore(file).Get(team).Coach;store.SavePlan(store.Plan(team) with{Delegations=new[]{new TrainingDelegation(TrainingResponsibility.Collective,coach.Id)}});
        using(var c=Connection(file))using(var q=c.CreateCommand()){q.CommandText="UPDATE training_plans SET data_json=json_remove(data_json,'$.Delegations[0].Mode')";q.ExecuteNonQuery();}
        var saved=Assert.Single(store.Plan(team).Delegations);Assert.Equal(TrainingDelegationMode.Delegated,saved.Mode);Assert.Equal(coach.Id,saved.Responsible);
    }
    [Fact] public void InvalidDelegationModeIsRejectedWithoutChangingAssignments()
    {
        var (_,file,_)=TacticalCareer();var team=new SqliteClubSquadStore(file).Teams().First(t=>t.Category=="Senior").Id;var store=new SqliteSportsStore(file);var coach=new SqliteAcademyStore(file).Get(team).Coach;
        Assert.Throws<InvalidDataException>(()=>store.SavePlan(store.Plan(team) with{Delegations=new[]{new TrainingDelegation(TrainingResponsibility.Collective,coach.Id,(TrainingDelegationMode)99)}}));Assert.Empty(store.Plan(team).Delegations);
    }
    [Fact] public void MixedDelegationKeepsSessionsAndOtherCareersIntact()
    {
        var (_,file,_)=TacticalCareer();var (_,other,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams().First(t=>t.Category=="Senior").Id;var store=new SqliteSportsStore(file);var coach=new SqliteAcademyStore(file).Get(team).Coach;var block=Session(team,squads.Players(team));var plan=store.SavePlan(store.Plan(team) with{Sessions=new[]{block}});
        store.SavePlan(plan with{Delegations=new[]{new TrainingDelegation(TrainingResponsibility.Collective,coach.Id,TrainingDelegationMode.Mixed)}});Assert.Equal(JsonSerializer.Serialize(block),JsonSerializer.Serialize(Assert.Single(store.Plan(team).Sessions)));Assert.Empty(new SqliteSportsStore(other).Plan(team).Delegations);Assert.Empty(new SqliteTrainingExecutionStore(file).CompletedSessions(team));
    }
    [Fact] public void MixedAreaCannotBeReportedAsFullyDelegated()
    {
        var person=Guid.NewGuid();var plan=TrainingPlan.Empty(Guid.NewGuid()) with{Delegations=Enum.GetValues<TrainingResponsibility>().Select(a=>new TrainingDelegation(a,person)).ToArray()};Assert.Equal(TrainingManagement.Delegated,plan.Management);Assert.Equal(TrainingManagement.Mixed,(plan with{Delegations=plan.Delegations.Select(d=>d.Area==TrainingResponsibility.Weekly?d with{Mode=TrainingDelegationMode.Mixed}:d).ToArray()}).Management);
    }
}
