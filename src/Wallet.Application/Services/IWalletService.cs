using Wallet.Application.Models;
using Wallet.Domain.Common;

namespace Wallet.Application.Services;

public interface IWalletService
{
    Task<Result<BalanceDto>> GetBalanceAsync(Guid walletId, CancellationToken cancellationToken = default);

    Task<Result<WithdrawalDto>> WithdrawAsync(Guid walletId, decimal amount, string idempotencyKey, CancellationToken cancellationToken = default);

}
