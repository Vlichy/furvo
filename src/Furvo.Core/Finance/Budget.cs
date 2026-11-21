using Furvo.Core.Domain;

namespace Furvo.Core.Finance;

public enum FinancialEvidenceKind { Verified, Derived, Estimated }
public sealed record BudgetSource(string Reference, string Version, DateOnly ReferenceDate,
    FinancialEvidenceKind Kind, string Method)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Reference) || string.IsNullOrWhiteSpace(Version) || ReferenceDate == default ||
            !Enum.IsDefined(Kind) || (Kind != FinancialEvidenceKind.Verified && string.IsNullOrWhiteSpace(Method)))
            throw new InvalidDataException("El presupuesto necesita procedencia, versión y método para datos derivados o estimados.");
    }
}

/// <summary>Partida autorizable de un club. El periodo financiero no activa temporadas del juego.</summary>
public sealed record BudgetDefinition(Guid Id, ClubId Club, string Category, string Basis,
    DateOnly From, DateOnly Until, string Currency, BudgetSource Source)
{
    public void Validate()
    {
        if (Id == Guid.Empty || Club.Value == Guid.Empty || string.IsNullOrWhiteSpace(Category) ||
            string.IsNullOrWhiteSpace(Basis) || From == default || Until < From || Currency != "EUR")
            throw new InvalidDataException("Partida, club, categoría, base, periodo o moneda no válidos.");
        ArgumentNullException.ThrowIfNull(Source); Source.Validate();
    }
}

public enum BudgetAction { Authorize, Reserve, Commit, Confirm, Amend, Release, Execute }
public enum BudgetAllocationKind { Reservation, Commitment }

// Los campos persistibles expresan céntimos; EuroAmount sigue siendo la única política monetaria.
public sealed record BudgetAllocation(Guid Id, string Origin, BudgetAllocationKind Kind,
    long AmountCents, long ReleasedCents = 0, long ExecutedCents = 0)
{
    public long NetCents => checked(AmountCents - ReleasedCents);
    public void Validate()
    {
        if (Id == Guid.Empty || string.IsNullOrWhiteSpace(Origin) || !Enum.IsDefined(Kind))
            throw new InvalidDataException("Asignación sin identidad económica.");
        EuroAmount.FromCents(AmountCents); EuroAmount.FromCents(ReleasedCents); EuroAmount.FromCents(ExecutedCents);
        if (ReleasedCents > AmountCents || ExecutedCents > NetCents ||
            (Kind == BudgetAllocationKind.Reservation && ExecutedCents != 0))
            throw new InvalidDataException("Liberación o ejecución incompatible con la asignación.");
    }
}

public sealed record BudgetChange(Guid Budget, int ExpectedRevision, BudgetAction Action,
    Guid Allocation, string Origin, EuroAmount Amount);

