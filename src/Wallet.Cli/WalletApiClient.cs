using System.Net.Http.Json;
using Wallet.Cli.Models;

namespace Wallet.Cli;

public sealed class WalletApiClient
{
    private readonly HttpClient _httpClient;

    public WalletApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(bool Success, BalanceResult? Balance, ProblemResponse? Problem)> GetBalanceAsync(
        Guid walletId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/api/wallets/{walletId}/balance", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var balance = await response.Content.ReadFromJsonAsync<BalanceResult>(cancellationToken: cancellationToken);
            return (true, balance, null);
        }

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(cancellationToken: cancellationToken);
        return (false, null, problem);
    }

    public async Task<(bool Success, WithdrawalResult? Withdrawal, ProblemResponse? Problem)> WithdrawAsync(
        Guid walletId, decimal amount, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/wallets/{walletId}/withdrawals")
        {
            Content = JsonContent.Create(new { Amount = amount })
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var withdrawal = await response.Content.ReadFromJsonAsync<WithdrawalResult>(cancellationToken: cancellationToken);
            return (true, withdrawal, null);
        }

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(cancellationToken: cancellationToken);
        return (false, null, problem);
    }
}
