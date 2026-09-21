using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wallet.Domain.Events;

namespace Wallet.Infrastructure.Events;

public sealed class LoggingEventConsumer : BackgroundService
{
    private readonly Channel<EventEnvelope> _channel;
    private readonly ILogger<LoggingEventConsumer> _logger;

    public LoggingEventConsumer(Channel<EventEnvelope> channel, ILogger<LoggingEventConsumer> logger)
    {
        _channel = channel;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var envelope in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            _logger.LogInformation(
                "Published event {EventType} ({EventId}) at {OccurredAtUtc}: {Payload}",
                envelope.EventType, envelope.EventId, envelope.OccurredAtUtc, envelope.Payload);
        }
    }
}
