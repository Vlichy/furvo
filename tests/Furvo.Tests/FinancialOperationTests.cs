using System.Security.Cryptography;
using Furvo.Core.Domain;
using Furvo.Core.Finance;
using Furvo.Persistence;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Furvo.Tests;

/// <summary>Copias temporales. El contador prueba atomicidad sin tocar operaciones económicas del juego.</summary>
public sealed class FinancialOperationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "FurvoFinancialOperationTests", Guid.NewGuid().ToString("N"));
    private readonly SaveId _career = SaveId.New();
    private string Database => Path.Combine(_directory, "save.db");

    public FinancialOperationTests()
    {
        Directory.CreateDirectory(_directory);
        new SqliteSaveStore(Database).Initialize(_career);
        using var connection = Connect();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE operation_probe(value INTEGER NOT NULL); INSERT INTO operation_probe VALUES(0)";
        command.ExecuteNonQuery();
    }

    private SqliteConnection Connect()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = Database, Mode = SqliteOpenMode.ReadWrite, ForeignKeys = true, Pooling = false
        }.ToString());
        connection.Open();
        return connection;
    }

    private FinancialOperationRequest Request() => new(Guid.NewGuid(), _career, "probe-v1", "fixture/one", "{\"cents\":123}");

    private void Install()
    {
        using var connection = Connect();
        using var transaction = connection.BeginTransaction();
        SqliteFinancialOperations.Migrate(connection, transaction, _career);
        SqliteFinancialOperations.Migrate(connection, transaction, _career);
        transaction.Commit();
    }

    private static string Apply(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE operation_probe SET value=value+1";
        command.ExecuteNonQuery();
        return "{\"receipt\":\"stable-result\"}";
    }

    private long Counter()
    {
        using var connection = Connect();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM operation_probe";
        return (long)command.ExecuteScalar()!;
    }

    [Fact]
    public void HistoricalSaveCanBeReadWithoutInstallingOrChangingIt()
    {
        var before = SHA256.HashData(File.ReadAllBytes(Database));
        using (var connection = Connect())
        using (var transaction = connection.BeginTransaction(deferred: true))
        {
            Assert.Null(SqliteFinancialOperations.Find(connection, transaction, Request()));
            Assert.Throws<InvalidOperationException>(() => SqliteFinancialOperations.Execute(connection, transaction, Request(), Apply));
        }
        Assert.Equal(before, SHA256.HashData(File.ReadAllBytes(Database)));
        Assert.Equal(_career, new SqliteWorldStore(Database).ReadInfo().SaveId);
    }

    [Fact]
    public void RepeatedExecutionRecoversCommittedResultWithoutRepeatingEffect()
    {
        Install();
        var request = Request();
        using (var connection = Connect())
        using (var transaction = connection.BeginTransaction())
        {
            var first = SqliteFinancialOperations.Execute(connection, transaction, request, Apply);
            Assert.False(first.Replayed);
            Assert.Equal("{\"receipt\":\"stable-result\"}", first.ResultJson);
            transaction.Commit(); // Se simula perder la respuesta al consumidor después de confirmar.
        }
        using (var connection = Connect())
        using (var transaction = connection.BeginTransaction())
        {
            var recovered = SqliteFinancialOperations.Execute(connection, transaction, request,
                (_, _) => throw new InvalidOperationException("El efecto no debe repetirse."));
            Assert.True(recovered.Replayed);
            Assert.Equal("{\"receipt\":\"stable-result\"}", recovered.ResultJson);
            transaction.Commit();
        }
        Assert.Equal(1, Counter());
    }

    [Fact]
    public void RejectsChangedTermsAndNewIdentifierForTheSameBusinessOrigin()
    {
        Install();
        var request = Request();
        using var connection = Connect();
        using var transaction = connection.BeginTransaction();
        SqliteFinancialOperations.Execute(connection, transaction, request, Apply);
        Assert.Throws<InvalidOperationException>(() => SqliteFinancialOperations.Execute(connection, transaction,
            request with { Terms = "{\"cents\":124}" }, Apply));
        Assert.Throws<InvalidOperationException>(() => SqliteFinancialOperations.Execute(connection, transaction,
            request with { Kind = "different-v1" }, Apply));
        Assert.Throws<InvalidOperationException>(() => SqliteFinancialOperations.Execute(connection, transaction,
            request with { Origin = "different-origin" }, Apply));
        Assert.Throws<InvalidOperationException>(() => SqliteFinancialOperations.Execute(connection, transaction,
            request with { Id = Guid.NewGuid() }, Apply));
        transaction.Commit();
        Assert.Equal(1, Counter());
    }

    [Fact]
    public void FailedExecutionRollsBackItsEffectsEvenIfOuterTransactionCommits()
    {
        Install();
        var request = Request();
        using (var connection = Connect())
        using (var transaction = connection.BeginTransaction())
        {
            Assert.Throws<InvalidDataException>(() => SqliteFinancialOperations.Execute(connection, transaction, request, (c, t) =>
            {
                Apply(c, t);
                throw new InvalidDataException("Fallo de prueba antes de confirmar.");
            }));
            Assert.Null(SqliteFinancialOperations.Find(connection, transaction, request));
            transaction.Commit();
        }
        Assert.Equal(0, Counter());
        using (var connection = Connect())
        using (var transaction = connection.BeginTransaction())
        {
            Assert.False(SqliteFinancialOperations.Execute(connection, transaction, request, Apply).Replayed);
            transaction.Commit();
        }
        Assert.Equal(1, Counter());
    }

    [Fact]
    public void OuterRollbackRemovesOperationAndBusinessEffectTogether()
    {
        Install();
        var request = Request();
        using (var connection = Connect())
        using (var transaction = connection.BeginTransaction())
        {
            SqliteFinancialOperations.Execute(connection, transaction, request, Apply);
            transaction.Rollback();
        }
        using (var connection = Connect())
        using (var transaction = connection.BeginTransaction(deferred: true))
            Assert.Null(SqliteFinancialOperations.Find(connection, transaction, request));
        Assert.Equal(0, Counter());
    }

    [Fact]
    public void InvalidResultCannotLeaveAPartialOperation()
    {
        Install();
        var request = Request();
        using (var connection = Connect())
        using (var transaction = connection.BeginTransaction())
        {
            Assert.ThrowsAny<System.Text.Json.JsonException>(() => SqliteFinancialOperations.Execute(connection, transaction, request,
                (c, t) => { Apply(c, t); return "invalid-json"; }));
            Assert.Null(SqliteFinancialOperations.Find(connection, transaction, request));
            transaction.Commit();
        }
        Assert.Equal(0, Counter());
    }

    [Fact]
    public void MigrationRollsBackAndRejectsAnotherCareerOrInitialWorld()
    {
        using (var connection = Connect())
        using (var transaction = connection.BeginTransaction())
        {
            Assert.Throws<InvalidOperationException>(() => SqliteFinancialOperations.Migrate(connection, transaction, SaveId.New()));
            SqliteFinancialOperations.Migrate(connection, transaction, _career);
            transaction.Rollback();
        }
        using (var connection = Connect())
        using (var transaction = connection.BeginTransaction(deferred: true))
            Assert.Null(SqliteFinancialOperations.Find(connection, transaction, Request()));

        var initial = Path.Combine(_directory, "initial.db");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "published-v4.db"), initial);
        using var world = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = initial, Pooling = false }.ToString());
        world.Open();
        using var worldTransaction = world.BeginTransaction();
        Assert.Throws<InvalidOperationException>(() => SqliteFinancialOperations.Migrate(world, worldTransaction, _career));
    }

    [Fact]
    public void SeparateCareersCanUseTheSameTokenAndOriginIndependently()
    {
        Install();
        var request = Request();
        using (var connection = Connect())
        using (var transaction = connection.BeginTransaction())
        {
            Assert.Throws<InvalidOperationException>(() => SqliteFinancialOperations.Find(connection, transaction,
                request with { Career = SaveId.New() }));
            SqliteFinancialOperations.Execute(connection, transaction, request, Apply);
            transaction.Commit();
        }
        using var other = new FinancialOperationTests();
        other.Install();
        using (var connection = other.Connect())
        using (var transaction = connection.BeginTransaction())
        {
            Assert.False(SqliteFinancialOperations.Execute(connection, transaction, request with { Career = other._career }, Apply).Replayed);
            transaction.Commit();
        }
        Assert.Equal(1, Counter());
        Assert.Equal(1, other.Counter());
    }

    [Fact]
    public async Task ConcurrentWritersApplyTheSameOperationOnlyOnce()
    {
        Install();
        var request = Request();
        var attempts = Enumerable.Range(0, 2).Select(_ => Task.Run(() =>
        {
            using var connection = Connect();
            using var transaction = connection.BeginTransaction(deferred: false);
            var result = SqliteFinancialOperations.Execute(connection, transaction, request, Apply);
            transaction.Commit();
            return result;
        }));
        var results = await Task.WhenAll(attempts);
        Assert.Single(results, result => !result.Replayed);
        Assert.Single(results, result => result.Replayed);
        Assert.Equal(1, Counter());
    }

    [Fact]
    public void RejectsUnknownSchemaAndIncompleteRequests()
    {
        Install();
        var request = Request();
        using var connection = Connect();
        using var transaction = connection.BeginTransaction();
        Assert.Throws<ArgumentException>(() => SqliteFinancialOperations.Find(connection, transaction,
            request with { Id = Guid.Empty }));
        Assert.Throws<ArgumentException>(() => SqliteFinancialOperations.Find(connection, transaction,
            request with { Terms = "" }));
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        // Copia aislada que simula una versión futura. El lector debe rechazarla sin migrar.
        command.CommandText = "DROP TABLE financial_operation_schema; CREATE TABLE financial_operation_schema(singleton INTEGER PRIMARY KEY, version INTEGER); INSERT INTO financial_operation_schema VALUES(1,2)";
        command.ExecuteNonQuery();
        Assert.Throws<NotSupportedException>(() => SqliteFinancialOperations.Find(connection, transaction, request));
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}

