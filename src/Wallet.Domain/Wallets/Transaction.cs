using Wallet.Domain.Enums;

namespace Wallet.Domain.Wallets;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTime CreatedAt { get; set; }

}
