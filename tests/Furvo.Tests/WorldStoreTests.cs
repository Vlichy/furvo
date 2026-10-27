using System.Security.Cryptography;
using Furvo.Core.Domain;
using Furvo.Core.Worlds;
using Furvo.Persistence;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Furvo.Tests;

/// <summary>Pruebas con SQLite real y directorio temporal exclusivo; no abren ni modifican partidas del usuario.</summary>
public sealed class WorldStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "FurvoWorldTests", Guid.NewGuid().ToString("N"));
    private static readonly DateOnly Start = new(2026, 7, 1);
    private static readonly DateOnly End = new(2027, 6, 30);

    /// <summary>Prepara un directorio propio para aislar esta prueba de ejecuciones paralelas.</summary>
    public WorldStoreTests() => Directory.CreateDirectory(_directory);
    private string Database(string name) => Path.Combine(_directory, name + ".db");
    private static Club MakeClub(string name) => new(ClubId.New(), name, "Ciudad de prueba", "País de prueba", 3);
    private static Player MakePlayer(PlayerId? id = null, PlayerAttributes? attributes = null) =>
        new(id ?? PlayerId.New(), "Jugador de prueba", new DateOnly(2000, 1, 2), "País de prueba",
            PlayerPosition.Midfielder, attributes);

    private sealed class Fixture
    {
        public Club Club { get; } = MakeClub("Club de prueba A");
        public Club OtherClub { get; } = MakeClub("Club de prueba B");
        public Player Player { get; } = MakePlayer();
        public Competition Competition { get; } = new(CompetitionId.New(), "Competición de prueba", "País de prueba");
        public Season Season { get; } = new(SeasonId.New(), Start, End);
        public Coach Coach => new(CoachId, "Entrenador de prueba", Club.Id);
        public CoachId CoachId { get; } = CoachId.New();
        public Contract Contract => new(ContractId, Club.Id, Player.Id, Start, End);
        public ContractId ContractId { get; } = ContractId.New();
        public Registration Registration => new(Club.Id, Competition.Id, Season.Id);
        public WorldData Data => new(clubs: [Club, OtherClub], players: [Player], contracts: [Contract],
            coaches: [Coach], competitions: [Competition], seasons: [Season], registrations: [Registration]);
    }

    private SqliteWorldStore EmptySave(string name)
    {
        var path = Database(name);
        new SqliteSaveStore(path).Initialize(SaveId.New());
        return new SqliteWorldStore(path);
    }

    private static SqliteConnection Connection(string path, bool foreignKeys = true)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            { DataSource = path, Mode = SqliteOpenMode.ReadWrite, ForeignKeys = foreignKeys, Pooling = false }.ToString());
        connection.Open();
        return connection;
    }

    private static void Execute(string path, string sql)
    {
        using var connection = Connection(path);
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    /// <summary>Recupera todas las relaciones del modelo después de liberar y reabrir SQLite.</summary>
    [Fact]
    public void AllEntitiesSurviveClosingAndReopening()
    {
        var f = new Fixture();
        var store = EmptySave("entities");
        store.Apply(f.Data);
        var reopened = new SqliteWorldStore(Database("entities"));
        var data = reopened.Load();
        Assert.Equal(f.Club, data.Clubs.Single(x => x.Id == f.Club.Id));
        var player = Assert.Single(data.Players);
        Assert.Equal(f.Player.Id, player.Id);
        Assert.Equal(f.Player.Name, player.Name);
        Assert.Equal(f.Player.BirthDate, player.BirthDate);
        Assert.Equal(f.Player.Nationality, player.Nationality);
        Assert.Equal(f.Player.MainPosition, player.MainPosition);
        Assert.Equal(f.Contract, Assert.Single(data.Contracts));
        Assert.Equal(f.Coach, Assert.Single(data.Coaches));
        Assert.Equal(f.Competition, Assert.Single(data.Competitions));
        Assert.Equal(f.Season, Assert.Single(data.Seasons));
        Assert.Equal(f.Registration, Assert.Single(data.Registrations));
        Assert.Equal(store.ReadInfo(), reopened.ReadInfo());
        // Una inscripción repetida es idempotente, nunca crea otra fila.
        reopened.Apply(new WorldData(registrations: [f.Registration]));
        Assert.Single(reopened.Load().Registrations);
    }

    /// <summary>Compara datos y hashes para demostrar aislamiento de la plantilla y ambas partidas.</summary>
    [Fact]
    public void TwoSavesAreIndependentFromEachOtherAndTheInitialWorld()
    {
        var f = new Fixture();
        var seedPath = Database("initial");
        var worldId = WorldId.New();
        var initial = SqliteWorldFiles.CreateInitialWorld(seedPath, worldId, f.Data);
        var initialHash = Hash(seedPath);
        var firstId = SaveId.New();
        var secondId = SaveId.New();
        var first = SqliteWorldFiles.CreateSave(seedPath, Database("first"), firstId);
        var second = SqliteWorldFiles.CreateSave(seedPath, Database("second"), secondId);
        var secondHash = Hash(Database("second"));
        var changedClub = new Club(f.Club.Id, "Nombre cambiado", f.Club.City, f.Club.Country, 4);
        first.Apply(new WorldData(clubs: [changedClub]));
        Assert.Equal(changedClub, first.Load().Clubs.Single(x => x.Id == f.Club.Id));
        Assert.Equal(f.Club, second.Load().Clubs.Single(x => x.Id == f.Club.Id));
        Assert.Equal(f.Club, initial.Load().Clubs.Single(x => x.Id == f.Club.Id));
        Assert.Equal(initialHash, Hash(seedPath));
        Assert.Equal(secondHash, Hash(Database("second")));
        Assert.Equal(firstId, first.ReadInfo().SaveId);
        Assert.Equal(secondId, second.ReadInfo().SaveId);
        Assert.Equal(worldId, first.ReadInfo().WorldId);
        Assert.Equal(worldId, second.ReadInfo().WorldId);
        Assert.Equal(f.Player.Id, Assert.Single(first.Load().Players).Id);
        Assert.Throws<InvalidOperationException>(() => initial.Apply(new WorldData(clubs: [changedClub])));
        Assert.Throws<InvalidOperationException>(() =>
            SqliteWorldFiles.CreateSave(Database("first"), Database("from-save"), SaveId.New()));
        Assert.False(File.Exists(Database("from-save")));
        Assert.Equal(initialHash, Hash(seedPath));
    }

    /// <summary>Mantiene la API genérica sin generación implícita de datos.</summary>
    [Fact]
    public void InitialWorldCanBeEmptyAndDoesNotGeneratePlayersOrClubs()
    {
        var world = SqliteWorldFiles.CreateInitialWorld(Database("empty-world"), WorldId.New(), new WorldData());
        Assert.Empty(world.Load().Clubs);
        Assert.Empty(world.Load().Players);
        Assert.Empty(world.Load().Contracts);
        Assert.Equal(WorldFileKind.InitialWorld, world.ReadInfo().Kind);
        Assert.Null(world.ReadInfo().SaveId);
    }

    /// <summary>Ensaya distintas formas de solapamiento para el mismo jugador.</summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    [InlineData(100, 200)]
    public void OverlappingContractsForOnePlayerAreRejected(int startOffset, int endOffset)
    {
        var f = new Fixture();
        var store = EmptySave("overlap");
        store.Apply(f.Data);
        var candidate = new Contract(ContractId.New(), f.OtherClub.Id, f.Player.Id,
            Start.AddDays(startOffset), End.AddDays(endOffset));
        Assert.Throws<SqliteException>(() => store.Apply(new WorldData(contracts: [candidate])));
        Assert.Equal(f.Contract, Assert.Single(store.Load().Contracts));
    }

    /// <summary>Fija la regla de fechas inclusivas: compartir el día final sigue siendo incompatible.</summary>
    [Fact]
    public void SameClubOverlapAndSharedEndDayAreRejectedButNextDayIsAllowed()
    {
        var f = new Fixture();
        var store = EmptySave("boundaries");
        store.Apply(f.Data);
        Assert.Throws<SqliteException>(() => store.Apply(new WorldData(contracts:
            [new Contract(ContractId.New(), f.Club.Id, f.Player.Id, End, End.AddDays(1))])));
        store.Apply(new WorldData(contracts:
            [new Contract(ContractId.New(), f.OtherClub.Id, f.Player.Id, End.AddDays(1), End.AddDays(20))]));
        Assert.Equal(2, store.Load().Contracts.Count);
    }

    /// <summary>Evita aplicar por error la exclusividad de contratos a todos los jugadores.</summary>
    [Fact]
    public void DifferentPlayersCanHaveContractsAtTheSameTime()
    {
        var f = new Fixture();
        var store = EmptySave("different-players");
        store.Apply(f.Data);
        var other = MakePlayer();
        var contract = new Contract(ContractId.New(), f.Club.Id, other.Id, Start, End);
        store.Apply(new WorldData(players: [other], contracts: [contract]));
        Assert.Equal(2, store.Load().Contracts.Count);
        Assert.Contains(contract, store.Load().Contracts);
    }

    /// <summary>Comprueba UPDATE y rollback de cambios anteriores del mismo lote.</summary>
    [Fact]
    public void ContractUpdatesAlsoRejectOverlapAndRollbackTheWholeBatch()
    {
        var f = new Fixture();
        var store = EmptySave("updates");
        store.Apply(f.Data);
        var later = new Contract(ContractId.New(), f.OtherClub.Id, f.Player.Id, End.AddDays(1), End.AddDays(20));
        store.Apply(new WorldData(contracts: [later]));
        var changedClub = new Club(f.Club.Id, "No debe persistir", f.Club.City, f.Club.Country, 5);
        Assert.Throws<SqliteException>(() => store.Apply(new WorldData(clubs: [changedClub], contracts:
            [new Contract(later.Id, f.OtherClub.Id, f.Player.Id, End, End.AddDays(20))])));
        Assert.Equal(f.Club, store.Load().Clubs.Single(x => x.Id == f.Club.Id));
        Assert.Contains(later, store.Load().Contracts);
        Execute(Database("updates"), $"UPDATE contracts SET end_date = '2027-06-29' WHERE id = '{f.Contract.Id}';");
        Assert.Throws<SqliteException>(() => Execute(Database("updates"),
            $"UPDATE contracts SET end_date = '2027-07-02' WHERE id = '{f.Contract.Id}';"));
    }

    /// <summary>Evita huérfanos y duplicados incluso ante operaciones SQL externas.</summary>
    [Fact]
    public void ForeignKeysRejectMissingContractCoachAndRegistrationReferences()
    {
        var f = new Fixture();
        var store = EmptySave("references");
        store.Apply(f.Data);
        Assert.Throws<SqliteException>(() => store.Apply(new WorldData(contracts:
            [new Contract(ContractId.New(), ClubId.New(), f.Player.Id, End.AddDays(1), End.AddDays(2))])));
        Assert.Throws<SqliteException>(() => store.Apply(new WorldData(contracts:
            [new Contract(ContractId.New(), f.Club.Id, PlayerId.New(), Start, End)])));
        Assert.Throws<SqliteException>(() => store.Apply(new WorldData(coaches:
            [new Coach(CoachId.New(), "Entrenador", ClubId.New())])));
        foreach (var registration in new[]
        {
            new Registration(ClubId.New(), f.Competition.Id, f.Season.Id),
            new Registration(f.Club.Id, CompetitionId.New(), f.Season.Id),
            new Registration(f.Club.Id, f.Competition.Id, SeasonId.New())
        })
            Assert.Throws<SqliteException>(() => store.Apply(new WorldData(registrations: [registration])));
        Assert.Throws<SqliteException>(() => Execute(Database("references"), $"DELETE FROM clubs WHERE id = '{f.Club.Id}';"));
        Assert.Throws<SqliteException>(() => Execute(Database("references"),
            $"INSERT INTO registrations VALUES ('{f.Club.Id}', '{f.Competition.Id}', '{f.Season.Id}');"));
    }

    /// <summary>Distingue desconocidos históricos de nuevas escrituras inválidas.</summary>
    [Fact]
    public void DatesIdsAndIncompleteNewEntitiesAreValidated()
    {
        var f = new Fixture();
        Assert.Throws<ArgumentException>(() => new Contract(ContractId.New(), f.Club.Id, f.Player.Id, End, Start));
        Assert.Throws<ArgumentException>(() => new Contract(ContractId.New(), f.Club.Id, f.Player.Id, Start, null));
        Assert.Throws<ArgumentException>(() => new Season(SeasonId.New(), End, Start));
        Assert.Throws<ArgumentException>(() => new Coach(default, "Entrenador", f.Club.Id));
        Assert.Throws<ArgumentException>(() => new Registration(f.Club.Id, default, f.Season.Id));
        Assert.Throws<ArgumentException>(() => new Club(default, "Club"));
        Assert.Throws<ArgumentException>(() => new Player(PlayerId.New(), " "));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Player(PlayerId.New(), "Jugador", mainPosition: (PlayerPosition)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Club(ClubId.New(), "Club", reputation: 6));
        var store = EmptySave("dates");
        Assert.Throws<ArgumentException>(() => store.Apply(new WorldData(clubs: [new Club(ClubId.New(), "Sin datos")])));
        store.Apply(f.Data);
        Assert.Throws<SqliteException>(() => Execute(Database("dates"),
            $"UPDATE contracts SET start_date = '2026-02-30' WHERE id = '{f.Contract.Id}';"));
        Assert.Throws<SqliteException>(() => Execute(Database("dates"),
            $"UPDATE contracts SET end_date = '2025-01-01' WHERE id = '{f.Contract.Id}';"));
        Assert.Throws<SqliteException>(() => Execute(Database("dates"),
            $"UPDATE clubs SET reputation = 6 WHERE id = '{f.Club.Id}';"));
    }

    /// <summary>Comprueba publicación atómica y limpieza del temporal ante errores.</summary>
    [Fact]
    public void CreationNeverOverwritesAndFailuresDoNotPublishPartialWorlds()
    {
        var f = new Fixture();
        var path = Database("existing");
        SqliteWorldFiles.CreateInitialWorld(path, WorldId.New(), f.Data);
        var before = Hash(path);
        Assert.Throws<IOException>(() => SqliteWorldFiles.CreateInitialWorld(path, WorldId.New(), new WorldData()));
        Assert.Throws<IOException>(() => SqliteWorldFiles.CreateSave(path, path, SaveId.New()));
        Assert.Equal(before, Hash(path));
        var invalid = new WorldData(clubs: [f.Club],
            contracts: [new Contract(ContractId.New(), f.Club.Id, f.Player.Id, Start, End)]);
        Assert.Throws<SqliteException>(() => SqliteWorldFiles.CreateInitialWorld(Database("failed"), WorldId.New(), invalid));
        Assert.False(File.Exists(Database("failed")));
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
        Assert.Throws<InvalidOperationException>(() => new SqliteSaveStore(path).Initialize(SaveId.New()));
        Assert.Equal(before, Hash(path));
    }

    /// <summary>Crea datos de esquema 1; permite referencias corruptas solo para comprobar rechazo y preservación del archivo.</summary>
    private void CreateLegacy(string path, Fixture f, SaveId saveId, bool orphan = false)
    {
        using (File.Create(path)) { }
        using var connection = Connection(path, foreignKeys: false);
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE save_metadata(singleton INTEGER PRIMARY KEY CHECK(singleton = 1), save_id TEXT NOT NULL UNIQUE);
            CREATE TABLE clubs(id TEXT PRIMARY KEY NOT NULL, name TEXT NOT NULL);
            CREATE TABLE players(id TEXT PRIMARY KEY NOT NULL, name TEXT NOT NULL);
            CREATE TABLE contracts(id TEXT PRIMARY KEY NOT NULL, club_id TEXT NOT NULL REFERENCES clubs(id),
                player_id TEXT NOT NULL REFERENCES players(id));
            INSERT INTO save_metadata VALUES (1, $save);
            INSERT INTO clubs VALUES ($club, $clubName);
            INSERT INTO players VALUES ($player, $playerName);
            INSERT INTO contracts VALUES ($contract, $contractClub, $player);
            PRAGMA user_version = 1;
            """;
        command.Parameters.AddWithValue("$save", saveId.ToString());
        command.Parameters.AddWithValue("$club", f.Club.Id.ToString());
        command.Parameters.AddWithValue("$clubName", f.Club.Name);
        command.Parameters.AddWithValue("$player", f.Player.Id.ToString());
        command.Parameters.AddWithValue("$playerName", f.Player.Name);
        command.Parameters.AddWithValue("$contract", f.Contract.Id.ToString());
        command.Parameters.AddWithValue("$contractClub", orphan ? ClubId.New().ToString() : f.Club.Id.ToString());
        command.ExecuteNonQuery();
    }

    /// <summary>Conserva filas antiguas y deja desconocidos los datos que nunca existieron.</summary>
    [Fact]
    public void VersionOneMigratesToANewFileWithoutInventingOrChangingOldData()
    {
        var f = new Fixture();
        var id = SaveId.New();
        var path = Database("legacy");
        CreateLegacy(path, f, id);
        var before = Hash(path);
        var old = new SqliteSaveStore(path);
        Assert.Equal(1, old.ReadMetadata()!.SchemaVersion);
        Assert.Equal(1, old.Initialize(id).SchemaVersion);
        Assert.Throws<NotSupportedException>(() => new SqliteWorldStore(path).Load());
        var migrated = SqliteWorldFiles.MigrateCopy(path, Database("migrated"));
        Assert.Equal(before, Hash(path));
        Assert.Equal(1, old.ReadMetadata()!.SchemaVersion);
        Assert.Equal(id, migrated.ReadInfo().SaveId);
        Assert.Equal(SqliteSaveStore.CurrentSchemaVersion, new SqliteSaveStore(Database("migrated")).ReadMetadata()!.SchemaVersion);
        var data = migrated.Load();
        Assert.Equal(f.Club.Id, Assert.Single(data.Clubs).Id);
        Assert.Equal(f.Club.Name, data.Clubs[0].Name);
        Assert.Null(data.Clubs[0].City);
        Assert.Null(data.Clubs[0].Reputation);
        Assert.Equal(f.Player.Id, Assert.Single(data.Players).Id);
        Assert.Null(data.Players[0].BirthDate);
        Assert.Empty(data.Players[0].Attributes.AllValues);
        Assert.Equal(f.Contract.Id, Assert.Single(data.Contracts).Id);
        Assert.Null(data.Contracts[0].StartDate);
        Assert.Null(data.Contracts[0].EndDate);
        // Un contrato histórico sin fechas bloquea otro contrato hasta completar sus fechas.
        migrated.Apply(new WorldData(clubs: [f.Club], players: [f.Player]));
        Assert.Throws<SqliteException>(() => migrated.Apply(new WorldData(contracts:
            [new Contract(ContractId.New(), f.Club.Id, f.Player.Id, Start, End)])));
        migrated.Apply(new WorldData(contracts: [f.Contract]));
        Assert.Equal(f.Contract, Assert.Single(migrated.Load().Contracts));
        var currentCopy = SqliteWorldFiles.MigrateCopy(Database("migrated"), Database("current-copy"));
        Assert.Equal(migrated.ReadInfo(), currentCopy.ReadInfo());
        Assert.Equal(f.Contract, Assert.Single(currentCopy.Load().Contracts));
    }

    /// <summary>Una referencia histórica corrupta debe bloquear la copia sin destruir el origen.</summary>
    [Fact]
    public void MigrationFailurePreservesTheOriginalAndDoesNotPublishTheDestination()
    {
        var f = new Fixture();
        var path = Database("broken-legacy");
        CreateLegacy(path, f, SaveId.New(), orphan: true);
        var before = Hash(path);
        Assert.Throws<InvalidOperationException>(() => SqliteWorldFiles.MigrateCopy(path, Database("bad-copy")));
        Assert.Equal(before, Hash(path));
        Assert.False(File.Exists(Database("bad-copy")));
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    /// <summary>Elimina únicamente el directorio temporal creado por esta instancia de prueba.</summary>
    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
