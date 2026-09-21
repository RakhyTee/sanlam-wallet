namespace Wallet.Cli.Models;

public class WithdrawalResult
{
    public Guid WithdrawalId { get; set; }
    public Guid WalletId { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public DateTime RequestedAt { get; set; }
}
