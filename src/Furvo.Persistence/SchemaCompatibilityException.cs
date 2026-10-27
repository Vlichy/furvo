namespace Furvo.Persistence;

/// <summary>Identifica un valor histórico incompatible para que pueda revisarse sin convertirlo automáticamente.</summary>
public sealed record IncompatibleScaleValue(string EntityId, string Field, string StoredValue, string AllowedRange);

/// <summary>Error de migración informativo; impide publicar una copia con puntuaciones fuera de las escalas aprobadas.</summary>
public sealed class SchemaCompatibilityException : InvalidOperationException
{
    public IReadOnlyList<IncompatibleScaleValue> Values { get; }

    /// <summary>Conserva todos los conflictos detectados y sus valores originales, en lugar de corregir solo el primero.</summary>
    public SchemaCompatibilityException(IEnumerable<IncompatibleScaleValue> values) : this(values.ToArray()) { }

    private SchemaCompatibilityException(IncompatibleScaleValue[] values)
        : base("Migración incompatible; no se convierten valores automáticamente. " +
            string.Join("; ", values.Select(x => $"{x.EntityId}: {x.Field}={x.StoredValue}, rango {x.AllowedRange}")))
    {
        Values = Array.AsReadOnly(values);
    }
}
