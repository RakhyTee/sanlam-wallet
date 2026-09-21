namespace Wallet.Domain.Events;

public sealed class FundsWithdrawn
{
    public Guid EventId { get; set; }
    public Guid WalletId { get; set; }
    public Guid WithdrawalId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal BalanceAfter { get; set; }
    public DateTime RequestedAt { get; set; }
}
