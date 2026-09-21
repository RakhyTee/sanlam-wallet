using Microsoft.Extensions.Logging;
using Wallet.Application.Abstractions;
using Wallet.Application.Mappers;
using Wallet.Application.Models;
using Wallet.Domain.Common;
using Wallet.Domain.Enums;
using Wallet.Domain.Events;
using Wallet.Domain.Wallets;

namespace Wallet.Application.Services;

public class WalletService : IWalletService
{
    private const int MaxAttempts = 2;

    private readonly IWalletRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<WalletService> _logger;

    public WalletService(IWalletRepository repository, IEventPublisher eventPublisher, TimeProvider timeProvider,
            ILogger<WalletService> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result<BalanceDto>> GetBalanceAsync(Guid walletId,
            CancellationToken cancellationToken = default)
    {
        var wallet = await _repository.GetByIdAsync(walletId, cancellationToken);
        if (wallet is null)
            return Result.Fail<BalanceDto>(WalletErrors.NotFound);

        return Result.Ok(new BalanceDto
        {
            WalletId = wallet.Id,
            Balance = wallet.Balance.Amount,
            Currency = wallet.Balance.Currency,
            AsOf = _timeProvider.GetUtcNow().UtcDateTime
        });
    }

    public async Task<Result<WithdrawalDto>> WithdrawAsync(Guid walletId, decimal amount, string idempotencyKey,
                CancellationToken cancellationToken = default)
    {
        
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var wallet = await _repository.GetForUpdateAsync(walletId, cancellationToken);
            if (wallet is null)
                return Result.Fail<WithdrawalDto>(WalletErrors.NotFound);

            var existing = await _repository.GetByIdempotencyKeyAsync(walletId, idempotencyKey, cancellationToken);
            if (existing is not null)
                return Result.Ok(WalletMapper.ToWithdrawalDto(existing));

            var withdrawResult = wallet.Withdraw(amount);
            if (withdrawResult.IsFailure)
                return Result.Fail<WithdrawalDto>(withdrawResult.Error!);

            var requestedAt = _timeProvider.GetUtcNow().UtcDateTime;

            var transaction = new Domain.Wallets.Transaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Type = TransactionType.Withdrawal,
                Amount = amount,
                BalanceAfter = wallet.Balance.Amount,
                IdempotencyKey = idempotencyKey,
                CreatedAt = requestedAt
            };

            try
            {
                await _repository.SaveAsync(wallet, transaction, cancellationToken);

                _logger.LogInformation(
                    "Withdrawal {WithdrawalId} succeeded for wallet {WalletId}, balance now {Balance}",
                    transaction.Id, wallet.Id, wallet.Balance.Amount);

                await _eventPublisher.PublishAsync(new FundsWithdrawn
                {
                    EventId = Guid.NewGuid(),
                    WalletId = wallet.Id,
                    WithdrawalId = transaction.Id,
                    Amount = amount,
                    Currency = wallet.Balance.Currency,
                    BalanceAfter = transaction.BalanceAfter,
                    RequestedAt = requestedAt
                }, cancellationToken);

                return Result.Ok(WalletMapper.ToWithdrawalDto(transaction));
            }
            catch (ConcurrencyConflictException)
            {
                if (attempt >= MaxAttempts)
                    return Result.Fail<WithdrawalDto>(WalletErrors.ConcurrencyConflict);

                _logger.LogWarning("Concurrency conflict on wallet {WalletId}, retrying", walletId);
            }
            catch (DuplicateWithdrawalException)
            {
                var raced = await _repository.GetByIdempotencyKeyAsync(walletId, idempotencyKey, cancellationToken);
                if (raced is not null)
                    return Result.Ok(WalletMapper.ToWithdrawalDto(raced));

                return Result.Fail<WithdrawalDto>(WalletErrors.ConcurrencyConflict);
            }
        }

        return Result.Fail<WithdrawalDto>(WalletErrors.ConcurrencyConflict);
    }
}
