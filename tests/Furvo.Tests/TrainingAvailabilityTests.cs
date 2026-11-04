using System.Security.Cryptography;
using System.Text.Json;
using Furvo.Core.Domain;
using Furvo.Core.Development;
using Furvo.Persistence;
using Xunit;
namespace Furvo.Tests;
public sealed class TrainingAvailabilityTests
{
    private static readonly PlayerId Player=new(Guid.Parse("10000000-0000-0000-0000-000000000001"));
    private static readonly DateOnly Start=new(2026,7,2);
    private static TrainingRestriction Restriction()=>new(Guid.NewGuid(),Player,TrainingAvailabilityState.Unavailable,TrainingRestrictionReason.PersonalLeave,Start,Start.AddDays(2));
    [Fact] public void NoRestrictionsMeansAvailableWithoutAttributesOrInventedObservations()
    {var result=TrainingAvailability.Check(Player,Start,"technical",Array.Empty<TrainingRestriction>());Assert.True(result.Allowed);Assert.Equal(TrainingAvailabilityState.Available,result.State);Assert.Empty(result.Restrictions);Assert.DoesNotContain(typeof(TrainingRestriction).GetProperties(),p=>p.Name.Contains("Attribute")||p.Name.Contains("Fatigue"));}
    [Theory][InlineData(-1,false)][InlineData(0,true)][InlineData(1,true)][InlineData(2,true)][InlineData(3,false)]
    public void RestrictionDatesAreInclusiveAndFutureOrFinishedRecordsDoNotBlock(int offset,bool active)
    {var result=TrainingAvailability.Check(Player,Start.AddDays(offset),"technical",new[]{Restriction()});Assert.Equal(active,!result.Allowed);Assert.Equal(active?TrainingAvailabilityState.Unavailable:TrainingAvailabilityState.Available,result.State);}
    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)][InlineData(6)]
    public void RejectsInvalidIdentityDatesStatesAndAuthorizations(int invalid)
    {var value=Restriction();value=invalid switch{0=>value with{Id=Guid.Empty},1=>value with{Player=default},2=>value with{End=Start.AddDays(-1)},3=>value with{Start=default},4=>value with{State=TrainingAvailabilityState.Available},5=>value with{State=TrainingAvailabilityState.Partial,AllowedExercises=new[]{"unknown"}},_=>value with{AllowedExercises=new[]{"technical"}}};Assert.Throws<InvalidDataException>(value.Validate);}
    [Theory][InlineData("active_recovery",true)][InlineData("technical",false)]
    public void PartialInjuryUsesExplicitActivitiesInsteadOfBlanketRecoveryBan(string exercise,bool allowed)
    {var value=Restriction() with{State=TrainingAvailabilityState.Partial,Reason=TrainingRestrictionReason.Injury,AllowedExercises=new[]{"active_recovery"}};Assert.Equal(allowed,TrainingAvailability.Check(Player,Start,exercise,new[]{value}).Allowed);Assert.Equal(TrainingAvailabilityState.Partial,TrainingAvailability.Check(Player,Start,exercise,new[]{value}).State);}
    [Fact] public void OpenEndedRestrictionAppliesOnlyToItsPlayerUntilExplicitlyFinished()
    {var entry=Restriction() with{End=null};Assert.False(TrainingAvailability.Check(Player,Start.AddYears(1),"technical",new[]{entry}).Allowed);Assert.True(TrainingAvailability.Check(new PlayerId(Guid.NewGuid()),Start,"technical",new[]{entry}).Allowed);}
    [Fact] public void DuplicateExplicitActivitiesAndUnknownReasonsAreRejected()
    {Assert.Throws<InvalidDataException>(()=>(Restriction() with{State=TrainingAvailabilityState.Partial,AllowedExercises=new[]{"technical","technical"}}).Validate());Assert.Throws<InvalidDataException>(()=>(Restriction() with{Reason=(TrainingRestrictionReason)999}).Validate());}
    [Fact] public void PartialWithoutStructuredPermissionRequiresExplicitResolution()
    {Assert.False(TrainingAvailability.Check(Player,Start,"active_recovery",new[]{Restriction() with{State=TrainingAvailabilityState.Partial}}).Allowed);}
    [Fact] public void OverlappingRestrictionsAreCombinedAndCompleteAbsenceCannotBeOverriddenByPartialPermission()
    {var partial=Restriction() with{State=TrainingAvailabilityState.Partial,AllowedExercises=new[]{"active_recovery"}};Assert.True(TrainingAvailability.Check(Player,Start,"active_recovery",new[]{partial}).Allowed);Assert.False(TrainingAvailability.Check(Player,Start,"active_recovery",new[]{partial,Restriction()}).Allowed);Assert.False(TrainingAvailability.Check(Player,Start,"active_recovery",new[]{partial,partial with{Id=Guid.NewGuid(),AllowedExercises=new[]{"technical"}}}).Allowed);}
}
public sealed partial class CareerCreationTests
{
    private static TrainingRestriction Absence(PlayerId player,DateOnly day)=>new(Guid.NewGuid(),player,TrainingAvailabilityState.Unavailable,TrainingRestrictionReason.PersonalLeave,day,day.AddDays(1),"Registro de prueba","fixture-only");
    [Theory][InlineData(TrainingScope.Individual)][InlineData(TrainingScope.Team)][InlineData(TrainingScope.Position)][InlineData(TrainingScope.Group)]
    public void EveryParticipantScopeChecksAvailabilityAndCollectiveSelectionCannotBypassStore(TrainingScope scope)
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var players=squads.Players(team);var player=players[0];var store=new SqliteSportsStore(file);var availability=new SqliteTrainingAvailabilityStore(file);availability.Save(Absence(player.Id,store.CareerDate()));
        var group=new TrainingGroup(Guid.NewGuid(),"Grupo",new[]{player.Id,players[1].Id},"Objetivo",null);var target=new TrainingTarget(scope,scope==TrainingScope.Individual?player.Id:null,scope==TrainingScope.Position?player.Positions!.Primary:null,scope==TrainingScope.Group?group.Id:null);
        var raw=TrainingRules.Resolve(target,players,new[]{group});var selection=TrainingAvailability.Select(target,squads.Candidates(team),new[]{group},store.CareerDate(),"technical",availability.Restrictions());Assert.Contains(player.Id,selection.Excluded.Keys);Assert.DoesNotContain(player.Id,selection.Included);
        var session=Session(team,players) with{Target=target,Participants=raw};Assert.Throws<InvalidDataException>(()=>store.SavePlan(store.Plan(team) with{Groups=new[]{group},Sessions=new[]{session}}));Assert.Empty(store.Plan(team).Sessions);
        if(selection.Included.Count>0){var revised=target with{Excluded=selection.Excluded.Keys.ToArray()};store.SavePlan(store.Plan(team) with{Groups=new[]{group},Sessions=new[]{session with{Target=revised,Participants=selection.Included}}});Assert.Contains(player.Id,store.Plan(team).Groups[0].Members);Assert.DoesNotContain(player.Id,Assert.Single(store.Plan(team).Sessions).Participants);}
    }
    [Fact] public void AvailabilityLifecyclePersistsUpdatesInclusiveFinishReopenAndCareerIsolation()
    {
        var (_,file,_)=TacticalCareer();var (_,other,_)=TacticalCareer();var player=new SqliteClubSquadStore(file).Players(new SqliteClubSquadStore(file).Teams()[0].Id)[0].Id;var store=new SqliteTrainingAvailabilityStore(file);var day=new SqliteSportsStore(file).CareerDate();var saved=store.Save(Absence(player,day));Assert.Equal(1,saved.Revision);
        var updated=store.Save(saved with{State=TrainingAvailabilityState.Partial,AllowedExercises=new[]{"active_recovery"},Notes="Autorización explícita"});Assert.Equal(2,updated.Revision);Assert.True(TrainingAvailability.Check(player,day,"active_recovery",new SqliteTrainingAvailabilityStore(file).Restrictions()).Allowed);
        var finished=store.Finish(updated.Id,day,updated.Revision);var loaded=Assert.Single(new SqliteTrainingAvailabilityStore(file).Restrictions());Assert.Equal(JsonSerializer.Serialize(finished),JsonSerializer.Serialize(loaded));Assert.False(TrainingAvailability.Check(player,day,"technical",new[]{loaded}).Allowed);Assert.True(TrainingAvailability.Check(player,day.AddDays(1),"technical",new[]{loaded}).Allowed);Assert.Empty(new SqliteTrainingAvailabilityStore(other).Restrictions());
    }
    [Fact] public void LegacyCareerReadDoesNotMigrateAndInvalidSaveRollsBackSchema()
    {
        var (_,file,_)=TacticalCareer();var player=new SqliteClubSquadStore(file).Players(new SqliteClubSquadStore(file).Teams()[0].Id)[0].Id;var store=new SqliteTrainingAvailabilityStore(file);var before=SHA256.HashData(File.ReadAllBytes(file));Assert.Empty(store.Restrictions());Assert.Equal(before,SHA256.HashData(File.ReadAllBytes(file)));
        Assert.Throws<InvalidOperationException>(()=>store.Save(Absence(player,new SqliteSportsStore(file).CareerDate()) with{Revision=8}));Assert.Empty(store.Restrictions());using var c=Connection(file);using var q=c.CreateCommand();q.CommandText="SELECT count(*) FROM sqlite_master WHERE name='training_availability'";Assert.Equal(0L,(long)q.ExecuteScalar()!);
    }
    [Fact] public void StableRestrictionIdentityRejectsForeignPlayersAndStaleEdits()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var players=squads.Players(squads.Teams()[0].Id);var store=new SqliteTrainingAvailabilityStore(file);var value=store.Save(Absence(players[0].Id,new SqliteSportsStore(file).CareerDate()));
        Assert.Throws<InvalidOperationException>(()=>store.Save(value with{Player=players[1].Id}));Assert.Throws<InvalidOperationException>(()=>store.Save(value with{Revision=0}));Assert.Throws<InvalidOperationException>(()=>store.Save(Absence(new(Guid.NewGuid()),value.Start)));Assert.Single(store.Restrictions());
    }
    [Fact] public void LateRestrictionMarksConflictWithoutRewritingExercisesAndOnlyEditedActivityRequiresReview()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var players=squads.Players(team);var store=new SqliteSportsStore(file);var session=Session(team,players);store.SavePlan(store.Plan(team) with{Sessions=new[]{session}});var snapshot=JsonSerializer.Serialize(store.Plan(team));var availability=new SqliteTrainingAvailabilityStore(file);availability.Save(Absence(players[0].Id,session.Date));Assert.Equal(snapshot,JsonSerializer.Serialize(store.Plan(team)));
        var before=SHA256.HashData(File.ReadAllBytes(file));var conflict=Assert.Single(availability.Conflicts(team));Assert.Equal(players[0].Id,conflict.Player);Assert.Equal(session.Id,conflict.Session);Assert.Equal(before,SHA256.HashData(File.ReadAllBytes(file)));
        store.SavePlan(store.Plan(team));Assert.Throws<InvalidDataException>(()=>store.SavePlan(store.Plan(team) with{Sessions=new[]{session with{Objective="Reconfirmar"}}}));
        var reviewed=session with{Target=session.Target with{Excluded=new[]{players[0].Id}},Participants=session.Participants.Where(p=>p!=players[0].Id).ToArray()};store.SavePlan(store.Plan(team) with{Sessions=new[]{reviewed}});Assert.Empty(availability.Conflicts(team));
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void RestrictionNeverRewritesPastOrPerformedParticipants(bool executed)
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var players=squads.Players(team);var store=new SqliteSportsStore(file);var session=Session(team,players);store.SavePlan(store.Plan(team) with{Sessions=new[]{session}});
        using(var c=Connection(file))using(var q=c.CreateCommand()){
            if(executed){q.CommandText="INSERT INTO training_attendance SELECT $id,$team,$date,json_object('Session',json_extract(data_json,'$.Sessions[0]')) FROM training_plans WHERE team_id=$team";q.Parameters.AddWithValue("$id",session.Id.ToString("D"));q.Parameters.AddWithValue("$date",session.Date.ToString("yyyy-MM-dd"));}
            else{q.CommandText="UPDATE training_plans SET data_json=json_set(data_json,'$.Sessions[0].Date',$date) WHERE team_id=$team";q.Parameters.AddWithValue("$date",session.Date.AddDays(-1).ToString("yyyy-MM-dd"));}q.Parameters.AddWithValue("$team",team.ToString("D"));q.ExecuteNonQuery();}
        var snapshot=JsonSerializer.Serialize(store.Plan(team).Sessions);var availability=new SqliteTrainingAvailabilityStore(file);availability.Save(Absence(players[0].Id,session.Date.AddDays(-1)));Assert.Empty(availability.Conflicts(team));store.SavePlan(store.Plan(team));Assert.Equal(snapshot,JsonSerializer.Serialize(new SqliteSportsStore(file).Plan(team).Sessions));
    }
    [Fact] public void ExerciseDateOverridesCurrentCareerDateAndPartialPermissionPersistsWithoutChangingPlayerValues()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var players=squads.Players(team);var before=players.ToDictionary(p=>p.Id,p=>p.Attributes.AllValues.ToArray());var store=new SqliteSportsStore(file);var day=store.CareerDate().AddDays(3);var restriction=Absence(players[0].Id,day) with{State=TrainingAvailabilityState.Partial,Reason=TrainingRestrictionReason.Injury,AllowedExercises=new[]{"active_recovery"}};new SqliteTrainingAvailabilityStore(file).Save(restriction);
        var target=new TrainingTarget(TrainingScope.Individual,players[0].Id);var block=Session(team,players,target:target);store.SavePlan(store.Plan(team) with{Sessions=new[]{block}});Assert.Throws<InvalidDataException>(()=>store.SavePlan(store.Plan(team) with{Sessions=new[]{block with{Date=day}}}));store.SavePlan(store.Plan(team) with{Sessions=new[]{block with{Date=day,Exercise="active_recovery"}}});Assert.Equal(day,Assert.Single(new SqliteSportsStore(file).Plan(team).Sessions).Date);Assert.All(squads.Players(team),p=>Assert.Equal(before[p.Id],p.Attributes.AllValues.ToArray()));
    }
}
