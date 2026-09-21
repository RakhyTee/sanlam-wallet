using FluentValidation;
using Wallet.Application.Commands.Withdraw;
using Wallet.Domain.Wallets;

namespace Wallet.Application.Validators.Withdraw;

public class WithdrawValidator : AbstractValidator<WithdrawCommand>
{
    private readonly IWalletRepository _repository;

    public WithdrawValidator(IWalletRepository repository)
    {
        _repository = repository;

        RuleFor(x => x.WalletId)
            .NotEmpty();

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithErrorCode(WalletErrors.InvalidAmount.Code)
            .WithMessage(WalletErrors.InvalidAmount.Message);

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty();

        RuleFor(x => x)
            .MustAsync(WalletExistsAsync)
            .WithErrorCode(WalletErrors.NotFound.Code)
            .WithMessage(WalletErrors.NotFound.Message)
            .WithName(nameof(WithdrawCommand.WalletId))
            .When(x => x.WalletId != Guid.Empty);

        RuleFor(x => x)
            .MustAsync(HaveSufficientFundsAsync)
            .WithErrorCode(WalletErrors.InsufficientFunds.Code)
            .WithMessage(WalletErrors.InsufficientFunds.Message)
            .WithName(nameof(WithdrawCommand.Amount))
            .When(x => x.WalletId != Guid.Empty && x.Amount > 0);
    }

    private async Task<bool> WalletExistsAsync(WithdrawCommand command, CancellationToken cancellationToken)
    {
        var wallet = await _repository.GetByIdAsync(command.WalletId, cancellationToken);
        return wallet is not null;
    }

    private async Task<bool> HaveSufficientFundsAsync(WithdrawCommand command, CancellationToken cancellationToken)
    {
        var wallet = await _repository.GetByIdAsync(command.WalletId, cancellationToken);
        return wallet is not null && wallet.Balance.Amount >= command.Amount;
    }

}
