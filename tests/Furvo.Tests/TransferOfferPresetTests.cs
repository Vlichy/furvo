using System.Security.Cryptography;
using System.Text.Json;
using Furvo.Core.Domain;
using Furvo.Core.Market;
using Furvo.Core.Queries;
using Furvo.Persistence;
using Xunit;
namespace Furvo.Tests;

public sealed class TransferOfferPresetTests
{
    private static readonly DateOnly Date=new(2026,7,1);
    [Theory][InlineData(2)][InlineData(6)][InlineData(10)]
    public void DeferredScheduleUsesCareerCalendarExactSumAndLastRounding(int years)
    {
        foreach(var amount in new[]{0.01m,0.02m,1m,1000.01m,1_000_000_000m}){
            var rows=TransferOfferPresets.Payments(amount,Date,years);
            Assert.Equal(amount,rows.Sum(r=>r.Amount));Assert.All(rows,r=>{Assert.True(r.Amount>0);Assert.True(r.Due>Date);Assert.Equal(TransferPaymentState.Proposed,r.State);});
            Assert.Equal(Date.AddYears(years),rows[^1].Due);Assert.Equal(rows.Count,rows.Select(r=>r.Id).Distinct().Count());
            Assert.Equal(Enumerable.Range(1,rows.Count),rows.Select(r=>r.Order));
            var again=TransferOfferPresets.Payments(amount,Date,years,rows);Assert.Equal(rows.Select(r=>r.Id),again.Select(r=>r.Id));
        }
    }
    [Fact] public void SinglePaymentIsAutomaticAndZeroDoesNotInventAPayment()
    {
        var row=Assert.Single(TransferOfferPresets.Payments(123.45m,Date));Assert.Equal(123.45m,row.Amount);Assert.Equal(Date,row.Due);
        Assert.Empty(TransferOfferPresets.Payments(0,Date));Assert.Throws<InvalidDataException>(()=>TransferOfferPresets.Payments(100,Date,3));
    }
    [Theory][InlineData("1000000000")][InlineData("1000000000,00")][InlineData("999999999.99")]
    public void TechnicalMaximumIsAcceptedIndependentlyOfClubBudget(string text)=>Assert.InRange(TransferRules.ParseMoney(text),0m,1_000_000_000m);
    [Theory][InlineData("-1")][InlineData("1000000000.01")][InlineData("1000000001")][InlineData("999999999999999999999999999999999999999999999")]
    public void NegativeOverMaximumAndOverflowAreRejected(string text)=>Assert.Throws<InvalidDataException>(()=>TransferRules.ParseMoney(text));
    [Fact] public void ConfigurablePolicyIsSharedByMoneyTermsAndRightModification()
    {
        var policy=new TransferMoneyPolicy(500);Assert.Equal(500,TransferRules.ParseMoney("500",policy));Assert.Throws<InvalidDataException>(()=>TransferRules.ParseMoney("500.01",policy));
        Assert.Throws<InvalidDataException>(()=>TransferOfferPresets.Payments(501,Date,2,policy:policy));Assert.Throws<InvalidDataException>(()=>TransferOfferPresets.Buyback(501,Date,1,policy));
        var right=new ContractualTransferRight(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),new PlayerId(Guid.NewGuid()),new ClubId(Guid.NewGuid()),new ClubId(Guid.NewGuid()),FutureFeeBasis.FutureTransferFee,20,"fixed cash",Date,Date.AddYears(1),State:ContractRightState.Active);
        Assert.Throws<InvalidDataException>(()=>right.Reduce(new(Guid.NewGuid(),right.Id,1,right.Debtor,right.Beneficiary,10,501,"EUR",true,true),Date,policy));
    }
    [Fact] public void StandardSaleAndProfitKeepDifferentDefinitionsAndAcquisitionBasis()
    {
        var seller=new ClubId(Guid.NewGuid());var player=new PlayerId(Guid.NewGuid());
        var sale=TransferOfferPresets.FutureFee(FutureFeeBasis.FutureTransferFee,20,seller,player,Date,100);
        var profit=TransferOfferPresets.FutureFee(FutureFeeBasis.ContractualProfit,30,seller,player,Date,100);
        Assert.Equal(TransferOfferPresets.SaleCode,sale.StandardCode);Assert.Null(sale.AcquisitionFixedBasis);Assert.Equal(100,profit.AcquisitionFixedBasis);
        Assert.Equal(DateOnly.MaxValue,sale.Until);Assert.NotEqual(sale.Calculation,profit.Calculation);
        TransferOfferPresets.ValidateStandard(sale,100);TransferOfferPresets.ValidateStandard(profit,100);
        Assert.Throws<InvalidDataException>(()=>TransferOfferPresets.ValidateStandard(profit,101));
        Assert.Throws<InvalidDataException>(()=>TransferOfferPresets.ValidateStandard(sale with{Calculation="otra base"},100));
        Assert.Throws<InvalidDataException>(()=>TransferOfferPresets.FutureFee(FutureFeeBasis.FutureTransferFee,-10,seller,player,Date,100));
        Assert.Throws<InvalidDataException>(()=>TransferOfferPresets.FutureFee(FutureFeeBasis.FutureTransferFee,101,seller,player,Date,100));
    }
    [Theory][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(5)][InlineData(10)]
    public void StandardBuybackAndMatchingHaveCalendarDurationsAndVersionedTerms(int years)
    {
        var buy=TransferOfferPresets.Buyback(500,Date,years);var matching=TransferOfferPresets.Matching(Date,years,7);
        Assert.Equal(Date.AddYears(years).AddDays(-1),buy.Until);Assert.Equal(buy.Until,matching.Until);
        Assert.Equal(TransferOfferPresets.BuybackCode,buy.StandardCode);Assert.Equal(TransferOfferPresets.MatchingCode,matching.StandardCode);
        Assert.Equal(7,matching.ResponseDays);Assert.Throws<InvalidDataException>(()=>TransferOfferPresets.Matching(Date,years,0));
    }
    [Fact] public void ExchangeSelectionUsesRealClubMembershipAndOnlyRecordedSellerInterest()
    {
        var club=new ClubSummary(new ClubId(Guid.NewGuid()),"Propio",null,2);var other=club with{Id=new ClubId(Guid.NewGuid()),Name="Externo"};
        MarketPlayer Player(string name,ClubSummary owner){var role=PlayerRole.CentreBack;var p=new Furvo.Core.Domain.Player(new PlayerId(Guid.NewGuid()),name,new DateOnly(2005,1,1),"España",PlayerPosition.Defender,new PlayerAttributes(PlayerAttributeCatalog.All.ToDictionary(a=>a.Code,_=>10)),new PlayerPositions(role));return new(WorldQueries.ProjectPlayer(p,owner,Date),owner.Id.Value,owner.Name,"First",null);}
        var alpha=Player("Alfa",club);var zeta=Player("Zeta",club);var target=Player("Objetivo",other);var data=new MarketSnapshot(Guid.NewGuid(),Date,new[]{club,other},new[]{zeta,alpha,target},club.Id);
        var normal=TransferExchangeSelection.Candidates(data,club.Id,other.Id,target.Details.Player.Id);Assert.Equal(new[]{"Alfa","Zeta"},normal.Select(c=>c.Player.Details.Player.Name));Assert.All(normal,c=>Assert.False(c.Interested));
        var actual=new RecordedTransferInterest(other.Id,zeta.Details.Player.Id,Date,"fixture:registered-interest");
        var interested=TransferExchangeSelection.Candidates(data,club.Id,other.Id,target.Details.Player.Id,new[]{actual});Assert.True(interested[0].Interested);Assert.Equal(zeta.Details.Player.Id,interested[0].Player.Details.Player.Id);
        Assert.All(TransferExchangeSelection.Candidates(data,club.Id,other.Id,target.Details.Player.Id,new[]{actual with{Club=club.Id},actual with{RecordedOn=Date.AddDays(1)}}),c=>Assert.False(c.Interested));
        Assert.DoesNotContain(normal,c=>c.Player.Details.Player.Id==target.Details.Player.Id);
    }
}
public sealed partial class CareerCreationTests
{
    [Fact] public void AutomaticOfferPresetsPersistAndSendingDoesNotChangeBusinessData()
    {
        var f=TransferFixture();var data=f.Store.Participants();var seller=data.Players.Single(p=>p.Details.Player.Id==f.Target).Details.Club!.Id;
        var before=TransferBusinessFingerprint(f.File);
        var terms=f.Terms with{FixedAmount=1000.01m,Payments=TransferOfferPresets.Payments(1000.01m,data.Date,6),FutureFees=new[]{TransferOfferPresets.FutureFee(FutureFeeBasis.FutureTransferFee,20,seller,f.Target,data.Date,1000.01m),TransferOfferPresets.FutureFee(FutureFeeBasis.ContractualProfit,30,seller,f.Target,data.Date,1000.01m)},Buyback=TransferOfferPresets.Buyback(5000,data.Date,3),Matching=TransferOfferPresets.Matching(data.Date,2,14)};
        var saved=f.Store.Draft(Guid.NewGuid(),0,f.Target,terms,Guid.NewGuid());var loaded=f.Store.History(saved.Id).Single();
        Assert.Equal(JsonSerializer.Serialize(terms),JsonSerializer.Serialize(loaded.Terms));f.Store.Send(saved.Id,1,Guid.NewGuid());Assert.Equal(before,TransferBusinessFingerprint(f.File));
    }
    [Fact] public void NewCapIsEnforcedInPersistenceAndOldLargeOffersRemainReadableAndWithdrawable()
    {
        var f=TransferFixture();var high=new SqliteTransferStore(f.File,new TransferMoneyPolicy(2_000_000_000m));var amount=1_500_000_000m;
        var old=f.Terms with{FixedAmount=amount,Payments=new[]{new TransferPayment(Guid.NewGuid(),1,amount,new DateOnly(2026,7,1))}};
        var saved=high.Draft(Guid.NewGuid(),0,f.Target,old,Guid.NewGuid());high.Send(saved.Id,1,Guid.NewGuid());
        var loaded=f.Store.History(saved.Id).Last();Assert.Equal(amount,loaded.Terms.FixedAmount);
        Assert.Throws<InvalidDataException>(()=>f.Store.Draft(Guid.NewGuid(),0,f.Target,old,Guid.NewGuid()));
        Assert.Equal(NegotiationState.Withdrawn,f.Store.Withdraw(saved.Id,loaded.Revision,Guid.NewGuid()).State);
    }
    [Fact] public void LegacyCustomConditionsAreNotRewrittenOnRead()
    {
        var f=TransferFixture();var data=f.Store.Participants();var seller=data.Players.Single(p=>p.Details.Player.Id==f.Target).Details.Club!.Id;
        var original=f.Terms with{Payments=new[]{new TransferPayment(Guid.NewGuid(),1,60,data.Date),new TransferPayment(Guid.NewGuid(),2,40,data.Date.AddYears(1))},FutureFees=new[]{new FutureFeeClause(Guid.NewGuid(),seller,f.Target,17,FutureFeeBasis.ContractualProfit,"definición anterior personalizada",data.Date,data.Date.AddYears(4),"activación anterior")},Buyback=new(900,data.Date,data.Date.AddYears(2),"condición anterior"),Matching=new(data.Date.AddYears(3),9,"notificación anterior","igualación anterior")};
        var d=f.Store.Draft(Guid.NewGuid(),0,f.Target,original,Guid.NewGuid());var hash=SHA256.HashData(File.ReadAllBytes(f.File));var reread=f.Store.History(d.Id).Single();
        Assert.Equal(JsonSerializer.Serialize(original),JsonSerializer.Serialize(reread.Terms));Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(f.File)));Assert.Null(reread.Terms.FutureFees[0].StandardCode);
    }
}
