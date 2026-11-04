using Furvo.Core.Development;
using Furvo.Core.Tactics;
using Furvo.Persistence;
using System.Text.Json;
using Xunit;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    [Theory]
    [InlineData("corner_practice",2)]
    [InlineData("free_kick_practice",3)]
    [InlineData("penalty_practice",1)]
    [InlineData("throw_practice",1)]
    [InlineData("technical",0)]
    public void RoutineChoicesContainOnlyKindsCompatibleWithTheExercise(string exercise,int count)
    {var kinds=TrainingRoutines.Compatible(exercise);Assert.Equal(count,kinds.Count);if(exercise=="free_kick_practice")Assert.All(kinds,k=>Assert.True(TrainingRoutines.Located(k)));if(exercise=="corner_practice")Assert.DoesNotContain(SetPieceKind.Penalty,kinds);}
    [Theory]
    [InlineData(SetPieceKind.DirectFreeKick,FreeKickLocation.Left)]
    [InlineData(SetPieceKind.IndirectFreeKick,FreeKickLocation.Distant)]
    [InlineData(SetPieceKind.DefendingFreeKick,FreeKickLocation.Right)]
    public void LocatedFreeKickCanBeSavedAndRecoveredWithoutModifyingTheTactic(SetPieceKind kind,FreeKickLocation location)
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var tactics=new SqliteTacticalPlanStore(file);var book=tactics.Load()!;var settings=tactics.Settings(book,TacticalPlanKind.Initial);var locations=new Dictionary<string,SetPiecePlan>{{TacticalSettings.FreeKickKey(kind,location),new(kind==SetPieceKind.DefendingFreeKick?"zonal":"short",null)}};tactics.SaveSettings(TacticalPlanKind.Initial,settings with{FreeKicks=locations});
        var store=new SqliteSportsStore(file);var before=JsonSerializer.Serialize(store.TrainingTactics(team));var reference=new RoutineReference(kind,location);Assert.True(TrainingRoutines.Designed(store.TrainingTactics(team),reference));var session=Session(team,squads.Players(team),"free_kick_practice") with{Routine=reference};store.SavePlan(store.Plan(team) with{Sessions=new[]{session}});var saved=Assert.Single(new SqliteSportsStore(file).Plan(team).Sessions);Assert.Equal(reference,saved.Routine);Assert.Equal(before,JsonSerializer.Serialize(store.TrainingTactics(team)));Assert.Null(settings.SetPieces[kind].Routine);
    }
    [Fact] public void UndefinedFreeKickLocationIsRejectedBeforeAnyPlanWrite()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var store=new SqliteSportsStore(file);var reference=new RoutineReference(SetPieceKind.DirectFreeKick,FreeKickLocation.Right);Assert.False(TrainingRoutines.Designed(store.TrainingTactics(team),reference));var session=Session(team,squads.Players(team),"free_kick_practice") with{Routine=reference};Assert.Throws<InvalidDataException>(()=>store.SavePlan(store.Plan(team) with{Sessions=new[]{session}}));Assert.Empty(store.Plan(team).Sessions);
    }
    [Fact] public void LegacyUnlocatedCornerAndFreeKickReferencesRemainUnlocatedOnReopen()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var tactics=new SqliteTacticalPlanStore(file);var settings=tactics.Settings(tactics.Load()!,TacticalPlanKind.Initial);var pieces=settings.SetPieces.ToDictionary();pieces[SetPieceKind.AttackingCorner]=new("short",null);pieces[SetPieceKind.DirectFreeKick]=new("short",null);tactics.SaveSettings(TacticalPlanKind.Initial,settings with{SetPieces=pieces});var store=new SqliteSportsStore(file);
        var corner=Session(team,squads.Players(team),"corner_practice") with{Routine=new(SetPieceKind.AttackingCorner)};var free=Session(team,squads.Players(team),"free_kick_practice",hour:11) with{Routine=new(SetPieceKind.DirectFreeKick)};store.SavePlan(store.Plan(team) with{Sessions=new[]{corner,free}});var reopened=new SqliteSportsStore(file).Plan(team);Assert.All(reopened.Sessions,s=>Assert.Null(s.Routine!.Location));Assert.Equal(corner.Routine,reopened.Sessions.Single(s=>s.Id==corner.Id).Routine);Assert.Equal(free.Routine,reopened.Sessions.Single(s=>s.Id==free.Id).Routine);
    }
    [Fact] public void IncompatibleRoutineAndInvalidLocationAreRejectedBySharedRules()
    {var settings=TacticalSettings.Undefined(new Furvo.Core.Domain.ClubId(Guid.NewGuid()));Assert.Throws<InvalidDataException>(()=>TrainingRoutines.Validate("corner_practice",settings,new(SetPieceKind.Penalty)));Assert.Throws<InvalidDataException>(()=>TrainingRoutines.Resolve(settings,new(SetPieceKind.AttackingCorner,FreeKickLocation.Left)));Assert.Throws<InvalidDataException>(()=>TrainingRoutines.Resolve(settings,new(SetPieceKind.DirectFreeKick,(FreeKickLocation)99)));}
}
