using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain;

namespace Wallet.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Type)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(o => o.LastError)
            .HasMaxLength(2000);

        builder.HasIndex(o => o.ProcessedAtUtc)
            .HasFilter("\"ProcessedAtUtc\" IS NULL");
    }
}
