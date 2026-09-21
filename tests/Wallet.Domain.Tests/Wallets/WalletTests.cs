using Wallet.Domain.Wallets;
using DomainWallet = Wallet.Domain.Wallets.Wallet;

namespace Wallet.Domain.Tests.Wallets;

public class WalletTests
{
    [Fact]
    public void Withdraw_SufficientFunds_DecrementsBalanceAndIncrementsVersion()
    {
        var wallet = new DomainWallet(Guid.NewGuid(), new Money(1000m, "ZAR"));

        var result = wallet.Withdraw(400m);

        Assert.True(result.IsSuccess);
        Assert.Equal(600m, wallet.Balance.Amount);
        Assert.Equal("ZAR", wallet.Balance.Currency);
        Assert.Equal(1, wallet.Version);
    }

    [Fact]
    public void Withdraw_ExactBalance_SucceedsAndLeavesZeroBalance()
    {
        var wallet = new DomainWallet(Guid.NewGuid(), new Money(500m, "ZAR"));

        var result = wallet.Withdraw(500m);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, wallet.Balance.Amount);
    }

    [Fact]
    public void Withdraw_AmountGreaterThanBalance_ReturnsInsufficientFundsAndDoesNotMutate()
    {
        var wallet = new DomainWallet(Guid.NewGuid(), new Money(400m, "ZAR"));

        var result = wallet.Withdraw(500m);

        Assert.True(result.IsFailure);
        Assert.Equal(WalletErrors.InsufficientFunds.Code, result.Error!.Code);
        Assert.Equal(400m, wallet.Balance.Amount);
        Assert.Equal(0, wallet.Version);
    }

    [Fact]
    public void Withdraw_MultipleSuccessiveWithdrawals_AccumulatesVersion()
    {
        var wallet = new DomainWallet(Guid.NewGuid(), new Money(1000m, "ZAR"));

        wallet.Withdraw(100m);
        wallet.Withdraw(100m);
        wallet.Withdraw(100m);

        Assert.Equal(700m, wallet.Balance.Amount);
        Assert.Equal(3, wallet.Version);
    }
}
