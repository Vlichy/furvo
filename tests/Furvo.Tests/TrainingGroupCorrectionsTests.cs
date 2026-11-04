using Furvo.Core.Development;
using Furvo.Core.Domain;
using Furvo.Persistence;
using System.Text.Json;
using System.Security.Cryptography;
using Xunit;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    private (string File,Guid Team,SqliteSportsStore Store,IReadOnlyList<Player> Players,TrainingGroup Group) EditableGroup()
    {var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var store=new SqliteSportsStore(file);var players=squads.Players(team);var group=new TrainingGroup(Guid.NewGuid(),"Grupo base",new[]{players[0].Id},"Objetivo guardado",null);store.SaveGroup(team,group,store.Plan(team).Revision);return(file,team,store,players,group);}
    [Fact] public void NewGroupDraftIsReusableAcrossViewsWithoutWritingAPlan()
    {
        var (file,team,store,players,_)=EditableGroup();var context=new TrainingGroupContext(Guid.NewGuid(),team);var book=new TrainingGroupDraftBook();book.Begin(context);var value=book.Get(context)! with{Name="Nuevo",Objective="Control",Members=new[]{players[1].Id},Responsible=store.Staff(team)[0].Id};var hash=SHA256.HashData(File.ReadAllBytes(file));book.Put(context,value);
        for(var view=0;view<7;view++)Assert.Equal("Nuevo",book.Get(context)!.Name);Assert.True(book.Get(context)!.Dirty);Assert.Null(book.Get(context)!.EditingId);Assert.Single(store.Plan(team).Groups);Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(file)));
    }
    [Theory]
    [InlineData(TrainingGroupExit.Cancel,false,false)]
    [InlineData(TrainingGroupExit.Save,false,false)]
    [InlineData(TrainingGroupExit.Discard,false,true)]
    [InlineData(TrainingGroupExit.Save,true,true)]
    public void GroupExitDecisionsPreserveCancelAndFailedSaveAndPersistOnlyExplicitSave(TrainingGroupExit choice,bool valid,bool leave)
    {
        var (_,team,store,_,group)=EditableGroup();var context=new TrainingGroupContext(Guid.NewGuid(),team);var book=new TrainingGroupDraftBook();book.Begin(context,group);book.Put(context,book.Get(context)! with{Name="Editado"});var called=false;
        var result=book.TryLeave(context,choice,d=>{called=true;if(!valid)return false;store.SaveGroup(team,d.Value(Guid.NewGuid()),store.Plan(team).Revision);return true;});Assert.Equal(leave,result);Assert.Equal(choice==TrainingGroupExit.Save,called);Assert.Equal(leave,book.Get(context) is null);Assert.Equal(choice==TrainingGroupExit.Save&&valid?"Editado":group.Name,Assert.Single(store.Plan(team).Groups).Name);
        if(!leave)Assert.Equal("Editado",book.Get(context)!.Name);else{book.Begin(context,store.Plan(team).Groups[0]);Assert.False(book.Get(context)!.Dirty);}
    }
    [Fact] public void GroupDraftsAreIsolatedByCareerAndTeam()
    {var book=new TrainingGroupDraftBook();var career=Guid.NewGuid();var team=Guid.NewGuid();var a=new TrainingGroupContext(career,team);var b=new TrainingGroupContext(career,Guid.NewGuid());var c=new TrainingGroupContext(Guid.NewGuid(),team);book.Begin(a);book.Put(a,book.Get(a)! with{Name="Solo A"});book.Begin(b);book.Begin(c);Assert.Equal("Solo A",book.Get(a)!.Name);Assert.Equal("",book.Get(b)!.Name);Assert.Equal("",book.Get(c)!.Name);}
    [Fact] public void UnreferencedGroupDeletionPersistsAndUsesStableIdInsteadOfName()
    {
        var (file,team,store,players,group)=EditableGroup();var other=group with{Id=Guid.NewGuid()};store.SaveGroup(team,other,store.Plan(team).Revision);store.DeleteGroup(team,group.Id,store.Plan(team).Revision);var after=store.Plan(team);Assert.Equal(other.Id,Assert.Single(after.Groups).Id);Assert.Equal(group.Name,after.Groups[0].Name);Assert.DoesNotContain(new SqliteSportsStore(file).Plan(team).Groups,g=>g.Id==group.Id);
    }
    [Fact] public void ReferencedGroupCannotBeDeletedOrRemovedWithItsActivitiesInOneSave()
    {
        var (file,team,store,players,group)=EditableGroup();var block=Session(team,players) with{Target=new(TrainingScope.Group,Group:group.Id),Participants=group.Members};store.SavePlan(store.Plan(team) with{Sessions=new[]{block}});var before=SHA256.HashData(File.ReadAllBytes(file));var used=Assert.Throws<TrainingGroupInUseException>(()=>store.DeleteGroup(team,group.Id,store.Plan(team).Revision));Assert.Equal(block.Id,Assert.Single(used.References).Id);Assert.Equal(block.Date,used.References[0].Date);Assert.Throws<TrainingGroupInUseException>(()=>store.SavePlan(store.Plan(team) with{Groups=Array.Empty<TrainingGroup>(),Sessions=Array.Empty<TrainingSession>()}));Assert.Equal(before,SHA256.HashData(File.ReadAllBytes(file)));Assert.Equal(group.Members,Assert.Single(store.Plan(team).Sessions).Participants);
    }
    [Fact] public void ReportReferencesPreventGroupDeletionWithoutChangingTheSnapshot()
    {
        var (file,team,store,_,group)=EditableGroup();var reports=new SqliteAnalysisReportService(file);var report=reports.Generate(team,new(AnalysisScope.Group,Group:group.Id),AnalysisReportKind.Technical,reports.Staff(team,AnalysisReportKind.Technical)[0].Id);var before=JsonSerializer.Serialize(report);var error=Assert.Throws<TrainingGroupInUseException>(()=>store.DeleteGroup(team,group.Id,store.Plan(team).Revision));Assert.Contains(error.References,r=>r.Kind=="Report"&&r.Id==report.Id);Assert.Equal(before,JsonSerializer.Serialize(store.Reports(team).Single()));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EditingMembersPreservesPastOrExecutedSnapshotsAndUpdatesOnlyPendingSessions(bool executed)
    {
        var (file,team,store,players,group)=EditableGroup();var date=store.CareerDate();var historic=Session(team,players) with{Target=new(TrainingScope.Group,Group:group.Id),Participants=group.Members};var future=historic with{Id=Guid.NewGuid(),Date=date.AddDays(2)};store.SavePlan(store.Plan(team) with{Sessions=new[]{historic,future}});
        using(var c=Connection(file))using(var q=c.CreateCommand()){if(executed){q.CommandText="INSERT INTO training_attendance SELECT $id,$team,$date,json_object('Session',json_extract(data_json,'$.Sessions[0]')) FROM training_plans WHERE team_id=$team";q.Parameters.AddWithValue("$id",historic.Id.ToString("D"));q.Parameters.AddWithValue("$team",team.ToString("D"));q.Parameters.AddWithValue("$date",date.ToString("yyyy-MM-dd"));}else{q.CommandText="UPDATE training_plans SET data_json=json_set(data_json,'$.Sessions[0].Date',$date) WHERE team_id=$team";q.Parameters.AddWithValue("$team",team.ToString("D"));q.Parameters.AddWithValue("$date",date.AddDays(-1).ToString("yyyy-MM-dd"));}q.ExecuteNonQuery();}
        if(!executed)historic=historic with{Date=date.AddDays(-1)};
        store.SaveGroup(team,group with{Members=new[]{players[1].Id}},store.Plan(team).Revision);var loaded=new SqliteSportsStore(file).Plan(team);Assert.Equal(JsonSerializer.Serialize(historic),JsonSerializer.Serialize(loaded.Sessions.Single(s=>s.Id==historic.Id)));Assert.Equal(players[1].Id,Assert.Single(loaded.Sessions.Single(s=>s.Id==future.Id).Participants));Assert.Equal(players[1].Id,Assert.Single(loaded.Groups[0].Members));
    }
    [Fact] public void ExecutedGroupSessionKeepsItsSnapshotEvenIfItsRoutineIsNoLongerConfigured()
    {
        var (file,team,store,players,group)=EditableGroup();var tactics=new SqliteTacticalPlanStore(file);var settings=tactics.Settings(tactics.Load()!,Furvo.Core.Tactics.TacticalPlanKind.Initial);var pieces=settings.SetPieces.ToDictionary();pieces[Furvo.Core.Tactics.SetPieceKind.AttackingCorner]=new("short",null);tactics.SaveSettings(Furvo.Core.Tactics.TacticalPlanKind.Initial,settings with{SetPieces=pieces});
        var block=Session(team,players,"corner_practice") with{Target=new(TrainingScope.Group,Group:group.Id),Participants=group.Members,Routine=new(Furvo.Core.Tactics.SetPieceKind.AttackingCorner)};store.SavePlan(store.Plan(team) with{Sessions=new[]{block}});
        using(var c=Connection(file))using(var q=c.CreateCommand()){q.CommandText="INSERT INTO training_attendance SELECT $id,$team,$date,json_object('Session',json_extract(data_json,'$.Sessions[0]')) FROM training_plans WHERE team_id=$team";q.Parameters.AddWithValue("$id",block.Id.ToString("D"));q.Parameters.AddWithValue("$team",team.ToString("D"));q.Parameters.AddWithValue("$date",block.Date.ToString("yyyy-MM-dd"));q.ExecuteNonQuery();}
        settings=tactics.Settings(tactics.Load()!,Furvo.Core.Tactics.TacticalPlanKind.Initial);pieces=settings.SetPieces.ToDictionary();pieces[Furvo.Core.Tactics.SetPieceKind.AttackingCorner]=new(null,null);tactics.SaveSettings(Furvo.Core.Tactics.TacticalPlanKind.Initial,settings with{SetPieces=pieces});store.SaveGroup(team,group with{Members=new[]{players[1].Id}},store.Plan(team).Revision);Assert.Equal(JsonSerializer.Serialize(block),JsonSerializer.Serialize(Assert.Single(store.Plan(team).Sessions)));
    }
    [Fact] public void StaleGroupRevisionCannotOverwriteNewPlanChanges()
    {var (_,team,store,_,group)=EditableGroup();var old=store.Plan(team);store.SavePlan(old);Assert.Throws<InvalidOperationException>(()=>store.SaveGroup(team,group with{Name="Obsoleto"},old.Revision));Assert.Equal(group.Name,store.Plan(team).Groups[0].Name);}
}
