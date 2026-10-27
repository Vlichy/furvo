using Furvo.Core.Domain;
using Furvo.Core.Worlds;

namespace Furvo.Core.Generation;

/// <summary>Construye exclusivamente las 24 fichas iniciales por club de esta entrega, sin ejecutar simulación deportiva.</summary>
public static class InitialSquadGenerator
{
    public const string AlgorithmVersion = "furvo-squads-1";
    public const int PlayersPerClub = 24;

    /// <summary>Función determinista sobre un mundo de clubes vacío: el orden de entrada se normaliza por ClubId.</summary>
    public static WorldData Generate(WorldData source, SquadGenerationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.Version != AlgorithmVersion) throw new NotSupportedException("Versión de generador desconocida.");
        if (source.Players.Count != 0 || source.Contracts.Count != 0)
            throw new ArgumentException("El origen debe contener clubes sin futbolistas ni contratos.", nameof(source));
        var season = source.Seasons.Single();
        var clubs = source.Clubs.OrderBy(x => x.Id.ToString(), StringComparer.Ordinal).ToArray();
        if (!clubs.Select(x => x.Id).SequenceEqual(settings.Clubs.Select(x => x.ClubId)))
            throw new ArgumentException("Los perfiles deben corresponder exactamente a los clubes del origen.", nameof(settings));
        var players = new List<Player>();
        var contracts = new List<Contract>();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var club in clubs)
        {
            var profile = settings.Clubs.Single(x => x.ClubId == club.Id);
            var roles = Roles(profile.Style);
            // Mezclar los cupos editoriales evita asociar una nacionalidad a una posición concreta.
            var countryOrder = Enumerable.Range(0, PlayersPerClub).ToArray();
            var clubRandom = new DeterministicRandom(DeterministicRandom.Context(settings, club.Id + "|countries"));
            for (var i = countryOrder.Length - 1; i > 0; i--)
            {
                var j = clubRandom.Next(0, i + 1);
                (countryOrder[i], countryOrder[j]) = (countryOrder[j], countryOrder[i]);
            }
            var spanishSlots = countryOrder.Take(profile.SpanishPlayers).ToHashSet();
            for (var slot = 0; slot < PlayersPerClub; slot++)
            {
                var key = club.Id + "|" + slot.ToString("D2", System.Globalization.CultureInfo.InvariantCulture);
                var rng = new DeterministicRandom(DeterministicRandom.Context(settings, key + "|traits"));
                var role = roles[slot];
                var age = Age(slot, profile.AgeBias, rng);
                var youngestBirth = season.StartDate.AddYears(-age);
                var oldestBirth = season.StartDate.AddYears(-age - 1).AddDays(1);
                var birth = oldestBirth.AddDays(rng.Next(0, youngestBirth.DayNumber - oldestBirth.DayNumber + 1));
                var culture = spanishSlots.Contains(slot) ? settings.Cultures.Single(x => x.Country == "España")
                    : ForeignCulture(settings.Cultures, rng);
                var name = Name(settings, key, culture, usedNames);
                var positions = new PlayerPositions(role, Secondary(role, slot, rng));
                // Herramienta interna desechable: no se guarda ni se presenta como capacidad general.
                var level = profile.AttributeBaseline + (slot == 2 ? -4 : rng.Next(-3, 2));
                if (age <= 21) level -= rng.Next(1, 3);
                if (profile.SpotlightSlot == slot) level = 16;
                if (slot == 0) level = profile.AttributeBaseline + 1;
                var playerId = new PlayerId(DeterministicRandom.Identity(DeterministicRandom.Context(settings, key + "|player")));
                var attributes = Attributes(role, age, level, slot, rng);
                var player = new Player(playerId, name, birth, culture.Country, PlayerPositions.GroupOf(role), attributes, positions);
                players.Add(player);
                // No se crean cláusulas ni condiciones económicas; se garantiza vigencia durante la temporada inicial.
                var start = season.StartDate.AddMonths(-rng.Next(0, 49));
                var earliest = birth.AddYears(17);
                if (start < earliest) start = earliest;
                var duration = age >= 34 ? rng.Next(1, 3) : age <= 21 ? rng.Next(3, 6) : rng.Next(2, 5);
                var contractId = new ContractId(DeterministicRandom.Identity(DeterministicRandom.Context(settings, key + "|contract")));
                contracts.Add(new Contract(contractId, club.Id, playerId, start, season.StartDate.AddYears(duration).AddDays(-1)));
            }
        }
        return new WorldData(source.Clubs, players, contracts, source.Coaches, source.Competitions, source.Seasons, source.Registrations);
    }

    /// <summary>Tres variantes de subposiciones; todas conservan 3/8/8/5 sin convertirlo en un límite del dominio.</summary>
    private static PlayerRole[] Roles(int style)
    {
        var gk = Enumerable.Repeat(PlayerRole.Goalkeeper, 3);
        PlayerRole[] defence = style switch
        {
            1 => [PlayerRole.CentreBack, PlayerRole.CentreBack, PlayerRole.CentreBack, PlayerRole.RightBack,
                PlayerRole.RightBack, PlayerRole.RightBack, PlayerRole.LeftBack, PlayerRole.LeftBack],
            2 => [PlayerRole.CentreBack, PlayerRole.CentreBack, PlayerRole.CentreBack, PlayerRole.RightBack,
                PlayerRole.RightBack, PlayerRole.LeftBack, PlayerRole.LeftBack, PlayerRole.LeftBack],
            _ => [PlayerRole.CentreBack, PlayerRole.CentreBack, PlayerRole.CentreBack, PlayerRole.CentreBack,
                PlayerRole.RightBack, PlayerRole.RightBack, PlayerRole.LeftBack, PlayerRole.LeftBack]
        };
        PlayerRole[] midfield = style switch
        {
            1 => [PlayerRole.DefensiveMidfielder, PlayerRole.CentralMidfielder, PlayerRole.CentralMidfielder, PlayerRole.CentralMidfielder,
                PlayerRole.AttackingMidfielder, PlayerRole.AttackingMidfielder, PlayerRole.RightMidfielder, PlayerRole.LeftMidfielder],
            2 => [PlayerRole.DefensiveMidfielder, PlayerRole.DefensiveMidfielder, PlayerRole.CentralMidfielder, PlayerRole.CentralMidfielder,
                PlayerRole.AttackingMidfielder, PlayerRole.AttackingMidfielder, PlayerRole.RightMidfielder, PlayerRole.LeftMidfielder],
            _ => [PlayerRole.DefensiveMidfielder, PlayerRole.DefensiveMidfielder, PlayerRole.CentralMidfielder, PlayerRole.CentralMidfielder,
                PlayerRole.CentralMidfielder, PlayerRole.AttackingMidfielder, PlayerRole.RightMidfielder, PlayerRole.LeftMidfielder]
        };
        PlayerRole[] attack = style switch
        {
            1 => [PlayerRole.Striker, PlayerRole.RightWinger, PlayerRole.RightWinger, PlayerRole.LeftWinger, PlayerRole.SecondStriker],
            2 => [PlayerRole.Striker, PlayerRole.Striker, PlayerRole.RightWinger, PlayerRole.LeftWinger, PlayerRole.LeftWinger],
            _ => [PlayerRole.Striker, PlayerRole.Striker, PlayerRole.RightWinger, PlayerRole.LeftWinger, PlayerRole.SecondStriker]
        };
        return gk.Concat(defence).Concat(midfield).Concat(attack).ToArray();
    }

    // Sesgo editorial de plantilla, no un sistema de envejecimiento: conserva jóvenes, consolidados y una cola excepcional.
    private static int Age(int slot, int bias, DeterministicRandom rng)
    {
        if (slot == 0) return rng.Next(27, 35);
        if (slot == 1) return rng.Next(23, 36);
        if (slot == 2) return rng.Next(17, 22);
        var roll = rng.Next(0, 100);
        if (roll < 20 - bias * 8) return rng.Next(17, 22);
        if (roll < 40 - bias * 6) return rng.Next(22, 25);
        if (roll < 80 - bias * 5) return rng.Next(25, 31);
        return roll < 98 ? rng.Next(31, 37) : rng.Next(37, 41);
    }

    // Pesos de repertorios de generación, no cupos federativos ni restricciones a extranjeros.
    private static NameCulture ForeignCulture(IReadOnlyList<NameCulture> cultures, DeterministicRandom rng)
    {
        var options = cultures.Where(x => x.Country != "España").ToArray();
        var ticket = rng.Next(0, options.Sum(x => x.Weight));
        foreach (var culture in options)
        {
            ticket -= culture.Weight;
            if (ticket < 0) return culture;
        }
        throw new InvalidOperationException("Distribución cultural inválida.");
    }

    /// <summary>Combina fragmentos de la misma cultura y resuelve colisiones con otro flujo explícito, sin cambiar IDs.</summary>
    private static string Name(SquadGenerationSettings settings, string key, NameCulture culture, HashSet<string> used)
    {
        for (var attempt = 0; attempt < 1000; attempt++)
        {
            var rng = new DeterministicRandom(DeterministicRandom.Context(settings, key + "|name|" + attempt.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            var given = culture.GivenNames[rng.Next(0, culture.GivenNames.Count)];
            var family = culture.FamilyNames[rng.Next(0, culture.FamilyNames.Count)];
            if (culture.TwoSurnames)
            {
                var other = culture.FamilyNames[rng.Next(0, culture.FamilyNames.Count)];
                if (other == family) continue;
                family += " " + other;
            }
            var name = culture.FamilyFirst ? family + " " + given : given + " " + family;
            if (used.Add(name)) return name;
        }
        throw new InvalidOperationException("El repertorio cultural no permite nombres únicos.");
    }

    // Polivalencia descriptiva compatible; no expresa una formación, instrucciones ni dominio táctico.
    private static PlayerRole[] Secondary(PlayerRole role, int slot, DeterministicRandom rng)
    {
        if (role == PlayerRole.Goalkeeper || (slot != 3 && rng.Next(0, 100) >= 58)) return [];
        PlayerRole[] options = role switch
        {
            PlayerRole.CentreBack => [PlayerRole.DefensiveMidfielder, PlayerRole.RightBack, PlayerRole.LeftBack],
            PlayerRole.RightBack => [PlayerRole.LeftBack, PlayerRole.RightMidfielder, PlayerRole.RightWinger],
            PlayerRole.LeftBack => [PlayerRole.RightBack, PlayerRole.LeftMidfielder, PlayerRole.LeftWinger],
            PlayerRole.DefensiveMidfielder => [PlayerRole.CentreBack, PlayerRole.CentralMidfielder],
            PlayerRole.CentralMidfielder => [PlayerRole.DefensiveMidfielder, PlayerRole.AttackingMidfielder, PlayerRole.RightMidfielder, PlayerRole.LeftMidfielder],
            PlayerRole.AttackingMidfielder => [PlayerRole.CentralMidfielder, PlayerRole.SecondStriker, PlayerRole.RightWinger, PlayerRole.LeftWinger],
            PlayerRole.RightMidfielder => [PlayerRole.RightWinger, PlayerRole.CentralMidfielder],
            PlayerRole.LeftMidfielder => [PlayerRole.LeftWinger, PlayerRole.CentralMidfielder],
            PlayerRole.Striker => [PlayerRole.SecondStriker, PlayerRole.RightWinger, PlayerRole.LeftWinger],
            PlayerRole.RightWinger => [PlayerRole.LeftWinger, PlayerRole.RightMidfielder, PlayerRole.SecondStriker],
            PlayerRole.LeftWinger => [PlayerRole.RightWinger, PlayerRole.LeftMidfielder, PlayerRole.SecondStriker],
            _ => [PlayerRole.Striker, PlayerRole.AttackingMidfielder]
        };
        var first = options[rng.Next(0, options.Length)];
        return rng.Next(0, 100) < 18 ? new[] { first, options[(Array.IndexOf(options, first) + 1) % options.Length] } : [first];
    }

    /// <summary>Parte de un perfil posicional, añade especialización, variación triangular y efectos editoriales de edad.</summary>
    private static PlayerAttributes Attributes(PlayerRole role, int age, int level, int slot, DeterministicRandom rng)
    {
        var (strong, weak) = Profile(role, slot);
        var chosenStrength = strong[rng.Next(0, strong.Length)];
        var chosenWeakness = weak[rng.Next(0, weak.Length)];
        var values = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var definition in PlayerAttributeCatalog.All.OrderBy(x => x.Code, StringComparer.Ordinal))
        {
            int value;
            if (definition.IsHidden)
            {
                // Independientes de una capacidad global: todos se conservan, pero no aparecen en VisibleValues.
                value = rng.Next(6, 17) + rng.Next(-2, 3);
            }
            else if (definition.Category == AttributeCategory.Goalkeeper)
                value = role == PlayerRole.Goalkeeper ? level + 2 + rng.Next(-2, 3) : rng.Next(1, 6);
            else
            {
                value = level + rng.Next(-2, 3) + rng.Next(-1, 2);
                if (strong.Contains(definition.Code)) value += 3;
                if (weak.Contains(definition.Code)) value -= 4;
                if (definition.Code == chosenStrength) value += 1;
                if (definition.Code == chosenWeakness) value -= 2;
                if (role == PlayerRole.Goalkeeper && definition.Category == AttributeCategory.Technical) value -= 5;
                if (age <= 21 && definition.Category == AttributeCategory.Mental) value -= 2;
                if (age <= 21 && definition.Code == "strength") value -= 2;
                if (age >= 31 && definition.Code is "decisions" or "anticipation" or "composure" or "tactical_understanding") value += 2;
                if (age >= 31 && definition.Code is "pace" or "acceleration" or "stamina") value -= age >= 36 ? 4 : 2;
            }
            values.Add(definition.Code, Math.Clamp(value, 1, 20));
        }
        return new PlayerAttributes(values);
    }

    // Listas de especialidades y carencias usadas solo al crear atributos, sin fórmulas de rendimiento en partidos.
    private static (string[] Strong, string[] Weak) Profile(PlayerRole role, int slot) => role switch
    {
        PlayerRole.Goalkeeper => (["agility", "jumping", "coordination", "concentration", "decisions"],
            ["dribbling", "finishing", "crossing", "marking", "off_the_ball"]),
        PlayerRole.CentreBack => (["marking", "tackling", "anticipation", "defensive_positioning", "heading", "strength", "jumping"],
            ["dribbling", "crossing", "corners", "long_shots"]),
        PlayerRole.RightBack or PlayerRole.LeftBack => (["pace", "acceleration", "stamina", "crossing", "tackling", "work_rate"],
            ["heading", "finishing", "long_shots"]),
        PlayerRole.DefensiveMidfielder => (["tackling", "marking", "decisions", "short_passing", "defensive_positioning", "anticipation"],
            ["finishing", "crossing", "off_the_ball"]),
        PlayerRole.CentralMidfielder when slot % 2 == 0 => (["vision", "short_passing", "long_passing", "decisions", "creativity"],
            ["pace", "acceleration", "heading", "finishing"]),
        PlayerRole.CentralMidfielder => (["stamina", "work_rate", "teamwork", "off_the_ball", "short_passing"],
            ["heading", "free_kicks", "corners"]),
        PlayerRole.AttackingMidfielder => (["ball_control", "technique", "first_touch", "vision", "creativity", "short_passing"],
            ["marking", "tackling", "strength"]),
        PlayerRole.RightMidfielder or PlayerRole.LeftMidfielder or PlayerRole.RightWinger or PlayerRole.LeftWinger =>
            (["pace", "acceleration", "dribbling", "agility", "crossing", "ball_control"], ["marking", "tackling", "heading", "strength"]),
        PlayerRole.Striker when slot % 2 == 0 => (["finishing", "off_the_ball", "shot_power", "heading", "strength", "composure"],
            ["short_passing", "marking", "tackling", "vision"]),
        PlayerRole.Striker => (["finishing", "off_the_ball", "pace", "acceleration", "composure"],
            ["short_passing", "marking", "tackling", "heading"]),
        _ => (["technique", "first_touch", "creativity", "off_the_ball", "finishing"],
            ["marking", "tackling", "strength", "heading"])
    };
}
