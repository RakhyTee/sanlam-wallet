using Wallet.Domain.Events;

namespace Wallet.Application.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync(FundsWithdrawn withdrawalEvent, CancellationToken cancellationToken = default);
}
