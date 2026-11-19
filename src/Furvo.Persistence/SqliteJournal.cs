using System.Globalization;
using Furvo.Core.Domain;
using Furvo.Core.Finance;
using Microsoft.Data.Sqlite;

namespace Furvo.Persistence;

/// <summary>Diario de observación explícito. Participa en la transacción del consumidor y nunca escribe la caja antigua.</summary>
public static class SqliteJournal
{
    private sealed record StoredLine(int Account, long Debit, long Credit);
    private sealed record Scope(DateOnly Cutoff, SeasonId Season, long Opening, long Cash);

    private static bool HasSchema(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = WorldDatabase.Command(connection, transaction,
            "SELECT count(*) FROM sqlite_master WHERE name IN ('journal_schema','journal_accounts','journal_scopes','journal_entries','journal_lines')");
        var count = Convert.ToInt32(command.ExecuteScalar());
        if (count == 0) return false;
        if (count != 5) throw new InvalidDataException("Extensión de diario incompleta.");
        if (!SqliteFinancialOperations.HasSchema(connection, transaction))
            throw new InvalidDataException("El diario requiere la extensión de operaciones completa.");
        command.CommandText = "SELECT version FROM journal_schema WHERE singleton=1";
        if (Convert.ToInt32(command.ExecuteScalar()) != 1)
            throw new NotSupportedException("Versión de diario no compatible.");
        return true;
    }

    public static void Migrate(SqliteConnection connection, SqliteTransaction transaction, SaveId career)
    {
        SqliteFinancialOperations.Migrate(connection, transaction, career);
        if (!HasSchema(connection, transaction)) CareerJson.Migrate(connection, transaction, "Journal001_Initial.sql");
    }

    private static Scope? ReadScope(SqliteConnection connection, SqliteTransaction transaction, ClubId club)
    {
        if (!HasSchema(connection, transaction)) return null;
        using var command = WorldDatabase.Command(connection, transaction,
            "SELECT cutoff,source_season_id,opening_cents,cash_cents FROM journal_scopes WHERE club_id=$club", ("$club", club.ToString()));
        using var reader = command.ExecuteReader();
        return reader.Read() ? new Scope(DateOnly.ParseExact(reader.GetString(0), "yyyy-MM-dd"),
            new SeasonId(Guid.Parse(reader.GetString(1))), reader.GetInt64(2), reader.GetInt64(3)) : null;
    }

    public static FinancialOperationResult Open(SqliteConnection connection, SqliteTransaction transaction,
        SaveId career, ClubId club, SeasonId sourceSeason, DateOnly cutoff, Guid operation)
    {
        if (club.Value == Guid.Empty || sourceSeason.Value == Guid.Empty || cutoff == default)
            throw new ArgumentException("La apertura requiere club, ejercicio de origen y fecha de corte.");
        var request = new FinancialOperationRequest(operation, career, "journal-opening-v1", club.ToString(),
            CareerJson.Write(new { Club = club, SourceSeason = sourceSeason, Cutoff = cutoff }));
        return SqliteFinancialOperations.Execute(connection, transaction, request, (c, t) =>
        {
            if (!HasSchema(c, t)) throw new InvalidOperationException("Instala explícitamente el diario antes de abrirlo.");
            if (ReadScope(c, t, club) is not null) throw new InvalidOperationException("El club ya tiene un corte de apertura.");
            var source = SqliteEconomyStore.ReadResources(c, t).SingleOrDefault(row => row.ClubId == club && row.SeasonId == sourceSeason);
            if (source?.Cash is not { } cash || source.Currency != "EUR")
                throw new InvalidOperationException("No existe caja EUR acreditada para la apertura solicitada.");
            var amount = EuroAmount.FromEuros(cash);
            using var scope = WorldDatabase.Command(c, t,
                "INSERT INTO journal_scopes VALUES($club,$date,$season,$opening,0,$operation,$entry)",
                ("$club", club.ToString()), ("$date", Date(cutoff)), ("$season", sourceSeason.ToString()),
                ("$opening", amount.Cents), ("$operation", operation.ToString("D")),
                ("$entry", amount.Cents == 0 ? null : operation.ToString("D")));
            scope.ExecuteNonQuery();
            // Caja cero abre el ámbito sin un asiento vacío ni líneas ficticias.
            if (amount.Cents != 0)
                Insert(c, t, new JournalEntry(operation, club, cutoff, operation, "Apertura técnica de caja acreditada",
                    [new(JournalAccount.Cash, amount, default), new(JournalAccount.OpeningBalance, default, amount)]), true);
            return CareerJson.Write(new { Club = club, Cutoff = cutoff, OpeningCents = amount.Cents });
        });
    }

