using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ContentFactory.Api.Modules.Channels;
using ContentFactory.Api.Modules.Content;
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

        var candidate1Id = Guid.Parse("00000000-0000-0000-0000-000000000201");
        var candidate2Id = Guid.Parse("00000000-0000-0000-0000-000000000202");
        var candidate3Id = Guid.Parse("00000000-0000-0000-0000-000000000203");
        var candidate4Id = Guid.Parse("00000000-0000-0000-0000-000000000204");

        if (existingCandidates == 0)
        {
            var candidates = new List<DiscoveryCandidate>
            {
                new()
                {
                    Id = candidate1Id,
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
                    Status = DiscoveryCandidateStatus.Promoted,
                    OriginType = OriginType.Automated,
                    PromotedAtUtc = DateTime.UtcNow.AddHours(-1),
                    PromotedByEmail = "silverio.bernal@gmail.com",
                    CreatedAtUtc = DateTime.UtcNow
                },
                new()
                {
                    Id = candidate2Id,
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
                    Id = candidate3Id,
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
                    Id = candidate4Id,
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
                    Status = DiscoveryCandidateStatus.Promoted,
                    OriginType = OriginType.Manual,
                    SubmitterEmail = "silverio.bernal@gmail.com",
                    PromotedAtUtc = DateTime.UtcNow.AddMinutes(-10),
                    PromotedByEmail = "silverio.bernal@gmail.com",
                    CreatedAtUtc = DateTime.UtcNow
                }
            };

            dbContext.DiscoveryCandidates.AddRange(candidates);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded initial representative Discovery Candidates for 'IA Simple ES'.");
        }

        // 5. Seed ContentItems, ContentItemEvidence, TruthSources, and EditorialTasks for CF-003
        var existingContentItems = await dbContext.ContentItems
            .Where(c => c.ChannelId == pilotChannel.Id)
            .CountAsync(cancellationToken);

        if (existingContentItems == 0)
        {
            var contentItem1Id = Guid.Parse("00000000-0000-0000-0000-000000000301");
            var evidence1Id = Guid.Parse("00000000-0000-0000-0000-000000000311");
            var evidence2Id = Guid.Parse("00000000-0000-0000-0000-000000000312");
            var truthSource1Id = Guid.Parse("00000000-0000-0000-0000-000000000321");

            var contentItem1 = new ContentItem
            {
                Id = contentItem1Id,
                ChannelId = pilotChannel.Id,
                Title = "3 Habilidades Clave que la IA No Reemplaza en 2026",
                Slug = "3-habilidades-clave-que-la-ia-no-reemplaza-2026",
                Stage = ContentItemStage.TruthSourceApproved,
                Status = ContentItemStatus.Active,
                Version = 2,
                CreatedAtUtc = DateTime.UtcNow.AddHours(-6),
                CreatedByEmail = ownerEmail,
                UpdatedAtUtc = DateTime.UtcNow.AddHours(-1),
                UpdatedByEmail = ownerEmail
            };

            var evidence1 = new ContentItemEvidence
            {
                Id = evidence1Id,
                ContentItemId = contentItem1Id,
                DiscoveryCandidateId = candidate3Id,
                OriginUrl = "https://elpais.com/tecnologia/2026/08/14/empleo-ia-formacion.html",
                Title = "El impacto real de la inteligencia artificial en el empleo junior: qué demandan las empresas hoy",
                Role = EvidenceRole.PrimaryLead,
                Status = EvidenceStatus.Captured,
                RawContent = "Estudio empírico sobre demanda de capacidades híbridas: criterio analítico + interacción con asistentes autónomos en entornos profesionales.",
                ExtractedText = "Las empresas valoran el criterio analítico, la auditoría de respuestas y la capacidad de orquestar flujos de trabajo sobre la simple generación automatizada de textos.",
                ContentHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("Las empresas valoran el criterio analítico, la auditoría de respuestas y la capacidad de orquestar flujos de trabajo sobre la simple generación automatizada de textos."))),
                CapturedAtUtc = DateTime.UtcNow.AddHours(-6),
                CreatedAtUtc = DateTime.UtcNow.AddHours(-6),
                CreatedByEmail = ownerEmail
            };

            var evidence2 = new ContentItemEvidence
            {
                Id = evidence2Id,
                ContentItemId = contentItem1Id,
                DiscoveryCandidateId = candidate4Id,
                OriginUrl = null,
                Title = "Nota editorial: Auditoría y criterio humano en flujos de IA",
                Role = EvidenceRole.SupportingEvidence,
                Status = EvidenceStatus.Captured,
                RawContent = "Nota de contexto: Enfatizar que la verificación de fuentes evita errores costosos en tareas legales, financieras y de atención al cliente.",
                ExtractedText = "Nota de contexto: Enfatizar que la verificación de fuentes evita errores costosos en tareas legales, financieras y de atención al cliente.",
                ContentHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("Nota de contexto: Enfatizar que la verificación de fuentes evita errores costosos en tareas legales, financieras y de atención al cliente."))),
                CapturedAtUtc = DateTime.UtcNow.AddHours(-5),
                CreatedAtUtc = DateTime.UtcNow.AddHours(-5),
                CreatedByEmail = ownerEmail
            };

            var claims1 = new List<VerifiableClaimDto>
            {
                new("El 68% de las empresas consultadas priorizan el criterio analítico sobre la velocidad al evaluar candidatos que usan IA.", "El País Empleo 2026", evidence1Id),
                new("La auditoría humana de datos previene riesgos legales y errores operativos en tareas administrativas.", "Nota editorial IA Simple", evidence2Id)
            };

            var truthSource1 = new TruthSource
            {
                Id = truthSource1Id,
                ContentItemId = contentItem1Id,
                Status = TruthSourceStatus.Approved,
                Summary = "Síntesis factual sobre cómo el criterio analítico y la capacidad de auditar respuestas diferencian a los profesionales que usan IA en 2026 frente a la automatización mecánica.",
                KeyIdeasJson = JsonSerializer.Serialize(new List<string>
                {
                    "El criterio analítico y la verificación de fuentes superan a la simple memorización de prompts",
                    "Las empresas buscan perfiles híbridos que sepan auditar respuestas y conectar herramientas con flujos reales",
                    "La adopción pragmática de IA genera ventaja sin necesidad de conocimientos avanzados de programación"
                }),
                VerifiableClaimsJson = JsonSerializer.Serialize(claims1),
                EvidenceReferencesJson = JsonSerializer.Serialize(new List<Guid> { evidence1Id, evidence2Id }),
                RiskNotes = "Evitar generalizaciones sobre despidos masivos o promesas irreales de ganancias inmediatas.",
                DoNotSayConstraintsJson = JsonSerializer.Serialize(new List<string>
                {
                    "No usar afirmaciones sensacionalistas como 'la IA te quitará el trabajo mañana'",
                    "No prometer fórmulas mágicas de productividad sin esfuerzo",
                    "Evitar tecnicismos de programación innecesarios"
                }),
                PossibleAnglesJson = JsonSerializer.Serialize(new List<string>
                {
                    "Las 3 habilidades que la IA no reemplaza en 2026",
                    "Cómo auditar respuestas de IA en tu trabajo diario"
                }),
                LocalizationNotes = "Español neutro y accesible para España e Hispanoamérica.",
                ApprovedAtUtc = DateTime.UtcNow.AddHours(-1),
                ApprovedByEmail = ownerEmail,
                Version = 2,
                CreatedAtUtc = DateTime.UtcNow.AddHours(-5),
                CreatedByEmail = ownerEmail,
                UpdatedAtUtc = DateTime.UtcNow.AddHours(-1),
                UpdatedByEmail = ownerEmail
            };

            var truthSourceVersion1 = new TruthSourceVersion
            {
                Id = Guid.NewGuid(),
                TruthSourceId = truthSource1Id,
                ContentItemId = contentItem1Id,
                VersionNumber = 1,
                SnapshotJson = JsonSerializer.Serialize(truthSource1),
                SupportingEvidenceIdsJson = JsonSerializer.Serialize(new List<Guid> { evidence1Id, evidence2Id }),
                ChangeSummary = "Borrador inicial generado por IA y ajustado con guardrails anti-sensacionalismo.",
                CreatedAtUtc = DateTime.UtcNow.AddHours(-5),
                CreatedByEmail = ownerEmail
            };

            // ContentItem 2 (Drafting / UnderReview with pending task)
            var contentItem2Id = Guid.Parse("00000000-0000-0000-0000-000000000302");
            var evidence3Id = Guid.Parse("00000000-0000-0000-0000-000000000313");
            var truthSource2Id = Guid.Parse("00000000-0000-0000-0000-000000000322");

            var contentItem2 = new ContentItem
            {
                Id = contentItem2Id,
                ChannelId = pilotChannel.Id,
                Title = "Modelos de Razonamiento en la Oficina: Menos Errores y Más Control",
                Slug = "modelos-razonamiento-en-la-oficina-menos-errores-mas-control",
                Stage = ContentItemStage.DraftingEvidence,
                Status = ContentItemStatus.Active,
                Version = 1,
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
                CreatedByEmail = ownerEmail,
                UpdatedAtUtc = DateTime.UtcNow.AddHours(-2),
                UpdatedByEmail = ownerEmail
            };

            var evidence3 = new ContentItemEvidence
            {
                Id = evidence3Id,
                ContentItemId = contentItem2Id,
                DiscoveryCandidateId = candidate1Id,
                OriginUrl = "https://www.xataka.com/inteligencia-artificial/modelos-razonamiento-transformacion-empresarial",
                Title = "Cómo los nuevos modelos de razonamiento reducen errores en flujos de trabajo empresariales",
                Role = EvidenceRole.PrimaryLead,
                Status = EvidenceStatus.Captured,
                RawContent = "Los modelos de razonamiento con presupuesto de cómputo en inferencia demuestran reducciones de alucinación del 70% en tareas operativas complejas.",
                ExtractedText = "Los nuevos modelos de razonamiento verifican cada paso lógico antes de emitir una respuesta final, reduciendo discrepancias en extracción documental y resúmenes estructurados.",
                ContentHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("Los nuevos modelos de razonamiento verifican cada paso lógico antes de emitir una respuesta final."))),
                CapturedAtUtc = DateTime.UtcNow.AddHours(-2),
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
                CreatedByEmail = ownerEmail
            };

            var claims2 = new List<VerifiableClaimDto>
            {
                new("El cálculo en inferencia reduce drásticamente inconsistencias en datos tabulares y documentación legal.", "Xataka IA 2026", evidence3Id)
            };

            var truthSource2 = new TruthSource
            {
                Id = truthSource2Id,
                ContentItemId = contentItem2Id,
                Status = TruthSourceStatus.UnderReview,
                Summary = "Evaluación de modelos de razonamiento estructurado aplicados a tareas administrativas cotidianas sin requerir infraestructura compleja.",
                KeyIdeasJson = JsonSerializer.Serialize(new List<string>
                {
                    "La verificación paso a paso disminuye fallos en resúmenes técnicos",
                    "Permite a pequeñas empresas delegar filtrado de documentos con supervisión humana"
                }),
                VerifiableClaimsJson = JsonSerializer.Serialize(claims2),
                EvidenceReferencesJson = JsonSerializer.Serialize(new List<Guid> { evidence3Id }),
                RiskNotes = "Verificar que las herramientas citadas dispongan de versión accesible para pymes.",
                DoNotSayConstraintsJson = JsonSerializer.Serialize(new List<string>
                {
                    "No vender la tecnología como infalible al 100%",
                    "Explicar con claridad que la supervisión humana sigue siendo obligatoria"
                }),
                PossibleAnglesJson = JsonSerializer.Serialize(new List<string>
                {
                    "Por qué los nuevos modelos de IA piensan antes de responder",
                    "Cómo usar IA de razonamiento para revisar contratos y presupuestos"
                }),
                LocalizationNotes = "Español directo y explicativo.",
                Version = 1,
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
                CreatedByEmail = ownerEmail,
                UpdatedAtUtc = DateTime.UtcNow.AddHours(-2),
                UpdatedByEmail = ownerEmail
            };

            var editorialTask1 = new EditorialTask
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000331"),
                ChannelId = pilotChannel.Id,
                ContentItemId = contentItem2Id,
                TaskType = EditorialTaskType.ReviewTruthSource,
                Priority = EditorialTaskPriority.High,
                Status = EditorialTaskStatus.Pending,
                AssignedUserEmail = ownerEmail,
                DueDateUtc = DateTime.UtcNow.AddHours(24),
                CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
                UpdatedAtUtc = DateTime.UtcNow.AddHours(-2),
                CreatedByEmail = ownerEmail
            };

            dbContext.ContentItems.AddRange(contentItem1, contentItem2);
            dbContext.ContentItemEvidences.AddRange(evidence1, evidence2, evidence3);
            dbContext.TruthSources.AddRange(truthSource1, truthSource2);
            dbContext.TruthSourceVersions.Add(truthSourceVersion1);
            dbContext.EditorialTasks.Add(editorialTask1);

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded initial representative ContentItems, Evidences, TruthSources, and EditorialTasks for 'IA Simple ES'.");
        }
    }
}
