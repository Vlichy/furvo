using Furvo.Core.Domain;

namespace Furvo.Core.Generation;

/// <summary>Parámetros editoriales exclusivos del generador; no son estadísticas oficiales de clubes o jugadores.</summary>
public sealed record ClubGenerationProfile(ClubId ClubId, int AttributeBaseline, int SpanishPlayers, int AgeBias, int Style, int? SpotlightSlot);

/// <summary>Fragmentos de nombres de una misma cultura, sin identidades procedentes de plantillas reales.</summary>
public sealed class NameCulture
{
    public string Country { get; }
    public IReadOnlyList<string> GivenNames { get; }
    public IReadOnlyList<string> FamilyNames { get; }
    public bool TwoSurnames { get; }
    public bool FamilyFirst { get; }
    public int Weight { get; }

    /// <summary>Copia los fragmentos para que la generación no cambie si el llamador altera sus listas.</summary>
    public NameCulture(string country, IEnumerable<string> givenNames, IEnumerable<string> familyNames,
        bool twoSurnames, bool familyFirst, int weight)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(country);
        var given = givenNames.ToArray();
        var family = familyNames.ToArray();
        if (given.Length < 2 || family.Length < 2 || given.Concat(family).Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Un repertorio cultural necesita fragmentos suficientes y no vacíos.");
        if (weight <= 0) throw new ArgumentOutOfRangeException(nameof(weight));
        Country = country;
        GivenNames = Array.AsReadOnly(given);
        FamilyNames = Array.AsReadOnly(family);
        TwoSurnames = twoSurnames;
        FamilyFirst = familyFirst;
        Weight = weight;
    }
}

/// <summary>Contrato de entradas reproducibles: versión, semilla, perfiles de clubes y repertorios culturales.</summary>
public sealed class SquadGenerationSettings
{
    public string Version { get; }
    public long Seed { get; }
    public IReadOnlyList<ClubGenerationProfile> Clubs { get; }
    public IReadOnlyList<NameCulture> Cultures { get; }

    /// <summary>Valida parámetros de esta publicación; el límite de 24 pertenece al generador, no al modelo de plantilla.</summary>
    public SquadGenerationSettings(string version, long seed, IEnumerable<ClubGenerationProfile> clubs, IEnumerable<NameCulture> cultures)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        var profiles = clubs.OrderBy(x => x.ClubId.ToString(), StringComparer.Ordinal).ToArray();
        var names = cultures.OrderBy(x => x.Country, StringComparer.Ordinal).ToArray();
        if (profiles.Select(x => x.ClubId).Distinct().Count() != profiles.Length || profiles.Any(x =>
            x.ClubId.Value == Guid.Empty || x.AttributeBaseline is < 6 or > 16 || x.SpanishPlayers is < 1 or > 23
            || x.AgeBias is < -1 or > 1 || x.Style is < 0 or > 2 || x.SpotlightSlot is < 0 or >= 24))
            throw new ArgumentException("Perfiles editoriales inválidos o duplicados.", nameof(clubs));
        if (names.Select(x => x.Country).Distinct(StringComparer.Ordinal).Count() != names.Length
            || !names.Any(x => x.Country == "España") || names.Length < 2)
            throw new ArgumentException("Repertorios culturales insuficientes o duplicados.", nameof(cultures));
        Version = version;
        Seed = seed;
        Clubs = Array.AsReadOnly(profiles);
        Cultures = Array.AsReadOnly(names);
    }
}
