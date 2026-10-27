using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
using Furvo.Core.Worlds;
using Furvo.Persistence;
using Xunit;

namespace Furvo.Tests;

/// <summary>Prueba la frontera pública de consulta sobre el SQLite publicado y su caché, sin depender de Godot.</summary>
public sealed class WorldQueryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "FurvoQueryTests", Guid.NewGuid().ToString("N"));
    private static string Publication => Path.Combine(AppContext.BaseDirectory, "published-v3.db");
    public WorldQueryTests() => Directory.CreateDirectory(_directory);
    private static byte[] Hash(string p) => SHA256.HashData(File.ReadAllBytes(p));

    [Fact]
    public void PublishedSqliteLoadsTwentyClubsAndTwentyFourPlayersEachWithoutWriting()
    {
        var before = Hash(Publication);
        var queries = PublishedWorldQueries.Open(Publication);
        Assert.Equal(new DateOnly(2026, 7, 1), queries.ReferenceDate);
        Assert.Equal(20, queries.GetClubs().Count);
        var players = queries.GetClubs().SelectMany(c =>
        {
            Assert.InRange(c.VisibleReputation!.Value, 1, 5);
            var squad = queries.GetSquad(c.Id);
            Assert.Equal(24, squad.Count);
            Assert.Equal(3, squad.Count(x => x.Group == PlayerPosition.Goalkeeper));
            return squad;
        }).ToArray();
        Assert.Equal(480, players.Select(x => x.Id).Distinct().Count());
        Assert.Equal(before, Hash(Publication));
    }

    [Fact]
    public void EveryPublicPlayerHasExactlyFiftyOneAttributesInFourCategoriesAndNoHiddenLeaks()
    {
        var queries = PublishedWorldQueries.Open(Publication);
        var hidden = PlayerAttributeCatalog.All.Where(x => x.IsHidden).Select(x => x.Code).ToHashSet();
        var publicCodes = PlayerAttributeCatalog.All.Where(x => !x.IsHidden).Select(x => x.Code).OrderBy(x => x).ToArray();
        foreach (var club in queries.GetClubs())
        foreach (var row in queries.GetSquad(club.Id))
        {
            var details = queries.GetPlayer(club.Id, row.Id);
            Assert.Equal(row, details.Player);
            Assert.Equal(club, details.Club);
            Assert.Equal(4, details.AttributeGroups.Count);
            Assert.Equal(new[] { 17, 16, 8, 10 }, details.AttributeGroups.Select(g => g.Values.Count));
            Assert.DoesNotContain(details.AttributeGroups, g => g.Category == AttributeCategory.Hidden);
            var values = details.AttributeGroups.SelectMany(g => g.Values).ToArray();
            Assert.Equal(publicCodes, values.Select(v => v.Code).OrderBy(x => x));
            Assert.All(values, v => { Assert.InRange(v.Value, 1, 20); Assert.DoesNotContain(v.Code, hidden); });
            var json = JsonSerializer.Serialize(details);
            foreach (var code in hidden) Assert.DoesNotContain(code, json);
        }
        // Ningún DTO devuelve Player, PlayerAttributes o WorldData por una propiedad pública.
        foreach (var type in new[] { typeof(PlayerSummary), typeof(PlayerDetails), typeof(VisibleAttributeGroup), typeof(VisibleAttribute), typeof(ClubSummary) })
            Assert.DoesNotContain(type.GetProperties(), p => p.PropertyType == typeof(Player) ||
                p.PropertyType == typeof(PlayerAttributes) || p.PropertyType == typeof(WorldData));
    }

    [Theory]
    [InlineData(SquadSort.Name, false)] [InlineData(SquadSort.Name, true)]
    [InlineData(SquadSort.Position, false)] [InlineData(SquadSort.Position, true)]
    [InlineData(SquadSort.Age, false)] [InlineData(SquadSort.Age, true)]
    public void SortingIsTypedStableAndDoesNotDropPlayers(SquadSort sort, bool descending)
    {
        var queries = PublishedWorldQueries.Open(Publication);
        foreach (var club in queries.GetClubs())
        {
            var original = queries.GetSquad(club.Id);
            var result = queries.GetSquad(club.Id, sort, descending);
            Assert.Equal(original.Select(p => p.Id).OrderBy(x => x.ToString()), result.Select(p => p.Id).OrderBy(x => x.ToString()));
            for (var i = 1; i < result.Count; i++)
            {
                var comparison = sort switch
                {
                    SquadSort.Name => StringComparer.OrdinalIgnoreCase.Compare(result[i - 1].Name, result[i].Name),
                    SquadSort.Position => Nullable.Compare(result[i - 1].Position, result[i].Position),
                    _ => Nullable.Compare(result[i - 1].Age, result[i].Age)
                };
                Assert.True(descending ? comparison >= 0 : comparison <= 0);
            }
        }
    }

    [Fact]
    public void QueriesAreIndependentOfInputOrderAndProcessCulture()
    {
        var world = new SqliteWorldStore(Publication).Load();
        var reversed = new WorldData(clubs: world.Clubs.Reverse(), players: world.Players.Reverse(), contracts: world.Contracts.Reverse());
        var first = new WorldQueries(world, new DateOnly(2026, 7, 1));
        var old = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            var second = new WorldQueries(reversed, first.ReferenceDate);
            Assert.Equal(first.GetClubs(), second.GetClubs());
            foreach (var club in first.GetClubs())
            foreach (var sort in Enum.GetValues<SquadSort>())
                Assert.Equal(first.GetSquad(club.Id, sort), second.GetSquad(club.Id, sort));
        }
        finally { CultureInfo.CurrentCulture = old; }
    }

    [Fact]
    public void MembershipUsesInclusiveContractDatesAndRejectsUnknownOrOtherClubPlayers()
    {
        var world = new SqliteWorldStore(Publication).Load();
        var contract = world.Contracts[0];
        var one = new WorldData(clubs: world.Clubs, players: world.Players, contracts: [contract]);
        Assert.Single(new WorldQueries(one, contract.StartDate!.Value).GetSquad(contract.ClubId));
        Assert.Single(new WorldQueries(one, contract.EndDate!.Value).GetSquad(contract.ClubId));
        Assert.Empty(new WorldQueries(one, contract.StartDate.Value.AddDays(-1)).GetSquad(contract.ClubId));
        Assert.Empty(new WorldQueries(one, contract.EndDate.Value.AddDays(1)).GetSquad(contract.ClubId));
        var queries = PublishedWorldQueries.Open(Publication);
        var other = world.Clubs.First(c => c.Id != contract.ClubId);
        Assert.Throws<KeyNotFoundException>(() => queries.GetSquad(ClubId.New()));
        Assert.Throws<KeyNotFoundException>(() => queries.GetPlayer(other.Id, contract.PlayerId));
        Assert.Throws<KeyNotFoundException>(() => queries.GetPlayer(contract.ClubId, PlayerId.New()));
        Assert.Throws<ArgumentOutOfRangeException>(() => queries.GetSquad(contract.ClubId, (SquadSort)99));
    }

    [Fact]
    public void MissingCorruptAndSaveFilesAreRejectedWithoutCreatingOrChangingThem()
    {
        var missing = Path.Combine(_directory, "missing.db");
        Assert.ThrowsAny<Exception>(() => PublishedWorldQueries.Open(missing));
        Assert.False(File.Exists(missing));
        var corrupt = Path.Combine(_directory, "corrupt.db");
        File.WriteAllText(corrupt, "fixture que no es SQLite");
        var before = Hash(corrupt);
        Assert.ThrowsAny<Exception>(() => PublishedWorldQueries.Open(corrupt));
        Assert.Equal(before, Hash(corrupt));
        var savePath = Path.Combine(_directory, "save.db");
        SqliteWorldFiles.CreateSave(Publication, savePath, SaveId.New());
        var saveBefore = Hash(savePath);
        Assert.Throws<InvalidDataException>(() => PublishedWorldQueries.Open(savePath));
        Assert.Equal(saveBefore, Hash(savePath));
    }

    [Fact]
    public void CacheIsRepeatableAndRejectsChangedBytesWithoutOverwritingThem()
    {
        using var publication = File.OpenRead(Publication);
        var local = PublishedWorldQueries.Materialize(publication, _directory);
        Assert.Equal(Hash(Publication), Hash(local));
        Assert.Equal(20, PublishedWorldQueries.Open(local).GetClubs().Count);
        publication.Position = 0;
        Assert.Equal(local, PublishedWorldQueries.Materialize(publication, _directory));
        File.WriteAllText(local, "fixture modificada");
        var changed = Hash(local);
        publication.Position = 0;
        Assert.Throws<InvalidDataException>(() => PublishedWorldQueries.Materialize(publication, _directory));
        Assert.Equal(changed, Hash(local));
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
        Assert.Throws<ArgumentException>(() => PublishedWorldQueries.Materialize(publication, "relative"));
    }

    [Fact]
    public void SpanishResourcesCoverEveryVisibleAttributeAndEveryPositionWithoutHiddenNames()
    {
        var xml = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "UiStrings.resx"));
        var entries = xml.Root!.Elements("data").ToDictionary(e => e.Attribute("name")!.Value, e => e.Element("value")!.Value);
        foreach (var definition in PlayerAttributeCatalog.All)
        {
            Assert.Equal(!definition.IsHidden, entries.ContainsKey("Attribute_" + definition.Code));
            if (!definition.IsHidden) Assert.Equal(definition.Name, entries["Attribute_" + definition.Code]);
        }
        foreach (var position in Enum.GetValues<PlayerRole>()) Assert.False(string.IsNullOrWhiteSpace(entries["Position_" + position]));
        Assert.Equal(entries.Count, entries.Keys.Distinct().Count());
    }

    /// <summary>Una ficha parcial falla expresamente y no se presenta como una ficha de 51 atributos completa.</summary>
    [Fact]
    public void IncompleteAttributesAreReportedAndSortingPreservesTheSelectedStableId()
    {
        var world = new SqliteWorldStore(Publication).Load();
        var player = world.Players[0];
        var contract = world.Contracts.Single(c => c.PlayerId == player.Id);
        var partial = new Player(player.Id, player.Name, player.BirthDate, player.Nationality, player.MainPosition,
            new PlayerAttributes(new Dictionary<string, int> { ["technique"] = 10 }), player.Positions);
        var incomplete = new WorldQueries(new WorldData(clubs: world.Clubs, players: [partial], contracts: [contract]), new DateOnly(2026, 7, 1));
        Assert.Throws<InvalidDataException>(() => incomplete.GetPlayer(contract.ClubId, player.Id));
        var queries = PublishedWorldQueries.Open(Publication);
        var selected = queries.GetSquad(contract.ClubId).Single(p => p.Id == player.Id);
        foreach (var sort in Enum.GetValues<SquadSort>())
        foreach (var descending in new[] { false, true })
        {
            var reordered = queries.GetSquad(contract.ClubId, sort, descending);
            Assert.Equal(selected, Assert.Single(reordered, p => p.Id == selected.Id));
            Assert.Equal(selected.Id, queries.GetPlayer(contract.ClubId, selected.Id).Player.Id);
        }
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
