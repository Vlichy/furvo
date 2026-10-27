using System.Security.Cryptography;
using Furvo.Core.Domain;
using Furvo.Core.Worlds;
using Furvo.Persistence;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Furvo.Tests;

/// <summary>Pruebas con SQLite real y directorio temporal exclusivo; no abren ni modifican partidas del usuario.</summary>
public sealed class ClubUniverseTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "FurvoUniverseTests", Guid.NewGuid().ToString("N"));
    /// <summary>Prepara un directorio propio para aislar esta prueba de ejecuciones paralelas.</summary>
    public ClubUniverseTests() => Directory.CreateDirectory(_directory);
    private string Database(string name) => Path.Combine(_directory, name + ".db");
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static void Sql(string path, string sql)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = path, ForeignKeys = true, Pooling = false }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    /// <summary>Comprueba los dos extremos válidos sin derivar reputación de infraestructura.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void ReputationBoundariesAreValid(int value)
    {
        Assert.Equal(value, new Club(ClubId.New(), "Prueba", reputation: value).Reputation);
    }

    /// <summary>Protege los límites definitivos de reputación 1–5 en el dominio.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void ReputationOutsideTheFinalScaleIsRejected(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Club(ClubId.New(), "Prueba", reputation: value));
    }

    /// <summary>Aplica ambos límites 1–20 a todo el catálogo, incluidos los ocultos.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    public void EveryAttributeAcceptsTheFinalBoundaries(int value)
    {
        var attributes = new PlayerAttributes(PlayerAttributeCatalog.All.Select(x => new KeyValuePair<string, int>(x.Code, value)));
        Assert.Equal(61, attributes.AllValues.Count);
        Assert.All(attributes.AllValues.Values, x => Assert.Equal(value, x));
        Assert.Equal(51, attributes.VisibleValues.Count);
        Assert.DoesNotContain(attributes.VisibleValues.Keys, x => PlayerAttributeCatalog.Get(x).IsHidden);
    }

    /// <summary>Verifica que los datos publicados constituyen un universo completo, sin generar jugadores.</summary>
    [Fact]
    public void InitialUniverseContainsExactlyTwentyCompleteDistinctClubsAndRegistrations()
    {
        var store = InitialWorld2026.LoadOrCreate(Database("initial"));
        var data = store.Load();
        Assert.Equal(20, data.Clubs.Count);
        Assert.Equal(20, data.Clubs.Select(x => x.Id).Distinct().Count());
        Assert.Equal(20, data.Clubs.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        var competition = Assert.Single(data.Competitions);
        Assert.Equal("Liga Cumbre de España", competition.Name);
        Assert.Equal("España", competition.Country);
        var season = Assert.Single(data.Seasons);
        Assert.Equal("2026/27", season.Name);
        Assert.Equal(new DateOnly(2026, 7, 1), season.StartDate);
        Assert.Equal(new DateOnly(2027, 6, 30), season.EndDate);
        Assert.Equal(20, data.Registrations.Count);
        Assert.All(data.Registrations, x =>
        {
            Assert.Equal(competition.Id, x.CompetitionId);
            Assert.Equal(season.Id, x.SeasonId);
            Assert.Contains(data.Clubs, c => c.Id == x.ClubId);
        });
        Assert.All(data.Clubs, club =>
        {
            Assert.False(string.IsNullOrWhiteSpace(club.Name));
            Assert.False(string.IsNullOrWhiteSpace(club.City));
            Assert.Equal("España", club.Country);
            Assert.InRange(club.Reputation!.Value, 1, 5);
            var profile = Assert.IsType<ClubProfile>(club.Profile);
            foreach (var text in new[] { profile.ShortName, profile.Region, profile.SportingIdentity,
                profile.PrimaryColor, profile.SecondaryColor, profile.StadiumName, profile.InitialCategory })
                Assert.False(string.IsNullOrWhiteSpace(text));
            Assert.Equal("Primera categoría", profile.InitialCategory);
            Assert.InRange(profile.FoundationYear, 1, 2026);
            Assert.True(profile.StadiumCapacity > 0);
        });
        Assert.Equal(5, data.Clubs.Select(x => x.Reputation).Distinct().Count());
        Assert.Empty(data.Players);
        Assert.Empty(data.Contracts);
        Assert.Empty(data.Coaches);
        Assert.Equal(new WorldId(Guid.Parse("72026027-0000-4000-8000-000000000004")), store.ReadInfo().WorldId);
        Assert.Null(store.ReadInfo().SaveId);
    }

    /// <summary>Compara datos e identidades y exige que una segunda carga no cambie el archivo.</summary>
    [Fact]
    public void InitialLoadIsIdempotentReproducibleAndSurvivesReopening()
    {
        var path = Database("initial");
        var first = InitialWorld2026.LoadOrCreate(path);
        var expected = first.Load();
        var before = Hash(path);
        var second = InitialWorld2026.LoadOrCreate(path);
        Assert.Equal(before, Hash(path));
        Assert.Equal(expected.Clubs, second.Load().Clubs);
        Assert.Equal(expected.Clubs, new SqliteWorldStore(path).Load().Clubs);
        var independentlyGenerated = InitialWorld2026.LoadOrCreate(Database("another"));
        Assert.Equal(first.ReadInfo(), independentlyGenerated.ReadInfo());
        Assert.Equal(expected.Clubs, independentlyGenerated.Load().Clubs);
        Assert.Equal(expected.Registrations, independentlyGenerated.Load().Registrations);
    }

    /// <summary>Comprueba la protección de archivos publicados, incluso si su contenido ya no coincide.</summary>
    [Fact]
    public void PublishedWorldAndExistingSavesAreNeverSilentlyReplaced()
    {
        var initialPath = Database("initial");
        InitialWorld2026.LoadOrCreate(initialPath);
        Sql(initialPath, "UPDATE clubs SET name = name || ' cambiado' WHERE id = (SELECT min(id) FROM clubs);");
        var before = Hash(initialPath);
        Assert.Throws<InvalidOperationException>(() => InitialWorld2026.LoadOrCreate(initialPath));
        Assert.Equal(before, Hash(initialPath));
        var untouched = Database("other-initial");
        InitialWorld2026.LoadOrCreate(untouched);
        var savePath = Database("save");
        SqliteWorldFiles.CreateSave(untouched, savePath, SaveId.New());
        var saveHash = Hash(savePath);
        Assert.Throws<InvalidOperationException>(() => InitialWorld2026.LoadOrCreate(savePath));
        Assert.Equal(saveHash, Hash(savePath));
        Assert.Throws<IOException>(() => SqliteWorldFiles.CreateSave(untouched, savePath, SaveId.New()));
        Assert.Equal(saveHash, Hash(savePath));
    }

    /// <summary>Demuestra que cada partida conserva sus perfiles y no comparte estado mutable con otras copias.</summary>
    [Fact]
    public void TwentyClubSavesHaveIndependentStateAndPreserveTheirProfiles()
    {
        var path = Database("initial");
        var initial = InitialWorld2026.LoadOrCreate(path);
        var initialHash = Hash(path);
        var first = SqliteWorldFiles.CreateSave(path, Database("first"), SaveId.New());
        var second = SqliteWorldFiles.CreateSave(path, Database("second"), SaveId.New());
        var secondHash = Hash(Database("second"));
        var club = first.Load().Clubs[0];
        first.Apply(new WorldData(clubs: [new Club(club.Id, "Nombre de prueba", club.City, club.Country, 2)]));
        var updated = new SqliteWorldStore(Database("first")).Load().Clubs[0];
        Assert.Equal("Nombre de prueba", updated.Name);
        Assert.Equal(club.Profile, updated.Profile);
        Assert.Equal(club, second.Load().Clubs[0]);
        Assert.Equal(club, initial.Load().Clubs[0]);
        Assert.Equal(20, first.Load().Registrations.Count);
        Assert.Equal(initialHash, Hash(path));
        Assert.Equal(secondHash, Hash(Database("second")));
        Assert.NotEqual(first.ReadInfo().SaveId, second.ReadInfo().SaveId);
        Assert.Equal(first.ReadInfo().WorldId, second.ReadInfo().WorldId);
        Assert.Equal(initial.Load().Clubs.Select(x => x.Id), first.Load().Clubs.Select(x => x.Id));
    }

    /// <summary>Escribe SQL directo para verificar restricciones reales y no solo validaciones de C#.</summary>
    [Fact]
    public void SqliteEnforcesClubScalesAndProfileIntegrity()
    {
        var path = Database("initial");
        InitialWorld2026.LoadOrCreate(path);
        foreach (var value in new[] { "0", "6", "2.5" })
            Assert.Throws<SqliteException>(() => Sql(path, $"UPDATE clubs SET reputation = {value};"));
        Sql(path, "UPDATE clubs SET reputation = 1;");
        Sql(path, "UPDATE clubs SET reputation = 5;");
        Assert.Throws<SqliteException>(() => Sql(path, "UPDATE clubs SET stadium_capacity = 0;"));
        Assert.Throws<SqliteException>(() => Sql(path, "UPDATE clubs SET foundation_year = 0;"));
        Assert.Throws<SqliteException>(() => Sql(path, "UPDATE clubs SET short_name = NULL;"));
        Assert.Throws<SqliteException>(() => Sql(path, """
            INSERT INTO clubs(id, name, city, country, reputation)
            VALUES ('72026027-0002-4000-8000-000000000001', 'Prueba', 'Ciudad', 'España', 6);
            """));
    }

    /// <summary>Rechaza capacidades que no podrían representar una instalación utilizable.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ClubProfileRequiresPositiveStadiumCapacity(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ClubProfile("Corto", "Región", 1900, "Identidad", "Azul", "Blanco", "Estadio", value, "Primera"));
    }

    /// <summary>Elimina únicamente el directorio temporal creado por esta instancia de prueba.</summary>
    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
