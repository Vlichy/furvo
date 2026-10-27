using System.Security.Cryptography;
using Furvo.Core.Domain;
using Furvo.Core.Worlds;
using Furvo.Persistence;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Furvo.Tests;

/// <summary>Verifica SQLite real, publicación atómica, posiciones y aislamiento de las 480 fichas.</summary>
public sealed class SquadPersistenceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "FurvoSquadTests", Guid.NewGuid().ToString("N"));
    public SquadPersistenceTests() => Directory.CreateDirectory(_directory);
    private string Database(string name) => Path.Combine(_directory, name + ".db");
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static SqliteConnection Open(string path)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = path, ForeignKeys = true, Pooling = false }.ToString());
        connection.Open();
        return connection;
    }

    /// <summary>La reapertura conserva 29.280 valores, posiciones y la procedencia de la publicación.</summary>
    [Fact]
    public void PublicationPersistsAllPlayersContractsAttributesPositionsAndGenerationMetadata()
    {
        var path = Database("v3");
        var world = InitialSquads2026.LoadOrCreate(path);
        var reopened = new SqliteWorldStore(path);
        Assert.Equal(InitialSquads2026.Fingerprint(InitialSquads2026.Generate()), InitialSquads2026.Fingerprint(reopened.Load()));
        Assert.Equal(480, reopened.Load().Players.Count);
        var provenance = InitialSquads2026.ReadGenerationInfo(path)!;
        Assert.Equal("furvo-squads-1", provenance.GeneratorVersion);
        Assert.Equal("20262703", provenance.Seed);
        Assert.Equal(64, provenance.ParameterHash.Length);
        Assert.Equal(64, provenance.CatalogHash.Length);
        Assert.NotEqual(world.ReadInfo().WorldId, provenance.SourceWorldId);
        using var connection = Open(path);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM player_attributes;";
        Assert.Equal(29280L, (long)command.ExecuteScalar()!);
        command.CommandText = "SELECT count(*) FROM player_positions WHERE is_primary = 1;";
        Assert.Equal(480L, (long)command.ExecuteScalar()!);
        command.CommandText = "PRAGMA foreign_key_check;";
        using (var reader = command.ExecuteReader()) Assert.False(reader.Read());
        command.CommandText = "PRAGMA integrity_check;";
        Assert.Equal("ok", command.ExecuteScalar());
    }

    /// <summary>Dos cargas no reescriben el archivo; una plantilla distinta o una partida nunca se convierte a v3.</summary>
    [Fact]
    public void PublicationIsIdempotentAndDoesNotOverwriteV2OrExistingSaves()
    {
        var v2path = Database("v2");
        var v2 = InitialWorld2026.LoadOrCreate(v2path);
        var before = Hash(v2path);
        var v3path = Database("v3");
        var v3 = InitialSquads2026.LoadOrCreate(v3path);
        Assert.Equal(before, Hash(v2path));
        Assert.Equal(v2.Load().Clubs, v3.Load().Clubs);
        Assert.Equal(v2.Load().Competitions, v3.Load().Competitions);
        Assert.Equal(v2.Load().Seasons, v3.Load().Seasons);
        var hash = Hash(v3path);
        Assert.Equal(v3.ReadInfo(), InitialSquads2026.LoadOrCreate(v3path).ReadInfo());
        Assert.Equal(hash, Hash(v3path));
        Assert.Throws<InvalidOperationException>(() => InitialSquads2026.LoadOrCreate(v2path));
        Assert.Equal(before, Hash(v2path));
        var savePath = Database("save");
        SqliteWorldFiles.CreateSave(v3path, savePath, SaveId.New());
        var saveHash = Hash(savePath);
        Assert.Throws<InvalidOperationException>(() => InitialSquads2026.LoadOrCreate(savePath));
        Assert.Equal(saveHash, Hash(savePath));
    }

    /// <summary>Cambiar datos publicados se detecta y no dispara una reparación silenciosa ni sobrescritura.</summary>
    [Fact]
    public void AChangedPublishedAttributeIsRejectedWithoutRewritingIt()
    {
        var path = Database("v3");
        InitialSquads2026.LoadOrCreate(path);
        using (var connection = Open(path))
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "UPDATE player_attributes SET value = CASE WHEN value = 20 THEN 19 ELSE value + 1 END WHERE player_id = (SELECT min(player_id) FROM player_attributes) AND attribute_code = 'technique';";
            command.ExecuteNonQuery();
        }
        var hash = Hash(path);
        Assert.Throws<InvalidOperationException>(() => InitialSquads2026.LoadOrCreate(path));
        Assert.Equal(hash, Hash(path));
    }

    /// <summary>Una edición parcial mantiene ocultos y posiciones y no altera otra partida ni la plantilla.</summary>
    [Fact]
    public void TwoSavesAreIndependentAndPartialAttributeUpdatesKeepHiddenValuesAndPositions()
    {
        var initialPath = Database("initial");
        var initial = InitialSquads2026.LoadOrCreate(initialPath);
        var initialHash = Hash(initialPath);
        var first = SqliteWorldFiles.CreateSave(initialPath, Database("first"), SaveId.New());
        var second = SqliteWorldFiles.CreateSave(initialPath, Database("second"), SaveId.New());
        var secondHash = Hash(Database("second"));
        var player = first.Load().Players.First(x => x.Positions!.Secondary.Count > 0);
        var oldValue = player.Attributes.AllValues["technique"];
        var changed = oldValue == 20 ? 19 : oldValue + 1;
        first.Apply(new WorldData(players: [new Player(player.Id, player.Name, player.BirthDate, player.Nationality, player.MainPosition,
            new PlayerAttributes(new Dictionary<string, int> { ["technique"] = changed }))]));
        var updated = new SqliteWorldStore(Database("first")).Load().Players.Single(x => x.Id == player.Id);
        Assert.Equal(changed, updated.Attributes.AllValues["technique"]);
        Assert.Equal(61, updated.Attributes.AllValues.Count);
        Assert.Equal(player.Positions!.Primary, updated.Positions!.Primary);
        Assert.Equal(player.Positions.Secondary, updated.Positions.Secondary);
        foreach (var definition in PlayerAttributeCatalog.All.Where(x => x.IsHidden))
            Assert.Equal(player.Attributes.AllValues[definition.Code], updated.Attributes.AllValues[definition.Code]);
        Assert.Equal(initialHash, Hash(initialPath));
        Assert.Equal(secondHash, Hash(Database("second")));
        Assert.Equal(oldValue, second.Load().Players.Single(x => x.Id == player.Id).Attributes.AllValues["technique"]);
        Assert.NotEqual(first.ReadInfo().SaveId, second.ReadInfo().SaveId);
        Assert.Equal(InitialSquads2026.ReadGenerationInfo(initialPath), InitialSquads2026.ReadGenerationInfo(Database("first")));
    }

    /// <summary>El número 24 no aparece como restricción del almacén: otra ficha válida puede añadirse a una partida.</summary>
    [Fact]
    public void TheDomainAndDatabaseDoNotImposeAPermanentRosterLimit()
    {
        var initialPath = Database("initial");
        InitialSquads2026.LoadOrCreate(initialPath);
        var save = SqliteWorldFiles.CreateSave(initialPath, Database("save"), SaveId.New());
        var data = save.Load();
        var existing = data.Players[0];
        var player = new Player(PlayerId.New(), "Fixture adicional", existing.BirthDate, existing.Nationality,
            existing.MainPosition, existing.Attributes, existing.Positions);
        var season = data.Seasons.Single();
        var club = data.Contracts.First().ClubId;
        save.Apply(new WorldData(players: [player], contracts:
            [new Contract(ContractId.New(), club, player.Id, season.StartDate, season.EndDate)]));
        Assert.Equal(481, save.Load().Players.Count);
        Assert.Equal(25, save.Load().Contracts.Count(x => x.ClubId == club));
    }

    /// <summary>Los errores de contrato revierten también los valores escritos antes en el mismo lote.</summary>
    [Fact]
    public void AnOverlappingContractRollsBackTheEntirePlayerUpdate()
    {
        var initialPath = Database("initial");
        InitialSquads2026.LoadOrCreate(initialPath);
        var save = SqliteWorldFiles.CreateSave(initialPath, Database("save"), SaveId.New());
        var data = save.Load();
        var player = data.Players[0];
        var contract = data.Contracts.Single(x => x.PlayerId == player.Id);
        var otherClub = data.Clubs.First(x => x.Id != contract.ClubId);
        var before = InitialSquads2026.Fingerprint(data);
        Assert.Throws<SqliteException>(() => save.Apply(new WorldData(players:
            [new Player(player.Id, "No debe persistir", player.BirthDate, player.Nationality, player.MainPosition)],
            contracts: [new Contract(ContractId.New(), otherClub.Id, player.Id, contract.StartDate, contract.EndDate)])));
        Assert.Equal(before, InitialSquads2026.Fingerprint(save.Load()));
    }

    /// <summary>La tabla detallada impide códigos inexistentes, dos principales y portería incoherente incluso con SQL directo.</summary>
    [Fact]
    public void SqliteEnforcesDetailedPositionIntegrityAndKeepsMainGroupConsistent()
    {
        var path = Database("v3");
        var world = InitialSquads2026.LoadOrCreate(path);
        var player = world.Load().Players.First(x => x.Positions!.Primary == PlayerRole.CentreBack);
        using var connection = Open(path);
        using var command = connection.CreateCommand();
        foreach (var sql in new[]
        {
            $"INSERT INTO player_positions VALUES ('{PlayerId.New()}', 2, 1);",
            $"INSERT INTO player_positions VALUES ('{player.Id}', 99, 0);",
            $"INSERT INTO player_positions VALUES ('{player.Id}', 3, 1);",
            $"INSERT INTO player_positions VALUES ('{player.Id}', 1, 0);",
            $"UPDATE players SET main_position = 4 WHERE id = '{player.Id}';"
        })
        {
            command.CommandText = sql;
            Assert.Throws<SqliteException>(() => command.ExecuteNonQuery());
        }
    }

    /// <summary>Reemplazar posiciones con otro grupo es atómico y no obliga a perder atributos ni crear un jugador distinto.</summary>
    [Fact]
    public void PositionReplacementIsAtomicAndCannotLeaveSecondaryPositionsWithoutAPrimary()
    {
        var initialPath = Database("initial");
        InitialSquads2026.LoadOrCreate(initialPath);
        var savePath = Database("save");
        var save = SqliteWorldFiles.CreateSave(initialPath, savePath, SaveId.New());
        var player = save.Load().Players.First(x => x.Positions!.Primary == PlayerRole.CentreBack && x.Positions.Secondary.Count > 0);
        using (var connection = Open(savePath))
        using (var command = connection.CreateCommand())
        {
            command.CommandText = $"DELETE FROM player_positions WHERE player_id = '{player.Id}' AND is_primary = 1;";
            Assert.Throws<SqliteException>(() => command.ExecuteNonQuery());
            command.CommandText = $"UPDATE player_positions SET is_primary = 0 WHERE player_id = '{player.Id}' AND is_primary = 1;";
            Assert.Throws<SqliteException>(() => command.ExecuteNonQuery());
        }
        save.Apply(new WorldData(players: [new Player(player.Id, player.Name, player.BirthDate, player.Nationality,
            PlayerPosition.Forward, positions: new PlayerPositions(PlayerRole.Striker, [PlayerRole.SecondStriker]))]));
        var changed = save.Load().Players.Single(x => x.Id == player.Id);
        Assert.Equal(PlayerPosition.Forward, changed.MainPosition);
        Assert.Equal(PlayerRole.Striker, changed.Positions!.Primary);
        Assert.Equal(new[] { PlayerRole.SecondStriker }, changed.Positions.Secondary);
        Assert.Equal(61, changed.Attributes.AllValues.Count);
        Assert.Equal(player.Attributes.AllValues["professionalism"], changed.Attributes.AllValues["professionalism"]);
        // Una ficha histórica sin desglose puede seguir existiendo; una secundaria aislada no es un desglose válido.
        var historical = new Player(PlayerId.New(), "Fixture sin desglose", player.BirthDate, player.Nationality, PlayerPosition.Defender);
        save.Apply(new WorldData(players: [historical]));
        using var check = Open(savePath);
        using var invalid = check.CreateCommand();
        invalid.CommandText = $"INSERT INTO player_positions VALUES ('{historical.Id}', 5, 0);";
        Assert.Throws<SqliteException>(() => invalid.ExecuteNonQuery());
    }

    /// <summary>Migrar el esquema 3 añade tablas opcionales sin inventar detalles ni alterar el archivo fuente.</summary>
    [Fact]
    public void SchemaThreeMigratesToACopyWithoutInventingDetailedPositions()
    {
        var source = Database("schema3");
        using (var connection = Open(source))
        using (var command = connection.CreateCommand())
        {
            foreach (var script in new[] { "001_Initial.sql", "002_World.sql", "003_ClubUniverseAndScales.sql" })
            {
                using var stream = typeof(SqliteSaveStore).Assembly.GetManifestResourceStream("Furvo.Persistence.Migrations." + script)!;
                using var reader = new StreamReader(stream);
                command.CommandText = reader.ReadToEnd();
                command.ExecuteNonQuery();
            }
            command.CommandText = """
                INSERT INTO save_metadata VALUES (1, '72026027-0009-4000-8000-000000000001');
                INSERT INTO world_info VALUES (1, 'save', '72026027-0009-4000-8000-000000000002');
                INSERT INTO players(id, name, birth_date, nationality, main_position)
                VALUES ('72026027-0009-4000-8000-000000000003', 'Fixture histórico', '2000-01-01', 'España', 2);
                PRAGMA user_version = 3;
                """;
            command.ExecuteNonQuery();
        }
        var before = Hash(source);
        var migrated = SqliteWorldFiles.MigrateCopy(source, Database("migrated"));
        var player = Assert.Single(migrated.Load().Players);
        Assert.Equal(PlayerPosition.Defender, player.MainPosition);
        Assert.Null(player.Positions);
        Assert.Empty(player.Attributes.AllValues);
        Assert.Null(InitialSquads2026.ReadGenerationInfo(Database("migrated")));
        Assert.Equal(4, new SqliteSaveStore(Database("migrated")).ReadMetadata()!.SchemaVersion);
        Assert.Equal(3, new SqliteSaveStore(source).ReadMetadata()!.SchemaVersion);
        Assert.Equal(before, Hash(source));
    }

    /// <summary>La carpeta pertenece solo a esta prueba; no contiene publicaciones ni partidas del usuario.</summary>
    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
