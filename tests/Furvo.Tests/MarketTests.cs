using System.Security.Cryptography;
using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
using Furvo.Core.Worlds;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Xunit;
namespace Furvo.Tests;
public sealed class MarketQueryTests
{
    private static MarketSnapshot Sample()
    {
        var a=new ClubSummary(new ClubId(Guid.Parse("10000000-0000-4000-8000-000000000001")),"Club Alba",null,2);var b=a with{Id=new ClubId(Guid.Parse("10000000-0000-4000-8000-000000000002")),Name="Club Bruma"};
        var roles=new[]{PlayerRole.CentralMidfielder,PlayerRole.Striker,PlayerRole.CentralMidfielder};var names=new[]{"Álvaro Prueba","Bruno Ejemplo","Carlos Prueba"};var rows=Enumerable.Range(0,3).Select(i=>{
            var club=i==0?a:b;var p=new Player(new PlayerId(Guid.Parse("20000000-0000-4000-8000-00000000000"+(i+1))),names[i],new DateOnly(2006-i*3,7,1),"España",PlayerPositions.GroupOf(roles[i]),new PlayerAttributes(PlayerAttributeCatalog.All.ToDictionary(d=>d.Code,_=>10+i)),new PlayerPositions(roles[i]));
            return new MarketPlayer(WorldQueries.ProjectPlayer(p,club,new(2026,7,1)),club.Id.Value,club.Name,"First",new(new ContractId(Guid.NewGuid()),club,new(2025,7,1),new(2027+i,6,30),true));}).ToArray();return new(Guid.NewGuid(),new(2026,7,1),new[]{a,b},rows);
    }
    [Theory][InlineData("Álvaro Prueba",1)][InlineData("pruEba",2)][InlineData("ÁLVARO",1)][InlineData("",3)][InlineData("Sin coincidencia",0)]
    public void MarketNameSearchUsesPartialCaseInsensitiveMatches(string name,int count)=>Assert.Equal(count,MarketQueries.Search(Sample(),new(Name:name)).Count);
    [Fact] public void MarketCombinesClubPositionAgeAndVisibleAttributeAsAnIntersection()
    {var s=Sample();var row=Assert.Single(MarketQueries.Search(s,new(Club:s.Clubs[1].Id,Position:PlayerRole.CentralMidfielder,MinimumAge:24,MaximumAge:27,Attribute:"short_passing",MinimumValue:12,MaximumValue:12)));Assert.Equal("Carlos Prueba",row.Details.Player.Name);Assert.Empty(MarketQueries.Search(s,new(MinimumAge:40)));}
    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)][InlineData(6)]
    public void MarketRejectsInvalidRangesPositionsAndUnknownAttributes(int kind)
    {var f=kind switch{0=>new MarketFilter(MinimumAge:-1),1=>new(MinimumAge:30,MaximumAge:20),2=>new(Attribute:"unknown"),3=>new(Attribute:"short_passing",MinimumValue:0),4=>new(Attribute:"short_passing",MaximumValue:21),5=>new(Attribute:"short_passing",MinimumValue:15,MaximumValue:10),_=>new(Position:(PlayerRole)999)};Assert.Throws<InvalidDataException>(()=>MarketQueries.Search(Sample(),f));}
    [Fact] public void EveryHiddenAttributeIsRejectedBeforeAnyFiltering()
    {var s=Sample();foreach(var definition in PlayerAttributeCatalog.All.Where(d=>d.IsHidden))Assert.Throws<InvalidDataException>(()=>MarketQueries.Search(s,new(Attribute:definition.Code)));Assert.Throws<InvalidDataException>(()=>MarketQueries.Search(s,new(Club:new ClubId(Guid.NewGuid()))));}
    [Theory][InlineData(MarketSort.Name,false)][InlineData(MarketSort.Name,true)][InlineData(MarketSort.Age,false)][InlineData(MarketSort.Age,true)][InlineData(MarketSort.Club,false)][InlineData(MarketSort.Club,true)][InlineData(MarketSort.Position,false)][InlineData(MarketSort.Position,true)][InlineData(MarketSort.ContractEnd,false)][InlineData(MarketSort.ContractEnd,true)]
    public void MarketOrderingIsStableAcrossInputOrdersAndUsesTypedPrimaryKeys(MarketSort sort,bool descending)
    {
        var s=Sample();var ordered=MarketQueries.Search(s,new(),sort,descending);var reversed=MarketQueries.Search(s with{Players=s.Players.Reverse().ToArray()},new(),sort,descending);Assert.Equal(ordered.Select(p=>p.Details.Player.Id),reversed.Select(p=>p.Details.Player.Id));Assert.Equal(3,ordered.Count);
        for(var i=1;i<ordered.Count;i++){var a=ordered[i-1];var b=ordered[i];var comparison=sort switch{MarketSort.Age=>Nullable.Compare(a.Details.Player.Age,b.Details.Player.Age),MarketSort.Club=>StringComparer.OrdinalIgnoreCase.Compare(a.Details.Club?.Name,b.Details.Club?.Name),MarketSort.Position=>Nullable.Compare(a.Details.Player.Position,b.Details.Player.Position),MarketSort.ContractEnd=>Nullable.Compare(a.Contract?.End,b.Contract?.End),_=>StringComparer.OrdinalIgnoreCase.Compare(a.Details.Player.Name,b.Details.Player.Name)};Assert.True(descending?comparison>=0:comparison<=0);}
    }
    [Fact] public void UnknownContractDatesRemainUnknownWithoutFreeStatusOrInventedMoney()
    {var s=Sample();var row=s.Players[0] with{Contract=s.Players[0].Contract! with{Start=null,End=null,Active=null}};Assert.Null(row.Contract.Active);Assert.Null(row.Contract.End);Assert.DoesNotContain(typeof(MarketContract).GetProperties(),p=>p.Name is "Salary" or "ReleaseClause" or "FreeAgent" or "MarketValue");Assert.DoesNotContain(typeof(MarketPlayer).GetProperties(),p=>p.PropertyType==typeof(Player)||p.PropertyType==typeof(PlayerAttributes));}
    [Fact] public void MarketContextKeepsAllFiltersAndStableSelectionAndIsIndependentPerCareer()
    {var s=Sample();var a=new MarketSearchContext(s.Career){Applied=new(Name:"Prueba",MinimumAge:18,Attribute:"short_passing",MinimumValue:10),Selected=s.Players[0].Details.Player.Id,Sort=MarketSort.Age,Descending=true,Scroll=450,Name="Prueba",MinimumAge="18",Attribute="short_passing",MinimumValueText="10"};var b=new MarketSearchContext(Guid.NewGuid());a.Reconcile(MarketQueries.Search(s,a.Applied));Assert.NotNull(a.Selected);Assert.Null(b.Selected);Assert.Equal(450,a.Scroll);Assert.True(a.Descending);Assert.Equal("Prueba",a.Name);a.Reconcile(Array.Empty<MarketPlayer>());Assert.Null(a.Selected);Assert.Throws<KeyNotFoundException>(()=>MarketQueries.Player(s,new PlayerId(Guid.NewGuid())));}
}
public sealed partial class CareerCreationTests
{
    [Fact] public void MarketReadsAllCareerClubsAndAcademyPlayersWithStableIdsAndOnlyPublicAttributes()
    {
        var (_,file,_)=TacticalCareer();var s=new SqliteMarketQueries(file).Read();var world=new SqliteWorldStore(file).Load();var own=new SqliteClubSquadStore(file).All();Assert.Equal(20,s.Clubs.Count);Assert.Equal(1894-own.Count,s.Players.Count);Assert.Equal(456,s.Players.Count(p=>p.Category=="First"));Assert.Equal(1414-(own.Count-24),s.Players.Count(p=>p.Category!="First"));Assert.Equal(s.Players.Count,s.Players.Select(p=>p.Details.Player.Id).Distinct().Count());Assert.All(world.Players.Where(p=>!own.ContainsKey(p.Id)),p=>Assert.Contains(s.Players,r=>r.Details.Player.Id==p.Id));Assert.DoesNotContain(s.Players,p=>own.ContainsKey(p.Details.Player.Id));
        Assert.All(s.Players,p=>{Assert.Equal(51,p.Details.AttributeGroups.Sum(g=>g.Values.Count));Assert.DoesNotContain(p.Details.AttributeGroups,g=>g.Category==AttributeCategory.Hidden);Assert.All(p.Details.AttributeGroups.SelectMany(g=>g.Values),v=>{Assert.False(PlayerAttributeCatalog.Get(v.Code).IsHidden);Assert.InRange(v.Value,1,20);});});
    }
    [Fact] public void MarketReadSearchAndPlayerDetailsNeverWriteSQLiteOrCreateSchemas()
    {var (_,file,_)=TacticalCareer();var before=SHA256.HashData(File.ReadAllBytes(file));var s=new SqliteMarketQueries(file).Read();var rows=MarketQueries.Search(s,new(MinimumAge:17,Attribute:"short_passing",MinimumValue:10),MarketSort.ContractEnd,true);Assert.NotEmpty(rows);Assert.Equal(rows[0],MarketQueries.Player(s,rows[0].Details.Player.Id));Assert.Equal(before,SHA256.HashData(File.ReadAllBytes(file)));}
    [Fact] public void MarketUsesCurrentCareerValuesAndDoesNotMixIndependentSaves()
    {
        var (_,a,_)=TacticalCareer();var (_,b,_)=TacticalCareer();var visible=new SqliteMarketQueries(a).Read().Players.First(p=>p.Category=="First").Details.Player.Id;var original=new SqliteWorldStore(a).Load().Players.Single(p=>p.Id==visible);var changed=new Player(original.Id,"Nombre cambiado solo en A",original.BirthDate,original.Nationality,original.MainPosition,original.Attributes,original.Positions);new SqliteWorldStore(a).Apply(new WorldData(players:new[]{changed}));var first=new SqliteMarketQueries(a).Read();var second=new SqliteMarketQueries(b).Read();Assert.NotEqual(first.Career,second.Career);Assert.Equal(changed.Name,MarketQueries.Player(first,original.Id).Details.Player.Name);Assert.Equal(original.Name,MarketQueries.Player(second,original.Id).Details.Player.Name);Assert.Equal(original.Name,new SqliteWorldStore(EconomicSource).Load().Players.Single(p=>p.Id==original.Id).Name);
    }
    [Fact] public void MarketRespectsStoredSportingAssignmentsWithoutChangingThem()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var teams=squads.Teams();var player=squads.Players(teams[0].Id)[0];new SqliteSportsStore(file).SavePlan(new SqliteSportsStore(file).Plan(teams[0].Id));
        using(var c=Connection(file))using(var q=c.CreateCommand()){q.CommandText="INSERT INTO sport_assignments VALUES($player,$club,$team)";q.Parameters.AddWithValue("$player",player.Id.ToString());q.Parameters.AddWithValue("$club",teams[0].Club.ToString());q.Parameters.AddWithValue("$team",teams[1].Id.ToString("D"));q.ExecuteNonQuery();}
        var before=SHA256.HashData(File.ReadAllBytes(file));var snapshot=new SqliteMarketQueries(file).Read();Assert.Throws<KeyNotFoundException>(()=>MarketQueries.Player(snapshot,player.Id));Assert.Contains(squads.Players(teams[1].Id),p=>p.Id==player.Id);Assert.Equal(1894-squads.All().Count,snapshot.Players.Count);Assert.Equal(before,SHA256.HashData(File.ReadAllBytes(file)));
    }
    [Fact] public void MarketDoesNotInventContractsForAcademyPlayersAndDoesNotTreatExpiredContractAsCurrentClub()
    {
        var (_,file,_)=TacticalCareer();var visible=new SqliteMarketQueries(file).Read().Players.First(p=>p.Category=="First").Details.Player.Id;var original=new SqliteWorldStore(file).Load().Contracts.Single(c=>c.PlayerId==visible);var date=new SqliteSportsStore(file).CareerDate();new SqliteWorldStore(file).Apply(new WorldData(contracts:new[]{new Contract(original.Id,original.ClubId,original.PlayerId,date.AddYears(-1),date.AddDays(-1))}));var snapshot=new SqliteMarketQueries(file).Read();Assert.DoesNotContain(snapshot.Players,p=>p.Details.Player.Id==original.PlayerId);var expired=new SqliteWorldStore(file).Load().Contracts.Single(c=>c.Id==original.Id);Assert.Equal(date.AddDays(-1),expired.EndDate);Assert.All(snapshot.Players.Where(p=>p.Category=="Juvenil"),p=>Assert.Null(p.Contract));
    }
    [Fact] public void MarketReadsLegacyCareerWithoutInitializingMissingAcademy()
    {
        var (_,file,_)=TacticalCareer();using(var c=Connection(file))using(var q=c.CreateCommand()){q.CommandText="DROP TRIGGER academy_world_distinct_player;DROP TRIGGER academy_world_distinct_player_update;DROP TABLE academy_players;DROP TABLE academy_staff;DROP TABLE academy_teams;DROP TABLE academy_schema_version;";q.ExecuteNonQuery();}var before=SHA256.HashData(File.ReadAllBytes(file));var s=new SqliteMarketQueries(file).Read();Assert.Equal(456,s.Players.Count);Assert.Equal(before,SHA256.HashData(File.ReadAllBytes(file)));Assert.False(new SqliteAcademyStore(file).IsInitialized);
    }
    [Fact] public void MarketRejectsInitialWorldAndMissingFileWithoutCreatingAnything()
    {Assert.Throws<InvalidOperationException>(()=>new SqliteMarketQueries(EconomicSource).Read());var missing=Path.Combine(_root,"missing-market.db");Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(()=>new SqliteMarketQueries(missing).Read());Assert.False(File.Exists(missing));}
    [Theory][InlineData(CareerStartMode.SignOffer,true)][InlineData(CareerStartMode.Unemployed,false)]
    public void MarketNavigationIsReadOnlyAndNeverConfersManagementOfOtherClubs(CareerStartMode mode,bool allowed)
    {var p=Profile();var career=EconomicStore().Create(Request(p,mode,mode==CareerStartMode.SignOffer?EconomicOffer(p):null));var w=new CareerWorkspace(career);Assert.Equal(allowed,w.CanOpen(CareerPage.MarketSearch));if(allowed){w.Open(CareerPage.MarketSearch);Assert.Equal(CareerArea.Market,w.Area);var other=new SqliteWorldStore(EconomicSource).Load().Clubs.First(c=>c.Id!=career.ClubId);Assert.False(w.CanManageClub(other.Id));Assert.Throws<InvalidOperationException>(()=>w.RequireManagement(other.Id));}}
}
