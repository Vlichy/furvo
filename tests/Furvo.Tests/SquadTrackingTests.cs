using Furvo.Core.Queries;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Xunit;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    [Fact] public void InitialStateHasOnlyConfirmedZeroCountsAndNoInventedIndicators()
    {
        var state=PlayerSportingStatus.Initial(true);Assert.Equal(new PlayerSeasonStatistics(0,0,0,null),state.Season);
        Assert.Null(state.Quality.Segments);Assert.Null(state.Physical.Percentage);Assert.Null(state.Rhythm.Segments);Assert.Null(state.Morale);
        Assert.Equal(5,state.LastFive().Count);Assert.All(state.LastFive(),a=>Assert.Null(a));
        Assert.Null(PlayerSportingStatus.Initial(false).Season);
    }
    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)]
    public void SegmentedContractAdmitsExactlyFiveSegmentsWithoutThresholdCalculations(int segments)
    {var value=new SegmentedStatus(segments,87,SportingTone.Positive);Assert.Equal(segments,value.Segments);Assert.Equal(87,value.Percentage);}
    [Theory][InlineData(-1,50)][InlineData(6,50)][InlineData(3,-1)][InlineData(3,101)]
    public void InvalidSegmentOrPercentDataIsRejected(int segments,int percent)=>Assert.Throws<ArgumentOutOfRangeException>(()=>new SegmentedStatus(segments,percent));
    [Fact] public void FormIsFiveIndependentPlayedAppearancesChronologicalWithUnavailableSlots()
    {
        var first=new PlayerAppearance(Guid.NewGuid(),new(2026,8,1),90,8m,SportingTone.Positive);
        var absent=new PlayerAppearance(Guid.NewGuid(),new(2026,8,2),0,1m,SportingTone.Negative);
        var second=new PlayerAppearance(Guid.NewGuid(),new(2026,8,3),15,6m,SportingTone.Intermediate);
        var status=new PlayerSportingStatus{Appearances=Array.AsReadOnly(new[]{second,absent,first})};var five=status.LastFive();
        Assert.Equal(first,five[0]);Assert.Equal(second,five[1]);Assert.All(five.Skip(2),a=>Assert.Null(a));Assert.DoesNotContain(five,a=>a==absent);
    }
    [Fact] public void FormKeepsLastFivePlayedAndDoesNotDeriveSeasonAverage()
    {
        var matches=Enumerable.Range(1,7).Select(i=>new PlayerAppearance(Guid.NewGuid(),new DateOnly(2026,8,i),90,i,SportingTone.Neutral)).ToArray();
        var state=new PlayerSportingStatus{Appearances=Array.AsReadOnly(matches.Reverse().ToArray()),Season=new(7,2,1,7.25m)};
        Assert.Equal(matches.Skip(2),state.LastFive());Assert.Equal(7.25m,state.Season.Average);
    }
    [Fact] public void DuplicateOrInvalidAppearancesAreNotRenderedAsRealGames()
    {
        var match=new PlayerAppearance(Guid.NewGuid(),new(2026,8,1),90,null,null);
        Assert.Throws<InvalidDataException>(()=>(new PlayerSportingStatus{Appearances=Array.AsReadOnly(new[]{match,match})}).LastFive());
        Assert.Throws<InvalidDataException>(()=>(new PlayerSportingStatus{Appearances=Array.AsReadOnly(new[]{match with{MinutesPlayed=-1}})}).LastFive());
    }
    [Fact] public void TrackingRowsRetainAllPlayersAndSortingDoesNotMutateIndicators()
    {
        var q=PublishedWorldQueries.Open(EconomicSource);var rows=q.GetSquad(q.GetClubs()[0].Id).Select(p=>new SquadTrackingRow(p,SquadAssignment.Unassigned,PlayerSportingStatus.Initial(true))).ToArray();
        var before=rows.Select(r=>r.Player.Id).ToArray();
        foreach(var order in Enum.GetValues<SquadTrackingSort>())
        {
            var sorted=SquadTrackingRows.Sort(rows,order,true);Assert.Equal(24,sorted.Select(r=>r.Player.Id).Distinct().Count());Assert.All(sorted,r=>Assert.Equal(0,r.Status.Season!.Played));
        }
        Assert.Equal(before,rows.Select(r=>r.Player.Id));Assert.All(rows,r=>Assert.Null(r.Status.Quality.Segments));
    }
    [Fact] public void UnknownValuesStayUnknownAndLastWhenSortingInEitherDirection()
    {
        var q=PublishedWorldQueries.Open(EconomicSource);var players=q.GetSquad(q.GetClubs()[0].Id);
        var known=new SquadTrackingRow(players[0],SquadAssignment.Unassigned,new(){Physical=new(4,87,SportingTone.Positive)});
        var unknown=new SquadTrackingRow(players[1],SquadAssignment.Unassigned,new());
        Assert.Equal(unknown,SquadTrackingRows.Sort(new[]{unknown,known},SquadTrackingSort.Physical)[1]);
        Assert.Equal(unknown,SquadTrackingRows.Sort(new[]{unknown,known},SquadTrackingSort.Physical,true)[1]);
    }
}
