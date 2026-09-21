using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wallet.Api.Models.Responses;
using Wallet.Domain.Wallets;
using Wallet.Infrastructure.Persistence;
using DomainWallet = Wallet.Domain.Wallets.Wallet;

namespace Wallet.Api.Tests;

public class WalletsControllerTests : IClassFixture<WalletApiFactory>
{
    private readonly WalletApiFactory _factory;
    private readonly HttpClient _client;

    public WalletsControllerTests(WalletApiFactory factory)
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

    private static HttpRequestMessage WithdrawRequestMessage(Guid walletId, decimal amount, string? idempotencyKey = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/wallets/{walletId}/withdrawals")
        {
            Content = JsonContent.Create(new { Amount = amount })
        };

        if (idempotencyKey is not null)
            request.Headers.Add("Idempotency-Key", idempotencyKey);

        return request;
    }

    [Fact]
    public async Task GetBalance_ExistingWallet_Returns200WithBalance()
    {
        var walletId = await SeedWalletAsync(1000m);

        var response = await _client.GetAsync($"/api/wallets/{walletId}/balance");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var balance = await response.Content.ReadFromJsonAsync<BalanceResponse>();
        Assert.NotNull(balance);
        Assert.Equal(walletId, balance!.WalletId);
        Assert.Equal(1000m, balance.Balance);
        Assert.Equal("ZAR", balance.Currency);
    }

    [Fact]
    public async Task GetBalance_UnknownWallet_Returns404()
    {
        var response = await _client.GetAsync($"/api/wallets/{Guid.NewGuid()}/balance");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Withdraw_ValidRequest_Returns201AndDecrementsBalance()
    {
        var walletId = await SeedWalletAsync(1000m);

        using var request = WithdrawRequestMessage(walletId, 400m, Guid.NewGuid().ToString());
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var withdrawal = await response.Content.ReadFromJsonAsync<WithdrawalResponse>();
        Assert.NotNull(withdrawal);
        Assert.Equal(400m, withdrawal!.Amount);
        Assert.Equal(600m, withdrawal.BalanceAfter);

        var balanceResponse = await _client.GetAsync($"/api/wallets/{walletId}/balance");
        var balance = await balanceResponse.Content.ReadFromJsonAsync<BalanceResponse>();
        Assert.Equal(600m, balance!.Balance);
    }

    [Fact]
    public async Task Withdraw_MissingIdempotencyKeyHeader_Returns400()
    {
        var walletId = await SeedWalletAsync(1000m);

        using var request = WithdrawRequestMessage(walletId, 100m);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Withdraw_NonPositiveAmount_Returns400ValidationProblem()
    {
        var walletId = await SeedWalletAsync(1000m);

        using var request = WithdrawRequestMessage(walletId, 0m, Guid.NewGuid().ToString());
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Withdraw_InsufficientFunds_Returns422()
    {
        var walletId = await SeedWalletAsync(100m);

        using var request = WithdrawRequestMessage(walletId, 500m, Guid.NewGuid().ToString());
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Withdraw_UnknownWallet_Returns404()
    {
        using var request = WithdrawRequestMessage(Guid.NewGuid(), 100m, Guid.NewGuid().ToString());
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Withdraw_RepeatedIdempotencyKey_ReturnsOriginal201WithoutDoubleWithdrawing()
    {
        var walletId = await SeedWalletAsync(1000m);
        var idempotencyKey = Guid.NewGuid().ToString();

        using var firstRequest = WithdrawRequestMessage(walletId, 300m, idempotencyKey);
        var firstResponse = await _client.SendAsync(firstRequest);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        var first = await firstResponse.Content.ReadFromJsonAsync<WithdrawalResponse>();

        using var secondRequest = WithdrawRequestMessage(walletId, 300m, idempotencyKey);
        var secondResponse = await _client.SendAsync(secondRequest);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        var second = await secondResponse.Content.ReadFromJsonAsync<WithdrawalResponse>();

        Assert.Equal(first!.WithdrawalId, second!.WithdrawalId);

        var balanceResponse = await _client.GetAsync($"/api/wallets/{walletId}/balance");
        var balance = await balanceResponse.Content.ReadFromJsonAsync<BalanceResponse>();
        Assert.Equal(700m, balance!.Balance);
    }
}
