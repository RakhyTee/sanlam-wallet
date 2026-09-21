using Wallet.Domain.Wallets;
using DomainWallet = Wallet.Domain.Wallets.Wallet;

namespace Wallet.Application;

public interface IWalletRepository
{
    Task<DomainWallet?> GetByIdAsync(Guid walletId, CancellationToken cancellationToken = default);

    Task<DomainWallet?> GetForUpdateAsync(Guid walletId, CancellationToken cancellationToken = default);

    Task<Transaction?> GetByIdempotencyKeyAsync(Guid walletId, string idempotencyKey, CancellationToken cancellationToken = default);

    Task SaveAsync(DomainWallet wallet, Transaction transaction, CancellationToken cancellationToken = default);

}
