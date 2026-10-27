using System.Security.Cryptography;
using System.Xml.Linq;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
using Furvo.Core.Worlds;
using Furvo.Persistence;
using Xunit;

namespace Furvo.Tests;

public sealed class NavigationFinanceQueryTests
{
    private static string Database => Path.Combine(AppContext.BaseDirectory, "published-v4.db");
    [Fact]
    public void SeasonsComeOnlyFromPublishedWorldAndDoNotWriteFinancialHistory()
    {
        var hash = SHA256.HashData(File.ReadAllBytes(Database)); var data = new SqliteWorldStore(Database).Load();
        var seasons = PublishedWorldQueries.Open(Database).GetSeasons();
        var row = Assert.Single(seasons); var source = Assert.Single(data.Seasons);
        Assert.Equal(source.Id, row.Id); Assert.Equal("2026/27", row.Name);
        Assert.Equal(source.StartDate,row.StartDate); Assert.Equal(source.EndDate,row.EndDate);
        Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(Database)));
    }
    [Fact]
    public void SeasonQueryHandlesEmptyAndMultipleActualSeasonsInStableOrder()
    {
        Assert.Empty(new WorldQueries(new WorldData(),new DateOnly(2026,7,1)).GetSeasons());
        var first = new Season(SeasonId.New(),new DateOnly(2026,7,1),new DateOnly(2027,6,30),"2026/27");
        var next = new Season(SeasonId.New(),new DateOnly(2027,7,1),new DateOnly(2028,6,30),"2027/28");
        var rows = new WorldQueries(new WorldData(seasons:[next,first]),first.StartDate).GetSeasons();
        Assert.Equal(new[] { first.Id,next.Id },rows.Select(r=>r.Id));
    }
    [Fact]
    public void SpecificAndGenericPositionsRemainInsideTheirGroupsWithoutChangingPlayers()
    {
        var world = new SqliteWorldStore(Database).Load(); var club = world.Clubs[0];
        var original = new WorldQueries(world,new DateOnly(2026,7,1)); var squad = original.GetSquad(club.Id);
        var one = squad.First(p=>p.Group==PlayerPosition.Goalkeeper).Id;
        var keeper = world.Players.Single(p=>p.Id==one);
        var genericKeeper = new Player(keeper.Id,keeper.Name,keeper.BirthDate,keeper.Nationality,keeper.MainPosition,keeper.Attributes);
        // Fixture aislado en memoria: no se escribe ni cambia la publicación.
        var mixed = new WorldData(clubs:world.Clubs,players:world.Players.Select(p=>p.Id==one?genericKeeper:p),contracts:world.Contracts);
        var queries = new WorldQueries(mixed,original.ReferenceDate);
        var sorted = queries.GetSquad(club.Id,SquadSort.Position);
        Assert.Equal(24,sorted.Count);
        Assert.Equal(3,sorted.TakeWhile(p=>p.Group==PlayerPosition.Goalkeeper).Count());
        Assert.Equal(sorted.Select(p=>p.Group).Order(),sorted.Select(p=>p.Group));
        Assert.Equal(original.GetSquad(club.Id).Select(p=>p.Id).OrderBy(x=>x.ToString()),sorted.Select(p=>p.Id).OrderBy(x=>x.ToString()));
        foreach(var c in original.GetClubs())
        {
            var rows = original.GetSquad(c.Id,SquadSort.Position);
            Assert.Equal(rows.Select(p=>p.Group).Order(),rows.Select(p=>p.Group));
            Assert.Equal(24,rows.Count);
        }
    }
    [Fact]
    public void MidfieldSideCodesStayDistinctFromWingersAndResourcesHaveConcreteNames()
    {
        Assert.Equal(8,(int)PlayerRole.RightMidfielder); Assert.Equal(9,(int)PlayerRole.LeftMidfielder);
        Assert.Equal(PlayerPosition.Midfielder,PlayerPositions.GroupOf(PlayerRole.RightMidfielder));
        Assert.Equal(PlayerPosition.Midfielder,PlayerPositions.GroupOf(PlayerRole.LeftMidfielder));
        Assert.Equal(PlayerPosition.Forward,PlayerPositions.GroupOf(PlayerRole.RightWinger));
        Assert.Equal(PlayerPosition.Forward,PlayerPositions.GroupOf(PlayerRole.LeftWinger));
        var strings = Strings();
        Assert.Equal("Centrocampista derecho",strings["Position_RightMidfielder"]);
        Assert.Equal("Centrocampista izquierdo",strings["Position_LeftMidfielder"]);
        Assert.DoesNotContain(strings.Where(p=>p.Key.StartsWith("Position_")),p=>p.Value is "Banda derecha" or "Banda izquierda");
    }
    [Fact]
    public void EconomicLabelsSeparateSubscriptionsWagesTransfersAndDebtPrincipal()
    {
        var strings = Strings();
        Assert.Equal("Abonos",strings["EconomySubscribers"]); Assert.Equal("Abonados",strings["ClubSubscribers"]);
        Assert.Equal("Compras de jugadores",strings["EconomySignings"]); Assert.Equal("Ventas de jugadores",strings["EconomyTransferIncome"]);
        Assert.Equal("Fichajes restante",strings["EconomyRemainingBudget"]); Assert.Equal("Margen salarial restante",strings["EconomyRemainingWages"]);
        Assert.Equal("Intereses de la deuda",strings["EconomyInterest"]);
        Assert.Contains("no es gasto",strings["EconomyPrincipalRepayment"]);
        Assert.Equal("No evaluado",strings["NotEvaluated"]);
    }
    private static Dictionary<string,string> Strings() => XDocument.Load(Path.Combine(AppContext.BaseDirectory,"UiStrings.resx"))
        .Root!.Elements("data").ToDictionary(e=>e.Attribute("name")!.Value,e=>e.Element("value")!.Value);
}
