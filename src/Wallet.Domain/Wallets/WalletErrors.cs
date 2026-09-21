using Wallet.Domain.Common;

namespace Wallet.Domain.Wallets;

public static class WalletErrors
{
    public static Error ConcurrencyConflict => new("Wallet.ConcurrencyConflict", "The wallet was updated concurrently. Please retry.");
    public static Error InsufficientFunds => new("Wallet.InsufficientFunds", "Insufficient funds in the wallet.");
    public static Error NotFound => new("Wallet.WalletNotFound", "The specified wallet was not found.");
    public static Error InvalidAmount => new("Wallet.InvalidAmount", "Amount must be greater than zero.");
}
