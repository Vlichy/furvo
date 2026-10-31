using Furvo.Core.Development;
using Furvo.Core.Domain;
using Furvo.Persistence;
using Xunit;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    [Theory]
    [InlineData(AnalysisScope.Collective,AnalysisReportKind.Technical)]
    [InlineData(AnalysisScope.Collective,AnalysisReportKind.SquadSummary)]
    [InlineData(AnalysisScope.Collective,AnalysisReportKind.Individual)]
    [InlineData(AnalysisScope.Individual,AnalysisReportKind.Technical)]
    [InlineData(AnalysisScope.Individual,AnalysisReportKind.SquadSummary)]
    [InlineData(AnalysisScope.Individual,AnalysisReportKind.Individual)]
    [InlineData(AnalysisScope.Position,AnalysisReportKind.Technical)]
    [InlineData(AnalysisScope.Position,AnalysisReportKind.SquadSummary)]
    [InlineData(AnalysisScope.Position,AnalysisReportKind.Individual)]
    [InlineData(AnalysisScope.Group,AnalysisReportKind.Technical)]
    [InlineData(AnalysisScope.Group,AnalysisReportKind.SquadSummary)]
    [InlineData(AnalysisScope.Group,AnalysisReportKind.Individual)]
    public void ReportsKeepScopeIndependentFromContent(AnalysisScope scope,AnalysisReportKind kind)
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var roster=squads.Details(team);var generator=new SqliteAnalysisReportService(file);var sports=new SqliteSportsStore(file);var group=new TrainingGroup(Guid.NewGuid(),"Grupo de prueba",roster.Take(2).Select(p=>p.Player.Id).ToArray(),"Supervisión",null);generator.SaveGroup(team,group,0);
        var selection=new AnalysisSelection(scope,scope==AnalysisScope.Individual?roster[0].Player.Id:null,scope==AnalysisScope.Position?roster[0].Player.Position:null,false,scope==AnalysisScope.Group?group.Id:null);
        var report=generator.Generate(team,selection,kind,generator.Staff(team,kind)[0].Id);var loaded=new SqliteSportsStore(file).Reports(team).Single();
        Assert.Equal(report.Id,loaded.Id);Assert.Equal(scope,loaded.Context!.Selection.Scope);Assert.Equal(kind,loaded.Kind);Assert.NotEqual(Guid.Empty,loaded.Context.Career);Assert.Equal(report.Analyst,loaded.Context.Staff.Id);
        var expected=AnalysisReportRules.Select(selection,roster,sports.Plan(team).Groups).Select(p=>p.Player.Id);Assert.Equal(expected,loaded.Context.Players.Select(p=>p.Details.Player.Id));
        foreach(var p in loaded.Context.Players){Assert.Equal(51,p.Details.AttributeGroups.Sum(g=>g.Values.Count));Assert.DoesNotContain(p.Details.AttributeGroups,g=>g.Category==AttributeCategory.Hidden);Assert.All(p.Details.AttributeGroups.SelectMany(g=>g.Values),a=>Assert.InRange(a.Value,1,20));Assert.Null(p.ObservedFatigue);Assert.Empty(p.ObservedAttributeChanges);}
    }
    [Fact] public void ReportStaffMustBeAnExistingEmployeeAndNeverAPlayer()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var service=new SqliteAnalysisReportService(file);
        foreach(var team in squads.Teams().Take(2)){var staff=service.Staff(team.Id,AnalysisReportKind.Technical);Assert.NotEmpty(staff);Assert.All(staff,s=>Assert.DoesNotContain(squads.Candidates(team.Id),p=>p.Id.Value==s.Id));Assert.Throws<InvalidDataException>(()=>service.Generate(team.Id,new(),AnalysisReportKind.Technical,Guid.Empty));Assert.Throws<InvalidDataException>(()=>service.Generate(team.Id,new(),AnalysisReportKind.Technical,squads.Candidates(team.Id)[0].Id.Value));}
    }
    [Fact] public void ReportGroupsAreSharedButHistoricalMembershipIsFrozen()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var members=squads.Candidates(team);var generator=new SqliteAnalysisReportService(file);var sports=new SqliteSportsStore(file);var group=new TrainingGroup(Guid.NewGuid(),"Banda",new[]{members[0].Id},"Seguimiento",null);var plan=generator.SaveGroup(team,group,0);
        var report=generator.Generate(team,new(AnalysisScope.Group,Group:group.Id),AnalysisReportKind.Technical,generator.Staff(team,AnalysisReportKind.Technical)[0].Id);
        generator.SaveGroup(team,group with{Name="Nueva banda",Members=new[]{members[1].Id}},plan.Revision);var loaded=sports.Reports(team).Single();Assert.Equal("Banda",loaded.Context!.GroupName);Assert.Equal(members[0].Id,Assert.Single(loaded.Context.Players).Details.Player.Id);Assert.Equal(members[1].Id,Assert.Single(sports.Plan(team).Groups).Members[0]);
        Assert.Throws<InvalidDataException>(()=>generator.SaveGroup(team,group with{Members=Array.Empty<PlayerId>()},sports.Plan(team).Revision));Assert.Throws<InvalidOperationException>(()=>sports.SaveReport(loaded with{Context=loaded.Context with{GroupName="Alterado"}}));
    }
    [Fact] public void ReportPositionFilterExplicitlyIncludesSecondaryRoles()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var roster=squads.Details(squads.Teams()[0].Id);var player=roster.First(p=>p.SecondaryPositions.Count>0);var role=player.SecondaryPositions[0];
        var primary=AnalysisReportRules.Select(new(AnalysisScope.Position,Position:role),roster,Array.Empty<TrainingGroup>());var secondary=AnalysisReportRules.Select(new(AnalysisScope.Position,Position:role,IncludeSecondary:true),roster,Array.Empty<TrainingGroup>());Assert.All(primary,p=>Assert.Equal(role,p.Player.Position));Assert.Contains(secondary,p=>p.Player.Id==player.Player.Id);Assert.True(secondary.Count>=primary.Count);
    }
    [Fact] public void ReportsRejectForeignCareerAndForeignPlayers()
    {
        var (_,file,_)=TacticalCareer();var (_,other,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var generator=new SqliteAnalysisReportService(file);var report=generator.Generate(team,new(),AnalysisReportKind.Technical,generator.Staff(team,AnalysisReportKind.Technical)[0].Id);
        Assert.Throws<InvalidDataException>(()=>new SqliteSportsStore(other).SaveReport(report));Assert.Empty(new SqliteSportsStore(other).Reports(team));Assert.Throws<InvalidDataException>(()=>generator.Generate(team,new(AnalysisScope.Individual,new PlayerId(Guid.NewGuid())),AnalysisReportKind.Technical,report.Analyst!.Value));
    }
    [Fact] public void SavingReportPrioritiesPreservesTrainingAndHistoricalContent()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var sports=new SqliteSportsStore(file);sports.SavePlan(sports.Plan(team) with{Sessions=new[]{Session(team,squads.Players(team))}});var before=System.Text.Json.JsonSerializer.Serialize(sports.Plan(team));var generator=new SqliteAnalysisReportService(file);var report=generator.Generate(team,new(),AnalysisReportKind.Technical,generator.Staff(team,AnalysisReportKind.Technical)[0].Id);generator.SavePriorities(team,report.Id,new[]{report.Findings[0].Topic});var loaded=new SqliteSportsStore(file).Reports(team).Single();
        Assert.Equal(before,System.Text.Json.JsonSerializer.Serialize(sports.Plan(team)));Assert.Equal(System.Text.Json.JsonSerializer.Serialize(report.Context),System.Text.Json.JsonSerializer.Serialize(loaded.Context));Assert.All(loaded.Context!.Players,p=>{Assert.Equal(1,p.ScheduledSessions);Assert.True(p.PlannedLoad>0);Assert.Null(p.ObservedFatigue);});Assert.Throws<InvalidDataException>(()=>generator.SavePriorities(team,report.Id,new[]{"inventada"}));
    }
    [Fact] public void GroupEditsDoNotSilentlyRewriteScheduledParticipants()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var members=squads.Players(team);var generator=new SqliteAnalysisReportService(file);var sports=new SqliteSportsStore(file);var group=new TrainingGroup(Guid.NewGuid(),"Grupo",new[]{members[0].Id},"Objetivo",null);var plan=generator.SaveGroup(team,group,0);
        var session=Session(team,members) with{Target=new(TrainingScope.Group,Group:group.Id),Participants=group.Members};plan=sports.SavePlan(plan with{Sessions=new[]{session}});generator.SaveGroup(team,group with{Members=new[]{members[1].Id}},plan.Revision);var after=sports.Plan(team);Assert.Equal(session.Participants,Assert.Single(after.Sessions).Participants);Assert.True(after.RequiresReview);
    }
    [Fact] public void LegacyReportsRemainReadableAndAllowOnlyPriorityUpdates()
    {
        var (_,file,_)=TacticalCareer();var sports=new SqliteSportsStore(file);var team=new SqliteClubSquadStore(file).Teams()[0].Id;sports.SavePlan(sports.Plan(team));
        var report=new AnalysisReport(Guid.NewGuid(),team,AnalysisReportKind.Technical,null,null,sports.CareerDate(),null,new[]{new AnalysisFinding("roster","24 jugadores","career-roster",sports.CareerDate())},Array.Empty<string>());
        using(var c=Connection(file))using(var q=c.CreateCommand()){q.CommandText="INSERT INTO sport_analysis_reports VALUES($id,$team,$json)";q.Parameters.AddWithValue("$id",report.Id.ToString("D"));q.Parameters.AddWithValue("$team",team.ToString("D"));q.Parameters.AddWithValue("$json",System.Text.Json.JsonSerializer.Serialize(report));q.ExecuteNonQuery();}
        var loaded=sports.Reports(team).Single();Assert.Null(loaded.Context);new SqliteAnalysisReportService(file).SavePriorities(team,loaded.Id,new[]{"roster"});Assert.Equal("24 jugadores",sports.Reports(team).Single().Findings[0].Content);Assert.Throws<InvalidOperationException>(()=>sports.SaveReport(loaded with{Date=loaded.Date.AddDays(-1)}));
    }
}
