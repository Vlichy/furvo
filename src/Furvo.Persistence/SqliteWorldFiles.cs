using Furvo.Core.Domain;
using Furvo.Core.Worlds;
using Microsoft.Data.Sqlite;

namespace Furvo.Persistence;

// Crea siempre archivos nuevos. La publicación mediante rename no sobrescribe destinos.
/// <summary>Crea archivos nuevos para plantillas, partidas o copias migradas; no sobrescribe destinos publicados.</summary>
public static class SqliteWorldFiles
{
    /// <summary>Publica una plantilla desde una instantánea validada dentro de una sola transacción.</summary>
    public static SqliteWorldStore CreateInitialWorld(string destination, WorldId worldId, WorldData data)
    {
        if (worldId.Value == Guid.Empty) throw new ArgumentException("Identidad vacía.", nameof(worldId));
        ArgumentNullException.ThrowIfNull(data);
        return Publish(destination, connection =>
        {
            using var transaction = connection.BeginTransaction();
            WorldDatabase.Migrate(connection, transaction);
            WorldDatabase.SetInfo(connection, transaction, WorldFileKind.InitialWorld, worldId);
            SqliteWorldStore.Write(connection, transaction, data);
            transaction.Commit();
        });
    }

    /// <summary>Publica datos y trazabilidad en la misma transacción, sin dejar una plantilla parcialmente identificada.</summary>
    internal static SqliteWorldStore CreateInitialWorldWithGeneration(string destination, WorldId worldId,
        WorldData data, SquadGenerationInfo info)
    {
        return Publish(destination, connection =>
        {
            using var transaction = connection.BeginTransaction();
            WorldDatabase.Migrate(connection, transaction);
            WorldDatabase.SetInfo(connection, transaction, WorldFileKind.InitialWorld, worldId);
            SqliteWorldStore.Write(connection, transaction, data);
            using var command = WorldDatabase.Command(connection, transaction, """
                INSERT INTO generation_metadata(singleton, generator_version, seed, parameter_hash, catalog_hash, source_world_id)
                VALUES (1, $version, $seed, $parameter, $catalog, $source);
                """, ("$version", info.GeneratorVersion), ("$seed", info.Seed), ("$parameter", info.ParameterHash),
                ("$catalog", info.CatalogHash), ("$source", info.SourceWorldId.ToString()));
            command.ExecuteNonQuery();
            transaction.Commit();
        });
    }

    /// <summary>Copia una instantánea SQLite consistente y cambia únicamente su rol e identidad de partida.</summary>
    public static SqliteWorldStore CreateSave(string initialWorld, string destination, SaveId saveId)
    {
        if (saveId.Value == Guid.Empty) throw new ArgumentException("Identidad vacía.", nameof(saveId));
        using var source = WorldDatabase.Open(initialWorld);
        using var snapshot = source.BeginTransaction(deferred: true);
        if (WorldDatabase.Info(source, snapshot).Kind != WorldFileKind.InitialWorld)
            throw new InvalidOperationException("Se requiere un mundo inicial, no otra partida.");
        return Publish(destination, target =>
        {
            source.BackupDatabase(target);
            using var transaction = target.BeginTransaction();
            using var command = WorldDatabase.Command(target, transaction, """
                UPDATE world_info SET kind = 'save' WHERE singleton = 1;
                INSERT INTO save_metadata(singleton, save_id) VALUES (1, $id);
                """, ("$id", saveId.ToString()));
            command.ExecuteNonQuery();
            transaction.Commit();
        });
    }

    // La migración conserva el SaveId y todas las filas originales. El origen nunca se escribe.
    /// <summary>Migra otro archivo, conserva los identificadores y deja el original abierto solo para lectura.</summary>
    public static SqliteWorldStore MigrateCopy(string sourcePath, string destination)
    {
        using var source = WorldDatabase.Open(sourcePath);
        using var snapshot = source.BeginTransaction(deferred: true);
        var version = WorldDatabase.Version(source, snapshot);
        if (version == 0) throw new NotSupportedException("El archivo no contiene una partida Furvo.");
        if (version == 1)
        {
            using var metadata = WorldDatabase.Command(source, snapshot, "SELECT save_id FROM save_metadata WHERE singleton = 1;");
            _ = new SaveId(Guid.Parse(metadata.ExecuteScalar() as string
                ?? throw new InvalidOperationException("Faltan metadatos de partida.")));
        }
        else _ = WorldDatabase.Info(source, snapshot, allowLegacy: true);
        return Publish(destination, target =>
        {
            source.BackupDatabase(target);
            using var transaction = target.BeginTransaction();
            WorldDatabase.Migrate(target, transaction);
            if (version == 1) WorldDatabase.SetInfo(target, transaction, WorldFileKind.Save, WorldId.New());
            transaction.Commit();
        });
    }

    /// <summary>Escribe en un temporal exclusivo y lo renombra sin sobrescribir; el finally limpia solo el temporal propio.</summary>
    internal static SqliteWorldStore Publish(string destination, Action<SqliteConnection> create)
    {
        var path = WorldDatabase.PathFor(destination);
        if (File.Exists(path)) throw new IOException("El destino ya existe; no se sobrescriben archivos.");
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        var ownsTemporary = false;
        try
        {
            using (new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { }
            ownsTemporary = true;
            using (var connection = WorldDatabase.Open(temporary, SqliteOpenMode.ReadWrite)) create(connection);
            File.Move(temporary, path, overwrite: false);
            return new SqliteWorldStore(path);
        }
        finally
        {
            // Solo el archivo temporal exclusivo de esta operación.
            if (ownsTemporary && File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
