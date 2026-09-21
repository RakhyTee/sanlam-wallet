using System.CommandLine;
using Wallet.Cli.Models;

namespace Wallet.Cli.Commands;

public sealed class BalanceCommand : Command
{
    private readonly WalletApiClient _client;

    public BalanceCommand(WalletApiClient client) : base("balance", "Get the current balance of a wallet")
    {
        _client = client;

        var walletIdArgument = new Argument<Guid>("walletId") { Description = "The wallet id" };
        Add(walletIdArgument);

        SetAction(async (parseResult, cancellationToken) =>
        {
            var walletId = parseResult.GetValue(walletIdArgument);
            await ExecuteAsync(walletId, cancellationToken);
        });
    }

    private async Task ExecuteAsync(Guid walletId, CancellationToken cancellationToken)
    {
        var (success, balance, problem) = await _client.GetBalanceAsync(walletId, cancellationToken);

        if (!success || balance is null)
        {
            ConsoleOutput.PrintError(problem);
            return;
        }

        Console.WriteLine($"Wallet {balance.WalletId}: {balance.Balance:F2} {balance.Currency} (as of {balance.AsOf:O})");
    }
}
