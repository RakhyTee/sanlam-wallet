using System.Threading.Channels;
using Wallet.Application.Abstractions;
using Wallet.Domain.Events;

namespace Wallet.Infrastructure.Events;

public sealed class InProcessEventPublisher : IEventPublisher
{
    private readonly Channel<EventEnvelope> _channel;

    public InProcessEventPublisher(Channel<EventEnvelope> channel)
    {
        _channel = channel;
    }

    public async Task PublishAsync(EventEnvelope envelope, CancellationToken cancellationToken = default) =>
        await _channel.Writer.WriteAsync(envelope, cancellationToken);
}
