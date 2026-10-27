namespace Furvo.Core.Domain;

/// <summary>Identidad tipada del competición; evita intercambiar GUID de entidades distintas.</summary>
public readonly record struct CompetitionId
{
    public Guid Value { get; }

    /// <summary>Reconstruye una identidad persistida y rechaza el GUID vacío; no genera otra al cargar.</summary>
    public CompetitionId(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("Identidad vacía.", nameof(value));
        Value = value;
    }

    /// <summary>Crea una identidad solo para una entidad nueva; los datos iniciales utilizan GUID fijos.</summary>
    public static CompetitionId New() => new(Guid.NewGuid());
    /// <summary>Usa el formato canónico D para que las claves SQLite se comparen de forma estable.</summary>
    public override string ToString() => Value.ToString("D");
}
