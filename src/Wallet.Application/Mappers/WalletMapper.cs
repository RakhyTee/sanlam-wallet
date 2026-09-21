using Wallet.Application.Models;

namespace Wallet.Application.Mappers;

public static class WalletMapper
{
    public static WithdrawalDto ToWithdrawalDto(Domain.Wallets.Transaction transaction) => new()
    {
        WithdrawalId = transaction.Id,
        WalletId = transaction.WalletId,
        Amount = transaction.Amount,
        BalanceAfter = transaction.BalanceAfter,
        RequestedAt = transaction.CreatedAtUtc
    };

    public static BalanceDto ToBalanceDto(Domain.Wallets.Wallet wallet, DateTime asOfUtc) => new()
    {
        WalletId = wallet.Id,
        Balance = wallet.Balance.Amount,
        Currency = wallet.Balance.Currency,
        AsOfUtc = asOfUtc
    };
}
