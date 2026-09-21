using Wallet.Api.Models.Responses;
using Wallet.Application.Models;

namespace Wallet.Api.Mappers;

public static class WalletResponseMapper
{
    public static BalanceResponse ToResponse(BalanceDto dto) => new()
    {
        WalletId = dto.WalletId,
        Balance = dto.Balance,
        Currency = dto.Currency,
        AsOfUtc = dto.AsOfUtc
    };

    public static WithdrawalResponse ToResponse(WithdrawalDto dto) => new()
    {
        WithdrawalId = dto.WithdrawalId,
        WalletId = dto.WalletId,
        Amount = dto.Amount,
        BalanceAfter = dto.BalanceAfter,
        RequestedAt = dto.RequestedAt
    };
}
