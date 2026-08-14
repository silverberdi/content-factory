using ContentFactory.Api.Modules.Channels;
using ContentFactory.Api.Modules.Identity;
using Microsoft.EntityFrameworkCore;

namespace ContentFactory.Api.Infrastructure;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider, bool isDevelopment, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        // Ensure database tables exist
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        // 1. Seed or verify SYSTEM_OWNER
        const string ownerEmail = "silverio.bernal@gmail.com";
        var owner = await dbContext.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email == ownerEmail, cancellationToken);

        if (owner == null)
        {
            owner = new User
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Email = ownerEmail,
                IsOwner = true,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            owner.Roles.Add(new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = owner.Id,
                Role = Roles.Technical,
                AssignedAtUtc = DateTime.UtcNow
            });

            owner.Roles.Add(new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = owner.Id,
                Role = Roles.Editorial,
                AssignedAtUtc = DateTime.UtcNow
            });

            dbContext.Users.Add(owner);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("SYSTEM_OWNER '{OwnerEmail}' initialized.", ownerEmail);
        }
        else
        {
            // Ensure owner state invariants
            var updated = false;
            if (!owner.IsOwner) { owner.IsOwner = true; updated = true; }
            if (!owner.IsActive) { owner.IsActive = true; updated = true; }
            if (!owner.Roles.Any(r => r.Role == Roles.Technical))
            {
                owner.Roles.Add(new UserRole { Id = Guid.NewGuid(), UserId = owner.Id, Role = Roles.Technical, AssignedAtUtc = DateTime.UtcNow });
                updated = true;
            }
            if (!owner.Roles.Any(r => r.Role == Roles.Editorial))
            {
                owner.Roles.Add(new UserRole { Id = Guid.NewGuid(), UserId = owner.Id, Role = Roles.Editorial, AssignedAtUtc = DateTime.UtcNow });
                updated = true;
            }
            if (updated)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        // 2. Seed initial pilot channel IA Simple ES (in both dev and prod baseline)
        const string pilotSlug = "ia-simple-es";
        var pilotChannel = await dbContext.Channels.FirstOrDefaultAsync(c => c.Slug == pilotSlug, cancellationToken);
        if (pilotChannel == null)
        {
            pilotChannel = new Channel
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000010"),
                Slug = pilotSlug,
                Name = "IA Simple ES",
                Language = "es",
                Niche = "AI and future of work",
                Status = ChannelStatus.Pilot,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            dbContext.Channels.Add(pilotChannel);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Initial pilot channel 'IA Simple ES' initialized.");
        }
    }
}
