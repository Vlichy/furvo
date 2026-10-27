using Furvo.Core.Queries;
using Furvo.Presentation.Components;
using Xunit;

namespace Furvo.Tests;

/// <summary>Los ejemplos de gráfico son fixtures de prueba, nunca resultados del catálogo comercial.</summary>
public sealed class ClubHistoryPlotTests
{
    [Fact]
    public void EmptyAndUnknownPositionsDoNotCreateResults()
    {
        Assert.Empty(ClubHistoryPlot.Build(Array.Empty<ClubSeasonStanding>()));
        Assert.Empty(ClubHistoryPlot.Build([new("Fixture", 2000, 1, null, ClubHistoryOrigin.AuthoredBefore2026)]));
    }
    [Fact]
    public void FirstPlaceIsAtTheTopAndSecondDivisionHasItsOwnBand()
    {
        var points = ClubHistoryPlot.Build([new("Fixture A", 2000, 1, 1, ClubHistoryOrigin.AuthoredBefore2026),
            new("Fixture B", 2001, 1, 20, ClubHistoryOrigin.AuthoredBefore2026), new("Fixture C", 2002, 2, 1, ClubHistoryOrigin.SimulatedSave),
            new("Fixture D", 2003, 2, 20, ClubHistoryOrigin.SimulatedSave)]);
        Assert.Equal(0, points[0].Y); Assert.Equal(1, points[3].Y); Assert.True(points[0].Y < points[1].Y && points[1].Y < points[2].Y);
        Assert.True(ClubHistoryPlot.CanConnect(points[1], points[2]));
    }
    [Fact]
    public void MissingYearsAreNotConnectedAndInputOrderIsStable()
    {
        ClubSeasonStanding[] rows = [new("Fixture A", 2000, 1, 4, ClubHistoryOrigin.AuthoredBefore2026),
            new("Fixture B", 2001, 1, null, ClubHistoryOrigin.AuthoredBefore2026), new("Fixture C", 2002, 2, 7, ClubHistoryOrigin.SimulatedSave)];
        var points = ClubHistoryPlot.Build(rows); Assert.Equal(points, ClubHistoryPlot.Build(rows.Reverse()));
        Assert.Equal(2, points.Count); Assert.False(ClubHistoryPlot.CanConnect(points[0], points[1]));
    }
    [Fact]
    public void InvalidPositionsDivisionsAndDuplicateSeasonsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ClubHistoryPlot.Build([new("Fixture", 2000, 1, 0, ClubHistoryOrigin.AuthoredBefore2026)]));
        Assert.Throws<ArgumentOutOfRangeException>(() => ClubHistoryPlot.Build([new("Fixture", 2000, 3, 1, ClubHistoryOrigin.AuthoredBefore2026)]));
        Assert.Throws<ArgumentException>(() => ClubHistoryPlot.Build([new("A", 2000, 1, 2, ClubHistoryOrigin.AuthoredBefore2026), new("B", 2000, 2, 3, ClubHistoryOrigin.SimulatedSave)]));
    }
}
