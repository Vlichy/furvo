using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Furvo.Core.Market;
using Furvo.Persistence;
using Xunit;
namespace Furvo.Tests;

public sealed class TransferRulesTests
{
    private readonly ClubId _seller=new(Guid.Parse("10000000-0000-4000-8000-000000000001"));
    private readonly PlayerId _player=new(Guid.Parse("20000000-0000-4000-8000-000000000001"));
    private readonly DateOnly _date=new(2026,7,1);
    private TransferTerms Terms(decimal amount=100)=>new("EUR",amount,new[]{new TransferPayment(Guid.NewGuid(),1,amount,_date)},Array.Empty<PlayerId>(),Array.Empty<TransferBonus>(),Array.Empty<FutureFeeClause>(),null,null,ArrivalKind.Immediate,null,_date.AddDays(10));
    [Theory][InlineData("10",10)][InlineData("10,50",10.5)][InlineData("10.50",10.5)][InlineData("0",0)]
    public void MoneyParsesWithoutImplicitConversion(string text,double value)=>Assert.Equal((decimal)value,TransferRules.ParseMoney(text));
    [Theory][InlineData("-1")][InlineData("1.000")][InlineData("1.2300")][InlineData("NaN")][InlineData("1.234")][InlineData("1.000,50")][InlineData("999999999999999999999999999999999")]
    public void InvalidMoneyRejected(string text)=>Assert.Throws<InvalidDataException>(()=>TransferRules.ParseMoney(text));
    [Theory][InlineData(0)][InlineData(-1)][InlineData(101)]
    public void InvalidPercentagesRejected(int n)=>Assert.Throws<InvalidDataException>(()=>TransferRules.Percentage(n));
    [Fact] public void PaymentsSupportSingleCustomInstallmentsAndValidateTotalOrderAndDates()
    {
        Terms().Validate(_date,_seller,_player,true);
        var t=Terms(120) with{Payments=new[]{new TransferPayment(Guid.NewGuid(),1,40,_date),new TransferPayment(Guid.NewGuid(),2,40,_date.AddMonths(12)),new TransferPayment(Guid.NewGuid(),3,40,_date.AddMonths(24))}};
        t.Validate(_date,_seller,_player,true);Assert.Equal(40,t.Immediate(_date));
        Assert.Throws<InvalidDataException>(()=>(t with{FixedAmount=121}).Validate(_date,_seller,_player,true));
        Assert.Throws<InvalidDataException>(()=>(t with{Payments=new[]{new TransferPayment(Guid.NewGuid(),1,120,_date.AddDays(-1))}}).Validate(_date,_seller,_player,true));
        Assert.Throws<InvalidDataException>(()=>(t with{Payments=new[]{new TransferPayment(Guid.NewGuid(),1,40,_date.AddDays(2)),new TransferPayment(Guid.NewGuid(),2,80,_date)}}).Validate(_date,_seller,_player,true));
    }
    [Fact] public void ProposalCannotClaimThatAPaymentHasAlreadyExecuted()
    {var t=Terms();Assert.Equal(TransferPaymentState.Proposed,t.Payments[0].State);Assert.Throws<InvalidDataException>(()=>(t with{Payments=new[]{t.Payments[0] with{State=TransferPaymentState.Paid}}}).Validate(_date,_seller,_player,true));}
    [Fact] public void ZeroMoneyRequiresExchangeOrExplicitFreeProposalWithoutInventingEligibility()
    {
        var zero=Terms(0) with{Payments=Array.Empty<TransferPayment>()};
        Assert.Throws<InvalidDataException>(()=>zero.Validate(_date,_seller,_player,false));Assert.Throws<InvalidDataException>(()=>zero.Validate(_date,_seller,_player,true));
        (zero with{OfferedPlayers=new[]{new PlayerId(Guid.NewGuid())}}).Validate(_date,_seller,_player,true);
        (zero with{ExplicitFreeProposal=true}).Validate(_date,_seller,_player,true);
    }
    [Fact] public void MultipleIndependentBonusesAndDifferentFeeBasesDoNotBecomeAGuaranteedPrice()
    {
        var b=new TransferBonus(Guid.NewGuid(),BonusObjective.OfficialAppearances,30,500,_date,_date.AddYears(1),1,false,"verified-scope");
        var fee=new FutureFeeClause(Guid.NewGuid(),_seller,_player,80,FutureFeeBasis.FutureTransferFee,"fixed monetary transfer fee",_date,_date.AddYears(5),"next completed sale");
        var t=Terms() with{Bonuses=new[]{b,b with{Id=Guid.NewGuid(),Threshold=60}},FutureFees=new[]{fee,fee with{Id=Guid.NewGuid(),Basis=FutureFeeBasis.ContractualProfit,Calculation="positive surplus, deductions defined separately"}}};
        t.Validate(_date,_seller,_player,true);Assert.Equal(100,t.FixedAmount);Assert.Equal(1000,t.MaximumBonuses);Assert.Equal(2,t.FutureFees.Count);
        Assert.Throws<InvalidDataException>(()=>(t with{FutureFees=new[]{fee,fee with{Id=Guid.NewGuid()}}}).Validate(_date,_seller,_player,true));
        Assert.Throws<InvalidDataException>(()=>(t with{Bonuses=new[]{b with{MaximumActivations=2}}}).Validate(_date,_seller,_player,true));
    }
    [Fact] public void BuybackMatchingAndArrivalAreOptionalExplicitAndDateValidated()
    {
        var t=Terms() with{Buyback=new(900,_date,_date.AddYears(2),"subject to player agreement"),Matching=new(_date.AddYears(2),7,"written notice","equal monetary terms"),Arrival=ArrivalKind.FutureDate,IntendedArrival=_date.AddMonths(2)};
        Assert.Throws<InvalidDataException>(()=>t.Validate(_date,_seller,_player,true)); // El calendario original precedía a la incorporación.
        t=t with{Payments=t.Payments.Select(p=>p with{Due=_date.AddMonths(2)}).ToArray()};
        t.Validate(_date,_seller,_player,true);
        Assert.Throws<InvalidDataException>(()=>(t with{IntendedArrival=null}).Validate(_date,_seller,_player,true));
        Assert.Throws<InvalidDataException>(()=>(t with{Buyback=t.Buyback! with{Until=_date.AddDays(-1)}}).Validate(_date,_seller,_player,true));
        Assert.Throws<InvalidDataException>(()=>(t with{Matching=t.Matching! with{ResponseDays=0}}).Validate(_date,_seller,_player,true));
    }
    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)][InlineData(6)][InlineData(7)][InlineData(8)][InlineData(9)][InlineData(10)][InlineData(11)]
    public void MalformedOptionalTermsAreRejected(int kind)
    {
        var t=Terms();var shared=Guid.NewGuid();
        t=kind switch{
            0=>t with{Payments=new[]{new TransferPayment(shared,1,-100,_date)}},
            1=>t with{Payments=new[]{new TransferPayment(shared,1,50,_date),new TransferPayment(shared,2,50,_date)}},
            2=>t with{Payments=new[]{new TransferPayment(Guid.NewGuid(),1,50,_date),new TransferPayment(Guid.NewGuid(),1,50,_date)}},
            3=>t with{OfferedPlayers=new[]{new PlayerId(shared),new PlayerId(shared)}},
            4=>t with{OfferedPlayers=new[]{_player}},
            5=>t with{Currency="EU"},
            6=>t with{Expires=_date.AddDays(-1)},
            7=>t with{FutureFees=new[]{new FutureFeeClause(shared,_seller,_player,0,FutureFeeBasis.FutureTransferFee,"cash",_date,_date.AddYears(1),"sale")}},
            8=>t with{Bonuses=new[]{new TransferBonus(shared,BonusObjective.Goals,0,100,_date,_date.AddYears(1),1,false,"known")}},
            9=>t with{Bonuses=new[]{new TransferBonus(shared,BonusObjective.Goals,1,100,_date,_date.AddYears(1),0,false,"known")}},
            10=>t with{Buyback=new(-1,_date,_date.AddYears(1),"conditional")},
            _=>t with{Matching=new(_date.AddYears(1),7,"","")}};
        Assert.Throws<InvalidDataException>(()=>t.Validate(_date,_seller,_player,true));
    }
    [Fact] public void MultipleBeneficiariesKeepSeparateBasesAndPendingRightsCannotBeModified()
    {
        var a=new ContractualTransferRight(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),_player,new ClubId(Guid.NewGuid()),_seller,FutureFeeBasis.FutureTransferFee,20,"fixed fee",_date,_date.AddYears(2));
        var b=a with{Id=Guid.NewGuid(),Beneficiary=new ClubId(Guid.NewGuid()),Basis=FutureFeeBasis.ContractualProfit,Percentage=10,Calculation="positive profit, fixed acquisition cost only"};
        Assert.NotEqual(a.Beneficiary,b.Beneficiary);Assert.NotEqual(a.Basis,b.Basis);Assert.Equal(20,a.Percentage);Assert.Equal(10,b.Percentage);
        Assert.Throws<InvalidDataException>(()=>a.Reduce(new(Guid.NewGuid(),a.Id,1,a.Debtor,a.Beneficiary,5,100,"EUR",true,true),_date));
    }
    [Theory][InlineData(NegotiationState.Draft,NegotiationState.Sent)][InlineData(NegotiationState.Sent,NegotiationState.PendingResponse)][InlineData(NegotiationState.Sent,NegotiationState.CounterReceived)][InlineData(NegotiationState.Sent,NegotiationState.AcceptedBetweenClubs)][InlineData(NegotiationState.AcceptedBetweenClubs,NegotiationState.PendingPlayers)][InlineData(NegotiationState.PendingPlayers,NegotiationState.Cancelled)][InlineData(NegotiationState.Sent,NegotiationState.Expired)]
    public void ExplicitTransitionsSupported(NegotiationState a,NegotiationState b)=>TransferRules.Transition(a,b);
    [Theory][InlineData(NegotiationState.Draft,NegotiationState.AcceptedBetweenClubs)][InlineData(NegotiationState.Withdrawn,NegotiationState.Sent)][InlineData(NegotiationState.Rejected,NegotiationState.CounterReceived)][InlineData(NegotiationState.PendingPlayers,NegotiationState.Sent)]
    public void ImpossibleTransitionsRejected(NegotiationState a,NegotiationState b)=>Assert.Throws<InvalidOperationException>(()=>TransferRules.Transition(a,b));
    [Fact] public void RightReductionRequiresMatchingAgreementPreservesOldVersionAndAllowsFullExtinction()
    {
        var r=new ContractualTransferRight(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),_player,new ClubId(Guid.NewGuid()),_seller,FutureFeeBasis.FutureTransferFee,50,"cash fee",_date,_date.AddYears(5),State:ContractRightState.Active);
        var a=new RightAmendmentAgreement(Guid.NewGuid(),r.Id,1,r.Debtor,r.Beneficiary,30,300,"EUR",true,true);
        var changed=r.Reduce(a,_date);Assert.Equal(50,r.Percentage);Assert.Equal(20,changed.Percentage);Assert.Equal(2,changed.Revision);
        Assert.Equal(0,changed.Reduce(a with{Id=Guid.NewGuid(),Revision=2,ExtinguishedPercentage=20},_date).Percentage);
        Assert.Throws<InvalidDataException>(()=>r.Reduce(a with{ExtinguishedPercentage=51},_date));
        Assert.Throws<InvalidDataException>(()=>r.Reduce(a with{BeneficiaryAccepted=false},_date));
        Assert.Throws<InvalidDataException>(()=>r.Reduce(a with{Right=Guid.NewGuid()},_date));
        Assert.Throws<InvalidDataException>(()=>changed.Reduce(a,_date));
    }
}
public sealed partial class CareerCreationTests
{
    private (string File,SqliteTransferStore Store,PlayerId Target,TransferTerms Terms) TransferFixture()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteTransferStore(file);var data=store.Participants();var target=data.Players.First(p=>p.Category=="First"&&p.Details.Club!.Id!=data.ManagedClub).Details.Player.Id;
        var t=new TransferTerms(store.Currency(),100,new[]{new TransferPayment(Guid.NewGuid(),1,100,data.Date)},Array.Empty<PlayerId>(),Array.Empty<TransferBonus>(),Array.Empty<FutureFeeClause>(),null,null,ArrivalKind.Immediate,null,data.Date.AddDays(10));
        return(file,store,target,t);
    }
    private static string TransferBusinessFingerprint(string file)
    {
        using var c=Connection(file);var rows=new List<string>();using var names=c.CreateCommand();names.CommandText="SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE 'transfer_%' ORDER BY name";
        var tables=new List<string>();using(var r=names.ExecuteReader())while(r.Read())tables.Add(r.GetString(0));
        foreach(var table in tables){using var q=c.CreateCommand();q.CommandText="SELECT * FROM ["+table+"] ORDER BY rowid";using var r=q.ExecuteReader();while(r.Read())rows.Add(table+":"+JsonSerializer.Serialize(Enumerable.Range(0,r.FieldCount).Select(i=>r.IsDBNull(i)?null:r.GetValue(i)).ToArray()));}
        return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join("|",rows))));
    }
    [Fact] public void TransferReadsOldCareerWithoutMigrationAndInvalidDraftRollsBackMigration()
    {
        var f=TransferFixture();var hash=SHA256.HashData(File.ReadAllBytes(f.File));Assert.Empty(f.Store.History());Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(f.File)));
        Assert.Throws<InvalidDataException>(()=>f.Store.Draft(Guid.NewGuid(),0,f.Target,f.Terms with{FixedAmount=-1},Guid.NewGuid()));
        Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(f.File)));using var c=Connection(f.File);using var q=c.CreateCommand();q.CommandText="SELECT count(*) FROM sqlite_master WHERE name='transfer_schema'";Assert.Equal(0L,q.ExecuteScalar());
    }
    [Fact] public void TransferDraftModifySendWithdrawReopenAndRetryPreserveEntireSportingAndEconomicState()
    {
        var f=TransferFixture();var before=TransferBusinessFingerprint(f.File);var id=Guid.NewGuid();var op=Guid.NewGuid();
        var draft=f.Store.Draft(id,0,f.Target,f.Terms,op);Assert.Equal(JsonSerializer.Serialize(draft),JsonSerializer.Serialize(new SqliteTransferStore(f.File).History(id).Single()));Assert.Equal(JsonSerializer.Serialize(draft),JsonSerializer.Serialize(f.Store.Draft(id,0,f.Target,f.Terms,op)));
        var revised=f.Store.Draft(id,1,f.Target,f.Terms with{Expires=f.Terms.Expires.AddDays(1)},Guid.NewGuid());Assert.Equal(2,revised.Revision);
        var sendOp=Guid.NewGuid();var sent=f.Store.Send(id,2,sendOp);Assert.Equal(NegotiationState.Sent,sent.State);Assert.Equal(JsonSerializer.Serialize(sent),JsonSerializer.Serialize(f.Store.Send(id,2,sendOp)));
        var withdrawn=f.Store.Withdraw(id,3,Guid.NewGuid());Assert.Equal(NegotiationState.Withdrawn,withdrawn.State);Assert.Equal(4,f.Store.History(id).Count);
        Assert.Equal(before,TransferBusinessFingerprint(f.File));Assert.Equal(JsonSerializer.Serialize(withdrawn),JsonSerializer.Serialize(new SqliteTransferStore(f.File).Current().Single()));
        Assert.Throws<InvalidOperationException>(()=>f.Store.Send(id,4,Guid.NewGuid()));
    }
    [Fact] public void TransferHistoryCannotBeRewrittenAndStaleEditorsCannotOverwrite()
    {
        var f=TransferFixture();var id=Guid.NewGuid();f.Store.Draft(id,0,f.Target,f.Terms,Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(()=>f.Store.Draft(id,0,f.Target,f.Terms,Guid.NewGuid()));
        using var c=Connection(f.File);using var q=c.CreateCommand();q.CommandText="DELETE FROM transfer_versions";Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(()=>q.ExecuteNonQuery());q.CommandText="UPDATE transfer_versions SET revision=99";Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(()=>q.ExecuteNonQuery());
        Assert.Single(f.Store.History(id));
    }
    [Theory][InlineData(1)][InlineData(2)]
    public void TransferExchangeSupportsSeveralOwnPlayersWithoutMoney(int number)
    {
        var f=TransferFixture();var data=f.Store.Participants();var own=data.Players.Where(p=>p.Details.Club!.Id==data.ManagedClub).Take(number).Select(p=>p.Details.Player.Id).ToArray();
        var draft=f.Store.Draft(Guid.NewGuid(),0,f.Target,f.Terms with{FixedAmount=0,Payments=Array.Empty<TransferPayment>(),OfferedPlayers=own},Guid.NewGuid());f.Store.Send(draft.Id,1,Guid.NewGuid());Assert.Equal(own,draft.Terms.OfferedPlayers);
    }
    [Fact] public void TransferRejectsOwnTargetDuplicateAndForeignExchangeParticipants()
    {
        var f=TransferFixture();var data=f.Store.Participants();var own=data.Players.First(p=>p.Details.Club!.Id==data.ManagedClub).Details.Player.Id;var foreign=data.Players.First(p=>p.Details.Club!.Id!=data.ManagedClub&&p.Details.Player.Id!=f.Target).Details.Player.Id;
        Assert.Throws<InvalidOperationException>(()=>f.Store.Draft(Guid.NewGuid(),0,own,f.Terms,Guid.NewGuid()));
        Assert.Throws<InvalidDataException>(()=>f.Store.Draft(Guid.NewGuid(),0,f.Target,f.Terms with{OfferedPlayers=new[]{own,own}},Guid.NewGuid()));
        Assert.Throws<InvalidOperationException>(()=>f.Store.Draft(Guid.NewGuid(),0,f.Target,f.Terms with{OfferedPlayers=new[]{foreign}},Guid.NewGuid()));
    }
    [Fact] public void TransferRevalidatesOwnershipAtSendAndWithdrawRemainsAvailable()
    {
        var f=TransferFixture();var data=f.Store.Participants();var own=data.Players.First(p=>p.Category=="First"&&p.Details.Club!.Id==data.ManagedClub).Details.Player.Id;
        var draft=f.Store.Draft(Guid.NewGuid(),0,f.Target,f.Terms with{OfferedPlayers=new[]{own}},Guid.NewGuid());
        var world=new SqliteWorldStore(f.File).Load();var old=world.Contracts.Single(c=>c.PlayerId==own);new SqliteWorldStore(f.File).Apply(new Furvo.Core.Worlds.WorldData(contracts:new[]{new Contract(old.Id,old.ClubId,old.PlayerId,data.Date.AddYears(-1),data.Date.AddDays(-1))}));
        Assert.Throws<InvalidOperationException>(()=>f.Store.Send(draft.Id,1,Guid.NewGuid()));Assert.Single(f.Store.History(draft.Id));f.Store.Cancel(draft.Id,1,Guid.NewGuid());
    }
    private sealed class TransferDecision(Func<NegotiationVersion,ClubDecision> decide):IClubNegotiationDecisions
    {public ClubDecision Decide(NegotiationVersion proposal)=>decide(proposal);}
    [Fact] public void TransferCounterAndAcceptanceAreVersionedAndNeverExecuteTransfer()
    {
        var f=TransferFixture();var before=TransferBusinessFingerprint(f.File);var draft=f.Store.Draft(Guid.NewGuid(),0,f.Target,f.Terms,Guid.NewGuid());var sent=f.Store.Send(draft.Id,1,Guid.NewGuid());
        var counter=f.Store.Respond(draft.Id,2,Guid.NewGuid(),new TransferDecision(p=>new(p.Id,p.Revision,p.Seller,ClubDecisionKind.Counter,p.Terms with{FixedAmount=200,Payments=new[]{new TransferPayment(Guid.NewGuid(),1,200,p.Date)}},"Synthetic seller fixture")));
        Assert.Equal(NegotiationState.CounterReceived,counter.State);Assert.Equal(100,f.Store.History(draft.Id)[1].Terms.FixedAmount);
        var revised=f.Store.Draft(draft.Id,3,f.Target,counter.Terms,Guid.NewGuid());f.Store.Send(draft.Id,4,Guid.NewGuid());
        var pending=f.Store.Respond(draft.Id,5,Guid.NewGuid(),new TransferDecision(p=>new(p.Id,p.Revision,p.Seller,ClubDecisionKind.Accept,null,"Synthetic acceptance fixture")));
        Assert.Equal(NegotiationState.PendingPlayers,pending.State);Assert.Contains(f.Store.History(draft.Id),v=>v.State==NegotiationState.AcceptedBetweenClubs);
        Assert.Equal(before,TransferBusinessFingerprint(f.File));Assert.Throws<InvalidOperationException>(()=>f.Store.Send(draft.Id,pending.Revision,Guid.NewGuid()));
    }
    [Fact] public void TransferRejectsResponseFromWrongClubAndSupportsRejectionWithoutAutomaticDecisions()
    {
        var f=TransferFixture();var d=f.Store.Draft(Guid.NewGuid(),0,f.Target,f.Terms,Guid.NewGuid());f.Store.Send(d.Id,1,Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(()=>f.Store.Respond(d.Id,2,Guid.NewGuid(),new TransferDecision(p=>new(p.Id,p.Revision,p.Buyer,ClubDecisionKind.Accept,null,"Invalid party"))));
        var rejected=f.Store.Respond(d.Id,2,Guid.NewGuid(),new TransferDecision(p=>new(p.Id,p.Revision,p.Seller,ClubDecisionKind.Reject,null,"Synthetic rejection")));Assert.Equal(NegotiationState.Rejected,rejected.State);
    }
    [Fact] public void TransferClausesPersistAndUnsupportedCompetitionConditionsAreRejected()
    {
        var f=TransferFixture();var data=f.Store.Participants();var seller=data.Players.Single(p=>p.Details.Player.Id==f.Target).Details.Club!.Id;var scope=f.Store.Competitions()[0].Id.ToString();
        var bonus=new TransferBonus(Guid.NewGuid(),BonusObjective.Goals,10,50,data.Date,data.Date.AddYears(1),1,false,scope);
        var fee=new FutureFeeClause(Guid.NewGuid(),seller,f.Target,20,FutureFeeBasis.FutureTransferFee,"Fixed monetary fee",data.Date,data.Date.AddYears(5),"completed future transfer");
        var terms=f.Terms with{Bonuses=new[]{bonus,bonus with{Id=Guid.NewGuid(),Threshold=20}},FutureFees=new[]{fee,fee with{Id=Guid.NewGuid(),Basis=FutureFeeBasis.ContractualProfit,Calculation="positive surplus after acquisition fixed fee only"}},Buyback=new(300,data.Date,data.Date.AddYears(1),"player consent and registration"),Matching=new(data.Date.AddYears(1),7,"written notification","equal all guaranteed payments")};
        var d=f.Store.Draft(Guid.NewGuid(),0,f.Target,terms,Guid.NewGuid());var loaded=f.Store.History(d.Id).Single();Assert.Equal(JsonSerializer.Serialize(terms),JsonSerializer.Serialize(loaded.Terms));
        Assert.Throws<InvalidDataException>(()=>f.Store.Draft(Guid.NewGuid(),0,f.Target,f.Terms with{Bonuses=new[]{bonus with{Objective=BonusObjective.CupTitle}}},Guid.NewGuid()));
    }
    [Fact] public void TransferReusedCommandTokensRejectDifferentConditionsAndCurrency()
    {
        var f=TransferFixture();var id=Guid.NewGuid();var token=Guid.NewGuid();f.Store.Draft(id,0,f.Target,f.Terms,token);
        Assert.Throws<InvalidOperationException>(()=>f.Store.Draft(id,0,f.Target,f.Terms with{FixedAmount=101},token));
        Assert.Throws<InvalidOperationException>(()=>f.Store.Draft(id,0,new PlayerId(Guid.NewGuid()),f.Terms,token));
        Assert.Throws<InvalidOperationException>(()=>f.Store.Draft(Guid.NewGuid(),0,f.Target,f.Terms with{Currency="USD"},Guid.NewGuid()));
        Assert.Single(f.Store.History(id));
    }
    [Fact] public void TransferLegacyPublicationWithoutEconomicsUsesConfiguredCurrencyWithoutInventingBudget()
    {
        var profile=Profile();var careers=Store();var career=careers.Create(Request(profile,CareerStartMode.SignOffer,Offer(profile)));var file=careers.PathFor(career.Id);
        Assert.Empty(SqliteEconomyStore.ReadResources(file));var store=new SqliteTransferStore(file);Assert.Equal("EUR",store.Currency());
        var data=store.Participants();var external=data.Players.First(p=>p.Details.Club is {} club&&club.Id!=data.ManagedClub).Details.Player.Id;
        var terms=new TransferTerms("EUR",0,Array.Empty<TransferPayment>(),Array.Empty<PlayerId>(),Array.Empty<TransferBonus>(),Array.Empty<FutureFeeClause>(),null,null,ArrivalKind.Immediate,null,data.Date,ExplicitFreeProposal:true);
        var before=TransferBusinessFingerprint(file);var d=store.Draft(Guid.NewGuid(),0,external,terms,Guid.NewGuid());store.Send(d.Id,1,Guid.NewGuid());
        Assert.Equal(before,TransferBusinessFingerprint(file));Assert.Empty(SqliteEconomyStore.ReadResources(file));
    }
    [Fact] public void TransferNegotiationsAreIsolatedBetweenCareersAndExpiredStateCannotBeInvented()
    {
        var a=TransferFixture();var b=TransferFixture();var hash=SHA256.HashData(File.ReadAllBytes(b.File));var d=a.Store.Draft(Guid.NewGuid(),0,a.Target,a.Terms,Guid.NewGuid());a.Store.Send(d.Id,1,Guid.NewGuid());
        Assert.Empty(b.Store.History(d.Id));Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(b.File)));Assert.Throws<InvalidOperationException>(()=>a.Store.Expire(d.Id,2,Guid.NewGuid()));
    }
    [Theory]
    [InlineData(ClubDecisionKind.Accept)]
    [InlineData(ClubDecisionKind.Reject)]
    [InlineData(ClubDecisionKind.Counter)]
    public void SellerResponseRetryReturnsRecordedResult(ClubDecisionKind kind)
    {
        var fixture=TransferFixture();
        var draft=fixture.Store.Draft(Guid.NewGuid(),0,fixture.Target,fixture.Terms,Guid.NewGuid());
        var sent=fixture.Store.Send(draft.Id,draft.Revision,Guid.NewGuid());
        var operation=Guid.NewGuid();
        var first=fixture.Store.Respond(sent.Id,sent.Revision,operation,new TransferDecision(p=>
            new(p.Id,p.Revision,p.Seller,kind,kind==ClubDecisionKind.Counter?p.Terms:null,"Fixture")));
        var historyCount=fixture.Store.History(sent.Id).Count;
        var reopened=new SqliteTransferStore(fixture.File);
        var retry=reopened.Respond(sent.Id,sent.Revision,operation,new TransferDecision(_=>throw new InvalidOperationException("No se debe repetir la decisión.")));
        Assert.Equal(JsonSerializer.Serialize(first),JsonSerializer.Serialize(retry));
        Assert.Equal(historyCount,reopened.History(sent.Id).Count);
        Assert.Throws<InvalidOperationException>(()=>reopened.Respond(sent.Id,sent.Revision+1,operation,new TransferDecision(_=>throw new Exception())));
    }

    [Fact]
    public void ValidProposalKeepsItsOriginalEstimatedDates()
    {
        var fixture=TransferFixture();
        var date=fixture.Store.Participants().Date;
        var seller=fixture.Store.Participants().Players.Single(p=>p.Details.Player.Id==fixture.Target).Details.Club!.Id;
        var plan=new TransferPaymentPlan(TransferPaymentPlans.Version,date,TransferPaymentPlans.Preset(3));
        var terms=fixture.Terms with {
            PaymentPlan=plan,
            Payments=TransferPaymentPlans.Generate(fixture.Terms.FixedAmount,plan),
            Buyback=TransferOfferPresets.Buyback(200,date,2)
        };
        terms.Validate(date.AddDays(1),seller,fixture.Target,true,proposalDate:date);
        Assert.Throws<InvalidDataException>(()=>terms.Validate(terms.Expires.AddDays(1),seller,fixture.Target,true,proposalDate:date));
    }

}
