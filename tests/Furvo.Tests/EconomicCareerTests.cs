using System.Security.Cryptography;
using System.Text.Json;
using Furvo.Core.Careers;
using Furvo.Core.Queries;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Microsoft.Data.Sqlite;
using Xunit;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    private static string EconomicSource=>Path.Combine(AppContext.BaseDirectory,"published-v5.db");
    private SqliteCareerStore EconomicStore()=>new(Path.Combine(_root,"economic-careers"),EconomicSource);
    private static ManagerOffer EconomicOffer(CoachProfile profile)=>new InitialManagerOfferSource(PublishedWorldQueries.Open(EconomicSource)).Create(profile,PublishedWorldQueries.Open(EconomicSource).GetClubs()[0].Id);
    [Fact] public void TwentyApprovedPlansMatchEveryProposedAmountAndQuery()
    {
        using var doc=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"economic-proposal-approved.json")));
        var original=JsonSerializer.Deserialize<ClubEconomicPlan[]>(doc.RootElement.GetProperty("Clubs"))!;
        var plans=EconomicInitialWorld2026.ReadApprovedPlans();Assert.Equal(20,plans.Count);
        var queries=PublishedWorldQueries.Open(EconomicSource);var season=queries.GetSeasons()[0].Id;
        foreach(var expected in original)
        {
            var plan=plans.Single(p=>p.ClubId==expected.ClubId);Assert.Equal(expected,plan);
            var club=queries.GetClubs().Single(c=>c.Id.Value==plan.ClubId);var resource=queries.GetFinancialResources(club.Id,season);
            Assert.Equal(plan,resource.EconomicPlan);Assert.Equal(plan.InitialCash,resource.Cash);
            Assert.Equal(plan.TransferBudget,resource.TransferRemaining);Assert.Null(resource.PlayerWageRemaining);
            Assert.Null(resource.EconomicPlan!.CommittedPlayerWages);Assert.Null(resource.ManagerCommittedAnnualCost);
            Assert.Equal(plan.ShortTermDebt+plan.LongTermDebt,plan.TotalDebt);
            Assert.True(plan.PlannedUnallocatedWageCapacity<=plan.PlayerWageBudget);
            Assert.True(plan.PlanningManagerEmployerCost<plan.ForecastExpense.SportingPersonnel);
            Assert.Equal(24,queries.GetSquad(club.Id).Count);
            Assert.All(queries.GetSquad(club.Id),p=>Assert.Equal(51,queries.GetPlayer(club.Id,p.Id).AttributeGroups.Sum(g=>g.Values.Count)));
        }
    }
    [Fact] public void EveryClubOffersApprovedSalaryWithoutBackgroundOrNationalityAdvantage()
    {
        var q=PublishedWorldQueries.Open(EconomicSource);var source=new InitialManagerOfferSource(q);
        foreach(var c in q.GetClubs())
        {
            var p=Profile();var novice=source.Create(p,c.Id);
            var former=new CoachProfile(CoachProfileId.New(),"Prueba","Internacional",new(1980,1,1),"JP","Ciudad",CoachExperience.FormerPlayer,"generic-v1",_date);
            var second=source.Create(former,c.Id);Assert.Equal(novice.Terms,second.Terms);Assert.Equal(novice.Resources,second.Resources);
            Assert.Empty(novice.SigningBlocks());Assert.Equal(novice.Resources.EconomicPlan!.ProposedManagerGrossAnnualSalary,novice.Terms.GrossAnnualSalary);
            Assert.True(novice.Terms.GrossAnnualSalary>17094);Assert.Equal("EUR",novice.Terms.Currency);
            Assert.Equal(new DateOnly(2027,6,30),novice.Terms.EndDate);Assert.Equal(12m,novice.Terms.CalculateDurationMonths());
        }
    }
    [Fact] public void SignatureInitializesAllClubsAndCommitsCoachExactlyOnceWithoutPayment()
    {
        var p=Profile();var offer=EconomicOffer(p);var store=EconomicStore();var request=Request(p,CareerStartMode.SignOffer,offer);
        var career=store.Create(request);Assert.Equal(career.Id,store.Create(request).Id);
        var restored=store.Load(career.Id);Assert.Equal(offer.Terms,restored.Contract!.AcceptedOffer.Terms);
        Assert.Equal(offer.Resources,restored.Contract.AcceptedOffer.Resources);
        var resources=SqliteEconomyStore.ReadResources(store.PathFor(career.Id));Assert.Equal(20,resources.Count);
        var selected=resources.Single(r=>r.ClubId==career.ClubId);
        Assert.Equal(offer.Resources.EconomicPlan!.PlanningManagerEmployerCost,selected.ManagerCommittedAnnualCost);
        Assert.Equal(offer.Resources.Cash,selected.Cash);Assert.Equal(offer.Resources.TransferBudget,selected.TransferRemaining);
        Assert.Equal(offer.Resources.EconomicPlan.ForecastExpense.SportingPersonnel,selected.EconomicPlan!.ForecastExpense.SportingPersonnel);
        Assert.All(resources.Where(r=>r.ClubId!=career.ClubId),r=>Assert.Null(r.ManagerCommittedAnnualCost));
        Assert.Single(Directory.GetFiles(Path.Combine(_root,"economic-careers"),"*.db"));
    }
    [Fact] public void TwoSignedCareersHaveIndependentCashAndUnchangedPublication()
    {
        var hash=SHA256.HashData(File.ReadAllBytes(EconomicSource));var p=Profile();var store=EconomicStore();var offer=EconomicOffer(p);
        var a=store.Create(Request(p,CareerStartMode.SignOffer,offer));var b=store.Create(Request(p,CareerStartMode.SignOffer,offer));
        using(var c=Connection(store.PathFor(a.Id)))using(var cmd=c.CreateCommand())
        {cmd.CommandText="UPDATE club_economics SET cash_cents=cash_cents-100 WHERE club_id=$club;";cmd.Parameters.AddWithValue("$club",offer.ClubId.ToString());cmd.ExecuteNonQuery();}
        Assert.Equal(offer.Resources.Cash-1,SqliteEconomyStore.ReadResources(store.PathFor(a.Id)).Single(r=>r.ClubId==offer.ClubId).Cash);
        Assert.Equal(offer.Resources.Cash,SqliteEconomyStore.ReadResources(store.PathFor(b.Id)).Single(r=>r.ClubId==offer.ClubId).Cash);
        Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(EconomicSource)));Assert.NotEqual(a.Coach.Id,b.Coach.Id);
    }
    [Theory][InlineData(CareerStartMode.Unemployed)][InlineData(CareerStartMode.RejectOffer)]
    public void UnemploymentInitializesWorldEconomyWithoutCoachBudgetOrCommitment(CareerStartMode mode)
    {
        var p=Profile();var store=EconomicStore();var career=store.Create(Request(p,mode,mode==CareerStartMode.RejectOffer?EconomicOffer(p):null));
        var loaded=store.Load(career.Id);Assert.Null(loaded.Contract);Assert.Null(loaded.ClubId);
        Assert.Equal(20,SqliteEconomyStore.ReadResources(store.PathFor(career.Id)).Count);
        Assert.All(SqliteEconomyStore.ReadResources(store.PathFor(career.Id)),r=>Assert.Null(r.ManagerCommittedAnnualCost));
    }
    [Theory][InlineData("budget")][InlineData("salary")][InlineData("plan")]
    public void TamperedOfferRollsBackWithoutPublishingPartialCareer(string field)
    {
        var p=Profile();var offer=EconomicOffer(p);var plan=offer.Resources.EconomicPlan!;
        offer=field switch{
            "budget"=>offer with{Resources=offer.Resources with{TransferBudget=1}},
            "plan"=>offer with{Resources=offer.Resources with{EconomicPlan=plan with{InitialCash=1}}},
            _=>offer with{Terms=offer.Terms with{GrossAnnualSalary=999999m},SalaryPolicy=null}};
        Assert.Throws<InvalidOperationException>(()=>EconomicStore().Create(Request(p,CareerStartMode.SignOffer,offer)));
        var folder=Path.Combine(_root,"economic-careers");Assert.True(!Directory.Exists(folder)||Directory.GetFiles(folder).Length==0);
    }
    [Fact] public void EconomicSqlIntegrityAndHistoricalPlanAreProtected()
    {
        var p=Profile();var store=EconomicStore();var career=store.Create(Request(p,CareerStartMode.Unemployed));
        using var c=Connection(store.PathFor(career.Id));
        foreach(var sql in new[]{"UPDATE club_economics SET cash_cents=-1;","UPDATE club_economics SET plan_json='{}';","INSERT INTO club_economics SELECT club_id,season_id,plan_json,0,0,NULL FROM club_economics LIMIT 1;"})
        {using var cmd=c.CreateCommand();cmd.CommandText=sql;Assert.Throws<SqliteException>(()=>cmd.ExecuteNonQuery());}
    }
    [Fact] public void FailureDuringEconomicCommitLeavesNoCareerOrPartialFile()
    {
        var failingSource=Path.Combine(_root,"failing-world.db");File.Copy(EconomicSource,failingSource);
        using(var c=Connection(failingSource))using(var command=c.CreateCommand())
        {
            command.CommandText="CREATE TRIGGER simulated_failure AFTER UPDATE OF manager_cost_cents ON club_economics BEGIN SELECT RAISE(ABORT,'simulated economic failure'); END;";
            command.ExecuteNonQuery();
        }
        var before=SHA256.HashData(File.ReadAllBytes(failingSource));var directory=Path.Combine(_root,"failed-careers");var store=new SqliteCareerStore(directory,failingSource);
        var p=Profile();Assert.Throws<SqliteException>(()=>store.Create(Request(p,CareerStartMode.SignOffer,EconomicOffer(p))));
        Assert.Empty(Directory.GetFiles(directory));Assert.Equal(before,SHA256.HashData(File.ReadAllBytes(failingSource)));
        Assert.All(SqliteEconomyStore.ReadResources(failingSource),r=>Assert.Null(r.ManagerCommittedAnnualCost));
    }
    [Fact] public void PublishingV5PreservesV4AndRejectsOverwriteAndModifiedOrigin()
    {
        var hash=SHA256.HashData(File.ReadAllBytes(Source));var destination=Path.Combine(_root,"new-v5.db");
        var first=EconomicInitialWorld2026.LoadOrCreate(Source,destination);var bytes=SHA256.HashData(File.ReadAllBytes(destination));
        EconomicInitialWorld2026.LoadOrCreate(Source,destination);Assert.Equal(bytes,SHA256.HashData(File.ReadAllBytes(destination)));
        Assert.Equal(InitialSquads2026.Fingerprint(new SqliteWorldStore(Source).Load()),InitialSquads2026.Fingerprint(first.Load()));
        Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(Source)));
        Assert.Throws<InvalidDataException>(()=>EconomicInitialWorld2026.LoadOrCreate(EconomicSource,Path.Combine(_root,"invalid.db")));
        Assert.Throws<InvalidDataException>(()=>EconomicInitialWorld2026.LoadOrCreate(Source,Source));
        Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(Source)));
    }
}
