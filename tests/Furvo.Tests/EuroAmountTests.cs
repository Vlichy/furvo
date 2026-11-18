using System.Globalization;
using Furvo.Core.Finance;
using Xunit;

namespace Furvo.Tests;

public sealed class EuroAmountTests
{
    [Theory]
    [InlineData("0", 0L)]
    [InlineData("0.01", 1L)]
    [InlineData("1.23", 123L)]
    [InlineData("1.230", 123L)]
    [InlineData("123456789.99", 12345678999L)]
    [InlineData("92233720368547758.07", long.MaxValue)]
    public void ConvertsEurosExactlyAndRoundTrips(string text, long cents)
    {
        var euros = decimal.Parse(text, CultureInfo.InvariantCulture);
        var amount = EuroAmount.FromEuros(euros);
        Assert.Equal(cents, amount.Cents);
        Assert.Equal(euros, amount.Euros);
        Assert.Equal(amount, EuroAmount.FromCents(cents));
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("1.239")]
    [InlineData("-1.239")]
    public void RejectsFractionsInsteadOfTruncatingOrRounding(string text)
    {
        var value = decimal.Parse(text, CultureInfo.InvariantCulture);
        Assert.Throws<InvalidDataException>(() => EuroAmount.FromEuros(value, MoneySign.Signed));
    }

    [Fact]
    public void NegativeAmountsRequireExplicitSignedContext()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EuroAmount.FromEuros(-1.23m));
        Assert.Throws<ArgumentOutOfRangeException>(() => EuroAmount.FromCents(-123));
        var adjustment = EuroAmount.FromEuros(-1.23m, MoneySign.Signed);
        Assert.Equal(-123, adjustment.Cents);
        Assert.Equal(-1.23m, adjustment.Euros);
        var minimum = EuroAmount.FromCents(long.MinValue, MoneySign.Signed);
        Assert.Equal(minimum, EuroAmount.FromEuros(minimum.Euros, MoneySign.Signed));
        Assert.Throws<ArgumentOutOfRangeException>(() => EuroAmount.FromCents(0, (MoneySign)99));
    }

    [Theory]
    [InlineData("92233720368547758.08")]
    [InlineData("-92233720368547758.09")]
    [InlineData("79228162514264337593543950335")]
    public void RejectsStorageOrMultiplicationOverflow(string text)
    {
        var value = decimal.Parse(text, CultureInfo.InvariantCulture);
        Assert.Throws<OverflowException>(() => EuroAmount.FromEuros(value, MoneySign.Signed));
    }
}
