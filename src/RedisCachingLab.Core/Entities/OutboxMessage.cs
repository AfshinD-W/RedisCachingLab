
namespace RedisCachingLab.Core.Entities;

public class OutboxMessage
{
    public int Id { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    public int RetryCount { get; set; }

    public DateTime? NextAttemptAt { get; set; }

    public string? LastError { get; set; }
}
