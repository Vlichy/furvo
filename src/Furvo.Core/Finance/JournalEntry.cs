using Furvo.Core.Domain;

namespace Furvo.Core.Finance;

public enum JournalAccount
{
    Cash = 1, OpeningBalance = 2, Receivables = 3, Payables = 4,
    Income = 5, Expense = 6, Assets = 7, FinancialDebt = 8, Equity = 9
}

public sealed record JournalLine(JournalAccount Account, EuroAmount Debit, EuroAmount Credit);

/// <summary>Documento completo. Las cantidades son positivas; los saldos derivados admiten signo.</summary>
public sealed record JournalEntry(Guid Id, ClubId Club, DateOnly Date, Guid OperationId, string Reference,
    IReadOnlyList<JournalLine> Lines, Guid? Reverses = null)
{
    public void Validate()
    {
        if (Id == Guid.Empty || Club.Value == Guid.Empty || OperationId == Guid.Empty || Date == default ||
            string.IsNullOrWhiteSpace(Reference) || Lines is null || Lines.Count < 2 || Reverses == Guid.Empty || Reverses == Id)
            throw new InvalidDataException("Asiento sin identidad, fecha, referencia o líneas completas.");
        long debit = 0, credit = 0;
        foreach (var line in Lines)
        {
            if (line is null || !Enum.IsDefined(line.Account) || line.Debit.Cents < 0 || line.Credit.Cents < 0 ||
                (line.Debit.Cents == 0) == (line.Credit.Cents == 0))
                throw new InvalidDataException("Cada apunte requiere una cuenta y un importe positivo en debe o haber.");
            debit = checked(debit + line.Debit.Cents);
            credit = checked(credit + line.Credit.Cents);
        }
        if (debit != credit) throw new InvalidDataException("El asiento no está equilibrado.");
    }

    public JournalEntry Reverse(Guid id, Guid operation, DateOnly date, string reference) =>
        new(id, Club, date, operation, reference, Lines.Select(line => new JournalLine(line.Account, line.Credit, line.Debit)).ToArray(), Id);
}

public sealed record JournalMovement(long Sequence, bool Opening, JournalEntry Entry);

/// <summary>La diferencia antigua es informativa: F1.4 aún no ha integrado sus movimientos.</summary>
public sealed record CashReconciliation(ClubId Club, DateOnly Cutoff, EuroAmount Opening,
    EuroAmount JournalCash, EuroAmount MaterializedCash, EuroAmount? LegacyCash)
{
    public bool Balanced => JournalCash == MaterializedCash;
    public bool LegacyChanged => LegacyCash is { } cash && cash != Opening;
}
