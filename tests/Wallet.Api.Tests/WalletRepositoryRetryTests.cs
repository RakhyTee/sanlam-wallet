using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wallet.Application;
using Wallet.Domain.Wallets;
using Wallet.Infrastructure.Persistence;
using Wallet.Infrastructure.Repositories;
using DomainWallet = Wallet.Domain.Wallets.Wallet;

namespace Wallet.Api.Tests;

// Regression test for a real bug: on a DbUpdateConcurrencyException, EF's identity map used to
// hand a retry (same DbContext) back the same stale, already-mutated Wallet instance instead of
// fresh data, and the failed Transaction stayed tracked as Added, so the retry always
// self-conflicted on the unique (WalletId, IdempotencyKey) index and could never succeed.
// WalletRepository.SaveAsync now calls ChangeTracker.Clear() on failure to fix this.
public class WalletRepositoryRetryTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"retry-test-{Guid.NewGuid():N}.db");
    private readonly WalletDbContext _context;
    private readonly WalletRepository _repository;

    public WalletRepositoryRetryTests()
    {
        var options = new DbContextOptionsBuilder<WalletDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _context = new WalletDbContext(options);
        _context.Database.Migrate();
        _repository = new WalletRepository(_context);
    }

    [Fact]
    public async Task Retry_AfterConcurrencyConflict_FetchesFreshWalletAndSucceeds()
    {
        var walletId = Guid.NewGuid();
        _context.Wallets.Add(new DomainWallet(walletId, new Money(1000m, "ZAR")));
        await _context.SaveChangesAsync();

        // Attempt 1: load and mutate in-memory, exactly like WalletService.WithdrawAsync does.
        var wallet1 = await _repository.GetForUpdateAsync(walletId);
        Assert.NotNull(wallet1);
        wallet1!.Withdraw(300m);

        var transaction1 = new Transaction
        {
            Id = Guid.NewGuid(),
            WalletId = walletId,
            Type = Domain.Enums.TransactionType.Withdrawal,
            Amount = 300m,
            BalanceAfter = wallet1.Balance.Amount,
            IdempotencyKey = "key-1",
            CreatedAt = DateTime.UtcNow
        };

        // Simulate a concurrent writer that already committed, via a second, independent DbContext,
        // so wallet1's captured original Version (0) no longer matches what's in the database.
        var otherOptions = new DbContextOptionsBuilder<WalletDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        await using (var otherContext = new WalletDbContext(otherOptions))
        {
            var otherWallet = await otherContext.Wallets.FirstAsync(w => w.Id == walletId);
            otherWallet.Withdraw(150m);
            await otherContext.SaveChangesAsync();
        }

        // Attempt 1's save fails with a concurrency conflict, exactly like production.
        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => _repository.SaveAsync(wallet1, transaction1, default));

        // --- Retry, using the SAME repository/DbContext instance, exactly as WalletService's loop does ---

        var wallet2 = await _repository.GetForUpdateAsync(walletId);

        // Fixed: a genuinely fresh instance reflecting the real current balance (1000 - 150 = 850
        // from the "other" writer), not attempt 1's stale, doubly-tracked state.
        Assert.NotSame(wallet1, wallet2);
        Assert.Equal(850m, wallet2!.Balance.Amount);

        var withdrawResult = wallet2.Withdraw(300m);
        Assert.True(withdrawResult.IsSuccess);

        var transaction2 = new Transaction
        {
            Id = Guid.NewGuid(),
            WalletId = walletId,
            Type = Domain.Enums.TransactionType.Withdrawal,
            Amount = 300m,
            BalanceAfter = wallet2.Balance.Amount,
            IdempotencyKey = "key-1",
            CreatedAt = DateTime.UtcNow
        };

        // Fixed: the retry actually succeeds now, instead of self-conflicting on the unique index.
        await _repository.SaveAsync(wallet2, transaction2, default);

        var finalWallet = await _repository.GetByIdAsync(walletId);
        Assert.Equal(550m, finalWallet!.Balance.Amount); // 1000 - 150 (other writer) - 300 (this retry)
    }

    public void Dispose()
    {
        _context.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}
