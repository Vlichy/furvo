using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Furvo.Core.Queries;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Xunit;

namespace Furvo.Tests;

/// <summary>Revisa el borrador real y protege la separación entre vista previa y publicación persistente.</summary>
public sealed class ClubDirectionProposalTests
{
    private static string Json()
    {
        using var stream = typeof(ClubDirectionProposalCatalog).Assembly.GetManifestResourceStream("Furvo.ClubDirectionProposals")!;
        using var reader = new StreamReader(stream); return reader.ReadToEnd();
    }
    [Fact]
    public void EveryPublishedClubHasItsOwnDraftWithThreeToFivePrinciplesAndAllHorizons()
    {
        var proposals = ClubDirectionProposalCatalog.Parse(Json());
        var queries = PublishedWorldQueries.Open(Path.Combine(AppContext.BaseDirectory, "published-v4.db"));
        Assert.Equal(20, proposals.Count);
        Assert.Equal(queries.GetClubs().Select(c => c.Id.ToString()).Order(), proposals.Keys.Order());
        var signatures = new HashSet<string>();
        foreach (var club in queries.GetClubs())
        {
            var proposal = proposals[club.Id.ToString()]; Assert.Equal(club.Id, proposal.ClubId);
            Assert.InRange(proposal.Principles.Count, 3, 5);
            Assert.True(signatures.Add(string.Join("|", proposal.Principles.Select(p => p.Title))));
            Assert.All(proposal.Principles, p => { Assert.False(string.IsNullOrWhiteSpace(p.Description)); Assert.False(string.IsNullOrWhiteSpace(p.Code)); });
            Assert.Equal(Enum.GetValues<BoardObjectiveHorizon>().Order(), proposal.Objectives.Select(o => o.Horizon).Distinct().Order());
            Assert.All(proposal.Objectives, o => { Assert.True(Enum.IsDefined(o.Priority)); Assert.Equal(2026, o.StartSeason); });
            Assert.True(((ICollection<ClubPrincipleProposal>)proposal.Principles).IsReadOnly);
            Assert.True(((ICollection<BoardObjectiveProposal>)proposal.Objectives).IsReadOnly);
        }
    }
    [Fact]
    public void DeadlinesAndOrderingAreReproducibleAndUnknownClubsDoNotBorrowAnotherProposal()
    {
        var first = ClubDirectionProposalCatalog.Parse(Json()); var second = ClubDirectionProposalCatalog.Parse(Json());
        foreach (var pair in first)
        {
            Assert.Equal(pair.Value.Principles, second[pair.Key].Principles);
            Assert.Equal(pair.Value.Objectives, second[pair.Key].Objectives);
            var ordered = pair.Value.Objectives.OrderBy(o => o.Horizon).ToArray();
            Assert.Equal(new DateOnly(2027,6,30), ordered[0].Deadline);
            Assert.Equal(new DateOnly(2029,6,30), ordered[1].Deadline);
            Assert.Equal(new DateOnly(2031,6,30), ordered[2].Deadline);
        }
        Assert.Null(ClubDirectionProposalCatalog.Find("c6ab97cd-67ec-42cf-a270-141aa8277db3"));
    }
    [Fact]
    public void ReadingDraftsDoesNotWriteDatabaseOrReplaceClubDetailsAndPlayers()
    {
        var database = Path.Combine(AppContext.BaseDirectory,"published-v4.db"); var before = SHA256.HashData(File.ReadAllBytes(database));
        var queries = PublishedWorldQueries.Open(database);
        var total = 0;
        foreach (var club in queries.GetClubs())
        {
            Assert.NotNull(ClubDirectionProposalCatalog.Find(club.Id.ToString()));
            Assert.Equal(club.VisibleReputation, queries.GetClubDetails(club.Id).Club.VisibleReputation);
            var players = queries.GetSquad(club.Id); Assert.Equal(24, players.Count); total += players.Count;
            foreach (var player in players)
                Assert.Equal(51, queries.GetPlayer(club.Id, player.Id).AttributeGroups.Sum(g => g.Values.Count));
        }
        Assert.Equal(480,total); Assert.Equal(before, SHA256.HashData(File.ReadAllBytes(database)));
    }
    [Theory]
    [InlineData("approved")] [InlineData("duplicate-club")] [InlineData("duplicate-principle")]
    [InlineData("missing-medium")] [InlineData("past-deadline")] [InlineData("too-few-principles")]
    [InlineData("invalid-priority")] [InlineData("long-before-medium")] [InlineData("wrong-language")]
    public void InvalidOrSupposedlyApprovedDraftsCannotBecomeActiveByChangingResource(string mutation)
    {
        var root = JsonNode.Parse(Json())!; var clubs = root["Clubs"]!.AsArray(); var club = clubs[0]!;
        var objectives = club["Objectives"]!.AsArray(); var principles = club["Principles"]!.AsArray();
        switch (mutation)
        {
            case "approved": root["Status"] = "Approved"; break;
            case "duplicate-club": clubs.Add(clubs[0]!.DeepClone()); break;
            case "duplicate-principle": principles[1]!["Code"] = principles[0]!["Code"]!.GetValue<string>(); break;
            case "missing-medium": objectives.RemoveAt(1); break;
            case "past-deadline": objectives[0]!["DueSeason"] = 2025; break;
            case "too-few-principles": principles.RemoveAt(3); principles.RemoveAt(2); break;
            case "invalid-priority": objectives[0]!["Priority"] = "Dismissal"; break;
            case "long-before-medium": objectives[2]!["DueSeason"] = 2027; break;
            case "wrong-language": root["Language"] = "en"; break;
        }
        var error = Record.Exception(() => ClubDirectionProposalCatalog.Parse(root.ToJsonString()));
        Assert.True(error is InvalidDataException or JsonException, error?.ToString() ?? "Se aceptó un borrador no válido.");
    }
}
