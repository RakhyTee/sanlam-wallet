using System.CommandLine;
using Wallet.Cli.Models;

namespace Wallet.Cli.Commands;

public class WithdrawCommand : Command
{
    private readonly WalletApiClient _client;

    public WithdrawCommand(WalletApiClient client) : base("withdraw", "Withdraw funds from a wallet")
    {
        _client = client;

        var walletIdArgument = new Argument<Guid>("walletId") { Description = "The wallet id" };
        var amountArgument = new Argument<decimal>("amount") { Description = "The amount to withdraw" };
        var idempotencyKeyOption = new Option<string?>("--idempotency-key")
        {
            Description = "A key to safely retry this withdrawal; generated if omitted"
        };

        Add(walletIdArgument);
        Add(amountArgument);
        Add(idempotencyKeyOption);

        SetAction(async (parseResult, cancellationToken) =>
        {
            var walletId = parseResult.GetValue(walletIdArgument);
            var amount = parseResult.GetValue(amountArgument);
            var idempotencyKey = parseResult.GetValue(idempotencyKeyOption);
            await ExecuteAsync(walletId, amount, idempotencyKey, cancellationToken);
        });
    }

    private async Task ExecuteAsync(Guid walletId, decimal amount, string? idempotencyKey, CancellationToken cancellationToken)
    {
        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? Guid.NewGuid().ToString() : idempotencyKey;

        var (success, withdrawal, problem) = await _client.WithdrawAsync(walletId, amount, key, cancellationToken);

        if (!success || withdrawal is null)
        {
            ConsoleOutput.PrintError(problem);
            return;
        }

        Console.WriteLine(
            $"Withdrew {withdrawal.Amount:F2} from wallet {withdrawal.WalletId}. Balance after: {withdrawal.BalanceAfter:F2}. Withdrawal id: {withdrawal.WithdrawalId}");
    }
}
