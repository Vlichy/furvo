using Furvo.Core.Development;
using Furvo.Core.Domain;
using Furvo.Persistence;
using System.Security.Cryptography;
using Xunit;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    [Fact] public void GlobalCalendarKeepsLongFieldActivityAlongsideGymAndKeeperActivities()
    {
        var plan=TrainingPlan.Empty(Guid.NewGuid());var date=new DateOnly(2026,7,1);
        var field=Block(plan.Team,date,7,375,TrainingTurn.Morning);var gym=Block(plan.Team,date,16,60,TrainingTurn.Afternoon) with{Exercise="general_strength"};var keeper=Block(plan.Team,date,18,30,TrainingTurn.Afternoon) with{Exercise="handling"};
        plan=plan with{Sessions=new[]{keeper,field,gym},Days=new[]{new TrainingDayPlan(date,TrainingDayKind.Double),new TrainingDayPlan(date.AddDays(1),TrainingDayKind.Rest)}};
        var calendar=TrainingCalendarWeek.Create(plan,date,date);var day=calendar.Days.Single(d=>d.Date==date);Assert.Equal(new[]{field.Id,gym.Id,keeper.Id},day.Entries.Select(e=>e.Session.Id));Assert.Equal("13:15",day.Entries[0].Session.Ends.ToString("HH:mm"));Assert.Equal(new[]{TrainingArea.Field,TrainingArea.Gym,TrainingArea.Goalkeepers},day.Entries.Select(e=>e.Area));Assert.Equal(TrainingDayKind.Double,day.Configuration!.Kind);Assert.Equal(TrainingDayKind.Rest,calendar.Days.Single(d=>d.Date==date.AddDays(1)).Configuration!.Kind);Assert.Equal(3,TrainingDailyPlanning.Read(plan,date).VisibleCount);Assert.Equal(465,TrainingDailyPlanning.Read(plan,date).GlobalTotals.ActivityMinutes);
    }
    [Fact] public void GlobalCalendarRefreshAfterCrossAreaEditAndDeletePreservesIdentityAndCategory()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var store=new SqliteSportsStore(file);var date=store.CareerDate();var player=squads.Candidates(team)[0].Id;
        var field=Block(team,date,9,60,TrainingTurn.Morning,new[]{player}) with{Target=new(TrainingScope.Individual,player)};var gym=field with{Id=Guid.NewGuid(),Start=new(11,0),Exercise="general_strength"};var saved=store.SavePlan(store.Plan(team) with{Sessions=new[]{field,gym}});
        var selected=TrainingCalendarWeek.Create(saved,date,date).Days.Single(d=>d.Date==date).Entries.First().Session;Assert.Equal(1,TrainingAgenda.TabFor(selected));
        saved=store.SavePlan(saved with{Sessions=saved.Sessions.Select(s=>s.Id==selected.Id?s with{Minutes=90}:s).ToArray()});var reloaded=new SqliteSportsStore(file).Plan(team);var view=TrainingCalendarWeek.Create(reloaded,date,date);var entry=Assert.Single(view.Days.SelectMany(d=>d.Entries),e=>e.Session.Id==field.Id);Assert.Equal(90,entry.Session.Minutes);Assert.Equal(TrainingArea.Field,entry.Area);Assert.Equal(2,view.Days.SelectMany(d=>d.Entries).Count());
        store.SavePlan(saved with{Sessions=saved.Sessions.Where(s=>s.Id!=gym.Id).ToArray()});Assert.Equal(field.Id,Assert.Single(TrainingCalendarWeek.Create(store.Plan(team),date,date).Days.SelectMany(d=>d.Entries)).Session.Id);
    }
    [Fact] public void GlobalCalendarReadsPreserveStoredPlanAndCareerIsolation()
    {
        var (_,file,_)=TacticalCareer();var (_,other,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var store=new SqliteSportsStore(file);var date=store.CareerDate();var player=squads.Candidates(team)[0].Id;store.SavePlan(store.Plan(team) with{Sessions=new[]{Block(team,date,7,375,TrainingTurn.Morning,new[]{player}) with{Target=new(TrainingScope.Individual,player)}}});
        var hash=SHA256.HashData(File.ReadAllBytes(file));var plan=store.Plan(team);var first=TrainingCalendarWeek.Create(plan,date,date);var second=TrainingCalendarWeek.Create(plan,date,date);Assert.Equal(first.Start,second.Start);Assert.Equal(first.Days.SelectMany(d=>d.Entries).Select(e=>e.Session.Id),second.Days.SelectMany(d=>d.Entries).Select(e=>e.Session.Id));Assert.Single(first.Days.SelectMany(d=>d.Entries));Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(file)));Assert.Empty(TrainingCalendarWeek.Create(new SqliteSportsStore(other).Plan(team),date,date).Days.SelectMany(d=>d.Entries));Assert.Empty(new SqliteTrainingExecutionStore(file).CompletedSessions(team));
    }
}
