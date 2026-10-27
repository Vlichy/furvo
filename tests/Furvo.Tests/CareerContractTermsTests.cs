using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Furvo.Tests;

public sealed partial class CareerCreationTests
{
    [Theory]
    [InlineData(2026,7,1,2027,6,30,"12")]
    [InlineData(2026,1,15,2026,2,14,"1")]
    [InlineData(2026,1,1,2026,1,15,"0.48")]
    [InlineData(2024,2,1,2024,2,29,"1")]
    [InlineData(2026,1,31,2026,2,27,"1")]
    [InlineData(2026,1,31,2026,2,28,"1.03")]
    [InlineData(9999,12,1,9999,12,31,"1")]
    public void DurationIsDerivedFromInclusiveCalendarDatesIncludingPartialAndLeapMonths(int y,int m,int d,int ey,int em,int ed,string months)
    {
        var terms=new ManagerEmploymentTerms(new DateOnly(y,m,d),new DateOnly(ey,em,ed),100,"EUR",null);
        Assert.Equal(decimal.Parse(months,CultureInfo.InvariantCulture),terms.CalculateDurationMonths());
        var json=JsonNode.Parse(JsonSerializer.Serialize(terms))!.AsObject();
        Assert.False(json.ContainsKey("DurationSeasons"));Assert.False(json.ContainsKey("DurationMonths"));
    }
    [Theory][InlineData(0)][InlineData(-1)]
    public void EndMustBeStrictlyAfterStart(int days)
    {
        var p=Profile();var offer=Offer(p);offer=offer with{Terms=offer.Terms with{EndDate=offer.Terms.StartDate!.Value.AddDays(days)}};
        Assert.Contains("OfferEndInvalid",offer.SigningBlocks());Assert.Null(offer.Terms.CalculateDurationMonths());
        Assert.Throws<InvalidOperationException>(()=>Store().Create(Request(p,CareerStartMode.SignOffer,offer)));
    }
    [Theory][InlineData(0)][InlineData(-1)]
    public void SalaryMustBePositive(int salary)
    {
        var offer=Offer(Profile());offer=offer with{Terms=offer.Terms with{GrossAnnualSalary=salary}};
        Assert.Contains("OfferSalaryInvalid",offer.SigningBlocks());
    }
    [Theory]
    [InlineData(null,"OfferCurrencyMissing")][InlineData("","OfferCurrencyMissing")]
    [InlineData("XYZ","OfferCurrencyInvalid")][InlineData("eur","OfferCurrencyInvalid")][InlineData("EURO","OfferCurrencyInvalid")]
    [InlineData("EUR",null)]
    public void CurrencyHasSpecificMissingOrUnsupportedReason(string? currency,string? expected)
    {
        var offer=Offer(Profile());offer=offer with{Terms=offer.Terms with{Currency=currency}};
        if(expected is null)Assert.Empty(offer.SigningBlocks());else Assert.Contains(expected,offer.SigningBlocks());
    }
    [Fact] public void ValidEmploymentWithPendingAdvisoryPreferencesCanSignWithoutInventedAcceptedCommitments()
    {
        var p=Profile();var offer=Offer(p) with
        {Commitments=new ManagerCommitments("",CommitmentApproval.Draft,[],[new ContractObjective("pending","",BoardObjectiveHorizon.Season,null,null,null)])
            {ContractualPhilosophy=[new ContractualPrinciple("pending","",null,false)]},TacticalProject=new ManagerTacticalProject(null,null,null,null)};
        Assert.Empty(offer.SigningBlocks());var store=Store();var career=store.Create(Request(p,CareerStartMode.SignOffer,offer));
        var signed=store.Load(career.Id).Contract!;Assert.Equal(2,signed.SnapshotVersion);
        Assert.Empty(signed.AcceptedOffer.Commitments.Principles);Assert.Empty(signed.AcceptedOffer.Commitments.Objectives);Assert.Empty(signed.AcceptedOffer.Commitments.ContractualPhilosophy);
        Assert.False(signed.AcceptedOffer.TacticalProject.Mandatory);Assert.Null(signed.AcceptedOffer.TacticalProject.Formation);
        Assert.Equal(10000m,signed.AcceptedOffer.Terms.GrossAnnualSalary);Assert.Equal(12m,signed.AcceptedOffer.Terms.CalculateDurationMonths());
    }
    [Theory]
    [InlineData("objective","content","OfferObjectiveContentMissing")][InlineData("objective","priority","OfferObjectivePriorityMissing")]
    [InlineData("philosophy","content","OfferPhilosophyContentMissing")][InlineData("philosophy","priority","OfferPhilosophyPriorityMissing")]
    [InlineData("tactical","content","OfferTacticalContentMissing")][InlineData("tactical","priority","OfferTacticalPriorityMissing")]
    public void OnlyExpresslyEnforceableObligationsRequireTheirContentAndPriority(string section,string missing,string reason)
    {
        var p=Profile();var offer=Offer(p);
        if(section=="objective")offer=offer with{Commitments=offer.Commitments with{Objectives=[new ContractObjective("required",missing=="content"?"":"Contenido exigible",BoardObjectiveHorizon.Season,missing=="priority"?null:BoardObjectivePriority.High,null,true)]}};
        if(section=="philosophy")offer=offer with{Commitments=offer.Commitments with{ContractualPhilosophy=[new ContractualPrinciple("required",missing=="content"?"":"Contenido exigible",missing=="priority"?null:BoardObjectivePriority.High,true)]}};
        if(section=="tactical")offer=offer with{TacticalProject=new ManagerTacticalProject(missing=="content"?null:"4-4-2",null,null,true){Priority=missing=="priority"?null:BoardObjectivePriority.High}};
        Assert.Contains(reason,offer.SigningBlocks());Assert.Throws<InvalidOperationException>(()=>Store().Create(Request(p,CareerStartMode.SignOffer,offer)));
    }
    [Fact] public void ExplicitCompletePhilosophyAndTacticalObligationsAreFrozenWhileAdvisoryOnesAreNot()
    {
        var p=Profile();var offer=Offer(p) with{Commitments=Offer(p).Commitments with
            {ContractualPhilosophy=[new("binding","Contenido aceptado",BoardObjectivePriority.High,true),new("advisory","",null,false)]},
            TacticalProject=new ManagerTacticalProject(null,"Contenido táctico exigible",null,true){Priority=BoardObjectivePriority.Medium}};
        Assert.Empty(offer.SigningBlocks());var store=Store();var career=store.Create(Request(p,CareerStartMode.SignOffer,offer));
        Assert.Equal("binding",Assert.Single(store.Load(career.Id).Contract!.AcceptedOffer.Commitments.ContractualPhilosophy).Code);
        Assert.Equal(BoardObjectivePriority.Medium,store.Load(career.Id).Contract!.AcceptedOffer.TacticalProject.Priority);
    }
    [Theory][InlineData(TerminationClauseKind.Unknown)][InlineData(TerminationClauseKind.Defined)][InlineData(TerminationClauseKind.NoSpecificEconomicClause)]
    public void TerminationUnknownDefinedAndNoEconomicClauseRemainDistinctAfterPersistence(TerminationClauseKind kind)
    {
        var p=Profile();var offer=Offer(p);offer=offer with{Terms=offer.Terms with{TerminationKind=kind,TerminationConditions=kind==TerminationClauseKind.Defined?"Cláusula solo de pruebas":null}};
        Assert.Empty(offer.SigningBlocks());var store=Store();var career=store.Create(Request(p,CareerStartMode.SignOffer,offer));
        var terms=store.Load(career.Id).Contract!.AcceptedOffer.Terms;
        Assert.Equal(kind,terms.TerminationKind);Assert.Equal(offer.Terms.TerminationConditions,terms.TerminationConditions);
        using var c=Connection(store.PathFor(career.Id));using var cmd=c.CreateCommand();cmd.CommandText="SELECT name FROM pragma_table_info('manager_contracts');";
        using(var r=cmd.ExecuteReader()){var names=new List<string>();while(r.Read())names.Add(r.GetString(0));Assert.DoesNotContain("duration_seasons",names);Assert.DoesNotContain("duration_months",names);}
        cmd.CommandText="SELECT offer_json FROM manager_contracts;";var json=JsonNode.Parse((string)cmd.ExecuteScalar()!)!;
        Assert.False(json["Terms"]!.AsObject().ContainsKey("DurationSeasons"));Assert.False(json["Terms"]!.AsObject().ContainsKey("DurationMonths"));
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void LegacyCareerVersionOneCanBeReadWithoutMigrationOrByteChanges(bool employed)
    {
        var p=Profile();var request=Request(p,employed?CareerStartMode.SignOffer:CareerStartMode.Unemployed,employed?Offer(p):null);
        var store=Store();var file=store.PathFor(request.Id);Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        Furvo.Persistence.SqliteWorldFiles.CreateSave(Source,file,request.Id);
        using(var c=Connection(file))using(var t=c.BeginTransaction())
        {
            using var migration=typeof(Furvo.Persistence.SqliteCareerStore).Assembly.GetManifestResourceStream("Furvo.Persistence.Migrations.Career001_Initial.sql")!;
            using var reader=new StreamReader(migration);using var cmd=c.CreateCommand();cmd.Transaction=t;cmd.CommandText=reader.ReadToEnd();cmd.ExecuteNonQuery();
            var profileJson=JsonSerializer.SerializeToNode(p)!;profileJson["Id"]=p.Id.ToString();
            cmd.CommandText="INSERT INTO career_coaches VALUES($coach,$profile,$json);";cmd.Parameters.AddWithValue("$coach",request.CoachId.ToString());cmd.Parameters.AddWithValue("$profile",p.Id.ToString());cmd.Parameters.AddWithValue("$json",profileJson.ToJsonString());cmd.ExecuteNonQuery();cmd.Parameters.Clear();
            if(employed)
            {
                var offer=request.Offer!;var json=JsonSerializer.SerializeToNode(offer)!;
                json["ProfileId"]=p.Id.ToString();json["ClubId"]=offer.ClubId.ToString();json["SeasonId"]=offer.SeasonId.ToString();json["Resources"]!["ClubId"]=offer.ClubId.ToString();json["Resources"]!["SeasonId"]=offer.SeasonId.ToString();
                json["Terms"]!.AsObject().Remove("TerminationKind");json["Terms"]!["DurationSeasons"]=99;json["Terms"]!["Approved"]=true;
                json["Commitments"]!.AsObject().Remove("ContractualPhilosophy");json["TacticalProject"]!.AsObject().Remove("Priority");
                cmd.CommandText="INSERT INTO manager_contracts VALUES($contract,1,$coach,$club,'2026-07-01','2027-06-30',99,'10000','EUR','Condición histórica',$json);";
                cmd.Parameters.AddWithValue("$contract",request.ContractId!.Value.ToString());cmd.Parameters.AddWithValue("$coach",request.CoachId.ToString());cmd.Parameters.AddWithValue("$club",offer.ClubId.ToString());cmd.Parameters.AddWithValue("$json",json.ToJsonString());cmd.ExecuteNonQuery();cmd.Parameters.Clear();
            }
            cmd.CommandText="INSERT INTO career_state VALUES(1,$save,$coach,$season,'2026-07-01',$mode,$employment,$club,$contract,$hash);";
            cmd.Parameters.AddWithValue("$save",request.Id.ToString());cmd.Parameters.AddWithValue("$coach",request.CoachId.ToString());
            cmd.Parameters.AddWithValue("$season",Furvo.Persistence.PublishedWorldQueries.Open(Source).GetSeasons()[0].Id.ToString());cmd.Parameters.AddWithValue("$mode",employed?3:1);cmd.Parameters.AddWithValue("$employment",employed?2:1);
            cmd.Parameters.AddWithValue("$club",employed?request.Offer!.ClubId.ToString():DBNull.Value);cmd.Parameters.AddWithValue("$contract",employed?request.ContractId!.Value.ToString():DBNull.Value);cmd.Parameters.AddWithValue("$hash",new string('a',64));cmd.ExecuteNonQuery();t.Commit();
        }
        var hash=SHA256.HashData(File.ReadAllBytes(file));var loaded=store.Load(request.Id);
        Assert.Equal(employed?CareerEmploymentState.Employed:CareerEmploymentState.Unemployed,loaded.Employment);
        if(employed){Assert.Equal(1,loaded.Contract!.SnapshotVersion);Assert.Equal(12m,loaded.Contract.AcceptedOffer.Terms.CalculateDurationMonths());Assert.Equal(3,loaded.Contract.AcceptedOffer.Commitments.Principles.Count);}
        Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(file)));
    }
}
