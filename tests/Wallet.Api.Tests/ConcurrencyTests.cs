using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wallet.Api.Models.Responses;
using Wallet.Domain.Wallets;
using Wallet.Infrastructure.Persistence;
using DomainWallet = Wallet.Domain.Wallets.Wallet;

namespace Wallet.Api.Tests;

public class ConcurrencyTests : IClassFixture<WalletApiFactory>
{
    private readonly WalletApiFactory _factory;
    private readonly HttpClient _client;

    public ConcurrencyTests(WalletApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<Guid> SeedWalletAsync(decimal balance, string currency = "ZAR")
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WalletDbContext>();

        var walletId = Guid.NewGuid();
        context.Wallets.Add(new DomainWallet(walletId, new Money(balance, currency)));
        await context.SaveChangesAsync();

        return walletId;
    }

    private static HttpRequestMessage WithdrawRequestMessage(Guid walletId, decimal amount, string idempotencyKey) =>
        new(HttpMethod.Post, $"/api/wallets/{walletId}/withdrawals")
        {
            Content = JsonContent.Create(new { Amount = amount }),
            Headers = { { "Idempotency-Key", idempotencyKey } }
        };

    [Fact]
    public async Task Withdraw_ConcurrentRequestsExceedingBalance_ExactlyExpectedCountSucceedAndBalanceNeverNegative()
    {
        var walletId = await SeedWalletAsync(1000m);
        const int requestCount = 20;
        const decimal amountPerRequest = 100m;

        var responses = await Task.WhenAll(Enumerable.Range(0, requestCount)
            .Select(_ => _client.SendAsync(WithdrawRequestMessage(walletId, amountPerRequest, Guid.NewGuid().ToString()))));

        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);

        var balanceResponse = await _client.GetAsync($"/api/wallets/{walletId}/balance");
        var balance = await balanceResponse.Content.ReadFromJsonAsync<BalanceResponse>();

        Assert.True(balance!.Balance >= 0, "Balance must never go negative under concurrent withdrawals.");
        Assert.Equal(10, successCount);
        Assert.Equal(1000m - successCount * amountPerRequest, balance.Balance);
    }

    [Fact]
    public async Task Withdraw_ConcurrentRequestsSameIdempotencyKey_OnlyOneSucceedsAndBothReturnSameWithdrawalId()
    {
        var walletId = await SeedWalletAsync(1000m);
        var idempotencyKey = Guid.NewGuid().ToString();

        var responses = await Task.WhenAll(
            _client.SendAsync(WithdrawRequestMessage(walletId, 300m, idempotencyKey)),
            _client.SendAsync(WithdrawRequestMessage(walletId, 300m, idempotencyKey)));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));

        var withdrawals = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<WithdrawalResponse>()));
        Assert.Equal(withdrawals[0]!.WithdrawalId, withdrawals[1]!.WithdrawalId);

        var balanceResponse = await _client.GetAsync($"/api/wallets/{walletId}/balance");
        var balance = await balanceResponse.Content.ReadFromJsonAsync<BalanceResponse>();
        Assert.Equal(700m, balance!.Balance);
    }
}
