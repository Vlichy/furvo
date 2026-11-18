using System.Security.Cryptography;
using Furvo.Core.Academy;
using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Furvo.Core.Lineups;
using Furvo.Core.Tactics;
using Furvo.Persistence;
using Xunit;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    private static string AcademyFingerprint(TacticalPlanBook book)=>string.Join("|",book.Plans.Select(PlanFingerprint));
    [Fact] public void AcademyCatalogCoversCanonicalClubsWithDifferentStructuresAndStableTeamIds()
    {
        var world=new SqliteWorldStore(EconomicSource).Load();var teams=AcademyCatalog.Teams;Assert.Equal(20,teams.Select(t=>t.ClubId).Distinct().Count());Assert.True(teams.GroupBy(t=>t.ClubId).Select(g=>g.Count()).Distinct().Count()>1);
        Assert.Equal(teams.Count,teams.Select(t=>t.Id).Distinct().Count());Assert.All(teams,t=>{var club=world.Clubs.Single(c=>c.Id==t.ClubId);Assert.StartsWith(club.Name+" ",t.Name);Assert.InRange(t.Size,20,22);Assert.NotEqual(Guid.Empty,t.Id);});
        Assert.Equal(5,teams.Count(t=>t.Name.StartsWith("Club Deportivo Horizonte de Madrid ",StringComparison.Ordinal)));Assert.All(teams.Where(t=>t.Category=="Juvenil"),t=>Assert.Equal(20,t.Size));
    }
    [Fact] public void AcademyGenerationIsReproducibleHasAllAttributesAndAgeCohorts()
    {
        var settings=InitialSquads2026.Settings();var used1=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var used2=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var ids=new HashSet<PlayerId>();
        foreach(var team in AcademyCatalog.Teams.OrderBy(t=>t.Id)){var a=AcademyGenerator.Generate(team,settings.Cultures,used1);var b=AcademyGenerator.Generate(team,settings.Cultures,used2);Assert.Equal(team.Size,a.Count);Assert.Equal(2,a.Count(p=>p.Positions!.Primary==PlayerRole.Goalkeeper));for(var i=0;i<a.Count;i++){var p=a[i];Assert.Equal(p.Id,b[i].Id);Assert.Equal(p.Name,b[i].Name);Assert.Equal(p.BirthDate,b[i].BirthDate);Assert.Equal(p.Attributes.AllValues,b[i].Attributes.AllValues);Assert.True(ids.Add(p.Id));Assert.Equal(61,p.Attributes.AllValues.Count);Assert.Equal(51,p.Attributes.VisibleValues.Count);Assert.All(p.Attributes.AllValues.Values,v=>Assert.InRange(v,1,20));Assert.InRange(p.BirthDate!.Value.Year,team.Category=="Juvenil"?2008:2004,team.Category=="Juvenil"?2010:2007);}}
    }
    [Fact] public void AcademyTeamsPlayersAndStaffPersistWithoutChangingFirstTeamWorld()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteAcademyStore(file);Assert.True(store.IsInitialized);var teams=store.List();Assert.NotEmpty(teams);var world=new SqliteWorldStore(file).Load();Assert.Equal(480,world.Players.Count);Assert.Equal(20,world.Clubs.Count);var all=new HashSet<PlayerId>();foreach(var team in teams){Assert.Equal(team.Team.Id,team.Coach.TeamId);Assert.NotEqual(Guid.Empty,team.Coach.Id);Assert.Equal(team.Team.Size,store.Players(team.Team.Id).Count);foreach(var p in store.Players(team.Team.Id)){Assert.True(all.Add(p.Id));Assert.DoesNotContain(world.Players,x=>x.Id==p.Id);}}
        using var c=Connection(file);using var q=c.CreateCommand();q.CommandText="SELECT count(*) FROM academy_teams";Assert.Equal((long)AcademyCatalog.Teams.Count,q.ExecuteScalar());q.CommandText="PRAGMA foreign_key_check";using var r=q.ExecuteReader();Assert.False(r.Read());
    }
    [Theory][InlineData(AcademyManagement.Direct,AcademyTacticMode.Synchronized)][InlineData(AcademyManagement.Direct,AcademyTacticMode.Own)][InlineData(AcademyManagement.Delegated,AcademyTacticMode.Synchronized)][InlineData(AcademyManagement.Delegated,AcademyTacticMode.Own)]
    public void AcademyManagementAndTacticModesAreIndependent(AcademyManagement management,AcademyTacticMode mode)
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteAcademyStore(file);var old=store.List()[0];var updated=store.SetManagement(old.Team.Id,management,old.Revision);updated=store.SetTacticMode(old.Team.Id,mode,updated.Revision,true);var reopened=new SqliteAcademyStore(file).Get(old.Team.Id);Assert.Equal(management,reopened.Management);Assert.Equal(mode,reopened.TacticMode);Assert.Equal(old.Coach,reopened.Coach);
    }
    [Fact] public void AcademySynchronizationReadsAllThreePlansAndSixDispositionsContinuously()
    {
        var (_,file,_)=TacticalCareer();var academy=new SqliteAcademyStore(file);var team=academy.List()[0];var before=academy.LoadPlans(team.Team.Id);var first=new SqliteTacticalPlanStore(file);var book=first.Load()!;
        foreach(var kind in Enum.GetValues<TacticalPlanKind>()){var p=book.Get(kind);foreach(var phase in Enum.GetValues<PossessionPhase>())p=TacticalPlanRules.MoveAndAssign(p,phase,"s1",.94,.18+(int)kind*.16);var instructions=p.Instructions.ToDictionary();instructions["counter_route"]="left";instructions["recovery"]="counter";p=p with{Mentality=(TeamMentality)((int)kind+1),Instructions=instructions};book=book.Replace(p);}first.Save(book);
        var hash=SHA256.HashData(File.ReadAllBytes(file));var after=academy.LoadPlans(team.Team.Id);Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(file)));var primaryIds=book.Plans.SelectMany(p=>p.Roster.Members.Select(m=>m.PlayerId)).ToHashSet();
        foreach(var kind in Enum.GetValues<TacticalPlanKind>()){var source=book.Get(kind);var p=after.Get(kind);Assert.Equal(source.WithBall.Positions,p.WithBall.Positions);Assert.Equal(source.WithoutBall.Positions,p.WithoutBall.Positions);Assert.Equal(source.Mentality,p.Mentality);Assert.Equal("left",p.Instructions["counter_route"]);Assert.All(p.Roster.Members,m=>Assert.DoesNotContain(m.PlayerId,primaryIds));Assert.Equal(before.Get(kind).Roster.Members.Select(m=>m.PlayerId).OrderBy(x=>x.ToString()),p.Roster.Members.Select(m=>m.PlayerId).OrderBy(x=>x.ToString()));Assert.Equal(11,p.Roster.Members.Count(m=>m.Role==LineupRole.Starter));}Assert.Equal(team.Revision,academy.Get(team.Team.Id).Revision);
    }
    [Fact] public void AcademyDetachingCapturesCurrentGeometryAndBecomesIndependent()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteAcademyStore(file);var state=store.List()[0];var inherited=store.LoadPlans(state.Team.Id);store.SetTacticMode(state.Team.Id,AcademyTacticMode.Own,state.Revision);var own=store.LoadPlans(state.Team.Id);Assert.Equal(AcademyFingerprint(inherited),AcademyFingerprint(own));
        var first=new SqliteTacticalPlanStore(file);var source=first.Load()!;first.Save(source.Replace(TacticalPlanRules.MoveAndAssign(source.Get(TacticalPlanKind.Initial),PossessionPhase.WithBall,"s1",.94,.35)));Assert.Equal(AcademyFingerprint(own),AcademyFingerprint(store.LoadPlans(state.Team.Id)));
        var primary=AcademyFingerprint(first.Load()!);own=store.SavePlans(state.Team.Id,own.Replace(TacticalPlanRules.MoveAndAssign(own.Get(TacticalPlanKind.Losing),PossessionPhase.WithoutBall,"s1",.91,.68)));Assert.Equal(primary,AcademyFingerprint(first.Load()!));Assert.Equal(AcademyFingerprint(own),AcademyFingerprint(new SqliteAcademyStore(file).LoadPlans(state.Team.Id)));
    }
    [Fact] public void AcademyResynchronizationRequiresConfirmationAndFollowsLatestSource()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteAcademyStore(file);var state=store.List()[0];state=store.SetTacticMode(state.Team.Id,AcademyTacticMode.Own,state.Revision);var hash=SHA256.HashData(File.ReadAllBytes(file));Assert.Throws<InvalidOperationException>(()=>store.SetTacticMode(state.Team.Id,AcademyTacticMode.Synchronized,state.Revision));Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(file)));Assert.Equal(AcademyTacticMode.Own,store.Get(state.Team.Id).TacticMode);
        store.SetTacticMode(state.Team.Id,AcademyTacticMode.Synchronized,state.Revision,true);var source=new SqliteTacticalPlanStore(file).Load()!;Assert.Equal(source.Get(TacticalPlanKind.Initial).WithBall.Positions,store.LoadPlans(state.Team.Id).Get(TacticalPlanKind.Initial).WithBall.Positions);
    }
    [Fact] public void AcademySettingsAndPlayersDoNotLeakBetweenCareers()
    {
        var (_,a,_)=TacticalCareer();var (_,b,_)=TacticalCareer();var first=new SqliteAcademyStore(a);var second=new SqliteAcademyStore(b);var state=first.List()[0];var hash=SHA256.HashData(File.ReadAllBytes(b));first.SetManagement(state.Team.Id,AcademyManagement.Direct,state.Revision);Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(b)));Assert.Equal(AcademyManagement.Delegated,second.Get(state.Team.Id).Management);Assert.Equal(first.Players(state.Team.Id).Select(p=>p.Id),second.Players(state.Team.Id).Select(p=>p.Id));
    }
    [Fact] public void ExistingCareersWithoutAcademyReadWithoutMigrationAndUpgradeExplicitly()
    {
        var (_,file,_)=TacticalCareer();var original=new SqliteWorldStore(file).Load().Players.Select(p=>(p.Id,p.Name,p.BirthDate)).ToArray();var primary=AcademyFingerprint(new SqliteTacticalPlanStore(file).Load()!);
        using(var c=Connection(file))using(var q=c.CreateCommand()){q.CommandText="DROP TRIGGER academy_world_distinct_player;DROP TRIGGER academy_world_distinct_player_update;DROP TABLE staff_employments;DROP TABLE staff_professions;DROP TABLE academy_players;DROP TABLE academy_staff;DROP TABLE academy_teams;DROP TABLE academy_schema_version;";q.ExecuteNonQuery();}
        var hash=SHA256.HashData(File.ReadAllBytes(file));var store=new SqliteAcademyStore(file);Assert.False(store.IsInitialized);Assert.Empty(store.List());Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(file)));store.InitializeMissing();Assert.True(store.IsInitialized);Assert.Equal(original,new SqliteWorldStore(file).Load().Players.Select(p=>(p.Id,p.Name,p.BirthDate)));Assert.Equal(primary,AcademyFingerprint(new SqliteTacticalPlanStore(file).Load()!));
    }
    [Fact] public void AcademyQueriesExposeExactlyFiftyOneVisibleAttributes()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteAcademyStore(file);var state=store.List()[0];var hidden=PlayerAttributeCatalog.All.Where(d=>d.IsHidden).Select(d=>d.Code).ToHashSet();foreach(var p in store.Details(state.Team.Id)){Assert.Equal(51,p.AttributeGroups.Sum(g=>g.Values.Count));Assert.All(p.AttributeGroups,g=>Assert.NotEqual(AttributeCategory.Hidden,g.Category));Assert.All(p.AttributeGroups.SelectMany(g=>g.Values),v=>Assert.DoesNotContain(v.Code,hidden));Assert.Equal(state.Team.Name,p.Club?.Name);}
    }
    [Fact] public void AcademyRejectsForeignPlayerAndStalePolicyWithoutWrites()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteAcademyStore(file);var state=store.List()[0];state=store.SetTacticMode(state.Team.Id,AcademyTacticMode.Own,state.Revision);var book=store.LoadPlans(state.Team.Id);var p=book.Get(TacticalPlanKind.Initial);var foreign=new SqliteWorldStore(file).Load().Players[0].Id;var members=p.Roster.Members.ToArray();members[0]=members[0] with{PlayerId=foreign};var hash=SHA256.HashData(File.ReadAllBytes(file));Assert.Throws<InvalidDataException>(()=>store.SavePlans(state.Team.Id,book.Replace(p with{Roster=p.Roster with{Members=members}})));Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(file)));store.SetManagement(state.Team.Id,AcademyManagement.Direct,state.Revision);Assert.Throws<InvalidOperationException>(()=>store.SetManagement(state.Team.Id,AcademyManagement.Delegated,state.Revision));
    }
    [Fact] public void AcademyDirectSynchronizedLineupUsesOwnPlayersWithoutEditingGeometry()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteAcademyStore(file);var state=store.List()[0];store.SetManagement(state.Team.Id,AcademyManagement.Direct,state.Revision);var editor=new SqliteAcademyLineupStore(file,state.Team.Id,TacticalPlanKind.Initial,PossessionPhase.WithoutBall);var l=editor.Load()!;var players=editor.Players();var a=l.Members.First(m=>m.Role==LineupRole.Starter&&players.Single(p=>p.Id==m.PlayerId).Primary!=PlayerRole.Goalkeeper);var b=l.Members.First(m=>m.Role!=LineupRole.Starter&&players.Single(p=>p.Id==m.PlayerId).Primary!=PlayerRole.Goalkeeper);var primary=AcademyFingerprint(new SqliteTacticalPlanStore(file).Load()!);var saved=editor.Save(LineupSelection.Exchange(l,a.PlayerId,b.PlayerId,players));var reopened=editor.Load()!;Assert.Equal(saved.Members,reopened.Members);Assert.Equal(primary,AcademyFingerprint(new SqliteTacticalPlanStore(file).Load()!));Assert.Equal(l.Formation.Slots,reopened.Formation.Slots);
    }
    [Fact] public void AcademyDelegationDoesNotBlockOwnPolicyButDoesBlockPlayerSelection()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteAcademyStore(file);var state=store.List()[0];store.SetTacticMode(state.Team.Id,AcademyTacticMode.Own,state.Revision);var book=store.LoadPlans(state.Team.Id);var p=book.Get(TacticalPlanKind.Initial);book=store.SavePlans(state.Team.Id,book.Replace(p with{Mentality=TeamMentality.Attacking}));Assert.Equal(TeamMentality.Attacking,book.Get(TacticalPlanKind.Initial).Mentality);var editor=new SqliteAcademyLineupStore(file,state.Team.Id,TacticalPlanKind.Initial,PossessionPhase.WithoutBall);Assert.Throws<InvalidOperationException>(()=>editor.Save(editor.Load()!));Assert.Equal(AcademyManagement.Delegated,store.Get(state.Team.Id).Management);
    }
    [Fact] public void AcademySynchronizedSchemeCannotWriteBackToFirstTeam()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteAcademyStore(file);var state=store.List()[0];var book=store.LoadPlans(state.Team.Id);var hash=SHA256.HashData(File.ReadAllBytes(file));Assert.Throws<InvalidOperationException>(()=>store.SavePlans(state.Team.Id,book));Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(file)));
        var foreign=AcademyCatalog.Teams.First(t=>t.ClubId!=state.Team.ClubId);Assert.Throws<InvalidOperationException>(()=>store.Get(foreign.Id));
    }
    [Fact] public void AcademyOwnInstructionsPersistByPlanAndDoNotChangeManagement()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteAcademyStore(file);var state=store.List()[0];store.SetTacticMode(state.Team.Id,AcademyTacticMode.Own,state.Revision);var plans=new SqliteAcademyPlanStore(file,state.Team.Id);var b=plans.Load()!;var settings=plans.Settings(b,TacticalPlanKind.Winning);var i=settings.Instructions.ToDictionary();i["passing"]="short";i["recovery"]="counter";i["counter_route"]="both";i["keeper"]="mixed";plans.SaveSettings(TacticalPlanKind.Winning,settings with{Instructions=i,Mentality=TeamMentality.Attacking});var reopened=new SqliteAcademyPlanStore(file,state.Team.Id);b=reopened.Load()!;Assert.Equal("both",reopened.Settings(b,TacticalPlanKind.Winning).Instructions["counter_route"]);Assert.Null(reopened.Settings(b,TacticalPlanKind.Initial).Instructions["counter_route"]);Assert.Equal(AcademyManagement.Delegated,store.Get(state.Team.Id).Management);
    }
    [Fact] public void AcademyReconciliationWarnsInsteadOfInventingMissingPlayers()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteAcademyStore(file);var state=store.List()[0];var source=new SqliteTacticalPlanStore(file).Load()!;Assert.Throws<InvalidDataException>(()=>AcademyTactics.Resolve(state,source,store.LineupPlayers(state.Team.Id).Take(10).ToArray()));
    }
    [Fact] public void AcademyMembershipConstraintsRejectDoublePermanentAssignments()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteAcademyStore(file);var team=store.List()[0];var p=store.Players(team.Team.Id)[0];var hash=SHA256.HashData(File.ReadAllBytes(file));
        Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(()=>new SqliteWorldStore(file).Apply(new Furvo.Core.Worlds.WorldData(players:new[]{p})));Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(file)));
        using var c=Connection(file);using var q=c.CreateCommand();q.CommandText="INSERT INTO academy_players SELECT * FROM academy_players LIMIT 1";Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(()=>q.ExecuteNonQuery());
    }
    [Fact] public void AcademyInitializationPreservesEveryOriginalPlayerAttributeAndContract()
    {
        var (_,file,_)=TacticalCareer();var source=new SqliteWorldStore(EconomicSource).Load();var actual=new SqliteWorldStore(file).Load();Assert.Equal(source.Contracts,actual.Contracts);Assert.Equal(480,actual.Players.Count);
        foreach(var p in source.Players){var same=actual.Players.Single(a=>a.Id==p.Id);Assert.Equal(p.Name,same.Name);Assert.Equal(p.BirthDate,same.BirthDate);Assert.Equal(p.Attributes.AllValues,same.Attributes.AllValues);Assert.Equal(p.Positions!.Primary,same.Positions!.Primary);Assert.Equal(p.Positions.Secondary,same.Positions.Secondary);}
    }
    [Theory][InlineData(AcademyTacticMode.Synchronized)][InlineData(AcademyTacticMode.Own)]
    public void AcademyDirectManagementCanExchangeCallupsWithoutChangingTheEleven(AcademyTacticMode mode)
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteAcademyStore(file);var state=store.List()[0];state=store.SetManagement(state.Team.Id,AcademyManagement.Direct,state.Revision);state=store.SetTacticMode(state.Team.Id,mode,state.Revision,true);var editor=new SqliteAcademyLineupStore(file,state.Team.Id,TacticalPlanKind.Initial,PossessionPhase.WithoutBall);var lineup=editor.Load()!;var players=editor.Players();var sub=lineup.Members.First(m=>m.Role==LineupRole.Substitute);var reserve=lineup.Members.First(m=>m.Role==LineupRole.Reserve);
        Assert.False(LineupSelection.CanExchange(lineup,sub.PlayerId,reserve.PlayerId,players));Assert.True(LineupSelection.CanExchange(lineup,sub.PlayerId,reserve.PlayerId,players,true));var proposed=LineupSelection.Exchange(lineup,sub.PlayerId,reserve.PlayerId,players,true);editor.Save(proposed);var reopened=editor.Load()!;Assert.Equal(LineupRole.Substitute,reopened.Members.Single(m=>m.PlayerId==reserve.PlayerId).Role);Assert.Equal(LineupRole.Reserve,reopened.Members.Single(m=>m.PlayerId==sub.PlayerId).Role);Assert.Equal(lineup.Members.Where(m=>m.Role==LineupRole.Starter),reopened.Members.Where(m=>m.Role==LineupRole.Starter));
    }
    [Fact] public void AcademyNewRolePrefersOwnNaturalPlayersButUnchangedRolePreservesManualSelection()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteAcademyStore(file);var state=store.List()[0];store.SetManagement(state.Team.Id,AcademyManagement.Direct,state.Revision);var first=new SqliteTacticalPlanStore(file);var source=first.Load()!;source=first.Save(source.Replace(TacticalPlanRules.MoveAndAssign(source.Get(TacticalPlanKind.Initial),PossessionPhase.WithoutBall,"s1",.7,.94)));
        var effective=store.LoadPlans(state.Team.Id);var l=effective.Get(TacticalPlanKind.Initial).View(effective.ClubId,effective.Revision,PossessionPhase.WithoutBall);var players=store.LineupPlayers(state.Team.Id);var member=l.Members.Single(m=>m.SlotId=="s1");Assert.True(LineupSelection.Compatibility(players.Single(p=>p.Id==member.PlayerId),PlayerRole.RightWinger)<=1);
        var bench=l.Members.First(m=>m.Role!=LineupRole.Starter&&players.Single(p=>p.Id==m.PlayerId).Primary==PlayerRole.CentreBack);var editor=new SqliteAcademyLineupStore(file,state.Team.Id,TacticalPlanKind.Initial,PossessionPhase.WithoutBall);editor.Save(LineupSelection.Exchange(l,member.PlayerId,bench.PlayerId,players));Assert.Equal(bench.PlayerId,editor.Load()!.Members.Single(m=>m.SlotId=="s1").PlayerId);
    }
    [Fact] public void AcademyReconcilesNewSlotIdentifiersWithoutDuplicatingOrInventingPlayers()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteAcademyStore(file);var state=store.List()[0];var roster=state.Rosters[TacticalPlanKind.Initial];var positions=FormationCatalog.Initial(state.Team.ClubId).Slots.Select(s=>s with{Id="new_"+s.Id}).ToArray();var formation=new FormationDefinition("new-slots",positions);var players=store.LineupPlayers(state.Team.Id);var assigned=AcademyTactics.Reconcile(state.Team.ClubId,roster.Roster,formation,players,roster.Roles);Assert.Equal(players.Count,assigned.Members.Count);Assert.Equal(11,assigned.Members.Count(m=>m.Role==LineupRole.Starter));Assert.Equal(players.Count,assigned.Members.Select(m=>m.PlayerId).Distinct().Count());Assert.All(assigned.Members.Where(m=>m.Role==LineupRole.Starter),m=>Assert.StartsWith("new_",m.SlotId));
    }
    [Fact] public void AcademyNavigationRequiresEmploymentAndReturnsToTeamArea()
    {
        var p=Profile();var unemployed=EconomicStore().Create(Request(p,CareerStartMode.Unemployed));var w=new CareerWorkspace(unemployed);Assert.False(w.CanOpen(CareerPage.Academy));var employed=EconomicStore().Create(Request(p,CareerStartMode.SignOffer,EconomicOffer(p)));w=new CareerWorkspace(employed);w.Open(CareerPage.Academy);Assert.Equal(CareerArea.Team,w.Area);Assert.True(w.CanManageClub(employed.ClubId!.Value));
    }
}
