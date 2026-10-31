using System.Security.Cryptography;
using Furvo.Core.Domain;
using Furvo.Core.Careers;
using Furvo.Core.Academy;
using Furvo.Core.Development;
using Furvo.Core.Tactics;
using Furvo.Persistence;
using Xunit;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    private static readonly Lazy<DateOnly> SportsStart=new(()=>new SqliteWorldStore(EconomicSource).Load().Seasons.Single().StartDate);
    private static DateOnly SportsDate=>SportsStart.Value;
    private static Guid Principal(string file){using var c=Connection(file);using var q=c.CreateCommand();q.CommandText="SELECT coach_id FROM career_state";return Guid.Parse((string)q.ExecuteScalar()!);}
    private static TrainingSession Session(Guid team,IReadOnlyList<Player> players,string exercise="technical",int hour=9,TrainingLoad load=TrainingLoad.Medium,TrainingTarget? target=null)
    {target??=new(TrainingScope.Team);return new(Guid.NewGuid(),team,SportsDate,new TimeOnly(hour,0),30,exercise,load,target,TrainingRules.Resolve(target,players,Array.Empty<TrainingGroup>()),Objective:"Objetivo de prueba");}
    private sealed class FixtureEligibility:IAcademyEligibility
    {public bool Allowed=true;public int Calls;public EligibilityDecision Check(EligibilityContext c,MobilityKind k){Calls++;return new(Allowed,Allowed?Array.Empty<string>():new[]{"Licencia revocada de prueba"},"fixture-only/v1");}}
    private sealed class FixtureClock(DateTime now):ITrainingExecutionClock
    {public DateTime CareerTime=>now;public bool Verified(Guid team,TrainingEvidence e)=>e.Evidence=="fixture-only";}
    [Fact] public void CareerClockUsesPersistedDateInsteadOfSQLiteCurrentDateKeyword()
    {var (careers,file,_)=TacticalCareer();using var c=Connection(file);using var q=c.CreateCommand();q.CommandText="SELECT save_id FROM career_state";var id=new SaveId(Guid.Parse((string)q.ExecuteScalar()!));Assert.Equal(careers.Load(id).CurrentDate,new SqliteSportsStore(file).CareerDate());Assert.Equal(SportsDate,new SqliteSportsStore(file).CareerDate());}
    [Fact] public void PublicPlanningAndMovementQueriesNeverExposeHiddenAttributesOrProgress()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteClubSquadStore(file);Assert.DoesNotContain(typeof(TrainingCandidate).GetProperties(),p=>p.Name.Contains("Attribute")||p.Name.Contains("Progress"));foreach(var location in store.Locations().Values){var values=location.Details.AttributeGroups.SelectMany(g=>g.Values).ToArray();Assert.Equal(51,values.Length);Assert.DoesNotContain(values,a=>PlayerAttributeCatalog.Get(a.Code).IsHidden);}Assert.Null(new SqliteTrainingExecutionStore(file).ObservedPhysical(store.Candidates(store.Teams()[0].Id)[0].Id));
    }
    [Fact] public void RegulationConfigurationRequiresMatchingCountryFederationSeasonCategoryAndCompetition()
    {
        var p=new Player(new PlayerId(Guid.NewGuid()),"Jugador de prueba",new DateOnly(2009,1,1));var club=new ClubId(Guid.NewGuid());var key=new AcademyRuleKey("ES","RFEF","2026/27","Juvenil","fixture-competition");var rule=new AcademyRule(key,new DateOnly(2008,1,1),null,25,"fixture-only",SportsDate,true);var validator=new ConfiguredAcademyEligibility(rule,_=>true);var context=new EligibilityContext(club,p,Guid.NewGuid(),Guid.NewGuid(),"Juvenil","fixture-competition",SportsDate,20,"ES","RFEF","2026/27");Assert.True(validator.Check(context,MobilityKind.Permanent).Allowed);Assert.False(validator.Check(context with{Country="FR"},MobilityKind.Permanent).Allowed);Assert.False(validator.Check(context with{Season="2027/28"},MobilityKind.Permanent).Allowed);Assert.False(validator.Check(context with{DestinationCount=25},MobilityKind.Permanent).Allowed);Assert.False(new ConfiguredAcademyEligibility(rule,_=>null).Check(context,MobilityKind.Permanent).Allowed);
    }
    private sealed class FixtureAuthorization(bool allowed):IFacilityPurchaseAuthorization
    {public bool CanCommit(FacilityAcquisition request,decimal cash,string currency)=>allowed&&request.Authorization=="fixture-only";}
    [Fact] public void AuthorizedAcquisitionIsAtomicAndDoesNotMakeOrderedFacilityUsable()
    {
        var (_,file,_)=TacticalCareer();var service=new SqliteSportsStore(file);var facility=service.Facility();var cash=SqliteEconomyStore.ReadResources(file).Single(r=>r.ClubId==facility.Club).Cash!.Value;var request=new FacilityAcquisition(Guid.NewGuid(),facility.Club,"footbonaut",service.CareerDate(),null,null,null,null,null,null,null,"PendingQuote",null);service.RequestFacility(request);var quote=request with{Purchase=100m,Installation=20m,AnnualMaintenance=5m,Operating=1m,Currency="EUR",LeadDays=10,SpaceConfirmed=true,Status="Approved",Authorization="fixture-only"};Assert.Throws<InvalidOperationException>(()=>service.CommitAcquisition(quote,new FixtureAuthorization(false)));Assert.Equal(cash,SqliteEconomyStore.ReadResources(file).Single(r=>r.ClubId==facility.Club).Cash);service.CommitAcquisition(quote,new FixtureAuthorization(true));Assert.Equal(cash-120,SqliteEconomyStore.ReadResources(file).Single(r=>r.ClubId==facility.Club).Cash);Assert.Equal(FacilityOwnership.Ordered,service.Facility().Ownership);Assert.False(service.Facility().Available(service.CareerDate().AddDays(15)));Assert.Throws<InvalidDataException>(()=>service.CommitAcquisition(quote,new FixtureAuthorization(true)));
    }
    [Fact] public void FailedMobilityTransactionRollsBackAssignmentAndPromotedEntity()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var teams=squads.Teams();var s=new SqliteSportsStore(file);s.SavePlan(s.Plan(teams[0].Id));using(var c=Connection(file)){using var q=c.CreateCommand();q.CommandText="CREATE TRIGGER reject_move BEFORE INSERT ON movement_history BEGIN SELECT RAISE(ABORT,'fixture rejection'); END;";q.ExecuteNonQuery();}var player=squads.Players(teams[1].Id)[0];var primary=new SqliteWorldStore(file).Load().Players.Count;Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(()=>new SqliteAcademyMobilityStore(file,new FixtureEligibility()).Move(new(player.Id,teams[1].Id,teams[0].Id,MobilityKind.Permanent,s.CareerDate()),Principal(file)));Assert.Equal(teams[1].Id,squads.All()[player.Id].Team);Assert.Equal(primary,new SqliteWorldStore(file).Load().Players.Count);Assert.Contains(new SqliteAcademyStore(file).Players(teams[1].Id),p=>p.Id==player.Id);
    }
    [Fact] public void CaptainsHaveExactlyTwoIndependentNullableSlotsAndNoDuplicates()
    {
        var a=new PlayerId(Guid.NewGuid());var b=new PlayerId(Guid.NewGuid());var squad=new HashSet<PlayerId>{a,b};var p=new TeamPersonnel(new Dictionary<SpecialistKind,IReadOnlyList<PlayerId>>(),new PlayerId?[]{null,b});p.Validate(squad);Assert.Equal(2,p.Captains.Count);Assert.Equal(b,p.Available(SpecialistKind.Captain,squad));Assert.Null(p.Available(SpecialistKind.Captain,new HashSet<PlayerId>{a}));Assert.Throws<InvalidDataException>(()=>(p with{CaptainSlots=new PlayerId?[]{b,b}}).Validate(squad));
    }
    [Fact] public void SportsReadDoesNotMigrateExistingCareerOrInventFacilityOwnership()
    {
        var (_,file,_)=TacticalCareer();var teams=new SqliteClubSquadStore(file).Teams();var hash=SHA256.HashData(File.ReadAllBytes(file));var s=new SqliteSportsStore(file);Assert.Empty(s.Plan(teams[0].Id).Sessions);Assert.Empty(s.Reports(teams[0].Id));Assert.Equal(FacilityOwnership.Unknown,s.Facility().Ownership);Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(file)));Assert.Equal(24,new SqliteClubSquadStore(file).Players(teams[0].Id).Count);
    }
    [Theory][InlineData("technical")][InlineData("general_strength")][InlineData("upper_body")][InlineData("power")][InlineData("prevention")][InlineData("active_recovery")]
    public void SessionModalitiesPersistWithoutChangingAttributes(string exercise)
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0];var players=squads.Players(team.Id);var values=players.ToDictionary(p=>p.Id,p=>p.Attributes.AllValues.ToArray());var s=new SqliteSportsStore(file);var plan=s.Plan(team.Id);s.SavePlan(plan with{Sessions=new[]{Session(team.Id,players,exercise)}});var loaded=new SqliteSportsStore(file).Plan(team.Id);Assert.Single(loaded.Sessions);Assert.Equal(exercise,loaded.Sessions[0].Exercise);Assert.All(squads.Players(team.Id),p=>Assert.Equal(values[p.Id],p.Attributes.AllValues.ToArray()));
    }
    [Fact] public void MultipleSessionsShareLoadAndRejectOverlapAcrossGroups()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var players=squads.Players(team);var service=new SqliteSportsStore(file);var plan=service.Plan(team);var a=Session(team,players);var b=Session(team,players,"general_strength",16);Assert.Equal(63m,TrainingCatalog.PlannedLoad(a)+TrainingCatalog.PlannedLoad(b));service.SavePlan(plan with{Sessions=new[]{a,b}});var current=service.Plan(team);Assert.Throws<InvalidDataException>(()=>service.SavePlan(current with{Sessions=new[]{a,b with{Start=a.Start}}}));Assert.Throws<InvalidDataException>(()=>service.SavePlan(current with{Sessions=new[]{a,a}}));
    }
    [Fact] public void IndividualPositionAndGroupParticipantsAreResolvedWithoutDuplication()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteClubSquadStore(file);var team=store.Teams()[0].Id;var players=store.Players(team);var p=players[0];var group=new TrainingGroup(Guid.NewGuid(),"Grupo",new[]{p.Id,players[1].Id},"Pase",null);Assert.Equal(new[]{p.Id},TrainingRules.Resolve(new(TrainingScope.Individual,p.Id),players,new[]{group}));Assert.Contains(p.Id,TrainingRules.Resolve(new(TrainingScope.Position,Position:p.Positions!.Primary),players,new[]{group}));Assert.Single(TrainingRules.Resolve(new(TrainingScope.Group,Group:group.Id,Excluded:new[]{p.Id}),players,new[]{group}));var service=new SqliteSportsStore(file);var target=new TrainingTarget(TrainingScope.Group,Group:group.Id);var session=Session(team,players) with{Target=target,Participants=group.Members};service.SavePlan(service.Plan(team) with{Groups=new[]{group},Sessions=new[]{session}});Assert.Single(service.Plan(team).Groups);
    }
    [Fact] public void GoalkeeperWorkRejectsOutfieldPlayersAndAcceptsKeeperGroups()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteClubSquadStore(file);var team=store.Teams()[0].Id;var players=store.Players(team);var service=new SqliteSportsStore(file);Assert.Throws<InvalidDataException>(()=>service.SavePlan(service.Plan(team) with{Sessions=new[]{Session(team,players,"reflexes")}}));var session=Session(team,players,"reflexes",target:new(TrainingScope.Position,Position:PlayerRole.Goalkeeper));service.SavePlan(service.Plan(team) with{Sessions=new[]{session}});Assert.All(service.Plan(team).Sessions.Single().Participants,id=>Assert.Equal(PlayerRole.Goalkeeper,players.Single(p=>p.Id==id).Positions!.Primary));
    }
    [Fact] public void YouthWorkloadUsesDifferentPlanningPolicyAndTacticsAreIndependent()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteClubSquadStore(file);var team=store.Teams().First(t=>t.Category=="Juvenil").Id;var players=store.Players(team);var service=new SqliteSportsStore(file);var tactics=new SqliteAcademyStore(file).LoadPlans(team);Assert.Throws<InvalidDataException>(()=>service.SavePlan(service.Plan(team) with{Sessions=new[]{Session(team,players,load:TrainingLoad.High)}}));service.SavePlan(service.Plan(team) with{Sessions=new[]{Session(team,players)}});Assert.Equal(3,tactics.Plans.Count);Assert.Equal(PlanFingerprint(tactics.Plans[0]),PlanFingerprint(new SqliteAcademyStore(file).LoadPlans(team).Plans[0]));
    }
    [Fact] public void DelegationIsAreaSpecificAndDoesNotInventSpecialists()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams().First(t=>t.Category=="Senior").Id;var service=new SqliteSportsStore(file);var staff=service.Staff(team);var coach=new SqliteAcademyStore(file).Get(team).Coach;var plan=service.SavePlan(service.Plan(team) with{Delegations=new[]{new TrainingDelegation(TrainingResponsibility.Collective,coach.Id)}});Assert.Equal(TrainingManagement.Mixed,plan.Management);Assert.Throws<InvalidDataException>(()=>service.SavePlan(plan with{Delegations=new[]{new TrainingDelegation(TrainingResponsibility.Gym,coach.Id)}}));Assert.DoesNotContain(staff,p=>p.Responsibilities.Contains(TrainingResponsibility.Goalkeepers));Assert.Equal(TrainingManagement.Manual,service.SavePlan(plan with{Delegations=Array.Empty<TrainingDelegation>()}).Management);
    }
    [Fact] public void SetPieceTrainingReferencesTheExistingTacticAndRequiresItToBeDesigned()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var service=new SqliteSportsStore(file);var s=Session(team,squads.Players(team),"corner_practice") with{Routine=new(SetPieceKind.AttackingCorner)};Assert.Throws<InvalidDataException>(()=>service.SavePlan(service.Plan(team) with{Sessions=new[]{s}}));var plans=new SqliteTacticalPlanStore(file);var book=plans.Load()!;var settings=plans.Settings(book,TacticalPlanKind.Initial);var pieces=settings.SetPieces.ToDictionary();pieces[SetPieceKind.AttackingCorner]=new("short",null);plans.SaveSettings(TacticalPlanKind.Initial,settings with{SetPieces=pieces});service.SavePlan(service.Plan(team) with{Sessions=new[]{s}});Assert.Equal("short",plans.Settings(plans.Load()!,TacticalPlanKind.Initial).SetPieces[SetPieceKind.AttackingCorner].Routine);
    }
    [Fact] public void FootbonautRequiresOwnershipAndSharesReservationsAcrossTeams()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0];var youth=squads.Teams().First(t=>t.Category=="Juvenil");var service=new SqliteSportsStore(file);var facility=service.Facility();var a=Session(team.Id,squads.Players(team.Id),"footbonaut_touch",target:new(TrainingScope.Individual,squads.Players(team.Id)[0].Id)) with{Facility=facility.Id};Assert.Throws<InvalidOperationException>(()=>service.SavePlan(service.Plan(team.Id) with{Sessions=new[]{a}}));service.RegisterFacility(facility with{Ownership=FacilityOwnership.Installed,AvailableFrom=SportsDate,Evidence="fixture-only: instalación de prueba"});service.SavePlan(service.Plan(team.Id) with{Sessions=new[]{a}});var b=Session(youth.Id,squads.Players(youth.Id),"footbonaut_pass",target:new(TrainingScope.Individual,squads.Players(youth.Id)[0].Id)) with{Facility=facility.Id};Assert.Throws<InvalidDataException>(()=>service.SavePlan(service.Plan(youth.Id) with{Sessions=new[]{b}}));service.SavePlan(service.Plan(youth.Id) with{Sessions=new[]{b with{Start=new TimeOnly(16,0)}}});Assert.Equal(2,new[]{team.Id,youth.Id}.Sum(id=>service.Plan(id).Sessions.Count));
    }
    [Fact] public void FacilityRequestsDoNotSpendMoneyOrInventEquipment()
    {
        var (_,file,_)=TacticalCareer();var service=new SqliteSportsStore(file);var facility=service.Facility();var cash=SqliteEconomyStore.ReadResources(file).Single(r=>r.ClubId==facility.Club).Cash;service.RequestFacility(new(Guid.NewGuid(),facility.Club,"footbonaut",service.CareerDate(),null,null,null,null,null,null,null,"PendingQuote",null));Assert.Single(service.Acquisitions());Assert.Equal(cash,SqliteEconomyStore.ReadResources(file).Single(r=>r.ClubId==facility.Club).Cash);Assert.Equal(FacilityOwnership.Unknown,service.Facility().Ownership);
    }
    [Theory][InlineData(0,1)][InlineData(1,0)][InlineData(0,3)][InlineData(3,0)][InlineData(1,3)][InlineData(3,1)][InlineData(2,3)][InlineData(3,2)]
    public void VerifiedPermanentMovesPreserveIdentityAttributesContractsAndValidPlans(int sourceIndex,int targetIndex)
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var teams=squads.Teams();var source=teams[sourceIndex];var target=teams[targetIndex];var player=squads.Players(source.Id).First(p=>p.Positions!.Primary!=PlayerRole.Goalkeeper);var before=squads.All().Count;var contracts=new SqliteWorldStore(file).Load().Contracts.ToArray();var policy=new FixtureEligibility();new SqliteAcademyMobilityStore(file,policy).Move(new(player.Id,source.Id,target.Id,MobilityKind.Permanent,SportsDate),Principal(file));Assert.True(policy.Calls>=2);Assert.Equal(before,squads.All().Count);Assert.DoesNotContain(squads.Players(source.Id),p=>p.Id==player.Id);var moved=squads.Players(target.Id).Single(p=>p.Id==player.Id);Assert.Equal(player.Attributes.AllValues,moved.Attributes.AllValues);Assert.Equal(player.Positions!.Primary,moved.Positions!.Primary);Assert.Equal(contracts,new SqliteWorldStore(file).Load().Contracts);Assert.Equal(11,new SqliteLineupStore(file).Load()!.Members.Count(m=>m.Role==Furvo.Core.Lineups.LineupRole.Starter));foreach(var id in new[]{source.Id,target.Id}.Where(id=>id!=teams[0].Id))Assert.Equal(3,new SqliteAcademyStore(file).LoadPlans(id).Plans.Count);
    }
    [Fact] public void UnverifiedMobilityAndTemporaryParticipationNeverWrite()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var teams=squads.Teams();var player=squads.Players(teams[1].Id)[0];var store=new SqliteAcademyMobilityStore(file);var hash=SHA256.HashData(File.ReadAllBytes(file));foreach(var kind in Enum.GetValues<MobilityKind>()){var request=new MobilityRequest(player.Id,teams[1].Id,teams[0].Id,kind,SportsDate);Assert.False(store.Check(request).Allowed);Assert.Throws<InvalidOperationException>(()=>store.Move(request,Principal(file)));}Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(file)));
    }
    [Fact] public void RecommendationsRequirePrincipalAndRevalidateBeforeAcceptance()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var teams=squads.Teams();var source=teams[1];var player=squads.Players(source.Id)[0];var coach=new SqliteAcademyStore(file).Get(source.Id).Coach;var policy=new FixtureEligibility();var store=new SqliteAcademyMobilityStore(file,policy);var proposal=new MovementRecommendation(Guid.NewGuid(),coach.Id,player.Id,source.Id,teams[0].Id,MobilityKind.Permanent,"Supervisión de atributos actuales, sin rendimiento inventado",SportsDate,RecommendationStatus.Pending);store.Recommend(proposal);Assert.Equal(source.Id,squads.All()[player.Id].Team);Assert.Throws<InvalidOperationException>(()=>store.Accept(proposal.Id,coach.Id));policy.Allowed=false;Assert.Throws<InvalidOperationException>(()=>store.Accept(proposal.Id,Principal(file)));Assert.Equal(RecommendationStatus.Pending,store.Recommendations().Single().Status);store.Reject(proposal.Id,Principal(file));Assert.Equal(RecommendationStatus.Rejected,store.Recommendations().Single().Status);Assert.Equal(source.Id,squads.All()[player.Id].Team);
    }
    [Fact] public void TrainingAnalysisAndFacilitiesStayIndependentAcrossCareers()
    {
        var (_,a,_)=TacticalCareer();var (_,b,_)=TacticalCareer();var squads=new SqliteClubSquadStore(a);var team=squads.Teams()[0].Id;var service=new SqliteSportsStore(a);service.SavePlan(service.Plan(team) with{Sessions=new[]{Session(team,squads.Players(team))}});service.RequestAnalysis(new(Guid.NewGuid(),team,"Solicitud sin datos",service.CareerDate(),null,"PendingData"));Assert.Empty(new SqliteSportsStore(b).Plan(team).Sessions);Assert.Empty(new SqliteSportsStore(b).AnalysisRequests(team));
    }
    [Fact] public void ReportsStoreOnlyObservedFindingsAndPrioritiesDoNotChangeTactics()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var service=new SqliteSportsStore(file);var before=PlanFingerprint(new SqliteTacticalPlanStore(file).Load()!.Plans[0]);var generator=new SqliteAnalysisReportService(file);var report=generator.Generate(team,new(),AnalysisReportKind.Technical,generator.Staff(team,AnalysisReportKind.Technical)[0].Id);generator.SavePriorities(team,report.Id,new[]{report.Findings[0].Topic});Assert.Single(service.Reports(team));Assert.Equal(before,PlanFingerprint(new SqliteTacticalPlanStore(file).Load()!.Plans[0]));Assert.Throws<InvalidDataException>(()=>service.SaveReport(report with{Priorities=new[]{"invented"}}));Assert.Throws<InvalidOperationException>(()=>service.SaveReport(report with{Findings=new[]{new AnalysisFinding("bad","Sin fuente","",service.CareerDate())}}));
    }
    [Fact] public void ExecutionNeedsRealClockEvidenceAndDoesNotImproveAttributesInstantly()
    {
        var (_,file,_)=TacticalCareer();var squads=new SqliteClubSquadStore(file);var team=squads.Teams()[0].Id;var player=squads.Players(team)[0];var service=new SqliteSportsStore(file);var session=Session(team,squads.Players(team),target:new(TrainingScope.Individual,player.Id));service.SavePlan(service.Plan(team) with{Sessions=new[]{session}});var evidence=new TrainingEvidence(session.Id,session.Date,new[]{player.Id},"fixture-only");var executor=new SqliteTrainingExecutionStore(file);var physical=new Dictionary<PlayerId,PhysicalObservation>{{player.Id,new(25,50,.1m,session.Date)}};Assert.Throws<InvalidOperationException>(()=>executor.Process(team,session.Date,new[]{evidence},physical,new FixtureClock(session.Begins)));executor.Process(team,session.Date,new[]{evidence},physical,new FixtureClock(session.Ends));Assert.NotEmpty(executor.State(player.Id).Accumulated);Assert.Empty(executor.State(player.Id).AttributeChanges);Assert.Throws<InvalidDataException>(()=>executor.Process(team,session.Date,new[]{evidence},physical,new FixtureClock(session.Ends)));Assert.Equal(player.Attributes.AllValues,squads.Players(team).Single(p=>p.Id==player.Id).Attributes.AllValues);Assert.Throws<InvalidOperationException>(()=>service.SavePlan(service.Plan(team) with{Sessions=Array.Empty<TrainingSession>()}));
    }
    [Fact] public void ExperimentalDevelopmentIsBoundedReproducibleAndAgesAcrossThirtySeasonBench()
    {
        var player=InitialSquads2026.Generate().Players.First(p=>p.Positions!.Primary!=PlayerRole.Goalkeeper);DevelopmentState Run(){var state=DevelopmentState.Empty(player.Id);var day=SportsDate;var physical=new PhysicalObservation(20,50,.1m,day);for(var n=0;n<30*365;n++){var d=day.AddDays(n);var s=Session(Guid.Parse("11111111-1111-1111-1111-111111111111"),new[]{player},"technical",load:TrainingLoad.Medium) with{Id=AcademyGenerator.Identity("bench-session/"+n),Date=d};var attended=n%7<3;state=DevelopmentEngine.Process(player,state,d,attended?new[]{s}:Array.Empty<TrainingSession>(),attended?new[]{new TrainingEvidence(s.Id,d,new[]{player.Id},"bench evidence")}:Array.Empty<TrainingEvidence>(),physical);physical=state.Physical!;Assert.InRange(physical.Fatigue,0,100);Assert.All(state.AttributeChanges,p=>Assert.InRange(player.Attributes.AllValues[p.Key]+p.Value,1,20));}return state;}var a=Run();var b=Run();Assert.Equal(a.AttributeChanges,b.AttributeChanges);Assert.Equal(a.Accumulated,b.Accumulated);Assert.DoesNotContain(a.AttributeChanges.Keys,k=>PlayerAttributeCatalog.Get(k).IsHidden);
    }
    [Fact] public void ExperimentalDoseHasBenefitsButVeryHighLoadCanBeCounterproductive()
    {
        var player=InitialSquads2026.Generate().Players.First(p=>p.Positions!.Primary!=PlayerRole.Goalkeeper);DevelopmentState Run(TrainingLoad load){var s=Session(Guid.NewGuid(),new[]{player},load:load) with{Minutes=60};return DevelopmentEngine.Process(player,DevelopmentState.Empty(player.Id),s.Date,new[]{s},new[]{new TrainingEvidence(s.Id,s.Date,new[]{player.Id},"fixture-only")},new(20,50,.1m,s.Date));}var low=Run(TrainingLoad.Low);var medium=Run(TrainingLoad.Medium);var high=Run(TrainingLoad.High);var excessive=Run(TrainingLoad.VeryHigh);Assert.True(medium.Accumulated["ball_control"]>low.Accumulated["ball_control"]);Assert.True(excessive.Accumulated["ball_control"]<high.Accumulated["ball_control"]);Assert.True(excessive.Physical!.Fatigue>high.Physical!.Fatigue);
    }
    [Fact] public void RecoveryDoesNotInstantlyEraseFatigue()
    {
        var player=InitialSquads2026.Generate().Players.First();var s=Session(Guid.NewGuid(),new[]{player},"rest",load:TrainingLoad.Rest);var result=DevelopmentEngine.Process(player,DevelopmentState.Empty(player.Id),s.Date,new[]{s},new[]{new TrainingEvidence(s.Id,s.Date,new[]{player.Id},"fixture-only")},new(70,50,.3m,s.Date));Assert.True(result.Physical!.Fatigue>0&&result.Physical.Fatigue<70);Assert.Empty(result.AttributeChanges);
    }
}
