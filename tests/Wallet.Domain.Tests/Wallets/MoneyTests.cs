using Wallet.Domain.Wallets;

namespace Wallet.Domain.Tests.Wallets;

public class MoneyTests
{
    [Fact]
    public void Constructor_ValidValues_SetsProperties()
    {
        var money = new Money(100m, "ZAR");

        Assert.Equal(100m, money.Amount);
        Assert.Equal("ZAR", money.Currency);
    }

    [Fact]
    public void Constructor_ZeroAmount_Succeeds()
    {
        var money = new Money(0m, "ZAR");

        Assert.Equal(0m, money.Amount);
    }

    [Fact]
    public void Constructor_NegativeAmount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-1m, "ZAR"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_InvalidCurrency_Throws(string? currency)
    {
        Assert.Throws<ArgumentException>(() => new Money(100m, currency!));
    }
}
