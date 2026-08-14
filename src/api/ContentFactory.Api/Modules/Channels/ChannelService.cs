using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Audit;
using Microsoft.EntityFrameworkCore;

namespace ContentFactory.Api.Modules.Channels;

public interface IChannelService
{
    Task<List<ChannelDto>> GetAllChannelsAsync(CancellationToken cancellationToken = default);
    Task<ChannelDto?> GetChannelByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ChannelDto> CreateChannelAsync(CreateChannelRequest request, Guid actingUserId, string actingEmail, CancellationToken cancellationToken = default);
    Task<ChannelDto> UpdateChannelAsync(Guid id, UpdateChannelRequest request, Guid actingUserId, string actingEmail, CancellationToken cancellationToken = default);
    Task DeleteChannelAsync(Guid id, Guid actingUserId, string actingEmail, CancellationToken cancellationToken = default);
}

public class ChannelService(AppDbContext dbContext, IAuditService auditService) : IChannelService
{
    public async Task<List<ChannelDto>> GetAllChannelsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Channels
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new ChannelDto(
                c.Id,
                c.Slug,
                c.Name,
                c.Language,
                c.Niche,
                c.Status,
                c.CreatedAtUtc,
                c.UpdatedAtUtc
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<ChannelDto?> GetChannelByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var c = await dbContext.Channels.FindAsync([id], cancellationToken);
        if (c == null) return null;

        return new ChannelDto(
            c.Id,
            c.Slug,
            c.Name,
            c.Language,
            c.Niche,
            c.Status,
            c.CreatedAtUtc,
            c.UpdatedAtUtc
        );
    }

    public async Task<ChannelDto> CreateChannelAsync(CreateChannelRequest request, Guid actingUserId, string actingEmail, CancellationToken cancellationToken = default)
    {
        var slug = (request.Slug ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = request.Name.Trim().ToLowerInvariant().Replace(' ', '-');
        }

        var existing = await dbContext.Channels.AnyAsync(c => c.Slug == slug, cancellationToken);
        if (existing)
        {
            throw new InvalidOperationException($"Channel slug '{slug}' already exists.");
        }

        var status = request.Status ?? ChannelStatus.Pilot;
        if (!ChannelStatus.All.Contains(status))
        {
            throw new ArgumentException($"Invalid channel status: '{status}'.");
        }

        var channel = new Channel
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = request.Name.Trim(),
            Language = string.IsNullOrWhiteSpace(request.Language) ? "es" : request.Language.Trim(),
            Niche = request.Niche?.Trim() ?? string.Empty,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        dbContext.Channels.Add(channel);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            action: "channel.created",
            targetType: "channel",
            targetId: channel.Id.ToString(),
            detailsJson: $"{{\"name\":\"{channel.Name}\",\"slug\":\"{channel.Slug}\",\"status\":\"{channel.Status}\",\"language\":\"{channel.Language}\"}}",
            actorUserId: actingUserId,
            actorEmail: actingEmail,
            cancellationToken: cancellationToken
        );

        return new ChannelDto(
            channel.Id,
            channel.Slug,
            channel.Name,
            channel.Language,
            channel.Niche,
            channel.Status,
            channel.CreatedAtUtc,
            channel.UpdatedAtUtc
        );
    }

    public async Task<ChannelDto> UpdateChannelAsync(Guid id, UpdateChannelRequest request, Guid actingUserId, string actingEmail, CancellationToken cancellationToken = default)
    {
        var channel = await dbContext.Channels.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Channel not found.");

        if (!ChannelStatus.All.Contains(request.Status))
        {
            throw new ArgumentException($"Invalid channel status: '{request.Status}'.");
        }

        var previousStatus = channel.Status;
        channel.Name = request.Name.Trim();
        channel.Language = request.Language.Trim();
        channel.Niche = request.Niche.Trim();
        channel.Status = request.Status;
        channel.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            action: "channel.updated",
            targetType: "channel",
            targetId: channel.Id.ToString(),
            detailsJson: $"{{\"previousStatus\":\"{previousStatus}\",\"newStatus\":\"{channel.Status}\",\"name\":\"{channel.Name}\"}}",
            actorUserId: actingUserId,
            actorEmail: actingEmail,
            cancellationToken: cancellationToken
        );

        return new ChannelDto(
            channel.Id,
            channel.Slug,
            channel.Name,
            channel.Language,
            channel.Niche,
            channel.Status,
            channel.CreatedAtUtc,
            channel.UpdatedAtUtc
        );
    }

    public async Task DeleteChannelAsync(Guid id, Guid actingUserId, string actingEmail, CancellationToken cancellationToken = default)
    {
        var channel = await dbContext.Channels.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException("Channel not found.");

        dbContext.Channels.Remove(channel);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            action: "channel.deleted",
            targetType: "channel",
            targetId: id.ToString(),
            detailsJson: $"{{\"slug\":\"{channel.Slug}\",\"name\":\"{channel.Name}\"}}",
            actorUserId: actingUserId,
            actorEmail: actingEmail,
            cancellationToken: cancellationToken
        );
    }
}
