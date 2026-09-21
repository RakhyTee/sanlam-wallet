using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Wallet.Application.Abstractions;
using Wallet.Application.Services;
using Wallet.Domain.Events;
using Wallet.Domain.Wallets;
using DomainWallet = Wallet.Domain.Wallets.Wallet;

namespace Wallet.Application.Tests.Services;

public class WalletServiceTests
{
    private static WalletService CreateSut(IWalletRepository repository, IEventPublisher? eventPublisher = null, TimeProvider? timeProvider = null) =>
        new(repository, eventPublisher ?? Substitute.For<IEventPublisher>(), timeProvider ?? new FakeTimeProvider(), Substitute.For<ILogger<WalletService>>());

    [Fact]
    public async Task WithdrawAsync_WalletNotFound_ReturnsNotFoundError()
    {
        var repository = Substitute.For<IWalletRepository>();
        repository.GetForUpdateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DomainWallet?)null);

        var sut = CreateSut(repository);

        var result = await sut.WithdrawAsync(Guid.NewGuid(), 100m, "key-1");

        Assert.True(result.IsFailure);
        Assert.Equal(WalletErrors.NotFound.Code, result.Error!.Code);
        await repository.DidNotReceive().SaveAsync(
            Arg.Any<DomainWallet>(), Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WithdrawAsync_InsufficientFunds_ReturnsInsufficientFundsError()
    {
        var walletId = Guid.NewGuid();
        var wallet = new DomainWallet(walletId, new Money(100m, "ZAR"));

        var repository = Substitute.For<IWalletRepository>();
        repository.GetForUpdateAsync(walletId, Arg.Any<CancellationToken>()).Returns(wallet);
        repository.GetByIdempotencyKeyAsync(walletId, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Transaction?)null);

        var sut = CreateSut(repository);

        var result = await sut.WithdrawAsync(walletId, 500m, "key-1");

        Assert.True(result.IsFailure);
        Assert.Equal(WalletErrors.InsufficientFunds.Code, result.Error!.Code);
    }

    [Fact]
    public async Task WithdrawAsync_SufficientFunds_DecrementsBalanceAndReturnsSuccess()
    {
        var walletId = Guid.NewGuid();
        var wallet = new DomainWallet(walletId, new Money(1000m, "ZAR"));

        var repository = Substitute.For<IWalletRepository>();
        repository.GetForUpdateAsync(walletId, Arg.Any<CancellationToken>()).Returns(wallet);
        repository.GetByIdempotencyKeyAsync(walletId, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Transaction?)null);

        var sut = CreateSut(repository);

        var result = await sut.WithdrawAsync(walletId, 400m, "key-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(400m, result.Value!.Amount);
        Assert.Equal(600m, result.Value.BalanceAfter);
        Assert.Equal(600m, wallet.Balance.Amount);
    }

    [Fact]
    public async Task WithdrawAsync_Success_CallsSaveAsyncWithTransaction()
    {
        var walletId = Guid.NewGuid();
        var wallet = new DomainWallet(walletId, new Money(1000m, "ZAR"));

        var repository = Substitute.For<IWalletRepository>();
        repository.GetForUpdateAsync(walletId, Arg.Any<CancellationToken>()).Returns(wallet);
        repository.GetByIdempotencyKeyAsync(walletId, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Transaction?)null);

        var sut = CreateSut(repository);

        await sut.WithdrawAsync(walletId, 400m, "key-1");

        await repository.Received(1).SaveAsync(
            Arg.Is<DomainWallet>(w => w.Id == walletId),
            Arg.Is<Transaction>(t => t.WalletId == walletId && t.Amount == 400m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WithdrawAsync_Success_PublishesFundsWithdrawnEvent()
    {
        var walletId = Guid.NewGuid();
        var wallet = new DomainWallet(walletId, new Money(1000m, "ZAR"));

        var repository = Substitute.For<IWalletRepository>();
        repository.GetForUpdateAsync(walletId, Arg.Any<CancellationToken>()).Returns(wallet);
        repository.GetByIdempotencyKeyAsync(walletId, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Transaction?)null);

        var eventPublisher = Substitute.For<IEventPublisher>();
        var sut = CreateSut(repository, eventPublisher: eventPublisher);

        await sut.WithdrawAsync(walletId, 400m, "key-1");

        await eventPublisher.Received(1).PublishAsync(
            Arg.Is<FundsWithdrawn>(e => e.WalletId == walletId && e.Amount == 400m && e.BalanceAfter == 600m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WithdrawAsync_InsufficientFunds_DoesNotPublishEvent()
    {
        var walletId = Guid.NewGuid();
        var wallet = new DomainWallet(walletId, new Money(100m, "ZAR"));

        var repository = Substitute.For<IWalletRepository>();
        repository.GetForUpdateAsync(walletId, Arg.Any<CancellationToken>()).Returns(wallet);
        repository.GetByIdempotencyKeyAsync(walletId, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Transaction?)null);

        var eventPublisher = Substitute.For<IEventPublisher>();
        var sut = CreateSut(repository, eventPublisher: eventPublisher);

        await sut.WithdrawAsync(walletId, 500m, "key-1");

        await eventPublisher.DidNotReceive().PublishAsync(Arg.Any<FundsWithdrawn>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WithdrawAsync_ExistingIdempotencyKey_ReturnsOriginalResultWithoutMutating()
    {
        var walletId = Guid.NewGuid();
        var wallet = new DomainWallet(walletId, new Money(1000m, "ZAR"));

        var existingTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            WalletId = walletId,
            Amount = 250m,
            BalanceAfter = 750m,
            IdempotencyKey = "key-1",
            CreatedAt = DateTime.UtcNow
        };

        var repository = Substitute.For<IWalletRepository>();
        repository.GetForUpdateAsync(walletId, Arg.Any<CancellationToken>()).Returns(wallet);
        repository.GetByIdempotencyKeyAsync(walletId, "key-1", Arg.Any<CancellationToken>()).Returns(existingTransaction);

        var sut = CreateSut(repository);

        var result = await sut.WithdrawAsync(walletId, 999m, "key-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(existingTransaction.Id, result.Value!.WithdrawalId);
        Assert.Equal(750m, result.Value.BalanceAfter);
        Assert.Equal(1000m, wallet.Balance.Amount);
        await repository.DidNotReceive().SaveAsync(
            Arg.Any<DomainWallet>(), Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WithdrawAsync_ConcurrencyConflictOnFirstAttempt_RetriesOnceAndSucceeds()
    {
        var walletId = Guid.NewGuid();

        var repository = Substitute.For<IWalletRepository>();
        repository.GetForUpdateAsync(walletId, Arg.Any<CancellationToken>())
            .Returns(_ => new DomainWallet(walletId, new Money(1000m, "ZAR")));
        repository.GetByIdempotencyKeyAsync(walletId, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Transaction?)null);

        var saveAttempts = 0;
        repository.SaveAsync(Arg.Any<DomainWallet>(), Arg.Any<Transaction>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                saveAttempts++;
                if (saveAttempts == 1)
                    throw new ConcurrencyConflictException("conflict");
                return Task.CompletedTask;
            });

        var sut = CreateSut(repository);

        var result = await sut.WithdrawAsync(walletId, 400m, "key-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, saveAttempts);
        await repository.Received(2).GetForUpdateAsync(walletId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WithdrawAsync_ConcurrencyConflictOnBothAttempts_ReturnsConcurrencyConflictError()
    {
        var walletId = Guid.NewGuid();

        var repository = Substitute.For<IWalletRepository>();
        repository.GetForUpdateAsync(walletId, Arg.Any<CancellationToken>())
            .Returns(_ => new DomainWallet(walletId, new Money(1000m, "ZAR")));
        repository.GetByIdempotencyKeyAsync(walletId, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Transaction?)null);
        repository.SaveAsync(Arg.Any<DomainWallet>(), Arg.Any<Transaction>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new ConcurrencyConflictException("conflict"));

        var sut = CreateSut(repository);

        var result = await sut.WithdrawAsync(walletId, 400m, "key-1");

        Assert.True(result.IsFailure);
        Assert.Equal(WalletErrors.ConcurrencyConflict.Code, result.Error!.Code);
        await repository.Received(2).SaveAsync(
            Arg.Any<DomainWallet>(), Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WithdrawAsync_DuplicateWithdrawalExceptionRaced_ReturnsWinningTransaction()
    {
        var walletId = Guid.NewGuid();
        var wallet = new DomainWallet(walletId, new Money(1000m, "ZAR"));

        var winningTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            WalletId = walletId,
            Amount = 400m,
            BalanceAfter = 600m,
            IdempotencyKey = "key-1",
            CreatedAt = DateTime.UtcNow
        };

        var repository = Substitute.For<IWalletRepository>();
        repository.GetForUpdateAsync(walletId, Arg.Any<CancellationToken>()).Returns(wallet);
        repository.GetByIdempotencyKeyAsync(walletId, "key-1", Arg.Any<CancellationToken>())
            .Returns((Transaction?)null, winningTransaction);
        repository.SaveAsync(Arg.Any<DomainWallet>(), Arg.Any<Transaction>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new DuplicateWithdrawalException("duplicate"));

        var sut = CreateSut(repository);

        var result = await sut.WithdrawAsync(walletId, 400m, "key-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(winningTransaction.Id, result.Value!.WithdrawalId);
        Assert.Equal(600m, result.Value.BalanceAfter);
    }

    [Fact]
    public async Task GetBalanceAsync_WalletNotFound_ReturnsNotFoundError()
    {
        var repository = Substitute.For<IWalletRepository>();
        repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DomainWallet?)null);

        var sut = CreateSut(repository);

        var result = await sut.GetBalanceAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(WalletErrors.NotFound.Code, result.Error!.Code);
    }

    [Fact]
    public async Task GetBalanceAsync_WalletExists_ReturnsMappedBalanceDto()
    {
        var walletId = Guid.NewGuid();
        var wallet = new DomainWallet(walletId, new Money(750m, "ZAR"));
        var now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        var repository = Substitute.For<IWalletRepository>();
        repository.GetByIdAsync(walletId, Arg.Any<CancellationToken>()).Returns(wallet);

        var sut = CreateSut(repository, timeProvider: new FakeTimeProvider(now));

        var result = await sut.GetBalanceAsync(walletId);

        Assert.True(result.IsSuccess);
        Assert.Equal(walletId, result.Value!.WalletId);
        Assert.Equal(750m, result.Value.Balance);
        Assert.Equal("ZAR", result.Value.Currency);
        Assert.Equal(now.UtcDateTime, result.Value.AsOf);
    }
}
