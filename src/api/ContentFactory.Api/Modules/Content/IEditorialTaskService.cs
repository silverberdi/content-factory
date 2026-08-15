using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Audit;
using Microsoft.EntityFrameworkCore;

namespace ContentFactory.Api.Modules.Content;

public interface IEditorialTaskService
{
    Task<List<EditorialTaskDto>> GetTasksAsync(
        Guid? channelId = null,
        string? status = null,
        string? priority = null,
        string? assignedEmail = null,
        CancellationToken cancellationToken = default);

    Task<EditorialTaskDto?> AssignTaskAsync(
        Guid taskId,
        AssignEditorialTaskRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<EditorialTaskDto?> UpdateTaskStatusAsync(
        Guid taskId,
        string status,
        string actorEmail,
        CancellationToken cancellationToken = default);
}

public class EditorialTaskService(
    AppDbContext dbContext,
    IAuditService auditService,
    ILogger<EditorialTaskService> logger) : IEditorialTaskService
{
    public async Task<List<EditorialTaskDto>> GetTasksAsync(
        Guid? channelId = null,
        string? status = null,
        string? priority = null,
        string? assignedEmail = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.EditorialTasks.AsNoTracking().AsQueryable();

        if (channelId.HasValue && channelId.Value != Guid.Empty)
        {
            query = query.Where(t => t.ChannelId == channelId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            query = query.Where(t => t.Priority == priority);
        }

        if (!string.IsNullOrWhiteSpace(assignedEmail))
        {
            query = query.Where(t => t.AssignedUserEmail == assignedEmail);
        }

        var tasks = await query
            .OrderByDescending(t => t.Priority == EditorialTaskPriority.Urgent ? 3 :
                                   t.Priority == EditorialTaskPriority.High ? 2 :
                                   t.Priority == EditorialTaskPriority.Normal ? 1 : 0)
            .ThenBy(t => t.DueDateUtc ?? DateTime.MaxValue)
            .ToListAsync(cancellationToken);

        var channelIds = tasks.Select(t => t.ChannelId).Distinct().ToList();
        var channels = await dbContext.Channels
            .Where(ch => channelIds.Contains(ch.Id))
            .ToDictionaryAsync(ch => ch.Id, ch => ch.Name, cancellationToken);

        var contentIds = tasks.Select(t => t.ContentItemId).Distinct().ToList();
        var contentTitles = await dbContext.ContentItems
            .Where(c => contentIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Title, cancellationToken);

        return tasks.Select(t =>
        {
            channels.TryGetValue(t.ChannelId, out var channelName);
            contentTitles.TryGetValue(t.ContentItemId, out var title);

            return new EditorialTaskDto(
                t.Id,
                t.ChannelId,
                channelName,
                t.ContentItemId,
                title,
                t.TaskType,
                t.Priority,
                t.Status,
                t.AssignedUserEmail,
                t.DueDateUtc,
                t.CompletedAtUtc,
                t.CompletedByEmail,
                t.CreatedAtUtc,
                t.UpdatedAtUtc,
                t.CreatedByEmail
            );
        }).ToList();
    }

    public async Task<EditorialTaskDto?> AssignTaskAsync(
        Guid taskId,
        AssignEditorialTaskRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var task = await dbContext.EditorialTasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task == null) return null;

        if (!string.IsNullOrWhiteSpace(request.AssignedUserEmail))
        {
            task.AssignedUserEmail = request.AssignedUserEmail.Trim();
            if (task.Status == EditorialTaskStatus.Pending)
            {
                task.Status = EditorialTaskStatus.InProgress;
            }
        }
        else
        {
            task.AssignedUserEmail = null;
        }

        if (!string.IsNullOrWhiteSpace(request.Priority) && EditorialTaskPriority.All.Contains(request.Priority))
        {
            task.Priority = request.Priority;
        }

        if (request.DueDateUtc.HasValue)
        {
            task.DueDateUtc = request.DueDateUtc.Value;
        }

        task.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "EditorialTask.Assigned",
            "EditorialTask",
            task.Id.ToString(),
            detailsJson: null,
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        var channel = await dbContext.Channels.FindAsync([task.ChannelId], cancellationToken);
        var content = await dbContext.ContentItems.FindAsync([task.ContentItemId], cancellationToken);

        return new EditorialTaskDto(
            task.Id,
            task.ChannelId,
            channel?.Name,
            task.ContentItemId,
            content?.Title,
            task.TaskType,
            task.Priority,
            task.Status,
            task.AssignedUserEmail,
            task.DueDateUtc,
            task.CompletedAtUtc,
            task.CompletedByEmail,
            task.CreatedAtUtc,
            task.UpdatedAtUtc,
            task.CreatedByEmail
        );
    }

    public async Task<EditorialTaskDto?> UpdateTaskStatusAsync(
        Guid taskId,
        string status,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (!EditorialTaskStatus.All.Contains(status))
        {
            throw new ArgumentException("Invalid task status.");
        }

        var task = await dbContext.EditorialTasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task == null) return null;

        task.Status = status;
        if (status == EditorialTaskStatus.Completed)
        {
            task.CompletedAtUtc = DateTime.UtcNow;
            task.CompletedByEmail = actorEmail;
        }
        else
        {
            task.CompletedAtUtc = null;
            task.CompletedByEmail = null;
        }

        task.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            $"EditorialTask.{status}",
            "EditorialTask",
            task.Id.ToString(),
            detailsJson: null,
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        var channel = await dbContext.Channels.FindAsync([task.ChannelId], cancellationToken);
        var content = await dbContext.ContentItems.FindAsync([task.ContentItemId], cancellationToken);

        return new EditorialTaskDto(
            task.Id,
            task.ChannelId,
            channel?.Name,
            task.ContentItemId,
            content?.Title,
            task.TaskType,
            task.Priority,
            task.Status,
            task.AssignedUserEmail,
            task.DueDateUtc,
            task.CompletedAtUtc,
            task.CompletedByEmail,
            task.CreatedAtUtc,
            task.UpdatedAtUtc,
            task.CreatedByEmail
        );
    }
}
