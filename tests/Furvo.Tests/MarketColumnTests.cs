using System.Security.Cryptography;
using Furvo.Core.Domain;
using Furvo.Core.Queries;
using Furvo.Persistence;
using Furvo.Presentation.Components;
using Xunit;
namespace Furvo.Tests;

public sealed class MarketColumnTests
{
    private static MarketSnapshot Sample()
    {
        var club=new ClubSummary(new ClubId(Guid.Parse("10000000-0000-4000-8000-000000000001")),"Club prueba",null,2);
        var names=new[]{"Adrián Del Fresno Aldecoa","Adrián del Fresno Pérez","Bruno Prueba","Carlos Prueba"};
        var values=new[]{2,10,9,20};
        var players=names.Select((name,i)=>{
            var p=new Player(new PlayerId(Guid.Parse("20000000-0000-4000-8000-00000000000"+(i+1))),name,new DateOnly(2001,7,1),"España",PlayerPosition.Defender,new PlayerAttributes(PlayerAttributeCatalog.All.ToDictionary(a=>a.Code,_=>values[i])),new PlayerPositions(PlayerRole.CentreBack));
            return new MarketPlayer(WorldQueries.ProjectPlayer(p,club,new DateOnly(2026,7,1)),club.Id.Value,club.Name,"First",null);
        }).ToArray();
        return new(Guid.NewGuid(),new DateOnly(2026,7,1),new[]{club},players);
    }
    [Theory]
    [InlineData("Adrián",2)][InlineData("adrian",2)][InlineData("ADRIÁN",2)][InlineData("Del Fresno",2)]
    [InlineData("Adrián Del Fresno Aldecoa",1)][InlineData("Fresno Aldecoa",1)]
    [InlineData("  adrian   del  fresno   aldecoa  ",1)][InlineData("Adrián\tDel\nFresno",2)]
    [InlineData("fresno ALDE",1)][InlineData("",4)]
    public void NamesNormalizeAccentsCaseAndSpacesWithoutChangingStoredNames(string query,int count)
    {var s=Sample();var names=s.Players.Select(p=>p.Details.Player.Name).ToArray();Assert.Equal(count,MarketQueries.Search(s,new(Name:query)).Count);Assert.Equal(names,s.Players.Select(p=>p.Details.Player.Name));}
    [Theory][InlineData("13","17",13,17)][InlineData("13","",13,20)][InlineData("","17",1,17)][InlineData("","",1,20)][InlineData(" 1 ","20",1,20)]
    public void AttributeBoundsAreTypedAndBlankBoundsOpenTheScale(string min,string max,int low,int high)
    {Assert.Equal((low,high),MarketQueries.ParseAttributeRange(min,max));}
    [Theory][InlineData("0","20")][InlineData("1","21")][InlineData("17","13")][InlineData("13.5","20")][InlineData("abc","20")][InlineData("-1","20")][InlineData("1","2,5")][InlineData("1","1e1")]
    public void InvalidTypedBoundsAreRejectedWithoutClamping(string min,string max)=>Assert.Throws<InvalidDataException>(()=>MarketQueries.ParseAttributeRange(min,max));
    [Fact] public void DefaultsUseSharedRelevantPublicAttributesForEveryPositionAndAnIndependentAllView()
    {
        var preferences=new MarketColumnPreferences();
        foreach(var role in Enum.GetValues<PlayerRole>()){
            Assert.Equal(PositionInsightCatalog.RelevantFor(role),preferences.Columns(role));
            Assert.All(preferences.Columns(role),code=>Assert.False(PlayerAttributeCatalog.Get(code).IsHidden));
            Assert.Equal(preferences.Columns(role).Count,preferences.Columns(role).Distinct().Count());
        }
        Assert.NotEmpty(preferences.Columns(null));
        Assert.Throws<ArgumentOutOfRangeException>(()=>preferences.Columns((PlayerRole)999));
    }
    [Fact] public void AnyOfAll51PublicAttributesCanBeSelectedOrderedRemovedAndRestoredIndependently()
    {
        var p=new MarketColumnPreferences();var all=PlayerAttributeCatalog.All.Where(a=>!a.IsHidden).Select(a=>a.Code).ToArray();
        p.Set(PlayerRole.CentreBack,all);Assert.Equal(51,p.Columns(PlayerRole.CentreBack).Count);
        p.Set(PlayerRole.CentreBack,MarketColumnPreferences.Move(p.Columns(PlayerRole.CentreBack),0,1));Assert.Equal(all[0],p.Columns(PlayerRole.CentreBack)[1]);
        p.Set(PlayerRole.CentreBack,p.Columns(PlayerRole.CentreBack).Where(c=>c!=all[0]));Assert.DoesNotContain(all[0],p.Columns(PlayerRole.CentreBack));
        p.Set(PlayerRole.Goalkeeper,new[]{"short_passing","reflexes"});p.Set(null,new[]{"pace"});
        p.Reset(PlayerRole.CentreBack);Assert.Equal(PositionInsightCatalog.RelevantFor(PlayerRole.CentreBack),p.Columns(PlayerRole.CentreBack));
        Assert.Equal(new[]{"short_passing","reflexes"},p.Columns(PlayerRole.Goalkeeper));Assert.Equal(new[]{"pace"},p.Columns(null));
        p.Set(PlayerRole.Striker,Array.Empty<string>());Assert.Empty(p.Columns(PlayerRole.Striker));
    }
    [Fact] public void PreferencesRejectEveryHiddenCodeAndDoNotExposeItThroughLookupOrOrdering()
    {
        var p=new MarketColumnPreferences();var s=Sample();
        foreach(var hidden in PlayerAttributeCatalog.All.Where(a=>a.IsHidden)){
            Assert.Throws<InvalidDataException>(()=>p.Set(null,new[]{hidden.Code}));
            Assert.Throws<InvalidDataException>(()=>MarketQueries.Search(s,new(),MarketSort.Attribute,false,hidden.Code));
            Assert.Throws<InvalidDataException>(()=>MarketQueries.AttributeValue(s.Players[0],hidden.Code));
            Assert.Throws<InvalidDataException>(()=>MarketColumnPreferences.Deserialize(System.Text.Json.JsonSerializer.Serialize(new {Version=1,Positions=new Dictionary<string,string[]>{{"All",new[]{hidden.Code}}}})));
        }
        Assert.Throws<InvalidDataException>(()=>p.Set(null,new[]{"pace","pace"}));
        Assert.Throws<InvalidDataException>(()=>p.Set(null,new[]{"invented"}));
        Assert.Throws<InvalidDataException>(()=>MarketQueries.Search(s,new(),MarketSort.Attribute,false));
    }
    [Fact] public void PreferencesPersistPerUserAcrossInstancesAndPositionsWithoutSharingMutableLists()
    {
        var root=Path.Combine(Path.GetTempPath(),"furvo-market-columns-"+Guid.NewGuid());var file=Path.Combine(root,"ui","market-columns.v1.json");
        try{
            var a=MarketColumnPreferences.Load(file);var input=new[]{"pace","marking","short_passing"};a.Set(PlayerRole.CentreBack,input);input[0]="strength";
            a.Set(PlayerRole.Goalkeeper,new[]{"handling","reflexes"});a.Set(null,Array.Empty<string>());a.Save(file);
            var b=MarketColumnPreferences.Load(file);Assert.Equal(new[]{"pace","marking","short_passing"},b.Columns(PlayerRole.CentreBack));Assert.Equal(new[]{"handling","reflexes"},b.Columns(PlayerRole.Goalkeeper));Assert.Empty(b.Columns(null));
            b.Reset(PlayerRole.CentreBack);b.Save(file);var c=MarketColumnPreferences.Load(file);Assert.Equal(MarketColumnPreferences.Defaults(PlayerRole.CentreBack),c.Columns(PlayerRole.CentreBack));Assert.Equal(b.Columns(PlayerRole.Goalkeeper),c.Columns(PlayerRole.Goalkeeper));Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(file)!,"*.tmp"));
            Assert.Equal(new[]{"pace","marking","short_passing"},a.Columns(PlayerRole.CentreBack));
        }finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
    [Theory][InlineData("{}")][InlineData("""{"Version":2,"Positions":{}}""")][InlineData("""{"Version":1,"Positions":{"999":[]}}""")][InlineData("""{"Version":1,"Positions":{"All":null}}""")]
    public void UnsupportedOrInvalidPreferenceDocumentsAreExplicitlyRejected(string json)=>Assert.Throws<InvalidDataException>(()=>MarketColumnPreferences.Deserialize(json));
    [Theory][InlineData(false)][InlineData(true)]
    public void AttributeSortIsNumericStableAndIndependentOfFilterAndDisplayedColumns(bool descending)
    {
        var s=Sample();var p=new MarketColumnPreferences();p.Set(PlayerRole.CentreBack,new[]{"pace"});var filter=new MarketFilter(Position:PlayerRole.CentreBack,MinimumAge:20,Attribute:"stamina",MinimumValue:9,MaximumValue:20);
        var rows=MarketQueries.Search(s,filter,MarketSort.Attribute,descending,"pace");
        Assert.Equal(descending?new[]{20,10,9}:new[]{9,10,20},rows.Select(r=>MarketQueries.AttributeValue(r,"pace")!.Value));
        Assert.Equal(rows.Select(r=>r.Details.Player.Id),MarketQueries.Search(s with{Players=s.Players.Reverse().ToArray()},filter,MarketSort.Attribute,descending,"pace").Select(r=>r.Details.Player.Id));
        Assert.DoesNotContain("stamina",p.Columns(PlayerRole.CentreBack));
        Assert.All(s.Players,row=>Assert.All(PlayerAttributeCatalog.All.Where(a=>!a.IsHidden),a=>Assert.Equal(row.Details.AttributeGroups.SelectMany(g=>g.Values).Single(v=>v.Code==a.Code).Value,MarketQueries.AttributeValue(row,a.Code))));
    }
    [Fact] public void HidingTheSortColumnFallsBackWithoutLosingFiltersOrSelectionAndOtherColumnsKeepAttributeOrder()
    {
        var s=Sample();var context=new MarketSearchContext(s.Career){Sort=MarketSort.Attribute,SortAttribute="pace",Descending=true,Selected=s.Players[1].Details.Player.Id,Applied=new(Attribute:"stamina",MinimumValue:9),Name="Prueba",Scroll=45,HorizontalScroll=300,MinimumValueText="9",MaximumValueText="20"};
        context.ReconcileSort(new[]{"marking","pace"});Assert.Equal(MarketSort.Attribute,context.Sort);
        context.ReconcileSort(new[]{"marking"});Assert.Equal(MarketSort.Name,context.Sort);Assert.Null(context.SortAttribute);Assert.False(context.Descending);Assert.Equal("stamina",context.Applied.Attribute);Assert.NotNull(context.Selected);Assert.Equal(300,context.HorizontalScroll);
        var other=new MarketSearchContext(Guid.NewGuid());Assert.Null(other.Selected);Assert.Equal(new MarketFilter(),other.Applied);
    }
}

