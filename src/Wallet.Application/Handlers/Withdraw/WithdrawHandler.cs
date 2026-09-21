using MediatR;
using Wallet.Application.Commands.Withdraw;
using Wallet.Application.Models;
using Wallet.Application.Services;
using Wallet.Domain.Common;

namespace Wallet.Application.Handlers.Withdraw;

public class WithdrawHandler : IRequestHandler<WithdrawCommand, Result<WithdrawalDto>>
{
    private readonly IWalletService _walletService;

    public WithdrawHandler(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public async Task<Result<WithdrawalDto>> Handle(WithdrawCommand request, CancellationToken cancellationToken)
    {
            var result = await _walletService.WithdrawAsync(request.WalletId, request.Amount, request.IdempotencyKey, cancellationToken);
            return result;      
    }

}
