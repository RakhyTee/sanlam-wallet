using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Events;

namespace Wallet.Infrastructure.Persistence.Configurations;

public class EventEnvelopeConfiguration : IEntityTypeConfiguration<EventEnvelope>
{
    public void Configure(EntityTypeBuilder<EventEnvelope> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(e => e.EventId);

        builder.Property(e => e.EventType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Payload)
            .IsRequired();
    }
}
