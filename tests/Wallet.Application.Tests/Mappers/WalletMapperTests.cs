using Wallet.Application.Mappers;
using Wallet.Domain.Enums;
using Wallet.Domain.Wallets;
using DomainWallet = Wallet.Domain.Wallets.Wallet;

namespace Wallet.Application.Tests.Mappers;

public class WalletMapperTests
{
    [Fact]
    public void ToBalanceDto_MapsAllFields()
    {
        var wallet = new DomainWallet(Guid.NewGuid(), new Money(250m, "ZAR"));
        var asOf = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var dto = WalletMapper.ToBalanceDto(wallet, asOf);

        Assert.Equal(wallet.Id, dto.WalletId);
        Assert.Equal(250m, dto.Balance);
        Assert.Equal("ZAR", dto.Currency);
        Assert.Equal(asOf, dto.AsOf);
    }

    [Fact]
    public void ToWithdrawalDto_MapsAllFields()
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            WalletId = Guid.NewGuid(),
            Type = TransactionType.Withdrawal,
            Amount = 100m,
            BalanceAfter = 900m,
            IdempotencyKey = "key-1",
            CreatedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        var dto = WalletMapper.ToWithdrawalDto(transaction);

        Assert.Equal(transaction.Id, dto.WithdrawalId);
        Assert.Equal(transaction.WalletId, dto.WalletId);
        Assert.Equal(100m, dto.Amount);
        Assert.Equal(900m, dto.BalanceAfter);
        Assert.Equal(transaction.CreatedAt, dto.RequestedAt);
    }
}
