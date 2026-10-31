using System.Security.Cryptography;
using Furvo.Core.Domain;
using Furvo.Core.Lineups;
using Furvo.Core.Tactics;
using Furvo.Persistence;
using Xunit;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    private static string PlanFingerprint(TacticalPlan plan)=>System.Text.Json.JsonSerializer.Serialize(plan);
    [Fact] public void ThreePlansAndSixPhasesProjectWithoutWritingThenMigrateExactly()
    {
        var (_,file,_)=TacticalCareer();var lineup=new SqliteLineupStore(file).Load()!;var legacy=new SqliteTacticsStore(file);
        var original=legacy.Save(TacticalSettings.Undefined(lineup.ClubId) with{Mentality=TeamMentality.Attacking});
        var hash=SHA256.HashData(File.ReadAllBytes(file));var store=new SqliteTacticalPlanStore(file);var book=store.Load()!;
        Assert.False(store.IsPersisted);Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(file)));Assert.Equal(3,book.Plans.Count);
        foreach(var p in book.Plans)foreach(var phase in Enum.GetValues<PossessionPhase>()){Assert.Equal(Fingerprint(lineup),Fingerprint(p.View(book.ClubId,book.Revision,phase)));Assert.Equal(original.Mentality,p.Mentality);}
        var saved=store.Save(book);Assert.True(store.IsPersisted);Assert.Equal(6,saved.Plans.Sum(p=>new[]{p.WithBall,p.WithoutBall}.Length));
        Assert.Equal(Fingerprint(lineup),Fingerprint(new SqliteLineupStore(file).Load()!));
        Assert.Equal(original.Mentality,new SqliteTacticsStore(file).Load()!.Mentality);
        using var c=Connection(file);using var cmd=c.CreateCommand();cmd.CommandText="SELECT count(*) FROM career_lineup_members;";Assert.Equal(0L,cmd.ExecuteScalar());
    }
    [Fact] public void FreeMovementAndRoleAreIndependentAndPreserveOtherFivePhases()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteTacticalPlanStore(file);var book=store.Load()!;var plan=book.Get(TacticalPlanKind.Initial);
        var slot=plan.WithBall.Positions.First(s=>s.Role!=PlayerRole.Goalkeeper);var other=PlanFingerprint(book.Get(TacticalPlanKind.Winning));var without=System.Text.Json.JsonSerializer.Serialize(plan.WithoutBall);
        var moved=TacticalPlanRules.Move(plan,PossessionPhase.WithBall,slot.Id,.9,.27);Assert.Equal(slot.Role,moved.WithBall.Positions.Single(s=>s.Id==slot.Id).Role);
        moved=TacticalPlanRules.SetRole(moved,PossessionPhase.WithBall,slot.Id,PlayerRole.RightWinger);
        var saved=store.Save(book.Replace(moved));var loaded=new SqliteTacticalPlanStore(file).Load()!;
        Assert.Equal(other,PlanFingerprint(loaded.Get(TacticalPlanKind.Winning)));Assert.Equal(without,System.Text.Json.JsonSerializer.Serialize(loaded.Get(TacticalPlanKind.Initial).WithoutBall));
        Assert.Equal(.9,loaded.Get(TacticalPlanKind.Initial).WithBall.Positions.Single(s=>s.Id==slot.Id).X);
        Assert.Equal(PlayerRole.RightWinger,loaded.Get(TacticalPlanKind.Initial).WithBall.Positions.Single(s=>s.Id==slot.Id).Role);
        Assert.Equal(plan.Roster.Members.Select(m=>m.PlayerId),saved.Get(TacticalPlanKind.Initial).Roster.Members.Select(m=>m.PlayerId));
    }
    [Fact] public void ExchangesUpdateBothPhasesOfOnePlanAndNoOtherPlan()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteTacticalPlanStore(file);var book=store.Load()!;var initial=book.Get(TacticalPlanKind.Initial);
        var before=PlanFingerprint(book.Get(TacticalPlanKind.Losing));var view=initial.View(book.ClubId,book.Revision,PossessionPhase.WithBall);var players=new SqliteLineupStore(file).Players();
        var a=view.Members.First(m=>m.Role==LineupRole.Starter&&players.Single(p=>p.Id==m.PlayerId).Primary!=PlayerRole.Goalkeeper);
        var b=view.Members.First(m=>m.Role==LineupRole.Substitute&&players.Single(p=>p.Id==m.PlayerId).Primary!=PlayerRole.Goalkeeper);
        var editor=new SqlitePlanLineupStore(file,TacticalPlanKind.Initial,PossessionPhase.WithBall);editor.Save(LineupSelection.Exchange(view,a.PlayerId,b.PlayerId,players));
        var loaded=store.Load()!;foreach(var phase in Enum.GetValues<PossessionPhase>())
            Assert.Equal(b.PlayerId,loaded.Get(TacticalPlanKind.Initial).View(loaded.ClubId,loaded.Revision,phase).Members.Single(m=>m.SlotId==a.SlotId).PlayerId);
        Assert.Equal(before,PlanFingerprint(loaded.Get(TacticalPlanKind.Losing)));
        var current=loaded.Get(TacticalPlanKind.Initial);var unchanged=System.Text.Json.JsonSerializer.Serialize(current.WithoutBall);
        var view2=current.View(loaded.ClubId,loaded.Revision,PossessionPhase.WithBall);
        var two=view2.Members.Where(m=>m.Role==LineupRole.Starter&&players.Single(p=>p.Id==m.PlayerId).Primary!=PlayerRole.Goalkeeper).Take(2).ToArray();
        var targetPosition=current.WithBall.Positions.Single(s=>s.Id==two[1].SlotId);
        editor.Save(LineupSelection.Exchange(view2,two[0].PlayerId,two[1].PlayerId,players));
        var after=store.Load()!.Get(TacticalPlanKind.Initial);
        Assert.Equal(unchanged,System.Text.Json.JsonSerializer.Serialize(after.WithoutBall));
        var actual=after.WithBall.Positions.Single(s=>s.Id==two[0].SlotId);
        Assert.Equal(targetPosition.X,actual.X);Assert.Equal(targetPosition.Y,actual.Y);
        Assert.Equal(two[0].PlayerId,after.Roster.Members.Single(m=>m.SlotId==two[0].SlotId).PlayerId);
    }
    [Fact] public void CopiesAreDeepIndependentAndResetKeepsComposition()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteTacticalPlanStore(file);var book=store.Load()!;
        var source=book.Get(TacticalPlanKind.Initial);var target=book.Get(TacticalPlanKind.Winning);
        var copy=TacticalPlanRules.DeepCopy(source,target.Id,target.Kind);
        copy=TacticalPlanRules.Move(copy,PossessionPhase.WithBall,"s1",.91,.28);
        Assert.NotEqual(source.WithBall.Positions.Single(s=>s.Id=="s1"),copy.WithBall.Positions.Single(s=>s.Id=="s1"));
        Assert.Equal(target.Id,copy.Id);Assert.Equal(source.Roster.Members,copy.Roster.Members);
        var reference=FormationCatalog.Get("3-5-2");copy=TacticalPlanRules.SetReference(copy,PossessionPhase.WithoutBall,reference);
        Assert.Equal("3-5-2",copy.WithoutBall.Reference.Id);Assert.False(copy.WithoutBall.IsCustom);
        var reset=TacticalPlanRules.Reset(copy,PossessionPhase.WithBall);Assert.False(reset.WithBall.IsCustom);Assert.Equal(source.WithBall.Positions,reset.WithBall.Positions);
        var saved=store.Save(book.Replace(copy));Assert.Equal(PlanFingerprint(source),PlanFingerprint(saved.Get(TacticalPlanKind.Initial)));
    }
    [Fact] public void MentalityAndAllInstructionsAreIndependentButSetPiecesStayCommon()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteTacticalPlanStore(file);var book=store.Load()!;
        var initial=store.Settings(book,TacticalPlanKind.Initial) with{Mentality=TeamMentality.Defensive};
        var instructions=initial.Instructions.ToDictionary();instructions["passing"]="short";initial=store.SaveSettings(TacticalPlanKind.Initial,initial with{Instructions=instructions});
        book=store.Load()!;var winning=store.Settings(book,TacticalPlanKind.Winning);
        Assert.Null(winning.Mentality);Assert.Null(winning.Instructions["passing"]);
        instructions=winning.Instructions.ToDictionary();instructions["recovery"]="counter";
        var roles=winning.SetPieces.ToDictionary();roles[SetPieceKind.AttackingCorner]=new("short",null);
        store.SaveSettings(TacticalPlanKind.Winning,winning with{Mentality=TeamMentality.VeryAttacking,Instructions=instructions,SetPieces=roles});
        book=store.Load()!;Assert.Equal(TeamMentality.Defensive,store.Settings(book,TacticalPlanKind.Initial).Mentality);
        Assert.Equal("short",store.Settings(book,TacticalPlanKind.Initial).Instructions["passing"]);Assert.Null(store.Settings(book,TacticalPlanKind.Initial).Instructions["recovery"]);
        Assert.Equal("short",store.Settings(book,TacticalPlanKind.Losing).SetPieces[SetPieceKind.AttackingCorner].Routine);
    }
    [Fact] public void InvalidCoordinatesRolesOverlapsAndStaleWritesCannotReplaceBook()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteTacticalPlanStore(file);var book=store.Load()!;var p=book.Get(TacticalPlanKind.Initial);var point=p.WithBall.Positions[1];var other=p.WithBall.Positions[2];
        Assert.Throws<InvalidDataException>(()=>TacticalPlanRules.Move(p,PossessionPhase.WithBall,point.Id,-.1,.5));
        Assert.Throws<InvalidDataException>(()=>TacticalPlanRules.Move(p,PossessionPhase.WithBall,point.Id,double.NaN,.5));
        Assert.Throws<InvalidDataException>(()=>TacticalPlanRules.Move(p,PossessionPhase.WithBall,point.Id,other.X,other.Y));
        Assert.Throws<InvalidDataException>(()=>TacticalPlanRules.SetRole(p,PossessionPhase.WithBall,"gk",PlayerRole.Striker));
        store.Save(book);Assert.Throws<InvalidOperationException>(()=>store.Save(book));
    }
    [Fact] public void SixCustomizedDispositionsSurviveClosingAndReopeningWithStableIds()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteTacticalPlanStore(file);var book=store.Load()!;
        foreach(var kind in Enum.GetValues<TacticalPlanKind>())foreach(var phase in Enum.GetValues<PossessionPhase>())
        {var p=book.Get(kind);p=TacticalPlanRules.Move(p,phase,"s1",.94,(int)kind*.15);book=book.Replace(p);}
        var saved=store.Save(book);var loaded=new SqliteTacticalPlanStore(file).Load()!;
        foreach(var kind in Enum.GetValues<TacticalPlanKind>()){Assert.Equal(saved.Get(kind).Id,loaded.Get(kind).Id);Assert.Equal(PlanFingerprint(saved.Get(kind)),PlanFingerprint(loaded.Get(kind)));}
        Assert.All(loaded.Plans,p=>{Assert.True(p.WithBall.IsCustom);Assert.True(p.WithoutBall.IsCustom);});
    }
    [Fact] public void ProposalsKeepSmallMovesAndPersistWingBackCodesWithoutChangingNaturalPositions()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteTacticalPlanStore(file);var book=store.Load()!;var p=book.Get(TacticalPlanKind.Initial);
        var before=new FormationSlot("test",PlayerRole.RightBack,.23,.88);
        Assert.Equal((PlayerRole.RightBack,(string?)null),TacticalPlanRules.Propose(before,.25,.88));
        Assert.Equal((PlayerRole.RightBack,(string?)"CAD"),TacticalPlanRules.Propose(before,.43,.88));
        Assert.Equal((PlayerRole.RightWinger,(string?)null),TacticalPlanRules.Propose(before,.8,.88));
        var players=new SqliteLineupStore(file).Players();var rolesBefore=players.Select(x=>(x.Id,x.Primary,string.Join(",",x.Secondary))).ToArray();
        var updated=TacticalPlanRules.SetRole(p,PossessionPhase.WithBall,"s1",PlayerRole.RightBack,"CAD");
        store.Save(book.Replace(updated));
        Assert.Equal("CAD",store.Load()!.Get(TacticalPlanKind.Initial).WithBall.Positions.Single(s=>s.Id=="s1").TacticalCode);
        Assert.Equal(rolesBefore,new SqliteLineupStore(file).Players().Select(x=>(x.Id,x.Primary,string.Join(",",x.Secondary))));
    }
    [Fact] public void LegacyCustomizedCoordinatesArePreservedAndMarkedBeforeAnyOverwrite()
    {
        var (_,file,_)=TacticalCareer();var legacy=new SqliteLineupStore(file);var old=legacy.Load()!;
        var slots=old.Formation.Slots.Select(s=>s.Id=="s1"?s with{X=.91,Y=.3}:s).ToArray();
        legacy.Save(old with{Formation=new(old.Formation.Id,slots)});
        var store=new SqliteTacticalPlanStore(file);var book=store.Load()!;var initial=book.Get(TacticalPlanKind.Initial);
        Assert.True(initial.WithBall.IsCustom);Assert.True(initial.WithoutBall.IsCustom);
        Assert.Equal(.91,initial.WithBall.Positions.Single(s=>s.Id=="s1").X);
        var reset=TacticalPlanRules.Reset(initial,PossessionPhase.WithBall);
        Assert.Equal(FormationCatalog.Get(old.Formation.Id).Slots,reset.WithBall.Positions);
        var saved=store.Save(book.Replace(reset));
        Assert.Equal(.91,saved.Get(TacticalPlanKind.Initial).WithoutBall.Positions.Single(s=>s.Id=="s1").X);
    }
    [Fact] public void SharedPhaseIdentitiesAreDerivedAndInvalidRosterRollsBack()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteTacticalPlanStore(file);var book=store.Load()!;var p=book.Get(TacticalPlanKind.Initial);
        var bad=p with{Roster=p.Roster with{Members=p.Roster.Members.SkipLast(1).Append(p.Roster.Members[0]).ToArray()}};
        Assert.Throws<InvalidDataException>(()=>store.Save(book.Replace(bad)));Assert.False(store.IsPersisted);
        var saved=store.Save(book);
        using(var c=Connection(file))using(var cmd=c.CreateCommand()){cmd.CommandText="CREATE TRIGGER forced_plan_failure BEFORE INSERT ON career_plan_dispositions BEGIN SELECT RAISE(ABORT,'test failure'); END;";cmd.ExecuteNonQuery();}
        Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(()=>store.Save(saved));Assert.Equal(saved.Revision,store.Load()!.Revision);
    }
}
