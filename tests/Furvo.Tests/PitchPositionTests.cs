using System.Xml.Linq;
using Furvo.Core.Domain;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Xunit;

namespace Furvo.Tests;

/// <summary>Comprueba que el campo representa exactamente las posiciones registradas y no expresa nuevas capacidades.</summary>
public sealed class PitchPositionTests
{
    [Fact]
    public void GoalkeeperHasOnePrimaryMarkerNearHisOwnGoal()
    {
        var marker = Assert.Single(PitchPositionLayout.Create(PlayerRole.Goalkeeper));
        Assert.Equal(PlayerRole.Goalkeeper, marker.Role);
        Assert.True(marker.IsPrimary);
        Assert.True(marker.X < 0.15);
        Assert.Equal(0.5, marker.Y);
    }

    [Fact]
    public void SecondaryPositionsAreExactDistinctAndStableRegardlessOfInputOrder()
    {
        var roles = new[] { PlayerRole.LeftBack, PlayerRole.DefensiveMidfielder };
        var markers = PitchPositionLayout.Create(PlayerRole.CentreBack, roles);
        Assert.Equal(3, markers.Count);
        Assert.Equal(PlayerRole.CentreBack, Assert.Single(markers, m => m.IsPrimary).Role);
        Assert.Equal(roles.OrderBy(r => (int)r), markers.Where(m => !m.IsPrimary).Select(m => m.Role));
        Assert.Equal(markers, PitchPositionLayout.Create(PlayerRole.CentreBack, roles.Reverse()));
        Assert.Single(PitchPositionLayout.Create(PlayerRole.CentreBack));
        Assert.Empty(PitchPositionLayout.Create(null));
    }

    [Fact]
    public void EveryPositionHasAnInsidePitchCoordinateAndCoherentLeftRightOrientation()
    {
        var markers = Enum.GetValues<PlayerRole>().Select(role => Assert.Single(PitchPositionLayout.Create(role))).ToArray();
        Assert.Equal(13, markers.Select(m => (m.X, m.Y)).Distinct().Count());
        // El campo mínimo dibujado mide 250×162: sus etiquetas/marcas de 28 px no deben solaparse.
        for (var i = 0; i < markers.Length; i++)
        for (var j = i + 1; j < markers.Length; j++)
        {
            var dx = (markers[i].X - markers[j].X) * 250;
            var dy = (markers[i].Y - markers[j].Y) * 162;
            Assert.True(Math.Sqrt(dx * dx + dy * dy) >= 28);
        }
        Assert.All(markers, m => { Assert.InRange(m.X, 0.05, 0.95); Assert.InRange(m.Y, 0.05, 0.95); });
        Assert.True(markers.Single(m => m.Role == PlayerRole.RightBack).Y > markers.Single(m => m.Role == PlayerRole.LeftBack).Y);
        Assert.True(markers.Single(m => m.Role == PlayerRole.CentreBack).X < markers.Single(m => m.Role == PlayerRole.Striker).X);
    }

    [Fact]
    public void InvalidOrInventedPositionCombinationsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => PitchPositionLayout.Create(PlayerRole.CentreBack, new[] { PlayerRole.CentreBack }));
        Assert.Throws<ArgumentException>(() => PitchPositionLayout.Create(PlayerRole.CentreBack, new[] { PlayerRole.LeftBack, PlayerRole.LeftBack }));
        Assert.Throws<ArgumentException>(() => PitchPositionLayout.Create(PlayerRole.Goalkeeper, new[] { PlayerRole.Striker }));
        Assert.Throws<ArgumentException>(() => PitchPositionLayout.Create(null, new[] { PlayerRole.LeftBack }));
        Assert.Throws<ArgumentOutOfRangeException>(() => PitchPositionLayout.Create((PlayerRole)99));
    }

    [Fact]
    public void EveryPublishedPlayerGetsExactlyHisRegisteredMarkers()
    {
        var queries = PublishedWorldQueries.Open(Path.Combine(AppContext.BaseDirectory, "published-v4.db"));
        foreach (var club in queries.GetClubs())
        foreach (var player in queries.GetSquad(club.Id))
        {
            var details = queries.GetPlayer(club.Id, player.Id);
            var markers = PitchPositionLayout.Create(player.Position, details.SecondaryPositions);
            Assert.Equal(1 + details.SecondaryPositions.Count, markers.Count);
            Assert.Equal(player.Position, markers.Single(m => m.IsPrimary).Role);
            Assert.Equal(details.SecondaryPositions, markers.Where(m => !m.IsPrimary).Select(m => m.Role));
        }
    }

    [Fact]
    public void SpanishResourcesCoverPitchRolesLegendAndBothInsightSections()
    {
        var entries = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "UiStrings.resx")).Root!.Elements("data")
            .ToDictionary(e => e.Attribute("name")!.Value, e => e.Element("value")!.Value);
        foreach (var role in Enum.GetValues<PlayerRole>()) Assert.False(string.IsNullOrWhiteSpace(entries["PitchCode_" + role]));
        foreach (var key in new[] { "PitchLegendPrimary", "PitchLegendSecondary", "PitchAttackDirection", "StrengthsHeading", "WeaknessesHeading", "NoStrengths", "NoWeaknesses" })
            Assert.False(string.IsNullOrWhiteSpace(entries[key]));
    }
}
