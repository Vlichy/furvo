using System.Security.Cryptography;
using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Furvo.Core.Lineups;
using Furvo.Core.Tactics;
using Furvo.Persistence;
using Microsoft.Data.Sqlite;
using Xunit;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    private (SqliteCareerStore Careers,string File,TacticalSettings Undefined) TacticalCareer()
    {
        var p=Profile();var careers=EconomicStore();var career=careers.Create(Request(p,CareerStartMode.SignOffer,EconomicOffer(p)));
        return(careers,careers.PathFor(career.Id),TacticalSettings.Undefined(career.ClubId!.Value));
    }
    [Fact] public void TacticsReadOnExistingCareerDoesNotMigrateOrWrite()
    {
        var (_,file,_)=TacticalCareer();var hash=SHA256.HashData(File.ReadAllBytes(file));
        Assert.Null(new SqliteTacticsStore(file).Load());Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(file)));
        using var c=Connection(file);using var cmd=c.CreateCommand();cmd.CommandText="SELECT count(*) FROM sqlite_master WHERE name='career_tactics';";Assert.Equal(0L,cmd.ExecuteScalar());
    }
    [Fact] public void EveryInstructionOptionAndMentalityRoundTripsWithoutChangingOtherFields()
    {
        var (_,file,settings)=TacticalCareer();var store=new SqliteTacticsStore(file);
        foreach(var mentality in Enum.GetValues<TeamMentality>())
        {
            settings=store.Save(settings with{Mentality=mentality});var restored=new SqliteTacticsStore(file).Load()!;
            Assert.Equal(mentality,restored.Mentality);Assert.All(restored.Instructions.Values,v=>Assert.Null(v));
        }
        foreach(var definition in TacticalCatalog.Instructions)
        {
            foreach(var option in definition.Options)
            {
                var next=settings.Instructions.ToDictionary();next[definition.Code]=option;
                settings=store.Save(settings with{Instructions=next});Assert.Equal(option,store.Load()!.Instructions[definition.Code]);
                Assert.Equal(TeamMentality.VeryAttacking,store.Load()!.Mentality);
            }
        }
        var reset=settings.Instructions.ToDictionary();reset["passing"]=null;
        settings=store.Save(settings with{Mentality=null,Instructions=reset});Assert.Null(store.Load()!.Mentality);Assert.Null(store.Load()!.Instructions["passing"]);
    }
    [Fact] public void AllSevenSetPieceKindsPersistRoutinesAndOnlyOwnClubTakers()
    {
        var (_,file,settings)=TacticalCareer();var store=new SqliteTacticsStore(file);var players=new SqliteLineupStore(file).Players();
        foreach(var kind in Enum.GetValues<SetPieceKind>())
            foreach(var routine in TacticalCatalog.Routines(kind))
            {
                var next=settings.SetPieces.ToDictionary();next[kind]=new(routine,TacticalCatalog.HasTaker(kind)?players[0].Id:null);
                settings=store.Save(settings with{SetPieces=next});Assert.Equal(next[kind],new SqliteTacticsStore(file).Load()!.SetPieces[kind]);
            }
        var invalid=settings.SetPieces.ToDictionary();invalid[SetPieceKind.Penalty]=new("direct",PlayerId.New());
        Assert.Throws<InvalidDataException>(()=>store.Save(settings with{SetPieces=invalid}));
        invalid=settings.SetPieces.ToDictionary();invalid[SetPieceKind.DefendingCorner]=new("zonal",players[0].Id);
        Assert.Throws<InvalidDataException>(()=>store.Save(settings with{SetPieces=invalid}));
    }
    [Fact] public void TacticsAreIndependentAndDoNotChangeLineupOrFoundationalData()
    {
        var sourceHash=SHA256.HashData(File.ReadAllBytes(EconomicSource));var (_,file,a)=TacticalCareer();var (_,second,b)=TacticalCareer();
        var lineupBefore=Fingerprint(new SqliteLineupStore(file).Load()!);
        new SqliteTacticsStore(file).Save(a with{Mentality=TeamMentality.Attacking});
        new SqliteTacticsStore(second).Save(b with{Mentality=TeamMentality.Defensive});
        Assert.Equal(TeamMentality.Attacking,new SqliteTacticsStore(file).Load()!.Mentality);
        Assert.Equal(TeamMentality.Defensive,new SqliteTacticsStore(second).Load()!.Mentality);
        Assert.Equal(lineupBefore,Fingerprint(new SqliteLineupStore(file).Load()!));Assert.Equal(sourceHash,SHA256.HashData(File.ReadAllBytes(EconomicSource)));
    }
    [Fact] public void InvalidAndStaleTacticalWritesPreserveTheLastSavedConfiguration()
    {
        var (_,file,settings)=TacticalCareer();var store=new SqliteTacticsStore(file);var saved=store.Save(settings);
        Assert.Throws<InvalidOperationException>(()=>store.Save(settings));
        Assert.Throws<InvalidDataException>(()=>store.Save(saved with{Mentality=(TeamMentality)99}));
        var bad=saved.Instructions.ToDictionary();bad["passing"]="teleport";
        Assert.Throws<InvalidDataException>(()=>store.Save(saved with{Instructions=bad}));
        Assert.Throws<InvalidOperationException>(()=>store.Save(saved with{ClubId=ClubId.New()}));
        Assert.Equal(saved.Revision,store.Load()!.Revision);Assert.Null(store.Load()!.Mentality);
    }
    [Fact] public void FailedTacticalTransactionRollsBackHeaderAndAssignments()
    {
        var (_,file,settings)=TacticalCareer();var store=new SqliteTacticsStore(file);var saved=store.Save(settings);
        using(var c=Connection(file))using(var cmd=c.CreateCommand()){
            cmd.CommandText="CREATE TRIGGER forced_tactics_failure BEFORE INSERT ON career_set_pieces BEGIN SELECT RAISE(ABORT,'test rollback'); END;";cmd.ExecuteNonQuery();}
        Assert.Throws<SqliteException>(()=>store.Save(saved with{Mentality=TeamMentality.Attacking}));
        var loaded=store.Load()!;Assert.Equal(saved.Revision,loaded.Revision);Assert.Null(loaded.Mentality);Assert.Equal(7,loaded.SetPieces.Count);
    }
    [Fact] public void FreeCoachAndInitialWorldCannotReceiveTactics()
    {
        var p=Profile();var careers=EconomicStore();var free=careers.Create(Request(p,CareerStartMode.Unemployed));
        var store=new SqliteTacticsStore(careers.PathFor(free.Id));Assert.Null(store.Load());
        Assert.Throws<InvalidOperationException>(()=>store.Save(TacticalSettings.Undefined(EconomicOffer(p).ClubId)));
        Assert.Throws<InvalidOperationException>(()=>new SqliteTacticsStore(EconomicSource).Load());
    }
    [Fact] public void SuitabilityAndStarterSwapUseOnlyRegisteredPositions()
    {
        var id=PlayerId.New();var player=new LineupPlayer(id,"Prueba",PlayerRole.CentreBack,new[]{PlayerRole.RightBack},new Dictionary<string,int>());
        Assert.Equal(0,LineupSelection.Compatibility(player,PlayerRole.CentreBack));
        Assert.Equal(1,LineupSelection.Compatibility(player,PlayerRole.RightBack));
        Assert.Equal(2,LineupSelection.Compatibility(player,PlayerRole.Striker));
        Assert.Equal(99,LineupSelection.Compatibility(player,PlayerRole.Goalkeeper));Assert.Single(player.Secondary);
        var (_,file,_)=TacticalCareer();var store=new SqliteLineupStore(file);var original=store.Load()!;var squad=store.Players();
        var before=store.Save(LineupSelection.Generate(original.ClubId,original.Seed,FormationCatalog.Get("4-4-2"),squad,original.BenchLimit,original));
        var slots=before.Formation.Slots.Where(s=>s.Role==PlayerRole.CentreBack).ToArray();
        var first=before.Members.Single(m=>m.SlotId==slots[0].Id);var second=before.Members.Single(m=>m.SlotId==slots[1].Id);
        var swapped=store.Save(LineupSelection.Replace(before,slots[0].Id,second.PlayerId,squad));
        Assert.Equal(second.PlayerId,swapped.Members.Single(m=>m.SlotId==slots[0].Id).PlayerId);
        Assert.Equal(first.PlayerId,swapped.Members.Single(m=>m.SlotId==slots[1].Id).PlayerId);
        Assert.Equal(11,swapped.Members.Count(m=>m.Role==LineupRole.Starter));Assert.Equal(Fingerprint(swapped),Fingerprint(store.Load()!));
    }
    [Fact] public void TacticalCatalogHasExactlyApprovedGroupsAndNoInventedDefaults()
    {
        Assert.Equal(21,TacticalCatalog.Instructions.Count);Assert.Equal(6,TacticalCatalog.Instructions.Count(d=>d.Group=="possession"));
        Assert.Equal(10,TacticalCatalog.Instructions.Count(d=>d.Group=="defending"));Assert.Equal(5,TacticalCatalog.Instructions.Count(d=>d.Group=="transition"));
        var s=TacticalSettings.Undefined(ClubId.New());Assert.Null(s.Mentality);Assert.All(s.Instructions.Values,v=>Assert.Null(v));
        Assert.All(s.SetPieces.Values,p=>{Assert.Null(p.Routine);Assert.Null(p.Taker);});
        TacticalCatalog.Validate(s,new HashSet<PlayerId>());
    }
}
