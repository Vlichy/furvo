using System.Text.Json;
using Furvo.Core.Domain;
using Furvo.Core.Finance;
using Furvo.Core.Worlds;
using Microsoft.Data.Sqlite;

namespace Furvo.Persistence;

/// <summary>Operaciones técnicas en una transacción ajena. Nunca abre conexiones ni confirma la transacción.</summary>
public static class SqliteFinancialOperations
{
    internal static void CheckCareer(SqliteConnection connection, SqliteTransaction transaction, SaveId career)
    {
        if (transaction.Connection != connection)
            throw new ArgumentException("La transacción no pertenece a la conexión indicada.");
        var info = WorldDatabase.Info(connection, transaction);
        if (career.Value == Guid.Empty || info.Kind != WorldFileKind.Save || info.SaveId != career)
            throw new InvalidOperationException("Las operaciones pertenecen a la carrera indicada, nunca a una publicación.");
    }

    internal static bool HasSchema(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = WorldDatabase.Command(connection, transaction,
            "SELECT count(*) FROM sqlite_master WHERE type='table' AND name IN ('financial_operation_schema','financial_operations')");
        var count = Convert.ToInt32(command.ExecuteScalar());
        if (count == 0) return false;
        if (count != 2) throw new InvalidDataException("Extensión de operaciones incompleta.");
        command.CommandText = "SELECT version FROM financial_operation_schema WHERE singleton=1";
        if (Convert.ToInt32(command.ExecuteScalar()) != 1)
            throw new NotSupportedException("Versión de operaciones financieras no compatible.");
        return true;
    }

    /// <summary>Instalación explícita y aditiva. También se revierte si el llamador deshace su transacción.</summary>
    public static void Migrate(SqliteConnection connection, SqliteTransaction transaction, SaveId career)
    {
        CheckCareer(connection, transaction, career);
        if (!HasSchema(connection, transaction))
            CareerJson.Migrate(connection, transaction, "FinancialOperations001_Initial.sql");
    }

    public static FinancialOperationResult? Find(SqliteConnection connection, SqliteTransaction transaction,
        FinancialOperationRequest request)
    {
        var fingerprint = request.Fingerprint();
        CheckCareer(connection, transaction, request.Career);
        if (!HasSchema(connection, transaction)) return null;
        using var command = WorldDatabase.Command(connection, transaction,
            "SELECT id,request_hash,result_json FROM financial_operations WHERE id=$id OR (kind=$kind AND origin=$origin)",
            ("$id", request.Id.ToString("D")), ("$kind", request.Kind), ("$origin", request.Origin));
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        if (reader.GetString(0) != request.Id.ToString("D") || reader.GetString(1) != fingerprint)
            throw new InvalidOperationException("El identificador o el origen ya corresponde a otra solicitud.");
        return new FinancialOperationResult(reader.GetString(2), true);
    }

    /// <summary>
    /// El delegado solo realiza escrituras SQLite en esta unidad de trabajo y devuelve JSON.
    /// No puede confirmar/cerrar la transacción ni producir efectos externos. El llamador debe usar una
    /// transacción de escritura; ante un conflicto de bloqueo, deshacerla y reintentar la misma solicitud.
    /// </summary>
    public static FinancialOperationResult Execute(SqliteConnection connection, SqliteTransaction transaction,
        FinancialOperationRequest request, Func<SqliteConnection, SqliteTransaction, string> execute)
    {
        ArgumentNullException.ThrowIfNull(execute);
        var replay = Find(connection, transaction, request);
        if (replay is not null) return replay;
        if (!HasSchema(connection, transaction))
            throw new InvalidOperationException("Instala explícitamente la extensión de operaciones antes de escribir.");

        var savepoint = "financial_operation_" + request.Id.ToString("N");
        using var command = WorldDatabase.Command(connection, transaction, "SAVEPOINT " + savepoint);
        command.ExecuteNonQuery();
        try
        {
            var result = execute(connection, transaction);
            using var json = JsonDocument.Parse(result);
            using var insert = WorldDatabase.Command(connection, transaction,
                "INSERT INTO financial_operations VALUES($id,$kind,$origin,$hash,$result)",
                ("$id", request.Id.ToString("D")), ("$kind", request.Kind), ("$origin", request.Origin),
                ("$hash", request.Fingerprint()), ("$result", result));
            insert.ExecuteNonQuery();
            command.CommandText = "RELEASE SAVEPOINT " + savepoint;
            command.ExecuteNonQuery();
            return new FinancialOperationResult(result, false);
        }
        catch
        {
            // El fallo no deja efectos parciales aunque el llamador continúe utilizando la transacción.
            command.CommandText = "ROLLBACK TO SAVEPOINT " + savepoint;
            command.ExecuteNonQuery();
            command.CommandText = "RELEASE SAVEPOINT " + savepoint;
            command.ExecuteNonQuery();
            throw;
        }
    }
}
