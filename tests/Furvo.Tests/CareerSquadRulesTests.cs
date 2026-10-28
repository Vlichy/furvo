using Furvo.Core.Careers;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Xunit;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    [Fact] public void AllThirteenDemarcationsHaveThreeExplicitDistinctVisibleAttributes()
    {
        Assert.Equal(13,Enum.GetValues<PlayerRole>().Length);
        foreach(var role in Enum.GetValues<PlayerRole>())
        {
            var codes=CareerSquadRules.CodesFor(role);Assert.Equal(3,codes.Count);Assert.Equal(3,codes.Distinct().Count());
            Assert.All(codes,code=>Assert.Contains(PlayerAttributeCatalog.All,d=>d.Code==code&&!d.IsHidden));
        }
        Assert.Equal(new[]{"reflexes","one_on_ones","goalkeeping_positioning"},CareerSquadRules.CodesFor(PlayerRole.Goalkeeper));
        Assert.NotEqual(CareerSquadRules.CodesFor(PlayerRole.CentreBack),CareerSquadRules.CodesFor(PlayerRole.RightBack));
        Assert.NotEqual(CareerSquadRules.CodesFor(PlayerRole.DefensiveMidfielder),CareerSquadRules.CodesFor(PlayerRole.AttackingMidfielder));
    }
    [Fact] public void All480RowsUsePersistedVisibleValuesWithNoAutomaticAssignments()
    {
        var q=PublishedWorldQueries.Open(EconomicSource);var total=0;
        foreach(var club in q.GetClubs())
        {
            var rows=q.GetSquad(club.Id).Select(p=>CareerSquadRules.Create(q.GetPlayer(club.Id,p.Id))).ToArray();Assert.Equal(24,rows.Select(r=>r.Player.Id).Distinct().Count());
            foreach(var row in rows)
            {
                Assert.Equal(SquadAssignment.Unassigned,row.Assignment);var details=q.GetPlayer(club.Id,row.Player.Id);
                var values=details.AttributeGroups.SelectMany(g=>g.Values).ToDictionary(v=>v.Code);
                Assert.Equal(CareerSquadRules.CodesFor(row.Position),row.Highlights.Select(v=>v.Code));
                Assert.All(row.Highlights,v=>{Assert.Equal(values[v.Code].Value,v.Value);Assert.InRange(v.Value,1,20);});
            }
            total+=rows.Length;
        }
        Assert.Equal(480,total);
    }
    [Fact] public void RolesAndFootballPositionsSortStablyRatherThanAlphabetically()
    {
        var q=PublishedWorldQueries.Open(EconomicSource);var club=q.GetClubs()[0];var rows=q.GetSquad(club.Id).Select(p=>CareerSquadRules.Create(q.GetPlayer(club.Id,p.Id))).ToArray();
        var assigned=rows.Select((r,i)=>r with{Assignment=(SquadAssignment)(i%4+1)}).ToArray();
        var sorted=CareerSquadRules.Sort(assigned);Assert.Equal(assigned.OrderBy(r=>r.Assignment).ThenBy(r=>CareerSquadRules.PositionOrder(r.Position)).ThenBy(r=>r.Player.Name,StringComparer.OrdinalIgnoreCase).Select(r=>r.Player.Id),sorted.Select(r=>r.Player.Id));
        Assert.Equal(sorted.Select(r=>r.Player.Id),CareerSquadRules.Sort(assigned.Reverse()).Select(r=>r.Player.Id));
        foreach(var sort in Enum.GetValues<CareerSquadSort>())Assert.Equal(24,CareerSquadRules.Sort(rows,sort,true).Select(r=>r.Player.Id).Distinct().Count());
        Assert.True(CareerSquadRules.PositionOrder(PlayerRole.RightWinger)<CareerSquadRules.PositionOrder(PlayerRole.Striker));
    }
    [Fact] public void ContextChangeOnlyChangesHighlightSelectionWithoutModifyingOriginalAttributes()
    {
        var q=PublishedWorldQueries.Open(EconomicSource);var c=q.GetClubs()[0];var detail=q.GetPlayer(c.Id,q.GetSquad(c.Id)[0].Id);var before=detail.AttributeGroups.SelectMany(g=>g.Values).ToArray();
        var a=CareerSquadRules.Create(detail,PlayerRole.Goalkeeper);var b=CareerSquadRules.Create(detail,PlayerRole.Striker);
        Assert.NotEqual(a.Highlights,b.Highlights);Assert.Equal(before,detail.AttributeGroups.SelectMany(g=>g.Values));
        Assert.Throws<InvalidDataException>(()=>CareerSquadRules.Create(detail with{AttributeGroups=Array.Empty<VisibleAttributeGroup>()}));
    }
    [Fact] public void PublicSelectorAndNoClubRoutesRespectRealEmployment()
    {
        var p=Profile();var c=EconomicStore().Create(Request(p,CareerStartMode.Unemployed));var free=new CareerWorkspace(c);
        free.Open(CareerPage.NoClub);Assert.Equal(CareerArea.Club,free.Area);Assert.False(free.CanOpen(CareerPage.PublicClubs));Assert.False(free.CanOpen(CareerPage.OwnSquad));
        var hired=new CareerWorkspace(EconomicStore().Create(Request(p,CareerStartMode.SignOffer,EconomicOffer(p))));
        Assert.False(hired.CanOpen(CareerPage.NoClub));Assert.False(hired.CanOpen(CareerPage.PublicClubs));hired.Open(CareerPage.OwnInformation);Assert.Equal(CareerArea.Club,hired.Area);
    }
}
