using System.Security.Cryptography;
using Furvo.Core.Domain;
using Furvo.Core.Finance;
using Furvo.Persistence;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Furvo.Tests;

/// <summary>Una copia aislada por prueba. Nunca modifica publicaciones o partidas personales.</summary>
public sealed partial class JournalTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "FurvoJournalTests", Guid.NewGuid().ToString("N"));
    private readonly SaveId _career = SaveId.New();
    private readonly ClubId _club;
    private readonly SeasonId _season;
    private static readonly DateOnly Cutoff = new(2026, 7, 1);
    private string FilePath => Path.Combine(_root, "journal.db");

    public JournalTests()
    {
        Directory.CreateDirectory(_root);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "published-v5.db"), FilePath);
        using (var connection = Connect())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "UPDATE world_info SET kind='save'; INSERT INTO save_metadata VALUES(1,$save); CREATE TABLE journal_probe(value INTEGER NOT NULL); INSERT INTO journal_probe VALUES(0)";
            command.Parameters.AddWithValue("$save", _career.ToString());
            command.ExecuteNonQuery();
        }
        var source = SqliteEconomyStore.ReadResources(FilePath)[0];
        _club = source.ClubId;
        _season = source.SeasonId;
        // Importe sintético pequeño para verificar céntimos y falta de fondos.
        Write((connection, transaction) => Sql(connection, transaction,
            "UPDATE club_economics SET cash_cents=10000 WHERE club_id=$club", ("$club", _club.ToString())));
    }

    private SqliteConnection Connect()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        { DataSource = FilePath, Mode = SqliteOpenMode.ReadWrite, ForeignKeys = true, Pooling = false }.ToString());
        connection.Open();
        return connection;
    }

    private static void Sql(SqliteConnection connection, SqliteTransaction transaction, string text,
        params (string Key, object Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = text;
        foreach (var (key, value) in parameters) command.Parameters.AddWithValue(key, value);
        command.ExecuteNonQuery();
    }

    private void Write(Action<SqliteConnection, SqliteTransaction> action)
    {
        using var connection = Connect();
        using var transaction = connection.BeginTransaction();
        action(connection, transaction);
        transaction.Commit();
    }

    private void Open(Guid? operation = null) => Write((c, t) =>
    {
        SqliteJournal.Migrate(c, t, _career);
        SqliteJournal.Open(c, t, _career, _club, _season, Cutoff, operation ?? Guid.NewGuid());
    });

    private JournalEntry Charge(long cents = 123, DateOnly? date = null) => new(Guid.NewGuid(), _club, date ?? Cutoff,
        Guid.NewGuid(), "Apunte sintético de prueba", [new(JournalAccount.Expense, EuroAmount.FromCents(cents), default),
            new(JournalAccount.Cash, default, EuroAmount.FromCents(cents))]);

    private CashReconciliation Reconcile()
    {
        using var connection = Connect();
        using var transaction = connection.BeginTransaction(deferred: true);
        return SqliteJournal.Reconcile(connection, transaction, _career, _club)!;
    }

    [Fact]
    public void OpeningUsesExistingCashWithoutIncomeAndReplaysAfterReopening()
    {
        var operation = Guid.NewGuid();
        Open(operation);
        var state = Reconcile();
        Assert.True(state.Balanced);
        Assert.Equal(10000, state.JournalCash.Cents);
        Assert.Equal(state.Opening, state.LegacyCash);
        using var connection = Connect();
        using var transaction = connection.BeginTransaction();
        Assert.True(SqliteJournal.Open(connection, transaction, _career, _club, _season, Cutoff, operation).Replayed);
        Assert.Throws<InvalidOperationException>(() => SqliteJournal.Open(connection, transaction, _career, _club, _season, Cutoff, Guid.NewGuid()));
        Assert.Throws<InvalidOperationException>(() => SqliteJournal.Open(connection, transaction, _career, _club, _season, Cutoff.AddDays(1), operation));
        var movement = Assert.Single(SqliteJournal.Movements(connection, transaction, _career, _club));
        Assert.True(movement.Opening);
        Assert.Equal(JournalAccount.OpeningBalance, movement.Entry.Lines[1].Account);
        Assert.Equal(0, SqliteJournal.Balance(connection, transaction, _career, _club, JournalAccount.Income, Cutoff).Cents);
    }

    [Fact]
    public void ZeroCashOpensWithoutAnEmptyOrFictitiousEntry()
    {
        Write((c, t) => Sql(c, t, "UPDATE club_economics SET cash_cents=0 WHERE club_id=$club", ("$club", _club.ToString())));
        Open();
        Assert.True(Reconcile().Balanced);
        Assert.Equal(0, Reconcile().JournalCash.Cents);
        using var connection = Connect();
        using var transaction = connection.BeginTransaction();
        Assert.Empty(SqliteJournal.Movements(connection, transaction, _career, _club));
        Assert.Throws<SqliteException>(() => SqliteJournal.Post(connection, transaction, _career, Charge(1)));
    }

    [Fact]
    public void PostedChargeReconcilesAndReplayDoesNotRepeatIt()
    {
        Open();
        var entry = Charge();
        Write((c, t) =>
        {
            Assert.False(SqliteJournal.Post(c, t, _career, entry).Replayed);
            Assert.True(SqliteJournal.Post(c, t, _career, entry).Replayed);
            Assert.Throws<InvalidOperationException>(() => SqliteJournal.Post(c, t, _career, entry with { Reference = "Otros términos" }));
        });
        var state = Reconcile();
        Assert.True(state.Balanced);
        Assert.Equal(9877, state.MaterializedCash.Cents);
        Assert.Equal(10000, state.LegacyCash!.Value.Cents);
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("one")]
    [InlineData("unbalanced")]
    [InlineData("negative")]
    [InlineData("both")]
    [InlineData("account")]
    public void RejectsInvalidDomainEntries(string problem)
    {
        var lines = Charge().Lines.ToArray();
        var entry = problem switch
        {
            "empty" => Charge() with { Lines = [] },
            "one" => Charge() with { Lines = [lines[0]] },
            "unbalanced" => Charge() with { Lines = [lines[0], lines[1] with { Credit = EuroAmount.FromCents(1) }] },
            "negative" => Charge() with { Lines = [lines[0] with { Debit = EuroAmount.FromCents(-1, MoneySign.Signed) }, lines[1]] },
            "both" => Charge() with { Lines = [lines[0] with { Credit = EuroAmount.FromCents(1) }, lines[1]] },
            _ => Charge() with { Lines = [lines[0] with { Account = (JournalAccount)99 }, lines[1]] }
        };
        Assert.Throws<InvalidDataException>(() => entry.Validate());
    }

    [Fact]
    public void RejectsSumOverflowAndPreservesSignedResults()
    {
        var max = EuroAmount.FromCents(long.MaxValue);
        var entry = Charge() with { Lines = [new(JournalAccount.Assets, max, default), new(JournalAccount.Expense, max, default),
            new(JournalAccount.Payables, default, max), new(JournalAccount.FinancialDebt, default, max)] };
        Assert.Throws<OverflowException>(entry.Validate);
        Open();
        // Una pérdida devengada y patrimonio negativo son representables sin tocar caja ni contratos.
        Write((c, t) =>
        {
            SqliteJournal.Post(c, t, _career, Charge() with { Lines = [new(JournalAccount.Expense, EuroAmount.FromCents(200), default),
                new(JournalAccount.Payables, default, EuroAmount.FromCents(200))] });
            SqliteJournal.Post(c, t, _career, Charge() with { Lines = [new(JournalAccount.Equity, EuroAmount.FromCents(50), default),
                new(JournalAccount.Payables, default, EuroAmount.FromCents(50))] });
            var profit = -SqliteJournal.Balance(c, t, _career, _club, JournalAccount.Income, Cutoff).Cents
                - SqliteJournal.Balance(c, t, _career, _club, JournalAccount.Expense, Cutoff).Cents;
            Assert.Equal(-200, profit);
            Assert.Equal(-50, -SqliteJournal.Balance(c, t, _career, _club, JournalAccount.Equity, Cutoff).Cents);
        });
        Assert.Equal(10000, Reconcile().JournalCash.Cents);
    }

    [Fact]
    public void SqlRejectsMalformedPartialAndMutableEntries()
    {
        Open();
        Write((c, t) =>
        {
            Assert.Throws<SqliteException>(() => Sql(c, t, "UPDATE journal_entries SET reference='alterado'"));
            Assert.Throws<SqliteException>(() => Sql(c, t, "DELETE FROM journal_entries"));
            Assert.Throws<SqliteException>(() => Sql(c, t, "UPDATE journal_lines SET debit_cents=1"));
            foreach (var lines in new[] { "[]", "[{\"Account\":1,\"Debit\":1,\"Credit\":0}]",
                "[{\"Account\":1,\"Debit\":1,\"Credit\":0},{\"Account\":5,\"Debit\":0,\"Credit\":2}]",
                "[{\"Account\":99,\"Debit\":1,\"Credit\":0},{\"Account\":5,\"Debit\":0,\"Credit\":1}]",
                "[{\"Account\":1,\"Debit\":1.5,\"Credit\":0},{\"Account\":5,\"Debit\":0,\"Credit\":1.5}]" })
                Assert.Throws<SqliteException>(() => Sql(c, t,
                    "INSERT INTO journal_entries(id,club_id,economic_date,operation_id,reference,opening,lines_json) VALUES($id,$club,'2026-07-01',$operation,'prueba',0,$lines)",
                    ("$id", Guid.NewGuid().ToString()), ("$club", _club.ToString()), ("$operation", Guid.NewGuid().ToString()), ("$lines", lines)));
        });
        Assert.True(Reconcile().Balanced);
    }

    [Fact]
    public void ExternalLegacyChangeIsNotAnInternalJournalDiscrepancy()
    {
        Open();
        Write((c, t) => Sql(c, t, "UPDATE club_economics SET cash_cents=cash_cents-10 WHERE club_id=$club", ("$club", _club.ToString())));
        var state = Reconcile();
        Assert.True(state.Balanced);
        Assert.True(state.LegacyChanged);
        Assert.Equal(9990, state.LegacyCash!.Value.Cents);
        Assert.Equal(10000, state.JournalCash.Cents);
        // La lectura no importa ni duplica el movimiento histórico.
        Assert.Equal(state, Reconcile());
    }

    [Fact]
    public void DetectsProjectionMismatchWithoutRepairingAndBlocksNewPosting()
    {
        Open();
        Write((c, t) => Sql(c, t, "UPDATE journal_scopes SET cash_cents=9999 WHERE club_id=$club", ("$club", _club.ToString())));
        Assert.False(Reconcile().Balanced);
        Assert.Equal(10000, Reconcile().JournalCash.Cents);
        Write((c, t) => Assert.Throws<InvalidDataException>(() => SqliteJournal.Post(c, t, _career, Charge())));
        Assert.Equal(9999, Reconcile().MaterializedCash.Cents);
    }

    [Fact]
    public void OuterRollbackAfterSavepointRemovesEntryReceiptAndEffects()
    {
        Open();
        var entry = Charge();
        using (var connection = Connect())
        using (var transaction = connection.BeginTransaction())
        {
            Sql(connection, transaction, "UPDATE journal_probe SET value=1");
            SqliteJournal.Post(connection, transaction, _career, entry);
            transaction.Rollback();
        }
        Assert.Equal(10000, Reconcile().JournalCash.Cents);
        Write((c, t) => Assert.False(SqliteJournal.Post(c, t, _career, entry).Replayed));
        using var check = Connect();
        using var command = check.CreateCommand();
        command.CommandText = "SELECT value FROM journal_probe";
        Assert.Equal(0L, command.ExecuteScalar());
    }

    [Theory]
    [InlineData("before")]
    [InlineData("line")]
    [InlineData("after")]
    public void FailuresDuringPostingLeaveNoPartialDocument(string stage)
    {
        Open();
        if (stage != "before")
            Write((c, t) => Sql(c, t, stage == "line"
                ? "CREATE TRIGGER injected BEFORE INSERT ON journal_entries WHEN NEW.opening=0 BEGIN SELECT CASE WHEN json_extract(NEW.lines_json,'$[0].Debit')>0 THEN RAISE(ABORT,'Fallo tras evaluar primera línea') END; END"
                : "CREATE TRIGGER injected AFTER INSERT ON journal_entries WHEN NEW.opening=0 BEGIN SELECT RAISE(ABORT,'Fallo tras documento completo'); END"));
        var entry = Charge();
        Write((c, t) =>
        {
            if (stage == "before") Assert.Throws<InvalidDataException>(() => SqliteJournal.Post(c, t, _career, entry with { Lines = [] }));
            else Assert.Throws<SqliteException>(() => SqliteJournal.Post(c, t, _career, entry));
        });
        Assert.Equal(10000, Reconcile().JournalCash.Cents);
        using var connection = Connect();
        using var transaction = connection.BeginTransaction();
        Assert.Single(SqliteJournal.Movements(connection, transaction, _career, _club));
    }

    [Fact]
    public void CounterEntryPreservesOriginalAndRestoresCash()
    {
        Open();
        var entry = Charge();
        var reversal = entry.Reverse(Guid.NewGuid(), Guid.NewGuid(), Cutoff.AddDays(1), "Corrección de prueba");
        Write((c, t) =>
        {
            SqliteJournal.Post(c, t, _career, entry);
            SqliteJournal.Post(c, t, _career, reversal);
            Assert.Equal(3, SqliteJournal.Movements(c, t, _career, _club).Count);
        });
        Assert.Equal(10000, Reconcile().JournalCash.Cents);
    }

    [Fact]
    public void CashAndHistoryContinueAcrossFinancialYearsWithoutNewOpening()
    {
        Open();
        Write((c, t) => SqliteJournal.Post(c, t, _career, Charge(123, new DateOnly(2027, 6, 30))));
        using var connection = Connect();
        using var transaction = connection.BeginTransaction();
        Assert.Equal(9877, SqliteJournal.Balance(connection, transaction, _career, _club, JournalAccount.Cash, new(2027, 6, 30)).Cents);
        Assert.Equal(9877, SqliteJournal.Balance(connection, transaction, _career, _club, JournalAccount.Cash, new(2027, 7, 1)).Cents);
        Assert.Equal(2, SqliteJournal.Movements(connection, transaction, _career, _club).Count);
        Assert.Throws<InvalidOperationException>(() => SqliteJournal.Open(connection, transaction, _career, _club, _season, new(2027, 7, 1), Guid.NewGuid()));
    }

    [Fact]
    public void ClubsRemainIndependentAndDatesBeforeCutoffAreUnknown()
    {
        Open();
        var other = SqliteEconomyStore.ReadResources(FilePath)[1];
        Write((c, t) =>
        {
            SqliteJournal.Open(c, t, _career, other.ClubId, other.SeasonId, Cutoff, Guid.NewGuid());
            SqliteJournal.Post(c, t, _career, Charge());
            Assert.Equal(EuroAmount.FromEuros(other.Cash!.Value), SqliteJournal.Reconcile(c, t, _career, other.ClubId)!.JournalCash);
            Assert.Throws<InvalidOperationException>(() => SqliteJournal.Balance(c, t, _career, _club, JournalAccount.Cash, Cutoff.AddDays(-1)));
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AbsentExtensionReadsWithoutWritingAndMigrationIsReversible(bool operationsInstalled)
    {
        if (operationsInstalled) Write((c, t) => SqliteFinancialOperations.Migrate(c, t, _career));
        var hash = SHA256.HashData(File.ReadAllBytes(FilePath));
        using (var connection = Connect())
        using (var transaction = connection.BeginTransaction(deferred: true))
        {
            Assert.Null(SqliteJournal.Reconcile(connection, transaction, _career, _club));
            Assert.Empty(SqliteJournal.Movements(connection, transaction, _career, _club));
        }
        Assert.Equal(hash, SHA256.HashData(File.ReadAllBytes(FilePath)));
        using (var connection = Connect())
        using (var transaction = connection.BeginTransaction())
        {
            SqliteJournal.Migrate(connection, transaction, _career);
            transaction.Rollback();
        }
        Assert.Equal(hash, SHA256.HashData(File.ReadAllBytes(FilePath)));
    }

    [Fact]
    public void MissingEconomicsAndPartialExtensionsAreExplicitFailures()
    {
        Write((c, t) => Sql(c, t, "DROP TABLE club_economics; DROP TABLE economic_publication"));
        Write((c, t) =>
        {
            SqliteJournal.Migrate(c, t, _career);
            Assert.Throws<InvalidOperationException>(() => SqliteJournal.Open(c, t, _career, _club, _season, Cutoff, Guid.NewGuid()));
            Assert.Null(SqliteJournal.Reconcile(c, t, _career, _club));
            Sql(c, t, "DROP VIEW journal_lines");
            Assert.Throws<InvalidDataException>(() => SqliteJournal.Movements(c, t, _career, _club));
        });
    }

    [Fact]
    public void ConfirmedJournalHasValidSqliteReferencesAfterReopening()
    {
        Open();
        Write((c, t) => SqliteJournal.Post(c, t, _career, Charge()));
        using var connection = Connect();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check";
        Assert.Equal("ok", command.ExecuteScalar());
        command.CommandText = "PRAGMA foreign_key_check";
        using var rows = command.ExecuteReader();
        Assert.False(rows.Read());
    }

    [Fact]
    public void MissingOperationReceiptPreventsCommittingAnOtherwiseBalancedEntry()
    {
        Open();
        using (var connection = Connect())
        using (var transaction = connection.BeginTransaction())
        {
            Sql(connection, transaction,
                "INSERT INTO journal_entries(id,club_id,economic_date,operation_id,reference,opening,lines_json) VALUES($id,$club,'2026-07-01',$operation,'sin recibo',0,'[{\"Account\":6,\"Debit\":1,\"Credit\":0},{\"Account\":1,\"Debit\":0,\"Credit\":1}]')",
                ("$id", Guid.NewGuid().ToString()), ("$club", _club.ToString()), ("$operation", Guid.NewGuid().ToString()));
            Assert.Throws<SqliteException>(transaction.Commit);
        }
        Assert.Equal(10000, Reconcile().JournalCash.Cents);
    }

    [Fact]
    public void MissingDependencyIsRejectedAndBackdatingCannotRewriteCashHistory()
    {
        Open();
        Write((c, t) =>
        {
            SqliteJournal.Post(c, t, _career, Charge(1, Cutoff.AddDays(2)));
            Assert.Throws<SqliteException>(() => SqliteJournal.Post(c, t, _career, Charge(1, Cutoff.AddDays(1))));
            Sql(c, t, "DROP TABLE financial_operation_schema");
            Assert.Throws<InvalidDataException>(() => SqliteJournal.Movements(c, t, _career, _club));
        });
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}

public sealed partial class CareerCreationTests
{
    [Fact]
    public void JournalOpeningPreservesCareerStaffMarketAndEconomicSnapshots()
    {
        var profile = Profile();
        var careers = EconomicStore();
        var career = careers.Create(Request(profile, Furvo.Core.Careers.CareerStartMode.SignOffer, EconomicOffer(profile)));
        var file = careers.PathFor(career.Id);
        var before = careers.Load(career.Id);
        var resources = SqliteEconomyStore.ReadResources(file);
        var staff = SqliteStaffDirectory.Read(file, career);
        var staffCount = staff.Count;
        var market = new SqliteTransferStore(file).History().Count;
        using (var connection = Connection(file))
        using (var transaction = connection.BeginTransaction())
        {
            SqliteJournal.Migrate(connection, transaction, career.Id);
            SqliteJournal.Open(connection, transaction, career.Id, career.ClubId!.Value, career.SeasonId, career.CurrentDate, Guid.NewGuid());
            transaction.Commit();
        }
        var after = careers.Load(career.Id);
        Assert.Equal(before.Contract!.AcceptedOffer.Terms, after.Contract!.AcceptedOffer.Terms);
        Assert.Equal(before.SeasonId, after.SeasonId);
        Assert.Equal(before.CurrentDate, after.CurrentDate);
        Assert.Equal(resources, SqliteEconomyStore.ReadResources(file));
        Assert.Equal(staffCount, SqliteStaffDirectory.Read(file, after).Count);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(staff.Select(person => person.Employment)),
            System.Text.Json.JsonSerializer.Serialize(SqliteStaffDirectory.Read(file, after).Select(person => person.Employment)));
        Assert.Equal(market, new SqliteTransferStore(file).History().Count);
        Assert.False(new Furvo.Core.Careers.CareerWorkspace(after).CanAdvanceTime);
    }
}
