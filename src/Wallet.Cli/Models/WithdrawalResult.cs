namespace Wallet.Cli.Models;

public sealed class WithdrawalResult
{
    public Guid WithdrawalId { get; set; }
    public Guid WalletId { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public DateTime RequestedAt { get; set; }
}
