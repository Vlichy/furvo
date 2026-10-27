using Furvo.Core.Domain;
using Furvo.Core.Saves;
using Furvo.Core.Worlds;
using Microsoft.Data.Sqlite;

namespace Furvo.Persistence;

/// <summary>Gestiona metadatos compatibles con versiones antiguas sin ejecutar migraciones sobre archivos existentes.</summary>
public sealed class SqliteSaveStore : ISaveStore
{
    public const int CurrentSchemaVersion = WorldDatabase.CurrentVersion;
    private readonly string _path;

    /// <summary>Valida una ruta explícita sin abrirla; la UI decidirá la carpeta escribible del usuario.</summary>
    public SqliteSaveStore(string databasePath) => _path = WorldDatabase.PathFor(databasePath);

    /// <summary>Inicializa únicamente bases vacías y comprueba SaveId; una base ajena o de otra partida se rechaza.</summary>
    public SaveMetadata Initialize(SaveId id)
    {
        if (id.Value == Guid.Empty) throw new ArgumentException("Identidad vacía.", nameof(id));
        using var connection = WorldDatabase.Open(_path, SqliteOpenMode.ReadWriteCreate);
        using var transaction = connection.BeginTransaction();
        var version = WorldDatabase.Version(connection, transaction);
        if (version == 0)
        {
            // No se inicializa una base SQLite ajena que contenga tablas.
            using var tables = WorldDatabase.Command(connection, transaction,
                "SELECT count(*) FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';");
            if (Convert.ToInt32(tables.ExecuteScalar()) != 0)
                throw new InvalidOperationException("El archivo no es una base Furvo vacía.");
            WorldDatabase.Migrate(connection, transaction);
            using var insert = WorldDatabase.Command(connection, transaction,
                "INSERT INTO save_metadata(singleton, save_id) VALUES (1, $id);", ("$id", id.ToString()));
            insert.ExecuteNonQuery();
            WorldDatabase.SetInfo(connection, transaction, WorldFileKind.Save, WorldId.New());
            version = CurrentSchemaVersion;
        }
        var metadata = Read(connection, transaction, version);
        if (metadata.Id != id) throw new InvalidOperationException("La base pertenece a otra partida.");
        transaction.Commit();
        return metadata;
    }

    /// <summary>Lee sin crear archivos ausentes; mantiene disponibles los metadatos de esquemas anteriores.</summary>
    public SaveMetadata? ReadMetadata()
    {
        if (!File.Exists(_path)) return null;
        using var connection = WorldDatabase.Open(_path);
        using var transaction = connection.BeginTransaction(deferred: true);
        var version = WorldDatabase.Version(connection, transaction);
        return version == 0 ? null : Read(connection, transaction, version);
    }

    /// <summary>Comprueba el rol cuando está disponible para no confundir un mundo inicial con una partida.</summary>
    private static SaveMetadata Read(SqliteConnection connection, SqliteTransaction transaction, int version)
    {
        if (version >= 2 && WorldDatabase.Info(connection, transaction, allowLegacy: true).Kind != WorldFileKind.Save)
            throw new InvalidOperationException("El mundo inicial no es una partida.");
        using var command = WorldDatabase.Command(connection, transaction,
            "SELECT save_id FROM save_metadata WHERE singleton = 1;");
        var value = command.ExecuteScalar() as string
            ?? throw new InvalidOperationException("Faltan los metadatos de la partida.");
        return new SaveMetadata(new SaveId(Guid.Parse(value)), version);
    }
}
