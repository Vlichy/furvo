namespace Furvo.Core.Domain;

/// <summary>Reglas del dominio compartidas por las entidades; no dependen de SQLite ni del motor gráfico.</summary>
internal static class DomainRules
{
    /// <summary>Rechaza también los identificadores default, que pueden eludir el constructor de un struct.</summary>
    public static T Id<T>(T id, Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("Identidad vacía.", nameof(id));
        return id;
    }

    /// <summary>Impide nombres y datos descriptivos vacíos sin normalizarlos ni alterar lo aportado.</summary>
    public static string Text(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
    }

    /// <summary>Conserva los desconocidos históricos como null; si existe texto, exige contenido.</summary>
    public static string? OptionalText(string? value) => value is null ? null : Text(value);

    /// <summary>La escala 1–20 se aplica a cualquier atributo, incluidos los ocultos; null representa desconocido.</summary>
    public static int? PlayerRating(int? value)
    {
        if (value is < 1 or > 20) throw new ArgumentOutOfRangeException(nameof(value), "Escala de 1 a 20.");
        return value;
    }

    /// <summary>Aplica la escala institucional 1–5, independiente de la calidad de los jugadores.</summary>
    public static int? ClubReputation(int? value)
    {
        if (value is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(value), "Reputación de 1 a 5.");
        return value;
    }

    /// <summary>Admite un intervalo histórico desconocido o dos fechas ordenadas; nunca un extremo aislado.</summary>
    public static void Dates(DateOnly? start, DateOnly? end)
    {
        if (start.HasValue != end.HasValue || start > end)
            throw new ArgumentException("El intervalo debe estar completo y ordenado.");
    }
}
