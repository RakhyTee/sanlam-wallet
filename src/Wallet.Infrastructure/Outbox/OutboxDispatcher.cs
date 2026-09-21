using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wallet.Application.Abstractions;
using Wallet.Domain.Events;
using Wallet.Infrastructure.Persistence;

namespace Wallet.Infrastructure.Outbox;

public sealed class OutboxDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(IServiceScopeFactory scopeFactory, IOptions<OutboxOptions> options, ILogger<OutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await DispatchBatchAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
        }
    }

    private async Task DispatchBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var messages = await context.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null && m.Attempts < _options.MaxAttempts)
            .OrderBy(m => m.OccurredAtUtc)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                var envelope = JsonSerializer.Deserialize<EventEnvelope>(message.Payload)
                    ?? throw new InvalidOperationException($"Outbox message {message.Id} payload could not be deserialized.");

                await publisher.PublishAsync(envelope, cancellationToken);

                message.ProcessedAtUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                message.Attempts += 1;
                message.LastError = ex.Message;

                _logger.LogWarning(ex,
                    "Failed to publish outbox message {MessageId}, attempt {Attempts}/{MaxAttempts}",
                    message.Id, message.Attempts, _options.MaxAttempts);
            }
        }

        if (messages.Count > 0)
            await context.SaveChangesAsync(cancellationToken);
    }
}