public sealed record BudgetSnapshot(BudgetDefinition Definition, int Revision, long AuthorizedCents,
    IReadOnlyList<BudgetAllocation> Allocations)
{
    public long ReservedCents => Total(BudgetAllocationKind.Reservation);
    public long CommittedCents => Total(BudgetAllocationKind.Commitment);
    public long ExecutedCents => checked((long)Allocations.Sum(a => (decimal)a.ExecutedCents));
    public EuroAmount Available => EuroAmount.FromCents(checked((long)((decimal)AuthorizedCents - ReservedCents - CommittedCents)),MoneySign.Signed);
    private long Total(BudgetAllocationKind kind) => checked((long)Allocations.Where(a=>a.Kind==kind).Sum(a=>(decimal)a.NetCents));
    public void Validate()
    {
        Definition.Validate(); EuroAmount.FromCents(AuthorizedCents);
        if (Revision < 1 || Allocations is null) throw new InvalidDataException("Revisión presupuestaria inválida.");
        foreach (var item in Allocations) item.Validate();
        if (Allocations.Select(a=>a.Id).Distinct().Count()!=Allocations.Count ||
            Allocations.Select(a=>a.Origin).Distinct(StringComparer.Ordinal).Count()!=Allocations.Count)
            throw new InvalidDataException("Identidad u origen presupuestario duplicado.");
        _=Available; _=ExecutedCents;
    }

    public BudgetSnapshot Apply(BudgetChange change)
    {
        Validate();
        if (change.Budget != Definition.Id || change.ExpectedRevision != Revision || !Enum.IsDefined(change.Action))
            throw new InvalidOperationException("El presupuesto cambió o la operación no es válida.");
        EuroAmount.FromCents(change.Amount.Cents);
        var rows=Allocations.ToList(); var authorized=AuthorizedCents;
        var index=rows.FindIndex(a=>a.Id==change.Allocation);
        if (change.Action==BudgetAction.Authorize)
        {
            if (change.Allocation!=Guid.Empty || !string.IsNullOrEmpty(change.Origin)) throw new InvalidDataException("La autorización no identifica una asignación.");
            authorized=change.Amount.Cents;
        }
        else if (change.Action is BudgetAction.Reserve or BudgetAction.Commit)
        {
            if (change.Amount.Cents==0 || index>=0 || rows.Any(a=>a.Origin==change.Origin))
                throw new InvalidOperationException("Asignación duplicada o importe vacío.");
            rows.Add(new(change.Allocation,change.Origin,change.Action==BudgetAction.Reserve ? BudgetAllocationKind.Reservation : BudgetAllocationKind.Commitment,change.Amount.Cents));
        }
        else
        {
            if(index<0 || rows[index].Origin!=change.Origin) throw new InvalidOperationException("No existe esa asignación de origen.");
            var item=rows[index];
            if(item.NetCents==0) throw new InvalidOperationException("La asignación está cancelada.");
            switch(change.Action)
            {
                case BudgetAction.Confirm:
                    if(item.Kind!=BudgetAllocationKind.Reservation || change.Amount.Cents!=item.NetCents)
                        throw new InvalidOperationException("Solo se confirma el importe reservado vigente.");
                    item=item with{Kind=BudgetAllocationKind.Commitment}; break;
                case BudgetAction.Amend:
                    if(change.Amount.Cents<item.ExecutedCents || change.Amount.Cents==0)
                        throw new InvalidOperationException("La revisión debe conservar lo ejecutado; para cancelar utiliza liberación.");
                    item=item with{AmountCents=checked(change.Amount.Cents+item.ReleasedCents)}; break;
                case BudgetAction.Release:
                    if(change.Amount.Cents==0 || change.Amount.Cents>item.NetCents-item.ExecutedCents)
                        throw new InvalidOperationException("Solo se libera importe no ejecutado, una vez.");
                    item=item with{ReleasedCents=checked(item.ReleasedCents+change.Amount.Cents)}; break;
                case BudgetAction.Execute:
                    if(item.Kind!=BudgetAllocationKind.Commitment || change.Amount.Cents==0 || change.Amount.Cents>item.NetCents-item.ExecutedCents)
                        throw new InvalidOperationException("Ejecución incompatible con el compromiso vigente.");
                    item=item with{ExecutedCents=checked(item.ExecutedCents+change.Amount.Cents)}; break;
                default: throw new InvalidDataException("Operación desconocida.");
            }
            rows[index]=item;
        }
        var next=this with{Revision=checked(Revision+1),AuthorizedCents=authorized,Allocations=rows.AsReadOnly()};
        next.Validate();
        // Una reducción autorizada puede revelar déficit; no se borran compromisos para ocultarlo.
        if(change.Action!=BudgetAction.Authorize && next.Available.Cents<0 && next.Available.Cents<Available.Cents)
            throw new InvalidOperationException("Presupuesto insuficiente. Se requiere una revisión autorizada previa.");
        return next;
    }
}

/// <summary>Puerto de permisos: no se confía en etiquetas de actor ni en validaciones de interfaz.</summary>
public interface IBudgetAuthorization
{
    bool Allows(SaveId career, ClubId club, string actor, string authority, BudgetAction action);
}
