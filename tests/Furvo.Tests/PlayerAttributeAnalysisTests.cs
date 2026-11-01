using System.Security.Cryptography;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
using Furvo.Persistence;
using Xunit;
namespace Furvo.Tests;
public sealed partial class CareerCreationTests
{
    [Theory]
    [InlineData(AttributeCategory.Technical,17)]
    [InlineData(AttributeCategory.Mental,16)]
    [InlineData(AttributeCategory.Physical,8)]
    [InlineData(AttributeCategory.Goalkeeper,10)]
    public void PlayerAnalysisSelectsAndComparesOnlyRequestedPublicCategory(AttributeCategory category,int count)
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteClubSquadStore(file);var players=store.Details(store.Teams()[0].Id);
        foreach(var sort in Enum.GetValues<AttributeAnalysisSort>()){
            var view=PlayerAttributeAnalysis.Select(players,players[0].Player.Id,players[1].Player.Id,category,sort);
            Assert.Equal(players[0].Player.Id,view.Player.Id);Assert.Equal(players[1].Player.Id,view.Comparison!.Id);Assert.Equal(count,view.Attributes.Count);
            Assert.All(view.Attributes,a=>{Assert.False(PlayerAttributeCatalog.Get(a.Code).IsHidden);Assert.Equal(category,PlayerAttributeCatalog.Get(a.Code).Category);Assert.InRange(a.Value,1,20);Assert.InRange(a.ComparisonValue!.Value,1,20);});
            Assert.Equal(view.Attributes,PlayerAttributeAnalysis.Select(players,players[0].Player.Id,players[1].Player.Id,category,sort).Attributes);
        }
    }
    [Fact] public void PlayerAnalysisRejectsHiddenCategoriesForeignPlayersAndSelfComparison()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteClubSquadStore(file);var players=store.Details(store.Teams()[0].Id);var selected=players[0].Player.Id;
        Assert.Throws<ArgumentException>(()=>PlayerAttributeAnalysis.Select(players,selected,null,AttributeCategory.Hidden));
        Assert.Throws<ArgumentException>(()=>PlayerAttributeAnalysis.Select(players,selected,selected,AttributeCategory.Technical));
        Assert.Throws<KeyNotFoundException>(()=>PlayerAttributeAnalysis.Select(players,new PlayerId(Guid.NewGuid()),null,AttributeCategory.Technical));
        Assert.Throws<KeyNotFoundException>(()=>PlayerAttributeAnalysis.Select(players,selected,new PlayerId(Guid.NewGuid()),AttributeCategory.Technical));
        Assert.Null(PlayerAttributeAnalysis.Select(players,selected,null,AttributeCategory.Technical).Comparison);
    }
    [Fact] public void MalformedPublicProjectionCannotLeakHiddenAttributesIndirectly()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteClubSquadStore(file);var original=store.Details(store.Teams()[0].Id)[0];var hidden=PlayerAttributeCatalog.All.First(a=>a.IsHidden).Code;
        var malicious=original with{AttributeGroups=original.AttributeGroups.Select(g=>g.Category==AttributeCategory.Technical?g with{Values=g.Values.Append(new(hidden,999)).ToArray()}:g).Append(new(AttributeCategory.Hidden,new[]{new VisibleAttribute(hidden,999)})).ToArray()};
        var view=PlayerAttributeAnalysis.Select(new[]{malicious},original.Player.Id,null,AttributeCategory.Technical);Assert.Equal(17,view.Attributes.Count);Assert.DoesNotContain(view.Attributes,a=>a.Code==hidden);
        Assert.Empty(PlayerAttributeAnalysis.History(original.Player.Id,AttributeCategory.Technical,new[]{new RecordedPlayerAttributes(Guid.NewGuid(),new(2026,7,1),original.Player.Id,malicious.AttributeGroups)}));
    }
    [Fact] public void AttributeOrderingUsesValuesAndSharedPositionRelevanceWithStableTies()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteClubSquadStore(file);var players=store.Details(store.Teams()[0].Id);var selected=players.First(p=>p.Player.Position==PlayerRole.Goalkeeper);
        var ascending=PlayerAttributeAnalysis.Select(players,selected.Player.Id,null,AttributeCategory.Goalkeeper,AttributeAnalysisSort.Value);
        var descending=PlayerAttributeAnalysis.Select(players,selected.Player.Id,null,AttributeCategory.Goalkeeper,AttributeAnalysisSort.Value,true);
        Assert.Equal(ascending.Attributes.Select(a=>a.Value).Order(),ascending.Attributes.Select(a=>a.Value));Assert.Equal(descending.Attributes.Select(a=>a.Value).OrderDescending(),descending.Attributes.Select(a=>a.Value));
        var relevance=PlayerAttributeAnalysis.Select(players,selected.Player.Id,null,AttributeCategory.Goalkeeper,AttributeAnalysisSort.Relevance);Assert.All(relevance.Attributes,a=>Assert.Contains(a.Code,PositionInsightCatalog.RelevantFor(PlayerRole.Goalkeeper)));
    }
    [Fact] public void AnalysisSelectionComparisonAndHistoryDoNotWriteCareerOrCreateReports()
    {
        var (_,file,_)=TacticalCareer();var before=SHA256.HashData(File.ReadAllBytes(file));var store=new SqliteClubSquadStore(file);var team=store.Teams()[0].Id;var players=store.Details(team);
        foreach(var p in players.Take(3))foreach(var category in Enum.GetValues<AttributeCategory>().Where(c=>c!=AttributeCategory.Hidden)){
            var comparison=players.First(other=>other.Player.Id!=p.Player.Id).Player.Id;_=PlayerAttributeAnalysis.Select(players,p.Player.Id,comparison,category,AttributeAnalysisSort.Relevance);_=PlayerAttributeAnalysis.History(p.Player.Id,category,Array.Empty<RecordedPlayerAttributes>());
        }
        Assert.Empty(new SqliteSportsStore(file).Reports(team));Assert.Equal(before,SHA256.HashData(File.ReadAllBytes(file)));
    }
    [Fact] public void EvolutionUsesOnlyRecordedChangesAcrossDistinctUnambiguousDates()
    {
        var (_,file,_)=TacticalCareer();var store=new SqliteClubSquadStore(file);var player=store.Details(store.Teams()[0].Id)[0];var date=new DateOnly(2026,7,1);var attribute=player.AttributeGroups.First(g=>g.Category==AttributeCategory.Technical).Values[0];var changed=attribute.Value==20?19:attribute.Value+1;
        IReadOnlyList<VisibleAttributeGroup> Values(int value)=>new[]{new VisibleAttributeGroup(AttributeCategory.Technical,new[]{new VisibleAttribute(attribute.Code,value)})};
        var a=new RecordedPlayerAttributes(Guid.NewGuid(),date,player.Player.Id,Values(attribute.Value));var b=new RecordedPlayerAttributes(Guid.NewGuid(),date.AddDays(7),player.Player.Id,Values(changed));
        Assert.Empty(PlayerAttributeAnalysis.History(player.Player.Id,AttributeCategory.Technical,new[]{a}));
        var history=PlayerAttributeAnalysis.History(player.Player.Id,AttributeCategory.Technical,new[]{b,a});var delta=Assert.Single(history);Assert.Equal(attribute.Value,delta.Before);Assert.Equal(changed,delta.After);Assert.Equal(date,delta.From);Assert.Equal(date.AddDays(7),delta.To);
        Assert.Empty(PlayerAttributeAnalysis.History(player.Player.Id,AttributeCategory.Technical,new[]{a,b with{Date=date}}));
        Assert.Throws<ArgumentException>(()=>PlayerAttributeAnalysis.History(player.Player.Id,AttributeCategory.Hidden,new[]{a,b}));
    }
}
