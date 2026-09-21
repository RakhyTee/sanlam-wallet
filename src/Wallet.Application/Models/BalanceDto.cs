namespace Wallet.Application.Models;

public class BalanceDto
{
    public Guid WalletId { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime AsOf { get; set; }

}
