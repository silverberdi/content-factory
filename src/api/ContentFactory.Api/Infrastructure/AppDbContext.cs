using ContentFactory.Api.Modules.Audit;
using ContentFactory.Api.Modules.Channels;
using ContentFactory.Api.Modules.Content;
using ContentFactory.Api.Modules.Discovery;
using ContentFactory.Api.Modules.Identity;
using Microsoft.EntityFrameworkCore;

namespace ContentFactory.Api.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserInvitation> UserInvitations => Set<UserInvitation>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<DiscoverySource> DiscoverySources => Set<DiscoverySource>();
    public DbSet<DiscoveryCandidate> DiscoveryCandidates => Set<DiscoveryCandidate>();
    public DbSet<ContentItem> ContentItems => Set<ContentItem>();
    public DbSet<ContentItemEvidence> ContentItemEvidences => Set<ContentItemEvidence>();
    public DbSet<TruthSource> TruthSources => Set<TruthSource>();
    public DbSet<TruthSourceVersion> TruthSourceVersions => Set<TruthSourceVersion>();
    public DbSet<ContentIdea> ContentIdeas => Set<ContentIdea>();
    public DbSet<ContentIdeaVersion> ContentIdeaVersions => Set<ContentIdeaVersion>();
    public DbSet<EditorialTask> EditorialTasks => Set<EditorialTask>();
    public DbSet<AiRecommendation> AiRecommendations => Set<AiRecommendation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.IsOwner).IsRequired();
            entity.Property(u => u.IsActive).IsRequired();
            entity.Property(u => u.CreatedAtUtc).IsRequired();

            entity.HasMany(u => u.Roles)
                  .WithOne()
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Role).IsRequired().HasMaxLength(64);
            entity.Property(r => r.AssignedAtUtc).IsRequired();
            entity.HasIndex(r => new { r.UserId, r.Role }).IsUnique();
        });

        modelBuilder.Entity<UserInvitation>(entity =>
        {
            entity.ToTable("user_invitations");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Email).IsRequired().HasMaxLength(256);
            entity.Property(i => i.Roles).IsRequired().HasMaxLength(256);
            entity.Property(i => i.Status).IsRequired().HasMaxLength(32);
            entity.Property(i => i.ExpiresAtUtc).IsRequired();
            entity.Property(i => i.CreatedAtUtc).IsRequired();
            entity.HasIndex(i => i.Email);
        });

        modelBuilder.Entity<Channel>(entity =>
        {
            entity.ToTable("channels");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Slug).IsRequired().HasMaxLength(128);
            entity.HasIndex(c => c.Slug).IsUnique();
            entity.Property(c => c.Name).IsRequired().HasMaxLength(256);
            entity.Property(c => c.Language).IsRequired().HasMaxLength(16);
            entity.Property(c => c.Niche).IsRequired().HasMaxLength(256);
            entity.Property(c => c.Status).IsRequired().HasMaxLength(32);
            entity.Property(c => c.CreatedAtUtc).IsRequired();
            entity.Property(c => c.UpdatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.ToTable("audit_events");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.ActorEmail).IsRequired().HasMaxLength(256);
            entity.Property(a => a.Action).IsRequired().HasMaxLength(128);
            entity.Property(a => a.TargetType).IsRequired().HasMaxLength(128);
            entity.Property(a => a.TargetId).IsRequired().HasMaxLength(128);
            entity.Property(a => a.CorrelationId).HasMaxLength(128);
            entity.Property(a => a.TimestampUtc).IsRequired();
            entity.HasIndex(a => a.TimestampUtc);
        });

        modelBuilder.Entity<DiscoverySource>(entity =>
        {
            entity.ToTable("discovery_sources");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.ChannelId).IsRequired();
            entity.Property(s => s.Name).IsRequired().HasMaxLength(256);
            entity.Property(s => s.OriginUrl).IsRequired().HasMaxLength(1024);
            entity.Property(s => s.SourceType).IsRequired().HasMaxLength(32);
            entity.Property(s => s.Language).IsRequired().HasMaxLength(16);
            entity.Property(s => s.Status).IsRequired().HasMaxLength(32);
            entity.Property(s => s.LastErrorMessage).HasMaxLength(2048);
            entity.Property(s => s.CreatedAtUtc).IsRequired();
            entity.Property(s => s.UpdatedAtUtc).IsRequired();
            entity.HasIndex(s => new { s.ChannelId, s.OriginUrl }).IsUnique();
            entity.HasIndex(s => s.Status);
        });

        modelBuilder.Entity<DiscoveryCandidate>(entity =>
        {
            entity.ToTable("discovery_candidates");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.ChannelId).IsRequired();
            entity.Property(c => c.ExternalUrl).HasMaxLength(1024);
            entity.Property(c => c.NormalizedUrl).HasMaxLength(1024);
            entity.Property(c => c.Title).IsRequired().HasMaxLength(512);
            entity.Property(c => c.Language).IsRequired().HasMaxLength(16);
            entity.Property(c => c.Author).HasMaxLength(256);
            entity.Property(c => c.Status).IsRequired().HasMaxLength(32);
            entity.Property(c => c.OriginType).IsRequired().HasMaxLength(32);
            entity.Property(c => c.SubmitterEmail).HasMaxLength(256);
            entity.Property(c => c.DismissalReason).HasMaxLength(512);
            entity.Property(c => c.EditorialNotes).HasMaxLength(2048);
            entity.Property(c => c.PromotedByEmail).HasMaxLength(256);
            entity.Property(c => c.DiscoveredAtUtc).IsRequired();
            entity.Property(c => c.CreatedAtUtc).IsRequired();
            entity.HasIndex(c => new { c.ChannelId, c.NormalizedUrl });
            entity.HasIndex(c => new { c.Status, c.DiscoveredAtUtc });
            entity.HasIndex(c => c.DiscoverySourceId);
        });

        modelBuilder.Entity<ContentItem>(entity =>
        {
            entity.ToTable("content_items");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.ChannelId).IsRequired();
            entity.Property(c => c.Title).IsRequired().HasMaxLength(512);
            entity.Property(c => c.Slug).IsRequired().HasMaxLength(512);
            entity.Property(c => c.Stage).IsRequired().HasMaxLength(64);
            entity.Property(c => c.Status).IsRequired().HasMaxLength(32);
            entity.Property(c => c.Version).IsRequired().IsConcurrencyToken();
            entity.Property(c => c.CreatedAtUtc).IsRequired();
            entity.Property(c => c.CreatedByEmail).IsRequired().HasMaxLength(256);
            entity.Property(c => c.UpdatedAtUtc).IsRequired();
            entity.Property(c => c.UpdatedByEmail).HasMaxLength(256);

            entity.HasMany(c => c.Evidences)
                  .WithOne()
                  .HasForeignKey(e => e.ContentItemId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => c.ChannelId);
            entity.HasIndex(c => new { c.ChannelId, c.Stage });
            entity.HasIndex(c => new { c.ChannelId, c.Status });
        });

        modelBuilder.Entity<ContentItemEvidence>(entity =>
        {
            entity.ToTable("content_item_evidence");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ContentItemId).IsRequired();
            entity.Property(e => e.OriginUrl).HasMaxLength(1024);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(512);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(32);
            entity.Property(e => e.ObjectStorageKey).HasMaxLength(512);
            entity.Property(e => e.ContentHash).IsRequired().HasMaxLength(128);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2048);
            entity.Property(e => e.Notes).HasMaxLength(2048);
            entity.Property(e => e.Author).HasMaxLength(256);
            entity.Property(e => e.CapturedAtUtc).IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.CreatedByEmail).IsRequired().HasMaxLength(256);

            entity.HasIndex(e => e.ContentItemId);
            entity.HasIndex(e => e.DiscoveryCandidateId);
            entity.HasIndex(e => e.ContentHash);
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<TruthSource>(entity =>
        {
            entity.ToTable("truth_sources");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.ContentItemId).IsRequired();
            entity.Property(t => t.Status).IsRequired().HasMaxLength(32);
            entity.Property(t => t.Version).IsRequired().IsConcurrencyToken();
            entity.Property(t => t.RejectionReason).HasMaxLength(2048);
            entity.Property(t => t.RejectedByEmail).HasMaxLength(256);
            entity.Property(t => t.ApprovedByEmail).HasMaxLength(256);
            entity.Property(t => t.CreatedAtUtc).IsRequired();
            entity.Property(t => t.CreatedByEmail).IsRequired().HasMaxLength(256);
            entity.Property(t => t.UpdatedAtUtc).IsRequired();
            entity.Property(t => t.UpdatedByEmail).HasMaxLength(256);

            entity.HasIndex(t => t.ContentItemId).IsUnique();
            entity.HasIndex(t => t.Status);
        });

        modelBuilder.Entity<TruthSourceVersion>(entity =>
        {
            entity.ToTable("truth_source_versions");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.TruthSourceId).IsRequired();
            entity.Property(v => v.ContentItemId).IsRequired();
            entity.Property(v => v.VersionNumber).IsRequired();
            entity.Property(v => v.SnapshotJson).IsRequired();
            entity.Property(v => v.SupportingEvidenceIdsJson).IsRequired();
            entity.Property(v => v.ChangeSummary).HasMaxLength(1024);
            entity.Property(v => v.CreatedAtUtc).IsRequired();
            entity.Property(v => v.CreatedByEmail).IsRequired().HasMaxLength(256);

            entity.HasIndex(v => v.TruthSourceId);
            entity.HasIndex(v => v.ContentItemId);
            entity.HasIndex(v => new { v.TruthSourceId, v.VersionNumber }).IsUnique();
        });

        modelBuilder.Entity<ContentIdea>(entity =>
        {
            entity.ToTable("content_ideas");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.ContentItemId).IsRequired();
            entity.Property(i => i.TruthSourceId).IsRequired();
            entity.Property(i => i.TruthSourceVersionId).IsRequired();
            entity.Property(i => i.Title).IsRequired().HasMaxLength(256);
            entity.Property(i => i.Angle).IsRequired().HasMaxLength(512);
            entity.Property(i => i.HookStrategy).IsRequired().HasMaxLength(512);
            entity.Property(i => i.AudienceValue).IsRequired().HasMaxLength(512);
            entity.Property(i => i.Format).IsRequired().HasMaxLength(64);
            entity.Property(i => i.IntendedOutcome).IsRequired().HasMaxLength(128);
            entity.Property(i => i.FreshnessClass).IsRequired().HasMaxLength(32);
            entity.Property(i => i.Priority).IsRequired().HasMaxLength(32);
            entity.Property(i => i.Rationale).IsRequired().HasMaxLength(1024);
            entity.Property(i => i.Status).IsRequired().HasMaxLength(32);
            entity.Property(i => i.DismissalNotes).HasMaxLength(1024);
            entity.Property(i => i.SelectedByEmail).HasMaxLength(256);
            entity.Property(i => i.Version).IsRequired().IsConcurrencyToken();
            entity.Property(i => i.CreatedAtUtc).IsRequired();
            entity.Property(i => i.CreatedByEmail).IsRequired().HasMaxLength(256);
            entity.Property(i => i.UpdatedAtUtc).IsRequired();
            entity.Property(i => i.UpdatedByEmail).HasMaxLength(256);

            entity.HasIndex(i => i.ContentItemId);
            entity.HasIndex(i => i.TruthSourceId);
            entity.HasIndex(i => i.TruthSourceVersionId);
            entity.HasIndex(i => new { i.ContentItemId, i.Status });
        });

        modelBuilder.Entity<ContentIdeaVersion>(entity =>
        {
            entity.ToTable("content_idea_versions");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.ContentIdeaId).IsRequired();
            entity.Property(v => v.ContentItemId).IsRequired();
            entity.Property(v => v.TruthSourceId).IsRequired();
            entity.Property(v => v.TruthSourceVersionId).IsRequired();
            entity.Property(v => v.VersionNumber).IsRequired();
            entity.Property(v => v.Title).IsRequired().HasMaxLength(256);
            entity.Property(v => v.Angle).IsRequired().HasMaxLength(512);
            entity.Property(v => v.HookStrategy).IsRequired().HasMaxLength(512);
            entity.Property(v => v.AudienceValue).IsRequired().HasMaxLength(512);
            entity.Property(v => v.Format).IsRequired().HasMaxLength(64);
            entity.Property(v => v.IntendedOutcome).IsRequired().HasMaxLength(128);
            entity.Property(v => v.FreshnessClass).IsRequired().HasMaxLength(32);
            entity.Property(v => v.Priority).IsRequired().HasMaxLength(32);
            entity.Property(v => v.Rationale).IsRequired().HasMaxLength(1024);
            entity.Property(v => v.Status).IsRequired().HasMaxLength(32);
            entity.Property(v => v.DismissalNotes).HasMaxLength(1024);
            entity.Property(v => v.EditedByEmail).IsRequired().HasMaxLength(256);
            entity.Property(v => v.EditedAtUtc).IsRequired();
            entity.Property(v => v.ChangeSummary).HasMaxLength(1024);

            entity.HasIndex(v => v.ContentIdeaId);
            entity.HasIndex(v => v.ContentItemId);
            entity.HasIndex(v => v.TruthSourceId);
            entity.HasIndex(v => new { v.ContentIdeaId, v.VersionNumber }).IsUnique();
        });

        modelBuilder.Entity<EditorialTask>(entity =>
        {
            entity.ToTable("editorial_tasks");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.ChannelId).IsRequired();
            entity.Property(t => t.ContentItemId).IsRequired();
            entity.Property(t => t.TaskType).IsRequired().HasMaxLength(64);
            entity.Property(t => t.Priority).IsRequired().HasMaxLength(32);
            entity.Property(t => t.Status).IsRequired().HasMaxLength(32);
            entity.Property(t => t.AssignedUserEmail).HasMaxLength(256);
            entity.Property(t => t.CompletedByEmail).HasMaxLength(256);
            entity.Property(t => t.CreatedAtUtc).IsRequired();
            entity.Property(t => t.UpdatedAtUtc).IsRequired();
            entity.Property(t => t.CreatedByEmail).IsRequired().HasMaxLength(256);

            entity.HasIndex(t => t.ChannelId);
            entity.HasIndex(t => t.ContentItemId);
            entity.HasIndex(t => new { t.Status, t.Priority });
        });

        modelBuilder.Entity<AiRecommendation>(entity =>
        {
            entity.ToTable("ai_recommendations");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.ChannelId).IsRequired();
            entity.Property(r => r.Capability).IsRequired().HasMaxLength(64);
            entity.Property(r => r.Provider).IsRequired().HasMaxLength(64);
            entity.Property(r => r.Model).IsRequired().HasMaxLength(64);
            entity.Property(r => r.PromptPolicyVersion).IsRequired().HasMaxLength(32);
            entity.Property(r => r.StructuredOutputJson).IsRequired();
            entity.Property(r => r.AcceptedState).IsRequired().HasMaxLength(32);
            entity.Property(r => r.EstimatedCostUsd).HasPrecision(18, 6);
            entity.Property(r => r.CreatedAtUtc).IsRequired();

            entity.HasIndex(r => r.ChannelId);
            entity.HasIndex(r => r.ContentItemId);
            entity.HasIndex(r => r.TruthSourceVersionId);
            entity.HasIndex(r => r.Capability);
        });
    }
}
