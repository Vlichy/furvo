using System.Security.Cryptography;
using Furvo.Core.Development;
using Furvo.Persistence;
using Xunit;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    [Theory]
    [InlineData(2026,7,1,2026,6,29)]
    [InlineData(2027,1,3,2026,12,28)]
    [InlineData(2028,3,1,2028,2,28)]
    [InlineData(2026,11,2,2026,11,2)]
    public void CalendarUsesSevenDaysStartingMonday(int y,int m,int d,int sy,int sm,int sd)
    {
        var reference=new DateOnly(y,m,d);var week=TrainingCalendarWeek.Create(TrainingPlan.Empty(Guid.NewGuid()),reference,reference);Assert.Equal(new DateOnly(sy,sm,sd),week.Start);Assert.Equal(7,week.Days.Count);Assert.Equal(DayOfWeek.Monday,week.Days[0].Date.DayOfWeek);Assert.Equal(DayOfWeek.Sunday,week.Days[6].Date.DayOfWeek);Assert.Single(week.Days,day=>day.IsCurrent);Assert.All(week.Days,day=>Assert.Empty(day.Entries));
    }
    [Theory]
    [InlineData(TrainingArea.Field)]
    [InlineData(TrainingArea.Gym)]
    [InlineData(TrainingArea.Goalkeepers)]
    [InlineData(TrainingArea.SetPieces)]
    [InlineData(TrainingArea.Recovery)]
    [InlineData(TrainingArea.Technology)]
    public void CalendarFiltersSameRecordsWithoutCopiesOrDuplicates(TrainingArea area)
    {
        var team=Guid.NewGuid();var date=new DateOnly(2026,7,1);
        var sessions=TrainingCatalog.Exercises.Select((e,i)=>new TrainingSession(Guid.NewGuid(),team,date,new TimeOnly(9,0),5,e.Code,TrainingLoad.Medium,new(TrainingScope.Team),Array.Empty<Furvo.Core.Domain.PlayerId>())).ToArray();
        var plan=TrainingPlan.Empty(team) with{Sessions=sessions};var full=TrainingCalendarWeek.Create(plan,date,date);var filtered=TrainingCalendarWeek.Create(plan,date,date,area);
        var selected=filtered.Days.SelectMany(d=>d.Entries).ToArray();Assert.Equal(sessions.Count(s=>TrainingCatalog.Get(s.Exercise).Area==area),selected.Length);Assert.All(selected,e=>{Assert.Equal(area,e.Area);Assert.Same(sessions.Single(s=>s.Id==e.Session.Id),e.Session);});Assert.Equal(sessions.Length,full.Days.SelectMany(d=>d.Entries).Select(e=>e.Session.Id).Distinct().Count());
    }
    [Fact] public void CalendarOrdersMultipleSessionsAndUsesStableIdentityForTies()
    {
        var team=Guid.NewGuid();var date=new DateOnly(2026,7,1);var a=new TrainingSession(Guid.Parse("11111111-1111-1111-1111-111111111111"),team,date,new(16,0),5,"technical",TrainingLoad.Low,new(TrainingScope.Team),Array.Empty<Furvo.Core.Domain.PlayerId>());var b=a with{Id=Guid.Parse("22222222-2222-2222-2222-222222222222"),Start=new(9,0)};var c=b with{Id=Guid.Parse("33333333-3333-3333-3333-333333333333")};var plan=TrainingPlan.Empty(team) with{Sessions=new[]{c,a,b,a with{Id=Guid.NewGuid(),Date=date.AddDays(7)}}};var entries=TrainingCalendarWeek.Create(plan,date,date).Days.Single(d=>d.Date==date).Entries;Assert.Equal(new[]{b.Id,c.Id,a.Id},entries.Select(e=>e.Session.Id));
    }
    [Fact] public void CalendarDistinguishesEmptyRestPlannedAndExecutionEvidence()
    {
        var team=Guid.NewGuid();var date=new DateOnly(2026,7,1);var session=new TrainingSession(Guid.NewGuid(),team,date,new(9,0),5,"technical",TrainingLoad.Low,new(TrainingScope.Team),Array.Empty<Furvo.Core.Domain.PlayerId>());var rest=session with{Id=Guid.NewGuid(),Exercise="rest",Load=TrainingLoad.Rest,Start=new(11,0)};var plan=TrainingPlan.Empty(team) with{Sessions=new[]{session,rest}};
        var pending=TrainingCalendarWeek.Create(plan,date,date);Assert.Equal(6,pending.Days.Count(d=>d.Entries.Count==0));Assert.Equal(new[]{TrainingCalendarState.Planned,TrainingCalendarState.Rest},pending.Days.SelectMany(d=>d.Entries).Select(e=>e.State));
        var done=TrainingCalendarWeek.Create(plan,date,date,completed:new HashSet<Guid>{session.Id});Assert.Equal(TrainingCalendarState.Completed,done.Days.SelectMany(d=>d.Entries).Single(e=>e.Session.Id==session.Id).State);
    }
    [Fact] public void CalendarReadIsCompatibleWithCareerWithoutSportsSchemaAndDoesNotWrite()
    {
        var (_,file,_)=TacticalCareer();var sports=new SqliteSportsStore(file);var team=new SqliteClubSquadStore(file).Teams()[0].Id;var before=SHA256.HashData(File.ReadAllBytes(file));Assert.Empty(new SqliteTrainingExecutionStore(file).CompletedSessions(team));_=TrainingCalendarWeek.Create(sports.Plan(team),sports.CareerDate(),sports.CareerDate());Assert.Equal(before,SHA256.HashData(File.ReadAllBytes(file)));
    }
    [Fact] public void CalendarSaveEditAndDeletePreserveIdentityWithoutExecutingSessions()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var sports=new SqliteSportsStore(file);var a=Session(team,squads.Players(team));var b=Session(team,squads.Players(team),"general_strength",hour:11);var plan=sports.SavePlan(sports.Plan(team) with{Sessions=new[]{a,b}});
        var edit=a with{Start=new(15,0)};plan=sports.SavePlan(plan with{Sessions=new[]{edit,b}});var loaded=new SqliteSportsStore(file).Plan(team);Assert.Equal(new[]{b.Id,a.Id},TrainingCalendarWeek.Create(loaded,a.Date,a.Date).Days.SelectMany(d=>d.Entries).Select(e=>e.Session.Id));Assert.Empty(new SqliteTrainingExecutionStore(file).CompletedSessions(team));Assert.Null(new SqliteTrainingExecutionStore(file).ObservedPhysical(a.Participants[0]));
        sports.SavePlan(plan with{Sessions=new[]{b}});Assert.Equal(b.Id,Assert.Single(new SqliteSportsStore(file).Plan(team).Sessions).Id);
    }
}
