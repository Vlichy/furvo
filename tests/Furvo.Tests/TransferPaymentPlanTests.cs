using System.Text.Json;
using Furvo.Core.Domain;
using Furvo.Core.Market;
using Furvo.Core.Queries;
using Xunit;
namespace Furvo.Tests;
public sealed class TransferPaymentPlanTests
{
    private static readonly DateOnly Date=new(2026,7,1);
    [Theory][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)]
    public void PresetsIncludeAdvanceAndExactCalendar(int count)
    {
        var plan=new TransferPaymentPlan(TransferPaymentPlans.Version,Date,TransferPaymentPlans.Preset(count));
        var rows=TransferPaymentPlans.Generate(8_000_000,plan);Assert.Equal(count,rows.Count);Assert.Equal(8_000_000,rows.Sum(p=>p.Amount));Assert.Equal(Date,rows[0].Due);
        Assert.Equal(Date.AddMonths(12*(count-1)),rows[^1].Due);Assert.Equal(count==4?3_200_000:count==1?8_000_000:4_000_000,rows[0].Amount);
        Assert.Equal(rows.Select(r=>r.Id),TransferPaymentPlans.Generate(8_000_000,plan,rows).Select(r=>r.Id));
    }
    [Theory][InlineData("0.01")][InlineData("0.02")][InlineData("21")][InlineData("1000.01")][InlineData("1000000000")]
    public void ExactCentsWithoutUniversalMinimum(string value)
    {
        var amount=decimal.Parse(value,System.Globalization.CultureInfo.InvariantCulture);
        foreach(var count in new[]{1,2,3,4}){var rows=TransferPaymentPlans.Generate(amount,new(TransferPaymentPlans.Version,Date,TransferPaymentPlans.Preset(count)));Assert.Equal(amount,rows.Sum(p=>p.Amount));Assert.All(rows,r=>Assert.True(r.Amount>0));}
    }
    [Fact] public void CustomAdvanceAndMonthsPersistAsStructuredProposal()
    {
        var plan=new TransferPaymentPlan(TransferPaymentPlans.Version,Date,new[]{new PaymentShare(0,60),new PaymentShare(6,20),new PaymentShare(18,20)});
        var rows=TransferPaymentPlans.Generate(1000.01m,plan);Assert.Equal(600,rows[0].Amount);Assert.Equal(200.01m,rows[^1].Amount);Assert.Equal(Date.AddMonths(18),rows[^1].Due);
        Assert.Equal(JsonSerializer.Serialize(plan),JsonSerializer.Serialize(JsonSerializer.Deserialize<TransferPaymentPlan>(JsonSerializer.Serialize(plan))));
    }
    [Theory][InlineData(-1,50,12,50)][InlineData(0,-1,12,101)][InlineData(0,50,0,50)][InlineData(0,50,37,50)][InlineData(0,40,12,40)][InlineData(1,50,12,50)]
    public void InvalidSharesAndDatesAreRejected(int firstMonth,int firstPercent,int secondMonth,int secondPercent)=>Assert.Throws<InvalidDataException>(()=>TransferPaymentPlans.Generate(100,new(TransferPaymentPlans.Version,Date,new[]{new PaymentShare(firstMonth,firstPercent),new PaymentShare(secondMonth,secondPercent)})));
    [Fact] public void NegativeAmountAndTechnicalCapRejected()
    {
        var plan=new TransferPaymentPlan(TransferPaymentPlans.Version,Date,TransferPaymentPlans.Preset(2));Assert.Throws<InvalidDataException>(()=>TransferPaymentPlans.Generate(-1,plan));Assert.Throws<InvalidDataException>(()=>TransferPaymentPlans.Generate(501,plan,policy:new(500)));
    }
    [Fact] public void EndOfMonthUsesCalendarMonthsAndLeapYear()
    {
        var date=new DateOnly(2028,1,31);var rows=TransferPaymentPlans.Generate(100,new(TransferPaymentPlans.Version,date,new[]{new PaymentShare(0,50),new PaymentShare(1,50)}));Assert.Equal(new DateOnly(2028,2,29),rows[1].Due);
    }
    [Fact] public void FinancialReviewDoesNotInventUnknownFactsOrBlockProposal()
    {
        var club=new ClubId(Guid.NewGuid());var resources=new ClubFinancialResources(club,new SeasonId(Guid.NewGuid())){Cash=40,TransferRemaining=80,Currency="EUR"};var plan=new TransferPaymentPlan(TransferPaymentPlans.Version,Date,TransferPaymentPlans.Preset(2));
        var terms=new TransferTerms("EUR",100,TransferPaymentPlans.Generate(100,plan),Array.Empty<PlayerId>(),Array.Empty<TransferBonus>(),Array.Empty<FutureFeeClause>(),null,null,ArrivalKind.Immediate,null,Date.AddDays(7),PaymentPlan:plan);
        var review=TransferFinancialReview.Assess(terms,resources,Date);Assert.Equal(50,review.Advance);Assert.Equal(50,review.Outstanding);Assert.Equal(FinancialCheckState.ExceedsKnownAmount,review.Liquidity);Assert.Equal(FinancialCheckState.ExceedsKnownAmount,review.Budget);Assert.Equal(FinancialCheckState.Pending,TransferFinancialReview.Assess(terms,null,Date).Liquidity);
        Assert.Equal(FinancialCheckState.Pending,TransferFinancialReview.Assess(terms,resources with{Currency="USD"},Date).Budget);
    }
    private static MarketPlayer Player(string name,string category,PlayerPosition group=PlayerPosition.Defender)
    {
        var player=new Player(new PlayerId(Guid.NewGuid()),name,new DateOnly(2006,1,1),"España",group,new PlayerAttributes(PlayerAttributeCatalog.All.ToDictionary(a=>a.Code,_=>10)),new PlayerPositions(PlayerRole.CentreBack));
        return new(WorldQueries.ProjectPlayer(player,null,Date),Guid.NewGuid(),"Equipo "+category,category,null);
    }
    [Theory][InlineData("alvaro")][InlineData("SÁNCHEZ")][InlineData("  Álvaro   Sánchez ")][InlineData("nchez")]
    public void SearchNormalizesNamesAndSpaces(string name)
    {
        var rows=new[]{new TransferExchangeCandidate(Player("Álvaro Sánchez","First"),false),new TransferExchangeCandidate(Player("Otro","Senior"),false)};
        Assert.Single(TransferExchangeFilters.Search(rows,name));
    }
    [Theory][InlineData(ExchangeTeamScope.First,"First")][InlineData(ExchangeTeamScope.Reserve,"Senior")][InlineData(ExchangeTeamScope.Academy,"Juvenil")]
    public void TeamFiltersUseActualHierarchy(ExchangeTeamScope scope,string category)
    {
        var rows=new[]{"First","Senior","Juvenil","Unknown"}.Select(c=>new TransferExchangeCandidate(Player("Nombre",c),false)).ToArray();Assert.Equal(category,Assert.Single(TransferExchangeFilters.Search(rows,"nom",PlayerPosition.Defender,scope)).Player.Category);Assert.Empty(TransferExchangeFilters.Search(rows,"",PlayerPosition.Goalkeeper,scope));
    }
    [Fact] public void SelectionDraftPreservesFiltersAndCancellationWithoutDuplicates()
    {
        var a=new PlayerId(Guid.NewGuid());var b=new PlayerId(Guid.NewGuid());var confirmed=new[]{a,a};var draft=new TransferExchangeDraft(confirmed);Assert.Single(draft.Selected);draft.Toggle(b);Assert.Equal(2,draft.Selected.Count);draft.Toggle(a);Assert.Equal(b,Assert.Single(draft.Selected));Assert.Equal(new[]{a,a},confirmed);
    }
    [Fact] public void FilterDoesNotExposeHiddenAttributeContainers()
    {
        var row=Assert.Single(TransferExchangeFilters.Search(new[]{new TransferExchangeCandidate(Player("Público","First"),false)},"publico"));Assert.Equal(51,row.Player.Details.AttributeGroups.Sum(g=>g.Values.Count));Assert.DoesNotContain(row.Player.Details.AttributeGroups,g=>g.Category==AttributeCategory.Hidden);
        foreach(var hidden in PlayerAttributeCatalog.All.Where(a=>a.IsHidden))Assert.DoesNotContain("\""+hidden.Code+"\"",JsonSerializer.Serialize(row));Assert.DoesNotContain(typeof(PlayerSummary).GetProperties(),p=>p.Name=="Attributes");
    }
}
public sealed partial class CareerCreationTests
{
    [Fact] public void EditablePaymentsAndSelectionsPersistWithoutExecutingTransfer()
    {
        var f=TransferFixture();var data=f.Store.Participants();var plan=new TransferPaymentPlan(TransferPaymentPlans.Version,data.Date,new[]{new PaymentShare(0,60),new PaymentShare(6,20),new PaymentShare(24,20)});
        var own=data.Players.Where(p=>p.Details.Club?.Id==data.ManagedClub).Take(2).Select(p=>p.Details.Player.Id).ToArray();var before=TransferBusinessFingerprint(f.File);
        var terms=f.Terms with{FixedAmount=1000.01m,Payments=TransferPaymentPlans.Generate(1000.01m,plan),PaymentPlan=plan,OfferedPlayers=own};var saved=f.Store.Draft(Guid.NewGuid(),0,f.Target,terms,Guid.NewGuid());f.Store.Send(saved.Id,1,Guid.NewGuid());
        var history=f.Store.History(saved.Id);Assert.Equal(2,history.Count);Assert.Equal(JsonSerializer.Serialize(terms),JsonSerializer.Serialize(history[^1].Terms));Assert.Equal(before,TransferBusinessFingerprint(f.File));
    }
    [Fact] public void PersistenceRejectsTamperedNewScheduleButPreservesLegacy()
    {
        var f=TransferFixture();var date=f.Store.Participants().Date;var plan=new TransferPaymentPlan(TransferPaymentPlans.Version,date,TransferPaymentPlans.Preset(3));var payments=TransferPaymentPlans.Generate(f.Terms.FixedAmount,plan);
        var invalid=f.Terms with{PaymentPlan=plan,Payments=payments.Select((p,i)=>i==0?p with{Due=date.AddDays(-1)}:p).ToArray()};Assert.Throws<InvalidDataException>(()=>f.Store.Draft(Guid.NewGuid(),0,f.Target,invalid,Guid.NewGuid()));
        invalid=f.Terms with{PaymentPlan=plan with{ReferenceDate=date.AddDays(1)},Payments=payments};Assert.Throws<InvalidDataException>(()=>f.Store.Draft(Guid.NewGuid(),0,f.Target,invalid,Guid.NewGuid()));
        Assert.Throws<InvalidDataException>(()=>f.Store.Draft(Guid.NewGuid(),0,f.Target,f.Terms with{Arrival=ArrivalKind.FutureDate,IntendedArrival=date.AddMonths(1)},Guid.NewGuid()));
        var legacy=f.Terms with{Payments=TransferOfferPresets.Payments(f.Terms.FixedAmount,date,10)};var saved=f.Store.Draft(Guid.NewGuid(),0,f.Target,legacy,Guid.NewGuid());Assert.Null(f.Store.History(saved.Id).Single().Terms.PaymentPlan);Assert.Equal(JsonSerializer.Serialize(legacy),JsonSerializer.Serialize(f.Store.History(saved.Id).Single().Terms));
    }
}
