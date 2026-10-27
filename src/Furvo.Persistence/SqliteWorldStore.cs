using System.Globalization;
using Furvo.Core.Domain;
using Furvo.Core.Worlds;
using Microsoft.Data.Sqlite;

namespace Furvo.Persistence;

/// <summary>Adaptador del puerto de mundo: lecturas coherentes y lotes transaccionales sobre una partida.</summary>
public sealed class SqliteWorldStore : IWorldStore
{
    private readonly string _path;

    // No crea archivos ni migra automáticamente.
    /// <summary>Guarda la ruta sin crear bases ni migrarlas de manera implícita.</summary>
    public SqliteWorldStore(string databasePath) => _path = WorldDatabase.PathFor(databasePath);

    /// <summary>Lee metadatos en una transacción de lectura y exige el esquema compatible.</summary>
    public WorldFileInfo ReadInfo()
    {
        using var connection = WorldDatabase.Open(_path);
        using var transaction = connection.BeginTransaction(deferred: true);
        return WorldDatabase.Info(connection, transaction);
    }

    /// <summary>Lee entidades y atributos bajo la misma instantánea para evitar combinar estados de momentos distintos.</summary>
    public WorldData Load()
    {
        using var connection = WorldDatabase.Open(_path);
        using var transaction = connection.BeginTransaction(deferred: true);
        _ = WorldDatabase.Info(connection, transaction);
        var values = Read(connection, transaction,
            "SELECT player_id, attribute_code, value FROM player_attributes ORDER BY player_id, attribute_code;",
            r => (PlayerId: Id(r, 0), Code: r.GetString(1), Value: r.GetInt32(2)))
            .GroupBy(x => x.PlayerId).ToDictionary(x => x.Key,
                x => new PlayerAttributes(x.Select(v => new KeyValuePair<string, int>(v.Code, v.Value))));
        var roles = Read(connection, transaction,
            "SELECT player_id, role, is_primary FROM player_positions ORDER BY player_id, is_primary DESC, role;",
            r => (PlayerId: Id(r, 0), Role: (PlayerRole)r.GetInt32(1), Primary: r.GetBoolean(2)))
            .GroupBy(x => x.PlayerId).ToDictionary(x => x.Key,
                x => new PlayerPositions(x.Single(v => v.Primary).Role, x.Where(v => !v.Primary).Select(v => v.Role)));
        return new WorldData(
            clubs: Read(connection, transaction, "SELECT id, name, city, country, reputation, short_name, region, foundation_year, sporting_identity, primary_color, secondary_color, stadium_name, stadium_capacity, initial_category FROM clubs ORDER BY id;", r => new Club(new ClubId(Id(r, 0)), r.GetString(1), Text(r, 2), Text(r, 3), Number(r, 4), r.IsDBNull(5) ? null : new ClubProfile(r.GetString(5), r.GetString(6), r.GetInt32(7), r.GetString(8), r.GetString(9), r.GetString(10), r.GetString(11), r.GetInt32(12), r.GetString(13)))),
            players: Read(connection, transaction, "SELECT id, name, birth_date, nationality, main_position FROM players ORDER BY id;", r => new Player(new PlayerId(Id(r, 0)), r.GetString(1), Day(r, 2), Text(r, 3), r.IsDBNull(4) ? null : (PlayerPosition)r.GetInt32(4), values.GetValueOrDefault(Id(r, 0), PlayerAttributes.Empty), roles.GetValueOrDefault(Id(r, 0)))),
            contracts: Read(connection, transaction, "SELECT id, club_id, player_id, start_date, end_date FROM contracts ORDER BY id;", r => new Contract(new ContractId(Id(r, 0)), new ClubId(Id(r, 1)), new PlayerId(Id(r, 2)), Day(r, 3), Day(r, 4))),
            coaches: Read(connection, transaction, "SELECT id, name, club_id FROM coaches ORDER BY id;", r => new Coach(new CoachId(Id(r, 0)), r.GetString(1), new ClubId(Id(r, 2)))),
            competitions: Read(connection, transaction, "SELECT id, name, country FROM competitions ORDER BY id;", r => new Competition(new CompetitionId(Id(r, 0)), r.GetString(1), r.GetString(2))),
            seasons: Read(connection, transaction, "SELECT id, start_date, end_date, name FROM seasons ORDER BY id;", r => new Season(new SeasonId(Id(r, 0)), Day(r, 1)!.Value, Day(r, 2)!.Value, Text(r, 3))),
            registrations: Read(connection, transaction, "SELECT club_id, competition_id, season_id FROM registrations ORDER BY club_id, competition_id, season_id;", r => new Registration(new ClubId(Id(r, 0)), new CompetitionId(Id(r, 1)), new SeasonId(Id(r, 2)))));
    }