    public static FinancialOperationResult Post(SqliteConnection connection, SqliteTransaction transaction,
        SaveId career, JournalEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        entry.Validate();
        var frozen = entry with { Lines = entry.Lines.ToArray() };
        var request = new FinancialOperationRequest(frozen.OperationId, career, "journal-post-v1", frozen.Id.ToString("D"), CareerJson.Write(frozen));
        return SqliteFinancialOperations.Execute(connection, transaction, request, (c, t) =>
        {
            var scope = ReadScope(c, t, frozen.Club) ?? throw new InvalidOperationException("El club carece de apertura contable.");
            if (!Reconcile(c, t, career, frozen.Club)!.Balanced)
                throw new InvalidDataException("La proyección de caja no concuerda con el diario.");
            if (frozen.Date < scope.Cutoff) throw new InvalidDataException("El asiento precede al corte de apertura.");
            if (frozen.Reverses is { } originalId)
            {
                var original = Movements(c, t, career, frozen.Club).SingleOrDefault(row => row.Entry.Id == originalId);
                if (original is null || original.Opening || original.Entry.Date > frozen.Date ||
                    !original.Entry.Lines.Select(line => new JournalLine(line.Account, line.Credit, line.Debit)).SequenceEqual(frozen.Lines))
                    throw new InvalidDataException("El contraasiento no invierte un asiento compatible del mismo club.");
            }
            Insert(c, t, frozen, false);
            return CareerJson.Write(new { EntryId = frozen.Id });
        });
    }

    private static void Insert(SqliteConnection connection, SqliteTransaction transaction, JournalEntry entry, bool opening)
    {
        entry.Validate();
        var lines = entry.Lines.Select(line => new StoredLine((int)line.Account, line.Debit.Cents, line.Credit.Cents)).ToArray();
        using var command = WorldDatabase.Command(connection, transaction,
            "INSERT INTO journal_entries(id,club_id,economic_date,operation_id,reference,opening,reverses,lines_json) VALUES($id,$club,$date,$operation,$reference,$opening,$reverses,$lines)",
            ("$id", entry.Id.ToString("D")), ("$club", entry.Club.ToString()), ("$date", Date(entry.Date)),
            ("$operation", entry.OperationId.ToString("D")), ("$reference", entry.Reference), ("$opening", opening ? 1 : 0),
            ("$reverses", entry.Reverses?.ToString("D")), ("$lines", CareerJson.Write(lines)));
        command.ExecuteNonQuery();
    }

    public static IReadOnlyList<JournalMovement> Movements(SqliteConnection connection, SqliteTransaction transaction,
        SaveId career, ClubId club, DateOnly? through = null)
    {
        SqliteFinancialOperations.CheckCareer(connection, transaction, career);
        if (!HasSchema(connection, transaction)) return [];
        using var command = WorldDatabase.Command(connection, transaction,
            "SELECT sequence,opening,id,economic_date,operation_id,reference,reverses,lines_json FROM journal_entries WHERE club_id=$club AND ($through IS NULL OR economic_date<=$through) ORDER BY economic_date,sequence",
            ("$club", club.ToString()), ("$through", through is { } date ? Date(date) : null));
        using var reader = command.ExecuteReader();
        var result = new List<JournalMovement>();
        while (reader.Read())
        {
            var lines = CareerJson.Read<StoredLine[]>(reader.GetString(7)).Select(line => new JournalLine((JournalAccount)line.Account,
                EuroAmount.FromCents(line.Debit), EuroAmount.FromCents(line.Credit))).ToArray();
            var entry = new JournalEntry(Guid.Parse(reader.GetString(2)), club, DateOnly.ParseExact(reader.GetString(3), "yyyy-MM-dd"),
                Guid.Parse(reader.GetString(4)), reader.GetString(5), lines, reader.IsDBNull(6) ? null : Guid.Parse(reader.GetString(6)));
            entry.Validate();
            result.Add(new JournalMovement(reader.GetInt64(0), reader.GetInt32(1) == 1, entry));
        }
        return result;
    }

    /// <summary>Saldo Debe menos Haber; resultado, patrimonio y otras cuentas pueden ser negativos.</summary>
    public static EuroAmount Balance(SqliteConnection connection, SqliteTransaction transaction, SaveId career,
        ClubId club, JournalAccount account, DateOnly through)
    {
        if (!Enum.IsDefined(account)) throw new ArgumentOutOfRangeException(nameof(account));
        _ = ScopeForDate(connection, transaction, career, club, through);
        decimal cents = 0;
        foreach (var line in Movements(connection, transaction, career, club, through).SelectMany(row => row.Entry.Lines).Where(line => line.Account == account))
            cents = checked(cents + line.Debit.Cents - line.Credit.Cents);
        return EuroAmount.FromCents(checked((long)cents), MoneySign.Signed);
    }

    private static Scope ScopeForDate(SqliteConnection connection, SqliteTransaction transaction, SaveId career, ClubId club, DateOnly date)
    {
        SqliteFinancialOperations.CheckCareer(connection, transaction, career);
        var scope = ReadScope(connection, transaction, club) ?? throw new InvalidOperationException("El club carece de apertura contable.");
        if (date < scope.Cutoff) throw new InvalidOperationException("No hay historial reconstruido anterior al corte.");
        return scope;
    }

    public static CashReconciliation? Reconcile(SqliteConnection connection, SqliteTransaction transaction, SaveId career, ClubId club)
    {
        SqliteFinancialOperations.CheckCareer(connection, transaction, career);
        var scope = ReadScope(connection, transaction, club);
        if (scope is null) return null;
        var calculated = Balance(connection, transaction, career, club, JournalAccount.Cash, DateOnly.MaxValue);
        var legacy = SqliteEconomyStore.ReadResources(connection, transaction).SingleOrDefault(row => row.ClubId == club && row.SeasonId == scope.Season)?.Cash;
        return new CashReconciliation(club, scope.Cutoff, EuroAmount.FromCents(scope.Opening), calculated,
            EuroAmount.FromCents(scope.Cash), legacy is { } value ? EuroAmount.FromEuros(value) : null);
    }

    private static string Date(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
