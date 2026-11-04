using Furvo.Core.Development;
using Xunit;
using Furvo.Core.Domain;
using Furvo.Persistence;
using System.Security.Cryptography;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    [Fact] public void SharedAgendaSummaryCountsActivitiesNotIndividualLoad()
    {
        var plan=TrainingPlan.Empty(Guid.NewGuid());var date=new DateOnly(2026,7,1);
        var a=Block(plan.Team,date,9,60,TrainingTurn.Morning);var b=Block(plan.Team,date,16,90,TrainingTurn.Afternoon) with{Exercise="general_strength"};
        plan=plan with{Sessions=new[]{a,b,Block(plan.Team,date.AddDays(7),9,30,TrainingTurn.Morning)},Days=new[]{new TrainingDayPlan(date.AddDays(1),TrainingDayKind.Rest)}};
        var view=TrainingAgenda.ReadWeek(plan,date);Assert.Equal(2,view.ExerciseCount);Assert.Equal(2,view.PlannedDays);Assert.Equal(150,view.ActivityMinutes);Assert.Equal(1,view.RestDays);Assert.Equal(60,view.CategoryMinutes[TrainingArea.Field]);Assert.Equal(90,view.CategoryMinutes[TrainingArea.Gym]);Assert.Equal(60,view.TurnMinutes[TrainingTurn.Morning]);Assert.Equal(90,view.TurnMinutes[TrainingTurn.Afternoon]);Assert.Equal(3,plan.Sessions.Count);
    }
    [Fact] public void SharedAgendaEmptyWeekDoesNotInventDaysOrRest()
    {var view=TrainingAgenda.ReadWeek(TrainingPlan.Empty(Guid.NewGuid()),new(2026,7,1));Assert.Equal(0,view.ExerciseCount);Assert.Equal(0,view.RestDays);Assert.Equal(0,view.PlannedDays);}
    [Fact] public void SharedAgendaCategoryRoutesAreStableAndPreserveSession()
    {var plan=TrainingPlan.Empty(Guid.NewGuid());foreach(var area in Enum.GetValues<TrainingArea>()){var block=Block(plan.Team,new(2026,7,1),9,30,TrainingTurn.Morning) with{Exercise=TrainingCatalog.Exercises.First(e=>e.Area==area).Code};Assert.Equal((int)area,TrainingAgenda.TabFor(block));Assert.Equal(30,block.Minutes);}}
    [Theory]
    [InlineData(TrainingArea.Field,"technical")]
    [InlineData(TrainingArea.Gym,"general_strength")]
    [InlineData(TrainingArea.Goalkeepers,"handling")]
    public void SharedAgendaSpecialtyCreateEditAndDeleteUseOneRecord(TrainingArea area,string exercise)
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var store=new SqliteSportsStore(file);var date=store.CareerDate();
        var player=squads.Candidates(team).First(p=>area!=TrainingArea.Goalkeepers||p.Positions?.Primary==PlayerRole.Goalkeeper).Id;
        var block=Block(team,date,9,30,TrainingTurn.Morning,new[]{player}) with{Exercise=exercise,Target=new(TrainingScope.Individual,player)};
        var plan=store.SavePlan(store.Plan(team) with{Sessions=new[]{block}});
        Assert.Equal(block.Id,Assert.Single(TrainingDailyPlanning.Read(plan,date,area).Turns.SelectMany(t=>t.Blocks)).Id);Assert.Equal(1,TrainingAgenda.ReadWeek(plan,date).ExerciseCount);
        plan=store.SavePlan(plan with{Sessions=new[]{block with{Minutes=45}}});var reopened=new SqliteSportsStore(file).Plan(team);Assert.Equal(block.Id,Assert.Single(reopened.Sessions).Id);Assert.Equal(45,TrainingAgenda.ReadWeek(reopened,date).ActivityMinutes);
        var hash=SHA256.HashData(File.ReadAllBytes(file));TrainingAgenda.ReadWeek(reopened,date);TrainingDailyPlanning.Read(reopened,date,area);Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(file)));
        store.SavePlan(plan with{Sessions=Array.Empty<TrainingSession>()});Assert.Empty(TrainingDailyPlanning.Read(store.Plan(team),date,area).Turns.SelectMany(t=>t.Blocks));Assert.Equal(0,TrainingAgenda.ReadWeek(store.Plan(team),date).ExerciseCount);Assert.Empty(new SqliteTrainingExecutionStore(file).CompletedSessions(team));
    }
    [Fact] public void SharedAgendaReportsCrossCategoryConflictWithExistingIdentityAndTimes()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var store=new SqliteSportsStore(file);var player=squads.Candidates(team)[0].Id;var block=Block(team,store.CareerDate(),9,60,TrainingTurn.Morning,new[]{player}) with{Target=new(TrainingScope.Individual,player)};
        var plan=store.SavePlan(store.Plan(team) with{Sessions=new[]{block}});var added=block with{Id=Guid.NewGuid(),Exercise="general_strength",Start=new(9,30)};
        var error=Assert.Throws<InvalidDataException>(()=>store.SavePlan(plan with{Sessions=plan.Sessions.Append(added).ToArray()}));var conflict=Assert.IsType<TrainingConflict>(error.Data[nameof(TrainingConflict)]);Assert.Equal(block.Id,conflict.First.Id);Assert.Equal(added.Id,conflict.Second.Id);Assert.Contains("09:00",error.Message);Assert.Single(store.Plan(team).Sessions);
    }
    [Fact] public void SharedAgendaPermitsFieldAndKeepersWithEffectiveExclusions()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var players=squads.Candidates(team);var store=new SqliteSportsStore(file);var keepers=players.Where(p=>p.Positions?.Primary==PlayerRole.Goalkeeper).Select(p=>p.Id).ToArray();
        var target=new TrainingTarget(TrainingScope.Team,Excluded:keepers);var a=Block(team,store.CareerDate(),9,60,TrainingTurn.Morning,TrainingRules.Resolve(target,players,Array.Empty<TrainingGroup>()).ToArray()) with{Target=target};
        var keeperTarget=new TrainingTarget(TrainingScope.Position,Position:PlayerRole.Goalkeeper);var b=a with{Id=Guid.NewGuid(),Exercise="handling",Target=keeperTarget,Participants=TrainingRules.Resolve(keeperTarget,players,Array.Empty<TrainingGroup>())};
        store.SavePlan(store.Plan(team) with{Sessions=new[]{a,b}});var plan=store.Plan(team);Assert.Equal(2,TrainingAgenda.ReadWeek(plan,store.CareerDate()).ExerciseCount);Assert.Empty(a.Participants.Intersect(b.Participants));Assert.Equal(120,TrainingAgenda.ReadWeek(plan,store.CareerDate()).ActivityMinutes);Assert.Equal(60,TrainingDailyPlanning.Read(plan,store.CareerDate()).GlobalTotals.EffectiveMinutes);
    }
    [Fact] public void SharedAgendaDoesNotCrossCareersOrTeams()
    {
        var (_,file,_)=TacticalCareer();var (_,other,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var teams=squads.Teams();var team=teams[0].Id;var store=new SqliteSportsStore(file);var player=squads.Candidates(team)[0].Id;var date=store.CareerDate();store.SavePlan(store.Plan(team) with{Sessions=new[]{Block(team,date,9,30,TrainingTurn.Morning,new[]{player}) with{Target=new(TrainingScope.Individual,player)}}});
        Assert.Equal(0,TrainingAgenda.ReadWeek(new SqliteSportsStore(other).Plan(team),date).ExerciseCount);Assert.Equal(0,TrainingAgenda.ReadWeek(store.Plan(teams[1].Id),date).ExerciseCount);
    }
}
