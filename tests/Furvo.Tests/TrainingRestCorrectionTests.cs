using System.Security.Cryptography;
using System.Text.Json;
using Furvo.Core.Development;
using Furvo.Core.Domain;
using Furvo.Persistence;
using Microsoft.Data.Sqlite;
using Xunit;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(150)]
    [InlineData(210)]
    [InlineData(300)]
    public void PositiveDurationsHaveNoSportingCapsAndDoNotExecuteTraining(int minutes)
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var store=new SqliteSportsStore(file);var player=squads.Players(team)[0];var attributes=player.Attributes.AllValues.ToArray();
        var block=Block(team,store.CareerDate(),9,minutes,TrainingTurn.Morning,new[]{player.Id}) with{Target=new(TrainingScope.Individual,player.Id),Load=TrainingLoad.VeryHigh};
        store.SavePlan(store.Plan(team) with{Sessions=new[]{block}});Assert.Equal(minutes,store.Plan(team).Sessions.Single().Minutes);Assert.Equal(attributes,squads.Players(team).Single(p=>p.Id==player.Id).Attributes.AllValues.ToArray());Assert.Empty(new SqliteTrainingExecutionStore(file).CompletedSessions(team));Assert.Null(new SqliteTrainingExecutionStore(file).ObservedPhysical(player.Id));
    }
    [Fact] public void WorkBeyondFormerDailyAndTurnCapsRemainsValidWithoutInventingRisk()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var player=squads.Candidates(team)[0].Id;var store=new SqliteSportsStore(file);var date=store.CareerDate();
        var a=Block(team,date,7,300,TrainingTurn.Morning,new[]{player}) with{Target=new(TrainingScope.Individual,player),Load=TrainingLoad.High};var b=a with{Id=Guid.NewGuid(),Start=new(14,0),Turn=TrainingTurn.Afternoon,Minutes=330};
        store.SavePlan(TrainingSchedule.SetDay(store.Plan(team) with{Sessions=new[]{a,b}},new(date,TrainingDayKind.Double)));var day=TrainingDailyPlanning.Read(store.Plan(team),date);Assert.Equal(630,day.GlobalTotals.ActivityMinutes);Assert.Equal(300,day.GlobalTotals.MorningActivityMinutes);Assert.Equal(330,day.GlobalTotals.AfternoonActivityMinutes);Assert.Null(new SqliteTrainingExecutionStore(file).ObservedPhysical(player));
    }
    [Theory]
    [InlineData(0)]
    [InlineData(-15)]
    public void NonPositiveDurationIsRejectedWithoutWriting(int minutes)
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteSportsStore(file);var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var player=squads.Candidates(team)[0].Id;var block=Block(team,store.CareerDate(),9,minutes,TrainingTurn.Morning,new[]{player}) with{Target=new(TrainingScope.Individual,player)};
        Assert.Throws<InvalidDataException>(()=>store.SavePlan(store.Plan(team) with{Sessions=new[]{block}}));Assert.Empty(store.Plan(team).Sessions);
    }
    [Fact] public void CrossDayAndWrongTurnRemainInvalidEvenWithoutDurationCaps()
    {
        var plan=TrainingPlan.Empty(Guid.NewGuid());var date=new DateOnly(2026,7,1);
        Assert.Throws<InvalidDataException>(()=>TrainingSchedule.ValidateChanges(plan with{Sessions=new[]{Block(plan.Team,date,19,360,TrainingTurn.Afternoon)}},plan,new(OperatingEndHour:24)));
        Assert.Throws<InvalidDataException>(()=>TrainingSchedule.ValidateChanges(plan with{Sessions=new[]{Block(plan.Team,date,13,150,TrainingTurn.Morning)}},plan));
    }
    [Fact] public void RestIsNotAnActiveExerciseAndCannotBeCreatedAsABlock()
    {
        Assert.DoesNotContain(TrainingCatalog.Exercises,e=>e.Code=="rest");
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var store=new SqliteSportsStore(file);var legacy=Session(team,squads.Players(team)) with{Exercise="rest",Load=TrainingLoad.Rest};
        Assert.Throws<InvalidDataException>(()=>store.SavePlan(store.Plan(team) with{Sessions=new[]{legacy}}));Assert.Empty(store.Plan(team).Sessions);
    }
    [Fact] public void ConfirmedRestPersistsWithoutExerciseLoadOrPendingTurn()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteSportsStore(file);var team=new SqliteClubSquadStore(file).Teams()[0].Id;var date=store.CareerDate();
        store.SavePlan(TrainingSchedule.SetDay(store.Plan(team),new(date,TrainingDayKind.Rest)));var plan=new SqliteSportsStore(file).Plan(team);Assert.Empty(plan.Sessions);var day=TrainingDailyPlanning.Read(plan,date);Assert.Equal(TrainingDayKind.Rest,day.Configuration!.Kind);Assert.Empty(day.Turns);Assert.Equal(0,day.GlobalTotals.ActivityMinutes);Assert.Empty(new SqliteTrainingExecutionStore(file).CompletedSessions(team));
    }
    [Fact] public void ActiveRecoveryKeepsItsScheduleAndIsNotFullRest()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var store=new SqliteSportsStore(file);store.SavePlan(store.Plan(team) with{Sessions=new[]{Session(team,squads.Players(team),"active_recovery")}});
        var day=TrainingDailyPlanning.Read(store.Plan(team),store.CareerDate());Assert.Equal(1,day.GlobalCount);Assert.Equal(30,day.GlobalTotals.ActivityMinutes);Assert.NotEqual(TrainingDayKind.Rest,day.Configuration!.Kind);
    }
    [Fact] public void RestReplacementRequiresConfirmationAndPreservesOtherDates()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var store=new SqliteSportsStore(file);var date=store.CareerDate();var original=Session(team,squads.Players(team));var other=original with{Id=Guid.NewGuid(),Date=date.AddDays(1)};var plan=store.SavePlan(store.Plan(team) with{Sessions=new[]{original,other}});
        Assert.Throws<InvalidOperationException>(()=>TrainingSchedule.SetDay(plan,new(date,TrainingDayKind.Rest)));Assert.Equal(2,store.Plan(team).Sessions.Count);
        store.SavePlan(TrainingSchedule.SetDay(plan,new(date,TrainingDayKind.Rest),true));var after=store.Plan(team);Assert.Equal(other.Id,Assert.Single(after.Sessions).Id);Assert.Equal(TrainingDayKind.Rest,after.Days!.Single(d=>d.Date==date).Kind);
    }
    private (string File,Guid Team,Guid RestId) LegacyRestFixture(bool mixed,bool confirmed,bool executed=false)
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var store=new SqliteSportsStore(file);var rest=Session(team,squads.Players(team)) with{Start=new(9,7),Objective="Registro histórico"};var blocks=mixed?new[]{rest,Session(team,squads.Players(team),hour:11)}:new[]{rest};store.SavePlan(store.Plan(team) with{Sessions=blocks});
        using var c=Connection(file);using var q=c.CreateCommand();q.CommandText="UPDATE training_plans SET data_json=json_set(data_json,'$.Sessions[0].Exercise','rest','$.Sessions[0].Load',0) WHERE team_id=$team;DELETE FROM training_day_plans WHERE team_id=$team;DROP TABLE training_rest_archive;DROP TABLE training_rest_schema;";q.Parameters.AddWithValue("$team",team.ToString("D"));q.ExecuteNonQuery();
        if(confirmed){q.CommandText="INSERT INTO training_day_plans VALUES($team,$date,1,NULL)";q.Parameters.AddWithValue("$date",store.CareerDate().ToString("yyyy-MM-dd"));q.ExecuteNonQuery();}
        if(executed){q.CommandText="INSERT INTO training_attendance SELECT $id,$team,$day,json_object('Session',json_extract(data_json,'$.Sessions[0]')) FROM training_plans WHERE team_id=$team;";q.Parameters.AddWithValue("$id",rest.Id.ToString("D"));q.Parameters.AddWithValue("$day",store.CareerDate().ToString("yyyy-MM-dd"));q.ExecuteNonQuery();}
        return(file,team,rest.Id);
    }
    [Theory]
    [InlineData(false,false,LegacyRestResolution.Review)]
    [InlineData(true,false,LegacyRestResolution.Review)]
    [InlineData(false,true,LegacyRestResolution.Converted)]
    public void LegacyRestMigrationPreservesCompleteRecordWithoutInventingAWholeDay(bool mixed,bool confirmed,LegacyRestResolution expected)
    {
        var (file,team,id)=LegacyRestFixture(mixed,confirmed);var hash=SHA256.HashData(File.ReadAllBytes(file));var store=new SqliteSportsStore(file);var projected=store.Plan(team);Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(file)));Assert.DoesNotContain(projected.Sessions,s=>s.Exercise=="rest");var archive=Assert.Single(projected.LegacyRests!);Assert.Equal(id,archive.Original.Id);Assert.Equal(new TimeOnly(9,7),archive.Original.Start);Assert.Equal(30,archive.Original.Minutes);Assert.Equal("Registro histórico",archive.Original.Objective);Assert.Equal(expected,archive.Resolution);
        store.SavePlan(projected);var reopened=new SqliteSportsStore(file).Plan(team);Assert.Equal(JsonSerializer.Serialize(archive.Original),JsonSerializer.Serialize(Assert.Single(reopened.LegacyRests!).Original));Assert.Equal(mixed?1:0,reopened.Sessions.Count);
        if(!confirmed)Assert.DoesNotContain(reopened.Days??Array.Empty<TrainingDayPlan>(),d=>d.Kind==TrainingDayKind.Rest);
        using var c=Connection(file);using var q=c.CreateCommand();q.CommandText="SELECT (SELECT version FROM training_rest_schema),(SELECT count(*) FROM training_rest_archive)";using var r=q.ExecuteReader();Assert.True(r.Read());Assert.Equal(1,r.GetInt32(0));Assert.Equal(1,r.GetInt32(1));
    }
    [Fact] public void ExecutedLegacyRestKeepsEvidenceAndRemainsMarkedForReview()
    {
        var (file,team,_)=LegacyRestFixture(false,true,true);var store=new SqliteSportsStore(file);var plan=store.Plan(team);Assert.True(plan.LegacyRests![0].WasExecuted);Assert.Equal(LegacyRestResolution.Review,plan.LegacyRests[0].Resolution);store.SavePlan(plan);
        using var c=Connection(file);using var q=c.CreateCommand();q.CommandText="SELECT count(*) FROM training_attendance";Assert.Equal(1,Convert.ToInt32(q.ExecuteScalar()));Assert.True(new SqliteSportsStore(file).Plan(team).LegacyRests![0].WasExecuted);
    }
    [Fact] public void GroupEditingArchivesLegacyRestBeforeRewritingThePlan()
    {
        var (file,team,id)=LegacyRestFixture(true,false);var store=new SqliteSportsStore(file);var players=new SqliteClubSquadStore(file).Candidates(team);new SqliteAnalysisReportService(file).SaveGroup(team,new(Guid.NewGuid(),"Grupo",new[]{players[0].Id},"Revisión",null),store.Plan(team).Revision);
        var reopened=store.Plan(team);Assert.Equal(id,Assert.Single(reopened.LegacyRests!).Original.Id);Assert.Single(reopened.Sessions);Assert.Single(reopened.Groups);
    }
    [Fact] public void FailedRestArchivingRollsBackWithoutLosingLegacyData()
    {
        var (file,team,_)=LegacyRestFixture(true,false);var store=new SqliteSportsStore(file);
        using(var c=Connection(file))using(var q=c.CreateCommand()){q.CommandText="CREATE TRIGGER reject_archive BEFORE INSERT ON training_plans BEGIN SELECT RAISE(ABORT,'fixture rejection'); END;";q.ExecuteNonQuery();}
        Assert.Throws<SqliteException>(()=>store.SavePlan(store.Plan(team)));
        using var connection=Connection(file);using var read=connection.CreateCommand();read.CommandText="SELECT json_extract(data_json,'$.Sessions[0].Exercise') FROM training_plans";Assert.Equal("rest",read.ExecuteScalar());Assert.Equal(LegacyRestResolution.Review,store.Plan(team).LegacyRests![0].Resolution);
    }
    [Fact] public void RestAndArchivedDataAreIsolatedBetweenCareers()
    {
        var (file,team,_)=LegacyRestFixture(false,false);var (_,other,_)=TacticalCareer();var store=new SqliteSportsStore(file);store.SavePlan(TrainingSchedule.SetDay(store.Plan(team),new(store.CareerDate(),TrainingDayKind.Rest)));
        Assert.Equal(LegacyRestResolution.Converted,store.Plan(team).LegacyRests![0].Resolution);var foreign=new SqliteSportsStore(other).Plan(team);Assert.Empty(foreign.LegacyRests??Array.Empty<LegacyTrainingRest>());Assert.Empty(foreign.Days??Array.Empty<TrainingDayPlan>());
    }
}
