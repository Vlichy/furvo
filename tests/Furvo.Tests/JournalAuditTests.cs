using Furvo.Core.Finance;
using Furvo.Persistence;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Furvo.Tests;

public sealed partial class JournalTests
{
    [Fact]
    public void AuditRejectsAccountBalanceOverflowBeforePersistingAnotherEntry()
    {
        Open();
        var first = Charge() with { Lines = [new(JournalAccount.Assets, EuroAmount.FromCents(long.MaxValue), default),
            new(JournalAccount.Payables, default, EuroAmount.FromCents(long.MaxValue))] };
        Write((c, t) => SqliteJournal.Post(c, t, _career, first));
        var next = Charge() with { Lines = [new(JournalAccount.Assets, EuroAmount.FromCents(1), default),
            new(JournalAccount.Payables, default, EuroAmount.FromCents(1))] };
        Write((c, t) => Assert.Throws<OverflowException>(() => SqliteJournal.Post(c, t, _career, next)));
        using var connection = Connect();
        using var transaction = connection.BeginTransaction();
        Assert.Equal(long.MaxValue, SqliteJournal.Balance(connection, transaction, _career, _club, JournalAccount.Assets, Cutoff).Cents);
        Assert.Equal(2, SqliteJournal.Movements(connection, transaction, _career, _club).Count);
    }

    [Fact]
    public void AuditRejectsDuplicateJsonKeysInsteadOfPersistingAmbiguousAmounts()
    {
        Open();
        Write((c, t) => Assert.Throws<SqliteException>(() =>
        {
            var id = Guid.NewGuid();
            var request = new FinancialOperationRequest(id, _career, "audit-raw-v1", id.ToString(), "duplicate-debit");
            SqliteFinancialOperations.Execute(c, t, request, (connection, transaction) =>
            {
                Sql(connection, transaction,
                    "INSERT INTO journal_entries(id,club_id,economic_date,operation_id,reference,opening,lines_json) VALUES($id,$club,'2026-07-01',$operation,'ambiguo',0,$lines)",
                    ("$id", Guid.NewGuid().ToString()), ("$club", _club.ToString()), ("$operation", id.ToString()),
                    ("$lines", "[{\"Account\":6,\"Debit\":1,\"Debit\":2,\"Credit\":0},{\"Account\":4,\"Debit\":0,\"Credit\":1}]"));
                return "{}";
            });
        }));
        Assert.True(Reconcile().Balanced);
    }

    [Fact]
    public void AuditRejectsAFictitiousOpeningAfterAZeroCashOpening()
    {
        Write((c, t) => Sql(c, t, "UPDATE club_economics SET cash_cents=0 WHERE club_id=$club", ("$club", _club.ToString())));
        var opening = Guid.NewGuid();
        Open(opening);
        Write((c, t) => SqliteJournal.Post(c, t, _career, Charge() with
        { Lines = [new(JournalAccount.Cash, EuroAmount.FromCents(10), default), new(JournalAccount.Income, default, EuroAmount.FromCents(10))] }));
        Write((c, t) => Assert.Throws<SqliteException>(() => Sql(c, t,
            "INSERT INTO journal_entries(id,club_id,economic_date,operation_id,reference,opening,lines_json) VALUES($id,$club,'2026-07-01',$operation,'apertura falsa',1,'[{\"Account\":1,\"Debit\":0,\"Credit\":1},{\"Account\":2,\"Debit\":1,\"Credit\":0}]')",
            ("$id", Guid.NewGuid().ToString()), ("$club", _club.ToString()), ("$operation", opening.ToString()))));
        Assert.Equal(10, Reconcile().JournalCash.Cents);
    }

    [Fact]
    public void AuditDetectsAMissingProjectionTriggerBeforeAcceptingAnEntry()
    {
        Open();
        Write((c, t) => Sql(c, t, "DROP TRIGGER journal_update_cash"));
        Write((c, t) => Assert.Throws<InvalidDataException>(() => SqliteJournal.Post(c, t, _career, Charge())));
    }

    [Fact]
    public async Task AuditConcurrentRetriesRecordTheSameFactOnce()
    {
        Open();
        var entry = Charge();
        var attempts = Enumerable.Range(0, 2).Select(_ => Task.Run(() =>
        {
            using var connection = Connect();
            using var transaction = connection.BeginTransaction(deferred: false);
            var result = SqliteJournal.Post(connection, transaction, _career, entry);
            transaction.Commit();
            return result;
        }));
        var results = await Task.WhenAll(attempts);
        Assert.Single(results, result => result.Replayed);
        Assert.Single(results, result => !result.Replayed);
        Assert.Equal(9877, Reconcile().JournalCash.Cents);
    }

    [Fact]
    public void AuditReplayAfterReopeningPreservesIdentityAndRejectsChangedTokens()
    {
        Open();
        var entry = Charge();
        Write((c, t) => Assert.False(SqliteJournal.Post(c, t, _career, entry).Replayed));
        Write((c, t) =>
        {
            Assert.True(SqliteJournal.Post(c, t, _career, entry).Replayed);
            Assert.Throws<InvalidOperationException>(() => SqliteJournal.Post(c, t, _career, entry with { OperationId = Guid.NewGuid() }));
            Assert.Throws<InvalidOperationException>(() => SqliteJournal.Post(c, t, _career, entry with { Id = Guid.NewGuid() }));
        });
        Assert.Equal(9877, Reconcile().JournalCash.Cents);
    }

