namespace InvestManagement.Infrastructure.Messaging;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }

    public int Attempts { get; set; }

    public DateTime? NextAttemptAtUtc { get; set; }
}