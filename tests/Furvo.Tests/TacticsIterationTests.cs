using System.Security.Cryptography;
using Furvo.Core.Domain;
using Furvo.Core.Lineups;
using Furvo.Core.Tactics;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Xunit;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    [Theory][InlineData(LineupRole.Substitute)][InlineData(LineupRole.Reserve)]
    public void CompleteExchangeSupportsBothDirectionsWithoutLoss(LineupRole role)
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteLineupStore(file);var before=store.Load()!;var squad=store.Players();
        var a=before.Members.First(m=>m.Role==LineupRole.Starter&&squad.Single(p=>p.Id==m.PlayerId).Primary!=PlayerRole.Goalkeeper);
        var b=before.Members.First(m=>m.Role==role&&squad.Single(p=>p.Id==m.PlayerId).Primary!=PlayerRole.Goalkeeper);
        var swapped=store.Save(LineupSelection.Exchange(before,a.PlayerId,b.PlayerId,squad));
        Assert.Equal(a.SlotId,swapped.Members.Single(m=>m.PlayerId==b.PlayerId).SlotId);
        Assert.Equal(role,swapped.Members.Single(m=>m.PlayerId==a.PlayerId).Role);
        Assert.Equal(24,swapped.Members.Select(m=>m.PlayerId).Distinct().Count());Assert.Equal(11,swapped.Members.Count(m=>m.Role==LineupRole.Starter));
        var restored=store.Save(LineupSelection.Exchange(swapped,b.PlayerId,a.PlayerId,squad));Assert.Equal(Fingerprint(before),Fingerprint(restored));
    }
    [Fact] public void InvalidDragTargetsAndCancelledOperationsCannotChangePersistentState()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteLineupStore(file);var before=store.Load()!;var squad=store.Players();
        var reserves=before.Members.Where(m=>m.Role!=LineupRole.Starter).Take(2).ToArray();
        Assert.False(LineupSelection.CanExchange(before,reserves[0].PlayerId,reserves[1].PlayerId,squad));
        Assert.False(LineupSelection.CanExchange(before,before.Members[0].PlayerId,PlayerId.New(),squad));
        Assert.False(LineupSelection.CanExchange(before,before.Members[0].PlayerId,before.Members[0].PlayerId,squad));
        Assert.Equal(Fingerprint(before),Fingerprint(store.Load()!));Assert.Equal(0,store.Load()!.Revision);
    }
    [Fact] public void OutfieldExchangeUpdatesSuitabilityWithoutChangingNaturalPositions()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteLineupStore(file);var original=store.Load()!;var squad=store.Players();
        var before=LineupSelection.Generate(original.ClubId,original.Seed,FormationCatalog.Get("4-4-2"),squad,original.BenchLimit);
        var cb=before.Members.First(m=>m.SlotId is {} id&&before.Formation.Slots.Single(s=>s.Id==id).Role==PlayerRole.CentreBack);
        var striker=before.Members.First(m=>m.SlotId is {} id&&before.Formation.Slots.Single(s=>s.Id==id).Role==PlayerRole.Striker);
        var swapped=LineupSelection.Exchange(before,cb.PlayerId,striker.PlayerId,squad);LineupSelection.Validate(swapped,squad);
        Assert.Equal(striker.PlayerId,swapped.Members.Single(m=>m.SlotId==cb.SlotId).PlayerId);
        Assert.Equal(2,LineupSelection.Compatibility(squad.Single(p=>p.Id==striker.PlayerId),PlayerRole.CentreBack));
    }
    [Fact] public void NewSetPieceDetailsAndFiveShootoutPlacesRoundTripInOrder()
    {
        var (_,file,settings)=TacticalCareer();var store=new SqliteTacticsStore(file);var ids=new SqliteLineupStore(file).Players().Select(p=>p.Id).ToArray();
        var details=SetPieceDetails.Undefined() with{WallSize=3,Lying=true};
        var assignments=details.Assignments.ToDictionary();assignments[SetPieceRole.Wall1]=ids[0];assignments[SetPieceRole.Wall2]=ids[1];assignments[SetPieceRole.Wall3]=ids[2];assignments[SetPieceRole.Prone]=ids[3];assignments[SetPieceRole.Marker1]=ids[4];
        var plans=settings.SetPieces.ToDictionary();plans[SetPieceKind.DefendingFreeKick]=new("man",null,details with{Assignments=assignments});
        var order=ids.Skip(5).Take(5).Select(id=>(PlayerId?)id).ToArray();
        var saved=store.Save(settings with{SetPieces=plans,PenaltyTakers=order});var loaded=new SqliteTacticsStore(file).Load()!;
        Assert.Equal(order,loaded.ShootoutOrder);Assert.Equal(true,loaded.SetPieces[SetPieceKind.DefendingFreeKick].Configuration.Lying);
        Assert.Equal(assignments,loaded.SetPieces[SetPieceKind.DefendingFreeKick].Configuration.Assignments);
        var duplicate=order.ToArray();duplicate[4]=duplicate[0];Assert.Throws<InvalidDataException>(()=>store.Save(saved with{PenaltyTakers=duplicate}));
        Assert.Equal(order,store.Load()!.ShootoutOrder);
    }
    [Fact] public void SimultaneousWallAndProneAssignmentsCannotShareAPlayer()
    {
        var (_,file,settings)=TacticalCareer();var id=new SqliteLineupStore(file).Players()[0].Id;
        var d=SetPieceDetails.Undefined() with{WallSize=2,Lying=true};var roles=d.Assignments.ToDictionary();roles[SetPieceRole.Wall1]=id;roles[SetPieceRole.Prone]=id;
        var plans=settings.SetPieces.ToDictionary();plans[SetPieceKind.DefendingFreeKick]=new("zonal",null,d with{Assignments=roles});
        Assert.Throws<InvalidDataException>(()=>new SqliteTacticsStore(file).Save(settings with{SetPieces=plans}));
        Assert.Null(new SqliteTacticsStore(file).Load());
    }
    [Fact] public void VersionOneReadIsByteIdenticalAndExplicitUpgradePreservesLegacyPenaltyTaker()
    {
        var (_,file,settings)=TacticalCareer();var id=new SqliteLineupStore(file).Players()[0].Id;
        // Se prepara la extensión v1 original, sin pasar por Save de la nueva versión.
        using(var c=Connection(file))
        {
            using var resource=typeof(SqliteTacticsStore).Assembly.GetManifestResourceStream("Furvo.Persistence.Migrations.Tactics001_Initial.sql")!;
            using var reader=new StreamReader(resource);using var cmd=c.CreateCommand();cmd.CommandText=reader.ReadToEnd();cmd.ExecuteNonQuery();
            cmd.CommandText="INSERT INTO career_tactics VALUES($club,1,3,$json,1);";cmd.Parameters.AddWithValue("$club",settings.ClubId.ToString());
            cmd.Parameters.AddWithValue("$json",System.Text.Json.JsonSerializer.Serialize(settings.Instructions));cmd.ExecuteNonQuery();
            foreach(var kind in Enum.GetValues<SetPieceKind>()){cmd.Parameters.Clear();cmd.CommandText="INSERT INTO career_set_pieces VALUES($club,$kind,$routine,$taker);";cmd.Parameters.AddWithValue("$club",settings.ClubId.ToString());cmd.Parameters.AddWithValue("$kind",(int)kind);cmd.Parameters.AddWithValue("$routine",kind==SetPieceKind.Penalty?"direct":DBNull.Value);cmd.Parameters.AddWithValue("$taker",kind==SetPieceKind.Penalty?id.ToString():DBNull.Value);cmd.ExecuteNonQuery();}
        }
        var hash=SHA256.HashData(File.ReadAllBytes(file));var store=new SqliteTacticsStore(file);var loaded=store.Load()!;
        Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(file)));Assert.Equal(id,loaded.SetPieces[SetPieceKind.Penalty].Taker);
        Assert.All(loaded.ShootoutOrder,p=>Assert.Null(p));
        var saved=store.Save(loaded);Assert.Equal(id,store.Load()!.SetPieces[SetPieceKind.Penalty].Taker);
        Assert.Equal(TeamMentality.Balanced,store.Load()!.Mentality);Assert.Equal(2,saved.Revision);
    }
    [Fact] public void AllPreviewsReferenceExistingSymbolsAndDistinctThrowStrategies()
    {
        var (_,file,settings)=TacticalCareer();var lineup=new SqliteLineupStore(file).Load()!;
        foreach(var kind in Enum.GetValues<SetPieceKind>())foreach(var routine in TacticalCatalog.Routines(kind))
        {
            var plans=settings.SetPieces.ToDictionary();plans[kind]=new(routine,null,SetPieceDetails.Undefined() with{CrossTarget="near_post",Movement="combined",Variant="one_two",WallSize=3,Lying=kind==SetPieceKind.DefendingFreeKick});
            var diagram=TacticDiagramBuilder.Build(lineup,lineup.Formation,settings with{SetPieces=plans},3,"set_piece",kind);diagram.Validate();
            Assert.All(diagram.Points.Where(p=>p.Symbol==DiagramSymbol.Player),p=>Assert.True(p.Pending)); // Sin asignaciones automáticas.
        }
        var fingerprints=new HashSet<string>();
        foreach(var routine in new[]{"short","long","flick","space","back","rehearsed"})
        {
            var plans=settings.SetPieces.ToDictionary();plans[SetPieceKind.ThrowIn]=new(routine,null,SetPieceDetails.Undefined() with{Variant="one_two"});
            var diagram=TacticDiagramBuilder.Build(lineup,lineup.Formation,settings with{SetPieces=plans},3,"set_piece",SetPieceKind.ThrowIn);
            fingerprints.Add(string.Join("|",diagram.Arrows.Select(a=>$"{a.From}>{a.To}:{a.Movement}"))+string.Join("|",diagram.Points.Select(p=>$"{p.Id}:{p.Advance}:{p.Width}")));
        }
        Assert.Equal(6,fingerprints.Count);
    }
    [Fact] public void MarkingScenesHaveZonesAndOnlyExplicitlyAssignedMarkersAreHighlighted()
    {
        var (_,file,settings)=TacticalCareer();var lineupStore=new SqliteLineupStore(file);var lineup=lineupStore.Load()!;var id=lineup.Members.First(m=>m.Role==LineupRole.Starter).PlayerId;
        var d=SetPieceDetails.Undefined();var assignments=d.Assignments.ToDictionary();assignments[SetPieceRole.Marker1]=id;
        foreach(var routine in new[]{"man","zonal","mixed"})
        {
            var plans=settings.SetPieces.ToDictionary();plans[SetPieceKind.DefendingCorner]=new(routine,null,d with{Assignments=assignments});
            var scene=TacticDiagramBuilder.Build(lineup,lineup.Formation,settings with{SetPieces=plans},3,"set_piece",SetPieceKind.DefendingCorner);
            Assert.Equal(routine=="zonal"?0:1,scene.Points.Count(p=>p.Marked));
            Assert.Equal(routine!="man",scene.Zones.Count>0);
            Assert.All(scene.Points.Where(p=>p.Id.StartsWith("Zone",StringComparison.Ordinal)),p=>Assert.Contains(scene.Zones,z=>p.Advance>=z.Advance&&p.Advance<=z.Advance+z.Length&&p.Width>=z.Width&&p.Width<=z.Width+z.Breadth));
            Assert.All(scene.Points.Where(p=>p.Marked),p=>{Assert.Equal(id,p.Player);Assert.False(p.Pending);});
        }
    }
}
