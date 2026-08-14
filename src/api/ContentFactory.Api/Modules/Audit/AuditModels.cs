namespace ContentFactory.Api.Modules.Audit;

public class AuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ActorUserId { get; set; }
    public string ActorEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string? DetailsJson { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}

public record AuditEventDto(
    Guid Id,
    Guid? ActorUserId,
    string ActorEmail,
    string Action,
    string TargetType,
    string TargetId,
    string? DetailsJson,
    string? CorrelationId,
    DateTime TimestampUtc
);

public interface IAuditService
{
    Task RecordAsync(string action, string targetType, string targetId, string? detailsJson = null, Guid? actorUserId = null, string? actorEmail = null, string? correlationId = null, CancellationToken cancellationToken = default);
    Task<List<AuditEventDto>> GetRecentEventsAsync(int limit = 50, CancellationToken cancellationToken = default);
}
