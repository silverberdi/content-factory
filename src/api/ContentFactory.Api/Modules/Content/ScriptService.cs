using System.Text.Json;
using System.Text.RegularExpressions;
using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Ai;
using ContentFactory.Api.Modules.Audit;
using Microsoft.EntityFrameworkCore;

namespace ContentFactory.Api.Modules.Content;

public partial class ScriptService(
    AppDbContext dbContext,
    IAiProviderRouter aiProviderRouter,
    IAuditService auditService,
    ILogger<ScriptService> logger) : IScriptService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<ScriptDto?> GetScriptByContentItemIdAsync(
        Guid contentItemId,
        CancellationToken cancellationToken = default)
    {
        var script = await dbContext.Scripts
            .AsNoTracking()
            .Include(s => s.Scenes.OrderBy(sc => sc.OrderIndex))
                .ThenInclude(sc => sc.EvidenceReferences)
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId, cancellationToken);

        if (script == null) return null;

        var (isStale, staleReason) = await EvaluateStaleLineageAsync(script, cancellationToken);
        return MapToDto(script, isStale, staleReason);
    }

    public async Task<ScriptDto?> GetScriptByIdAsync(
        Guid contentItemId,
        Guid scriptId,
        CancellationToken cancellationToken = default)
    {
        var script = await dbContext.Scripts
            .AsNoTracking()
            .Include(s => s.Scenes.OrderBy(sc => sc.OrderIndex))
                .ThenInclude(sc => sc.EvidenceReferences)
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId && s.Id == scriptId, cancellationToken);

        if (script == null) return null;

        var (isStale, staleReason) = await EvaluateStaleLineageAsync(script, cancellationToken);
        return MapToDto(script, isStale, staleReason);
    }

    public async Task<List<ScriptVersionDto>> GetScriptVersionsAsync(
        Guid contentItemId,
        Guid scriptId,
        CancellationToken cancellationToken = default)
    {
        var versions = await dbContext.ScriptVersions
            .AsNoTracking()
            .Where(v => v.ContentItemId == contentItemId && v.ScriptId == scriptId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);

        return versions.Select(MapToVersionDto).ToList();
    }

    public async Task<ScriptVersionDto?> GetScriptVersionAsync(
        Guid contentItemId,
        Guid scriptId,
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        var version = await dbContext.ScriptVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.ContentItemId == contentItemId && v.ScriptId == scriptId && v.Id == versionId, cancellationToken);

        return version != null ? MapToVersionDto(version) : null;
    }

    public async Task<ScriptDto> CreateScriptAsync(
        Guid contentItemId,
        CreateScriptRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.");

        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == contentItemId, cancellationToken)
            ?? throw new ArgumentException("ContentItem not found.");

        var activeIdea = await dbContext.ContentIdeas
            .FirstOrDefaultAsync(i => i.ContentItemId == contentItemId && i.Status == ContentIdeaStatus.Selected, cancellationToken)
            ?? throw new InvalidOperationException("Script creation requires an active selected ContentIdea and an approved TruthSource.");

        var truthSource = await dbContext.TruthSources
            .FirstOrDefaultAsync(t => t.ContentItemId == contentItemId, cancellationToken)
            ?? throw new InvalidOperationException("Script creation requires an active selected ContentIdea and an approved TruthSource.");

        if (truthSource.Status != TruthSourceStatus.Approved)
        {
            throw new InvalidOperationException("Script creation requires an active selected ContentIdea and an approved TruthSource.");
        }

        var latestTruthSourceVersion = await dbContext.TruthSourceVersions
            .Where(v => v.TruthSourceId == truthSource.Id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var truthSourceVersionId = latestTruthSourceVersion?.Id ?? Guid.NewGuid();

        // Check if an existing script already exists on this ContentItem
        var existingScript = await dbContext.Scripts
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId, cancellationToken);

        if (existingScript != null)
        {
            throw new InvalidOperationException("A script already exists for this ContentItem. Update or regenerate the existing script.");
        }

        var pacingWpm = request.PacingWpm is > 0 ? request.PacingWpm.Value : 140;
        var targetDuration = request.TargetDurationSeconds is > 0 ? request.TargetDurationSeconds.Value : 45;

        var script = new Script
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItemId,
            ChannelId = contentItem.ChannelId,
            ContentIdeaId = activeIdea.Id,
            ContentIdeaVersionId = activeIdea.Id, // Lineage to selected idea version
            TruthSourceId = truthSource.Id,
            TruthSourceVersionId = truthSourceVersionId,
            Title = request.Title.Trim(),
            TargetDurationSeconds = targetDuration,
            PacingWpm = pacingWpm,
            Language = string.IsNullOrWhiteSpace(request.Language) ? "es-ES" : request.Language.Trim(),
            Status = ScriptStatus.Draft,
            Version = 1,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = actorEmail,
            UpdatedAtUtc = DateTime.UtcNow,
            UpdatedByEmail = actorEmail
        };

        var orderIndex = 1;
        if (request.Scenes != null && request.Scenes.Count > 0)
        {
            foreach (var sc in request.Scenes)
            {
                var wordCount = CountWords(sc.NarrationText);
                var estDuration = CalculateDuration(wordCount, pacingWpm);

                var scene = new ScriptScene
                {
                    Id = sc.Id.HasValue && sc.Id.Value != Guid.Empty ? sc.Id.Value : Guid.NewGuid(),
                    ScriptId = script.Id,
                    OrderIndex = sc.OrderIndex > 0 ? sc.OrderIndex : orderIndex++,
                    SceneType = string.IsNullOrWhiteSpace(sc.SceneType) ? SceneType.Hook : sc.SceneType.Trim(),
                    NarrationText = sc.NarrationText?.Trim() ?? string.Empty,
                    VisualPrompt = sc.VisualPrompt?.Trim() ?? string.Empty,
                    EstimatedDurationSeconds = estDuration,
                    WordCount = wordCount
                };

                if (sc.EvidenceReferences != null)
                {
                    foreach (var er in sc.EvidenceReferences)
                    {
                        scene.EvidenceReferences.Add(new ScriptSceneEvidenceReference
                        {
                            Id = Guid.NewGuid(),
                            ScriptSceneId = scene.Id,
                            TruthSourceClaimId = er.TruthSourceClaimId,
                            ClaimStatement = er.ClaimStatement?.Trim() ?? string.Empty,
                            EditorialNote = er.EditorialNote?.Trim()
                        });
                    }
                }

                script.Scenes.Add(scene);
            }
        }
        else
        {
            // Default 5 scenes structure
            var defaultTypes = new[] { SceneType.Hook, SceneType.Problem, SceneType.Insight, SceneType.Climax, SceneType.CallToAction };
            foreach (var type in defaultTypes)
            {
                script.Scenes.Add(new ScriptScene
                {
                    Id = Guid.NewGuid(),
                    ScriptId = script.Id,
                    OrderIndex = orderIndex++,
                    SceneType = type,
                    NarrationText = string.Empty,
                    VisualPrompt = string.Empty,
                    EstimatedDurationSeconds = 0,
                    WordCount = 0
                });
            }
        }

        script.TotalWordCount = script.Scenes.Sum(s => s.WordCount);
        script.EstimatedDurationSeconds = CalculateDuration(script.TotalWordCount, pacingWpm);

        var versionSnapshot = CreateVersionSnapshot(script, "Creación manual inicial del guión.", actorEmail);

        contentItem.Stage = ContentItemStage.ScriptDrafted;
        contentItem.UpdatedAtUtc = DateTime.UtcNow;
        contentItem.UpdatedByEmail = actorEmail;

        dbContext.Scripts.Add(script);
        dbContext.ScriptVersions.Add(versionSnapshot);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "Script.Created",
            "Script",
            script.Id.ToString(),
            $"Created script '{script.Title}' with {script.Scenes.Count} scenes ({script.TotalWordCount} words, ~{script.EstimatedDurationSeconds:F1}s).",
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        return MapToDto(script, isStale: false, staleReason: null);
    }

    public async Task<ScriptDto> UpdateScriptAsync(
        Guid contentItemId,
        Guid scriptId,
        UpdateScriptRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.");

        var script = await dbContext.Scripts
            .Include(s => s.Scenes)
                .ThenInclude(sc => sc.EvidenceReferences)
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId && s.Id == scriptId, cancellationToken)
            ?? throw new ArgumentException("Script not found.");

        if (script.Version != request.ExpectedVersion)
        {
            throw new ConcurrencyConflictException(
                "The script was modified by another operator. Please reload latest changes.",
                script.Version);
        }

        // Archive previous state into ScriptVersion
        var versionSnapshot = CreateVersionSnapshot(
            script,
            string.IsNullOrWhiteSpace(request.ChangeSummary) ? "Edición manual por el operador." : request.ChangeSummary.Trim(),
            actorEmail);
        dbContext.ScriptVersions.Add(versionSnapshot);

        var pacingWpm = request.PacingWpm is > 0 ? request.PacingWpm.Value : script.PacingWpm;
        var targetDuration = request.TargetDurationSeconds is > 0 ? request.TargetDurationSeconds.Value : script.TargetDurationSeconds;

        script.Title = request.Title.Trim();
        script.TargetDurationSeconds = targetDuration;
        script.PacingWpm = pacingWpm;
        script.Language = string.IsNullOrWhiteSpace(request.Language) ? script.Language : request.Language.Trim();

        // Clear existing scenes and replace
        dbContext.ScriptScenes.RemoveRange(script.Scenes);
        script.Scenes.Clear();

        var orderIndex = 1;
        foreach (var sc in request.Scenes)
        {
            var wordCount = CountWords(sc.NarrationText);
            var estDuration = CalculateDuration(wordCount, pacingWpm);

            var scene = new ScriptScene
            {
                Id = sc.Id.HasValue && sc.Id.Value != Guid.Empty ? sc.Id.Value : Guid.NewGuid(),
                ScriptId = script.Id,
                OrderIndex = sc.OrderIndex > 0 ? sc.OrderIndex : orderIndex++,
                SceneType = string.IsNullOrWhiteSpace(sc.SceneType) ? SceneType.Hook : sc.SceneType.Trim(),
                NarrationText = sc.NarrationText?.Trim() ?? string.Empty,
                VisualPrompt = sc.VisualPrompt?.Trim() ?? string.Empty,
                EstimatedDurationSeconds = estDuration,
                WordCount = wordCount
            };

            if (sc.EvidenceReferences != null)
            {
                foreach (var er in sc.EvidenceReferences)
                {
                    scene.EvidenceReferences.Add(new ScriptSceneEvidenceReference
                    {
                        Id = Guid.NewGuid(),
                        ScriptSceneId = scene.Id,
                        TruthSourceClaimId = er.TruthSourceClaimId,
                        ClaimStatement = er.ClaimStatement?.Trim() ?? string.Empty,
                        EditorialNote = er.EditorialNote?.Trim()
                    });
                }
            }

            script.Scenes.Add(scene);
        }

        script.TotalWordCount = script.Scenes.Sum(s => s.WordCount);
        script.EstimatedDurationSeconds = CalculateDuration(script.TotalWordCount, pacingWpm);
        script.Version += 1;
        script.UpdatedAtUtc = DateTime.UtcNow;
        script.UpdatedByEmail = actorEmail;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "Script.Updated",
            "Script",
            script.Id.ToString(),
            $"Updated script '{script.Title}' to version {script.Version} ({script.TotalWordCount} words, ~{script.EstimatedDurationSeconds:F1}s).",
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        var (isStale, staleReason) = await EvaluateStaleLineageAsync(script, cancellationToken);
        return MapToDto(script, isStale, staleReason);
    }

    public async Task<ScriptDto> GenerateAiScriptAsync(
        Guid contentItemId,
        GenerateScriptOptions? options,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == contentItemId, cancellationToken)
            ?? throw new ArgumentException("ContentItem not found.");

        var activeIdea = await dbContext.ContentIdeas
            .FirstOrDefaultAsync(i => i.ContentItemId == contentItemId && i.Status == ContentIdeaStatus.Selected, cancellationToken)
            ?? throw new InvalidOperationException("Script generation requires an active selected ContentIdea and an approved TruthSource.");

        var truthSource = await dbContext.TruthSources
            .FirstOrDefaultAsync(t => t.ContentItemId == contentItemId, cancellationToken)
            ?? throw new InvalidOperationException("Script generation requires an active selected ContentIdea and an approved TruthSource.");

        if (truthSource.Status != TruthSourceStatus.Approved)
        {
            throw new InvalidOperationException("Script generation requires an active selected ContentIdea and an approved TruthSource.");
        }

        var latestTruthSourceVersion = await dbContext.TruthSourceVersions
            .Where(v => v.TruthSourceId == truthSource.Id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var truthSourceVersionId = latestTruthSourceVersion?.Id ?? Guid.NewGuid();

        var channel = await dbContext.Channels
            .AsNoTracking()
            .FirstOrDefaultAsync(ch => ch.Id == contentItem.ChannelId, cancellationToken);

        var keyIdeas = DeserializeJsonList(truthSource.KeyIdeasJson);
        var verifiableClaims = DeserializeClaimsList(truthSource.VerifiableClaimsJson);
        var doNotSay = DeserializeJsonList(truthSource.DoNotSayConstraintsJson);

        var pacingWpm = options?.PacingWpm is > 0 ? options.PacingWpm.Value : 140;
        var targetDuration = options?.TargetDurationSeconds is > 0 ? options.TargetDurationSeconds.Value : 45;

        var request = new GenerateScriptRequest(
            ChannelId: contentItem.ChannelId,
            ChannelName: channel?.Name ?? "IA Simple ES",
            ChannelLanguage: channel?.Language ?? "es-ES",
            ChannelNiche: channel?.Niche ?? "Technology",
            TruthSourceId: truthSource.Id,
            TruthSourceVersionId: truthSourceVersionId,
            ContentIdeaId: activeIdea.Id,
            ContentIdeaVersionId: activeIdea.Id,
            IdeaTitle: activeIdea.Title,
            IdeaAngle: activeIdea.Angle,
            IdeaHookStrategy: activeIdea.HookStrategy,
            IdeaAudienceValue: activeIdea.AudienceValue,
            IdeaFormat: activeIdea.Format,
            IdeaIntendedOutcome: activeIdea.IntendedOutcome,
            Summary: truthSource.Summary,
            KeyIdeas: keyIdeas,
            VerifiableClaims: verifiableClaims,
            DoNotSayConstraints: doNotSay,
            TargetDurationSeconds: targetDuration,
            PacingWpm: pacingWpm,
            CustomInstructions: options?.CustomInstructions,
            ToneStyle: options?.ToneStyle
        );

        var context = new AiRoutingContext(contentItem.ChannelId, contentItemId);
        var aiResult = await aiProviderRouter.GenerateScriptAsync(request, context, cancellationToken);

        if (!aiResult.Success || aiResult.Data == null || aiResult.Data.Script == null)
        {
            throw new InvalidOperationException($"AI script generation failed: {aiResult.ErrorMessage ?? "No script generated."}");
        }

        var generated = aiResult.Data.Script;

        var existingScript = await dbContext.Scripts
            .Include(s => s.Scenes)
                .ThenInclude(sc => sc.EvidenceReferences)
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId, cancellationToken);

        Script script;
        if (existingScript != null)
        {
            // Snapshot previous state before replacing with AI generation
            var versionSnapshot = CreateVersionSnapshot(
                existingScript,
                "Regeneración automática por IA a partir de TruthSource e Idea seleccionada.",
                actorEmail);
            dbContext.ScriptVersions.Add(versionSnapshot);

            existingScript.Title = generated.Title.Trim();
            existingScript.ContentIdeaId = activeIdea.Id;
            existingScript.ContentIdeaVersionId = activeIdea.Id;
            existingScript.TruthSourceId = truthSource.Id;
            existingScript.TruthSourceVersionId = truthSourceVersionId;
            existingScript.TargetDurationSeconds = generated.TargetDurationSeconds;
            existingScript.PacingWpm = pacingWpm;
            existingScript.Language = generated.Language;
            existingScript.Status = ScriptStatus.Draft;
            existingScript.RejectionReason = null;
            existingScript.RejectedAtUtc = null;
            existingScript.RejectedByEmail = null;

            dbContext.ScriptScenes.RemoveRange(existingScript.Scenes);
            existingScript.Scenes.Clear();

            foreach (var sc in generated.Scenes)
            {
                var wordCount = CountWords(sc.NarrationText);
                var estDuration = CalculateDuration(wordCount, pacingWpm);

                var scene = new ScriptScene
                {
                    Id = Guid.NewGuid(),
                    ScriptId = existingScript.Id,
                    OrderIndex = sc.OrderIndex,
                    SceneType = sc.SceneType,
                    NarrationText = sc.NarrationText.Trim(),
                    VisualPrompt = sc.VisualPrompt.Trim(),
                    EstimatedDurationSeconds = estDuration,
                    WordCount = wordCount
                };

                if (sc.EvidenceReferences != null)
                {
                    foreach (var er in sc.EvidenceReferences)
                    {
                        scene.EvidenceReferences.Add(new ScriptSceneEvidenceReference
                        {
                            Id = Guid.NewGuid(),
                            ScriptSceneId = scene.Id,
                            TruthSourceClaimId = er.TruthSourceClaimId,
                            ClaimStatement = er.ClaimStatement?.Trim() ?? string.Empty,
                            EditorialNote = er.EditorialNote?.Trim()
                        });
                    }
                }

                existingScript.Scenes.Add(scene);
            }

            existingScript.TotalWordCount = existingScript.Scenes.Sum(s => s.WordCount);
            existingScript.EstimatedDurationSeconds = CalculateDuration(existingScript.TotalWordCount, pacingWpm);
            existingScript.Version += 1;
            existingScript.UpdatedAtUtc = DateTime.UtcNow;
            existingScript.UpdatedByEmail = actorEmail;

            script = existingScript;
        }
        else
        {
            script = new Script
            {
                Id = Guid.NewGuid(),
                ContentItemId = contentItemId,
                ChannelId = contentItem.ChannelId,
                ContentIdeaId = activeIdea.Id,
                ContentIdeaVersionId = activeIdea.Id,
                TruthSourceId = truthSource.Id,
                TruthSourceVersionId = truthSourceVersionId,
                Title = generated.Title.Trim(),
                TargetDurationSeconds = generated.TargetDurationSeconds,
                PacingWpm = pacingWpm,
                Language = generated.Language,
                Status = ScriptStatus.Draft,
                Version = 1,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByEmail = actorEmail,
                UpdatedAtUtc = DateTime.UtcNow,
                UpdatedByEmail = actorEmail
            };

            foreach (var sc in generated.Scenes)
            {
                var wordCount = CountWords(sc.NarrationText);
                var estDuration = CalculateDuration(wordCount, pacingWpm);

                var scene = new ScriptScene
                {
                    Id = Guid.NewGuid(),
                    ScriptId = script.Id,
                    OrderIndex = sc.OrderIndex,
                    SceneType = sc.SceneType,
                    NarrationText = sc.NarrationText.Trim(),
                    VisualPrompt = sc.VisualPrompt.Trim(),
                    EstimatedDurationSeconds = estDuration,
                    WordCount = wordCount
                };

                if (sc.EvidenceReferences != null)
                {
                    foreach (var er in sc.EvidenceReferences)
                    {
                        scene.EvidenceReferences.Add(new ScriptSceneEvidenceReference
                        {
                            Id = Guid.NewGuid(),
                            ScriptSceneId = scene.Id,
                            TruthSourceClaimId = er.TruthSourceClaimId,
                            ClaimStatement = er.ClaimStatement?.Trim() ?? string.Empty,
                            EditorialNote = er.EditorialNote?.Trim()
                        });
                    }
                }

                script.Scenes.Add(scene);
            }

            script.TotalWordCount = script.Scenes.Sum(s => s.WordCount);
            script.EstimatedDurationSeconds = CalculateDuration(script.TotalWordCount, pacingWpm);

            var v1 = CreateVersionSnapshot(script, "Generación inicial por IA.", actorEmail);
            dbContext.Scripts.Add(script);
            dbContext.ScriptVersions.Add(v1);
        }

        contentItem.Stage = ContentItemStage.ScriptDrafted;
        contentItem.UpdatedAtUtc = DateTime.UtcNow;
        contentItem.UpdatedByEmail = actorEmail;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "Script.AiGenerated",
            "Script",
            script.Id.ToString(),
            $"Generated AI script '{script.Title}' with {script.Scenes.Count} scenes ({script.TotalWordCount} words, ~{script.EstimatedDurationSeconds:F1}s).",
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        return MapToDto(script, isStale: false, staleReason: null);
    }

    public async Task<ScriptReviewResultDto> ReviewScriptAsync(
        Guid contentItemId,
        Guid scriptId,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == contentItemId, cancellationToken)
            ?? throw new ArgumentException("ContentItem not found.");

        var script = await dbContext.Scripts
            .AsNoTracking()
            .Include(s => s.Scenes.OrderBy(sc => sc.OrderIndex))
                .ThenInclude(sc => sc.EvidenceReferences)
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId && s.Id == scriptId, cancellationToken)
            ?? throw new ArgumentException("Script not found.");

        var truthSource = await dbContext.TruthSources
            .FirstOrDefaultAsync(t => t.ContentItemId == contentItemId, cancellationToken)
            ?? throw new InvalidOperationException("Script review requires a TruthSource.");

        var channel = await dbContext.Channels
            .AsNoTracking()
            .FirstOrDefaultAsync(ch => ch.Id == contentItem.ChannelId, cancellationToken);

        var keyIdeas = DeserializeJsonList(truthSource.KeyIdeasJson);
        var verifiableClaims = DeserializeClaimsList(truthSource.VerifiableClaimsJson);
        var doNotSay = DeserializeJsonList(truthSource.DoNotSayConstraintsJson);

        var sceneDtos = script.Scenes.Select(MapToSceneDto).ToList();

        var request = new ReviewScriptRequest(
            ChannelId: contentItem.ChannelId,
            ChannelName: channel?.Name ?? "IA Simple ES",
            ChannelLanguage: channel?.Language ?? "es-ES",
            TruthSourceId: truthSource.Id,
            TruthSourceVersionId: script.TruthSourceVersionId,
            TruthSourceSummary: truthSource.Summary,
            KeyIdeas: keyIdeas,
            VerifiableClaims: verifiableClaims,
            DoNotSayConstraints: doNotSay,
            ScriptTitle: script.Title,
            TargetDurationSeconds: script.TargetDurationSeconds,
            PacingWpm: script.PacingWpm,
            Scenes: sceneDtos
        );

        var context = new AiRoutingContext(contentItem.ChannelId, contentItemId);
        var aiResult = await aiProviderRouter.ReviewScriptAsync(request, context, cancellationToken);

        if (!aiResult.Success || aiResult.Data == null || aiResult.Data.ReviewResult == null)
        {
            throw new InvalidOperationException($"AI script review failed: {aiResult.ErrorMessage ?? "No review produced."}");
        }

        await auditService.RecordAsync(
            "Script.AiCritiqueRequested",
            "Script",
            script.Id.ToString(),
            $"Advisory AI critique performed for script '{script.Title}' (Overall: {aiResult.Data.ReviewResult.OverallStatus}).",
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        return aiResult.Data.ReviewResult;
    }

    public async Task<ScriptDto> SubmitForReviewAsync(
        Guid contentItemId,
        Guid scriptId,
        SubmitScriptForReviewRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == contentItemId, cancellationToken)
            ?? throw new ArgumentException("ContentItem not found.");

        var script = await dbContext.Scripts
            .Include(s => s.Scenes)
                .ThenInclude(sc => sc.EvidenceReferences)
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId && s.Id == scriptId, cancellationToken)
            ?? throw new ArgumentException("Script not found.");

        if (script.Version != request.ExpectedVersion)
        {
            throw new ConcurrencyConflictException(
                "The script was modified by another operator. Please reload latest changes.",
                script.Version);
        }

        var versionSnapshot = CreateVersionSnapshot(script, "Enviado a revisión editorial.", actorEmail);
        dbContext.ScriptVersions.Add(versionSnapshot);

        script.Status = ScriptStatus.UnderReview;
        script.SubmittedForReviewAtUtc = DateTime.UtcNow;
        script.SubmittedForReviewByEmail = actorEmail;
        script.Version += 1;
        script.UpdatedAtUtc = DateTime.UtcNow;
        script.UpdatedByEmail = actorEmail;

        contentItem.Stage = ContentItemStage.ScriptUnderReview;
        contentItem.UpdatedAtUtc = DateTime.UtcNow;
        contentItem.UpdatedByEmail = actorEmail;

        // Auto-create EditorialTask for Script Review
        var existingTask = await dbContext.EditorialTasks
            .FirstOrDefaultAsync(t => t.ContentItemId == contentItemId && t.TaskType == EditorialTaskType.ReviewScript && t.Status == EditorialTaskStatus.Pending, cancellationToken);

        if (existingTask == null)
        {
            dbContext.EditorialTasks.Add(new EditorialTask
            {
                Id = Guid.NewGuid(),
                ChannelId = contentItem.ChannelId,
                ContentItemId = contentItemId,
                TaskType = EditorialTaskType.ReviewScript,
                Priority = EditorialTaskPriority.Normal,
                Status = EditorialTaskStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                CreatedByEmail = actorEmail
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "Script.SubmittedForReview",
            "Script",
            script.Id.ToString(),
            $"Submitted script '{script.Title}' for editorial review.",
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        var (isStale, staleReason) = await EvaluateStaleLineageAsync(script, cancellationToken);
        return MapToDto(script, isStale, staleReason);
    }

    public async Task<ScriptDto> ApproveScriptAsync(
        Guid contentItemId,
        Guid scriptId,
        ApproveScriptRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == contentItemId, cancellationToken)
            ?? throw new ArgumentException("ContentItem not found.");

        var script = await dbContext.Scripts
            .Include(s => s.Scenes)
                .ThenInclude(sc => sc.EvidenceReferences)
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId && s.Id == scriptId, cancellationToken)
            ?? throw new ArgumentException("Script not found.");

        if (script.Version != request.ExpectedVersion)
        {
            throw new ConcurrencyConflictException(
                "The script was modified by another operator. Please reload latest changes.",
                script.Version);
        }

        var truthSource = await dbContext.TruthSources
            .FirstOrDefaultAsync(t => t.ContentItemId == contentItemId, cancellationToken)
            ?? throw new InvalidOperationException("Script approval requires an approved TruthSource.");

        if (truthSource.Status != TruthSourceStatus.Approved)
        {
            throw new InvalidOperationException("Script approval requires an approved TruthSource.");
        }

        var (isStale, staleReason) = await EvaluateStaleLineageAsync(script, cancellationToken);
        if (isStale)
        {
            throw new InvalidOperationException($"Cannot approve a stale script: {staleReason}. Please reconcile or regenerate the script.");
        }

        var versionSnapshot = CreateVersionSnapshot(script, "Guión aprobado por el operador editorial.", actorEmail);
        dbContext.ScriptVersions.Add(versionSnapshot);

        script.Status = ScriptStatus.Approved;
        script.ApprovedAtUtc = DateTime.UtcNow;
        script.ApprovedByEmail = actorEmail;
        script.RejectionReason = null;
        script.RejectedAtUtc = null;
        script.RejectedByEmail = null;
        script.Version += 1;
        script.UpdatedAtUtc = DateTime.UtcNow;
        script.UpdatedByEmail = actorEmail;

        contentItem.Stage = ContentItemStage.ScriptApproved;
        contentItem.UpdatedAtUtc = DateTime.UtcNow;
        contentItem.UpdatedByEmail = actorEmail;

        // Complete any pending ReviewScript tasks for this ContentItem
        var pendingTasks = await dbContext.EditorialTasks
            .Where(t => t.ContentItemId == contentItemId && t.TaskType == EditorialTaskType.ReviewScript && t.Status != EditorialTaskStatus.Completed)
            .ToListAsync(cancellationToken);

        foreach (var task in pendingTasks)
        {
            task.Status = EditorialTaskStatus.Completed;
            task.CompletedAtUtc = DateTime.UtcNow;
            task.CompletedByEmail = actorEmail;
            task.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "Script.Approved",
            "Script",
            script.Id.ToString(),
            $"Approved script '{script.Title}'. ContentItem lifecycle stage advanced to ScriptApproved.",
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        return MapToDto(script, isStale: false, staleReason: null);
    }

    public async Task<ScriptDto> RejectScriptAsync(
        Guid contentItemId,
        Guid scriptId,
        RejectScriptRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Rejection reason is required.");

        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == contentItemId, cancellationToken)
            ?? throw new ArgumentException("ContentItem not found.");

        var script = await dbContext.Scripts
            .Include(s => s.Scenes)
                .ThenInclude(sc => sc.EvidenceReferences)
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId && s.Id == scriptId, cancellationToken)
            ?? throw new ArgumentException("Script not found.");

        if (script.Version != request.ExpectedVersion)
        {
            throw new ConcurrencyConflictException(
                "The script was modified by another operator. Please reload latest changes.",
                script.Version);
        }

        var versionSnapshot = CreateVersionSnapshot(
            script,
            $"Rechazado por el operador: {request.Reason.Trim()}",
            actorEmail);
        dbContext.ScriptVersions.Add(versionSnapshot);

        script.Status = ScriptStatus.Rejected;
        script.RejectionReason = request.Reason.Trim();
        script.RejectedAtUtc = DateTime.UtcNow;
        script.RejectedByEmail = actorEmail;
        script.Version += 1;
        script.UpdatedAtUtc = DateTime.UtcNow;
        script.UpdatedByEmail = actorEmail;

        // Complete any pending ReviewScript tasks for this ContentItem
        var pendingTasks = await dbContext.EditorialTasks
            .Where(t => t.ContentItemId == contentItemId && t.TaskType == EditorialTaskType.ReviewScript && t.Status != EditorialTaskStatus.Completed)
            .ToListAsync(cancellationToken);

        foreach (var task in pendingTasks)
        {
            task.Status = EditorialTaskStatus.Completed;
            task.CompletedAtUtc = DateTime.UtcNow;
            task.CompletedByEmail = actorEmail;
            task.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "Script.Rejected",
            "Script",
            script.Id.ToString(),
            $"Rejected script '{script.Title}' with reason: {script.RejectionReason}",
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        var (isStale, staleReason) = await EvaluateStaleLineageAsync(script, cancellationToken);
        return MapToDto(script, isStale, staleReason);
    }

    public async Task<ScriptDto> ReopenScriptAsync(
        Guid contentItemId,
        Guid scriptId,
        ReopenScriptRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == contentItemId, cancellationToken)
            ?? throw new ArgumentException("ContentItem not found.");

        var script = await dbContext.Scripts
            .Include(s => s.Scenes)
                .ThenInclude(sc => sc.EvidenceReferences)
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId && s.Id == scriptId, cancellationToken)
            ?? throw new ArgumentException("Script not found.");

        if (script.Version != request.ExpectedVersion)
        {
            throw new ConcurrencyConflictException(
                "The script was modified by another operator. Please reload latest changes.",
                script.Version);
        }

        var versionSnapshot = CreateVersionSnapshot(script, "Reabierto para revisión editorial.", actorEmail);
        dbContext.ScriptVersions.Add(versionSnapshot);

        script.Status = ScriptStatus.Draft;
        script.RejectionReason = null;
        script.RejectedAtUtc = null;
        script.RejectedByEmail = null;
        script.Version += 1;
        script.UpdatedAtUtc = DateTime.UtcNow;
        script.UpdatedByEmail = actorEmail;

        contentItem.Stage = ContentItemStage.ScriptDrafted;
        contentItem.UpdatedAtUtc = DateTime.UtcNow;
        contentItem.UpdatedByEmail = actorEmail;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "Script.Reopened",
            "Script",
            script.Id.ToString(),
            $"Reopened script '{script.Title}' to Draft for revision.",
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        var (isStale, staleReason) = await EvaluateStaleLineageAsync(script, cancellationToken);
        return MapToDto(script, isStale, staleReason);
    }

    private async Task<(bool IsStale, string? StaleReason)> EvaluateStaleLineageAsync(
        Script script,
        CancellationToken cancellationToken)
    {
        var activeIdea = await dbContext.ContentIdeas
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.ContentItemId == script.ContentItemId && i.Status == ContentIdeaStatus.Selected, cancellationToken);

        if (activeIdea == null)
        {
            return (true, "No active Selected ContentIdea found for this piece.");
        }

        if (script.ContentIdeaId != activeIdea.Id || script.ContentIdeaVersionId != activeIdea.Id)
        {
            return (true, "The active Selected ContentIdea has changed. Reconciliation required.");
        }

        var approvedTruthSource = await dbContext.TruthSources
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ContentItemId == script.ContentItemId && t.Status == TruthSourceStatus.Approved, cancellationToken);

        if (approvedTruthSource == null)
        {
            return (true, "The TruthSource is no longer in Approved status.");
        }

        var latestTruthSourceVersion = await dbContext.TruthSourceVersions
            .AsNoTracking()
            .Where(v => v.TruthSourceId == approvedTruthSource.Id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestTruthSourceVersion != null && script.TruthSourceVersionId != latestTruthSourceVersion.Id)
        {
            return (true, "The TruthSource factual foundation has evolved to a newer version. Reconciliation required.");
        }

        return (false, null);
    }

    private static ScriptVersion CreateVersionSnapshot(Script script, string changeSummary, string actorEmail)
    {
        var snapshotDto = new
        {
            script.Id,
            script.ContentItemId,
            script.ChannelId,
            script.ContentIdeaId,
            script.ContentIdeaVersionId,
            script.TruthSourceId,
            script.TruthSourceVersionId,
            script.Title,
            script.TargetDurationSeconds,
            script.PacingWpm,
            script.EstimatedDurationSeconds,
            script.TotalWordCount,
            script.Language,
            script.Status,
            script.RejectionReason,
            Scenes = script.Scenes.OrderBy(sc => sc.OrderIndex).Select(sc => new
            {
                sc.Id,
                sc.OrderIndex,
                sc.SceneType,
                sc.NarrationText,
                sc.VisualPrompt,
                sc.EstimatedDurationSeconds,
                sc.WordCount,
                EvidenceReferences = sc.EvidenceReferences.Select(er => new
                {
                    er.Id,
                    er.TruthSourceClaimId,
                    er.ClaimStatement,
                    er.EditorialNote
                }).ToList()
            }).ToList()
        };

        return new ScriptVersion
        {
            Id = Guid.NewGuid(),
            ScriptId = script.Id,
            ContentItemId = script.ContentItemId,
            ContentIdeaId = script.ContentIdeaId,
            ContentIdeaVersionId = script.ContentIdeaVersionId,
            TruthSourceId = script.TruthSourceId,
            TruthSourceVersionId = script.TruthSourceVersionId,
            VersionNumber = script.Version,
            SnapshotJson = JsonSerializer.Serialize(snapshotDto, JsonOptions),
            ChangeSummary = changeSummary,
            Status = script.Status,
            RejectionReason = script.RejectionReason,
            PacingWpm = script.PacingWpm,
            EstimatedDurationSeconds = script.EstimatedDurationSeconds,
            TotalWordCount = script.TotalWordCount,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = actorEmail
        };
    }

    private static int CountWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        var words = WordCountRegex().Split(text.Trim());
        return words.Count(w => !string.IsNullOrWhiteSpace(w));
    }

    private static double CalculateDuration(int wordCount, int pacingWpm)
    {
        if (wordCount <= 0 || pacingWpm <= 0) return 0;
        var wordsPerSec = pacingWpm / 60.0;
        return Math.Round(wordCount / wordsPerSec, 1);
    }

    private static ScriptDto MapToDto(Script script, bool isStale, string? staleReason) =>
        new(
            script.Id,
            script.ContentItemId,
            script.ChannelId,
            script.ContentIdeaId,
            script.ContentIdeaVersionId,
            script.TruthSourceId,
            script.TruthSourceVersionId,
            script.Title,
            script.TargetDurationSeconds,
            script.PacingWpm,
            script.EstimatedDurationSeconds,
            script.TotalWordCount,
            script.Language,
            script.Status,
            script.RejectionReason,
            script.RejectedAtUtc,
            script.RejectedByEmail,
            script.ApprovedAtUtc,
            script.ApprovedByEmail,
            script.SubmittedForReviewAtUtc,
            script.SubmittedForReviewByEmail,
            isStale,
            staleReason,
            script.Version,
            script.CreatedAtUtc,
            script.CreatedByEmail,
            script.UpdatedAtUtc,
            script.UpdatedByEmail,
            script.Scenes.OrderBy(sc => sc.OrderIndex).Select(MapToSceneDto).ToList()
        );

    private static ScriptSceneDto MapToSceneDto(ScriptScene sc) =>
        new(
            sc.Id,
            sc.ScriptId,
            sc.OrderIndex,
            sc.SceneType,
            sc.NarrationText,
            sc.VisualPrompt,
            sc.EstimatedDurationSeconds,
            sc.WordCount,
            sc.EvidenceReferences.Select(er => new ScriptSceneEvidenceReferenceDto(
                er.Id,
                er.ScriptSceneId,
                er.TruthSourceClaimId,
                er.ClaimStatement,
                er.EditorialNote
            )).ToList()
        );

    private static ScriptVersionDto MapToVersionDto(ScriptVersion v) =>
        new(
            v.Id,
            v.ScriptId,
            v.ContentItemId,
            v.ContentIdeaId,
            v.ContentIdeaVersionId,
            v.TruthSourceId,
            v.TruthSourceVersionId,
            v.VersionNumber,
            v.SnapshotJson,
            v.ChangeSummary,
            v.Status,
            v.RejectionReason,
            v.PacingWpm,
            v.EstimatedDurationSeconds,
            v.TotalWordCount,
            v.CreatedAtUtc,
            v.CreatedByEmail
        );

    private static List<string> DeserializeJsonList(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static List<VerifiableClaimDto> DeserializeClaimsList(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<VerifiableClaimDto>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WordCountRegex();
}
