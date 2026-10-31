using System.Security.Cryptography;
using Furvo.Core.Domain;
using Furvo.Core.Lineups;
using Furvo.Core.Tactics;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Microsoft.Data.Sqlite;
using Xunit;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    [Fact] public void MovementModesDifferAndExchangeHasReciprocalAnchoredArrows()
    {
        var (_,file,s)=TacticalCareer();var l=new SqliteLineupStore(file).Load()!;
        TacticDiagram Scene(string v){var i=s.Instructions.ToDictionary();i["movement"]=v;return TacticDiagramBuilder.Build(l,l.Formation,s with{Instructions=i},1,"movement",SetPieceKind.AttackingCorner,true);}
        var hold=Scene("hold");var exchange=Scene("exchange");var mixed=Scene("mixed");
        Assert.Empty(hold.Arrows);Assert.Equal(11,hold.Zones.Count);Assert.Equal(2,exchange.Arrows.Count);Assert.Empty(exchange.Zones);
        Assert.Equal(exchange.Arrows[0].From,exchange.Arrows[1].To);Assert.Equal(exchange.Arrows[0].To,exchange.Arrows[1].From);
        Assert.Equal(2,mixed.Arrows.Count);Assert.Equal(9,mixed.Zones.Count);
        Assert.All(exchange.Points,p=>{var slot=l.Formation.Slots.Single(x=>x.Id==p.Id);Assert.Equal(slot.X,p.Advance);Assert.Equal(slot.Y,p.Width);});
    }
    [Fact] public void MarkingModesHaveZonesOrExplicitOpponentConnections()
    {
        var (_,file,s)=TacticalCareer();var l=new SqliteLineupStore(file).Load()!;
        foreach(var mode in new[]{"zonal","mixed","man"}){var i=s.Instructions.ToDictionary();i["marking"]=mode;var scene=TacticDiagramBuilder.Build(l,l.Formation,s with{Instructions=i},1,"marking",SetPieceKind.AttackingCorner,true);
            Assert.Equal(mode=="zonal"?4:mode=="mixed"?2:0,scene.Zones.Count);Assert.Equal(mode=="zonal"?0:mode=="mixed"?2:4,scene.Arrows.Count);
            Assert.All(scene.Arrows,a=>Assert.Equal(DiagramSymbol.Opponent,scene.Points.Single(p=>p.Id==a.To).Symbol));}
    }
    [Fact] public void CounterRoutesAndRecoveryModesHaveDistinctGraphs()
    {
        var (_,file,s)=TacticalCareer();var l=new SqliteLineupStore(file).Load()!;var graphs=new HashSet<string>();
        foreach(var route in new[]{"right","left","both","centre","long"}){var i=s.Instructions.ToDictionary();i["recovery"]="counter";i["counter_route"]=route;var scene=TacticDiagramBuilder.Build(l,l.Formation,s with{Instructions=i},2,"counter_route",SetPieceKind.AttackingCorner,true);scene.Validate();Assert.NotEmpty(scene.Arrows);graphs.Add(string.Join(";",scene.Arrows));if(route=="both")Assert.Equal(2,scene.Arrows.Count);if(route=="long")Assert.All(scene.Arrows,a=>Assert.False(a.Short));}
        Assert.Equal(5,graphs.Count);
        var keep=s.Instructions.ToDictionary();keep["recovery"]="keep";var possession=TacticDiagramBuilder.Build(l,l.Formation,s with{Instructions=keep},2,"recovery",SetPieceKind.AttackingCorner,true);Assert.Equal(3,possession.Arrows.Count);Assert.All(possession.Arrows,a=>Assert.True(a.Short));
        keep["recovery"]="balance";var balance=TacticDiagramBuilder.Build(l,l.Formation,s with{Instructions=keep},2,"recovery",SetPieceKind.AttackingCorner,true);Assert.Empty(balance.Arrows);Assert.NotEmpty(balance.Zones);
    }
    [Fact] public void KeeperMixedHasTwoShortAndTwoLongRealReceiversAndFiltersPreferences()
    {
        var (_,file,s)=TacticalCareer();var l=new SqliteLineupStore(file).Load()!;var i=s.Instructions.ToDictionary();i["keeper"]="mixed";
        var scene=TacticDiagramBuilder.Build(l,l.Formation,s with{Instructions=i},2,"keeper",SetPieceKind.AttackingCorner,true);
        Assert.Equal(2,scene.Arrows.Count(a=>a.Short==true));Assert.Equal(2,scene.Arrows.Count(a=>a.Short==false));
        Assert.All(scene.Arrows,a=>{Assert.Equal(PlayerRole.Goalkeeper,l.Formation.Slots.Single(p=>p.Id==a.From).Role);Assert.NotNull(scene.Points.Single(p=>p.Id==a.To).Player);});
        i["keeper_target"]="defenders";scene=TacticDiagramBuilder.Build(l,l.Formation,s with{Instructions=i},2,"keeper_target",SetPieceKind.AttackingCorner,true);Assert.All(scene.Arrows,a=>{Assert.True(scene.Points.Single(p=>p.Id==a.To).Advance<.45);Assert.True(a.Highlight);});
    }
    [Fact] public void LocationsHaveDifferentOriginsAndLocationSpecificRoutines()
    {
        var (_,file,s)=TacticalCareer();var l=new SqliteLineupStore(file).Load()!;var origins=new HashSet<string>();
        foreach(var location in Enum.GetValues<FreeKickLocation>()){var p=s.SetPieces.ToDictionary();p[SetPieceKind.DirectFreeKick]=new("short",null);var scene=TacticDiagramBuilder.Build(l,l.Formation,s with{SetPieces=p},3,"set_piece",SetPieceKind.DirectFreeKick,true,location);scene.Validate();var taker=scene.Points.Single(x=>x.Id=="taker");origins.Add(taker.Advance+":"+taker.Width);Assert.Contains(scene.Points,x=>x.Id=="ball");Assert.True(scene.Arrows.Single().Short);}
        Assert.Equal(4,origins.Count);Assert.DoesNotContain("direct",TacticalCatalog.FreeKickRoutines(SetPieceKind.DirectFreeKick,FreeKickLocation.Left));Assert.DoesNotContain("cross",TacticalCatalog.FreeKickRoutines(SetPieceKind.DirectFreeKick,FreeKickLocation.Edge));Assert.Contains("area",TacticalCatalog.FreeKickRoutines(SetPieceKind.DirectFreeKick,FreeKickLocation.Distant));
    }
    [Fact] public void IndependentFreeKicksRoundTripWithoutChangingLegacyOrFormations()
    {
        var (_,file,s)=TacticalCareer();var store=new SqliteTacticalPlanStore(file);var book=store.Load()!;var shared=new Dictionary<string,SetPiecePlan>();var ids=new SqliteLineupStore(file).Players().Select(p=>p.Id).ToArray();
        foreach(var kind in new[]{SetPieceKind.DirectFreeKick,SetPieceKind.DefendingFreeKick})foreach(var location in Enum.GetValues<FreeKickLocation>()){var a=SetPieceDetails.Undefined().Assignments.ToDictionary();a[SetPieceRole.Wall1]=ids[0];a[SetPieceRole.Wall2]=ids[1];a[SetPieceRole.Prone]=ids[2];var defensive=kind==SetPieceKind.DefendingFreeKick;shared[TacticalSettings.FreeKickKey(kind,location)]=new(defensive?"mixed":"short",defensive?null:ids[3],SetPieceDetails.Undefined() with{WallSize=defensive?2:null,Lying=defensive&&location==FreeKickLocation.Edge,Assignments=a});}
        var instructions=s.Instructions.ToDictionary();instructions["counter_route"]="left";instructions["recovery"]="counter";
        var saved=store.SaveSettings(TacticalPlanKind.Winning,s with{Revision=book.Revision,Instructions=instructions,FreeKicks=shared});var reopened=new SqliteTacticalPlanStore(file);var after=reopened.Load()!;var loaded=reopened.Settings(after,TacticalPlanKind.Losing);Assert.Equal(8,loaded.FreeKicks!.Count);
        foreach(var (key,plan) in shared){Assert.Equal(plan.Routine,loaded.FreeKicks[key].Routine);Assert.Equal(plan.Configuration.Assignments,loaded.FreeKicks[key].Configuration.Assignments);Assert.Equal(plan.Configuration.Lying,loaded.FreeKicks[key].Configuration.Lying);}
        Assert.Equal("left",reopened.Settings(after,TacticalPlanKind.Winning).Instructions["counter_route"]);Assert.Null(loaded.Instructions["counter_route"]);
        foreach(var kind in Enum.GetValues<TacticalPlanKind>()){Assert.Equal(book.Get(kind).WithBall.Positions,after.Get(kind).WithBall.Positions);Assert.Equal(book.Get(kind).WithoutBall.Positions,after.Get(kind).WithoutBall.Positions);}
        Assert.All(loaded.SetPieces.Values,p=>Assert.Null(p.Routine));
    }
    [Fact] public void FreeKickDuplicateAndInvalidLocationAreRejectedWithoutWrites()
    {
        var (_,file,s)=TacticalCareer();var store=new SqliteTacticsStore(file);var id=new SqliteLineupStore(file).Players()[0].Id;var d=SetPieceDetails.Undefined();var a=d.Assignments.ToDictionary();a[SetPieceRole.Wall1]=id;a[SetPieceRole.Prone]=id;
        var free=new Dictionary<string,SetPiecePlan>{{"5:4",new("man",null,d with{WallSize=2,Lying=true,Assignments=a})}};var hash=SHA256.HashData(File.ReadAllBytes(file));Assert.Throws<InvalidDataException>(()=>store.Save(s with{FreeKicks=free}));Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(file)));
        free=new(){{"3:1",new("direct",null)}};Assert.Throws<InvalidDataException>(()=>store.Save(s with{FreeKicks=free}));
        free=new(){{"5:1",new("man",null,d with{WallSize=2,Lying=true})}};Assert.Throws<InvalidDataException>(()=>store.Save(s with{FreeKicks=free}));
    }
    [Fact] public void DefensiveLocationsKeepProneBehindWallAndAssignmentsConnected()
    {
        var (_,file,s)=TacticalCareer();var l=new SqliteLineupStore(file).Load()!;var pieces=s.SetPieces.ToDictionary();pieces[SetPieceKind.DefendingFreeKick]=new("mixed",null,SetPieceDetails.Undefined() with{WallSize=3,Lying=true});
        foreach(var location in new[]{FreeKickLocation.Edge,FreeKickLocation.Distant}){var scene=TacticDiagramBuilder.Build(l,l.Formation,s with{SetPieces=pieces},3,"set_piece",SetPieceKind.DefendingFreeKick,true,location);var wall=scene.Points.Where(p=>p.Id.StartsWith("Wall")).ToArray();Assert.Equal(3,wall.Length);Assert.True(scene.Points.Single(p=>p.Lying).Advance<wall[0].Advance);Assert.Equal(2,scene.Arrows.Count);Assert.Contains(scene.Points,p=>p.Id=="NearPost");Assert.Contains(scene.Points,p=>p.Id=="Rebound");}
    }
    [Fact] public void VersionTwoCareerReadIsUnchangedAndExplicitSaveMigratesWithoutLosingLegacyRoutine()
    {
        var (_,file,s)=TacticalCareer();var store=new SqliteTacticsStore(file);var pieces=s.SetPieces.ToDictionary();pieces[SetPieceKind.DirectFreeKick]=new("direct",null);store.Save(s with{SetPieces=pieces});
        using(var c=new SqliteConnection("Data Source="+file+";Pooling=False")){c.Open();using var cmd=c.CreateCommand();cmd.CommandText="DROP TABLE career_tactical_personnel; DROP TABLE career_tactical_familiarity; DROP TABLE career_opponent_orders; DROP TABLE career_free_kicks; UPDATE tactics_schema_version SET version=2; UPDATE career_tactics SET instructions_json=json_remove(instructions_json,'$.counter_route');";cmd.ExecuteNonQuery();}
        var hash=SHA256.HashData(File.ReadAllBytes(file));var read=store.Load()!;Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(file)));Assert.Equal("direct",read.FreeKick(SetPieceKind.DirectFreeKick,FreeKickLocation.Edge).Routine);Assert.Null(read.FreeKick(SetPieceKind.DirectFreeKick,FreeKickLocation.Left).Routine);
        store.Save(read with{FreeKicks=new Dictionary<string,SetPiecePlan>{{"3:1",new("short",null)}}});var loaded=store.Load()!;Assert.Equal("direct",loaded.FreeKick(SetPieceKind.DirectFreeKick,FreeKickLocation.Edge).Routine);Assert.Equal("short",loaded.FreeKick(SetPieceKind.DirectFreeKick,FreeKickLocation.Left).Routine);
        using var connection=new SqliteConnection("Data Source="+file+";Pooling=False");connection.Open();using var version=connection.CreateCommand();version.CommandText="SELECT version FROM tactics_schema_version";Assert.Equal(4L,version.ExecuteScalar());
    }
    [Fact] public void LegacyInstructionsAndRolesUpgradeOnlyInMemoryOnRead()
    {
        var (_,file,s)=TacticalCareer();var store=new SqliteTacticsStore(file);store.Save(s);using(var c=new SqliteConnection("Data Source="+file+";Pooling=False")){c.Open();using var cmd=c.CreateCommand();cmd.CommandText="UPDATE career_tactics SET instructions_json=json_remove(instructions_json,'$.counter_route'); UPDATE career_set_pieces SET details_json=NULL;";cmd.ExecuteNonQuery();}
        var hash=SHA256.HashData(File.ReadAllBytes(file));var loaded=store.Load()!;Assert.True(loaded.Instructions.ContainsKey("counter_route"));Assert.Null(loaded.Instructions["counter_route"]);Assert.All(loaded.SetPieces.Values,p=>Assert.Equal(Enum.GetValues<SetPieceRole>().Length,p.Configuration.Assignments.Count));Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(file)));
    }
}
