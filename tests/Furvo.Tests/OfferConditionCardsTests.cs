using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using Furvo.Core.Domain;
using Furvo.Core.Market;
using Furvo.Presentation.Components;
using Xunit;
namespace Furvo.Tests;
public sealed class OfferConditionCardsTests
{
    private static readonly DateOnly Date=new(2026,7,1);
    [Fact] public void NewChoicesAreSeparatedAndHistoricalValuesAreNotLost()
    {
        Assert.Equal(new[]{1,2,3,5,10},OfferConditionOptions.BuybackYears);Assert.Equal(new[]{1,2,3,5},OfferConditionOptions.MatchingYears);Assert.Equal(new[]{3,7,14},OfferConditionOptions.MatchingDays);Assert.Equal(new[]{3,7,14,30},OfferConditionOptions.ValidityDays);
        Assert.Equal(new[]{1,2,3,5,10},OfferConditionOptions.Preserve(OfferConditionOptions.MatchingYears,10));Assert.Equal(new[]{3,7,14,30},OfferConditionOptions.Preserve(OfferConditionOptions.MatchingDays,30));
        Assert.Equal(new[]{3,7,14,30,60},OfferConditionOptions.Preserve(OfferConditionOptions.ValidityDays,60));Assert.Equal(4,OfferConditionOptions.Preserve(OfferConditionOptions.ValidityDays,7).Count);
    }
    [Theory][InlineData(1,"1 año","1 día")][InlineData(2,"2 años","2 días")][InlineData(7,"7 años","7 días")]
    public void ResourcesAndKeysUseCorrectSingularAndPlural(int value,string years,string days)
    {
        var strings=XDocument.Load(Path.Combine(AppContext.BaseDirectory,"UiStrings.resx")).Root!.Elements("data").ToDictionary(e=>e.Attribute("name")!.Value,e=>e.Element("value")!.Value);
        Assert.Equal(years,string.Format(CultureInfo.GetCultureInfo("es-ES"),strings[OfferConditionOptions.YearsKey(value)],value));Assert.Equal(days,string.Format(CultureInfo.GetCultureInfo("es-ES"),strings[OfferConditionOptions.DaysKey(value)],value));
        var buy=string.Format(CultureInfo.GetCultureInfo("es-ES"),strings["OfferBuybackDynamic"],12_000_000m.ToString("N2",CultureInfo.GetCultureInfo("es-ES"))+" EUR",years);Assert.Contains("12.000.000,00 EUR",buy);Assert.Contains(years,buy);
        var match=string.Format(CultureInfo.GetCultureInfo("es-ES"),strings["OfferMatchingDynamic"],years,days);Assert.Contains(years,match);Assert.Contains(days,match);
    }
    [Theory][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(5)][InlineData(10)]
    public void ExistingBuybackRulesAndVersionArePreserved(int years)
    {
        var clause=TransferOfferPresets.Buyback(12_000_000,Date,years);Assert.Equal(years,clause.DurationYears);Assert.Equal(Date.AddYears(years).AddDays(-1),clause.Until);Assert.Equal(TransferOfferPresets.BuybackConditions,clause.Conditions);
    }
    [Theory][InlineData(1,3)][InlineData(2,7)][InlineData(3,14)][InlineData(5,7)][InlineData(10,30)]
    public void MatchingDurationsAndResponsesRemainIndependentIncludingLegacy(int years,int days)
    {
        var clause=TransferOfferPresets.Matching(Date,years,days);Assert.Equal(Date.AddYears(years).AddDays(-1),clause.Until);Assert.Equal(days,clause.ResponseDays);Assert.Equal(TransferOfferPresets.MatchingCriteria,clause.MatchingCriteria);
    }
    [Theory][InlineData(-1)][InlineData(1000000001)]
    public void BuybackMoneyValidationIsNotRelaxed(int amount)=>Assert.Throws<InvalidDataException>(()=>TransferOfferPresets.Buyback(amount,Date,1));
}
public sealed partial class CareerCreationTests
{
    [Theory][InlineData(3)][InlineData(7)][InlineData(14)][InlineData(30)]
    public void VisualConditionsPersistIndependentlyAndSendingDoesNotExecuteThem(int validity)
    {
        var f=TransferFixture();var data=f.Store.Participants();var before=TransferBusinessFingerprint(f.File);
        var terms=f.Terms with{Buyback=TransferOfferPresets.Buyback(12_000_000,data.Date,2),Matching=TransferOfferPresets.Matching(data.Date,3,7),Expires=data.Date.AddDays(validity)};
        var saved=f.Store.Draft(Guid.NewGuid(),0,f.Target,terms,Guid.NewGuid());f.Store.Send(saved.Id,1,Guid.NewGuid());var history=f.Store.History(saved.Id);Assert.Equal(2,history.Count);Assert.Equal(JsonSerializer.Serialize(terms),JsonSerializer.Serialize(history[^1].Terms));Assert.Equal(data.Date.AddDays(validity),history[^1].Terms.Expires);Assert.Equal(7,history[^1].Terms.Matching!.ResponseDays);Assert.Equal(before,TransferBusinessFingerprint(f.File));
    }
    [Theory][InlineData(ArrivalKind.Immediate)][InlineData(ArrivalKind.NextWindow)][InlineData(ArrivalKind.FutureDate)]
    public void ArrivalChoicesRoundTripWithoutInventingWindows(ArrivalKind arrival)
    {
        var f=TransferFixture();var date=f.Store.Participants().Date;DateOnly? intended=arrival==ArrivalKind.FutureDate?date.AddDays(30):null;
        var terms=f.Terms with{Arrival=arrival,IntendedArrival=intended,Payments=f.Terms.Payments.Select(p=>p with{Due=intended??date}).ToArray()};var saved=f.Store.Draft(Guid.NewGuid(),0,f.Target,terms,Guid.NewGuid());var loaded=f.Store.History(saved.Id).Single().Terms;Assert.Equal(arrival,loaded.Arrival);Assert.Equal(intended,loaded.IntendedArrival);Assert.Null(loaded.Buyback);Assert.Null(loaded.Matching);
        Assert.Throws<InvalidDataException>(()=>f.Store.Draft(Guid.NewGuid(),0,f.Target,terms with{Arrival=ArrivalKind.FutureDate,IntendedArrival=date.AddDays(-1)},Guid.NewGuid()));
    }
    [Fact] public void PreviousMatchingAndUnusualValidityAreReadableWithoutRewriting()
    {
        var f=TransferFixture();var date=f.Store.Participants().Date;var terms=f.Terms with{Matching=TransferOfferPresets.Matching(date,10,30),Expires=date.AddDays(60)};var saved=f.Store.Draft(Guid.NewGuid(),0,f.Target,terms,Guid.NewGuid());var before=System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(f.File));var loaded=f.Store.History(saved.Id).Single().Terms;Assert.Equal(10,loaded.Matching!.DurationYears);Assert.Equal(30,loaded.Matching.ResponseDays);Assert.Equal(date.AddDays(60),loaded.Expires);Assert.Equal(before,System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(f.File)));
    }
}
