using MediatR;
using Wallet.Application.Models;
using Wallet.Application.Queries.Balance;
using Wallet.Application.Services;
using Wallet.Domain.Common;

namespace Wallet.Application.Handlers.Balance;

public class GetBalanceHandler : IRequestHandler<GetBalanceQuery, Result<BalanceDto>>
{
    private readonly IWalletService _walletService;

    public GetBalanceHandler(IWalletService walletService)
    {
        _walletService = walletService;
    }

    public async Task<Result<BalanceDto>> Handle(GetBalanceQuery request, CancellationToken cancellationToken)
    {
        return await _walletService.GetBalanceAsync(request.WalletId, cancellationToken);
    }
}
