using Wallet.Domain.Common;

namespace Wallet.Domain.Wallets;

public class Wallet
{
    public Guid Id { get; private set; }
    public Money Balance { get; private set; } = null!;
    public int Version { get; private set; }

    private Wallet()
    {
    }

    public Wallet(Guid id, Money balance)
    {
        Id = id;
        Balance = balance;
        Version = 0;
    }

    public Result Withdraw(decimal amount)
    {
        if (Balance.Amount < amount)
            return Result.Fail(WalletErrors.InsufficientFunds);

        Balance = new Money(Balance.Amount - amount, Balance.Currency);
        Version += 1;

        return Result.Ok();
    }
}
