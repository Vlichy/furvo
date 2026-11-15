using System.Security.Cryptography;
using System.Text.Json;
using Furvo.Core.Academy;
using Furvo.Core.Development;
using Furvo.Core.Domain;
using Furvo.Presentation.Components;
using Furvo.Persistence;
using Xunit;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    private (string File,Guid Team,PlayerId Player,Guid Coach,SqliteAcademyFollowupStore Store) FollowupFixture()
    {var (_,file,_)=TacticalCareer();var academy=new SqliteAcademyStore(file);var state=academy.List()[0];return(file,state.Team.Id,academy.Players(state.Team.Id)[0].Id,state.Coach.Id,new(file));}
    [Fact] public void AcademyObservationsCreateEditReopenAndDeleteWithStableIdentityAndAuthor()
    {
        var (file,team,player,coach,store)=FollowupFixture();Assert.Empty(store.Observations(player));var note=store.Create(Guid.NewGuid(),team,player,"Observación humana",coach);Assert.Null(note.ModifiedOn);Assert.Equal(coach,note.Author);Assert.False(string.IsNullOrWhiteSpace(note.AuthorName));
        var edited=store.Edit(note.Id,"Texto revisado",Principal(file),note.Revision);var loaded=Assert.Single(new SqliteAcademyFollowupStore(file).Observations(player));Assert.Equal(edited,loaded);Assert.Equal(note.Id,loaded.Id);Assert.Equal(note.Author,loaded.Author);Assert.Equal(note.RegisteredOn,loaded.RegisteredOn);Assert.Equal(new SqliteSportsStore(file).CareerDate(),loaded.ModifiedOn);Assert.Equal(2,loaded.Revision);
        store.Delete(loaded.Id,coach,loaded.Revision);Assert.Empty(new SqliteAcademyFollowupStore(file).Observations(player));
    }
    [Fact] public void AcademyObservationReadsDoNotCreateSchemaAndFirstExplicitSaveMigratesOnlyItsCareer()
    {
        var (file,team,player,_,store)=FollowupFixture();var (_,other,_)=TacticalCareer();var before=SHA256.HashData(File.ReadAllBytes(file));Assert.Empty(store.Observations(player));Assert.Empty(store.Reports(player));Assert.Equal(before,SHA256.HashData(File.ReadAllBytes(file)));
        store.Create(Guid.NewGuid(),team,player,"Solo carrera A",Principal(file));Assert.Single(new SqliteAcademyFollowupStore(file).Observations(player));Assert.Empty(new SqliteAcademyFollowupStore(other).Observations(player));using var c=Connection(other);using var q=c.CreateCommand();q.CommandText="SELECT count(*) FROM sqlite_master WHERE name='academy_observations'";Assert.Equal(0L,(long)q.ExecuteScalar()!);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void AcademyObservationRejectsEmptyTextOrIdentityWithoutPartialMigration(bool emptyIdentity)
    {var (file,team,player,coach,store)=FollowupFixture();Assert.Throws<InvalidDataException>(()=>store.Create(emptyIdentity?Guid.Empty:Guid.NewGuid(),team,player,emptyIdentity?"Válido":"   ",coach));Assert.Empty(store.Observations(player));using var c=Connection(file);using var q=c.CreateCommand();q.CommandText="SELECT count(*) FROM sqlite_master WHERE name='academy_observations'";Assert.Equal(0L,(long)q.ExecuteScalar()!);}
    [Fact] public void AcademyObservationRejectsForeignPlayerTeamAndUnknownAuthor()
    {
        var (file,team,player,coach,store)=FollowupFixture();var academy=new SqliteAcademyStore(file);var other=academy.List().First(t=>t.Team.Id!=team);Assert.Throws<InvalidDataException>(()=>store.Create(Guid.NewGuid(),other.Team.Id,player,"Texto",other.Coach.Id));Assert.Throws<UnauthorizedAccessException>(()=>store.Create(Guid.NewGuid(),team,player,"Texto",Guid.NewGuid()));Assert.Throws<InvalidOperationException>(()=>store.Observations(new PlayerId(Guid.NewGuid())));Assert.Throws<InvalidOperationException>(()=>store.Create(Guid.NewGuid(),AcademyCatalog.Teams.First(t=>t.ClubId!=academy.Get(team).Team.ClubId).Id,player,"Texto",coach));Assert.Empty(store.Observations(player));
    }
    [Fact] public void OtherTeamCoachCannotModifyOrDeleteAnObservation()
    {
        var (file,team,player,coach,store)=FollowupFixture();var note=store.Create(Guid.NewGuid(),team,player,"Original",coach);var foreignCoach=new SqliteAcademyStore(file).List().First(t=>t.Team.Id!=team).Coach.Id;var before=SHA256.HashData(File.ReadAllBytes(file));Assert.Throws<UnauthorizedAccessException>(()=>store.Edit(note.Id,"No autorizado",foreignCoach,note.Revision));Assert.Throws<UnauthorizedAccessException>(()=>store.Delete(note.Id,foreignCoach,note.Revision));Assert.Equal(before,SHA256.HashData(File.ReadAllBytes(file)));Assert.Equal(note,Assert.Single(store.Observations(player)));
    }
    [Fact] public void ObservationRevisionPreventsStaleEditsDeletionAndDuplicateCreation()
    {
        var (file,team,player,coach,store)=FollowupFixture();var note=store.Create(Guid.NewGuid(),team,player,"Original",coach);var next=store.Edit(note.Id,"Segundo",coach,note.Revision);Assert.Throws<InvalidOperationException>(()=>store.Edit(note.Id,"Obsoleto",coach,note.Revision));Assert.Throws<InvalidOperationException>(()=>store.Delete(note.Id,coach,note.Revision));Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(()=>store.Create(note.Id,team,player,"Duplicada",coach));Assert.Equal(next,Assert.Single(store.Observations(player)));
    }
    [Fact] public void ObservationFiltersAreByPlayerIdNotNameOrWholeTeam()
    {var (file,team,player,coach,store)=FollowupFixture();var second=new SqliteAcademyStore(file).Players(team)[1].Id;store.Create(Guid.NewGuid(),team,player,"Uno",coach);store.Create(Guid.NewGuid(),team,second,"Dos",coach);Assert.Equal("Uno",Assert.Single(store.Observations(player)).Text);Assert.Equal("Dos",Assert.Single(store.Observations(second)).Text);}
    [Fact] public void FollowupFindsExistingCollectiveAndIndividualReportsWithoutRewritingSnapshots()
    {
        var (file,team,player,_,store)=FollowupFixture();var reports=new SqliteAnalysisReportService(file);var author=reports.Staff(team,AnalysisReportKind.Individual)[0].Id;var collective=reports.Generate(team,new(),AnalysisReportKind.Technical,author);var individual=reports.Generate(team,new(AnalysisScope.Individual,player),AnalysisReportKind.Individual,author);var another=new SqliteAcademyStore(file).Players(team)[1].Id;reports.Generate(team,new(AnalysisScope.Individual,another),AnalysisReportKind.Individual,author);
        var before=SHA256.HashData(File.ReadAllBytes(file));var loaded=store.Reports(player);Assert.Equal(2,loaded.Count);Assert.Contains(loaded,r=>r.Id==collective.Id);Assert.Contains(loaded,r=>r.Id==individual.Id);Assert.All(loaded,r=>Assert.Equal(JsonSerializer.Serialize(r),JsonSerializer.Serialize(new SqliteSportsStore(file).Reports(team).Single(p=>p.Id==r.Id))));Assert.Equal(before,SHA256.HashData(File.ReadAllBytes(file)));
        var comparison=AcademyFollowup.Compare(collective,individual,player);Assert.Equal(51,comparison.Count);Assert.All(comparison,c=>{Assert.False(PlayerAttributeCatalog.Get(c.Code).IsHidden);Assert.Equal(c.First,c.Second);});Assert.Equal(before,SHA256.HashData(File.ReadAllBytes(file)));
    }
    [Fact] public void ReportComparisonRejectsHiddenDataAndMissingPlayer()
    {
        var (file,team,player,_,store)=FollowupFixture();var reports=new SqliteAnalysisReportService(file);var author=reports.Staff(team,AnalysisReportKind.Individual)[0].Id;var first=reports.Generate(team,new(AnalysisScope.Individual,player),AnalysisReportKind.Individual,author);var second=first with{Id=Guid.NewGuid()};var snapshot=second.Context!.Players[0];var details=snapshot.Details with{AttributeGroups=snapshot.Details.AttributeGroups.Append(new Furvo.Core.Queries.VisibleAttributeGroup(AttributeCategory.Hidden,new[]{new Furvo.Core.Queries.VisibleAttribute(PlayerAttributeCatalog.All.First(a=>a.IsHidden).Code,10)})).ToArray()};var corrupted=second with{Context=second.Context with{Players=new[]{snapshot with{Details=details}}}};
        Assert.Throws<InvalidDataException>(()=>AcademyFollowup.Compare(first,corrupted,player));Assert.Throws<InvalidDataException>(()=>AcademyFollowup.Compare(first,second,new PlayerId(Guid.NewGuid())));Assert.Throws<InvalidDataException>(()=>AcademyFollowup.Compare(first,first,player));
    }
    [Fact] public void ExistingRecommendationsAndMovementHistoryRemainSeparateReadOnlyAndNormativelyBlocked()
    {
        var (file,team,player,coach,_)=FollowupFixture();var squads=new SqliteClubSquadStore(file);var destination=squads.Teams()[0].Id;var store=new SqliteAcademyMobilityStore(file);var date=new SqliteSportsStore(file).CareerDate();var recommendation=new MovementRecommendation(Guid.NewGuid(),coach,player,team,destination,MobilityKind.Permanent,"Recomendación registrada",date,RecommendationStatus.Pending);store.Recommend(recommendation);store.Reject(recommendation.Id,Principal(file));
        // Registro histórico sintético ya ejecutado: la consulta no llama a Move ni altera pertenencias.
        using(var c=Connection(file))using(var q=c.CreateCommand()){q.CommandText="INSERT INTO movement_history(player_id,occurred_on,data_json,evidence) VALUES($player,$date,$json,'fixture-only/historical')";q.Parameters.AddWithValue("$player",player.ToString());q.Parameters.AddWithValue("$date",date.ToString("yyyy-MM-dd"));q.Parameters.AddWithValue("$json",JsonSerializer.Serialize(new{Player=player.ToString(),Source=team,Destination=destination,Kind=1,Date=date}));q.ExecuteNonQuery();}
        var before=SHA256.HashData(File.ReadAllBytes(file));var history=Assert.Single(store.History(player));Assert.Equal(player,history.Request.Player);Assert.Equal("fixture-only/historical",history.Evidence);Assert.Equal(RecommendationStatus.Rejected,Assert.Single(store.Recommendations()).Status);Assert.False(store.Check(new(player,team,destination,MobilityKind.Permanent,date)).Allowed);Assert.Throws<InvalidOperationException>(()=>store.Move(new(player,team,destination,MobilityKind.Permanent,date),Principal(file)));Assert.Equal(before,SHA256.HashData(File.ReadAllBytes(file)));Assert.Equal(team,squads.All()[player].Team);
    }
    [Fact] public void FollowupContextIsIsolatedByCareerTeamAndRetainsSectionFiltersAndSelection()
    {
        var (file,team,player,_,_)=FollowupFixture();var contexts=new AcademyRosterContexts();var career=Guid.NewGuid();var state=contexts.For(career,team);state.Selected=player;state.Sort=2;state.Descending=true;state.NameFilter="Texto";state.Section=3;state.PositionFilter=PlayerRole.CentreBack;
        Assert.Same(state,contexts.For(career,team));Assert.Null(contexts.For(Guid.NewGuid(),team).Selected);Assert.Null(contexts.For(career,Guid.NewGuid()).Selected);Assert.Equal(3,contexts.For(career,team).Section);Assert.Equal("Texto",state.NameFilter);Assert.Equal(PlayerRole.CentreBack,state.PositionFilter);
    }
    [Theory][InlineData(0,false)][InlineData(0,true)][InlineData(1,false)][InlineData(1,true)][InlineData(2,false)][InlineData(2,true)]
    public void AcademyRosterContextSortsAndPreservesStableSelection(int sort,bool descending)
    {var (file,team,player,_,_)=FollowupFixture();var rows=new SqliteAcademyStore(file).Details(team);var state=new AcademyRosterContext{Selected=player,Sort=sort,Descending=descending};var ordered=state.Rows(rows);Assert.Equal(player,state.Selected);Assert.Equal(rows.Count,ordered.Count);Assert.Equal(ordered.Count,ordered.Select(p=>p.Player.Id).Distinct().Count());var again=state.Rows(rows);Assert.Equal(ordered.Select(p=>p.Player.Id),again.Select(p=>p.Player.Id));}
    [Fact] public void AcademyContextFiltersAndClearsSelectionWhenPlayerDisappears()
    {var (file,team,player,_,_)=FollowupFixture();var rows=new SqliteAcademyStore(file).Details(team);var selected=rows.Single(p=>p.Player.Id==player);var state=new AcademyRosterContext{Selected=player,NameFilter=selected.Player.Name,PositionFilter=selected.Player.Position};Assert.Equal(player,Assert.Single(state.Rows(rows)).Player.Id);state.Rows(rows.Where(p=>p.Player.Id!=player).ToArray());Assert.Null(state.Selected);Assert.Equal(selected.Player.Name,state.NameFilter);}
}
