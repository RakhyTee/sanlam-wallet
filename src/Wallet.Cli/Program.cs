using System.CommandLine;
using Wallet.Cli;
using Wallet.Cli.Commands;

var options = new CliOptions();

var envBaseUrl = Environment.GetEnvironmentVariable("WALLET_API_BASE_URL");
if (!string.IsNullOrWhiteSpace(envBaseUrl))
    options.BaseUrl = envBaseUrl;

using var httpClient = new HttpClient { BaseAddress = new Uri(options.BaseUrl) };
var apiClient = new WalletApiClient(httpClient);

var rootCommand = new RootCommand("Wallet CLI");
rootCommand.Add(new BalanceCommand(apiClient));
rootCommand.Add(new WithdrawCommand(apiClient));

return await rootCommand.Parse(args).InvokeAsync();
