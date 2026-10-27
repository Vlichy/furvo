using System.Globalization;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Xunit;

namespace Furvo.Tests;

/// <summary>Comprueba selección descriptiva y protección de atributos; no valida una fórmula de habilidad porque no existe.</summary>
public sealed class PositionInsightsTests
{
    private static VisibleAttributeGroup[] Groups(Func<AttributeDefinition, int> score) => PlayerAttributeCatalog.All
        .GroupBy(d => d.Category).Select(g => new VisibleAttributeGroup(g.Key,
            Array.AsReadOnly(g.Select(d => new VisibleAttribute(d.Code, score(d))).ToArray()))).ToArray();

    [Fact]
    public void EveryDetailedPositionHasExplicitDistinctPublicRelevantAttributes()
    {
        var catalog = PlayerAttributeCatalog.All.Where(d => !d.IsHidden).Select(d => d.Code).ToHashSet();
        foreach (var role in Enum.GetValues<PlayerRole>())
        {
            var codes = PositionInsightRules.RelevantFor(role);
            Assert.True(codes.Count >= 6);
            Assert.Equal(codes.Count, codes.Distinct().Count());
            Assert.All(codes, code => Assert.Contains(code, catalog));
        }
        Assert.All(PositionInsightRules.RelevantFor(PlayerRole.Goalkeeper), code =>
            Assert.Equal(AttributeCategory.Goalkeeper, PlayerAttributeCatalog.All.Single(d => d.Code == code).Category));
        Assert.Throws<ArgumentOutOfRangeException>(() => PositionInsightRules.RelevantFor((PlayerRole)99));
    }

    [Fact]
    public void GoalkeeperDoesNotReceiveOffensiveStrengthsOrWeaknesses()
    {
        var groups = Groups(d => d.Code switch
        {
            "finishing" or "crossing" => 20, "corners" => 1,
            "reflexes" => 19, "handling" => 17, "command_of_area" => 14,
            "rushing_out" => 1, "aerial_claims" => 3, "one_on_ones" => 5, _ => 10
        });
        var result = PositionInsightRules.Describe(PlayerRole.Goalkeeper, groups);
        Assert.Equal(new[] { "reflexes", "handling", "command_of_area" }, result.Strengths.Select(v => v.Code));
        Assert.Equal(new[] { "rushing_out", "aerial_claims", "one_on_ones" }, result.Weaknesses.Select(v => v.Code));
    }

    [Fact]
    public void PositionChangesRelevanceWithoutCreatingAnOverallRating()
    {
        var groups = Groups(d => d.Code switch
            { "marking" => 18, "tackling" => 17, "heading" => 16, "finishing" => 20, "dribbling" => 19, "off_the_ball" => 15, _ => 10 });
        var defender = PositionInsightRules.Describe(PlayerRole.CentreBack, groups);
        var striker = PositionInsightRules.Describe(PlayerRole.Striker, groups);
        Assert.Equal("marking", defender.Strengths[0].Code);
        Assert.Equal("finishing", striker.Strengths[0].Code);
        Assert.DoesNotContain(defender.Strengths, a => a.Code is "finishing" or "dribbling");
        Assert.Equal(new[] { "Strengths", "Weaknesses" }, typeof(PositionInsights).GetProperties().Select(p => p.Name));
    }

    [Fact]
    public void TiesAreOrdinalStableAndThresholdsDoNotForceThreeItems()
    {
        var groups = Groups(_ => 14);
        var old = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            var first = PositionInsightRules.Describe(PlayerRole.Goalkeeper, groups);
            var reversed = groups.Reverse().Select(g => new VisibleAttributeGroup(g.Category, g.Values.Reverse().ToArray()));
            var second = PositionInsightRules.Describe(PlayerRole.Goalkeeper, reversed);
            Assert.Equal(first.Strengths, second.Strengths);
            Assert.Equal(PositionInsightRules.RelevantFor(PlayerRole.Goalkeeper).OrderBy(x => x, StringComparer.Ordinal).Take(3), first.Strengths.Select(v => v.Code));
            Assert.Empty(first.Weaknesses);
            Assert.Empty(PositionInsightRules.Describe(PlayerRole.Goalkeeper, Groups(_ => 13)).Strengths);
            Assert.Empty(PositionInsightRules.Describe(PlayerRole.Goalkeeper, Groups(_ => 8)).Weaknesses);
            Assert.Equal(3, PositionInsightRules.Describe(PlayerRole.Goalkeeper, Groups(_ => 7)).Weaknesses.Count);
        }
        finally { CultureInfo.CurrentCulture = old; }
    }

    [Fact]
    public void HiddenAndMislabeledValuesCannotLeakAndUnknownPositionInventsNothing()
    {
        var groups = Groups(_ => 10).Concat(new[]
        {
            new VisibleAttributeGroup(AttributeCategory.Hidden, new[] { new VisibleAttribute("professionalism", 20), new VisibleAttribute("reflexes", 20) }),
            new VisibleAttributeGroup(AttributeCategory.Technical, new[] { new VisibleAttribute("ambition", 1), new VisibleAttribute("handling", 20) })
        }).ToArray();
        var result = PositionInsightRules.Describe(PlayerRole.Goalkeeper, groups);
        Assert.Empty(result.Strengths); Assert.Empty(result.Weaknesses);
        Assert.Empty(PositionInsightRules.Describe(null, Groups(_ => 20)).Strengths);
        Assert.Empty(PositionInsightRules.Describe(null, Groups(_ => 1)).Weaknesses);
        Assert.Throws<ArgumentOutOfRangeException>(() => PositionInsightRules.Describe(PlayerRole.Goalkeeper, Groups(_ => 21)));
        Assert.Throws<InvalidDataException>(() => PositionInsightRules.Describe(PlayerRole.Goalkeeper,
            Groups(_ => 10).Append(new VisibleAttributeGroup(AttributeCategory.Goalkeeper, new[] { new VisibleAttribute("reflexes", 19) }))));
    }

    [Fact]
    public void AllPublishedPlayersKeepTheirValuesAndOnlyReceiveRelevantPublicDescriptions()
    {
        var queries = PublishedWorldQueries.Open(Path.Combine(AppContext.BaseDirectory, "published-v4.db"));
        var count = 0;
        foreach (var club in queries.GetClubs())
        foreach (var player in queries.GetSquad(club.Id))
        {
            var details = queries.GetPlayer(club.Id, player.Id);
            var original = details.AttributeGroups.SelectMany(g => g.Values).ToDictionary(v => v.Code, v => v.Value);
            var result = PositionInsightRules.Describe(player.Position, details.AttributeGroups);
            Assert.InRange(result.Strengths.Count, 0, 3); Assert.InRange(result.Weaknesses.Count, 0, 3);
            Assert.All(result.Strengths, v => Assert.InRange(v.Value, 14, 20));
            Assert.All(result.Weaknesses, v => Assert.InRange(v.Value, 1, 7));
            foreach (var value in result.Strengths.Concat(result.Weaknesses))
            {
                Assert.Contains(value.Code, PositionInsightRules.RelevantFor(player.Position!.Value));
                Assert.Equal(original[value.Code], value.Value);
                Assert.False(PlayerAttributeCatalog.All.Single(d => d.Code == value.Code).IsHidden);
            }
            Assert.Equal(51, original.Count);
            Assert.Equal(original.OrderBy(x => x.Key), details.AttributeGroups.SelectMany(g => g.Values).ToDictionary(v => v.Code, v => v.Value).OrderBy(x => x.Key));
            count++;
        }
        Assert.Equal(480, count);
    }
}
