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

        // 2. Seed initial pilot channel IA Simple ES (Baseline channel)
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

        // In Production, stop after system baseline (Schema, Owner, Canonical Roles, Pilot Channel).
        // Zero representative demo/development content (DiscoverySources, Candidates, ContentItems, TruthSources, Ideas, Tasks) is created in Production or content_factory_prod.
        var isProdDb = !dbContext.Database.IsInMemory() && string.Equals(dbContext.Database.GetDbConnection().Database, "content_factory_prod", StringComparison.OrdinalIgnoreCase);
        if (!isDevelopment || isProdDb)
        {
            logger.LogInformation("Production bootstrap initialization complete (schema, SYSTEM_OWNER, canonical roles, pilot channel). Skipping representative development seed data (isDevelopment={IsDev}, isProdDb={IsProdDb}).", isDevelopment, isProdDb);
            return;
        }

        // 3. Seed initial Discovery Sources for IA Simple ES (Development only)
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

            var idea1Id = Guid.Parse("00000000-0000-0000-0000-000000000401");
            var idea2Id = Guid.Parse("00000000-0000-0000-0000-000000000402");

            var idea1 = new ContentIdea
            {
                Id = idea1Id,
                ContentItemId = contentItem1Id,
                TruthSourceId = truthSource1Id,
                TruthSourceVersionId = truthSourceVersion1.Id,
                Title = "3 Habilidades que la IA NO te Puede Quitar en 2026",
                Angle = "Contraintuitivo / Empoderamiento profesional: Enfocarse en criterio analítico y verificación crítica en lugar de memorizar prompts.",
                HookStrategy = "Pregunta provocadora inicial: '¿Crees que un prompt te salvará el empleo en 2026? Te equivocas: estas 3 habilidades valen 10 veces más.'",
                AudienceValue = "El espectador aprende qué competencias híbridas valoran los empleadores y cómo auditar respuestas de IA.",
                Format = "YouTube Short 30-60s",
                IntendedOutcome = "Inspiración práctica / Retención alta",
                FreshnessClass = IdeaFreshnessClass.Timely,
                Priority = IdeaPriority.High,
                Rationale = "Aprovecha el dato del 68% de empresas priorizando criterio analítico sobre velocidad mecánica.",
                Status = ContentIdeaStatus.Selected,
                SelectedAtUtc = DateTime.UtcNow.AddMinutes(-30),
                SelectedByEmail = ownerEmail,
                Version = 1,
                CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
                CreatedByEmail = ownerEmail,
                UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-30),
                UpdatedByEmail = ownerEmail
            };

            var idea1Version1 = new ContentIdeaVersion
            {
                Id = Guid.NewGuid(),
                ContentIdeaId = idea1Id,
                ContentItemId = contentItem1Id,
                TruthSourceId = truthSource1Id,
                TruthSourceVersionId = truthSourceVersion1.Id,
                VersionNumber = 1,
                Title = idea1.Title,
                Angle = idea1.Angle,
                HookStrategy = idea1.HookStrategy,
                AudienceValue = idea1.AudienceValue,
                Format = idea1.Format,
                IntendedOutcome = idea1.IntendedOutcome,
                FreshnessClass = idea1.FreshnessClass,
                Priority = idea1.Priority,
                Rationale = idea1.Rationale,
                Status = idea1.Status,
                EditedByEmail = ownerEmail,
                EditedAtUtc = DateTime.UtcNow.AddMinutes(-30),
                ChangeSummary = "Idea inicial seleccionada para guionización."
            };

            var idea2 = new ContentIdea
            {
                Id = idea2Id,
                ContentItemId = contentItem1Id,
                TruthSourceId = truthSource1Id,
                TruthSourceVersionId = truthSourceVersion1.Id,
                Title = "El Error de 1.000€ que Cometen al Usar IA en la Oficina",
                Angle = "Alerta de riesgo operativo: Por qué la falta de auditoría humana en respuestas automatizadas genera fallos legales y contables.",
                HookStrategy = "Dato de impacto: 'Un error tonto en un resumen de IA puede costarte miles de euros si no sabes hacer esto...'",
                AudienceValue = "Consejo directo de verificación y checklist de 3 pasos para auditar documentos.",
                Format = "YouTube Short 30-60s",
                IntendedOutcome = "Prevención de errores / Tip accionable",
                FreshnessClass = IdeaFreshnessClass.Evergreen,
                Priority = IdeaPriority.Normal,
                Rationale = "Ángulo alternativo basado en la nota de auditoría y prevención de riesgos operativos.",
                Status = ContentIdeaStatus.Proposed,
                Version = 1,
                CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
                CreatedByEmail = ownerEmail,
                UpdatedAtUtc = DateTime.UtcNow.AddHours(-1),
                UpdatedByEmail = ownerEmail
            };

            var idea2Version1 = new ContentIdeaVersion
            {
                Id = Guid.NewGuid(),
                ContentIdeaId = idea2Id,
                ContentItemId = contentItem1Id,
                TruthSourceId = truthSource1Id,
                TruthSourceVersionId = truthSourceVersion1.Id,
                VersionNumber = 1,
                Title = idea2.Title,
                Angle = idea2.Angle,
                HookStrategy = idea2.HookStrategy,
                AudienceValue = idea2.AudienceValue,
                Format = idea2.Format,
                IntendedOutcome = idea2.IntendedOutcome,
                FreshnessClass = idea2.FreshnessClass,
                Priority = idea2.Priority,
                Rationale = idea2.Rationale,
                Status = idea2.Status,
                EditedByEmail = ownerEmail,
                EditedAtUtc = DateTime.UtcNow.AddHours(-1),
                ChangeSummary = "Propuesta alternativa de ángulo de riesgo operativo."
            };

            // Advance contentItem1 stage to IdeaSelected since idea1 is Selected
            contentItem1.Stage = ContentItemStage.ScriptUnderReview;

            var script1Id = Guid.Parse("00000000-0000-0000-0000-000000000501");
            var script1 = new Script
            {
                Id = script1Id,
                ContentItemId = contentItem1Id,
                ChannelId = pilotChannel.Id,
                ContentIdeaId = idea1Id,
                ContentIdeaVersionId = idea1Id,
                TruthSourceId = truthSource1Id,
                TruthSourceVersionId = truthSourceVersion1.Id,
                Title = "3 Habilidades que la IA NO te Puede Quitar en 2026",
                TargetDurationSeconds = 45,
                PacingWpm = 140,
                EstimatedDurationSeconds = 43.7,
                TotalWordCount = 102,
                Language = "es-ES",
                Status = ScriptStatus.UnderReview,
                SubmittedForReviewAtUtc = DateTime.UtcNow.AddMinutes(-20),
                SubmittedForReviewByEmail = ownerEmail,
                Version = 1,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-25),
                CreatedByEmail = ownerEmail,
                UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-20),
                UpdatedByEmail = ownerEmail
            };

            var sc1 = new ScriptScene
            {
                Id = Guid.NewGuid(),
                ScriptId = script1Id,
                OrderIndex = 1,
                SceneType = SceneType.Hook,
                NarrationText = "¿Crees que un prompt te salvará el empleo en 2026? Te equivocas: estas 3 habilidades valen 10 veces más.",
                VisualPrompt = "Primer plano dinámico a cámara con tipografía de alto contraste resaltando '2026'.",
                EstimatedDurationSeconds = 7.7,
                WordCount = 18,
                EvidenceReferences =
                [
                    new ScriptSceneEvidenceReference
                    {
                        Id = Guid.NewGuid(),
                        ClaimStatement = "El 68% de las empresas priorizan criterio analítico sobre velocidad mecánica al evaluar candidatos con IA.",
                        EditorialNote = "Gancho alineado con la afirmación verificable del estudio de empleo."
                    }
                ]
            };

            var sc2 = new ScriptScene
            {
                Id = Guid.NewGuid(),
                ScriptId = script1Id,
                OrderIndex = 2,
                SceneType = SceneType.Problem,
                NarrationText = "Generar texto en 5 segundos no impresiona a nadie si el resultado contiene alucinaciones o errores contables.",
                VisualPrompt = "B-roll rápido de oficina con documentos resaltados con marcas de error rojas.",
                EstimatedDurationSeconds = 7.7,
                WordCount = 18
            };

            var sc3 = new ScriptScene
            {
                Id = Guid.NewGuid(),
                ScriptId = script1Id,
                OrderIndex = 3,
                SceneType = SceneType.Insight,
                NarrationText = "El 68% de las empresas buscan profesionales que sepan auditar respuestas, verificar fuentes y conectar herramientas a procesos reales.",
                VisualPrompt = "Gráfico animado con el porcentaje del 68% y diagrama de flujo de verificación en 3 pasos.",
                EstimatedDurationSeconds = 9.4,
                WordCount = 22,
                EvidenceReferences =
                [
                    new ScriptSceneEvidenceReference
                    {
                        Id = Guid.NewGuid(),
                        ClaimStatement = "El 68% de las empresas consultadas priorizan el criterio analítico sobre la velocidad al evaluar candidatos que usan IA.",
                        EditorialNote = "Dato empírico verificado en TruthSource."
                    }
                ]
            };

            var sc4 = new ScriptScene
            {
                Id = Guid.NewGuid(),
                ScriptId = script1Id,
                OrderIndex = 4,
                SceneType = SceneType.Climax,
                NarrationText = "El verdadero poder no es automatizar a ciegas: es tener el criterio para saber cuándo una respuesta es correcta.",
                VisualPrompt = "Primer plano seguro del presentador con icono de verificación verde en pantalla.",
                EstimatedDurationSeconds = 9.4,
                WordCount = 22
            };

            var sc5 = new ScriptScene
            {
                Id = Guid.NewGuid(),
                ScriptId = script1Id,
                OrderIndex = 5,
                SceneType = SceneType.CallToAction,
                NarrationText = "Guarda este video para tu próxima reunión y cuéntame en comentarios qué habilidad estás entrenando hoy.",
                VisualPrompt = "Animación sutil de botón guardar y flecha apuntando a comentarios.",
                EstimatedDurationSeconds = 9.4,
                WordCount = 22
            };

            script1.Scenes.AddRange([sc1, sc2, sc3, sc4, sc5]);

            script1.Status = ScriptStatus.Approved;
            script1.ApprovedAtUtc = DateTime.UtcNow.AddMinutes(-10);
            script1.ApprovedByEmail = ownerEmail;
            contentItem1.Stage = ContentItemStage.StoryboardDrafted;

            var script1Version = new ScriptVersion
            {
                Id = Guid.NewGuid(),
                ScriptId = script1Id,
                ContentItemId = contentItem1Id,
                ContentIdeaId = idea1Id,
                ContentIdeaVersionId = idea1Id,
                TruthSourceId = truthSource1Id,
                TruthSourceVersionId = truthSourceVersion1.Id,
                VersionNumber = 1,
                SnapshotJson = JsonSerializer.Serialize(new
                {
                    script1.Id,
                    script1.Title,
                    script1.TargetDurationSeconds,
                    script1.PacingWpm,
                    script1.EstimatedDurationSeconds,
                    script1.TotalWordCount,
                    script1.Language,
                    script1.Status,
                    Scenes = script1.Scenes
                }),
                ChangeSummary = "Guión aprobado por el equipo editorial.",
                Status = ScriptStatus.Approved,
                PacingWpm = 140,
                EstimatedDurationSeconds = 43.7,
                TotalWordCount = 102,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-20),
                CreatedByEmail = ownerEmail
            };

            var storyboard1Id = Guid.Parse("00000000-0000-0000-0000-000000000501");
            var storyboard1 = new Storyboard
            {
                Id = storyboard1Id,
                ContentItemId = contentItem1Id,
                ChannelId = pilotChannel.Id,
                ScriptId = script1Id,
                ScriptVersionId = script1Version.Id,
                TruthSourceId = truthSource1Id,
                TruthSourceVersionId = truthSourceVersion1.Id,
                IsCurrent = true,
                Title = "Storyboard: 3 Habilidades que la IA NO te Puede Quitar en 2026",
                TargetDurationSeconds = 45,
                TotalEstimatedDurationSeconds = 43.7,
                Status = StoryboardStatus.Draft,
                Version = 1,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                CreatedByEmail = ownerEmail,
                UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                UpdatedByEmail = ownerEmail
            };

            var frame1 = new StoryboardFrame
            {
                Id = Guid.NewGuid(),
                StoryboardId = storyboard1Id,
                OrderIndex = 1,
                ScriptSceneId = sc1.Id,
                ScriptSceneOrderIndex = 1,
                FramingIntent = FramingIntent.CloseUp,
                CompositionIntent = "Rostro centrado con mirada desafiante a cámara en encuadre vertical 9:16",
                CameraMotionIntent = CameraMotionIntent.SlowZoomIn,
                Subject = "Profesional joven mirando fijamente a cámara",
                Environment = "Oficina moderna desenfocada con luz lateral fría",
                StyleIntent = "Cinematográfico realista 8k",
                VisualPrompt = "Primer plano en formato vertical 9:16 de profesional mirando a cámara con determinación, iluminación cinematográfica sobria.",
                NegativePrompt = "blur, low quality, distorted hands",
                AudioCue = sc1.NarrationText,
                EstimatedDurationSeconds = sc1.EstimatedDurationSeconds,
                OnScreenText = "3 HABILIDADES QUE LA IA NO SUSTITUIRÁ",
                TransitionIntent = TransitionIntent.Cut,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5)
            };

            var frame2 = new StoryboardFrame
            {
                Id = Guid.NewGuid(),
                StoryboardId = storyboard1Id,
                OrderIndex = 2,
                ScriptSceneId = sc2.Id,
                ScriptSceneOrderIndex = 2,
                FramingIntent = FramingIntent.MediumShot,
                CompositionIntent = "Sujeto a la izquierda con overlay de error en tercio derecho",
                CameraMotionIntent = CameraMotionIntent.TrackingShot,
                Subject = "Pantalla de laptop mostrando código con alerta roja de fallo de IA",
                Environment = "Escritorio de trabajo con iluminación tenue",
                StyleIntent = "Dark Tech Editorial",
                VisualPrompt = "Plano medio vertical 9:16 de interfaz de software con aviso de error en datos no auditados.",
                NegativePrompt = "blur, cartoonish",
                AudioCue = sc2.NarrationText,
                EstimatedDurationSeconds = sc2.EstimatedDurationSeconds,
                OnScreenText = "MEMORIZAR PROMPTS = OBSOLETO",
                TransitionIntent = TransitionIntent.Dissolve,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5)
            };

            var frame3 = new StoryboardFrame
            {
                Id = Guid.NewGuid(),
                StoryboardId = storyboard1Id,
                OrderIndex = 3,
                ScriptSceneId = sc3.Id,
                ScriptSceneOrderIndex = 3,
                FramingIntent = FramingIntent.WideShot,
                CompositionIntent = "Diagrama de flujo de auditoría de 3 pasos centrado",
                CameraMotionIntent = CameraMotionIntent.SlowZoomIn,
                Subject = "Gráfico holográfico interactivo con métrica del 68%",
                Environment = "Fondo minimalista tecnológico",
                StyleIntent = "3D Render Isométrico Limpio",
                VisualPrompt = "Animación gráfica 9:16 de estadística 68% empresas buscando profesionales con criterio analítico.",
                NegativePrompt = "messy layout, small unreadable text",
                AudioCue = sc3.NarrationText,
                EstimatedDurationSeconds = sc3.EstimatedDurationSeconds,
                OnScreenText = "68% EMPRESAS PIDEN CRITERIO",
                TransitionIntent = TransitionIntent.Cut,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5)
            };

            var frame4 = new StoryboardFrame
            {
                Id = Guid.NewGuid(),
                StoryboardId = storyboard1Id,
                OrderIndex = 4,
                ScriptSceneId = sc4.Id,
                ScriptSceneOrderIndex = 4,
                FramingIntent = FramingIntent.MediumShot,
                CompositionIntent = "Presentador en tercio medio con icono de check verde verificado",
                CameraMotionIntent = CameraMotionIntent.Static,
                Subject = "Operador experto validando reporte en tablet",
                Environment = "Sala de reuniones con iluminación balanceada",
                StyleIntent = "Fotografía editorial sobria",
                VisualPrompt = "Plano medio vertical 9:16 de profesional validando datos con confianza, estética premium.",
                NegativePrompt = "artificial smile, low res",
                AudioCue = sc4.NarrationText,
                EstimatedDurationSeconds = sc4.EstimatedDurationSeconds,
                OnScreenText = "CRITERIO > VELOCIDAD CIEGA",
                TransitionIntent = TransitionIntent.Cut,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5)
            };

            var frame5 = new StoryboardFrame
            {
                Id = Guid.NewGuid(),
                StoryboardId = storyboard1Id,
                OrderIndex = 5,
                ScriptSceneId = sc5.Id,
                ScriptSceneOrderIndex = 5,
                FramingIntent = FramingIntent.CloseUp,
                CompositionIntent = "Botón de guardado y flecha animada en tercio inferior",
                CameraMotionIntent = CameraMotionIntent.PanUp,
                Subject = "Mano interactuando con smartphone guardando video",
                Environment = "Fondo desenfocado de canal IA Simple ES",
                StyleIntent = "Clean UI Overlay",
                VisualPrompt = "Primer plano vertical 9:16 de interacción táctil guardando contenido con animación dinámica.",
                NegativePrompt = "distorted hand, blurry",
                AudioCue = sc5.NarrationText,
                EstimatedDurationSeconds = sc5.EstimatedDurationSeconds,
                OnScreenText = "GUARDA ESTE VIDEO",
                TransitionIntent = TransitionIntent.Dissolve,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5)
            };

            storyboard1.Frames.AddRange([frame1, frame2, frame3, frame4, frame5]);

            var assetPlan1Id = Guid.NewGuid();
            var assetPlan1 = new AssetPlan
            {
                Id = assetPlan1Id,
                StoryboardId = storyboard1Id,
                ContentItemId = contentItem1Id,
                Status = AssetPlanStatus.Planned,
                Version = 1,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5)
            };

            foreach (var fr in storyboard1.Frames)
            {
                assetPlan1.Requirements.Add(new AssetRequirement
                {
                    Id = Guid.NewGuid(),
                    AssetPlanId = assetPlan1Id,
                    FrameId = fr.Id,
                    FrameOrderIndex = fr.OrderIndex,
                    AssetType = AssetType.AiImage,
                    AspectRatio = "9:16",
                    VisualPrompt = fr.VisualPrompt,
                    NegativePrompt = fr.NegativePrompt,
                    StyleIntent = fr.StyleIntent,
                    MotionIntent = fr.CameraMotionIntent,
                    TargetDurationSeconds = fr.EstimatedDurationSeconds,
                    VoiceIntent = "Locución sobria en español",
                    MusicMood = "Tech Ambient",
                    SubtitleProfile = "Captions cinéticos centrados",
                    OverlaySpecification = fr.OnScreenText,
                    CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                    UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5)
                });
            }

            assetPlan1.Requirements.Add(new AssetRequirement
            {
                Id = Guid.NewGuid(),
                AssetPlanId = assetPlan1Id,
                AssetType = AssetType.TtsVoiceover,
                AspectRatio = "N/A",
                VisualPrompt = "Pista completa de narración en off",
                VoiceIntent = "Español neutro con cadencia profesional",
                TargetDurationSeconds = 43.7,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5)
            });

            assetPlan1.Requirements.Add(new AssetRequirement
            {
                Id = Guid.NewGuid(),
                AssetPlanId = assetPlan1Id,
                AssetType = AssetType.SubtitleTrack,
                AspectRatio = "9:16",
                VisualPrompt = "Captions cinéticos sincronizados",
                SubtitleProfile = "Spanish formatted kinetic captions",
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5)
            });

            storyboard1.AssetPlan = assetPlan1;

            var storyboard1Version = new StoryboardVersion
            {
                Id = Guid.NewGuid(),
                StoryboardId = storyboard1Id,
                ContentItemId = contentItem1Id,
                ScriptId = script1Id,
                ScriptVersionId = script1Version.Id,
                TruthSourceId = truthSource1Id,
                TruthSourceVersionId = truthSourceVersion1.Id,
                VersionNumber = 1,
                SnapshotJson = JsonSerializer.Serialize(new
                {
                    storyboard1.Id,
                    storyboard1.Title,
                    storyboard1.TargetDurationSeconds,
                    storyboard1.TotalEstimatedDurationSeconds,
                    storyboard1.Status,
                    Frames = storyboard1.Frames,
                    AssetPlan = storyboard1.AssetPlan
                }),
                ChangeSummary = "Planificación inicial de tomas visuales y especificación de activos de producción.",
                Status = StoryboardStatus.Draft,
                TotalEstimatedDurationSeconds = 43.7,
                TotalFrameCount = 5,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                CreatedByEmail = ownerEmail
            };

            var editorialTask2 = new EditorialTask
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000332"),
                ChannelId = pilotChannel.Id,
                ContentItemId = contentItem1Id,
                TaskType = EditorialTaskType.ReviewStoryboard,
                Priority = EditorialTaskPriority.Normal,
                Status = EditorialTaskStatus.Pending,
                AssignedUserEmail = ownerEmail,
                DueDateUtc = DateTime.UtcNow.AddHours(12),
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                CreatedByEmail = ownerEmail
            };

            dbContext.ContentItems.AddRange(contentItem1, contentItem2);
            dbContext.ContentItemEvidences.AddRange(evidence1, evidence2, evidence3);
            dbContext.TruthSources.AddRange(truthSource1, truthSource2);
            dbContext.TruthSourceVersions.Add(truthSourceVersion1);
            dbContext.ContentIdeas.AddRange(idea1, idea2);
            dbContext.ContentIdeaVersions.AddRange(idea1Version1, idea2Version1);
            dbContext.Scripts.Add(script1);
            dbContext.ScriptVersions.Add(script1Version);
            dbContext.Storyboards.Add(storyboard1);
            dbContext.StoryboardVersions.Add(storyboard1Version);
            dbContext.EditorialTasks.AddRange(editorialTask1, editorialTask2);

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded initial representative ContentItems, Evidences, TruthSources, ContentIdeas, Scripts, Storyboards, and EditorialTasks for 'IA Simple ES'.");
        }
    }
}
