using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wallet.Domain.Wallets;
using DomainWallet = Wallet.Domain.Wallets.Wallet;

namespace Wallet.Infrastructure.Persistence;

public class WalletSeeder
{
    public static readonly Guid SeedWalletId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private const decimal SeedBalance = 1000.00m;
    private const string SeedCurrency = "ZAR";

    private readonly WalletDbContext _context;
    private readonly ILogger<WalletSeeder> _logger;

    public WalletSeeder(WalletDbContext context, ILogger<WalletSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.MigrateAsync(cancellationToken);

        var exists = await _context.Wallets.AnyAsync(w => w.Id == SeedWalletId, cancellationToken);
        if (exists)
            return;

        _context.Wallets.Add(new DomainWallet(SeedWalletId, new Money(SeedBalance, SeedCurrency)));

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Seeded wallet {WalletId} with balance {Balance} {Currency}",
            SeedWalletId, SeedBalance, SeedCurrency);
    }
}
