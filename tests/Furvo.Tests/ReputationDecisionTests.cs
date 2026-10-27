using System.Text.Json;
using Furvo.Persistence;
using Xunit;

namespace Furvo.Tests;

/// <summary>Verifica la tabla manual aprobada sin introducir todavía cambios de esquema o evolución de reputación.</summary>
public sealed class ReputationDecisionTests
{
    [Fact]
    public void TwentyApprovedValuesAreExactAndBoundToTheExistingStableClubIds()
    {
        var expected = new Dictionary<string, int>
        {
            ["Horizonte"] = 98, ["Brisa"] = 96, ["Pedernal"] = 86, ["Aljarafe"] = 76,
            ["Ferrería"] = 74, ["Lumbre"] = 72, ["Azahar"] = 68, ["Mareal"] = 66,
            ["Ceramista"] = 64, ["Faro"] = 54, ["Ensenada"] = 52, ["Litoral"] = 47,
            ["Senda"] = 44, ["Mosaico"] = 34, ["Abra"] = 32, ["Acequia"] = 30,
            ["Umbral"] = 28, ["Robledal"] = 26, ["Palmeral"] = 24, ["Fragua del Sur"] = 14
        };
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "club-reputations.json")));
        var rows = document.RootElement.GetProperty("Clubs").EnumerateArray().ToArray();
        var clubs = PublishedWorldQueries.Open(Path.Combine(AppContext.BaseDirectory, "published-v3.db")).GetClubs();
        Assert.Equal(20, rows.Length);
        Assert.Equal(20, rows.Select(r => r.GetProperty("ClubId").GetString()).Distinct().Count());
        Assert.Equal(expected.Keys.OrderBy(x => x), rows.Select(r => r.GetProperty("Name").GetString()!).OrderBy(x => x));
        // El nombre solo contrasta la autoría documental; la asociación efectiva y única usa el ID persistido.
        var source = new SqliteWorldStore(Path.Combine(AppContext.BaseDirectory, "published-v3.db")).Load();
        foreach (var row in rows)
        {
            var name = row.GetProperty("Name").GetString()!;
            var internalValue = row.GetProperty("Internal").GetInt32();
            var id = row.GetProperty("ClubId").GetString();
            Assert.Equal(expected[name], internalValue);
            var club = Assert.Single(source.Clubs, c => c.Id.ToString() == id);
            Assert.Equal(name, club.Profile!.ShortName);
            var visible = Math.Min(5, internalValue / 20 + 1);
            Assert.Equal(visible, row.GetProperty("Visible").GetInt32());
            Assert.Equal(visible, Assert.Single(clubs, c => c.Id == club.Id).VisibleReputation);
        }
    }

    [Fact]
    public void ApprovedBandsCoverZeroToOneHundredExactlyOnceIncludingBoundaries()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "club-reputations.json")));
        var bands = document.RootElement.GetProperty("Bands").EnumerateArray().ToArray();
        Assert.Equal(new[] { 0, 20, 40, 60, 80 }, bands.Select(b => b.GetProperty("Minimum").GetInt32()));
        Assert.Equal(new[] { 19, 39, 59, 79, 100 }, bands.Select(b => b.GetProperty("Maximum").GetInt32()));
        for (var score = 0; score <= 100; score++)
        {
            var band = Assert.Single(bands, b => score >= b.GetProperty("Minimum").GetInt32() && score <= b.GetProperty("Maximum").GetInt32());
            Assert.Equal(Math.Min(5, score / 20 + 1), band.GetProperty("Visible").GetInt32());
        }
    }
}
