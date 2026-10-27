using System.Collections.ObjectModel;

namespace Furvo.Core.Domain;

/// <summary>Categorías estables del catálogo; Hidden señala valores excluidos de la presentación normal.</summary>
public enum AttributeCategory { Technical = 1, Mental = 2, Physical = 3, Goalkeeper = 4, Hidden = 5 }

/// <summary>Definición de un atributo, sin puntuación ni efectos de simulación. El código es su clave persistente.</summary>
public sealed record AttributeDefinition(string Code, string Name, AttributeCategory Category)
{
    public bool IsHidden => Category == AttributeCategory.Hidden;
}

// Los códigos son identidades estables de datos; los nombres visibles pueden traducirse.
/// <summary>Catálogo aprobado de 61 atributos; sus códigos no se traducen ni se regeneran al cargar.</summary>
public static class PlayerAttributeCatalog
{
    public static IReadOnlyList<AttributeDefinition> All { get; } = Array.AsReadOnly<AttributeDefinition>(
    [
        new("ball_control", "Control del balón", AttributeCategory.Technical),
        new("first_touch", "Primer toque", AttributeCategory.Technical),
        new("technique", "Técnica", AttributeCategory.Technical),
        new("dribbling", "Regate", AttributeCategory.Technical),
        new("short_passing", "Pase corto", AttributeCategory.Technical),
        new("long_passing", "Pase largo", AttributeCategory.Technical),
        new("crossing", "Centros", AttributeCategory.Technical),
        new("finishing", "Remate", AttributeCategory.Technical),
        new("long_shots", "Disparo lejano", AttributeCategory.Technical),
        new("shot_power", "Potencia de disparo", AttributeCategory.Technical),
        new("heading", "Cabeceo", AttributeCategory.Technical),
        new("tackling", "Entradas", AttributeCategory.Technical),
        new("marking", "Marcaje", AttributeCategory.Technical),
        new("penalties", "Penaltis", AttributeCategory.Technical),
        new("free_kicks", "Faltas", AttributeCategory.Technical),
        new("corners", "Córneres", AttributeCategory.Technical),
        new("throw_ins", "Saques de banda", AttributeCategory.Technical),
        new("anticipation", "Anticipación", AttributeCategory.Mental),
        new("concentration", "Concentración", AttributeCategory.Mental),
        new("decisions", "Decisiones", AttributeCategory.Mental),
        new("vision", "Visión de juego", AttributeCategory.Mental),
        new("defensive_positioning", "Colocación defensiva", AttributeCategory.Mental),
        new("off_the_ball", "Desmarques", AttributeCategory.Mental),
        new("teamwork", "Trabajo en equipo", AttributeCategory.Mental),
        new("work_rate", "Esfuerzo", AttributeCategory.Mental),
        new("determination", "Determinación", AttributeCategory.Mental),
        new("composure", "Serenidad", AttributeCategory.Mental),
        new("bravery", "Valentía", AttributeCategory.Mental),
        new("aggression", "Agresividad", AttributeCategory.Mental),
        new("leadership", "Liderazgo", AttributeCategory.Mental),
        new("creativity", "Talento / creatividad", AttributeCategory.Mental),
        new("tactical_discipline", "Disciplina táctica", AttributeCategory.Mental),
        new("tactical_understanding", "Comprensión táctica", AttributeCategory.Mental),
        new("pace", "Velocidad", AttributeCategory.Physical),
        new("acceleration", "Aceleración", AttributeCategory.Physical),
        new("stamina", "Resistencia", AttributeCategory.Physical),
        new("strength", "Fuerza", AttributeCategory.Physical),
        new("agility", "Agilidad", AttributeCategory.Physical),
        new("balance", "Equilibrio", AttributeCategory.Physical),
        new("jumping", "Salto", AttributeCategory.Physical),
        new("coordination", "Coordinación", AttributeCategory.Physical),
        new("reflexes", "Reflejos", AttributeCategory.Goalkeeper),
        new("handling", "Blocaje", AttributeCategory.Goalkeeper),
        new("punching", "Despejes", AttributeCategory.Goalkeeper),
        new("one_on_ones", "Uno contra uno", AttributeCategory.Goalkeeper),
        new("aerial_claims", "Salidas aéreas", AttributeCategory.Goalkeeper),
        new("rushing_out", "Salidas a los pies", AttributeCategory.Goalkeeper),
        new("goalkeeping_positioning", "Colocación en portería", AttributeCategory.Goalkeeper),
        new("command_of_area", "Mando del área", AttributeCategory.Goalkeeper),
        new("hand_distribution", "Saque con la mano", AttributeCategory.Goalkeeper),
        new("defensive_communication", "Comunicación defensiva", AttributeCategory.Goalkeeper),
        new("professionalism", "Profesionalidad", AttributeCategory.Hidden),
        new("ambition", "Ambición", AttributeCategory.Hidden),
        new("consistency", "Regularidad", AttributeCategory.Hidden),
        new("big_matches", "Rendimiento en grandes partidos", AttributeCategory.Hidden),
        new("adaptability", "Adaptabilidad", AttributeCategory.Hidden),
        new("injury_proneness", "Propensión a lesiones", AttributeCategory.Hidden),
        new("pressure_resistance", "Resistencia a la presión", AttributeCategory.Hidden),
        new("temperament", "Temperamento", AttributeCategory.Hidden),
        new("loyalty", "Lealtad", AttributeCategory.Hidden),
        new("learning_ability", "Capacidad de aprendizaje", AttributeCategory.Hidden),
    ]);

    private static readonly IReadOnlyDictionary<string, AttributeDefinition> ByCode =
        All.ToDictionary(x => x.Code, StringComparer.Ordinal);

    /// <summary>Rechaza códigos no registrados para evitar almacenar atributos mal escritos o sin categoría.</summary>
    public static AttributeDefinition Get(string code) => ByCode.TryGetValue(code, out var definition)
        ? definition : throw new ArgumentException("Atributo desconocido: " + code, nameof(code));
}

// No asigna valores por defecto. Ausencia significa desconocido, nunca cero ni una media.
/// <summary>Copia inmutable de valores individuales 1–20; ausencia significa desconocido y no un valor por defecto.</summary>
public sealed class PlayerAttributes
{
    public static PlayerAttributes Empty { get; } = new([]);
    /// <summary>Valores íntegros para persistencia y lógica interna; incluye información oculta.</summary>
    public IReadOnlyDictionary<string, int> AllValues { get; }
    /// <summary>Proyección para la futura interfaz normal: excluye todos los atributos de categoría Hidden.</summary>
    public IReadOnlyDictionary<string, int> VisibleValues { get; }

    /// <summary>Valida códigos, rangos y duplicados antes de copiar; cambios posteriores del diccionario recibido no afectan a la entidad.</summary>
    public PlayerAttributes(IEnumerable<KeyValuePair<string, int>> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var all = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (code, value) in values)
        {
            _ = PlayerAttributeCatalog.Get(code);
            _ = DomainRules.PlayerRating(value);
            if (!all.TryAdd(code, value)) throw new ArgumentException("Atributo repetido: " + code, nameof(values));
        }
        AllValues = new ReadOnlyDictionary<string, int>(all);
        VisibleValues = new ReadOnlyDictionary<string, int>(
            all.Where(x => !PlayerAttributeCatalog.Get(x.Key).IsHidden).ToDictionary(x => x.Key, x => x.Value));
    }
}
