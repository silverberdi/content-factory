using ContentFactory.Api.Modules.Channels;
using ContentFactory.Api.Modules.Discovery;
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

        // 0. Schema Migration (Canonical EF Core Migrations for Relational, EnsureCreated for InMemory)
        if (dbContext.Database.IsRelational())
        {
            logger.LogInformation("Applying EF Core migrations to relational database...");
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }

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

        // 3. Seed initial Discovery Sources for IA Simple ES
        var existingSources = await dbContext.DiscoverySources
            .Where(s => s.ChannelId == pilotChannel.Id)
            .ToListAsync(cancellationToken);

        var seededSource1Id = Guid.Parse("00000000-0000-0000-0000-000000000101");
        var seededSource2Id = Guid.Parse("00000000-0000-0000-0000-000000000102");
        var seededSource3Id = Guid.Parse("00000000-0000-0000-0000-000000000103");

        if (existingSources.Count == 0)
        {
            var sources = new List<DiscoverySource>
            {
                new()
                {
                    Id = seededSource1Id,
                    ChannelId = pilotChannel.Id,
                    Name = "Xataka Inteligencia Artificial",
                    OriginUrl = "https://www.xataka.com/categoria/inteligencia-artificial/feed",
                    SourceType = SourceType.Feed,
                    Language = "es",
                    PollingIntervalMinutes = 60,
                    Status = DiscoverySourceStatus.Active,
                    LastSyncAtUtc = DateTime.UtcNow.AddMinutes(-20),
                    NextSyncAtUtc = DateTime.UtcNow.AddMinutes(40),
                    FailureCount = 0,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                },
                new()
                {
                    Id = seededSource2Id,
                    ChannelId = pilotChannel.Id,
                    Name = "Genbeta IA",
                    OriginUrl = "https://www.genbeta.com/categoria/inteligencia-artificial/feed",
                    SourceType = SourceType.Feed,
                    Language = "es",
                    PollingIntervalMinutes = 120,
                    Status = DiscoverySourceStatus.Active,
                    LastSyncAtUtc = DateTime.UtcNow.AddMinutes(-45),
                    NextSyncAtUtc = DateTime.UtcNow.AddMinutes(75),
                    FailureCount = 0,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                },
                new()
                {
                    Id = seededSource3Id,
                    ChannelId = pilotChannel.Id,
                    Name = "MIT Technology Review (Español)",
                    OriginUrl = "https://www.technologyreview.es/feed/temas/inteligencia-artificial",
                    SourceType = SourceType.Feed,
                    Language = "es",
                    PollingIntervalMinutes = 180,
                    Status = DiscoverySourceStatus.Active,
                    LastSyncAtUtc = DateTime.UtcNow.AddHours(-1),
                    NextSyncAtUtc = DateTime.UtcNow.AddHours(2),
                    FailureCount = 0,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                }
            };

            dbContext.DiscoverySources.AddRange(sources);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded default Discovery Sources for 'IA Simple ES'.");
        }

        // 4. Seed initial Discovery Candidates for development human verification
        var existingCandidates = await dbContext.DiscoveryCandidates
            .Where(c => c.ChannelId == pilotChannel.Id)
            .CountAsync(cancellationToken);

        if (existingCandidates == 0)
        {
            var candidates = new List<DiscoveryCandidate>
            {
                new()
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000201"),
                    ChannelId = pilotChannel.Id,
                    DiscoverySourceId = seededSource1Id,
                    ExternalUrl = "https://www.xataka.com/inteligencia-artificial/modelos-razonamiento-transformacion-empresarial",
                    NormalizedUrl = "https://www.xataka.com/inteligencia-artificial/modelos-razonamiento-transformacion-empresarial",
                    Title = "Cómo los nuevos modelos de razonamiento reducen errores en flujos de trabajo empresariales",
                    Summary = "Análisis sobre la transición de LLMs generativos a modelos estructurados con cadena de verificación en pymes.",
                    RawContent = "Los modelos de razonamiento con presupuesto de cómputo en inferencia demuestran reducciones de alucinación del 70% en tareas operativas complejas.",
                    Language = "es",
                    Author = "Redacción Xataka",
                    DiscoveredAtUtc = DateTime.UtcNow.AddHours(-2),
                    Status = DiscoveryCandidateStatus.PendingReview,
                    OriginType = OriginType.Automated,
                    CreatedAtUtc = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000202"),
                    ChannelId = pilotChannel.Id,
                    DiscoverySourceId = seededSource2Id,
                    ExternalUrl = "https://www.genbeta.com/herramientas-ia/automatizacion-rutinas-administrativas-oficina",
                    NormalizedUrl = "https://www.genbeta.com/herramientas-ia/automatizacion-rutinas-administrativas-oficina",
                    Title = "Las 5 herramientas de IA más prácticas para ahorrar 10 horas semanales en la oficina",
                    Summary = "Guía directa y sin tecnicismos sobre automatización de correo, resúmenes de llamadas y extracción de datos.",
                    RawContent = "Recopilación de utilidades gratuitas y accesibles para profesionales no técnicos que buscan ganar productividad inmediata.",
                    Language = "es",
                    Author = "Genbeta Prod",
                    DiscoveredAtUtc = DateTime.UtcNow.AddHours(-5),
                    Status = DiscoveryCandidateStatus.PendingReview,
                    OriginType = OriginType.Automated,
                    CreatedAtUtc = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000203"),
                    ChannelId = pilotChannel.Id,
                    DiscoverySourceId = null,
                    ExternalUrl = "https://elpais.com/tecnologia/2026/08/14/empleo-ia-formacion.html",
                    NormalizedUrl = "https://elpais.com/tecnologia/2026/08/14/empleo-ia-formacion.html",
                    Title = "El impacto real de la inteligencia artificial en el empleo junior: qué demandan las empresas hoy",
                    Summary = "Reportaje especial sobre habilidades complementarias y adaptación en sectores de servicios en España e Hispanoamérica.",
                    RawContent = "Estudio empírico sobre demanda de capacidades híbridas: criterio analítico + interacción con asistentes autónomos.",
                    Language = "es",
                    Author = "silverio.bernal@gmail.com",
                    DiscoveredAtUtc = DateTime.UtcNow.AddHours(-8),
                    Status = DiscoveryCandidateStatus.Promoted,
                    OriginType = OriginType.Manual,
                    SubmitterEmail = "silverio.bernal@gmail.com",
                    PromotedAtUtc = DateTime.UtcNow.AddHours(-1),
                    PromotedByEmail = "silverio.bernal@gmail.com",
                    EditorialNotes = "Excelente ángulo para un YouTube Short de 50s: 'Las 3 habilidades que la IA no reemplaza en 2026'.",
                    CreatedAtUtc = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000204"),
                    ChannelId = pilotChannel.Id,
                    DiscoverySourceId = null,
                    ExternalUrl = null,
                    NormalizedUrl = null,
                    Title = "Idea editorial: Comparativa de asistentes de voz locales vs nube para privacidad",
                    Summary = "Nota rápida: Evaluar si los usuarios prefieren modelos pequeños en el móvil (on-device) por seguridad de datos.",
                    RawContent = "Nota rápida: Evaluar si los usuarios prefieren modelos pequeños en el móvil (on-device) por seguridad de datos.",
                    Language = "es",
                    Author = "silverio.bernal@gmail.com",
                    DiscoveredAtUtc = DateTime.UtcNow.AddMinutes(-30),
                    Status = DiscoveryCandidateStatus.PendingReview,
                    OriginType = OriginType.Manual,
                    SubmitterEmail = "silverio.bernal@gmail.com",
                    CreatedAtUtc = DateTime.UtcNow
                }
            };

            dbContext.DiscoveryCandidates.AddRange(candidates);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded initial representative Discovery Candidates for 'IA Simple ES'.");
        }
    }
}
