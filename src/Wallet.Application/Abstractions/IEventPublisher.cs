using Wallet.Domain.Events;

namespace Wallet.Application.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync(EventEnvelope envelope, CancellationToken cancellationToken = default);
}
