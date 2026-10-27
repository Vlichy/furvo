using System.Security.Cryptography;
using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Furvo.Tests;

/// <summary>Los contratos aprobados de este archivo son fixtures sintéticos, nunca reglas económicas del juego.</summary>
public sealed partial class CareerCreationTests : IDisposable
{
    private readonly string _root=Path.Combine(Path.GetTempPath(),"FurvoCareers",Guid.NewGuid().ToString("N"));
    private static string Source=>Path.Combine(AppContext.BaseDirectory,"published-v4.db");
    private readonly DateOnly _date=new(2026,7,1);
    public CareerCreationTests()=>Directory.CreateDirectory(_root);
    private CoachProfile Profile(CoachProfileId? id=null,string name="Prueba")=>new(id??CoachProfileId.New(),name,"Entrenador",new DateOnly(1980,3,4),"ES","Ciudad de prueba",CoachExperience.Novice,"generic-v1",_date);
    private SqliteCareerStore Store()=>new(Path.Combine(_root,"careers"),Source);
    private static ManagerOffer Offer(CoachProfile p,bool approved=true)
    {
        var q=PublishedWorldQueries.Open(Source);var c=q.GetClubs()[0];var s=q.GetSeasons()[0];
        var principles=Array.AsReadOnly(Enumerable.Range(1,3).Select(i=>new ClubPrincipleProposal("test-"+i,"Prueba "+i,"Compromiso solo de pruebas")).ToArray());
        var objectives=Array.AsReadOnly(new[]{new ContractObjective("test-goal","Objetivo solo de pruebas",BoardObjectiveHorizon.Season,BoardObjectivePriority.High,new DateOnly(2027,6,30),true)});
        return new ManagerOffer(Guid.NewGuid(),p.Id,c.Id,s.Id,new ManagerEmploymentTerms(s.StartDate,s.EndDate,10000m,"EUR","Condición sintética de pruebas",TerminationClauseKind.Defined),
            new ManagerCommitments("test-only",approved?CommitmentApproval.Approved:CommitmentApproval.Draft,principles,objectives),new ManagerTacticalProject("4-4-2","Estilo solo de pruebas","Orientativo",false),
            new ClubFinancialResources(c.Id,s.Id){TransferBudget=100000m,TransferRemaining=90000m,PlayerWageBudget=200000m,PlayerWageRemaining=20000m,StaffBudget=15000m,Currency="EUR"});
    }
    private CareerCreationRequest Request(CoachProfile p,CareerStartMode mode,ManagerOffer? offer=null)=>new(SaveId.New(),CoachId.New(),p,mode,offer,mode==CareerStartMode.SignOffer?ManagerContractId.New():null);
    [Fact] public void ProfilePersistsAndRepeatedSameIdentityDoesNotDuplicate()
    {
        var db=Path.Combine(_root,"profiles.db");var store=new SqliteCoachProfileStore(db);Assert.Empty(store.List());Assert.False(File.Exists(db));
        var p=Profile();store.Create(p);store.Create(p);Assert.Equal(p,Assert.Single(new SqliteCoachProfileStore(db).List()));
        Assert.Throws<InvalidOperationException>(()=>store.Create(Profile(p.Id,"Otro")));Assert.Equal(p,Assert.Single(store.List()));
    }
    [Theory][InlineData("")][InlineData(" ")][InlineData("a\nb")]
    public void IncompleteOrControlCharacterNamesAreRejected(string name)=>Assert.Throws<ArgumentException>(()=>Profile(name:name));
    [Fact] public void ProfileRejectsLongTextInvalidNationalityAndNonAdult()
    {
        Assert.Throws<ArgumentException>(()=>Profile(name:new string('a',61)));
        Assert.Throws<ArgumentException>(()=>new CoachProfile(CoachProfileId.New(),"A","B",new DateOnly(2010,1,1),"ES","C",CoachExperience.Novice,"generic-v1",_date));
        Assert.Throws<ArgumentException>(()=>new CoachProfile(CoachProfileId.New(),"A","B",new DateOnly(1980,1,1),"XX","C",CoachExperience.Novice,"generic-v1",_date));
        Assert.Throws<ArgumentException>(()=>new CoachProfile(CoachProfileId.New(),"A","B",new DateOnly(1980,1,1),"ES","",CoachExperience.Novice,"generic-v1",_date));
    }
    [Fact] public void SameProfileStartsTwoIndependentUnemployedCareersWithOwnCoachIdentities()
    {
        var p=Profile();var store=Store();var a=store.Create(Request(p,CareerStartMode.Unemployed));var b=store.Create(Request(p,CareerStartMode.Unemployed));
        Assert.NotEqual(a.Id,b.Id);Assert.NotEqual(a.Coach.Id,b.Coach.Id);Assert.NotEqual(p.Id.Value,a.Coach.Id.Value);
        Assert.Equal(p,a.Coach.ProfileSnapshot);Assert.Equal(p,b.Coach.ProfileSnapshot);Assert.Null(a.ClubId);Assert.Null(a.Contract);Assert.Equal(_date,a.CurrentDate);
        Assert.Equal(CareerEmploymentState.Unemployed,new SqliteCareerStore(Path.Combine(_root,"careers"),Source).Load(a.Id).Employment);
        var original=new SqliteWorldStore(Source).Load().Clubs[0];var changed=new Club(original.Id,"Nombre en una sola partida",original.City,original.Country,original.Reputation,original.Profile);
        new SqliteWorldStore(store.PathFor(a.Id)).Apply(new Furvo.Core.Worlds.WorldData(clubs:[changed]));
        Assert.Equal(changed.Name,new SqliteWorldStore(store.PathFor(a.Id)).Load().Clubs.Single(c=>c.Id==original.Id).Name);
        Assert.Equal(original.Name,new SqliteWorldStore(store.PathFor(b.Id)).Load().Clubs.Single(c=>c.Id==original.Id).Name);
        Assert.Equal(original.Name,new SqliteWorldStore(Source).Load().Clubs.Single(c=>c.Id==original.Id).Name);
    }
    [Fact] public void ValidSyntheticOfferSignsAndFreezesAcceptedConditionsAndSeparateBudgets()
    {
        var p=Profile();var offer=Offer(p);var store=Store();var request=Request(p,CareerStartMode.SignOffer,offer);var career=store.Create(request);
        Assert.Equal(CareerEmploymentState.Employed,career.Employment);Assert.Equal(offer.ClubId,career.ClubId);Assert.NotNull(career.Contract);
        var loaded=store.Load(career.Id);var accepted=loaded.Contract!.AcceptedOffer;
        Assert.Equal(10000m,accepted.Terms.GrossAnnualSalary);Assert.Empty(accepted.Commitments.Principles);
        Assert.Equal(offer.Commitments.Objectives,accepted.Commitments.Objectives);Assert.Equal(100000m,accepted.Resources.TransferBudget);
        Assert.Equal(200000m,accepted.Resources.PlayerWageBudget);Assert.Equal(15000m,accepted.Resources.StaffBudget);
        var changed=offer with{Terms=offer.Terms with{GrossAnnualSalary=50000m},Commitments=offer.Commitments with{Version="changed"}};
        Assert.NotEqual(changed.Terms.GrossAnnualSalary,store.Load(career.Id).Contract!.AcceptedOffer.Terms.GrossAnnualSalary);
        Assert.Equal(career.Id,new SqliteWorldStore(store.PathFor(career.Id)).ReadInfo().SaveId);
    }
    [Fact] public void RejectionPersistsDeclinedOfferButNeverCreatesActiveContract()
    {
        var p=Profile();var offer=Offer(p,false);var store=Store();var career=store.Create(Request(p,CareerStartMode.RejectOffer,offer));var loaded=store.Load(career.Id);
        Assert.Null(loaded.ClubId);Assert.Null(loaded.Contract);Assert.Equal(offer.Id,loaded.RejectedOffer!.Id);Assert.Equal(CareerEmploymentState.Unemployed,loaded.Employment);
    }
    [Fact] public void SameConfirmationIsIdempotentAndConflictingIdentityCannotOverwrite()
    {
        var p=Profile();var store=Store();var request=Request(p,CareerStartMode.Unemployed);var a=store.Create(request);var b=store.Create(request);
        Assert.Equal(a.Id,b.Id);Assert.Single(Directory.GetFiles(Path.Combine(_root,"careers"),"*.db"));
        Assert.Throws<InvalidOperationException>(()=>store.Create(request with{CoachId=CoachId.New()}));Assert.Equal(a.Coach.Id,store.Load(a.Id).Coach.Id);
    }
    [Theory][InlineData("salary")][InlineData("end")][InlineData("end-equal")][InlineData("termination")][InlineData("mandatory-content")][InlineData("mandatory-priority")]
    public void MissingRequiredTermsOrEnforceableContentCannotPublishEmployedCareers(string missing)
    {
        var p=Profile();var offer=Offer(p);offer=missing switch
        {"salary"=>offer with{Terms=offer.Terms with{GrossAnnualSalary=null}},"end"=>offer with{Terms=offer.Terms with{EndDate=null}},
        "end-equal"=>offer with{Terms=offer.Terms with{EndDate=offer.Terms.StartDate}},"termination"=>offer with{Terms=offer.Terms with{TerminationConditions=null}},
        "mandatory-content"=>offer with{Commitments=offer.Commitments with{Objectives=[offer.Commitments.Objectives[0] with{Description=""}]}},
        _=>offer with{Commitments=offer.Commitments with{Objectives=[offer.Commitments.Objectives[0] with{Priority=null}]}}};
        Assert.NotEmpty(offer.SigningBlocks());Assert.Throws<InvalidOperationException>(()=>Store().Create(Request(p,CareerStartMode.SignOffer,offer)));
        Assert.False(Directory.Exists(Path.Combine(_root,"careers")));
    }
    [Fact] public void InvalidReferencesOrUnemployedContractsDoNotLeavePartialFiles()
    {
        var p=Profile();var store=Store();var invalid=Offer(p) with{ClubId=ClubId.New()};
        Assert.Throws<InvalidOperationException>(()=>store.Create(Request(p,CareerStartMode.SignOffer,invalid)));
        Assert.Throws<InvalidOperationException>(()=>store.Create(Request(p,CareerStartMode.Unemployed,Offer(p))));
        Assert.Throws<InvalidOperationException>(()=>store.Create(Request(p,CareerStartMode.RejectOffer,Offer(p)) with{ContractId=ManagerContractId.New()}));
        Assert.False(Directory.Exists(Path.Combine(_root,"careers")));
    }
    [Fact] public void CareerCreationDoesNotModifyInitialWorldAndRetainsAllPlayersAndAttributes()
    {
        var hash=SHA256.HashData(File.ReadAllBytes(Source));var store=Store();var a=store.Create(Request(Profile(),CareerStartMode.Unemployed));
        var world=new SqliteWorldStore(store.PathFor(a.Id)).Load();Assert.Equal(20,world.Clubs.Count);Assert.Equal(480,world.Players.Count);Assert.Equal(480,world.Contracts.Count);
        Assert.All(world.Players,p=>Assert.Equal(61,p.Attributes.AllValues.Count));Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(Source)));
    }
    [Fact] public void DatabasePreventsActiveContractInUnemploymentAndRemovalOfRequiredContract()
    {
        var p=Profile();var store=Store();var a=store.Create(Request(p,CareerStartMode.Unemployed));
        using(var c=Connection(store.PathFor(a.Id)))using(var command=c.CreateCommand())
        {command.CommandText="INSERT INTO manager_contracts VALUES($id,1,$coach,$club,'2026-07-01','2027-06-30','100','EUR',1,'test','{}');";command.Parameters.AddWithValue("$id",Guid.NewGuid().ToString());command.Parameters.AddWithValue("$coach",a.Coach.Id.ToString());command.Parameters.AddWithValue("$club",Offer(p).ClubId.ToString());Assert.Throws<SqliteException>(()=>command.ExecuteNonQuery());}
        var employed=store.Create(Request(p,CareerStartMode.SignOffer,Offer(p)));
        using(var c=Connection(store.PathFor(employed.Id)))using(var command=c.CreateCommand()) { command.CommandText="DELETE FROM manager_contracts;";Assert.Throws<SqliteException>(()=>command.ExecuteNonQuery()); }
    }
    [Fact] public void ChoosingOrReturningNeverCreatesCareerAndProfileIsRetained()
    {
        var p=Profile();var q=PublishedWorldQueries.Open(Source);var flow=new CareerCreationFlow(q,new TestOffers(),Store());flow.SelectProfile(p);
        flow.SelectClub(q.GetClubs()[0].Id);Assert.False(Directory.Exists(Path.Combine(_root,"careers")));flow.BackToClubs();Assert.Equal(p,flow.Profile);Assert.Null(flow.Offer);
        var request=flow.Prepare(CareerStartMode.Unemployed);Assert.Same(request,flow.Prepare(CareerStartMode.Unemployed));var created=flow.Confirm();Assert.Equal(created.Id,flow.Confirm().Id);
    }
    [Fact] public void ProfileSnapshotInExistingCareerDoesNotFollowLaterProfileChanges()
    {
        var p=Profile();var profiles=new SqliteCoachProfileStore(Path.Combine(_root,"profiles.db"));profiles.Create(p);var store=Store();var created=store.Create(Request(p,CareerStartMode.Unemployed));
        using(var c=Connection(Path.Combine(_root,"profiles.db")))using(var cmd=c.CreateCommand())
        {cmd.CommandText="UPDATE coach_profiles SET first_name='Cambio posterior' WHERE id=$id;";cmd.Parameters.AddWithValue("$id",p.Id.ToString());cmd.ExecuteNonQuery();}
        Assert.Equal("Cambio posterior",Assert.Single(profiles.List()).FirstName);
        Assert.Equal(p.FirstName,store.Load(created.Id).Coach.ProfileSnapshot.FirstName);
    }
    [Fact] public void FailedPublicationCleansItsTemporaryFileAndSameRequestCanRetry()
    {
        var badSource=Path.Combine(_root,"incompatible-source.db");File.Copy(Source,badSource);
        using(var c=Connection(badSource))using(var cmd=c.CreateCommand()){cmd.CommandText="CREATE TABLE career_schema_version(test INTEGER);";cmd.ExecuteNonQuery();}
        var request=Request(Profile(),CareerStartMode.Unemployed);var directory=Path.Combine(_root,"careers");
        Assert.Throws<SqliteException>(()=>new SqliteCareerStore(directory,badSource).Create(request));
        Assert.Empty(Directory.GetFileSystemEntries(directory));
        Assert.Equal(request.Id,new SqliteCareerStore(directory,Source).Create(request).Id);
    }
    [Theory][InlineData(2008,2,29,2026,2,28)][InlineData(1926,2,28,2026,2,28)]
    public void ProfileAgeValidationAndSqlAgreeAtLeapAndMaximumAgeBoundaries(int y,int m,int d,int ry,int rm,int rd)
    {
        var p=new CoachProfile(CoachProfileId.New(),"Prueba","Edad",new DateOnly(y,m,d),"ES","Ciudad",CoachExperience.Novice,"generic-v1",new DateOnly(ry,rm,rd));
        var store=new SqliteCoachProfileStore(Path.Combine(_root,"profiles.db"));Assert.Equal(p,store.Create(p));Assert.Equal(p,Assert.Single(store.List()));
    }
    [Fact] public void ActualOfferSourceMatchesExplorerDraftsAndFinancialQueryForAllTwentyClubs()
    {
        var p=Profile();var queries=PublishedWorldQueries.Open(Source);var source=new InitialManagerOfferSource(queries);
        foreach(var club in queries.GetClubs())
        {
            var proposal=ClubDirectionProposalCatalog.Find(club.Id.ToString())!;var offer=source.Create(p,club.Id);
            Assert.Equal(proposal.Principles,offer.Commitments.Principles);Assert.Equal(proposal.Version,offer.Commitments.Version);
            Assert.Equal(proposal.Objectives.Select(o=>o.Code),offer.Commitments.Objectives.Select(o=>o.Code));
            Assert.All(offer.Commitments.Objectives,o=>Assert.Null(o.Mandatory));
            Assert.Equal(queries.GetFinancialResources(club.Id,offer.SeasonId),offer.Resources);
            Assert.Equal(new DateOnly(2026,7,1),offer.Terms.StartDate);Assert.Equal(new DateOnly(2027,6,30),offer.Terms.EndDate);Assert.Equal(12m,offer.Terms.CalculateDurationMonths());Assert.Equal("EUR",offer.Terms.Currency);Assert.Null(offer.Terms.GrossAnnualSalary);
            Assert.Equal(new[]{"OfferSalaryMissing"},offer.SigningBlocks());
        }
    }
    private sealed class TestOffers:IManagerOfferSource { public ManagerOffer Create(CoachProfile p,ClubId club)=>Offer(p) with{ClubId=club,Resources=Offer(p).Resources with{ClubId=club}}; }
    private static SqliteConnection Connection(string path) {var c=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=path,ForeignKeys=true,Pooling=false}.ToString());c.Open();return c;}
    public void Dispose()
    {
        var allowed=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"FurvoCareers"))+Path.DirectorySeparatorChar;
        if(!Path.GetFullPath(_root).StartsWith(allowed,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Ruta temporal ajena.");
        Directory.Delete(_root,true);
    }
}
