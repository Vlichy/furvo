using System.Globalization;
using Furvo.Core.Domain;
using Furvo.Core.Generation;
using Furvo.Core.Worlds;
using Furvo.Persistence;
using Xunit;

namespace Furvo.Tests;

/// <summary>Valida resultados deportivos y determinismo sin abrir bases ni utilizar datos de plantillas reales.</summary>
public sealed class SquadGenerationTests
{
    private static readonly Lazy<WorldData> Generated = new(() => InitialSquads2026.Generate());
    private static WorldData Data => Generated.Value;

    /// <summary>El reparto es propio de esta publicación: todos tienen identidad, contrato y posición detallada.</summary>
    [Fact]
    public void AllTwentySquadsHaveExactlyTheRequiredPositionsAndStableUniqueIdentities()
    {
        Assert.Equal(20, Data.Clubs.Count);
        Assert.Equal(480, Data.Players.Count);
        Assert.Equal(480, Data.Contracts.Count);
        Assert.Equal(480, Data.Players.Select(x => x.Id).Distinct().Count());
        Assert.Equal(480, Data.Contracts.Select(x => x.Id).Distinct().Count());
        Assert.Equal(480, Data.Players.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Empty(Data.Players.Select(x => x.Id.Value).Intersect(Data.Contracts.Select(x => x.Id.Value)));
        foreach (var club in Data.Clubs)
        {
            var contracts = Data.Contracts.Where(x => x.ClubId == club.Id).ToArray();
            Assert.Equal(24, contracts.Length);
            var ids = contracts.Select(x => x.PlayerId).ToHashSet();
            var squad = Data.Players.Where(x => ids.Contains(x.Id)).ToArray();
            Assert.Equal(3, squad.Count(x => x.MainPosition == PlayerPosition.Goalkeeper));
            Assert.Equal(8, squad.Count(x => x.MainPosition == PlayerPosition.Defender));
            Assert.Equal(8, squad.Count(x => x.MainPosition == PlayerPosition.Midfielder));
            Assert.Equal(5, squad.Count(x => x.MainPosition == PlayerPosition.Forward));
            Assert.Contains(squad, x => x.Positions!.Primary == PlayerRole.CentreBack);
            Assert.Contains(squad, x => x.Positions!.Primary == PlayerRole.RightBack);
            Assert.Contains(squad, x => x.Positions!.Primary == PlayerRole.LeftBack);
            Assert.Contains(squad, x => x.Positions!.Secondary.Count > 0);
        }
        Assert.All(Data.Players, player =>
        {
            Assert.NotNull(player.Positions);
            Assert.Equal(player.MainPosition, PlayerPositions.GroupOf(player.Positions!.Primary));
            Assert.DoesNotContain(player.Positions.Primary, player.Positions.Secondary);
            Assert.Equal(player.Positions.Secondary.Count, player.Positions.Secondary.Distinct().Count());
            Assert.Single(Data.Contracts, x => x.PlayerId == player.Id);
        });
    }

    /// <summary>Los 29.280 valores son individuales; los diez ocultos se guardan pero nunca se proyectan como visibles.</summary>
    [Fact]
    public void EveryPlayerHasTheCompleteIndividualCatalogWithoutUniformAttributeVectors()
    {
        var catalog = PlayerAttributeCatalog.All.Select(x => x.Code).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(29280, Data.Players.Sum(x => x.Attributes.AllValues.Count));
        foreach (var player in Data.Players)
        {
            Assert.Equal(catalog, player.Attributes.AllValues.Keys.OrderBy(x => x, StringComparer.Ordinal));
            Assert.All(player.Attributes.AllValues.Values, x => Assert.InRange(x, 1, 20));
            Assert.Equal(51, player.Attributes.VisibleValues.Count);
            Assert.DoesNotContain(player.Attributes.VisibleValues.Keys, x => PlayerAttributeCatalog.Get(x).IsHidden);
            Assert.Equal(10, player.Attributes.AllValues.Keys.Count(x => PlayerAttributeCatalog.Get(x).IsHidden));
            Assert.True(player.Attributes.AllValues.Values.Distinct().Count() >= 7);
            Assert.True(player.Attributes.VisibleValues.Values.Min() < 12);
        }
        Assert.Equal(480, Data.Players.Select(x => string.Join(",", x.Attributes.AllValues.OrderBy(v => v.Key, StringComparer.Ordinal)
            .Select(v => v.Value))).Distinct().Count());
    }

    /// <summary>Compara fortalezas con debilidades posicionales, sin introducir una valoración general del jugador.</summary>
    [Fact]
    public void ProfilesAreCoherentWithPositionsAndKeepersDoNotReceiveOutfieldTechnicalStars()
    {
        static double Mean(Player p, params string[] codes) => codes.Average(c => p.Attributes.AllValues[c]);
        foreach (var player in Data.Players)
        {
            switch (player.Positions!.Primary)
            {
                case PlayerRole.Goalkeeper:
                    var keeping = PlayerAttributeCatalog.All.Where(x => x.Category == AttributeCategory.Goalkeeper).Average(x => player.Attributes.AllValues[x.Code]);
                    var technique = PlayerAttributeCatalog.All.Where(x => x.Category == AttributeCategory.Technical).Average(x => player.Attributes.AllValues[x.Code]);
                    Assert.True(keeping > technique + 2, player.Name);
                    break;
                case PlayerRole.CentreBack:
                    Assert.True(Mean(player, "marking", "tackling", "defensive_positioning", "anticipation")
                        > Mean(player, "dribbling", "crossing", "corners", "long_shots") + 1, player.Name);
                    break;
                case PlayerRole.RightWinger:
                case PlayerRole.LeftWinger:
                    Assert.True(Mean(player, "pace", "acceleration", "dribbling", "agility")
                        > Mean(player, "marking", "tackling", "heading") + 1, player.Name);
                    break;
                case PlayerRole.Striker:
                    Assert.True(Mean(player, "finishing", "off_the_ball") >
                        Mean(player, "marking", "tackling", "short_passing") + 1, player.Name);
                    break;
            }
            if (player.MainPosition != PlayerPosition.Goalkeeper)
                Assert.All(PlayerAttributeCatalog.All.Where(x => x.Category == AttributeCategory.Goalkeeper),
                    x => Assert.InRange(player.Attributes.AllValues[x.Code], 1, 5));
        }
    }

    /// <summary>La plantilla mezcla edades y nacionalidades; el tercer portero joven no obliga a rejuvenecer todo el club.</summary>
    [Fact]
    public void BirthDatesAndCulturalNamesAreCoherentAtTheExplicitSeasonStart()
    {
        var start = Data.Seasons.Single().StartDate;
        var settings = InitialSquads2026.Settings();
        Assert.All(Data.Players, x => Assert.InRange(x.AgeOn(start), 17, 40));
        Assert.Equal(318, Data.Players.Count(x => x.Nationality == "España"));
        Assert.True(Data.Players.Select(x => x.Nationality).Distinct().Count() >= 12);
        Assert.Contains(Data.Players, x => x.AgeOn(start) >= 37);
        foreach (var club in Data.Clubs)
        {
            var ids = Data.Contracts.Where(x => x.ClubId == club.Id).Select(x => x.PlayerId).ToHashSet();
            var squad = Data.Players.Where(x => ids.Contains(x.Id)).ToArray();
            Assert.Contains(squad, x => x.MainPosition == PlayerPosition.Goalkeeper && x.AgeOn(start) <= 21);
            Assert.True(squad.Select(x => x.Nationality).Distinct().Count() >= 2);
            Assert.True(squad.Select(x => x.AgeOn(start)).Distinct().Count() >= 7);
        }
        foreach (var player in Data.Players)
        {
            var culture = settings.Cultures.Single(x => x.Country == player.Nationality);
            Assert.True(culture.FamilyFirst
                ? culture.GivenNames.Any(x => player.Name.EndsWith(" " + x, StringComparison.Ordinal))
                : culture.GivenNames.Any(x => player.Name.StartsWith(x + " ", StringComparison.Ordinal)));
            Assert.Contains(culture.FamilyNames, x => player.Name.Contains(x, StringComparison.Ordinal));
        }
    }

    /// <summary>Los clubes importantes tienen otra distribución, pero los modestos conservan especialistas y los grandes jóvenes limitados.</summary>
    [Fact]
    public void ClubsDifferWithoutMakingReputationAnIndividualRating()
    {
        var elite = Data.Clubs.Where(x => x.Reputation == 5).Select(x => x.Id).ToHashSet();
        var modest = Data.Clubs.Where(x => x.Reputation <= 2).Select(x => x.Id).ToHashSet();
        var eliteIds = Data.Contracts.Where(x => elite.Contains(x.ClubId)).Select(x => x.PlayerId).ToHashSet();
        var modestIds = Data.Contracts.Where(x => modest.Contains(x.ClubId)).Select(x => x.PlayerId).ToHashSet();
        var outfieldCodes = PlayerAttributeCatalog.All.Where(x => !x.IsHidden && x.Category != AttributeCategory.Goalkeeper).Select(x => x.Code).ToArray();
        var high = Data.Players.Where(x => eliteIds.Contains(x.Id)).ToArray();
        var low = Data.Players.Where(x => modestIds.Contains(x.Id)).ToArray();
        // Estas medias son únicamente una comprobación estadística de fixtures; no se incorporan al modelo o a SQLite.
        Assert.True(high.Average(p => outfieldCodes.Average(c => p.Attributes.AllValues[c]))
            > low.Average(p => outfieldCodes.Average(c => p.Attributes.AllValues[c])) + 1);
        Assert.Contains(low, p => p.Attributes.VisibleValues.Values.Max() >= 18);
        Assert.Contains(high, p => p.AgeOn(Data.Seasons.Single().StartDate) <= 21
            && outfieldCodes.Average(c => p.Attributes.AllValues[c]) < 12);
        foreach (var name in new[] { "Overall", "Quality", "Ability", "Rating", "ClubId" })
            Assert.Null(typeof(Player).GetProperty(name));
    }

    /// <summary>La afiliación se determina por un único contrato que cubre toda la temporada, sin duplicarla en Player.</summary>
    [Fact]
    public void EveryPlayerHasOneValidInitialContractAndNoIncompatibleOverlap()
    {
        var season = Data.Seasons.Single();
        foreach (var player in Data.Players)
        {
            var contract = Assert.Single(Data.Contracts, x => x.PlayerId == player.Id);
            Assert.Contains(Data.Clubs, x => x.Id == contract.ClubId);
            Assert.True(contract.StartDate <= season.StartDate);
            Assert.True(contract.EndDate >= season.EndDate);
            Assert.True(contract.StartDate >= player.BirthDate!.Value.AddYears(17));
        }
        Assert.True(Data.Contracts.Select(x => x.EndDate).Distinct().Count() >= 4);
        Assert.True(Data.Contracts.Select(x => x.StartDate).Distinct().Count() >= 20);
    }

    /// <summary>Misma semilla y parámetros producen las mismas identidades y valores, incluso con orden o cultura de proceso distintos.</summary>
    [Fact]
    public void GenerationIsReproducibleAndIndependentOfInputOrderAndCurrentCulture()
    {
        var first = InitialSquads2026.Generate();
        var settings = InitialSquads2026.Settings();
        var reversed = new SquadGenerationSettings(settings.Version, settings.Seed, settings.Clubs.Reverse(), settings.Cultures.Reverse());
        var emptySource = new WorldData(first.Clubs.Reverse(), competitions: first.Competitions, seasons: first.Seasons, registrations: first.Registrations.Reverse());
        Assert.Equal(InitialSquads2026.Fingerprint(first), InitialSquads2026.Fingerprint(InitialSquadGenerator.Generate(emptySource, reversed)));
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            Assert.Equal(InitialSquads2026.Fingerprint(first), InitialSquads2026.Fingerprint(InitialSquads2026.Generate(settings)));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    /// <summary>Otra semilla cambia la población sin alterar las identidades de los clubes o la temporada.</summary>
    [Fact]
    public void AChangedSeedChangesPlayersButNotTheSourceClubs()
    {
        var settings = InitialSquads2026.Settings();
        var different = InitialSquads2026.Generate(new SquadGenerationSettings(settings.Version, settings.Seed + 1, settings.Clubs, settings.Cultures));
        Assert.NotEqual(InitialSquads2026.Fingerprint(Data), InitialSquads2026.Fingerprint(different));
        Assert.Empty(Data.Players.Select(x => x.Id).Intersect(different.Players.Select(x => x.Id)));
        Assert.Equal(Data.Clubs, different.Clubs);
        Assert.Equal(480, different.Players.Count);
    }

    /// <summary>Los errores de entradas no se ocultan ni generan una población parcial con parámetros desconocidos.</summary>
    [Fact]
    public void InvalidVersionsIncompleteClubProfilesAndNonEmptySourcesAreRejected()
    {
        var settings = InitialSquads2026.Settings();
        Assert.Throws<NotSupportedException>(() => InitialSquads2026.Generate(
            new SquadGenerationSettings("unknown", settings.Seed, settings.Clubs, settings.Cultures)));
        Assert.Throws<ArgumentException>(() => InitialSquads2026.Generate(
            new SquadGenerationSettings(settings.Version, settings.Seed, settings.Clubs.Skip(1), settings.Cultures)));
        Assert.Throws<ArgumentException>(() => InitialSquadGenerator.Generate(Data, settings));
    }

    /// <summary>La edad se calcula en la fecha solicitada y no depende del día de ejecución de la prueba.</summary>
    [Theory]
    [InlineData(2026, 6, 30, 25)]
    [InlineData(2026, 7, 1, 26)]
    [InlineData(2026, 7, 2, 26)]
    public void AgeIsCalculatedAtTheRequestedDate(int year, int month, int day, int expected)
    {
        var player = new Player(PlayerId.New(), "Prueba", new DateOnly(2000, 7, 1));
        Assert.Equal(expected, player.AgeOn(new DateOnly(year, month, day)));
    }

    /// <summary>Los datos históricos desconocidos y los desgloses inválidos no se convierten en edades o posiciones ficticias.</summary>
    [Fact]
    public void UnknownAgesAndIncompatibleDetailedPositionsAreRejected()
    {
        Assert.Throws<InvalidOperationException>(() => new Player(PlayerId.New(), "Prueba").AgeOn(new DateOnly(2026, 7, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Player(PlayerId.New(), "Prueba", new DateOnly(2000, 1, 1)).AgeOn(new DateOnly(1999, 1, 1)));
        Assert.Throws<ArgumentException>(() => new PlayerPositions(PlayerRole.CentreBack, [PlayerRole.CentreBack]));
        Assert.Throws<ArgumentException>(() => new PlayerPositions(PlayerRole.RightBack, [PlayerRole.LeftBack, PlayerRole.LeftBack]));
        Assert.Throws<ArgumentException>(() => new PlayerPositions(PlayerRole.Goalkeeper, [PlayerRole.Striker]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlayerPositions((PlayerRole)99));
        Assert.Throws<ArgumentException>(() => new Player(PlayerId.New(), "Prueba", mainPosition: PlayerPosition.Forward,
            positions: new PlayerPositions(PlayerRole.CentreBack)));
    }
}
