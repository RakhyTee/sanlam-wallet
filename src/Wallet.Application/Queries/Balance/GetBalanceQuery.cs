using MediatR;
using Wallet.Application.Models;
using Wallet.Domain.Common;

namespace Wallet.Application.Queries.Balance;

public class GetBalanceQuery : IRequest<Result<BalanceDto>>
{
    public Guid WalletId { get; set; }
}
