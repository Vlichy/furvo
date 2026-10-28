using System.Security.Cryptography;
using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Furvo.Core.Lineups;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Xunit;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    private static string Fingerprint(CareerLineup state)=>string.Join("|",state.Members.OrderBy(m=>m.PlayerId.ToString()).Select(m=>$"{m.PlayerId}:{m.Role}:{m.SlotId}"));
    [Fact] public void FormationCatalogHasTwentyClubsAndElevenValidDistinctSchemes()
    {
        Assert.Equal(20,FormationCatalog.Clubs.Count);Assert.Equal(11,FormationCatalog.All.Count);
        foreach(var f in FormationCatalog.All){FormationCatalog.Validate(f);Assert.Equal(11,f.Slots.Count);Assert.Single(f.Slots,s=>s.Role==PlayerRole.Goalkeeper);Assert.Equal(11,f.Slots.Select(s=>(s.X,s.Y)).Distinct().Count());}
    }
    [Fact] public void EveryClubAndFormationProducesCompleteReproducibleLineupWithoutHiddenData()
    {
        var q=PublishedWorldQueries.Open(EconomicSource);
        foreach(var club in q.GetClubs())
        {
            var squad=q.GetSquad(club.Id).Select(p=>LineupPlayer.From(q.GetPlayer(club.Id,p.Id))).ToArray();
            Assert.All(squad,p=>Assert.Equal(51,p.Visible.Count));
            foreach(var f in FormationCatalog.All)
            {
                var a=LineupSelection.Generate(club.Id,"0123456789abcdef",f,squad);
                var b=LineupSelection.Generate(club.Id,"0123456789abcdef",f,squad.Reverse().ToArray());
                Assert.Equal(Fingerprint(a),Fingerprint(b));Assert.Equal(11,a.Members.Count(m=>m.Role==LineupRole.Starter));
                Assert.Equal(7,a.Members.Count(m=>m.Role==LineupRole.Substitute));Assert.Equal(6,a.Members.Count(m=>m.Role==LineupRole.Reserve));
                var changed=LineupSelection.Generate(club.Id,a.Seed,f,squad,a.BenchLimit,a);Assert.Equal(Fingerprint(a),Fingerprint(changed));
            }
        }
    }
    [Fact] public void LineupReplacementPersistsAndNeverChangesAnotherCareerOrWorld()
    {
        var hash=SHA256.HashData(File.ReadAllBytes(EconomicSource));var p=Profile();var store=EconomicStore();var offer=EconomicOffer(p);
        var a=store.Create(Request(p,CareerStartMode.SignOffer,offer));var b=store.Create(Request(p,CareerStartMode.SignOffer,offer));
        var db=new SqliteLineupStore(store.PathFor(a.Id));var second=new SqliteLineupStore(store.PathFor(b.Id));
        var before=db.Load()!;var secondBefore=Fingerprint(second.Load()!);var players=db.Players();
        var gk=before.Formation.Slots.Single(s=>s.Role==PlayerRole.Goalkeeper);
        var replacement=players.First(x=>x.Primary==PlayerRole.Goalkeeper&&before.Members.Single(m=>m.PlayerId==x.Id).Role!=LineupRole.Starter);
        var saved=db.Save(LineupSelection.Replace(before,gk.Id,replacement.Id,players));
        Assert.Equal(1,saved.Revision);Assert.Equal(Fingerprint(saved),Fingerprint(new SqliteLineupStore(store.PathFor(a.Id)).Load()!));
        Assert.Throws<InvalidOperationException>(()=>db.Save(before));Assert.Equal(secondBefore,Fingerprint(second.Load()!));
        Assert.Throws<InvalidOperationException>(()=>LineupSelection.Replace(saved,gk.Id,players.First(x=>x.Primary!=PlayerRole.Goalkeeper).Id,players));
        var changed=db.Save(LineupSelection.Generate(saved.ClubId,saved.Seed,FormationCatalog.All.First(f=>f.Id!=saved.Formation.Id),players,saved.BenchLimit,saved));
        Assert.Equal(2,changed.Revision);Assert.Equal(Fingerprint(changed),Fingerprint(db.Load()!));
        Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(EconomicSource)));
    }
    [Fact] public void LegacyLineupReadsNeverMigrateAndExplicitInitializationIsIdempotent()
    {
        var p=Profile();var store=EconomicStore();var career=store.Create(Request(p,CareerStartMode.SignOffer,EconomicOffer(p)));var file=store.PathFor(career.Id);
        using(var c=Connection(file))using(var cmd=c.CreateCommand()){cmd.CommandText="DROP TABLE career_lineup_members; DROP TABLE career_lineups; DROP TABLE career_lineup_seed; DROP TABLE lineup_schema_version;";cmd.ExecuteNonQuery();}
        var hash=SHA256.HashData(File.ReadAllBytes(file));var db=new SqliteLineupStore(file);
        Assert.Null(db.Load());store.Load(career.Id);Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(file)));
        var created=db.InitializeMissing();Assert.Equal(Fingerprint(created),Fingerprint(db.InitializeMissing()));Assert.Equal(0,db.Load()!.Revision);
    }
    [Fact] public void EveryClubCreatesAndReopensItsConfiguredPersistentEleven()
    {
        var p=Profile();var store=EconomicStore();var q=PublishedWorldQueries.Open(EconomicSource);
        foreach(var club in q.GetClubs())
        {
            var offer=new InitialManagerOfferSource(q).Create(p,club.Id);var career=store.Create(Request(p,CareerStartMode.SignOffer,offer));
            var db=new SqliteLineupStore(store.PathFor(career.Id));var state=db.Load()!;
            Assert.Equal(FormationCatalog.Initial(club.Id).Id,state.Formation.Id);Assert.Equal(LineupSelection.SeedFor(career.Id.Value),state.Seed);
            store.Load(career.Id);Assert.Equal(Fingerprint(state),Fingerprint(db.Load()!));
        }
    }
    [Fact] public void SeedVariesTiedChoicesAndHiddenValuesCannotInfluenceSelection()
    {
        var q=PublishedWorldQueries.Open(EconomicSource);var club=q.GetClubs()[0];
        var players=q.GetSquad(club.Id).Select(p=>LineupPlayer.From(q.GetPlayer(club.Id,p.Id))).Select(p=>p with{Visible=p.Visible.ToDictionary(k=>k.Key,k=>10)}).ToArray();
        var f=FormationCatalog.Initial(club.Id);
        var a=LineupSelection.Generate(club.Id,"0000000000000001",f,players);
        var b=LineupSelection.Generate(club.Id,"0000000000000002",f,players);
        Assert.NotEqual(Fingerprint(a),Fingerprint(b));
        var injected=players.Select(p=>p with{Visible=p.Visible.Concat(new[]{new KeyValuePair<string,int>("professionalism",20)}).ToDictionary()}).ToArray();
        Assert.Equal(Fingerprint(a),Fingerprint(LineupSelection.Generate(club.Id,a.Seed,f,injected)));
        foreach(var role in Enum.GetValues<PlayerRole>())
        {
            var list=LineupSelection.Candidates(players,role);Assert.Equal(list.OrderBy(p=>LineupSelection.Compatibility(p,role)).Select(p=>p.Id),list.Select(p=>p.Id));
            Assert.All(list,p=>Assert.InRange(LineupSelection.Compatibility(p,role),0,1));
        }
    }
    [Fact] public void InvalidRosterCannotOverwritePersistentLineup()
    {
        var p=Profile();var store=EconomicStore();var career=store.Create(Request(p,CareerStartMode.SignOffer,EconomicOffer(p)));
        var db=new SqliteLineupStore(store.PathFor(career.Id));var original=db.Load()!;
        var duplicate=original with{Members=original.Members.SkipLast(1).Append(original.Members[0]).ToArray()};
        Assert.Throws<InvalidDataException>(()=>db.Save(duplicate));
        Assert.Equal(Fingerprint(original),Fingerprint(db.Load()!));Assert.Equal(0,db.Load()!.Revision);
    }
    [Fact] public void UnemployedCareerCannotGenerateTeamLineup()
    {
        var p=Profile();var store=EconomicStore();var career=store.Create(Request(p,CareerStartMode.Unemployed));var db=new SqliteLineupStore(store.PathFor(career.Id));
        Assert.Null(db.Load());Assert.Throws<InvalidOperationException>(()=>db.InitializeMissing());
    }
}
