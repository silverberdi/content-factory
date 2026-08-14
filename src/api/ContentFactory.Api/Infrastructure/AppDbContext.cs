using ContentFactory.Api.Modules.Audit;
using ContentFactory.Api.Modules.Channels;
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
    }
}