public sealed partial class CareerCreationTests
{
    [Fact]
    public void FinancialOperationExtensionPreservesExistingCareerAndEconomicContracts()
    {
        var profile = Profile();
        var store = EconomicStore();
        var career = store.Create(Request(profile, Furvo.Core.Careers.CareerStartMode.SignOffer, EconomicOffer(profile)));
        var file = store.PathFor(career.Id);
        var before = store.Load(career.Id);
        var resources = SqliteEconomyStore.ReadResources(file);
        var sourceHash = SHA256.HashData(File.ReadAllBytes(EconomicSource));
        using (var connection = Connection(file))
        using (var transaction = connection.BeginTransaction())
        {
            SqliteFinancialOperations.Migrate(connection, transaction, career.Id);
            var request = new FinancialOperationRequest(Guid.NewGuid(), career.Id, "compatibility-v1", "fixture/career", "{}");
            SqliteFinancialOperations.Execute(connection, transaction, request, (_, _) => "{\"checked\":true}");
            transaction.Commit();
        }
        var after = store.Load(career.Id);
        Assert.Equal(before.Id, after.Id);
        Assert.Equal(before.CurrentDate, after.CurrentDate);
        Assert.Equal(before.SeasonId, after.SeasonId);
        Assert.Equal(before.Contract!.AcceptedOffer.Terms, after.Contract!.AcceptedOffer.Terms);
        Assert.Equal(resources, SqliteEconomyStore.ReadResources(file));
        Assert.Equal(sourceHash, SHA256.HashData(File.ReadAllBytes(EconomicSource)));
        using var check = Connection(file);
        using var integrity = check.CreateCommand();
        integrity.CommandText = "PRAGMA integrity_check";
        Assert.Equal("ok", integrity.ExecuteScalar());
        integrity.CommandText = "PRAGMA foreign_key_check";
        using var rows = integrity.ExecuteReader();
        Assert.False(rows.Read());
    }
}
