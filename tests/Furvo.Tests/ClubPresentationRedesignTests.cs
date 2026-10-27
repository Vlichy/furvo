using Furvo.Core.Queries;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Xunit;

namespace Furvo.Tests;

/// <summary>Protege la separación de datos aprobados, recursos provisionales y geometría de presentación.</summary>
public sealed class ClubPresentationRedesignTests
{
    [Fact]
    public void EveryPublishedClubHasThreeOriginalProvisionalKitsAndADistinctIdentity()
    {
        var clubs = PublishedWorldQueries.Open(Path.Combine(AppContext.BaseDirectory, "published-v4.db")).GetClubs();
        var identities = new HashSet<string>();
        foreach (var club in clubs)
        {
            var visual = ClubVisualCatalog.Get(club.Id.ToString());
            Assert.Equal(club.Id.ToString(), visual.ClubId); Assert.NotEqual("?", visual.Monogram); Assert.NotEqual("F", visual.Monogram);
            Assert.Equal(3, visual.Kits.Length);
            Assert.True(identities.Add(visual.Monogram + visual.Kits[0].Primary + visual.EmblemMotif));
            Assert.Equal(3, visual.Kits.Distinct().Count());
        }
        Assert.Equal(20, identities.Count);
    }
    [Fact]
    public void UnknownClubUsesNeutralFallbackWithoutBorrowingPublishedClubIdentity()
    {
        var id = "c6ab97cd-67ec-42cf-a270-141aa8277db3";
        Assert.Equal(id, ClubVisualCatalog.Get(id).ClubId); Assert.Equal("?", ClubVisualCatalog.Get(id).Monogram);
    }
    [Fact]
    public void AllFourAreasAreAtGoalEndsAndNeverAtTheTouchlines()
    {
        Assert.Equal(4, FootballPitchGeometry.EndAreas.Count);
        foreach (var area in FootballPitchGeometry.EndAreas)
        {
            Assert.True(area.X == 0 || area.X + area.Width == FootballPitchGeometry.Length);
            Assert.True(area.Y > 0 && area.Y + area.Height < FootballPitchGeometry.Width);
            Assert.Equal(FootballPitchGeometry.Width / 2, area.Y + area.Height / 2, 6);
        }
        Assert.Equal(2, FootballPitchGeometry.EndAreas.Count(a => a.Width == 16.5));
        Assert.Equal(2, FootballPitchGeometry.EndAreas.Count(a => a.Width == 5.5));
    }
    [Fact]
    public void PenaltyArcsMeetTheFrontOfThePenaltyAreaAndRemainOutsideIt()
    {
        var boundary = FootballPitchGeometry.PenaltySpot + FootballPitchGeometry.PenaltyRadius * Math.Cos(FootballPitchGeometry.PenaltyArcHalfAngle);
        Assert.Equal(16.5, boundary, 6); Assert.True(FootballPitchGeometry.PenaltySpot + FootballPitchGeometry.PenaltyRadius > boundary);
    }
    [Theory]
    [InlineData(0)] [InlineData(6)] [InlineData(-1)]
    public void StadiumCategoryRejectsValuesOutsideOneToFive(int category) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new ClubStadiumDetails(null, null, null) { Category = category });
    [Fact]
    public void StadiumCategoryIsOptionalAndIndependentOfCapacityAndClubReputation()
    {
        Assert.Null(new ClubStadiumDetails(null, null, 90000).Category);
        Assert.Equal(1, new ClubStadiumDetails(null, null, 90000) { Category = 1 }.Category);
        Assert.Equal(5, new ClubStadiumDetails(null, null, 1000) { Category = 5 }.Category);
    }
    [Fact]
    public void NewInstitutionalFieldsStayUnknownInAllTwentyPublishedClubs()
    {
        var queries = PublishedWorldQueries.Open(Path.Combine(AppContext.BaseDirectory, "published-v4.db"));
        foreach (var club in queries.GetClubs())
        {
            var details = queries.GetClubDetails(club.Id);
            Assert.Empty(details.Identity.SupporterAssociations); Assert.Null(details.Identity.Ownership); Assert.Null(details.Identity.Affiliation);
            Assert.Null(details.Identity.President); Assert.Null(details.Identity.MajorityOwner); Assert.Null(details.Identity.GroupName);
            Assert.Null(details.Stadium.Category); Assert.Null(details.Stadium.PitchDimensions); Assert.Null(details.Stadium.OpeningYear);
            Assert.Null(details.Stadium.Surface); Assert.Null(details.Stadium.TrainingFacilities); Assert.Null(details.Stadium.YouthFacilities); Assert.Null(details.Stadium.TrainingGround);
            Assert.Empty(queries.GetClubHistory(club.Id).Timeline);
        }
        var identity = new ClubIdentityDetails(null,null,null,null,null,null) { SupporterAssociations = ["Asociación A", "Asociación B"] };
        Assert.Equal(2, identity.SupporterAssociations.Count);
    }
}
