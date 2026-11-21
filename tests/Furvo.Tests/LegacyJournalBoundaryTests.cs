using Furvo.Core.Domain;
using Furvo.Core.Development;
using Furvo.Persistence;
using Xunit;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    [Fact]
    public void IndependentAcquisitionMustNotBypassAnOpenedJournal()
    {
        var (_,file,_)=TacticalCareer();var service=new SqliteSportsStore(file);var club=service.Facility().Club;
        var resource=SqliteEconomyStore.ReadResources(file).Single(r=>r.ClubId==club);
        using(var c=Connection(file))using(var t=c.BeginTransaction()){
            using var q=c.CreateCommand();q.Transaction=t;q.CommandText="SELECT save_id FROM career_state";
            var save=new SaveId(Guid.Parse((string)q.ExecuteScalar()!));
            SqliteJournal.Migrate(c,t,save);SqliteJournal.Open(c,t,save,club,resource.SeasonId,service.CareerDate(),Guid.NewGuid());t.Commit();
        }
        var request=new FacilityAcquisition(Guid.NewGuid(),club,"footbonaut",service.CareerDate(),null,null,null,null,null,null,null,"PendingQuote",null);
        service.RequestFacility(request);
        var quote=request with{Purchase=100m,Installation=20m,AnnualMaintenance=5m,Operating=1m,Currency="EUR",LeadDays=10,SpaceConfirmed=true,Status="Approved",Authorization="fixture-only"};
        Assert.Throws<InvalidOperationException>(()=>service.CommitAcquisition(quote,new FixtureAuthorization(true)));
        Assert.Equal(resource.Cash,SqliteEconomyStore.ReadResources(file).Single(r=>r.ClubId==club).Cash);
        Assert.Equal("PendingQuote",Assert.Single(service.Acquisitions()).Status);
    }
}
