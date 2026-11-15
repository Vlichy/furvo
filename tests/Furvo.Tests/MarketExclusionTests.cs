using System.Security.Cryptography;
using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Xunit;
namespace Furvo.Tests;

public sealed class MarketExclusionTests
{
    private static MarketSnapshot Sample()
    {
        // Los nombres de clubes coinciden a propósito: la pertenencia se decide únicamente por ID.
        var own=new ClubSummary(new ClubId(Guid.Parse("10000000-0000-4000-8000-000000000001")),"Club con mismo nombre",null,2);
        var other=own with{Id=new ClubId(Guid.Parse("10000000-0000-4000-8000-000000000002"))};
        var players=Enumerable.Range(0,6).Select(i=>{
            var club=i<3?own:other;var category=new[]{"First","Senior","Juvenil"}[i%3];
            var p=new Player(new PlayerId(Guid.Parse("20000000-0000-4000-8000-00000000000"+(i+1))),"Adrián "+(i<3?"Propio":"Externo")+" "+category,new DateOnly(2005,7,1),"España",PlayerPosition.Defender,new PlayerAttributes(PlayerAttributeCatalog.All.ToDictionary(a=>a.Code,_=>15)),new PlayerPositions(PlayerRole.CentreBack));
            return new MarketPlayer(WorldQueries.ProjectPlayer(p,club,new DateOnly(2026,7,1)),Guid.NewGuid(),category,category,null);
        }).ToArray();
        return new(Guid.NewGuid(),new DateOnly(2026,7,1),new[]{own,other},players,own.Id);
    }
    [Theory][InlineData("First")][InlineData("Senior")][InlineData("Juvenil")]
    public void ManagedClubIsExcludedAtEveryLevelAndMatchingExternalTeamRemains(string category)
    {var s=Sample();var row=Assert.Single(MarketQueries.Search(s,new()),p=>p.Category==category);Assert.Equal(s.Clubs[1].Id,row.Details.Club!.Id);Assert.Equal(3,MarketQueries.Search(s,new()).Count);Assert.All(s.Players.Where(p=>p.Details.Club!.Id==s.ManagedClub),p=>Assert.Throws<KeyNotFoundException>(()=>MarketQueries.Player(s,p.Details.Player.Id)));}
    [Theory][InlineData(0,0)][InlineData(1,3)][InlineData(2,0)][InlineData(3,3)][InlineData(4,3)][InlineData(5,3)][InlineData(6,3)][InlineData(7,3)]
    public void NoFilterCanReintroduceOwnPlayers(int kind,int count)
    {
        var s=Sample();
        MarketFilter filter=kind switch{
            0=>new(Name:"Adrián Propio First"),1=>new(Name:"  adrian "),2=>new(Club:s.ManagedClub),3=>new(Club:s.Clubs[1].Id),
            4=>new(Position:PlayerRole.CentreBack),5=>new(Attribute:"marking",MinimumValue:13,MaximumValue:17),
            6=>new(MinimumAge:20,MaximumAge:22),_=>new(Name:"ADRIAN",Club:s.Clubs[1].Id,Position:PlayerRole.CentreBack,MinimumAge:21,MaximumAge:21,Attribute:"marking",MinimumValue:15,MaximumValue:15)};
        var rows=MarketQueries.Search(s,filter,MarketSort.Attribute,true,"pace");
        Assert.Equal(count,rows.Count);Assert.DoesNotContain(rows,p=>p.Details.Club!.Id==s.ManagedClub);
    }
    [Fact] public void ChangingManagedClubChangesResultsAndSelectionScopeWithoutChangingColumnPreferences()
    {
        var s=Sample();var old=new MarketSearchContext(s.Career,s.ManagedClub){Selected=s.Players[3].Details.Player.Id,Name="adrian",Sort=MarketSort.Attribute,SortAttribute="pace"};
        var preferences=new MarketColumnPreferences();preferences.Set(PlayerRole.CentreBack,new[]{"pace","marking"});var before=preferences.Serialize();
        var changed=s with{ManagedClub=s.Clubs[1].Id};
        Assert.False(old.MatchesScope(changed.Career,changed.ManagedClub));Assert.False(old.MatchesScope(Guid.NewGuid(),s.ManagedClub));
        Assert.All(MarketQueries.Search(changed,new()),p=>Assert.Equal(s.ManagedClub,p.Details.Club!.Id));
        Assert.Throws<KeyNotFoundException>(()=>MarketQueries.Player(changed,old.Selected!.Value));
        var clean=new MarketSearchContext(changed.Career,changed.ManagedClub);Assert.Null(clean.Selected);Assert.Empty(clean.Name);Assert.Equal(before,preferences.Serialize());
    }
    [Fact] public void UnverifiedMembershipIsNotPresentedAsAnExternalClub()
    {var s=Sample();var unknown=s.Players[3] with{Details=s.Players[3].Details with{Club=null}};var changed=s with{Players=new[]{unknown}};Assert.Empty(MarketQueries.Search(changed,new()));Assert.Throws<KeyNotFoundException>(()=>MarketQueries.Player(changed,unknown.Details.Player.Id));}
}

