namespace Wallet.Cli.Models;

public sealed class BalanceResult
{
    public Guid WalletId { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime AsOf { get; set; }
}
