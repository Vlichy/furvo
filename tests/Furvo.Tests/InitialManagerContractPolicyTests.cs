using System.Security.Cryptography;
using Furvo.Core.Careers;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Xunit;

namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    [Fact] public void InitialDatesAndClubCurrencyIgnoreCoachNationalityAndBackground()
    {
        var queries=PublishedWorldQueries.Open(Source);var source=new InitialManagerOfferSource(queries);
        foreach(var background in NewCoachBackgrounds.Options)
        foreach(var nationality in new[]{"ES","JP"})
        {
            var p=new CoachProfile(CoachProfileId.New(),"Prueba","Internacional",new(1980,1,1),nationality,"Ciudad",background,"generic-v1",_date);
            foreach(var club in queries.GetClubs())
            {
                var offer=source.Create(p,club.Id);
                Assert.Equal(new DateOnly(2026,7,1),offer.Terms.StartDate);
                Assert.Equal(new DateOnly(2027,6,30),offer.Terms.EndDate);
                Assert.Equal(12m,offer.Terms.CalculateDurationMonths());
                Assert.Equal("EUR",offer.Terms.Currency);Assert.Null(offer.Terms.GrossAnnualSalary);
                Assert.Equal(new[]{"OfferSalaryMissing"},offer.SigningBlocks());
            }
        }
    }
    [Theory][InlineData(null)][InlineData(0)][InlineData(-1)]
    public void UnapprovedOrInvalidPremiumNeverInventsSalary(int? premium)
    {
        var rule=new InitialManagerSalaryRule("test-only",premium);
        Assert.Null(rule.Calculate(ContractCountryPolicies.For("España")));
    }
    [Theory][InlineData(null)][InlineData("Sin configurar")]
    public void UnknownClubCountryHasSpecificConfigurationError(string? country)=>
        Assert.Throws<ContractCountryConfigurationException>(()=>ContractCountryPolicies.For(country));
    [Theory][InlineData(0)][InlineData(17093)][InlineData(17094)]
    public void AnnualSalaryMustStrictlyExceedAnnualLegalFloor(int amount)
    {
        var p=Profile();var source=new InitialManagerOfferSource(PublishedWorldQueries.Open(Source),new("test-only",1000m));
        var offer=source.Create(p,Offer(p).ClubId);offer=offer with{Terms=offer.Terms with{GrossAnnualSalary=amount}};
        Assert.Contains("OfferSalaryBelowMinimum",offer.SigningBlocks());
        Assert.Throws<InvalidOperationException>(()=>Store().Create(Request(p,CareerStartMode.SignOffer,offer)));
    }
    [Fact] public void ApprovedTestRulePersistsSnapshotWithoutChangingBudgetsOrDrafts()
    {
        var p=Profile();var q=PublishedWorldQueries.Open(Source);
        // Solo fixture: 1000 EUR de margen no es una decisión económica del juego.
        var offer=new InitialManagerOfferSource(q,new("test-only",1000m)).Create(p,q.GetClubs()[0].Id);
        Assert.Equal(18094m,offer.Terms.GrossAnnualSalary);Assert.Empty(offer.SigningBlocks());
        var store=Store();var career=store.Create(Request(p,CareerStartMode.SignOffer,offer));
        var accepted=store.Load(career.Id).Contract!.AcceptedOffer;
        Assert.Equal(offer.SalaryPolicy,accepted.SalaryPolicy);Assert.Equal(offer.Terms,accepted.Terms);
        Assert.Equal(offer.Resources,accepted.Resources);Assert.Empty(accepted.Commitments.Objectives);
        Assert.Empty(accepted.Commitments.Principles);Assert.Null(accepted.Terms.TerminationConditions);
        Assert.Equal(TerminationClauseKind.Unknown,accepted.Terms.TerminationKind);
    }
    [Fact] public void SalaryMustFollowApprovedRuleAndConfiguredCurrency()
    {
        var p=Profile();var q=PublishedWorldQueries.Open(Source);var initial=new InitialManagerOfferSource(q).Create(p,q.GetClubs()[0].Id);
        var supplied=initial with{Terms=initial.Terms with{GrossAnnualSalary=20000m}};
        Assert.Contains("OfferSalaryRulePending",supplied.SigningBlocks());
        var approved=new InitialManagerOfferSource(q,new("test-only",1000m)).Create(p,q.GetClubs()[0].Id);
        Assert.Contains("OfferSalaryRuleMismatch",(approved with{Terms=approved.Terms with{GrossAnnualSalary=20000m}}).SigningBlocks());
        Assert.Contains("OfferSalaryCurrencyMismatch",(approved with{Terms=approved.Terms with{Currency="JPY"}}).SigningBlocks());
    }
    [Fact] public void NewProfilesOnlyAllowTwoBackgroundsAndExistingExperienceIsPreserved()
    {
        Assert.Equal(new[]{CoachExperience.Novice,CoachExperience.FormerPlayer},NewCoachBackgrounds.Options);
        var db=Path.Combine(_root,"backgrounds.db");var store=new SqliteCoachProfileStore(db);
        var old=new CoachProfile(CoachProfileId.New(),"Prueba","Histórica",new(1980,1,1),"ES","Ciudad",CoachExperience.Experienced,"generic-v1",_date);
        store.Create(Profile());Assert.Throws<ArgumentException>(()=>store.Create(old));
        // Simula un perfil escrito por la versión anterior, sin migrar su esquema.
        using(var c=Connection(db))using(var cmd=c.CreateCommand())
        {
            cmd.CommandText="INSERT INTO coach_profiles VALUES($id,'Prueba','Histórica','1980-01-01','ES','Ciudad',3,'generic-v1','2026-07-01');";
            cmd.Parameters.AddWithValue("$id",old.Id.ToString());cmd.ExecuteNonQuery();
        }
        var hash=SHA256.HashData(File.ReadAllBytes(db));Assert.Contains(old,store.List());
        Assert.Equal(old,store.Create(old));Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(db)));
        var career=Store().Create(Request(old,CareerStartMode.Unemployed));
        Assert.Equal(CoachExperience.Experienced,career.Coach.ProfileSnapshot.Experience);
    }
    [Fact] public void NonAnnualHistoricalDatesAreNotRelabelledOrRewritten()
    {
        var terms=new ManagerEmploymentTerms(new(2026,7,1),new(2028,6,30),10000m,"EUR",null);
        Assert.Equal(new DateOnly(2028,6,30),terms.EndDate);Assert.Equal(24m,terms.CalculateDurationMonths());
    }
}
