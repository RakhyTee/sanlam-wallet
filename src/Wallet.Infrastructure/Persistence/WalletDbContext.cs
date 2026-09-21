using Microsoft.EntityFrameworkCore;
using Wallet.Domain.Events;
using Wallet.Domain.Wallets;
using DomainWallet = Wallet.Domain.Wallets.Wallet;

namespace Wallet.Infrastructure.Persistence;

public sealed class WalletDbContext : DbContext
{
    public DbSet<DomainWallet> Wallets => Set<DomainWallet>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<EventEnvelope> Events => Set<EventEnvelope>();

    public WalletDbContext(DbContextOptions<WalletDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WalletDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
