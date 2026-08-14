using ContentFactory.Api.Modules.Audit;
using Microsoft.EntityFrameworkCore;

namespace ContentFactory.Api.Infrastructure;

public class AuditService(AppDbContext dbContext, ILogger<AuditService> logger) : IAuditService
{
    public async Task RecordAsync(
        string action,
        string targetType,
        string targetId,
        string? detailsJson = null,
        Guid? actorUserId = null,
        string? actorEmail = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            ActorEmail = actorEmail ?? "system",
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            DetailsJson = detailsJson,
            CorrelationId = correlationId,
            TimestampUtc = DateTime.UtcNow
        };

        dbContext.AuditEvents.Add(auditEvent);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Audit event recorded: {Action} on {TargetType}:{TargetId} by {ActorEmail}",
            action, targetType, targetId, auditEvent.ActorEmail);
    }

    public async Task<List<AuditEventDto>> GetRecentEventsAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        return await dbContext.AuditEvents
            .OrderByDescending(a => a.TimestampUtc)
            .Take(limit)
            .Select(a => new AuditEventDto(
                a.Id,
                a.ActorUserId,
                a.ActorEmail,
                a.Action,
                a.TargetType,
                a.TargetId,
                a.DetailsJson,
                a.CorrelationId,
                a.TimestampUtc
            ))
            .ToListAsync(cancellationToken);
    }
}
