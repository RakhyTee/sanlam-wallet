using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainWallet = Wallet.Domain.Wallets.Wallet;

namespace Wallet.Infrastructure.Persistence.Configurations;

public sealed class WalletConfiguration : IEntityTypeConfiguration<DomainWallet>
{
    public void Configure(EntityTypeBuilder<DomainWallet> builder)
    {
        builder.ToTable("Wallets", table =>
            table.HasCheckConstraint("CK_Wallets_Balance_NonNegative", "\"Balance\" >= 0"));

        builder.HasKey(w => w.Id);

        builder.OwnsOne(w => w.Balance, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Balance")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(w => w.Version)
            .IsConcurrencyToken();
    }
}
