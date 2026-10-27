using System.Xml.Linq;
using Furvo.Core.Domain;
using Furvo.Presentation.Components;
using Xunit;

namespace Furvo.Tests;

/// <summary>Verifica límites de color y rutas de retrato del código real de presentación, sin efectos sobre dominio ni persistencia.</summary>
public sealed class PlayerSheetPresentationTests
{
    [Theory]
    [InlineData(1, AttributeValueStyle.LowColor)] [InlineData(5, AttributeValueStyle.LowColor)]
    [InlineData(6, AttributeValueStyle.NeutralColor)] [InlineData(10, AttributeValueStyle.NeutralColor)]
    [InlineData(11, AttributeValueStyle.GoodColor)] [InlineData(15, AttributeValueStyle.GoodColor)]
    [InlineData(16, AttributeValueStyle.HighColor)] [InlineData(20, AttributeValueStyle.HighColor)]
    public void AttributeColorsRespectEveryApprovedBoundary(int value, string color) => Assert.Equal(color, AttributeValueStyle.ColorHex(value));

    [Theory]
    [InlineData(0)] [InlineData(21)]
    public void InvalidRatingsAreNotSilentlyRescaledForDisplay(int value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => AttributeValueStyle.ColorHex(value));

    [Fact]
    public void PortraitCandidatesUseTheStableIdAndOnlyPackagedImageLocations()
    {
        var value = Guid.Parse("12345678-1234-4234-8234-123456789abc");
        var id = new PlayerId(value);
        var candidates = PortraitResourcePaths.ForPlayer(id);
        Assert.Equal(new[] { "res://Assets/Portraits/Players/12345678123442348234123456789abc.png",
            "res://Assets/Portraits/Players/12345678123442348234123456789abc.webp" }, candidates);
        Assert.Equal(candidates, PortraitResourcePaths.ForPlayer(new PlayerId(value)));
        Assert.NotEqual(candidates[0], PortraitResourcePaths.ForPlayer(PlayerId.New())[0]);
        Assert.Equal(value, id.Value);
        Assert.Equal("res://Assets/Portraits/avatar.svg", PortraitResourcePaths.Placeholder);
        Assert.Throws<ArgumentException>(() => PortraitResourcePaths.ForPlayer(default));
    }

    [Fact]
    public void GenericAvatarIsOriginalVectorWithThreeToFourAspectAndNoExternalImages()
    {
        var svg = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "avatar.svg"));
        var root = svg.Root!;
        Assert.Equal("svg", root.Name.LocalName);
        Assert.Equal("0 0 168 224", root.Attribute("viewBox")!.Value);
        Assert.Equal(3m / 4m, decimal.Parse(root.Attribute("width")!.Value) / decimal.Parse(root.Attribute("height")!.Value));
        Assert.DoesNotContain(root.Descendants(), e => e.Name.LocalName is "image" or "script" or "foreignObject");
        Assert.DoesNotContain(root.Descendants().Attributes(), a => a.Name.LocalName is "href" or "src");
    }
}