    [Fact]
    public void AuditRejectsPartialAndDoubleReversals()
    {
        Open();
        var entry = Charge();
        Clock(Cutoff.AddDays(1));
        var reversal = entry.Reverse(Guid.NewGuid(), Guid.NewGuid(), Cutoff.AddDays(1), "Contraasiento auditado");
        Write((c, t) =>
        {
            SqliteJournal.Post(c, t, _career, entry);
            var partial = reversal with { Lines = [new(JournalAccount.Expense, default, EuroAmount.FromCents(10)),
                new(JournalAccount.Cash, EuroAmount.FromCents(10), default)] };
            Assert.Throws<InvalidDataException>(() => SqliteJournal.Post(c, t, _career, partial));
            Assert.Throws<InvalidDataException>(() => SqliteJournal.Post(c, t, _career, reversal with { Reverses = Guid.NewGuid() }));
            SqliteJournal.Post(c, t, _career, reversal);
            Assert.Throws<SqliteException>(() => SqliteJournal.Post(c, t, _career, reversal with { Id = Guid.NewGuid(), OperationId = Guid.NewGuid() }));
        });
        Assert.Equal(10000, Reconcile().JournalCash.Cents);
    }

    [Fact]
    public void AuditCashOverflowOrInsufficientFundsLeaveNoNewEntry()
    {
        Open();
        var excessiveInflow = Charge() with { Lines = [new(JournalAccount.Cash, EuroAmount.FromCents(long.MaxValue), default),
            new(JournalAccount.Income, default, EuroAmount.FromCents(long.MaxValue))] };
        Write((c, t) =>
        {
            Assert.Throws<OverflowException>(() => SqliteJournal.Post(c, t, _career, excessiveInflow));
            Assert.Throws<SqliteException>(() => SqliteJournal.Post(c, t, _career, Charge(10001)));
            Assert.Single(SqliteJournal.Movements(c, t, _career, _club));
        });
        Assert.True(Reconcile().Balanced);
        Assert.Equal(10000, Reconcile().JournalCash.Cents);
    }

    private void RestoreVersionOneSchema()
    {
        // Reproduce exactamente el trigger y metadatos del formato publicado, solo en esta copia.
        using var resource = typeof(SqliteJournal).Assembly.GetManifestResourceStream("Furvo.Persistence.Migrations.Journal001_Initial.sql")!;
        using var reader = new StreamReader(resource);
        var sql = reader.ReadToEnd();
        var start = sql.IndexOf("CREATE TRIGGER journal_valid_entry", StringComparison.Ordinal);
        var end = sql.IndexOf("CREATE TRIGGER journal_update_cash", StringComparison.Ordinal);
        var trigger = sql[start..end];
        Write((c, t) => Sql(c, t, "DROP TRIGGER journal_valid_entry; " + trigger +
            "DROP TABLE journal_schema; CREATE TABLE journal_schema(singleton INTEGER PRIMARY KEY CHECK(singleton=1),version INTEGER NOT NULL CHECK(version=1)); INSERT INTO journal_schema VALUES(1,1)"));
    }

    [Fact]
    public void AuditVersionOneUpgradePreservesPostedDocumentsAndRequiresExplicitMigration()
    {
        Open();
        Write((c, t) => SqliteJournal.Post(c, t, _career, Charge()));
        RestoreVersionOneSchema();
        using var connection = Connect();
        using var transaction = connection.BeginTransaction();
        var before = System.Text.Json.JsonSerializer.Serialize(SqliteJournal.Movements(connection, transaction, _career, _club));
        Assert.Throws<InvalidOperationException>(() => SqliteJournal.Post(connection, transaction, _career, Charge()));
        SqliteJournal.Migrate(connection, transaction, _career);
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(SqliteJournal.Movements(connection, transaction, _career, _club)));
        Assert.True(SqliteJournal.Reconcile(connection, transaction, _career, _club)!.Balanced);
        transaction.Commit();
        Assert.Equal(9877, Reconcile().JournalCash.Cents);
    }

    [Fact]
    public void AuditVersionOneMigrationRollbackPreservesTheOldFile()
    {
        Open();
        RestoreVersionOneSchema();
        var hash = System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(FilePath));
        using (var connection = Connect())
        using (var transaction = connection.BeginTransaction())
        {
            SqliteJournal.Migrate(connection, transaction, _career);
            transaction.Rollback();
        }
        Assert.Equal(hash, System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(FilePath)));
    }

    [Fact]
    public void AuditInvalidVersionOneHistoryIsRejectedWithoutRewritingIt()
    {
        Open();
        RestoreVersionOneSchema();
        Write((c, t) =>
        {
            var id = Guid.NewGuid();
            SqliteFinancialOperations.Execute(c, t, new(id, _career, "audit-old-v1", id.ToString(), "ambiguous"), (connection, transaction) =>
            {
                Sql(connection, transaction,
                    "INSERT INTO journal_entries(id,club_id,economic_date,operation_id,reference,opening,lines_json) VALUES($id,$club,'2026-07-01',$operation,'ambiguo',0,'[{\"Account\":6,\"Debit\":1,\"Debit\":2,\"Credit\":0},{\"Account\":4,\"Debit\":0,\"Credit\":1}]')",
                    ("$id", Guid.NewGuid().ToString()), ("$club", _club.ToString()), ("$operation", id.ToString()));
                return "{}";
            });
        });
        var hash = System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(FilePath));
        Write((c, t) => Assert.Throws<InvalidDataException>(() => SqliteJournal.Migrate(c, t, _career)));
        Assert.Equal(hash, System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(FilePath)));
    }
}
