using System.Security.Cryptography;
using Furvo.Core.Domain;
using Furvo.Core.Worlds;
using Furvo.Persistence;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Furvo.Tests;

/// <summary>Compara las publicaciones reales y prueba la corrección única, sin modificar archivos de origen o partidas ajenas.</summary>
public sealed class AdjustedWorldTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "FurvoAdjustedWorldTests", Guid.NewGuid().ToString("N"));
    private static string V3 => Path.Combine(AppContext.BaseDirectory, "published-v3.db");
    private static string V4 => Path.Combine(AppContext.BaseDirectory, "published-v4.db");
    private static byte[] Hash(string p) => SHA256.HashData(File.ReadAllBytes(p));
    public AdjustedWorldTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void PublishedV4ChangesOnlyFraguasVisibleReputationAndKeepsEveryPlayerAndAttribute()
    {
        var v3 = new SqliteWorldStore(V3);
        var v4 = new SqliteWorldStore(V4);
        var source = v3.Load();
        var target = v4.Load();
        Assert.NotEqual(v3.ReadInfo().WorldId, v4.ReadInfo().WorldId);
        var changes = target.Clubs.Where(c => c != source.Clubs.Single(x => x.Id == c.Id)).ToArray();
        var changed = Assert.Single(changes);
        Assert.Equal("72026027-0003-4000-8000-000000000005", changed.Id.ToString());
        Assert.Equal("Fragua del Sur", changed.Profile!.ShortName);
        Assert.Equal(2, changed.Reputation);
        Assert.Equal(1, source.Clubs.Single(c => c.Id == changed.Id).Reputation);
        // Restaurar solo ese campo permite comparar todos los datos, incluidos los diez atributos ocultos, sin omitir entidades.
        var restored = new WorldData(target.Clubs.Select(c => c.Id == changed.Id ? source.Clubs.Single(x => x.Id == c.Id) : c),
            target.Players, target.Contracts, target.Coaches, target.Competitions, target.Seasons, target.Registrations);
        Assert.Equal(InitialSquads2026.Fingerprint(source), InitialSquads2026.Fingerprint(restored));
        Assert.Equal(480, target.Players.Count);
        Assert.Equal(29280, target.Players.Sum(p => p.Attributes.AllValues.Count));
        Assert.Equal(v3.ReadInfo().WorldId, InitialSquads2026.ReadGenerationInfo(V4)!.SourceWorldId);
        Assert.Equal(WorldFileKind.InitialWorld, v4.ReadInfo().Kind);
    }

    [Fact]
    public void RevisedInternalCatalogHasTwentyExactValuesAndOnlyOneDiffersFromPreviousApproval()
    {
        var expected = new Dictionary<string, int>
        {
            ["Horizonte"] = 98, ["Brisa"] = 96, ["Pedernal"] = 86, ["Aljarafe"] = 76,
            ["Ferrería"] = 74, ["Lumbre"] = 72, ["Azahar"] = 68, ["Mareal"] = 66,
            ["Ceramista"] = 64, ["Faro"] = 54, ["Ensenada"] = 52, ["Litoral"] = 47,
            ["Senda"] = 44, ["Mosaico"] = 34, ["Abra"] = 32, ["Acequia"] = 30,
            ["Umbral"] = 28, ["Robledal"] = 26, ["Palmeral"] = 24, ["Fragua del Sur"] = 32
        };
        var source = new SqliteWorldStore(V3).Load();
        var revised = new SqliteWorldStore(V4).Load();
        var values = AdjustedInitialWorld2026.ReadReputations();
        Assert.Equal(20, values.Count);
        foreach (var value in values)
        {
            Assert.Equal(expected[value.Name], value.Internal);
            Assert.Equal(value.Name, revised.Clubs.Single(c => c.Id == value.ClubId).Profile!.ShortName);
            Assert.Equal(Math.Min(5, value.Internal / 20 + 1), value.Visible);
            Assert.Equal(value.Visible, revised.Clubs.Single(c => c.Id == value.ClubId).Reputation);
            if (value.Name != "Fragua del Sur") Assert.Equal(value.Visible, source.Clubs.Single(c => c.Id == value.ClubId).Reputation);
        }
    }

    [Fact]
    public void VisibleScaleCoversZeroToOneHundredAndRejectsInvalidValues()
    {
        var id = ClubId.New();
        for (var value = 0; value <= 100; value++)
            Assert.Equal(Math.Min(5, value / 20 + 1), new InitialClubReputation(id, "Fixture", value).Visible);
        Assert.Throws<ArgumentOutOfRangeException>(() => new InitialClubReputation(id, "Fixture", -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InitialClubReputation(id, "Fixture", 101));
    }

    [Fact]
    public void NewPublicationIsIdempotentAndNeverOverwritesV3OrSaves()
    {
        var destination = Path.Combine(_directory, "v4.db");
        var before = Hash(V3);
        var world = AdjustedInitialWorld2026.LoadOrCreate(V3, destination);
        var targetHash = Hash(destination);
        Assert.Equal(InitialSquads2026.Fingerprint(new SqliteWorldStore(V4).Load()), InitialSquads2026.Fingerprint(world.Load()));
        Assert.Equal(world.ReadInfo(), AdjustedInitialWorld2026.LoadOrCreate(V3, destination).ReadInfo());
        Assert.Equal(targetHash, Hash(destination));
        Assert.Throws<InvalidOperationException>(() => AdjustedInitialWorld2026.LoadOrCreate(V3, V3));
        Assert.Equal(before, Hash(V3));
        var savePath = Path.Combine(_directory, "save.db");
        SqliteWorldFiles.CreateSave(destination, savePath, SaveId.New());
        var saveHash = Hash(savePath);
        Assert.Throws<InvalidOperationException>(() => AdjustedInitialWorld2026.LoadOrCreate(V3, savePath));
        Assert.Equal(saveHash, Hash(savePath));
        var missing = Path.Combine(_directory, "missing.db");
        Assert.Throws<SqliteException>(() => AdjustedInitialWorld2026.LoadOrCreate(missing, Path.Combine(_directory, "unused.db")));
        Assert.False(File.Exists(missing));
    }

    [Fact]
    public void V4QueriesKeepAllSquadsAndFiftyOnePublicAttributes()
    {
        var queries = PublishedWorldQueries.Open(V4);
        var before = Hash(V4);
        Assert.Equal(20, queries.GetClubs().Count);
        Assert.Equal(2, queries.GetClubs().Single(c => c.Id.ToString() == "72026027-0003-4000-8000-000000000005").VisibleReputation);
        foreach (var club in queries.GetClubs())
        {
            var squad = queries.GetSquad(club.Id);
            Assert.Equal(24, squad.Count);
            foreach (var player in squad)
            {
                var groups = queries.GetPlayer(club.Id, player.Id).AttributeGroups;
                Assert.Equal(new[] { 17, 16, 8, 10 }, groups.Select(g => g.Values.Count));
                Assert.DoesNotContain(groups, g => g.Category == AttributeCategory.Hidden);
                Assert.All(groups.SelectMany(g => g.Values), a => Assert.InRange(a.Value, 1, 20));
            }
        }
        Assert.Equal(before, Hash(V4));
    }

    [Fact]
    public void V4KeepsSchemaFourAndPassesSqliteIntegrityChecks()
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = V4, Mode = SqliteOpenMode.ReadOnly, ForeignKeys = true, Pooling = false }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        Assert.Equal(4L, (long)command.ExecuteScalar()!);
        command.CommandText = "PRAGMA integrity_check;";
        Assert.Equal("ok", command.ExecuteScalar());
        command.CommandText = "PRAGMA foreign_key_check;";
        using var reader = command.ExecuteReader();
        Assert.False(reader.Read());
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
