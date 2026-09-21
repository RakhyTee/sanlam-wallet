using System.Text.Json;
using Microsoft.Extensions.Logging;
using Wallet.Application.Mappers;
using Wallet.Application.Models;
using Wallet.Domain;
using Wallet.Domain.Common;
using Wallet.Domain.Enums;
using Wallet.Domain.Events;
using Wallet.Domain.Wallets;

namespace Wallet.Application.Services;

public class WalletService : IWalletService
{
    private const int MaxAttempts = 2;
    private const string FundsWithdrawnEventType = "FundsWithdrawn";
    private const int EventSchemaVersion = 1;

    private readonly IWalletRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<WalletService> _logger;

    public WalletService(IWalletRepository repository, TimeProvider timeProvider,
            ILogger<WalletService> logger)
    {
        _repository = repository;
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
            AsOfUtc = _timeProvider.GetUtcNow().UtcDateTime
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

            var occurredAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

            var transaction = new Domain.Wallets.Transaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Type = TransactionType.Withdrawal,
                Amount = amount,
                BalanceAfter = wallet.Balance.Amount,
                IdempotencyKey = idempotencyKey,
                CreatedAtUtc = occurredAtUtc
            };

            var outboxMessage = BuildOutboxMessage(wallet.Id, transaction, amount, wallet.Balance.Currency, occurredAtUtc);

            try
            {
                await _repository.SaveAsync(wallet, transaction, outboxMessage, cancellationToken);

                _logger.LogInformation(
                    "Withdrawal {WithdrawalId} succeeded for wallet {WalletId}, balance now {Balance}",
                    transaction.Id, wallet.Id, wallet.Balance.Amount);

                return Result.Ok(WalletMapper.ToWithdrawalDto(transaction));
            }
            catch (ConcurrencyConflictException) when (attempt < MaxAttempts)
            {
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

    private static OutboxMessage BuildOutboxMessage(
        Guid walletId, Domain.Wallets.Transaction transaction, decimal amount, string currency, DateTime occurredAtUtc)
    {
        var domainEvent = new FundsWithdrawn
        {
            EventId = Guid.NewGuid(),
            WalletId = walletId,
            WithdrawalId = transaction.Id,
            Amount = amount,
            Currency = currency,
            BalanceAfter = transaction.BalanceAfter,
            OccurredAtUtc = occurredAtUtc
        };

        var envelope = new EventEnvelope
        {
            EventId = domainEvent.EventId,
            EventType = FundsWithdrawnEventType,
            Version = EventSchemaVersion,
            OccurredAtUtc = occurredAtUtc,
            Payload = JsonSerializer.Serialize(domainEvent)
        };

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = FundsWithdrawnEventType,
            Payload = JsonSerializer.Serialize(envelope),
            OccurredAtUtc = occurredAtUtc
        };
    }

}
