using Furvo.Core.Domain;
using Furvo.Core.Worlds;
using Microsoft.Data.Sqlite;

namespace Furvo.Persistence;

/// <summary>Infraestructura SQLite compartida: conexiones seguras, metadatos y migraciones versionadas.</summary>
internal static class WorldDatabase
{
    internal const int CurrentVersion = 4;

    /// <summary>Evita que distintas carpetas de trabajo redirijan inadvertidamente una partida.</summary>
    internal static string PathFor(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!Path.IsPathFullyQualified(path)) throw new ArgumentException("Se requiere una ruta absoluta.", nameof(path));
        return Path.GetFullPath(path);
    }

    /// <summary>Activa claves foráneas en cada conexión y desactiva pooling para liberar los archivos al terminar.</summary>
    internal static SqliteConnection Open(string path, SqliteOpenMode mode = SqliteOpenMode.ReadOnly)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = PathFor(path), Mode = mode, ForeignKeys = true, Pooling = false
        }.ToString());
        try { connection.Open(); return connection; }
        catch { connection.Dispose(); throw; }
    }

    /// <summary>Asocia SQL y parámetros a la transacción del llamador; null se conserva como desconocido SQLite.</summary>
    internal static SqliteCommand Command(SqliteConnection connection, SqliteTransaction transaction,
        string sql, params (string Name, object? Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return command;
    }

    /// <summary>Rechaza formatos futuros antes de ejecutar cambios para impedir degradaciones accidentales.</summary>
    internal static int Version(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = Command(connection, transaction, "PRAGMA user_version;");
        var version = Convert.ToInt32(command.ExecuteScalar());
        if (version < 0 || version > CurrentVersion)
            throw new NotSupportedException($"Versión de esquema no compatible: {version}.");
        return version;
    }

    /// <summary>Obliga a migrar mediante copia antes de leer o editar un mundo de otro formato.</summary>
    internal static void RequireCurrent(SqliteConnection connection, SqliteTransaction transaction)
    {
        if (Version(connection, transaction) != CurrentVersion)
            throw new NotSupportedException("Se requiere el esquema 4; migra a una copia mediante MigrateCopy.");
    }

    /// <summary>Ejecuta scripts consecutivos en la transacción recibida; la versión avanza solo junto con sus datos e integridad.</summary>
    internal static void Migrate(SqliteConnection connection, SqliteTransaction transaction)
    {
        var version = Version(connection, transaction);
        for (var next = version + 1; next <= CurrentVersion; next++)
        {
            if (next == 3) ValidateScales(connection, transaction);
            var resource = next switch
            {
                1 => "001_Initial.sql",
                2 => "002_World.sql",
                3 => "003_ClubUniverseAndScales.sql",
                4 => "004_PlayerPositionsAndGeneration.sql",
                _ => throw new NotSupportedException("Migración desconocida.")
            };
            using var stream = typeof(WorldDatabase).Assembly.GetManifestResourceStream("Furvo.Persistence.Migrations." + resource)
                ?? throw new InvalidOperationException("Falta la migración " + resource);
            using var reader = new StreamReader(stream);
            using var command = Command(connection, transaction, reader.ReadToEnd());
            command.ExecuteNonQuery();
            command.CommandText = $"PRAGMA user_version = {next};";
            command.ExecuteNonQuery();
        }
        using var check = Command(connection, transaction, "PRAGMA foreign_key_check;");
        using var result = check.ExecuteReader();
        if (result.Read()) throw new InvalidOperationException("La base contiene referencias huérfanas.");
    }

    /// <summary>Recoge todos los valores incompatibles antes de reconstruir restricciones; no recorta ni reescala puntuaciones.</summary>
    private static void ValidateScales(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = Command(connection, transaction, """
            SELECT id, 'Club.reputation', CAST(reputation AS TEXT), '1..5' FROM clubs
            WHERE reputation IS NOT NULL AND (typeof(reputation) <> 'integer' OR reputation NOT BETWEEN 1 AND 5)
            UNION ALL
            SELECT player_id, 'Player.' || attribute_code, CAST(value AS TEXT), '1..20' FROM player_attributes
            WHERE typeof(value) <> 'integer' OR value NOT BETWEEN 1 AND 20
            ORDER BY 1, 2;
            """);
        using var reader = command.ExecuteReader();
        var incompatible = new List<IncompatibleScaleValue>();
        while (reader.Read()) incompatible.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
        if (incompatible.Count > 0) throw new SchemaCompatibilityException(incompatible);
    }

    /// <summary>Vincula el archivo a su origen y rol sin duplicar esa información en todas las entidades.</summary>
    internal static void SetInfo(SqliteConnection connection, SqliteTransaction transaction,
        WorldFileKind kind, WorldId worldId)
    {
        using var command = Command(connection, transaction,
            "INSERT INTO world_info(singleton, kind, world_id) VALUES (1, $kind, $id);",
            ("$kind", kind == WorldFileKind.InitialWorld ? "initial" : "save"), ("$id", worldId.ToString()));
        command.ExecuteNonQuery();
    }

    /// <summary>Solo permite formatos anteriores cuando se pide inspección explícita; verifica coherencia de rol y SaveId.</summary>
    internal static WorldFileInfo Info(SqliteConnection connection, SqliteTransaction transaction, bool allowLegacy = false)
    {
        if (!allowLegacy) RequireCurrent(connection, transaction);
        else if (Version(connection, transaction) < 2) throw new NotSupportedException("Sin metadatos del mundo.");
        using var command = Command(connection, transaction, """
            SELECT kind, world_id, (SELECT save_id FROM save_metadata WHERE singleton = 1)
            FROM world_info WHERE singleton = 1;
            """);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw new InvalidOperationException("Faltan metadatos del mundo.");
        var kind = reader.GetString(0) switch
        {
            "initial" => WorldFileKind.InitialWorld,
            "save" => WorldFileKind.Save,
            _ => throw new InvalidOperationException("Tipo de mundo inválido.")
        };
        var saveId = reader.IsDBNull(2) ? (SaveId?)null : new SaveId(Guid.Parse(reader.GetString(2)));
        if ((kind == WorldFileKind.Save) != saveId.HasValue)
            throw new InvalidOperationException("Metadatos de partida incoherentes.");
        return new WorldFileInfo(kind, new WorldId(Guid.Parse(reader.GetString(1))), saveId);
    }
}
