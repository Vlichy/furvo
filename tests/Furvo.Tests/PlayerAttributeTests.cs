using Furvo.Core.Domain;
using Furvo.Core.Worlds;
using Furvo.Persistence;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Furvo.Tests;

/// <summary>Pruebas con SQLite real y directorio temporal exclusivo; no abren ni modifican partidas del usuario.</summary>
public sealed class PlayerAttributeTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "FurvoAttributeTests", Guid.NewGuid().ToString("N"));
    /// <summary>Prepara un directorio propio para aislar esta prueba de ejecuciones paralelas.</summary>
    public PlayerAttributeTests() => Directory.CreateDirectory(_directory);
    private string Database => Path.Combine(_directory, "attributes.db");

    /// <summary>Fija los 61 códigos y sus categorías para detectar simplificaciones accidentales.</summary>
    [Fact]
    public void CatalogPreservesAllApprovedGroupsAndHiddenFlags()
    {
        Assert.Equal(61, PlayerAttributeCatalog.All.Count);
        Assert.Equal(61, PlayerAttributeCatalog.All.Select(x => x.Code).Distinct().Count());
        Assert.Equal(17, PlayerAttributeCatalog.All.Count(x => x.Category == AttributeCategory.Technical));
        Assert.Equal(16, PlayerAttributeCatalog.All.Count(x => x.Category == AttributeCategory.Mental));
        Assert.Equal(8, PlayerAttributeCatalog.All.Count(x => x.Category == AttributeCategory.Physical));
        Assert.Equal(10, PlayerAttributeCatalog.All.Count(x => x.Category == AttributeCategory.Goalkeeper));
        Assert.Equal(10, PlayerAttributeCatalog.All.Count(x => x.IsHidden));
        Assert.Empty(new PlayerAttributes([]).AllValues);
    }

    /// <summary>Comprueba el rango en todos los atributos, no solo en una muestra visible.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    public void AllAttributesRejectOutOfRangeValues(int value)
    {
        foreach (var definition in PlayerAttributeCatalog.All)
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PlayerAttributes(new Dictionary<string, int> { [definition.Code] = value }));
    }

    /// <summary>Protege la identidad del catálogo y la inmutabilidad frente al diccionario recibido.</summary>
    [Fact]
    public void UnknownAndDuplicateAttributesAreRejectedAndInputsAreCopied()
    {
        Assert.Throws<ArgumentException>(() => new PlayerAttributes(new Dictionary<string, int> { ["unknown"] = 10 }));
        Assert.Throws<ArgumentException>(() => new PlayerAttributes(
            [new("technique", 10), new("technique", 14)]));
        var original = new Dictionary<string, int> { ["technique"] = 1, ["professionalism"] = 20 };
        var attributes = new PlayerAttributes(original);
        original["technique"] = 19;
        Assert.Equal(1, attributes.AllValues["technique"]);
        Assert.Equal(20, attributes.AllValues["professionalism"]);
        Assert.Single(attributes.VisibleValues);
        Assert.False(attributes.VisibleValues.ContainsKey("professionalism"));
        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<string, int>)attributes.AllValues).Add("pace", 10));
    }

    /// <summary>Introduce únicamente fixtures temporales de límites; no forma parte del generador del mundo comercial.</summary>
    private SqliteWorldStore Populate()
    {
        new SqliteSaveStore(Database).Initialize(SaveId.New());
        var store = new SqliteWorldStore(Database);
        // Solo datos sintéticos de prueba; ningún generador ni plantilla de producción.
        var values = PlayerAttributeCatalog.All.Select((x, i) =>
            new KeyValuePair<string, int>(x.Code, i % 2 == 0 ? 1 : 20));
        var player = new Player(PlayerId.New(), "Jugador de prueba", new DateOnly(2000, 1, 1), "País",
            PlayerPosition.Goalkeeper, new PlayerAttributes(values));
        store.Apply(new WorldData(players: [player]));
        return store;
    }

    /// <summary>Garantiza guardado individual y evita perder valores ocultos al actualizar solo un atributo.</summary>
    [Fact]
    public void EveryIndividualAttributeSurvivesReopeningAndUpdatesDoNotDiscardHiddenValues()
    {
        var store = Populate();
        var original = Assert.Single(store.Load().Players);
        var reopened = new SqliteWorldStore(Database);
        var player = Assert.Single(reopened.Load().Players);
        Assert.Equal(original.Id, player.Id);
        Assert.Equal(61, player.Attributes.AllValues.Count);
        foreach (var (code, value) in original.Attributes.AllValues) Assert.Equal(value, player.Attributes.AllValues[code]);
        Assert.Equal(51, player.Attributes.VisibleValues.Count);
        Assert.DoesNotContain(player.Attributes.VisibleValues.Keys, code => PlayerAttributeCatalog.Get(code).IsHidden);
        reopened.Apply(new WorldData(players:
            [new Player(player.Id, player.Name, player.BirthDate, player.Nationality, player.MainPosition,
                new PlayerAttributes(new Dictionary<string, int> { ["technique"] = 12 }))]));
        var updated = Assert.Single(reopened.Load().Players);
        Assert.Equal(12, updated.Attributes.AllValues["technique"]);
        Assert.Equal(61, updated.Attributes.AllValues.Count);
        Assert.Equal(original.Attributes.AllValues["loyalty"], updated.Attributes.AllValues["loyalty"]);
    }

    /// <summary>Comprueba directamente rango entero, referencias, clave compuesta y coherencia del catálogo SQL.</summary>
    [Fact]
    public void SqliteEnforcesAttributeRangeForeignKeysAndUniqueness()
    {
        var store = Populate();
        var player = Assert.Single(store.Load().Players);
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = Database, ForeignKeys = true, Pooling = false }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        foreach (var value in new[] { "0", "21", "1.5" })
        {
            command.CommandText = $"UPDATE player_attributes SET value = {value} WHERE attribute_code = 'technique';";
            Assert.Throws<SqliteException>(() => command.ExecuteNonQuery());
        }
        foreach (var sql in new[]
        {
            $"INSERT INTO player_attributes VALUES ('{player.Id}', 'unknown', 10);",
            $"INSERT INTO player_attributes VALUES ('{PlayerId.New()}', 'technique', 10);",
            $"INSERT INTO player_attributes VALUES ('{player.Id}', 'technique', 10);"
        })
        {
            command.CommandText = sql;
            Assert.Throws<SqliteException>(() => command.ExecuteNonQuery());
        }
        command.CommandText = "SELECT count(*) FROM attribute_definitions;";
        Assert.Equal(61L, (long)command.ExecuteScalar()!);
        command.CommandText = "SELECT category FROM attribute_definitions WHERE code = 'loyalty';";
        Assert.Equal((long)AttributeCategory.Hidden, (long)command.ExecuteScalar()!);
        command.CommandText = "SELECT code, name, category FROM attribute_definitions;";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var definition = PlayerAttributeCatalog.Get(reader.GetString(0));
            Assert.Equal(definition.Name, reader.GetString(1));
            Assert.Equal((int)definition.Category, reader.GetInt32(2));
        }
    }

    /// <summary>Verifica aislamiento también para información oculta, no solo para nombres de clubes.</summary>
    [Fact]
    public void AttributesRemainIndependentAcrossInitialWorldAndTwoSaves()
    {
        var source = Populate();
        var data = source.Load();
        var initialPath = Path.Combine(_directory, "initial.db");
        var initial = SqliteWorldFiles.CreateInitialWorld(initialPath, WorldId.New(), data);
        var first = SqliteWorldFiles.CreateSave(initialPath, Path.Combine(_directory, "first.db"), SaveId.New());
        var second = SqliteWorldFiles.CreateSave(initialPath, Path.Combine(_directory, "second.db"), SaveId.New());
        var player = Assert.Single(data.Players);
        first.Apply(new WorldData(players:
            [new Player(player.Id, player.Name, player.BirthDate, player.Nationality, player.MainPosition,
                new PlayerAttributes(new Dictionary<string, int> { ["professionalism"] = 15 }))]));
        Assert.Equal(15, Assert.Single(first.Load().Players).Attributes.AllValues["professionalism"]);
        Assert.Equal(player.Attributes.AllValues["professionalism"],
            Assert.Single(second.Load().Players).Attributes.AllValues["professionalism"]);
        Assert.Equal(player.Attributes.AllValues["professionalism"],
            Assert.Single(initial.Load().Players).Attributes.AllValues["professionalism"]);
    }

    /// <summary>Elimina únicamente el directorio temporal creado por esta instancia de prueba.</summary>
    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
