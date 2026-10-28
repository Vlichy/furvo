using System.Security.Cryptography;
using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
using Furvo.Persistence;
using Xunit;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    [Theory][InlineData(CareerStartMode.Unemployed)][InlineData(CareerStartMode.RejectOffer)][InlineData(CareerStartMode.SignOffer)]
    public void WorkspaceUsesPersistedIdentityAndDateAndNavigationDoesNotWrite(CareerStartMode mode)
    {
        var p=Profile();var store=EconomicStore();var c=store.Create(Request(p,mode,mode==CareerStartMode.Unemployed?null:EconomicOffer(p)));
        var hash=SHA256.HashData(File.ReadAllBytes(store.PathFor(c.Id)));var w=new CareerWorkspace(store.Load(c.Id));
        foreach(var page in Enum.GetValues<CareerPage>())if(w.CanOpen(page))w.Open(page);
        Assert.Equal(c.Id,w.Career.Id);Assert.Equal(c.Coach.Id,w.Career.Coach.Id);Assert.Equal(c.CurrentDate,w.Career.CurrentDate);
        Assert.False(w.CanAdvanceTime);Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(store.PathFor(c.Id))));
        if(mode!=CareerStartMode.SignOffer){Assert.False(w.HasClub);Assert.Null(w.Career.ClubId);Assert.Null(w.Career.Contract);}
    }
    [Theory][InlineData(CareerPage.OwnSquad)][InlineData(CareerPage.OwnInformation)][InlineData(CareerPage.OwnEconomy)][InlineData(CareerPage.OwnHistory)]
    public void UnemployedCannotOpenOwnClubRoutesEvenWhenCalledDirectly(CareerPage page)
    {
        var p=Profile();var c=EconomicStore().Create(Request(p,CareerStartMode.Unemployed));var w=new CareerWorkspace(c);
        Assert.False(w.CanOpen(page));Assert.Throws<InvalidOperationException>(()=>w.Open(page));Assert.Equal(CareerPage.Desktop,w.Page);
        Assert.Throws<InvalidOperationException>(()=>w.RequireManagement(EconomicOffer(p).ClubId));
    }
    [Fact] public void ConsultationNeverConfersManagementOrChangesEmployer()
    {
        var p=Profile();var c=EconomicStore().Create(Request(p,CareerStartMode.SignOffer,EconomicOffer(p)));var w=new CareerWorkspace(c);
        var other=PublishedWorldQueries.Open(EconomicSource).GetClubs().First(club=>club.Id!=c.ClubId).Id;
        Assert.False(w.CanOpen(CareerPage.PublicClubs));Assert.Throws<InvalidOperationException>(()=>w.Open(CareerPage.PublicClubs));Assert.True(w.CanManageClub(c.ClubId!.Value));Assert.False(w.CanManageClub(other));
        Assert.Throws<InvalidOperationException>(()=>w.RequireManagement(other));Assert.Equal(c.ClubId,w.Career.ClubId);
        w.Open(CareerPage.OwnSquad);Assert.Equal(CareerArea.Team,w.Area);w.Open(CareerPage.OwnEconomy);Assert.Equal(CareerArea.Club,w.Area);
        w.Open(CareerPage.Contract);Assert.Equal(CareerArea.Career,w.Area);w.Open(CareerPage.Inbox);Assert.Equal(CareerArea.Home,w.Area);
    }
    [Fact] public void IndependentWorkspacesDoNotShareRoutesAndReadTheirOwnCareerEconomy()
    {
        var p=Profile();var store=EconomicStore();var first=store.Create(Request(p,CareerStartMode.SignOffer,EconomicOffer(p)));var second=store.Create(Request(p,CareerStartMode.Unemployed));
        var a=new CareerWorkspace(first);var b=new CareerWorkspace(second);a.Open(CareerPage.OwnEconomy);
        Assert.Equal(CareerPage.Desktop,b.Page);Assert.NotEqual(a.Career.Coach.Id,b.Career.Coach.Id);
        using(var connection=Connection(store.PathFor(first.Id)))using(var cmd=connection.CreateCommand())
        {cmd.CommandText="UPDATE club_economics SET cash_cents=cash_cents-500 WHERE club_id=$club;";cmd.Parameters.AddWithValue("$club",first.ClubId!.Value.ToString());cmd.ExecuteNonQuery();}
        var q=new WorldQueries(new SqliteWorldStore(store.PathFor(first.Id)).Load(),first.CurrentDate,SqliteEconomyStore.ReadResources(store.PathFor(first.Id)));
        Assert.Equal(first.Contract!.AcceptedOffer.Resources.Cash-5,q.GetFinancialResources(first.ClubId!.Value,first.SeasonId).Cash);
        Assert.Equal(first.Contract.AcceptedOffer.Resources.Cash,SqliteEconomyStore.ReadResources(store.PathFor(second.Id)).Single(r=>r.ClubId==first.ClubId).Cash);
    }
    [Fact] public void InvalidWorkspaceStateOrUnknownRoutesAreRejected()
    {
        var c=EconomicStore().Create(Request(Profile(),CareerStartMode.Unemployed));
        Assert.Throws<ArgumentException>(()=>new CareerWorkspace(c with{ClubId=ClubId.New()}));
        var w=new CareerWorkspace(c);Assert.False(w.CanOpen((CareerPage)999));Assert.Throws<InvalidOperationException>(()=>w.Open((CareerPage)999));
    }
}
