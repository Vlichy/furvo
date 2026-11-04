using System.Security.Cryptography;
using Furvo.Core.Development;
using Furvo.Core.Domain;
using Furvo.Persistence;
using Xunit;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    public void DailyReadLoadsOnlySelectedDateInStableChronologicalOrder(int offset)
    {
        var plan=TrainingPlan.Empty(Guid.NewGuid());var date=new DateOnly(2026,7,1).AddDays(offset);
        var a=Block(plan.Team,date,11,30,TrainingTurn.Morning);var b=Block(plan.Team,date,9,30,TrainingTurn.Morning);var foreign=Block(plan.Team,date.AddDays(1),9,30,TrainingTurn.Morning);
        plan=plan with{Sessions=new[]{a,foreign,b}};var view=TrainingDailyPlanning.Read(plan,date);Assert.Equal(2,view.GlobalCount);Assert.Equal(new[]{b.Id,a.Id},view.Turns.SelectMany(t=>t.Blocks).Select(b=>b.Id));Assert.DoesNotContain(view.Turns.SelectMany(t=>t.Blocks),b=>b.Id==foreign.Id);Assert.Same(b,view.Turns[0].Blocks[0]);
    }
    [Theory]
    [InlineData(TrainingArea.Field)]
    [InlineData(TrainingArea.Gym)]
    [InlineData(TrainingArea.Goalkeepers)]
    [InlineData(TrainingArea.SetPieces)]
    [InlineData(TrainingArea.Recovery)]
    [InlineData(TrainingArea.Technology)]
    public void DailyCategoryFilterPreservesGlobalTotalsAndOriginalIdentities(TrainingArea area)
    {
        var plan=TrainingPlan.Empty(Guid.NewGuid());var date=new DateOnly(2026,7,1);
        plan=plan with{Sessions=Enum.GetValues<TrainingArea>().Select((a,i)=>Block(plan.Team,date,9,15,TrainingTurn.Morning) with{Exercise=TrainingCatalog.Exercises.First(e=>e.Area==a).Code}).ToArray()};
        var all=TrainingDailyPlanning.Read(plan,date);var filtered=TrainingDailyPlanning.Read(plan,date,area);Assert.Equal(6,filtered.GlobalCount);Assert.Equal(1,filtered.VisibleCount);Assert.Equal(all.GlobalTotals.ActivityMinutes,filtered.GlobalTotals.ActivityMinutes);Assert.Equal(area,TrainingCatalog.Get(Assert.Single(filtered.Turns.SelectMany(t=>t.Blocks)).Exercise).Area);
    }
    [Fact] public void DailyEmptySingleDoubleAndRestDoNotInventActivities()
    {
        var plan=TrainingPlan.Empty(Guid.NewGuid());var date=new DateOnly(2026,7,1);
        var empty=TrainingDailyPlanning.Read(plan,date);Assert.Null(empty.Configuration);Assert.Equal(0,empty.GlobalCount);Assert.Equal(0,empty.GlobalTotals.ActivityMinutes);
        var single=TrainingDailyPlanning.Read(TrainingSchedule.SetDay(plan,new(date,TrainingDayKind.Single,TrainingTurn.Afternoon)),date);Assert.Equal(TrainingTurn.Afternoon,Assert.Single(single.Turns).Turn);Assert.Empty(single.Turns[0].Blocks);
        var both=TrainingDailyPlanning.Read(TrainingSchedule.SetDay(plan,new(date,TrainingDayKind.Double)),date);Assert.Equal(2,both.Turns.Count);Assert.All(both.Turns,t=>Assert.Empty(t.Blocks));
        var rest=TrainingDailyPlanning.Read(TrainingSchedule.SetDay(plan,new(date,TrainingDayKind.Rest)),date);Assert.Equal(0,rest.VisibleCount);Assert.Empty(rest.Turns);
    }
    [Fact] public void DailyTotalsExcludeGapsAndRestAndSeparateBothTurns()
    {
        var plan=TrainingPlan.Empty(Guid.NewGuid());var date=new DateOnly(2026,7,1);var player=new PlayerId(Guid.NewGuid());
        plan=plan with{Sessions=new[]{Block(plan.Team,date,9,60,TrainingTurn.Morning,new[]{player}),Block(plan.Team,date,11,90,TrainingTurn.Morning,new[]{player}),Block(plan.Team,date,16,60,TrainingTurn.Afternoon,new[]{player}),Block(plan.Team,date,18,30,TrainingTurn.Afternoon,new[]{player}) with{Exercise="rest",Load=TrainingLoad.Rest}}};
        var totals=TrainingDailyPlanning.Read(plan,date).GlobalTotals;Assert.Equal(150,totals.MorningActivityMinutes);Assert.Equal(60,totals.AfternoonActivityMinutes);Assert.Equal(210,totals.ActivityMinutes);Assert.Equal(210,totals.EffectiveMinutes);Assert.Equal(210,totals.IndividualMinutes[player]);
    }
    [Fact] public void DailySimultaneousActivitiesSeparateActivityMinutesFromIndividualMinutes()
    {
        var plan=TrainingPlan.Empty(Guid.NewGuid());var date=new DateOnly(2026,7,1);var a=new PlayerId(Guid.NewGuid());var b=new PlayerId(Guid.NewGuid());
        plan=plan with{Sessions=new[]{Block(plan.Team,date,9,60,TrainingTurn.Morning,new[]{a}),Block(plan.Team,date,9,60,TrainingTurn.Morning,new[]{b})}};
        var totals=TrainingDailyPlanning.Read(plan,date).GlobalTotals;Assert.Equal(120,totals.ActivityMinutes);Assert.Equal(60,totals.EffectiveMinutes);Assert.Equal(60,totals.IndividualMinutes[a]);Assert.Equal(60,totals.IndividualMinutes[b]);
    }
    [Fact] public void DailySuggestionRoundsProposalWithoutChangingStoredLegacyTimes()
    {
        var plan=TrainingPlan.Empty(Guid.NewGuid());var date=new DateOnly(2026,7,1);var original=Block(plan.Team,date,9,30,TrainingTurn.Morning) with{Start=new(9,7),Turn=null};plan=plan with{Sessions=new[]{original}};
        var next=TrainingDailyPlanning.Suggest(plan,date,TrainingTurn.Morning,45);Assert.NotNull(next);Assert.Equal(new TimeOnly(9,45),next.Start);Assert.Equal(new TimeSpan(10,30,0),next.End);Assert.Equal(new TimeOnly(9,7),original.Start);Assert.Single(plan.Sessions);
    }
    [Fact] public void DailySuggestionRespectsOperatingWindowWithoutSportingCaps()
    {
        var plan=TrainingPlan.Empty(Guid.NewGuid());var date=new DateOnly(2026,7,1);var empty=TrainingDailyPlanning.Suggest(plan,date,TrainingTurn.Morning,45);Assert.Equal(new TimeOnly(7,0),empty!.Start);
        plan=plan with{Sessions=new[]{Block(plan.Team,date,14,120,TrainingTurn.Afternoon),Block(plan.Team,date,16,120,TrainingTurn.Afternoon),Block(plan.Team,date,18,120,TrainingTurn.Afternoon)}};
        Assert.Null(TrainingDailyPlanning.Suggest(plan,date,TrainingTurn.Afternoon,45));
        var limited=TrainingPlan.Empty(plan.Team) with{Sessions=new[]{Block(plan.Team,date,9,120,TrainingTurn.Morning)}};
        Assert.Equal(new TimeOnly(11,0),TrainingDailyPlanning.Suggest(limited,date,TrainingTurn.Morning,90)!.Start);
    }
    [Fact] public void OperatingWindowRejectsNewOffHoursButPreservesExistingOnes()
    {
        var empty=TrainingPlan.Empty(Guid.NewGuid());var date=new DateOnly(2026,7,1);var early=Block(empty.Team,date,6,30,TrainingTurn.Morning);var old=empty with{Sessions=new[]{early}};
        Assert.Throws<InvalidDataException>(()=>TrainingSchedule.ValidateChanges(old,empty));TrainingSchedule.ValidateChanges(old with{Sessions=new[]{early with{Objective="Conservado"}}},old);
        TrainingSchedule.ValidateChanges(old,empty,new(OperatingStartHour:6));
    }
    [Fact] public void DailyReadRejectsDuplicateBlockIdentityInsteadOfDoubleCounting()
    {
        var plan=TrainingPlan.Empty(Guid.NewGuid());var date=new DateOnly(2026,7,1);var block=Block(plan.Team,date,9,30,TrainingTurn.Morning);Assert.Throws<InvalidDataException>(()=>TrainingDailyPlanning.Read(plan with{Sessions=new[]{block,block}},date));
    }
    [Fact] public void DailyCreateEditDeleteAndCalendarShareOnePersistentPlan()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var player=squads.Candidates(team)[0].Id;var service=new SqliteSportsStore(file);var date=service.CareerDate();
        TrainingSession Own(int hour,DateOnly d)=>Block(team,d,hour,30,TrainingTurn.Morning,new[]{player}) with{Target=new(TrainingScope.Individual,player)};
        var a=Own(9,date);var b=Own(11,date);var other=Own(9,date.AddDays(1));var plan=service.SavePlan(service.Plan(team) with{Sessions=new[]{a,b,other}});Assert.Equal(60,TrainingDailyPlanning.Read(plan,date).GlobalTotals.ActivityMinutes);
        plan=service.SavePlan(plan with{Sessions=plan.Sessions.Select(s=>s.Id==a.Id?s with{Minutes=45}:s).ToArray()});var day=TrainingDailyPlanning.Read(new SqliteSportsStore(file).Plan(team),date);Assert.Equal(75,day.GlobalTotals.ActivityMinutes);Assert.Equal(2,day.GlobalCount);
        service.SavePlan(plan with{Sessions=plan.Sessions.Where(s=>s.Id!=b.Id).ToArray()});var reopened=new SqliteSportsStore(file).Plan(team);Assert.Equal(45,TrainingDailyPlanning.Read(reopened,date).GlobalTotals.ActivityMinutes);Assert.Contains(reopened.Sessions,s=>s.Id==other.Id&&s.Minutes==30);Assert.Single(TrainingCalendarWeek.Create(reopened,date,date).Days.Single(d=>d.Date==date).Entries);Assert.Empty(new SqliteTrainingExecutionStore(file).CompletedSessions(team));
    }
    [Fact] public void DailyReadIsReadOnlyAndCareerSpecific()
    {
        var (_,file,_)=TacticalCareer();var (_,other,_)=TacticalCareer();var team=new SqliteClubSquadStore(file).Teams()[0].Id;var service=new SqliteSportsStore(file);var player=new SqliteClubSquadStore(file).Candidates(team)[0].Id;var date=service.CareerDate();service.SavePlan(service.Plan(team) with{Sessions=new[]{Block(team,date,9,30,TrainingTurn.Morning,new[]{player}) with{Target=new(TrainingScope.Individual,player)}}});
        var hash=SHA256.HashData(File.ReadAllBytes(file));Assert.Equal(1,TrainingDailyPlanning.Read(service.Plan(team),date).GlobalCount);Assert.Equal(0,TrainingDailyPlanning.Read(new SqliteSportsStore(other).Plan(team),date).GlobalCount);Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(file)));
    }
    [Fact] public void CategoryFilterDoesNotHideConflictsFromPersistenceValidation()
    {
        var (_,file,_)=TacticalCareer();var service=new SqliteSportsStore(file);var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var player=squads.Candidates(team)[0].Id;var date=service.CareerDate();
        var a=Block(team,date,9,30,TrainingTurn.Morning,new[]{player}) with{Target=new(TrainingScope.Individual,player)};var plan=service.SavePlan(service.Plan(team) with{Sessions=new[]{a}});Assert.Equal(0,TrainingDailyPlanning.Read(plan,date,TrainingArea.Gym).VisibleCount);
        Assert.Throws<InvalidDataException>(()=>service.SavePlan(plan with{Sessions=plan.Sessions.Append(a with{Id=Guid.NewGuid(),Exercise="general_strength"}).ToArray()}));Assert.Single(service.Plan(team).Sessions);
    }
}