public sealed partial class CareerCreationTests
{
    [Fact] public void ColumnPreferencesAndNormalizedSearchLeaveEntireCareerAndInitialWorldUnchanged()
    {
        var (_,file,_)=TacticalCareer();var before=SHA256.HashData(File.ReadAllBytes(file));var foundation=SHA256.HashData(File.ReadAllBytes(EconomicSource));var snapshot=new SqliteMarketQueries(file).Read();
        var preferences=new MarketColumnPreferences();preferences.Set(PlayerRole.CentreBack,PlayerAttributeCatalog.All.Where(a=>!a.IsHidden).Select(a=>a.Code));var path=Path.Combine(_root,"interface","market.json");preferences.Save(path);
        var restored=MarketColumnPreferences.Load(path);Assert.Equal(51,restored.Columns(PlayerRole.CentreBack).Count);
        foreach(var code in restored.Columns(PlayerRole.CentreBack)){
            var rows=MarketQueries.Search(snapshot,new(Name:" adrian ",Attribute:"stamina",MinimumValue:1),MarketSort.Attribute,true,code);
            foreach(var row in rows)_=MarketQueries.Player(snapshot,row.Details.Player.Id);
        }
        Assert.Equal(before,SHA256.HashData(File.ReadAllBytes(file)));Assert.Equal(foundation,SHA256.HashData(File.ReadAllBytes(EconomicSource)));
    }
}
