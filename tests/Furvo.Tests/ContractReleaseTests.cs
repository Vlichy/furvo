using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Microsoft.Data.Sqlite;
using Xunit;
namespace Furvo.Tests;
public sealed class ContractReleaseTests
{
    private static readonly DateOnly Date=new(2026,7,1);
    private static readonly ContractId Id=new(Guid.NewGuid());
    private static readonly ClubId Seller=new(Guid.NewGuid()),Buyer=new(Guid.NewGuid());
    private static Contract Contract(ContractReleaseClause? clause=null)=>new(Id,Seller,new PlayerId(Guid.NewGuid()),new(2025,7,1),new(2028,6,30),clause);
    private static ContractReleaseClause Known()=>new(Id,ReleaseClausePresence.Present,12_000_000,"EUR",Disclosure:ReleaseClauseDisclosure.PublicConfirmed,Evidence:"fixture-public-evidence",ConfirmedOn:Date);
    private static ReleaseClauseContext Context()=>new(Date,Buyer,"España","España",BuyerClub:Buyer);
    [Fact] public void KnownAndCurrentClauseIsContractBoundAndPublic()
    {var c=Contract(Known());var view=ContractInformation.Release(c,Context());Assert.Equal(ReleaseInformationState.Known,view.State);Assert.Equal(12_000_000,view.Amount);Assert.Equal(ReleaseApplicability.Applicable,view.Applicability);Assert.Equal(Id,c.ReleaseClause!.ContractId);}
    [Fact] public void ConfirmedAbsentIsDistinctFromLegacyUnknown()
    {Assert.Equal(ReleaseInformationState.Absent,ContractInformation.Release(Contract(Known() with{Presence=ReleaseClausePresence.Absent,Amount=null,Currency=null}),Context()).State);Assert.Equal(ReleaseClauseInfo.Unknown,ContractInformation.Release(Contract(),Context()));Assert.Equal(ReleaseClauseInfo.Unknown,ContractInformation.Release(null,Context()));
        Assert.Equal(ReleaseClauseInfo.Unknown,ContractInformation.Release(Contract(Known() with{Presence=ReleaseClausePresence.Absent,Amount=null,Currency=null}),Context() with{Date=new DateOnly(2030,1,1)}));}
    [Theory][InlineData(-1)][InlineData(0)][InlineData(1000000001)]
    public void InvalidAmountsCannotEnterContract(int amount)=>Assert.Throws<InvalidDataException>(()=>Contract(Known() with{Amount=amount}));
    [Fact] public void AmountPrecisionCurrencyAndIdentityAreValidated()
    {Assert.Throws<InvalidDataException>(()=>Contract(Known() with{Amount=1.001m}));Assert.Throws<InvalidDataException>(()=>Contract(Known() with{Currency="eur"}));Assert.Throws<InvalidDataException>(()=>Contract(Known() with{ContractId=new(Guid.NewGuid())}));Assert.Throws<InvalidDataException>(()=>Contract(Known() with{Presence=ReleaseClausePresence.Absent}));Assert.Equal(1_000_000_000,Contract(Known() with{Amount=1_000_000_000}).ReleaseClause!.Amount);}
    [Theory][InlineData(-1)][InlineData(1)]
    public void ExpiredOrFutureClauseIsNotApplicable(int direction)
    {var clause=direction<0?Known() with{Until=Date.AddDays(-1)}:Known() with{From=Date.AddDays(1)};var view=ContractInformation.Release(Contract(clause),Context());Assert.Equal(ReleaseInformationState.NotApplicable,view.State);Assert.Null(view.Amount);}
    [Fact] public void ClauseDatesMustFitContract()
    {Assert.Throws<InvalidDataException>(()=>Contract(Known() with{From=new(2024,1,1)}));Assert.Throws<InvalidDataException>(()=>Contract(Known() with{Until=new(2030,1,1)}));Assert.Throws<InvalidDataException>(()=>Contract(Known() with{From=Date.AddDays(1),Until=Date}));}
    [Fact] public void PrivateAndFutureKnowledgeIsRemovedBeforePublicProjection()
    {
        var privateContract=Contract(Known() with{Disclosure=ReleaseClauseDisclosure.ClubOnly,Evidence="PRIVATE-MARKER"});var publicView=ContractInformation.Release(privateContract,Context());Assert.Equal(ReleaseClauseInfo.Unknown,publicView);Assert.DoesNotContain("PRIVATE-MARKER",JsonSerializer.Serialize(publicView));Assert.DoesNotContain("12000000",JsonSerializer.Serialize(publicView));Assert.Null(publicView.Currency);
        Assert.Equal(ReleaseInformationState.Known,ContractInformation.Release(privateContract,Context() with{ViewerClub=Seller,BuyerClub=null}).State);
        Assert.Equal(ReleaseClauseInfo.Unknown,ContractInformation.Release(Contract(Known() with{ConfirmedOn=Date.AddDays(1)}),Context()));
        Assert.Throws<InvalidDataException>(()=>Contract(Known() with{Evidence=null}));Assert.Throws<InvalidDataException>(()=>Contract(Known() with{ConfirmedOn=null}));
    }
    [Fact] public void ScopeAndStructuredConditionsDoNotInventApplicability()
    {
        var c=Contract(Known() with{Scope=ReleaseClauseScope.International});Assert.Equal(ReleaseInformationState.NotApplicable,ContractInformation.Release(c,Context()).State);Assert.Equal(ReleaseApplicability.PendingConditions,ContractInformation.Release(c,Context() with{BuyerCountry=null}).Applicability);
        c=Contract(Known() with{Conditions=new[]{new ReleaseClauseCondition(ReleaseConditionKind.Competition,Guid.NewGuid())}});Assert.Equal(ReleaseApplicability.PendingConditions,ContractInformation.Release(c,Context()).Applicability);
        c=Contract(Known() with{Conditions=new[]{new ReleaseClauseCondition(ReleaseConditionKind.BuyerClub,Buyer.Value)}});Assert.Equal(ReleaseApplicability.Applicable,ContractInformation.Release(c,Context()).Applicability);Assert.Equal(ReleaseApplicability.PendingConditions,ContractInformation.Release(c,Context() with{ViewerClub=Seller,BuyerClub=null}).Applicability);
    }
    [Theory][InlineData(11999999,ReleaseOfferNotice.Below)][InlineData(12000000,ReleaseOfferNotice.AtOrAbove)][InlineData(12000001,ReleaseOfferNotice.AtOrAbove)]
    public void OfferComparisonNeverAcceptsOrExecutes(int amount,ReleaseOfferNotice expected)
    {var view=ContractInformation.Release(Contract(Known()),Context());Assert.Equal(expected,ContractInformation.Notice(view,amount,"EUR"));Assert.Equal(ReleaseOfferNotice.CurrencyMismatch,ContractInformation.Notice(view,amount,"USD"));}
    [Fact] public void UnknownPendingAbsentAndNotApplicableHaveDifferentNotices()
    {Assert.Equal(ReleaseOfferNotice.Unknown,ContractInformation.Notice(ReleaseClauseInfo.Unknown,100,"EUR"));Assert.Equal(ReleaseOfferNotice.Absent,ContractInformation.Notice(new(ReleaseInformationState.Absent),100,"EUR"));Assert.Equal(ReleaseOfferNotice.NotApplicable,ContractInformation.Notice(new(ReleaseInformationState.NotApplicable),100,"EUR"));Assert.Equal(ReleaseOfferNotice.PendingConditions,ContractInformation.Notice(new(ReleaseInformationState.Known,100,"EUR",ReleaseApplicability.PendingConditions),100,"EUR"));}
    [Fact] public void RealPresentationFormatsKnownAndUnavailableWithoutInternalData()
    {
        var text=XDocument.Load(Path.Combine(AppContext.BaseDirectory,"UiStrings.resx")).Root!.Elements("data").ToDictionary(e=>e.Attribute("name")!.Value,e=>e.Element("value")!.Value);string T(string key)=>text[key];
        var view=ContractInformation.Release(Contract(Known()),Context());Assert.Equal("12.000.000,00 EUR",ReleaseClauseText.Value(view,T));Assert.Equal("Desconocida",ReleaseClauseText.Value(ReleaseClauseInfo.Unknown,T));Assert.Equal("Sin cláusula",ReleaseClauseText.Value(new(ReleaseInformationState.Absent),T));Assert.Equal("No vigente",ReleaseClauseText.Value(new(ReleaseInformationState.NotApplicable),T));Assert.Contains("no implica aceptación automática",ReleaseClauseText.Notice(view,12_000_000,"EUR",T));Assert.DoesNotContain("12.000.000",ReleaseClauseText.Notice(ReleaseClauseInfo.Unknown,12_000_000,"EUR",T));
    }
}
public sealed partial class CareerCreationTests
{
    private static string ReleaseBusinessFingerprint(string file)
    {
        using var c=Connection(file);using var names=c.CreateCommand();names.CommandText="SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE 'transfer_%' AND name NOT LIKE 'contract_release_%' ORDER BY name";var tables=new List<string>();using(var r=names.ExecuteReader())while(r.Read())tables.Add(r.GetString(0));var rows=new List<string>();foreach(var table in tables){using var q=c.CreateCommand();q.CommandText="SELECT * FROM ["+table+"] ORDER BY rowid";using var r=q.ExecuteReader();while(r.Read())rows.Add(table+":"+JsonSerializer.Serialize(Enumerable.Range(0,r.FieldCount).Select(i=>r.IsDBNull(i)?null:r.GetValue(i)).ToArray()));}return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join("|",rows))));
    }
    [Fact] public void ContractReleaseMigrationKeepsLegacyUnknownAndReadersNeverMigrate()
    {
        var f=TransferFixture();var before=SHA256.HashData(File.ReadAllBytes(f.File));var data=new SqliteMarketQueries(f.File).Read();Assert.All(data.Players.Where(p=>p.Contract is not null),p=>Assert.Equal(ReleaseClauseInfo.Unknown,p.Contract!.Release));Assert.Equal(before,SHA256.HashData(File.ReadAllBytes(f.File)));
        var business=ReleaseBusinessFingerprint(f.File);new SqliteContractReleaseStore(f.File).Migrate();Assert.Equal(business,ReleaseBusinessFingerprint(f.File));Assert.All(new SqliteWorldStore(f.File).Load().Contracts,c=>Assert.Null(c.ReleaseClause));using var c=Connection(f.File);using var q=c.CreateCommand();q.CommandText="SELECT count(*) FROM contract_release_terms";Assert.Equal(0,Convert.ToInt32(q.ExecuteScalar()));
    }
    [Fact] public void ClausePersistenceQueriesAndOffersDoNotMovePlayersOrPay()
    {
        var f=TransferFixture();var data=f.Store.Participants();var contract=new SqliteWorldStore(f.File).Load().Contracts.Single(k=>k.PlayerId==f.Target);var before=ReleaseBusinessFingerprint(f.File);var clause=new ContractReleaseClause(contract.Id,ReleaseClausePresence.Present,12_000_000.01m,"EUR",Disclosure:ReleaseClauseDisclosure.PublicConfirmed,Evidence:"fixture-confirmed-source",ConfirmedOn:data.Date);new SqliteContractReleaseStore(f.File).Record(clause);
        var loaded=new SqliteWorldStore(f.File).Load().Contracts.Single(k=>k.Id==contract.Id);Assert.Equal(12_000_000.01m,loaded.ReleaseClause!.Amount);var row=MarketQueries.Player(new SqliteMarketQueries(f.File).Read(),f.Target);Assert.Equal(12_000_000.01m,row.Contract!.Release!.Amount);Assert.Equal(before,ReleaseBusinessFingerprint(f.File));
        var hash=SHA256.HashData(File.ReadAllBytes(f.File));f.Store.Participants();new SqliteContractReleaseStore(f.File).Player(f.Target);Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(f.File)));var saved=f.Store.Draft(Guid.NewGuid(),0,f.Target,f.Terms,Guid.NewGuid());f.Store.Send(saved.Id,1,Guid.NewGuid());Assert.Equal(before,ReleaseBusinessFingerprint(f.File));Assert.Equal(2,f.Store.History(saved.Id).Count);
    }
    [Fact] public void PrivateClausesAreKnownOnlyToTheOwnerAndNeverLeakThroughMarket()
    {
        var f=TransferFixture();var world=new SqliteWorldStore(f.File).Load();var date=f.Store.Participants().Date;var external=world.Contracts.Single(c=>c.PlayerId==f.Target);var store=new SqliteContractReleaseStore(f.File);store.Record(new(external.Id,ReleaseClausePresence.Present,87654321.09m,"EUR",Evidence:"CONFIDENTIAL-FIXTURE"));
        var view=MarketQueries.Player(new SqliteMarketQueries(f.File).Read(),f.Target);Assert.Equal(ReleaseClauseInfo.Unknown,view.Contract!.Release);Assert.DoesNotContain("CONFIDENTIAL-FIXTURE",JsonSerializer.Serialize(view));Assert.DoesNotContain("87654321",JsonSerializer.Serialize(view));Assert.All(view.Details.AttributeGroups,g=>Assert.NotEqual(AttributeCategory.Hidden,g.Category));
        var own=world.Contracts.First(c=>c.ClubId==f.Store.Participants().ManagedClub);store.Record(new(own.Id,ReleaseClausePresence.Present,87654321.09m,"EUR"));Assert.Equal(87654321.09m,store.Player(own.PlayerId).Release.Amount);
    }
    [Fact] public void ConfirmedAbsenceAndConditionalKnowledgePersistWithoutFabricatedAmounts()
    {
        var f=TransferFixture();var world=new SqliteWorldStore(f.File).Load();var contract=world.Contracts.Single(c=>c.PlayerId==f.Target);var store=new SqliteContractReleaseStore(f.File);var date=f.Store.Participants().Date;
        store.Record(new(contract.Id,ReleaseClausePresence.Absent,Disclosure:ReleaseClauseDisclosure.PublicConfirmed,Evidence:"fixture-no-clause",ConfirmedOn:date));Assert.Equal(ReleaseInformationState.Absent,MarketQueries.Player(new SqliteMarketQueries(f.File).Read(),f.Target).Contract!.Release!.State);
        var condition=new ReleaseClauseCondition(ReleaseConditionKind.Competition,world.Competitions[0].Id.Value);store.Record(new(contract.Id,ReleaseClausePresence.Present,100,"EUR",Conditions:new[]{condition},Disclosure:ReleaseClauseDisclosure.PublicConfirmed,Evidence:"fixture-conditional",ConfirmedOn:date));var result=MarketQueries.Player(new SqliteMarketQueries(f.File).Read(),f.Target).Contract!.Release!;Assert.Equal(ReleaseApplicability.PendingConditions,result.Applicability);Assert.Equal(ReleaseOfferNotice.PendingConditions,ContractInformation.Notice(result,100,"EUR"));
    }
    [Fact] public void ReleaseWritesRollbackAndForeignKeysAndIntervalsAreEnforced()
    {
        var f=TransferFixture();var contract=new SqliteWorldStore(f.File).Load().Contracts.Single(c=>c.PlayerId==f.Target);var store=new SqliteContractReleaseStore(f.File);var before=SHA256.HashData(File.ReadAllBytes(f.File));Assert.Throws<InvalidDataException>(()=>store.Record(new(contract.Id,ReleaseClausePresence.Present,-1,"EUR")));Assert.Throws<KeyNotFoundException>(()=>store.Record(new(new(Guid.NewGuid()),ReleaseClausePresence.Absent)));Assert.Equal(before,SHA256.HashData(File.ReadAllBytes(f.File)));
        store.Record(new(contract.Id,ReleaseClausePresence.Present,100,"EUR",From:contract.StartDate,Until:contract.EndDate));using var c=Connection(f.File);using var q=c.CreateCommand();q.CommandText="UPDATE contract_release_terms SET amount_minor=-1";Assert.Throws<SqliteException>(()=>q.ExecuteNonQuery());q.CommandText="UPDATE contract_release_terms SET valid_from='2026-99-01'";Assert.Throws<SqliteException>(()=>q.ExecuteNonQuery());q.CommandText="UPDATE contract_release_terms SET amount_minor=100000000001";Assert.Throws<SqliteException>(()=>q.ExecuteNonQuery());q.CommandText="UPDATE contract_release_terms SET conditions_json=json_array(json_object('Kind',1,'Reference','missing'))";Assert.Throws<SqliteException>(()=>q.ExecuteNonQuery());q.CommandText="INSERT INTO contract_release_terms(contract_id,presence,scope,conditions_json,disclosure) VALUES('missing',1,0,'[]',0)";Assert.Throws<SqliteException>(()=>q.ExecuteNonQuery());
        q.CommandText="UPDATE contracts SET end_date='2026-07-02' WHERE id='"+contract.Id+"'";Assert.Throws<SqliteException>(()=>q.ExecuteNonQuery());
    }
    [Fact] public void ReleaseDataIsIsolatedAndUnrelatedContractUpdatesDoNotEraseIt()
    {
        var a=TransferFixture();var b=TransferFixture();var contract=new SqliteWorldStore(a.File).Load().Contracts.Single(c=>c.PlayerId==a.Target);var bHash=SHA256.HashData(File.ReadAllBytes(b.File));new SqliteContractReleaseStore(a.File).Record(new(contract.Id,ReleaseClausePresence.Present,100,"EUR"));var without=new Contract(contract.Id,contract.ClubId,contract.PlayerId,contract.StartDate,contract.EndDate);new SqliteWorldStore(a.File).Apply(new Furvo.Core.Worlds.WorldData(contracts:new[]{without}));Assert.Equal(100,new SqliteWorldStore(a.File).Load().Contracts.Single(c=>c.Id==contract.Id).ReleaseClause!.Amount);Assert.Equal(bHash,SHA256.HashData(File.ReadAllBytes(b.File)));
    }
}
