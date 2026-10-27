using System.Security.Cryptography;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
using Furvo.Core.Worlds;
using Furvo.Persistence;
using Xunit;

namespace Furvo.Tests;

/// <summary>Contrasta datos institucionales con SQLite y garantiza que las propuestas no se presentan como historia real.</summary>
public sealed class ClubDetailsQueryTests
{
    private static string Database => Path.Combine(AppContext.BaseDirectory, "published-v4.db");
    [Fact]
    public void AllTwentyClubDetailsUseExistingDatabaseValuesAndCurrentRegistrations()
    {
        var world = new SqliteWorldStore(Database).Load(); var queries = PublishedWorldQueries.Open(Database);
        foreach (var club in world.Clubs)
        {
            var data = queries.GetClubDetails(club.Id);
            Assert.Equal(club.Id, data.Club.Id); Assert.Equal(club.Name, data.Club.Name); Assert.Equal(club.Reputation, data.Club.VisibleReputation);
            Assert.Equal(club.Country, data.Identity.Country); Assert.Equal(club.Profile!.Region, data.Identity.Region);
            Assert.Equal(club.Profile.FoundationYear, data.Identity.FoundationYear); Assert.Equal(club.Profile.PrimaryColor, data.Identity.PrimaryColor);
            Assert.Equal(club.Profile.SecondaryColor, data.Identity.SecondaryColor); Assert.Equal(club.Profile.SportingIdentity, data.Identity.Tradition);
            Assert.Equal(club.Profile.StadiumName, data.Stadium.Name); Assert.Equal(club.Profile.StadiumCapacity, data.Stadium.Capacity);
            var competition = Assert.Single(data.Competitions); Assert.Equal(world.Competitions.Single().Id, competition.CompetitionId);
            Assert.Equal(world.Seasons.Single().Id, competition.SeasonId);
        }
    }
    [Fact]
    public void UnknownInstitutionalAndStadiumFieldsRemainNullRatherThanInvented()
    {
        var queries = PublishedWorldQueries.Open(Database);
        foreach (var club in queries.GetClubs())
        {
            var data = queries.GetClubDetails(club.Id);
            Assert.Null(data.Identity.Nickname); Assert.Null(data.Identity.SupportersName); Assert.Null(data.Identity.Rivalries); Assert.Null(data.Identity.Subscribers);
            Assert.Null(data.Stadium.City); Assert.Null(data.Stadium.AverageAttendance); Assert.Null(data.Stadium.RecordAttendance); Assert.Null(data.Stadium.OccupancyPercent);
        }
    }
    [Fact]
    public void NoApprovedHistoryMeansNoFakeSeasonResultsTitlesPeopleOrCounts()
    {
        var queries = PublishedWorldQueries.Open(Database);
        foreach (var club in queries.GetClubs())
        {
            var history = queries.GetClubHistory(club.Id);
            Assert.Equal(club.Id, history.ClubId); Assert.False(history.IsApprovedDataAvailable);
            Assert.Empty(history.Standings); Assert.Empty(history.Honours); Assert.Empty(history.Players); Assert.Empty(history.Coaches);
            foreach (var property in typeof(ClubHistoryDetails).GetProperties().Where(p => p.PropertyType == typeof(int?) || p.PropertyType == typeof(string)))
                Assert.Null(property.GetValue(history));
        }
    }
    [Fact]
    public void MissingProfilesAndExpiredRegistrationsDoNotProduceInstitutionalOrCompetitionData()
    {
        var world = new SqliteWorldStore(Database).Load(); var club = world.Clubs[0];
        var minimal = new Club(club.Id, club.Name, club.City, club.Country, club.Reputation);
        var data = new WorldQueries(new WorldData(clubs: [minimal]), new DateOnly(2026, 7, 1)).GetClubDetails(club.Id);
        Assert.Null(data.Identity.FoundationYear); Assert.Null(data.Stadium.Name); Assert.Empty(data.Competitions);
        Assert.Empty(new WorldQueries(world, world.Seasons.Single().EndDate.AddDays(1)).GetClubDetails(club.Id).Competitions);
    }
    [Fact]
    public void QueriesRejectUnknownClubsAndDoNotChangePublishedBytesOrLeakPlayerData()
    {
        var before = SHA256.HashData(File.ReadAllBytes(Database)); var queries = PublishedWorldQueries.Open(Database);
        Assert.Throws<KeyNotFoundException>(() => queries.GetClubDetails(ClubId.New()));
        Assert.Throws<KeyNotFoundException>(() => queries.GetClubHistory(ClubId.New()));
        foreach (var club in queries.GetClubs()) { _ = queries.GetClubDetails(club.Id); _ = queries.GetClubHistory(club.Id); Assert.Equal(24, queries.GetSquad(club.Id).Count); }
        Assert.Equal(before, SHA256.HashData(File.ReadAllBytes(Database)));
        Assert.DoesNotContain(typeof(ClubDetails).GetProperties(), p => p.PropertyType == typeof(Player) || p.PropertyType == typeof(PlayerAttributes));
    }
}
