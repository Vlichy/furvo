using Furvo.Core.Domain;
using Furvo.Core.Lineups;
using Furvo.Core.Tactics;
using Furvo.Persistence;
using Xunit;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    [Theory]
    [InlineData(PlayerRole.CentralMidfielder,.7,.46,PlayerRole.AttackingMidfielder)]
    [InlineData(PlayerRole.RightBack,.72,.9,PlayerRole.RightWinger)]
    [InlineData(PlayerRole.CentralMidfielder,.4,.63,PlayerRole.DefensiveMidfielder)]
    [InlineData(PlayerRole.LeftBack,.72,.1,PlayerRole.LeftWinger)]
    public void DropAssignsRoleAndReopensOnlyActiveDisposition(PlayerRole previous,double x,double y,PlayerRole expected)
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteTacticalPlanStore(file);var before=store.Load()!;var p=before.Get(TacticalPlanKind.Initial);
        p=TacticalPlanRules.SetRole(p,PossessionPhase.WithoutBall,"s1",previous);var edited=TacticalPlanRules.MoveAndAssign(p,PossessionPhase.WithoutBall,"s1",x,y);
        var playersBefore=new SqliteLineupStore(file).Players().Select(a=>(a.Id,a.Primary,string.Join(",",a.Secondary))).ToArray();
        var saved=store.Save(before.Replace(edited));var reopened=new SqliteTacticalPlanStore(file).Load()!;var slot=reopened.Get(TacticalPlanKind.Initial).WithoutBall.Positions.Single(a=>a.Id=="s1");
        Assert.Equal(expected,slot.Role);Assert.Equal(x,slot.X);Assert.Equal(y,slot.Y);Assert.Null(slot.TacticalCode);Assert.Equal(before.Revision+1,reopened.Revision);
        Assert.Equal(before.Get(TacticalPlanKind.Initial).WithBall.Positions,reopened.Get(TacticalPlanKind.Initial).WithBall.Positions);
        foreach(var kind in new[]{TacticalPlanKind.Winning,TacticalPlanKind.Losing})Assert.Equal(PlanFingerprint(before.Get(kind)),PlanFingerprint(reopened.Get(kind)));
        Assert.Equal(playersBefore,new SqliteLineupStore(file).Players().Select(a=>(a.Id,a.Primary,string.Join(",",a.Secondary))));
    }
    [Fact] public void AutomaticDropCrossingBoundaryAppliesRoleEvenForSmallDisplacement()
    {
        var (_,file,_)=TacticalCareer();var book=new SqliteTacticalPlanStore(file).Load()!;var p=book.Get(TacticalPlanKind.Initial);
        p=TacticalPlanRules.Move(p,PossessionPhase.WithoutBall,"s1",.61,.46);p=TacticalPlanRules.SetRole(p,PossessionPhase.WithoutBall,"s1",PlayerRole.CentralMidfielder);
        p=TacticalPlanRules.MoveAndAssign(p,PossessionPhase.WithoutBall,"s1",.63,.46);Assert.Equal(PlayerRole.AttackingMidfielder,p.WithoutBall.Positions.Single(s=>s.Id=="s1").Role);
        var goalkeeper=p.WithoutBall.Positions.Single(s=>s.Role==PlayerRole.Goalkeeper);var moved=TacticalPlanRules.MoveAndAssign(p,PossessionPhase.WithoutBall,goalkeeper.Id,.96,.15);Assert.Equal(PlayerRole.Goalkeeper,moved.WithoutBall.Positions.Single(s=>s.Id==goalkeeper.Id).Role);
    }
    [Fact] public void SuitabilityUsesNaturalAndSecondaryRolesWithoutChangingThePlayer()
    {
        var natural=new LineupPlayer(PlayerId.New(),"Prueba",PlayerRole.CentralMidfielder,new[]{PlayerRole.AttackingMidfielder},new Dictionary<string,int>());
        Assert.Equal(0,LineupSelection.Compatibility(natural,PlayerRole.CentralMidfielder));Assert.Equal(1,LineupSelection.Compatibility(natural,PlayerRole.AttackingMidfielder));Assert.Equal(2,LineupSelection.Compatibility(natural,PlayerRole.LeftWinger));
        Assert.Equal(PlayerRole.CentralMidfielder,natural.Primary);Assert.Equal(new[]{PlayerRole.AttackingMidfielder},natural.Secondary);
    }
}