    /// <summary>Rechaza plantillas y confirma todos los cambios juntos; cualquier error provoca rollback al disponer la transacción.</summary>
    public void Apply(WorldData changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        using var connection = WorldDatabase.Open(_path, SqliteOpenMode.ReadWrite);
        using var transaction = connection.BeginTransaction();
        if (WorldDatabase.Info(connection, transaction).Kind != WorldFileKind.Save)
            throw new InvalidOperationException("El mundo inicial es inmutable; crea una partida para editarlo.");
        Write(connection, transaction, changes);
        transaction.Commit();
    }

    /// <summary>Guarda primero entidades referenciadas; las FK y triggers mantienen integridad incluso si falla una escritura posterior.</summary>
    internal static void Write(SqliteConnection connection, SqliteTransaction transaction, WorldData changes)
    {
        // Se valida el lote completo antes de escribir. Las FK y los triggers garantizan el estado persistido.
        if (changes.Clubs.Any(x => x.City is null || x.Country is null || x.Reputation is null)
            || changes.Players.Any(x => x.BirthDate is null || x.Nationality is null || x.MainPosition is null)
            || changes.Contracts.Any(x => x.StartDate is null || x.EndDate is null))
            throw new ArgumentException("Las altas/actualizaciones requieren datos completos. Los NULL históricos solo se conservan al migrar.", nameof(changes));

        foreach (var x in changes.Clubs)
        {
            using var command = WorldDatabase.Command(connection, transaction,
                "INSERT INTO clubs (id, name, city, country, reputation, short_name, region, foundation_year, sporting_identity, primary_color, secondary_color, stadium_name, stadium_capacity, initial_category) VALUES ($id, $name, $city, $country, $reputation, $short_name, $region, $foundation_year, $sporting_identity, $primary_color, $secondary_color, $stadium_name, $stadium_capacity, $initial_category) ON CONFLICT(id) DO UPDATE SET name = excluded.name, city = excluded.city, country = excluded.country, reputation = excluded.reputation, short_name = COALESCE(excluded.short_name, clubs.short_name), region = COALESCE(excluded.region, clubs.region), foundation_year = COALESCE(excluded.foundation_year, clubs.foundation_year), sporting_identity = COALESCE(excluded.sporting_identity, clubs.sporting_identity), primary_color = COALESCE(excluded.primary_color, clubs.primary_color), secondary_color = COALESCE(excluded.secondary_color, clubs.secondary_color), stadium_name = COALESCE(excluded.stadium_name, clubs.stadium_name), stadium_capacity = COALESCE(excluded.stadium_capacity, clubs.stadium_capacity), initial_category = COALESCE(excluded.initial_category, clubs.initial_category);",
                ("$id", x.Id.ToString()), ("$name", x.Name), ("$city", x.City), ("$country", x.Country), ("$reputation", x.Reputation), ("$short_name", x.Profile?.ShortName), ("$region", x.Profile?.Region), ("$foundation_year", x.Profile?.FoundationYear), ("$sporting_identity", x.Profile?.SportingIdentity), ("$primary_color", x.Profile?.PrimaryColor), ("$secondary_color", x.Profile?.SecondaryColor), ("$stadium_name", x.Profile?.StadiumName), ("$stadium_capacity", x.Profile?.StadiumCapacity), ("$initial_category", x.Profile?.InitialCategory));
            command.ExecuteNonQuery();
        }

        foreach (var x in changes.Players)
        {
            // Un desglose aportado se sustituye completo en la misma transacción; si se omite se conserva.
            if (x.Positions is not null)
            {
                using var clear = WorldDatabase.Command(connection, transaction,
                    "DELETE FROM player_positions WHERE player_id = $id AND is_primary = 0; DELETE FROM player_positions WHERE player_id = $id;", ("$id", x.Id.ToString()));
                clear.ExecuteNonQuery();
            }
            using var command = WorldDatabase.Command(connection, transaction,
                "INSERT INTO players (id, name, birth_date, nationality, main_position) VALUES ($id, $name, $birth_date, $nationality, $main_position) ON CONFLICT(id) DO UPDATE SET name = excluded.name, birth_date = excluded.birth_date, nationality = excluded.nationality, main_position = excluded.main_position;",
                ("$id", x.Id.ToString()), ("$name", x.Name), ("$birth_date", Date(x.BirthDate)), ("$nationality", x.Nationality), ("$main_position", x.MainPosition.HasValue ? (int)x.MainPosition.Value : null));
            command.ExecuteNonQuery();
            if (x.Positions is { } positions)
            {
                foreach (var role in new[] { positions.Primary }.Concat(positions.Secondary))
                {
                    using var position = WorldDatabase.Command(connection, transaction,
                        "INSERT INTO player_positions(player_id, role, is_primary) VALUES ($player, $role, $primary);",
                        ("$player", x.Id.ToString()), ("$role", (int)role), ("$primary", role == positions.Primary ? 1 : 0));
                    position.ExecuteNonQuery();
                }
            }
            foreach (var (code, value) in x.Attributes.AllValues)
            {
                using var attribute = WorldDatabase.Command(connection, transaction, """
                    INSERT INTO player_attributes(player_id, attribute_code, value) VALUES ($player, $code, $value)
                    ON CONFLICT(player_id, attribute_code) DO UPDATE SET value = excluded.value;
                    """, ("$player", x.Id.ToString()), ("$code", code), ("$value", value));
                attribute.ExecuteNonQuery();
            }
        }

        foreach (var x in changes.Competitions)
        {
            using var command = WorldDatabase.Command(connection, transaction,
                "INSERT INTO competitions (id, name, country) VALUES ($id, $name, $country) ON CONFLICT(id) DO UPDATE SET name = excluded.name, country = excluded.country;",
                ("$id", x.Id.ToString()), ("$name", x.Name), ("$country", x.Country));
            command.ExecuteNonQuery();
        }

        foreach (var x in changes.Seasons)
        {
            using var command = WorldDatabase.Command(connection, transaction,
                "INSERT INTO seasons (id, start_date, end_date, name) VALUES ($id, $start_date, $end_date, $name) ON CONFLICT(id) DO UPDATE SET start_date = excluded.start_date, end_date = excluded.end_date, name = COALESCE(excluded.name, seasons.name);",
                ("$id", x.Id.ToString()), ("$start_date", Date(x.StartDate)), ("$end_date", Date(x.EndDate)), ("$name", x.Name));
            command.ExecuteNonQuery();
        }

        foreach (var x in changes.Contracts)
        {
            using var command = WorldDatabase.Command(connection, transaction,
                "INSERT INTO contracts (id, club_id, player_id, start_date, end_date) VALUES ($id, $club_id, $player_id, $start_date, $end_date) ON CONFLICT(id) DO UPDATE SET club_id = excluded.club_id, player_id = excluded.player_id, start_date = excluded.start_date, end_date = excluded.end_date;",
                ("$id", x.Id.ToString()), ("$club_id", x.ClubId.ToString()), ("$player_id", x.PlayerId.ToString()), ("$start_date", Date(x.StartDate)), ("$end_date", Date(x.EndDate)));
            command.ExecuteNonQuery();
        }

        foreach (var x in changes.Coaches)
        {
            using var command = WorldDatabase.Command(connection, transaction,
                "INSERT INTO coaches (id, name, club_id) VALUES ($id, $name, $club_id) ON CONFLICT(id) DO UPDATE SET name = excluded.name, club_id = excluded.club_id;",
                ("$id", x.Id.ToString()), ("$name", x.Name), ("$club_id", x.ClubId.ToString()));
            command.ExecuteNonQuery();
        }

        foreach (var x in changes.Registrations)
        {
            using var command = WorldDatabase.Command(connection, transaction,
                "INSERT INTO registrations (club_id, competition_id, season_id) VALUES ($club_id, $competition_id, $season_id) ON CONFLICT(club_id, competition_id, season_id) DO NOTHING;",
                ("$club_id", x.ClubId.ToString()), ("$competition_id", x.CompetitionId.ToString()), ("$season_id", x.SeasonId.ToString()));
            command.ExecuteNonQuery();
        }
    }

    /// <summary>Materializa cada resultado antes de cerrar su lector, manteniendo la transacción común del mundo.</summary>
    private static List<T> Read<T>(SqliteConnection connection, SqliteTransaction transaction,
        string sql, Func<SqliteDataReader, T> map)
    {
        using var command = WorldDatabase.Command(connection, transaction, sql);
        using var reader = command.ExecuteReader();
        var result = new List<T>();
        while (reader.Read()) result.Add(map(reader));
        return result;
    }

    private static Guid Id(SqliteDataReader r, int index) => Guid.Parse(r.GetString(index));
    private static string? Text(SqliteDataReader r, int index) => r.IsDBNull(index) ? null : r.GetString(index);
    private static int? Number(SqliteDataReader r, int index) => r.IsDBNull(index) ? null : r.GetInt32(index);
    private static DateOnly? Day(SqliteDataReader r, int index) => r.IsDBNull(index) ? null :
        DateOnly.ParseExact(r.GetString(index), "yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static string? Date(DateOnly? value) => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
