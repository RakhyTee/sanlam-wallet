using System.Text.Json;
using Wallet.Application.Abstractions;
using Wallet.Domain.Events;
using Wallet.Infrastructure.Persistence;

namespace Wallet.Infrastructure.Events;

//Best-effort, not atomic with the withdrawal: WalletService calls this after
//WalletRepository.SaveAsync has already committed. A crash between those two
//calls loses the event row, never the withdrawal itself. Chosen deliberately
//over a same-transaction (outbox-style) write to keep this minimal.
public sealed class DbEventPublisher : IEventPublisher
{
    private const string FundsWithdrawnEventType = "FundsWithdrawn";
    private const int EventSchemaVersion = 1;

    private readonly WalletDbContext _context;

    public DbEventPublisher(WalletDbContext context)
    {
        _context = context;
    }

    public async Task PublishAsync(FundsWithdrawn withdrawalEvent, CancellationToken cancellationToken = default)
    {
        var envelope = new EventEnvelope
        {
            EventId = withdrawalEvent.EventId,
            EventType = FundsWithdrawnEventType,
            Version = EventSchemaVersion,
            RequestedAt = withdrawalEvent.RequestedAt,
            Payload = JsonSerializer.Serialize(withdrawalEvent)
        };

        _context.Events.Add(envelope);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
