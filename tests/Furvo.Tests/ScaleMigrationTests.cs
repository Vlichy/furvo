using System.Security.Cryptography;
using Furvo.Core.Domain;
using Furvo.Core.Worlds;
using Furvo.Persistence;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Furvo.Tests;

/// <summary>Pruebas con SQLite real y directorio temporal exclusivo; no abren ni modifican partidas del usuario.</summary>
public sealed class ScaleMigrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "FurvoScaleTests", Guid.NewGuid().ToString("N"));
    private readonly ClubId _club = ClubId.New();
    private readonly PlayerId _player = PlayerId.New();
    private readonly SaveId _save = SaveId.New();
    private readonly WorldId _world = WorldId.New();
    /// <summary>Prepara un directorio propio para aislar esta prueba de ejecuciones paralelas.</summary>
    public ScaleMigrationTests() => Directory.CreateDirectory(_directory);
    private string Database(string name) => Path.Combine(_directory, name + ".db");
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    /// <summary>Construye el formato anterior ejecutando sus SQL originales; evita falsear una migración con el esquema nuevo.</summary>
    private void CreateSchemaTwo(string path, int reputation, int upperAttribute, bool initial = false)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = path, ForeignKeys = true, Pooling = false }.ToString());
        connection.Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        foreach (var resource in new[] { "001_Initial.sql", "002_World.sql" })
        {
            using var stream = typeof(SqliteSaveStore).Assembly.GetManifestResourceStream("Furvo.Persistence.Migrations." + resource)!;
            using var reader = new StreamReader(stream);
            command.CommandText = reader.ReadToEnd();
            command.ExecuteNonQuery();
        }
        command.CommandText = """
            INSERT INTO world_info VALUES (1, $kind, $world);
            INSERT INTO clubs(id, name, city, country, reputation) VALUES ($club, 'Club histórico', 'Ciudad', 'España', $rep);
            INSERT INTO players(id, name, birth_date, nationality, main_position)
                VALUES ($player, 'Jugador de prueba', '2000-01-01', 'España', 1);
            INSERT INTO contracts VALUES ($contract, $club, $player, '2026-07-01', '2027-06-30');
            PRAGMA user_version = 2;
            """;
        command.Parameters.AddWithValue("$kind", initial ? "initial" : "save");
        command.Parameters.AddWithValue("$world", _world.ToString());
        command.Parameters.AddWithValue("$club", _club.ToString());
        command.Parameters.AddWithValue("$player", _player.ToString());
        command.Parameters.AddWithValue("$contract", ContractId.New().ToString());
        command.Parameters.AddWithValue("$rep", reputation);
        command.ExecuteNonQuery();
        command.Parameters.Clear();
        if (!initial)
        {
            command.CommandText = "INSERT INTO save_metadata VALUES (1, $save);";
            command.Parameters.AddWithValue("$save", _save.ToString());
            command.ExecuteNonQuery();
            command.Parameters.Clear();
        }
        foreach (var definition in PlayerAttributeCatalog.All)
        {
            command.CommandText = "INSERT INTO player_attributes VALUES ($player, $code, $value);";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("$player", _player.ToString());
            command.Parameters.AddWithValue("$code", definition.Code);
            command.Parameters.AddWithValue("$value", definition.Code == "technique" ? upperAttribute : 1);
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    /// <summary>Verifica que una migración no modifica puntuaciones válidas ni rompe sus referencias.</summary>
    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 20)]
    public void MigrationPreservesValidScaleValuesIdsAndReferences(int reputation, int attribute)
    {
        var source = Database("old");
        CreateSchemaTwo(source, reputation, attribute);
        var before = Hash(source);
        Assert.Equal(2, new SqliteSaveStore(source).ReadMetadata()!.SchemaVersion);
        Assert.Equal(2, new SqliteSaveStore(source).Initialize(_save).SchemaVersion);
        Assert.Throws<NotSupportedException>(() => new SqliteWorldStore(source).Load());
        var migrated = SqliteWorldFiles.MigrateCopy(source, Database("new"));
        Assert.Equal(before, Hash(source));
        Assert.Equal(2, new SqliteSaveStore(source).ReadMetadata()!.SchemaVersion);
        Assert.Equal(SqliteSaveStore.CurrentSchemaVersion, new SqliteSaveStore(Database("new")).ReadMetadata()!.SchemaVersion);
        Assert.Equal(_save, migrated.ReadInfo().SaveId);
        Assert.Equal(_world, migrated.ReadInfo().WorldId);
        var data = migrated.Load();
        Assert.Equal(reputation, Assert.Single(data.Clubs).Reputation);
        Assert.Null(data.Clubs[0].Profile);
        var player = Assert.Single(data.Players);
        Assert.Equal(_player, player.Id);
        Assert.Equal(61, player.Attributes.AllValues.Count);
        Assert.Equal(attribute, player.Attributes.AllValues["technique"]);
        Assert.Equal(51, player.Attributes.VisibleValues.Count);
        Assert.Equal(_club, Assert.Single(data.Contracts).ClubId);
        Assert.Equal(_player, data.Contracts[0].PlayerId);
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = Database("new"), Pooling = false }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_key_check;";
        using var reader = command.ExecuteReader();
        Assert.False(reader.Read());
    }

    /// <summary>Exige conflictos detallados, original intacto y ausencia de un destino parcialmente migrado.</summary>
    [Theory]
    [InlineData(6, 20, 1)]
    [InlineData(5, 21, 1)]
    [InlineData(100, 100, 2)]
    public void IncompatibleOldValuesAreReportedWithoutConvertingOrDamagingAnything(int reputation, int attribute, int issues)
    {
        var source = Database("old");
        CreateSchemaTwo(source, reputation, attribute);
        var before = Hash(source);
        var error = Assert.Throws<SchemaCompatibilityException>(() => SqliteWorldFiles.MigrateCopy(source, Database("failed")));
        Assert.Equal(issues, error.Values.Count);
        if (reputation > 5) Assert.Contains(error.Values, x => x.EntityId == _club.ToString()
            && x.StoredValue == reputation.ToString() && x.AllowedRange == "1..5");
        if (attribute > 20) Assert.Contains(error.Values, x => x.EntityId == _player.ToString()
            && x.Field == "Player.technique" && x.StoredValue == attribute.ToString() && x.AllowedRange == "1..20");
        Assert.Equal(before, Hash(source));
        Assert.False(File.Exists(Database("failed")));
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
        Assert.Equal(2, new SqliteSaveStore(source).ReadMetadata()!.SchemaVersion);
    }

    /// <summary>Comprueba que el rechazo de destino ocurre sin dañar ni siquiera una publicación válida.</summary>
    [Fact]
    public void IncompatibleMigrationNeverOverwritesAnExistingDestination()
    {
        var source = Database("old");
        CreateSchemaTwo(source, 50, 100);
        var target = Database("published");
        InitialWorld2026.LoadOrCreate(target);
        var sourceHash = Hash(source);
        var targetHash = Hash(target);
        Assert.Throws<IOException>(() => SqliteWorldFiles.MigrateCopy(source, target));
        Assert.Equal(sourceHash, Hash(source));
        Assert.Equal(targetHash, Hash(target));
    }

    /// <summary>Conserva el rol de plantilla y su inmutabilidad después de migrar una copia.</summary>
    [Fact]
    public void AnInitialWorldCanAlsoBeMigratedOnlyToANewFile()
    {
        var source = Database("old-initial");
        CreateSchemaTwo(source, 3, 20, initial: true);
        var before = Hash(source);
        var migrated = SqliteWorldFiles.MigrateCopy(source, Database("new-initial"));
        Assert.Equal(WorldFileKind.InitialWorld, migrated.ReadInfo().Kind);
        Assert.Null(migrated.ReadInfo().SaveId);
        Assert.Equal(_world, migrated.ReadInfo().WorldId);
        Assert.Equal(before, Hash(source));
        Assert.Throws<InvalidOperationException>(() => migrated.Apply(new WorldData()));
    }

    /// <summary>Elimina únicamente el directorio temporal creado por esta instancia de prueba.</summary>
    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
