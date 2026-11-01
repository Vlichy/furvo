using System.Security.Cryptography;
using Furvo.Core.Development;
using Furvo.Core.Domain;
using Furvo.Persistence;
using Xunit;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    private static TrainingSession Block(Guid team,DateOnly date,int hour,int minutes,TrainingTurn turn,PlayerId[]? players=null)=>new(Guid.NewGuid(),team,date,new(hour,0),minutes,"technical",TrainingLoad.VeryLow,new(TrainingScope.Team),players??Array.Empty<PlayerId>(),Objective:"Bloque de prueba",Turn:turn);
    [Theory]
    [InlineData(9,0,10,0,60)]
    [InlineData(11,0,12,30,90)]
    [InlineData(9,7,9,37,30)]
    [InlineData(23,0,24,0,60)]
    public void BlockDurationUsesExactStartAndEnd(int h,int m,int endH,int endM,int expected)=>Assert.Equal(expected,TrainingSchedule.Duration(new(h,m),new(endH,endM,0)));
    [Fact] public void BlockRejectsEndBeforeOrEqualToStart()
    {Assert.Throws<InvalidDataException>(()=>TrainingSchedule.Duration(new(11,0),new(10,0,0)));Assert.Throws<InvalidDataException>(()=>TrainingSchedule.Duration(new(11,0),new(11,0,0)));}
    [Fact] public void TrainingTurnCountsEffectiveWorkWithoutCountingGaps()
    {var team=Guid.NewGuid();var day=new DateOnly(2026,7,1);var a=Block(team,day,9,60,TrainingTurn.Morning);var b=Block(team,day,11,90,TrainingTurn.Morning);var plan=TrainingPlan.Empty(team) with{Sessions=new[]{a,b}};var summary=Assert.Single(TrainingSchedule.Turns(plan,day));Assert.Equal(150,summary.EffectiveMinutes);Assert.Equal(210,summary.ElapsedMinutes);}
    [Fact] public void ParallelGroupsDoNotDoubleCountEffectiveTurnTime()
    {var team=Guid.NewGuid();var day=new DateOnly(2026,7,1);var a=Block(team,day,9,60,TrainingTurn.Morning);Assert.Equal(60,TrainingSchedule.EffectiveMinutes(new[]{a,a with{Id=Guid.NewGuid()}}));}
    [Theory]
    [InlineData(14)]
    [InlineData(121)]
    public void NewBlockDurationLimitsAreEnforced(int minutes)
    {var team=Guid.NewGuid();var old=TrainingPlan.Empty(team);Assert.Throws<InvalidDataException>(()=>TrainingSchedule.ValidateChanges(old with{Sessions=new[]{Block(team,new(2026,7,1),9,minutes,TrainingTurn.Morning)}},old));}
    [Fact] public void BlockDurationPolicyIsConfigurable()
    {var old=TrainingPlan.Empty(Guid.NewGuid());var next=old with{Sessions=new[]{Block(old.Team,new(2026,7,1),9,10,TrainingTurn.Morning)}};TrainingSchedule.ValidateChanges(next,old,new(MinimumBlockMinutes:10));Assert.Throws<InvalidDataException>(()=>TrainingSchedule.ValidateChanges(next,old));}
    [Fact] public void MaximumEffectiveTurnAndDayAreIndependentLimits()
    {
        var old=TrainingPlan.Empty(Guid.NewGuid());var date=new DateOnly(2026,7,1);
        var morning=new[]{Block(old.Team,date,9,120,TrainingTurn.Morning),Block(old.Team,date,11,90,TrainingTurn.Morning)};Assert.Throws<InvalidDataException>(()=>TrainingSchedule.ValidateChanges(old with{Sessions=morning},old));
        var all=new[]{Block(old.Team,date,9,90,TrainingTurn.Morning),Block(old.Team,date,11,90,TrainingTurn.Morning),Block(old.Team,date,15,75,TrainingTurn.Afternoon),Block(old.Team,date,17,75,TrainingTurn.Afternoon)};
        Assert.Throws<InvalidDataException>(()=>TrainingSchedule.ValidateChanges(old with{Sessions=all},old));TrainingSchedule.ValidateChanges(old with{Sessions=all},old,new(MaximumDayMinutes:330));
    }
    [Fact] public void SingleAndDoubleDaysPreserveBlocksUntilRemovalIsConfirmed()
    {
        var date=new DateOnly(2026,7,1);var plan=TrainingPlan.Empty(Guid.NewGuid());var morning=Block(plan.Team,date,9,60,TrainingTurn.Morning);var afternoon=Block(plan.Team,date,16,60,TrainingTurn.Afternoon);plan=plan with{Sessions=new[]{morning,afternoon}};
        Assert.Throws<InvalidOperationException>(()=>TrainingSchedule.SetDay(plan,new(date,TrainingDayKind.Single,TrainingTurn.Morning)));
        var single=TrainingSchedule.SetDay(plan,new(date,TrainingDayKind.Single,TrainingTurn.Morning),true);Assert.Equal(morning.Id,Assert.Single(single.Sessions).Id);
        var twice=TrainingSchedule.SetDay(plan,new(date,TrainingDayKind.Double));Assert.Equal(2,twice.Sessions.Count);Assert.Equal(2,TrainingSchedule.Turns(twice,date).Count);
        Assert.Throws<InvalidOperationException>(()=>TrainingSchedule.SetDay(plan,new(date,TrainingDayKind.Rest)));
        Assert.Empty(TrainingSchedule.SetDay(plan,new(date,TrainingDayKind.Rest),true).Sessions);
    }
    [Fact] public void ShortBreakWarningDoesNotFabricateFatigueOrBlockSaving()
    {
        var plan=TrainingPlan.Empty(Guid.NewGuid());var date=new DateOnly(2026,7,1);plan=plan with{Sessions=new[]{Block(plan.Team,date,13,45,TrainingTurn.Morning),Block(plan.Team,date,14,30,TrainingTurn.Afternoon)}};
        Assert.Contains("ShortTurnBreak",TrainingSchedule.Warnings(plan,date));Assert.Empty(TrainingSchedule.Warnings(plan,date,new(MinimumTurnBreakMinutes:15)));TrainingSchedule.ValidateChanges(plan,TrainingPlan.Empty(plan.Team));
    }
    [Fact] public void LegacyCrossingBoundaryRemainsPendingInsteadOfInventingATurn()
    {var block=Block(Guid.NewGuid(),new(2026,7,1),13,90,TrainingTurn.Morning) with{Turn=null};Assert.Null(TrainingSchedule.Classify(block));Assert.Null(TrainingSchedule.Day(TrainingPlan.Empty(block.Team) with{Sessions=new[]{block}},block.Date));Assert.Contains("LegacyTurnPending",TrainingSchedule.Warnings(TrainingPlan.Empty(block.Team) with{Sessions=new[]{block}},block.Date));}
    [Fact] public void RealStaffNamesAndActivityEligibilityAreReadFromExistingEmployees()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var sports=new SqliteSportsStore(file);var team=squads.Teams()[0].Id;var staff=sports.Staff(team);Assert.DoesNotContain(staff,p=>p.Name=="TrainingHeadCoach");Assert.All(staff,p=>Assert.False(string.IsNullOrWhiteSpace(p.Name)));
        var block=Block(team,sports.CareerDate(),9,30,TrainingTurn.Morning);Assert.NotEmpty(TrainingSchedule.Eligible(staff,block));Assert.Empty(TrainingSchedule.Eligible(staff,block with{Exercise="reflexes"}));Assert.Empty(TrainingSchedule.Eligible(staff,block with{Team=Guid.NewGuid()}));
    }
    [Fact] public void SimultaneousSeparateGroupsAreAllowedButSharedPlayersOrStaffAreRejected()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var players=squads.Players(team);var sports=new SqliteSportsStore(file);var a=Block(team,sports.CareerDate(),9,30,TrainingTurn.Morning,new[]{players[0].Id}) with{Target=new(TrainingScope.Individual,players[0].Id)};var b=a with{Id=Guid.NewGuid(),Target=new(TrainingScope.Individual,players[1].Id),Participants=new[]{players[1].Id}};
        var plan=sports.SavePlan(sports.Plan(team) with{Sessions=new[]{a,b}});Assert.Equal(2,plan.Sessions.Count);
        Assert.Throws<InvalidDataException>(()=>sports.SavePlan(plan with{Sessions=new[]{a,b with{Target=a.Target,Participants=a.Participants}}}));
        var owner=sports.Staff(team)[0].Id;Assert.Throws<InvalidDataException>(()=>sports.SavePlan(plan with{Sessions=new[]{a with{Responsible=owner},b with{Responsible=owner}}}));
    }
    [Fact] public void DelegationSupportsManualMixedAndDelegatedWithoutChangingPlans()
    {
        var team=Guid.NewGuid();var coach=new TrainingStaff(Guid.NewGuid(),"Empleado de prueba",team,Enum.GetValues<TrainingResponsibility>());var plan=TrainingPlan.Empty(team);Assert.Equal(TrainingManagement.Manual,plan.Management);
        var mixed=plan with{Delegations=new[]{new TrainingDelegation(TrainingResponsibility.Collective,coach.Id)}};Assert.Equal(TrainingManagement.Mixed,mixed.Management);
        var all=plan with{Delegations=Enum.GetValues<TrainingResponsibility>().Select(a=>new TrainingDelegation(a,coach.Id)).ToArray()};Assert.Equal(TrainingManagement.Delegated,all.Management);TrainingRules.Validate(all,Array.Empty<Player>(),new[]{coach});Assert.Throws<InvalidDataException>(()=>TrainingRules.Validate(all,Array.Empty<Player>(),Array.Empty<TrainingStaff>()));
    }
    [Fact] public void DayPlansPersistIndependentlyByCareerAndNeverCreateBlocksForRest()
    {
        var (_,a,_)=TacticalCareer();var (_,b,_)=TacticalCareer();var team=new SqliteClubSquadStore(a).Teams()[0].Id;var service=new SqliteSportsStore(a);var date=service.CareerDate();service.SavePlan(TrainingSchedule.SetDay(service.Plan(team),new(date,TrainingDayKind.Rest)));
        var reopened=new SqliteSportsStore(a).Plan(team);Assert.Equal(TrainingDayKind.Rest,Assert.Single(reopened.Days!).Kind);Assert.Empty(reopened.Sessions);Assert.Empty(new SqliteSportsStore(b).Plan(team).Days??Array.Empty<TrainingDayPlan>());Assert.Empty(new SqliteTrainingExecutionStore(a).CompletedSessions(team));
    }
    [Fact] public void LegacyDailyDurationIsNotRewrittenWhenOnlyObjectiveChanges()
    {
        var old=TrainingPlan.Empty(Guid.NewGuid());var date=new DateOnly(2026,7,1);
        old=old with{Sessions=new[]{Block(old.Team,date,9,180,TrainingTurn.Morning) with{Turn=null},Block(old.Team,date,15,180,TrainingTurn.Afternoon) with{Turn=null}}};
        var changed=old with{Sessions=old.Sessions.Select(s=>s with{Objective="Objetivo actualizado"}).ToArray()};
        TrainingSchedule.ValidateChanges(changed,old);Assert.Equal(360,TrainingSchedule.EffectiveMinutes(changed.Sessions));
    }
    [Fact] public void VersionedMigrationKeepsLegacyTimesDurationAndDelegationExactly()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams().First(t=>t.Category=="Senior").Id;var sports=new SqliteSportsStore(file);var coach=new SqliteAcademyStore(file).Get(team).Coach;var block=Session(team,squads.Players(team));sports.SavePlan(sports.Plan(team) with{Sessions=new[]{block},Delegations=new[]{new TrainingDelegation(TrainingResponsibility.Collective,coach.Id)}});
        using(var c=Connection(file))using(var q=c.CreateCommand()){q.CommandText="UPDATE training_plans SET data_json=json_remove(json_set(data_json,'$.Sessions[0].Start','09:07:00','$.Sessions[0].Minutes',5),'$.Sessions[0].Turn','$.Days'); DROP TABLE training_day_plans; DROP TABLE training_day_schema;";q.ExecuteNonQuery();}
        var before=SHA256.HashData(File.ReadAllBytes(file));var legacy=sports.Plan(team);Assert.Equal(before,SHA256.HashData(File.ReadAllBytes(file)));var original=Assert.Single(legacy.Sessions);Assert.Equal(new TimeOnly(9,7),original.Start);Assert.Equal(5,original.Minutes);Assert.Null(original.Turn);
        sports.SavePlan(legacy);var loaded=new SqliteSportsStore(file).Plan(team);Assert.Equal(original.Start,loaded.Sessions[0].Start);Assert.Equal(5,loaded.Sessions[0].Minutes);Assert.Equal(coach.Id,Assert.Single(loaded.Delegations).Responsible);
        using var connection=Connection(file);using var check=connection.CreateCommand();check.CommandText="SELECT version FROM training_day_schema";Assert.Equal(1,Convert.ToInt32(check.ExecuteScalar()));
    }
}
