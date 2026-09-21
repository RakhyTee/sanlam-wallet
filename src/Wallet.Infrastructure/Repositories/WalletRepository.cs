using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wallet.Application;
using Wallet.Domain.Wallets;
using Wallet.Infrastructure.Persistence;
using DomainWallet = Wallet.Domain.Wallets.Wallet;

namespace Wallet.Infrastructure.Repositories;

public sealed class WalletRepository : IWalletRepository
{
    private const int SqliteConstraintErrorCode = 19;

    private readonly WalletDbContext _context;

    public WalletRepository(WalletDbContext context)
    {
        _context = context;
    }

    public Task<DomainWallet?> GetByIdAsync(Guid walletId, CancellationToken cancellationToken = default) =>
        _context.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == walletId, cancellationToken);

    public Task<DomainWallet?> GetForUpdateAsync(Guid walletId, CancellationToken cancellationToken = default) =>
        _context.Wallets
            .FirstOrDefaultAsync(w => w.Id == walletId, cancellationToken);

    public Task<Transaction?> GetByIdempotencyKeyAsync(Guid walletId, string idempotencyKey, CancellationToken cancellationToken = default) =>
        _context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.WalletId == walletId && t.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task SaveAsync(DomainWallet wallet, Transaction transaction, CancellationToken cancellationToken = default)
    {
        _context.Transactions.Add(transaction);

        await using var dbTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException($"Wallet {wallet.Id} was updated concurrently.", ex);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new DuplicateWithdrawalException(
                $"Withdrawal with idempotency key '{transaction.IdempotencyKey}' was already processed for wallet {wallet.Id}.", ex);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqliteException { SqliteErrorCode: SqliteConstraintErrorCode };
}
