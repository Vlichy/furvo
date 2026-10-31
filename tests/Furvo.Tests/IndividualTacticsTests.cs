using System.Security.Cryptography;
using Furvo.Core.Domain;
using Furvo.Core.Lineups;
using Furvo.Core.Tactics;
using Furvo.Core.Academy;
using Furvo.Persistence;
using Xunit;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    [Fact] public void EveryPositionHasDistinctCompatibleRolePresets()
    {
        foreach(var position in Enum.GetValues<PlayerRole>())Assert.Contains(IndividualTactics.Roles,r=>r.Positions.Contains(position));
        Assert.Equal(35,IndividualTactics.Roles.Count);Assert.Equal(35,IndividualTactics.Roles.Select(r=>r.Code).Distinct().Count());
        foreach(var role in IndividualTactics.Roles)foreach(var position in role.Positions)foreach(var phase in Enum.GetValues<PossessionPhase>())IndividualTactics.Validate(new(role.Code,new Dictionary<string,string>()),position,phase);
        foreach(var group in IndividualTactics.Roles.GroupBy(r=>string.Join(',',r.Positions)))Assert.Equal(group.Count(),group.Select(r=>string.Join(';',r.Defaults.OrderBy(v=>v.Key).Select(v=>v.Key+'='+v.Value))).Distinct().Count());
    }
    [Fact] public void RoleDefaultsAndCompatibleCustomizationAreIndependent()
    {
        var initial=new SlotBehaviour("back_attacking",new Dictionary<string,string>());var values=IndividualTactics.Effective(initial,PlayerRole.RightBack,PossessionPhase.WithBall);Assert.Equal("frequent",values["forward_runs"]);Assert.Equal("outside",values["width"]);
        var next=IndividualTactics.Customize(initial,"risk","low",PlayerRole.RightBack,PossessionPhase.WithBall);next=IndividualTactics.Customize(next,"distance","short",PlayerRole.RightBack,PossessionPhase.WithBall);Assert.Equal(2,next.Overrides.Count);Assert.Empty(initial.Overrides);Assert.Equal("frequent",IndividualTactics.Effective(next,PlayerRole.RightBack,PossessionPhase.WithBall)["forward_runs"]);
        Assert.Throws<InvalidOperationException>(()=>IndividualTactics.SelectRole(next,"back_inverted",PlayerRole.RightBack,PossessionPhase.WithBall,false));Assert.Empty(IndividualTactics.SelectRole(next,"back_inverted",PlayerRole.RightBack,PossessionPhase.WithBall,true).Overrides);
    }
    [Fact] public void InvalidPositionPhaseAndUnknownIndividualOrdersAreRejected()
    {
        Assert.Throws<InvalidDataException>(()=>IndividualTactics.Validate(new("poacher",new Dictionary<string,string>()),PlayerRole.Goalkeeper,PossessionPhase.WithBall));
        var keeper=IndividualTactics.Initial(PlayerRole.Goalkeeper);Assert.DoesNotContain(IndividualTactics.Available(PlayerRole.Goalkeeper,PossessionPhase.WithBall),d=>d.Code=="shooting");
        Assert.Throws<InvalidDataException>(()=>IndividualTactics.Customize(keeper,"shooting","more",PlayerRole.Goalkeeper,PossessionPhase.WithBall));
        Assert.Throws<InvalidDataException>(()=>IndividualTactics.Customize(IndividualTactics.Initial(PlayerRole.Striker),"pressing","high",PlayerRole.Striker,PossessionPhase.WithBall));
        Assert.Throws<InvalidDataException>(()=>IndividualTactics.Customize(keeper,"risk","invalid",PlayerRole.Goalkeeper,PossessionPhase.WithBall));
    }
    [Fact] public void IndividualOrdersPersistInOneOfSixPhasesWithoutChangingPlayers()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteTacticalPlanStore(file);var book=store.Load()!;var p=book.Get(TacticalPlanKind.Initial);var slot=p.WithBall.Positions.First(s=>s.Role!=PlayerRole.Goalkeeper);var value=IndividualTactics.Customize(IndividualTactics.Initial(slot.Role),"risk","high",slot.Role,PossessionPhase.WithBall);
        var next=p.WithDisposition(PossessionPhase.WithBall,p.WithBall with{Behaviours=new Dictionary<string,SlotBehaviour>{{slot.Id,value}}});store.Save(book.Replace(next));var loaded=new SqliteTacticalPlanStore(file).Load()!;
        Assert.Equal("high",loaded.Get(p.Kind).WithBall.Behaviour(slot.Id).Overrides["risk"]);Assert.Null(loaded.Get(p.Kind).WithoutBall.Behaviours);Assert.Equal(PlanFingerprint(book.Get(TacticalPlanKind.Winning)),PlanFingerprint(loaded.Get(TacticalPlanKind.Winning)));Assert.Equal(p.Roster.Members,loaded.Get(p.Kind).Roster.Members);
        store.Save(loaded);Assert.Equal("high",store.Load()!.Get(p.Kind).WithBall.Behaviour(slot.Id).Overrides["risk"]);
    }
    [Theory][InlineData("press_direction","left")][InlineData("opponent_keeper","selective")][InlineData("offside_trap","on")][InlineData("offside_trap","off")][InlineData("press_beaten","repress")]
    public void AdvancedDefensiveInstructionsPersist(string code,string option)
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteTacticalPlanStore(file);var book=store.Load()!;var p=book.Get(TacticalPlanKind.Losing);var values=p.Instructions.ToDictionary();values[code]=option;store.Save(book.Replace(p with{Instructions=values}));Assert.Equal(option,store.Load()!.Get(p.Kind).Instructions[code]);Assert.Null(store.Load()!.Get(TacticalPlanKind.Initial).Instructions[code]);
    }
    [Fact] public void CaptainsAndSpecialistsPersistAndSkipUnavailablePlayers()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteTacticalPlanStore(file);var book=store.Load()!;var players=new SqliteLineupStore(file).Players();var ids=players.Take(3).Select(p=>p.Id).ToArray();var settings=store.Settings(book,TacticalPlanKind.Initial);var priorities=Enum.GetValues<SpecialistKind>().ToDictionary(k=>k,k=>(IReadOnlyList<PlayerId>)ids);var personnel=new TeamPersonnel(priorities);var original=PlanFingerprint(book.Get(TacticalPlanKind.Initial));
        store.SaveSettings(TacticalPlanKind.Initial,settings with{Personnel=personnel});var loaded=store.Settings(store.Load()!,TacticalPlanKind.Initial).Personnel!;Assert.Equal(ids.Take(2),loaded.Order(SpecialistKind.Captain));Assert.Equal(3,loaded.Priorities[SpecialistKind.Captain].Count);Assert.Equal(ids[1],loaded.Available(SpecialistKind.Captain,new HashSet<PlayerId>{ids[1],ids[2]}));Assert.Null(loaded.Available(SpecialistKind.Penalty,new HashSet<PlayerId>()));Assert.Equal(original,PlanFingerprint(store.Load()!.Get(TacticalPlanKind.Initial)));Assert.Equal(settings.ShootoutOrder,store.Settings(store.Load()!,TacticalPlanKind.Initial).ShootoutOrder);
        Assert.Throws<InvalidDataException>(()=>new TeamPersonnel(new Dictionary<SpecialistKind,IReadOnlyList<PlayerId>>{{SpecialistKind.Captain,new[]{ids[0],ids[0]}}}).Validate(players.Select(p=>p.Id).ToHashSet()));
    }
    [Fact] public void SynchronizedAcademyInheritsBehaviourBySlotNotPlayerOrPersonnel()
    {
        var (_,file,_)=TacticalCareer();var academy=new SqliteAcademyStore(file);var team=academy.List()[0];var store=new SqliteTacticalPlanStore(file);var book=store.Load()!;var p=book.Get(TacticalPlanKind.Winning);var slot=p.WithBall.Positions.First(s=>s.Role!=PlayerRole.Goalkeeper);var behaviour=IndividualTactics.Customize(IndividualTactics.Initial(slot.Role),"risk","high",slot.Role,PossessionPhase.WithBall);var values=p.Instructions.ToDictionary();values["offside_trap"]="on";store.Save(book.Replace(p with{WithBall=p.WithBall with{Behaviours=new Dictionary<string,SlotBehaviour>{{slot.Id,behaviour}}},Instructions=values}));
        var synced=academy.LoadPlans(team.Team.Id);Assert.Equal("high",synced.Get(p.Kind).WithBall.Behaviour(slot.Id).Overrides["risk"]);Assert.Equal("on",synced.Get(p.Kind).Instructions["offside_trap"]);Assert.Empty(synced.Get(p.Kind).Roster.Members.Select(m=>m.PlayerId).Intersect(p.Roster.Members.Select(m=>m.PlayerId)));
        var a=new SqliteAcademyPlanStore(file,team.Team.Id);var settings=a.Settings(synced,p.Kind);var own=synced.Get(p.Kind).Roster.Members.First().PlayerId;a.SaveSettings(p.Kind,settings with{Personnel=new(new Dictionary<SpecialistKind,IReadOnlyList<PlayerId>>{{SpecialistKind.Captain,new[]{own}}})});Assert.Equal(own,a.Settings(a.Load()!,p.Kind).Personnel!.Order(SpecialistKind.Captain).Single());Assert.Empty((store.Settings(store.Load()!,p.Kind).Personnel??TeamPersonnel.Empty()).Order(SpecialistKind.Captain));
    }
    [Fact] public void OldPlanReadAddsOnlyUndefinedAdvancedPreferencesWithoutWriting()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteTacticalPlanStore(file);store.Save(store.Load()!);using(var c=Connection(file)){using var q=c.CreateCommand();q.CommandText="DROP TABLE career_slot_behaviours; DROP TABLE tactical_behaviour_schema; UPDATE career_tactical_plans SET instructions_json=json_remove(instructions_json,'$.press_direction','$.opponent_keeper','$.offside_trap','$.press_beaten');";q.ExecuteNonQuery();}
        var hash=SHA256.HashData(File.ReadAllBytes(file));var loaded=store.Load()!;Assert.Null(loaded.Get(TacticalPlanKind.Initial).Instructions["offside_trap"]);Assert.Null(loaded.Get(TacticalPlanKind.Initial).WithBall.Behaviours);Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(file)));store.Save(loaded);
    }
    [Fact] public void FamiliarityRequiresEvidenceAndDoesNotGrowOnTacticalSaves()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteFamiliarityStore(file);var hash=SHA256.HashData(File.ReadAllBytes(file));Assert.Empty(store.Read());Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(file)));var planStore=new SqliteTacticalPlanStore(file);var book=planStore.Load()!;var id=book.Plans.First().Roster.Members.First().PlayerId;var observation=new FamiliarityObservation(id,FamiliarityAspect.Role,"role/v1",.4m,new DateOnly(2026,7,1),"fixture de prueba; sin entrenamiento simulado");store.Record(observation);Assert.Equal(observation,store.Read().Single());planStore.Save(book);Assert.Equal(.4m,store.Read().Single().Value);Assert.Throws<InvalidDataException>(()=>store.Record(observation with{Value=1.01m}));Assert.Throws<InvalidDataException>(()=>store.Record(observation with{Evidence=""}));
    }
    [Fact] public void RivalOrdersCannotEscapeTheirMatchOrOpponent()
    {
        var match=Guid.NewGuid();var club=new ClubId(Guid.NewGuid());var id=new PlayerId(Guid.NewGuid());var order=new OpponentOrder(match,club,id,true,true,true,true,"normal");order.Validate(match,club,new HashSet<PlayerId>{id});Assert.Throws<InvalidDataException>(()=>order.Validate(Guid.NewGuid(),club,new HashSet<PlayerId>{id}));Assert.Throws<InvalidDataException>(()=>order.Validate(match,club,new HashSet<PlayerId>()));
    }
    [Fact] public void TacticalConflictWarningsDoNotRewriteGeometry()
    {
        var value=new SlotBehaviour("centre_stopper",new Dictionary<string,string>());Assert.Contains("IndividualLowLineWarning",IndividualTactics.Warnings(value,PlayerRole.CentreBack,PossessionPhase.WithoutBall,new Dictionary<string,string?>{{"defensive_line","low"}}));
    }
    [Fact] public void CrossPhaseCopyKeepsRoleAndOnlyApplicableOverrides()
    {
        var (_,file,_)=TacticalCareer();var book=new SqliteTacticalPlanStore(file).Load()!;var p=book.Get(TacticalPlanKind.Initial);var slot=p.WithBall.Positions.First(s=>s.Role!=PlayerRole.Goalkeeper);var value=IndividualTactics.Customize(IndividualTactics.Initial(slot.Role),"shooting","more",slot.Role,PossessionPhase.WithBall);p=p with{WithBall=p.WithBall with{Behaviours=new Dictionary<string,SlotBehaviour>{{slot.Id,value}}}};var copied=TacticalPlanRules.CopyDisposition(p,PossessionPhase.WithoutBall,p,PossessionPhase.WithBall);Assert.Equal(value.Role,copied.WithoutBall.Behaviour(slot.Id).Role);Assert.Empty(copied.WithoutBall.Behaviour(slot.Id).Overrides);Assert.Equal("more",copied.WithBall.Behaviour(slot.Id).Overrides["shooting"]);
    }
    [Theory][InlineData("press_direction","left")][InlineData("press_direction","flanks")][InlineData("opponent_keeper","press")][InlineData("offside_trap","on")][InlineData("press_beaten","repress")][InlineData("press_beaten","positions")]
    public void AdvancedDiagramsUseRealPlayerOrigins(string code,string option)
    {
        var (_,file,settings)=TacticalCareer();var lineup=new SqliteLineupStore(file).Load()!;var values=settings.Instructions.ToDictionary();values[code]=option;var diagram=Furvo.Presentation.Components.TacticDiagramBuilder.Build(lineup,lineup.Formation,settings with{Instructions=values},1,code,SetPieceKind.Penalty,true);diagram.Validate();Assert.NotEmpty(diagram.Arrows);Assert.All(diagram.Arrows,a=>Assert.Contains(lineup.Formation.Slots,s=>s.Id==a.From));
    }
    private sealed class VerifiedTestMatch(Guid match,ClubId rival,PlayerId player):IMatchPreparationContext
    {public IReadOnlySet<PlayerId> OpponentSquad(Guid candidate,ClubId own,ClubId opponent){if(candidate!=match||opponent!=rival||own==rival)throw new InvalidDataException("Fuera del encuentro de prueba verificado.");return new HashSet<PlayerId>{player};}}
    [Fact] public void RivalOrderPersistenceRequiresVerifiedMatchContext()
    {
        var (_,file,_)=TacticalCareer();var match=Guid.NewGuid();var rival=new ClubId(Guid.NewGuid());var player=new PlayerId(Guid.NewGuid());var store=new SqliteOpponentOrderStore(file,new VerifiedTestMatch(match,rival,player));var order=new OpponentOrder(match,rival,player,true,false,true,false,"cautious");store.Save(order);Assert.Equal(order,store.Read(match,rival).Single());Assert.Throws<InvalidDataException>(()=>store.Save(order with{MatchId=Guid.NewGuid()}));
    }
}
