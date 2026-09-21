using MediatR;
using Wallet.Application.Models;
using Wallet.Domain.Common;

namespace Wallet.Application.Commands.Withdraw;

public class WithdrawCommand : IRequest<Result<WithdrawalDto>>
{
    public Guid WalletId { get; set; }
    public decimal Amount { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}