public sealed partial class CareerCreationTests
{
    [Theory][InlineData("First")][InlineData("Senior")][InlineData("Juvenil")]
    public void MarketSnapshotExcludesOwnLevelBeforeProjectionAndOwnedQueriesStayAvailable(string category)
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var teams=squads.Teams().Where(t=>t.Category==category).ToArray();Assert.NotEmpty(teams);
        var before=SHA256.HashData(File.ReadAllBytes(file));var snapshot=new SqliteMarketQueries(file).Read();var own=squads.All();
        Assert.Equal(1894-own.Count,snapshot.Players.Count);Assert.DoesNotContain(snapshot.Players,p=>p.Details.Club!.Id==snapshot.ManagedClub);
        foreach(var team in teams){
            var players=squads.Players(team.Id);Assert.NotEmpty(players);
            foreach(var player in players){Assert.DoesNotContain(snapshot.Players,p=>p.Details.Player.Id==player.Id);Assert.Empty(MarketQueries.Search(snapshot,new(Name:player.Name)));Assert.Throws<KeyNotFoundException>(()=>MarketQueries.Player(snapshot,player.Id));}
            Assert.Equal(players.Count,squads.Details(team.Id).Count);
            Assert.All(squads.Details(team.Id),p=>Assert.Equal(51,p.AttributeGroups.Sum(g=>g.Values.Count)));
        }
        Assert.Equal(before,SHA256.HashData(File.ReadAllBytes(file)));
    }
    [Fact] public void MarketReadKeepsImmutablePersistedOwnerInsteadOfAcceptingAnArbitraryUiClub()
    {
        var (_,file,_)=TacticalCareer();var before=new SqliteMarketQueries(file).Read();var hash=SHA256.HashData(File.ReadAllBytes(file));var other=before.Players.First(p=>p.Category=="First").Details.Club!.Id;
        // La carrera actual protege su empleo inicial. Mercado no elimina ese disparador para probar un sistema futuro.
        using(var c=Connection(file))using(var q=c.CreateCommand()){q.CommandText="UPDATE career_state SET club_id=$club WHERE singleton=1";q.Parameters.AddWithValue("$club",other.ToString());Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(()=>q.ExecuteNonQuery());}
        var after=new SqliteMarketQueries(file).Read();
        Assert.Equal(before.Career,after.Career);Assert.Equal(before.ManagedClub,after.ManagedClub);Assert.DoesNotContain(after.Players,p=>p.Details.Club!.Id==after.ManagedClub);
        Assert.Contains(after.Players,p=>p.Details.Club!.Id==other);Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(file)));
    }
    [Fact] public void MarketIndependentCareersUseTheirOwnManagedClubAndNeverMutateEitherFile()
    {
        var (_,a,_)=TacticalCareer();var first=new SqliteMarketQueries(a).Read();
        var profile=Profile();var club=first.Clubs.First(c=>c.Id!=first.ManagedClub).Id;var offer=new InitialManagerOfferSource(PublishedWorldQueries.Open(EconomicSource)).Create(profile,club);
        var store=EconomicStore();var career=store.Create(Request(profile,CareerStartMode.SignOffer,offer));var b=store.PathFor(career.Id);
        var hashA=SHA256.HashData(File.ReadAllBytes(a));var hashB=SHA256.HashData(File.ReadAllBytes(b));var second=new SqliteMarketQueries(b).Read();
        Assert.NotEqual(first.Career,second.Career);Assert.NotEqual(first.ManagedClub,second.ManagedClub);Assert.DoesNotContain(second.Players,p=>p.Details.Club!.Id==club);
        Assert.Contains(second.Players,p=>p.Details.Club!.Id==first.ManagedClub);Assert.Contains(first.Players,p=>p.Details.Club!.Id==club);
        Assert.Equal(hashA,SHA256.HashData(File.ReadAllBytes(a)));Assert.Equal(hashB,SHA256.HashData(File.ReadAllBytes(b)));
    }
}
