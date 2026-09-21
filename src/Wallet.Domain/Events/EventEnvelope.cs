namespace Wallet.Domain.Events;

public class EventEnvelope
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string Payload { get; set; } = string.Empty;
}
