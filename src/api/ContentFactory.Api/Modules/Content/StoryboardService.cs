using System.Text.Json;
using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Modules.Ai;
using ContentFactory.Api.Modules.Audit;
using Microsoft.EntityFrameworkCore;

namespace ContentFactory.Api.Modules.Content;

public class StoryboardService(
    AppDbContext dbContext,
    IAiProviderRouter aiProviderRouter,
    IAuditService auditService,
    ILogger<StoryboardService> logger) : IStoryboardService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<StoryboardDto?> GetStoryboardByContentItemIdAsync(
        Guid contentItemId,
        CancellationToken cancellationToken = default)
    {
        var storyboard = await dbContext.Storyboards
            .AsNoTracking()
            .Include(s => s.Frames.OrderBy(f => f.OrderIndex))
            .Include(s => s.AssetPlan!)
                .ThenInclude(ap => ap.Requirements.OrderBy(r => r.FrameOrderIndex ?? 999))
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId && s.IsCurrent, cancellationToken);

        if (storyboard == null) return null;

        var (isStale, staleReason) = await EvaluateStaleLineageAsync(storyboard, cancellationToken);
        return MapToDto(storyboard, isStale, staleReason);
    }

    public async Task<StoryboardDto?> GetStoryboardByIdAsync(
        Guid contentItemId,
        Guid storyboardId,
        CancellationToken cancellationToken = default)
    {
        var storyboard = await dbContext.Storyboards
            .AsNoTracking()
            .Include(s => s.Frames.OrderBy(f => f.OrderIndex))
            .Include(s => s.AssetPlan!)
                .ThenInclude(ap => ap.Requirements.OrderBy(r => r.FrameOrderIndex ?? 999))
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId && s.Id == storyboardId, cancellationToken);

        if (storyboard == null) return null;

        var (isStale, staleReason) = await EvaluateStaleLineageAsync(storyboard, cancellationToken);
        return MapToDto(storyboard, isStale, staleReason);
    }

    public async Task<List<StoryboardVersionDto>> GetStoryboardVersionsAsync(
        Guid contentItemId,
        Guid storyboardId,
        CancellationToken cancellationToken = default)
    {
        var versions = await dbContext.StoryboardVersions
            .AsNoTracking()
            .Where(v => v.ContentItemId == contentItemId && v.StoryboardId == storyboardId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);

        return versions.Select(MapToVersionDto).ToList();
    }

    public async Task<StoryboardVersionDto?> GetStoryboardVersionAsync(
        Guid contentItemId,
        Guid storyboardId,
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        var version = await dbContext.StoryboardVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.ContentItemId == contentItemId && v.StoryboardId == storyboardId && v.Id == versionId, cancellationToken);

        return version != null ? MapToVersionDto(version) : null;
    }

    public async Task<StoryboardDto> CreateStoryboardAsync(
        Guid contentItemId,
        CreateStoryboardRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.");

        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == contentItemId, cancellationToken)
            ?? throw new ArgumentException("ContentItem not found.");

        var approvedScript = await dbContext.Scripts
            .Include(s => s.Scenes)
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId && s.Status == ScriptStatus.Approved, cancellationToken)
            ?? throw new InvalidOperationException("Storyboard creation requires an approved Script and an approved TruthSource.");

        var approvedTruthSource = await dbContext.TruthSources
            .FirstOrDefaultAsync(t => t.ContentItemId == contentItemId && t.Status == TruthSourceStatus.Approved, cancellationToken)
            ?? throw new InvalidOperationException("Storyboard creation requires an approved Script and an approved TruthSource.");

        var latestScriptVersion = await dbContext.ScriptVersions
            .Where(v => v.ScriptId == approvedScript.Id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var latestTruthSourceVersion = await dbContext.TruthSourceVersions
            .Where(v => v.TruthSourceId == approvedTruthSource.Id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var existingCurrentStoryboard = await dbContext.Storyboards
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId && s.IsCurrent, cancellationToken);

        if (existingCurrentStoryboard != null)
        {
            throw new InvalidOperationException("A current Storyboard already exists for this ContentItem. Update or reconcile the existing storyboard.");
        }

        var frames = new List<StoryboardFrame>();
        var frameIndex = 1;

        if (request.Frames != null && request.Frames.Count > 0)
        {
            foreach (var reqFrame in request.Frames)
            {
                frames.Add(new StoryboardFrame
                {
                    Id = reqFrame.Id ?? Guid.NewGuid(),
                    OrderIndex = reqFrame.OrderIndex > 0 ? reqFrame.OrderIndex : frameIndex++,
                    ScriptSceneId = reqFrame.ScriptSceneId,
                    ScriptSceneOrderIndex = reqFrame.ScriptSceneOrderIndex,
                    FramingIntent = reqFrame.FramingIntent ?? FramingIntent.MediumShot,
                    CompositionIntent = reqFrame.CompositionIntent ?? string.Empty,
                    CameraMotionIntent = reqFrame.CameraMotionIntent ?? CameraMotionIntent.Static,
                    Subject = reqFrame.Subject ?? string.Empty,
                    Environment = reqFrame.Environment ?? string.Empty,
                    StyleIntent = reqFrame.StyleIntent ?? string.Empty,
                    VisualPrompt = reqFrame.VisualPrompt ?? string.Empty,
                    NegativePrompt = reqFrame.NegativePrompt ?? string.Empty,
                    AudioCue = reqFrame.AudioCue ?? string.Empty,
                    EstimatedDurationSeconds = reqFrame.EstimatedDurationSeconds > 0 ? reqFrame.EstimatedDurationSeconds : 3.0,
                    OnScreenText = reqFrame.OnScreenText ?? string.Empty,
                    TransitionIntent = reqFrame.TransitionIntent ?? TransitionIntent.Cut,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }
        }
        else
        {
            // Seed from approved script scenes
            foreach (var scene in approvedScript.Scenes.OrderBy(s => s.OrderIndex))
            {
                frames.Add(new StoryboardFrame
                {
                    Id = Guid.NewGuid(),
                    OrderIndex = frameIndex++,
                    ScriptSceneId = scene.Id,
                    ScriptSceneOrderIndex = scene.OrderIndex,
                    FramingIntent = scene.SceneType == SceneType.Hook ? FramingIntent.CloseUp : FramingIntent.MediumShot,
                    CompositionIntent = "Vertical 9:16 framing, subject centered, safe zone for captions",
                    CameraMotionIntent = scene.SceneType == SceneType.Hook ? CameraMotionIntent.SlowZoomIn : CameraMotionIntent.Static,
                    Subject = "Scene visual concept",
                    Environment = "Clean modern tech studio",
                    StyleIntent = "Tech Minimalist 9:16",
                    VisualPrompt = scene.VisualPrompt,
                    NegativePrompt = "deformed face, bad anatomy, text overlay, blurry",
                    AudioCue = scene.NarrationText,
                    EstimatedDurationSeconds = scene.EstimatedDurationSeconds > 0 ? scene.EstimatedDurationSeconds : 5.0,
                    OnScreenText = string.Empty,
                    TransitionIntent = TransitionIntent.Cut,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }
        }

        var totalEstimatedDuration = Math.Round(frames.Sum(f => f.EstimatedDurationSeconds), 1);
        var targetDuration = request.TargetDurationSeconds ?? approvedScript.TargetDurationSeconds;

        var storyboard = new Storyboard
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItemId,
            ChannelId = approvedScript.ChannelId,
            ScriptId = approvedScript.Id,
            ScriptVersionId = latestScriptVersion?.Id ?? Guid.NewGuid(),
            TruthSourceId = approvedTruthSource.Id,
            TruthSourceVersionId = latestTruthSourceVersion?.Id ?? Guid.NewGuid(),
            IsCurrent = true,
            Title = request.Title,
            TargetDurationSeconds = targetDuration,
            TotalEstimatedDurationSeconds = totalEstimatedDuration,
            Status = StoryboardStatus.Draft,
            Version = 1,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = actorEmail,
            UpdatedAtUtc = DateTime.UtcNow,
            UpdatedByEmail = actorEmail,
            Frames = frames
        };

        foreach (var frame in storyboard.Frames)
        {
            frame.StoryboardId = storyboard.Id;
        }

        // Build AssetPlan
        var assetPlan = new AssetPlan
        {
            Id = Guid.NewGuid(),
            StoryboardId = storyboard.Id,
            ContentItemId = contentItemId,
            Status = AssetPlanStatus.Planned,
            Version = 1,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        if (request.AssetRequirements != null && request.AssetRequirements.Count > 0)
        {
            foreach (var req in request.AssetRequirements)
            {
                assetPlan.Requirements.Add(new AssetRequirement
                {
                    Id = req.Id ?? Guid.NewGuid(),
                    AssetPlanId = assetPlan.Id,
                    FrameId = req.FrameId,
                    FrameOrderIndex = req.FrameOrderIndex,
                    AssetType = req.AssetType ?? AssetType.AiImage,
                    AspectRatio = req.AspectRatio ?? "9:16",
                    VisualPrompt = req.VisualPrompt ?? string.Empty,
                    NegativePrompt = req.NegativePrompt ?? string.Empty,
                    StyleIntent = req.StyleIntent ?? string.Empty,
                    MotionIntent = req.MotionIntent ?? string.Empty,
                    TargetDurationSeconds = req.TargetDurationSeconds,
                    VoiceIntent = req.VoiceIntent ?? string.Empty,
                    MusicMood = req.MusicMood ?? string.Empty,
                    SoundEffectIntent = req.SoundEffectIntent ?? string.Empty,
                    SubtitleProfile = req.SubtitleProfile ?? string.Empty,
                    OverlaySpecification = req.OverlaySpecification ?? string.Empty,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }
        }
        else
        {
            // Seed provider-agnostic asset requirements from frames
            foreach (var frame in storyboard.Frames)
            {
                assetPlan.Requirements.Add(new AssetRequirement
                {
                    Id = Guid.NewGuid(),
                    AssetPlanId = assetPlan.Id,
                    FrameId = frame.Id,
                    FrameOrderIndex = frame.OrderIndex,
                    AssetType = AssetType.AiImage,
                    AspectRatio = "9:16",
                    VisualPrompt = frame.VisualPrompt,
                    NegativePrompt = frame.NegativePrompt,
                    StyleIntent = frame.StyleIntent,
                    MotionIntent = frame.CameraMotionIntent,
                    TargetDurationSeconds = frame.EstimatedDurationSeconds,
                    VoiceIntent = "Sober Spanish narrator",
                    MusicMood = "Tech Ambient",
                    SubtitleProfile = "Center-bottom kinetic captions",
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }

            // Audio track & subtitle track
            assetPlan.Requirements.Add(new AssetRequirement
            {
                Id = Guid.NewGuid(),
                AssetPlanId = assetPlan.Id,
                AssetType = AssetType.TtsVoiceover,
                AspectRatio = "N/A",
                VisualPrompt = "Full narration track",
                VoiceIntent = "Spanish neutral professional voiceover",
                TargetDurationSeconds = totalEstimatedDuration,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });

            assetPlan.Requirements.Add(new AssetRequirement
            {
                Id = Guid.NewGuid(),
                AssetPlanId = assetPlan.Id,
                AssetType = AssetType.SubtitleTrack,
                AspectRatio = "9:16",
                VisualPrompt = "Synchronized kinetic subtitles",
                SubtitleProfile = "Spanish formatted kinetic captions",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        storyboard.AssetPlan = assetPlan;

        var versionSnapshot = CreateVersionSnapshot(storyboard, "Initial storyboard draft", actorEmail);

        dbContext.Storyboards.Add(storyboard);
        dbContext.StoryboardVersions.Add(versionSnapshot);

        contentItem.Stage = ContentItemStage.StoryboardDrafted;
        contentItem.UpdatedAtUtc = DateTime.UtcNow;
        contentItem.UpdatedByEmail = actorEmail;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "Storyboard.Created",
            "Storyboard",
            storyboard.Id.ToString(),
            $"Created Storyboard '{storyboard.Title}' in Draft with {storyboard.Frames.Count} frames.",
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        return MapToDto(storyboard, isStale: false, staleReason: null);
    }

    public async Task<StoryboardDto> UpdateStoryboardAsync(
        Guid contentItemId,
        Guid storyboardId,
        UpdateStoryboardRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var storyboard = await dbContext.Storyboards
            .Include(s => s.Frames)
            .Include(s => s.AssetPlan!)
                .ThenInclude(ap => ap.Requirements)
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId && s.Id == storyboardId, cancellationToken)
            ?? throw new ArgumentException("Storyboard not found.");

        if (request.ExpectedVersion != storyboard.Version)
        {
            throw new ConcurrencyConflictException(
                $"Optimistic concurrency conflict on Storyboard '{storyboard.Id}'. Expected version {request.ExpectedVersion}, but current version is {storyboard.Version}.",
                storyboard.Version);
        }

        if (storyboard.Status != StoryboardStatus.Draft && storyboard.Status != StoryboardStatus.Rejected)
        {
            throw new InvalidOperationException($"Storyboard in status '{storyboard.Status}' cannot be edited. Reopen it first.");
        }

        storyboard.Title = request.Title;
        if (request.TargetDurationSeconds.HasValue && request.TargetDurationSeconds.Value > 0)
        {
            storyboard.TargetDurationSeconds = request.TargetDurationSeconds.Value;
        }

        dbContext.StoryboardFrames.RemoveRange(storyboard.Frames);
        storyboard.Frames.Clear();

        var frameIndex = 1;
        foreach (var reqFrame in request.Frames)
        {
            var newFrame = new StoryboardFrame
            {
                Id = reqFrame.Id.HasValue && reqFrame.Id.Value != Guid.Empty ? reqFrame.Id.Value : Guid.NewGuid(),
                StoryboardId = storyboard.Id,
                OrderIndex = reqFrame.OrderIndex > 0 ? reqFrame.OrderIndex : frameIndex++,
                ScriptSceneId = reqFrame.ScriptSceneId,
                ScriptSceneOrderIndex = reqFrame.ScriptSceneOrderIndex,
                FramingIntent = reqFrame.FramingIntent ?? FramingIntent.MediumShot,
                CompositionIntent = reqFrame.CompositionIntent ?? string.Empty,
                CameraMotionIntent = reqFrame.CameraMotionIntent ?? CameraMotionIntent.Static,
                Subject = reqFrame.Subject ?? string.Empty,
                Environment = reqFrame.Environment ?? string.Empty,
                StyleIntent = reqFrame.StyleIntent ?? string.Empty,
                VisualPrompt = reqFrame.VisualPrompt ?? string.Empty,
                NegativePrompt = reqFrame.NegativePrompt ?? string.Empty,
                AudioCue = reqFrame.AudioCue ?? string.Empty,
                EstimatedDurationSeconds = reqFrame.EstimatedDurationSeconds > 0 ? reqFrame.EstimatedDurationSeconds : 3.0,
                OnScreenText = reqFrame.OnScreenText ?? string.Empty,
                TransitionIntent = reqFrame.TransitionIntent ?? TransitionIntent.Cut,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            storyboard.Frames.Add(newFrame);
            dbContext.Entry(newFrame).State = EntityState.Added;
        }

        storyboard.TotalEstimatedDurationSeconds = Math.Round(storyboard.Frames.Sum(f => f.EstimatedDurationSeconds), 1);

        // Update AssetPlan
        if (storyboard.AssetPlan == null)
        {
            storyboard.AssetPlan = new AssetPlan
            {
                Id = Guid.NewGuid(),
                StoryboardId = storyboard.Id,
                ContentItemId = contentItemId,
                Status = AssetPlanStatus.Planned,
                Version = 1,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            dbContext.AssetPlans.Add(storyboard.AssetPlan);
        }

        dbContext.AssetRequirements.RemoveRange(storyboard.AssetPlan.Requirements);
        storyboard.AssetPlan.Requirements.Clear();

        if (request.AssetRequirements != null && request.AssetRequirements.Count > 0)
        {
            foreach (var req in request.AssetRequirements)
            {
                var newReq = new AssetRequirement
                {
                    Id = req.Id.HasValue && req.Id.Value != Guid.Empty ? req.Id.Value : Guid.NewGuid(),
                    AssetPlanId = storyboard.AssetPlan.Id,
                    FrameId = req.FrameId,
                    FrameOrderIndex = req.FrameOrderIndex,
                    AssetType = req.AssetType ?? AssetType.AiImage,
                    AspectRatio = req.AspectRatio ?? "9:16",
                    VisualPrompt = req.VisualPrompt ?? string.Empty,
                    NegativePrompt = req.NegativePrompt ?? string.Empty,
                    StyleIntent = req.StyleIntent ?? string.Empty,
                    MotionIntent = req.MotionIntent ?? string.Empty,
                    TargetDurationSeconds = req.TargetDurationSeconds,
                    VoiceIntent = req.VoiceIntent ?? string.Empty,
                    MusicMood = req.MusicMood ?? string.Empty,
                    SoundEffectIntent = req.SoundEffectIntent ?? string.Empty,
                    SubtitleProfile = req.SubtitleProfile ?? string.Empty,
                    OverlaySpecification = req.OverlaySpecification ?? string.Empty,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                storyboard.AssetPlan.Requirements.Add(newReq);
                dbContext.Entry(newReq).State = EntityState.Added;
            }
        }
        else
        {
            foreach (var frame in storyboard.Frames)
            {
                var newReq = new AssetRequirement
                {
                    Id = Guid.NewGuid(),
                    AssetPlanId = storyboard.AssetPlan.Id,
                    FrameId = frame.Id,
                    FrameOrderIndex = frame.OrderIndex,
                    AssetType = AssetType.AiImage,
                    AspectRatio = "9:16",
                    VisualPrompt = frame.VisualPrompt,
                    NegativePrompt = frame.NegativePrompt,
                    StyleIntent = frame.StyleIntent,
                    MotionIntent = frame.CameraMotionIntent,
                    TargetDurationSeconds = frame.EstimatedDurationSeconds,
                    VoiceIntent = "Sober Spanish narrator",
                    MusicMood = "Tech Ambient",
                    SubtitleProfile = "Center-bottom kinetic captions",
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                storyboard.AssetPlan.Requirements.Add(newReq);
                dbContext.Entry(newReq).State = EntityState.Added;
            }

            var ttsReq = new AssetRequirement
            {
                Id = Guid.NewGuid(),
                AssetPlanId = storyboard.AssetPlan.Id,
                AssetType = AssetType.TtsVoiceover,
                AspectRatio = "N/A",
                VisualPrompt = "Full narration track",
                VoiceIntent = "Spanish neutral voiceover",
                TargetDurationSeconds = storyboard.TotalEstimatedDurationSeconds,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            storyboard.AssetPlan.Requirements.Add(ttsReq);
            dbContext.Entry(ttsReq).State = EntityState.Added;

            var subReq = new AssetRequirement
            {
                Id = Guid.NewGuid(),
                AssetPlanId = storyboard.AssetPlan.Id,
                AssetType = AssetType.SubtitleTrack,
                AspectRatio = "9:16",
                VisualPrompt = "Synchronized kinetic captions",
                SubtitleProfile = "Spanish formatted captions",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            storyboard.AssetPlan.Requirements.Add(subReq);
            dbContext.Entry(subReq).State = EntityState.Added;
        }

        storyboard.AssetPlan.UpdatedAtUtc = DateTime.UtcNow;
        storyboard.Version++;
        storyboard.UpdatedAtUtc = DateTime.UtcNow;
        storyboard.UpdatedByEmail = actorEmail;

        var versionSnapshot = CreateVersionSnapshot(
            storyboard,
            request.ChangeSummary ?? "Updated storyboard frames and production asset requirements",
            actorEmail);

        dbContext.StoryboardVersions.Add(versionSnapshot);

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "Storyboard.Updated",
            "Storyboard",
            storyboard.Id.ToString(),
            $"Updated Storyboard '{storyboard.Title}' to version {storyboard.Version}.",
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        var (isStale, staleReason) = await EvaluateStaleLineageAsync(storyboard, cancellationToken);
        return MapToDto(storyboard, isStale, staleReason);
    }

    public async Task<StoryboardDto> GenerateAiStoryboardAsync(
        Guid contentItemId,
        GenerateStoryboardOptions? options,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == contentItemId, cancellationToken)
            ?? throw new ArgumentException("ContentItem not found.");

        var approvedScript = await dbContext.Scripts
            .Include(s => s.Scenes)
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId && s.Status == ScriptStatus.Approved, cancellationToken)
            ?? throw new InvalidOperationException("AI Storyboard generation requires an approved Script.");

        var approvedTruthSource = await dbContext.TruthSources
            .FirstOrDefaultAsync(t => t.ContentItemId == contentItemId && t.Status == TruthSourceStatus.Approved, cancellationToken)
            ?? throw new InvalidOperationException("AI Storyboard generation requires an approved TruthSource.");

        var channel = await dbContext.Channels
            .FirstOrDefaultAsync(c => c.Id == approvedScript.ChannelId, cancellationToken);

        var channelName = channel?.Name ?? "IA Simple ES";
        var channelLanguage = channel?.Language ?? "es";
        var channelNiche = channel?.Niche ?? "AI and tech";

        var scenesDto = approvedScript.Scenes.OrderBy(s => s.OrderIndex).Select(s => new ScriptSceneDto(
            s.Id,
            s.ScriptId,
            s.OrderIndex,
            s.SceneType,
            s.NarrationText,
            s.VisualPrompt,
            s.EstimatedDurationSeconds,
            s.WordCount,
            []
        )).ToList();

        var latestScriptVersion = await dbContext.ScriptVersions
            .Where(v => v.ScriptId == approvedScript.Id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
        var scriptVersionId = latestScriptVersion?.Id ?? Guid.NewGuid();

        var latestTruthSourceVersion = await dbContext.TruthSourceVersions
            .Where(v => v.TruthSourceId == approvedTruthSource.Id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
        var truthSourceVersionId = latestTruthSourceVersion?.Id ?? Guid.NewGuid();

        var aiRequest = new PlanStoryboardRequest(
            ChannelId: approvedScript.ChannelId,
            ChannelName: channelName,
            ChannelLanguage: channelLanguage,
            ChannelNiche: channelNiche,
            ScriptId: approvedScript.Id,
            ScriptVersionId: scriptVersionId,
            TruthSourceId: approvedTruthSource.Id,
            TruthSourceVersionId: truthSourceVersionId,
            ScriptTitle: approvedScript.Title,
            TargetDurationSeconds: options?.TargetDurationSeconds ?? approvedScript.TargetDurationSeconds,
            PacingWpm: approvedScript.PacingWpm,
            Scenes: scenesDto,
            VisualStylePreset: options?.VisualStylePreset,
            CameraMotionIntensity: options?.CameraMotionIntensity,
            FrameDensityMultiplier: options?.FrameDensityMultiplier
        );

        var routingContext = new AiRoutingContext(approvedScript.ChannelId, contentItemId);
        var aiResult = await aiProviderRouter.PlanStoryboardAsync(aiRequest, routingContext, cancellationToken);

        if (!aiResult.Success || aiResult.Data == null)
        {
            throw new InvalidOperationException($"AI storyboard planning failed: {aiResult.ErrorMessage ?? "Unknown provider error"}");
        }

        var genResult = aiResult.Data.Storyboard;

        var existingCurrent = await dbContext.Storyboards
            .Include(s => s.Frames)
            .Include(s => s.AssetPlan!)
                .ThenInclude(ap => ap.Requirements)
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId && s.IsCurrent, cancellationToken);

        Storyboard storyboard;
        var sceneMap = approvedScript.Scenes.ToDictionary(s => s.OrderIndex, s => s.Id);

        if (existingCurrent != null)
        {
            if (existingCurrent.Status != StoryboardStatus.Draft && existingCurrent.Status != StoryboardStatus.Rejected)
            {
                throw new InvalidOperationException($"Cannot overwrite Storyboard in '{existingCurrent.Status}' status. Reopen it first.");
            }

            storyboard = existingCurrent;
            storyboard.Title = genResult.Title;
            storyboard.TargetDurationSeconds = genResult.TargetDurationSeconds;

            dbContext.StoryboardFrames.RemoveRange(storyboard.Frames);
            storyboard.Frames.Clear();

            foreach (var frameItem in genResult.Frames)
            {
                var sceneId = sceneMap.TryGetValue(frameItem.ScriptSceneOrderIndex, out var id) ? id : Guid.Empty;
                var newFrame = new StoryboardFrame
                {
                    Id = Guid.NewGuid(),
                    StoryboardId = storyboard.Id,
                    OrderIndex = frameItem.OrderIndex,
                    ScriptSceneId = sceneId,
                    ScriptSceneOrderIndex = frameItem.ScriptSceneOrderIndex,
                    FramingIntent = frameItem.FramingIntent,
                    CompositionIntent = frameItem.CompositionIntent,
                    CameraMotionIntent = frameItem.CameraMotionIntent,
                    Subject = frameItem.Subject,
                    Environment = frameItem.Environment,
                    StyleIntent = frameItem.StyleIntent,
                    VisualPrompt = frameItem.VisualPrompt,
                    NegativePrompt = frameItem.NegativePrompt,
                    AudioCue = frameItem.AudioCue,
                    EstimatedDurationSeconds = frameItem.EstimatedDurationSeconds,
                    OnScreenText = frameItem.OnScreenText,
                    TransitionIntent = frameItem.TransitionIntent,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                storyboard.Frames.Add(newFrame);
                dbContext.Entry(newFrame).State = EntityState.Added;
            }

            storyboard.TotalEstimatedDurationSeconds = Math.Round(storyboard.Frames.Sum(f => f.EstimatedDurationSeconds), 1);

            if (storyboard.AssetPlan == null)
            {
                storyboard.AssetPlan = new AssetPlan
                {
                    Id = Guid.NewGuid(),
                    StoryboardId = storyboard.Id,
                    ContentItemId = contentItemId,
                    Status = AssetPlanStatus.Planned,
                    Version = 1,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                dbContext.AssetPlans.Add(storyboard.AssetPlan);
            }

            dbContext.AssetRequirements.RemoveRange(storyboard.AssetPlan.Requirements);
            storyboard.AssetPlan.Requirements.Clear();

            // Rebuild provider-neutral requirements
            foreach (var frame in storyboard.Frames)
            {
                var newReq = new AssetRequirement
                {
                    Id = Guid.NewGuid(),
                    AssetPlanId = storyboard.AssetPlan.Id,
                    FrameId = frame.Id,
                    FrameOrderIndex = frame.OrderIndex,
                    AssetType = AssetType.AiImage,
                    AspectRatio = "9:16",
                    VisualPrompt = frame.VisualPrompt,
                    NegativePrompt = frame.NegativePrompt,
                    StyleIntent = frame.StyleIntent,
                    MotionIntent = frame.CameraMotionIntent,
                    TargetDurationSeconds = frame.EstimatedDurationSeconds,
                    VoiceIntent = "Sober Spanish narrator",
                    MusicMood = "Tech Ambient",
                    SubtitleProfile = "Center-bottom kinetic captions",
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                storyboard.AssetPlan.Requirements.Add(newReq);
                dbContext.Entry(newReq).State = EntityState.Added;
            }

            var ttsReq = new AssetRequirement
            {
                Id = Guid.NewGuid(),
                AssetPlanId = storyboard.AssetPlan.Id,
                AssetType = AssetType.TtsVoiceover,
                AspectRatio = "N/A",
                VisualPrompt = "Full narration track",
                VoiceIntent = "Spanish neutral voiceover",
                TargetDurationSeconds = storyboard.TotalEstimatedDurationSeconds,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            storyboard.AssetPlan.Requirements.Add(ttsReq);
            dbContext.Entry(ttsReq).State = EntityState.Added;

            var subReq = new AssetRequirement
            {
                Id = Guid.NewGuid(),
                AssetPlanId = storyboard.AssetPlan.Id,
                AssetType = AssetType.SubtitleTrack,
                AspectRatio = "9:16",
                VisualPrompt = "Synchronized kinetic captions",
                SubtitleProfile = "Spanish formatted captions",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            storyboard.AssetPlan.Requirements.Add(subReq);
            dbContext.Entry(subReq).State = EntityState.Added;

            storyboard.Version++;
            storyboard.UpdatedAtUtc = DateTime.UtcNow;
            storyboard.UpdatedByEmail = actorEmail;
        }
        else
        {
            storyboard = new Storyboard
            {
                Id = Guid.NewGuid(),
                ContentItemId = contentItemId,
                ChannelId = approvedScript.ChannelId,
                ScriptId = approvedScript.Id,
                ScriptVersionId = scriptVersionId,
                TruthSourceId = approvedTruthSource.Id,
                TruthSourceVersionId = truthSourceVersionId,
                IsCurrent = true,
                Title = genResult.Title,
                TargetDurationSeconds = genResult.TargetDurationSeconds,
                Status = StoryboardStatus.Draft,
                Version = 1,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByEmail = actorEmail,
                UpdatedAtUtc = DateTime.UtcNow,
                UpdatedByEmail = actorEmail
            };

            foreach (var frameItem in genResult.Frames)
            {
                var sceneId = sceneMap.TryGetValue(frameItem.ScriptSceneOrderIndex, out var id) ? id : Guid.Empty;
                storyboard.Frames.Add(new StoryboardFrame
                {
                    Id = Guid.NewGuid(),
                    StoryboardId = storyboard.Id,
                    OrderIndex = frameItem.OrderIndex,
                    ScriptSceneId = sceneId,
                    ScriptSceneOrderIndex = frameItem.ScriptSceneOrderIndex,
                    FramingIntent = frameItem.FramingIntent,
                    CompositionIntent = frameItem.CompositionIntent,
                    CameraMotionIntent = frameItem.CameraMotionIntent,
                    Subject = frameItem.Subject,
                    Environment = frameItem.Environment,
                    StyleIntent = frameItem.StyleIntent,
                    VisualPrompt = frameItem.VisualPrompt,
                    NegativePrompt = frameItem.NegativePrompt,
                    AudioCue = frameItem.AudioCue,
                    EstimatedDurationSeconds = frameItem.EstimatedDurationSeconds,
                    OnScreenText = frameItem.OnScreenText,
                    TransitionIntent = frameItem.TransitionIntent,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }

            storyboard.TotalEstimatedDurationSeconds = Math.Round(storyboard.Frames.Sum(f => f.EstimatedDurationSeconds), 1);

            var assetPlan = new AssetPlan
            {
                Id = Guid.NewGuid(),
                StoryboardId = storyboard.Id,
                ContentItemId = contentItemId,
                Status = AssetPlanStatus.Planned,
                Version = 1,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            foreach (var frame in storyboard.Frames)
            {
                assetPlan.Requirements.Add(new AssetRequirement
                {
                    Id = Guid.NewGuid(),
                    AssetPlanId = assetPlan.Id,
                    FrameId = frame.Id,
                    FrameOrderIndex = frame.OrderIndex,
                    AssetType = AssetType.AiImage,
                    AspectRatio = "9:16",
                    VisualPrompt = frame.VisualPrompt,
                    NegativePrompt = frame.NegativePrompt,
                    StyleIntent = frame.StyleIntent,
                    MotionIntent = frame.CameraMotionIntent,
                    TargetDurationSeconds = frame.EstimatedDurationSeconds,
                    VoiceIntent = "Sober Spanish narrator",
                    MusicMood = "Tech Ambient",
                    SubtitleProfile = "Center-bottom kinetic captions",
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }

            assetPlan.Requirements.Add(new AssetRequirement
            {
                Id = Guid.NewGuid(),
                AssetPlanId = assetPlan.Id,
                AssetType = AssetType.TtsVoiceover,
                AspectRatio = "N/A",
                VisualPrompt = "Full narration track",
                VoiceIntent = "Spanish neutral voiceover",
                TargetDurationSeconds = storyboard.TotalEstimatedDurationSeconds,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });

            assetPlan.Requirements.Add(new AssetRequirement
            {
                Id = Guid.NewGuid(),
                AssetPlanId = assetPlan.Id,
                AssetType = AssetType.SubtitleTrack,
                AspectRatio = "9:16",
                VisualPrompt = "Synchronized kinetic captions",
                SubtitleProfile = "Spanish formatted captions",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });

            storyboard.AssetPlan = assetPlan;
            dbContext.Storyboards.Add(storyboard);
        }

        var versionSnapshot = CreateVersionSnapshot(
            storyboard,
            $"AI generated visual storyboard with {storyboard.Frames.Count} frames",
            actorEmail);

        dbContext.StoryboardVersions.Add(versionSnapshot);

        contentItem.Stage = ContentItemStage.StoryboardDrafted;
        contentItem.UpdatedAtUtc = DateTime.UtcNow;
        contentItem.UpdatedByEmail = actorEmail;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "Storyboard.GeneratedWithAi",
            "Storyboard",
            storyboard.Id.ToString(),
            $"Generated Storyboard '{storyboard.Title}' via AI provider with {storyboard.Frames.Count} frames.",
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        return MapToDto(storyboard, isStale: false, staleReason: null);
    }

    public async Task<StoryboardReviewResultDto> ReviewStoryboardAsync(
        Guid contentItemId,
        Guid storyboardId,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var storyboard = await dbContext.Storyboards
            .AsNoTracking()
            .Include(s => s.Frames)
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId && s.Id == storyboardId, cancellationToken)
            ?? throw new ArgumentException("Storyboard not found.");

        var script = await dbContext.Scripts
            .AsNoTracking()
            .Include(s => s.Scenes)
            .FirstOrDefaultAsync(s => s.Id == storyboard.ScriptId, cancellationToken)
            ?? throw new InvalidOperationException("Linked script not found.");

        var channel = await dbContext.Channels
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == storyboard.ChannelId, cancellationToken);

        var channelName = channel?.Name ?? "IA Simple ES";
        var channelLanguage = channel?.Language ?? "es";

        var scriptScenes = script.Scenes.OrderBy(s => s.OrderIndex).Select(s => new ScriptSceneDto(
            s.Id,
            s.ScriptId,
            s.OrderIndex,
            s.SceneType,
            s.NarrationText,
            s.VisualPrompt,
            s.EstimatedDurationSeconds,
            s.WordCount,
            []
        )).ToList();

        var framesDto = storyboard.Frames.OrderBy(f => f.OrderIndex).Select(MapToFrameDto).ToList();

        var request = new ReviewStoryboardRequest(
            ChannelId: storyboard.ChannelId,
            ChannelName: channelName,
            ChannelLanguage: channelLanguage,
            ScriptId: storyboard.ScriptId,
            ScriptVersionId: storyboard.ScriptVersionId,
            ScriptTitle: script.Title,
            ScriptTargetDurationSeconds: script.TargetDurationSeconds,
            ScriptScenes: scriptScenes,
            StoryboardTitle: storyboard.Title,
            TargetDurationSeconds: storyboard.TargetDurationSeconds,
            Frames: framesDto
        );

        var routingContext = new AiRoutingContext(storyboard.ChannelId, contentItemId);
        var result = await aiProviderRouter.ReviewStoryboardAsync(request, routingContext, cancellationToken);

        if (!result.Success || result.Data == null)
        {
            throw new InvalidOperationException($"AI storyboard review failed: {result.ErrorMessage ?? "Unknown provider error"}");
        }

        await auditService.RecordAsync(
            "Storyboard.ReviewedWithAi",
            "Storyboard",
            storyboard.Id.ToString(),
            $"Executed advisory AI review on Storyboard '{storyboard.Title}'. Overall status: {result.Data.ReviewResult.OverallStatus}.",
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        return result.Data.ReviewResult;
    }

    public async Task<StoryboardDto> SubmitForReviewAsync(
        Guid contentItemId,
        Guid storyboardId,
        SubmitStoryboardForReviewRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var storyboard = await dbContext.Storyboards
            .Include(s => s.Frames)
            .Include(s => s.AssetPlan!)
                .ThenInclude(ap => ap.Requirements)
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId && s.Id == storyboardId, cancellationToken)
            ?? throw new ArgumentException("Storyboard not found.");

        if (request.ExpectedVersion != storyboard.Version)
        {
            throw new ConcurrencyConflictException(
                $"Optimistic concurrency conflict on Storyboard '{storyboard.Id}'. Expected version {request.ExpectedVersion}, but current version is {storyboard.Version}.",
                storyboard.Version);
        }

        if (storyboard.Status != StoryboardStatus.Draft && storyboard.Status != StoryboardStatus.Rejected)
        {
            throw new InvalidOperationException($"Storyboard in status '{storyboard.Status}' cannot be submitted for review. Must be in Draft or Rejected status.");
        }

        if (storyboard.Frames.Count == 0)
        {
            throw new InvalidOperationException("Cannot submit an empty Storyboard for review. At least one frame is required.");
        }

        if (storyboard.AssetPlan == null || storyboard.AssetPlan.Requirements.Count == 0)
        {
            throw new InvalidOperationException("Cannot submit Storyboard without a complete AssetPlan. Production requirements are required.");
        }

        var (isStale, staleReason) = await EvaluateStaleLineageAsync(storyboard, cancellationToken);
        if (isStale)
        {
            throw new InvalidOperationException($"Cannot submit stale Storyboard for review: {staleReason}. Storyboard reconciliation is required.");
        }

        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == contentItemId, cancellationToken)
            ?? throw new ArgumentException("ContentItem not found.");

        storyboard.Status = StoryboardStatus.UnderReview;
        storyboard.SubmittedForReviewAtUtc = DateTime.UtcNow;
        storyboard.SubmittedForReviewByEmail = actorEmail;
        storyboard.Version++;
        storyboard.UpdatedAtUtc = DateTime.UtcNow;
        storyboard.UpdatedByEmail = actorEmail;

        var versionSnapshot = CreateVersionSnapshot(
            storyboard,
            "Submitted storyboard and asset plan for editorial review",
            actorEmail);

        dbContext.StoryboardVersions.Add(versionSnapshot);

        contentItem.Stage = ContentItemStage.StoryboardUnderReview;
        contentItem.UpdatedAtUtc = DateTime.UtcNow;
        contentItem.UpdatedByEmail = actorEmail;

        var existingTask = await dbContext.EditorialTasks
            .FirstOrDefaultAsync(t => t.ContentItemId == contentItemId && t.TaskType == EditorialTaskType.ReviewStoryboard && t.Status == EditorialTaskStatus.Pending, cancellationToken);

        if (existingTask == null)
        {
            dbContext.EditorialTasks.Add(new EditorialTask
            {
                Id = Guid.NewGuid(),
                ChannelId = contentItem.ChannelId,
                ContentItemId = contentItemId,
                TaskType = EditorialTaskType.ReviewStoryboard,
                Priority = EditorialTaskPriority.Normal,
                Status = EditorialTaskStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                CreatedByEmail = actorEmail
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "Storyboard.SubmittedForReview",
            "Storyboard",
            storyboard.Id.ToString(),
            $"Submitted Storyboard '{storyboard.Title}' for editorial review.",
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        return MapToDto(storyboard, false, null);
    }

    public async Task<StoryboardDto> ApproveStoryboardAsync(
        Guid contentItemId,
        Guid storyboardId,
        ApproveStoryboardRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var storyboard = await dbContext.Storyboards
            .Include(s => s.Frames)
            .Include(s => s.AssetPlan!)
                .ThenInclude(ap => ap.Requirements)
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId && s.Id == storyboardId, cancellationToken)
            ?? throw new ArgumentException("Storyboard not found.");

        if (request.ExpectedVersion != storyboard.Version)
        {
            throw new ConcurrencyConflictException(
                $"Optimistic concurrency conflict on Storyboard '{storyboard.Id}'. Expected version {request.ExpectedVersion}, but current version is {storyboard.Version}.",
                storyboard.Version);
        }

        if (storyboard.Status != StoryboardStatus.UnderReview)
        {
            throw new InvalidOperationException($"Storyboard in status '{storyboard.Status}' cannot be approved. It must be UnderReview.");
        }

        var (isStale, staleReason) = await EvaluateStaleLineageAsync(storyboard, cancellationToken);
        if (isStale)
        {
            throw new InvalidOperationException($"Cannot approve stale storyboard: {staleReason}");
        }

        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == contentItemId, cancellationToken)
            ?? throw new ArgumentException("ContentItem not found.");

        storyboard.Status = StoryboardStatus.Approved;
        storyboard.ApprovedAtUtc = DateTime.UtcNow;
        storyboard.ApprovedByEmail = actorEmail;
        storyboard.RejectionReason = null;
        storyboard.RejectedAtUtc = null;
        storyboard.RejectedByEmail = null;

        // Single gate approval: approve the exact AssetPlan inside this Storyboard
        if (storyboard.AssetPlan != null)
        {
            storyboard.AssetPlan.Status = AssetPlanStatus.ReadyForGeneration;
            storyboard.AssetPlan.UpdatedAtUtc = DateTime.UtcNow;
        }

        storyboard.Version++;
        storyboard.UpdatedAtUtc = DateTime.UtcNow;
        storyboard.UpdatedByEmail = actorEmail;

        var versionSnapshot = CreateVersionSnapshot(
            storyboard,
            "Approved storyboard and asset plan production specification",
            actorEmail);

        dbContext.StoryboardVersions.Add(versionSnapshot);

        contentItem.Stage = ContentItemStage.StoryboardApproved;
        contentItem.UpdatedAtUtc = DateTime.UtcNow;
        contentItem.UpdatedByEmail = actorEmail;

        // Complete pending ReviewStoryboard task
        var pendingTask = await dbContext.EditorialTasks
            .FirstOrDefaultAsync(t => t.ContentItemId == contentItemId &&
                                     t.TaskType == EditorialTaskType.ReviewStoryboard &&
                                     t.Status == EditorialTaskStatus.Pending, cancellationToken);

        if (pendingTask != null)
        {
            pendingTask.Status = EditorialTaskStatus.Completed;
            pendingTask.CompletedAtUtc = DateTime.UtcNow;
            pendingTask.CompletedByEmail = actorEmail;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "Storyboard.Approved",
            "Storyboard",
            storyboard.Id.ToString(),
            $"Approved Storyboard '{storyboard.Title}'. AssetPlan is now ReadyForGeneration.",
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        return MapToDto(storyboard, isStale: false, staleReason: null);
    }

    public async Task<StoryboardDto> RejectStoryboardAsync(
        Guid contentItemId,
        Guid storyboardId,
        RejectStoryboardRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Rejection reason is required.");

        var storyboard = await dbContext.Storyboards
            .Include(s => s.Frames)
            .Include(s => s.AssetPlan!)
                .ThenInclude(ap => ap.Requirements)
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId && s.Id == storyboardId, cancellationToken)
            ?? throw new ArgumentException("Storyboard not found.");

        if (request.ExpectedVersion != storyboard.Version)
        {
            throw new ConcurrencyConflictException(
                $"Optimistic concurrency conflict on Storyboard '{storyboard.Id}'. Expected version {request.ExpectedVersion}, but current version is {storyboard.Version}.",
                storyboard.Version);
        }

        if (storyboard.Status != StoryboardStatus.UnderReview)
        {
            throw new InvalidOperationException($"Storyboard in status '{storyboard.Status}' cannot be rejected. It must be UnderReview.");
        }

        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == contentItemId, cancellationToken)
            ?? throw new ArgumentException("ContentItem not found.");

        storyboard.Status = StoryboardStatus.Rejected;
        storyboard.RejectionReason = request.Reason;
        storyboard.RejectedAtUtc = DateTime.UtcNow;
        storyboard.RejectedByEmail = actorEmail;
        storyboard.Version++;
        storyboard.UpdatedAtUtc = DateTime.UtcNow;
        storyboard.UpdatedByEmail = actorEmail;

        var versionSnapshot = CreateVersionSnapshot(
            storyboard,
            $"Rejected storyboard: {request.Reason}",
            actorEmail);

        dbContext.StoryboardVersions.Add(versionSnapshot);

        contentItem.Stage = ContentItemStage.StoryboardDrafted;
        contentItem.UpdatedAtUtc = DateTime.UtcNow;
        contentItem.UpdatedByEmail = actorEmail;

        // Close pending task as rejected
        var pendingTask = await dbContext.EditorialTasks
            .FirstOrDefaultAsync(t => t.ContentItemId == contentItemId &&
                                     t.TaskType == EditorialTaskType.ReviewStoryboard &&
                                     t.Status == EditorialTaskStatus.Pending, cancellationToken);

        if (pendingTask != null)
        {
            pendingTask.Status = EditorialTaskStatus.Completed;
            pendingTask.CompletedAtUtc = DateTime.UtcNow;
            pendingTask.CompletedByEmail = actorEmail;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "Storyboard.Rejected",
            "Storyboard",
            storyboard.Id.ToString(),
            $"Rejected Storyboard '{storyboard.Title}'. Reason: {request.Reason}",
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        var (isStale, staleReason) = await EvaluateStaleLineageAsync(storyboard, cancellationToken);
        return MapToDto(storyboard, isStale, staleReason);
    }

    public async Task<StoryboardDto> ReopenStoryboardAsync(
        Guid contentItemId,
        Guid storyboardId,
        ReopenStoryboardRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var storyboard = await dbContext.Storyboards
            .Include(s => s.Frames)
            .Include(s => s.AssetPlan!)
                .ThenInclude(ap => ap.Requirements)
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId && s.Id == storyboardId, cancellationToken)
            ?? throw new ArgumentException("Storyboard not found.");

        if (request.ExpectedVersion != storyboard.Version)
        {
            throw new ConcurrencyConflictException(
                $"Optimistic concurrency conflict on Storyboard '{storyboard.Id}'. Expected version {request.ExpectedVersion}, but current version is {storyboard.Version}.",
                storyboard.Version);
        }

        if (storyboard.Status != StoryboardStatus.Rejected && storyboard.Status != StoryboardStatus.Approved)
        {
            throw new InvalidOperationException($"Only Rejected or Approved Storyboards can be reopened to Draft (current status: '{storyboard.Status}').");
        }

        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == contentItemId, cancellationToken)
            ?? throw new ArgumentException("ContentItem not found.");

        storyboard.Status = StoryboardStatus.Draft;
        if (storyboard.AssetPlan != null)
        {
            storyboard.AssetPlan.Status = AssetPlanStatus.Planned;
            storyboard.AssetPlan.UpdatedAtUtc = DateTime.UtcNow;
        }

        storyboard.Version++;
        storyboard.UpdatedAtUtc = DateTime.UtcNow;
        storyboard.UpdatedByEmail = actorEmail;

        var versionSnapshot = CreateVersionSnapshot(
            storyboard,
            "Reopened storyboard to Draft for revision",
            actorEmail);

        dbContext.StoryboardVersions.Add(versionSnapshot);

        contentItem.Stage = ContentItemStage.StoryboardDrafted;
        contentItem.UpdatedAtUtc = DateTime.UtcNow;
        contentItem.UpdatedByEmail = actorEmail;

        // Cancel any pending task
        var pendingTask = await dbContext.EditorialTasks
            .FirstOrDefaultAsync(t => t.ContentItemId == contentItemId &&
                                     t.TaskType == EditorialTaskType.ReviewStoryboard &&
                                     t.Status == EditorialTaskStatus.Pending, cancellationToken);

        if (pendingTask != null)
        {
            pendingTask.Status = EditorialTaskStatus.Completed;
            pendingTask.CompletedAtUtc = DateTime.UtcNow;
            pendingTask.CompletedByEmail = actorEmail;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "Storyboard.Reopened",
            "Storyboard",
            storyboard.Id.ToString(),
            $"Reopened Storyboard '{storyboard.Title}' to Draft.",
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        var (isStale, staleReason) = await EvaluateStaleLineageAsync(storyboard, cancellationToken);
        return MapToDto(storyboard, isStale, staleReason);
    }

    public async Task<StoryboardDto> ReconcileStoryboardAsync(
        Guid contentItemId,
        Guid storyboardId,
        ReconcileStoryboardRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var existingStoryboard = await dbContext.Storyboards
            .Include(s => s.Frames)
            .Include(s => s.AssetPlan!)
                .ThenInclude(ap => ap.Requirements)
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId && s.Id == storyboardId, cancellationToken)
            ?? throw new ArgumentException("Storyboard not found.");

        if (request.ExpectedVersion != existingStoryboard.Version)
        {
            throw new ConcurrencyConflictException(
                $"Optimistic concurrency conflict on Storyboard '{existingStoryboard.Id}'. Expected version {request.ExpectedVersion}, but current version is {existingStoryboard.Version}.",
                existingStoryboard.Version);
        }

        var approvedScript = await dbContext.Scripts
            .Include(s => s.Scenes)
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId && s.Status == ScriptStatus.Approved, cancellationToken)
            ?? throw new InvalidOperationException("Reconciliation requires an approved Script and an approved TruthSource.");

        var approvedTruthSource = await dbContext.TruthSources
            .FirstOrDefaultAsync(t => t.ContentItemId == contentItemId && t.Status == TruthSourceStatus.Approved, cancellationToken)
            ?? throw new InvalidOperationException("Reconciliation requires an approved Script and an approved TruthSource.");

        var latestScriptVersion = await dbContext.ScriptVersions
            .Where(v => v.ScriptId == approvedScript.Id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var latestTruthSourceVersion = await dbContext.TruthSourceVersions
            .Where(v => v.TruthSourceId == approvedTruthSource.Id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var contentItem = await dbContext.ContentItems
            .FirstOrDefaultAsync(c => c.Id == contentItemId, cancellationToken)
            ?? throw new ArgumentException("ContentItem not found.");

        // Mark existing predecessor storyboard as historical (IsCurrent = false)
        existingStoryboard.IsCurrent = false;
        existingStoryboard.SupersededAtUtc = DateTime.UtcNow;
        existingStoryboard.UpdatedAtUtc = DateTime.UtcNow;
        existingStoryboard.UpdatedByEmail = actorEmail;

        // Build successor storyboard
        var newFrames = new List<StoryboardFrame>();
        var frameIndex = 1;
        var sceneMap = approvedScript.Scenes.ToDictionary(s => s.OrderIndex, s => s.Id);

        // Reuse compatible editorial frames or re-seed from new approved script scenes
        if (existingStoryboard.Frames.Count > 0)
        {
            foreach (var oldFrame in existingStoryboard.Frames.OrderBy(f => f.OrderIndex))
            {
                var sceneId = sceneMap.TryGetValue(oldFrame.ScriptSceneOrderIndex, out var id)
                    ? id
                    : (approvedScript.Scenes.FirstOrDefault()?.Id ?? Guid.Empty);

                newFrames.Add(new StoryboardFrame
                {
                    Id = Guid.NewGuid(),
                    OrderIndex = frameIndex++,
                    ScriptSceneId = sceneId,
                    ScriptSceneOrderIndex = oldFrame.ScriptSceneOrderIndex,
                    FramingIntent = oldFrame.FramingIntent,
                    CompositionIntent = oldFrame.CompositionIntent,
                    CameraMotionIntent = oldFrame.CameraMotionIntent,
                    Subject = oldFrame.Subject,
                    Environment = oldFrame.Environment,
                    StyleIntent = oldFrame.StyleIntent,
                    VisualPrompt = oldFrame.VisualPrompt,
                    NegativePrompt = oldFrame.NegativePrompt,
                    AudioCue = oldFrame.AudioCue,
                    EstimatedDurationSeconds = oldFrame.EstimatedDurationSeconds,
                    OnScreenText = oldFrame.OnScreenText,
                    TransitionIntent = oldFrame.TransitionIntent,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }
        }
        else
        {
            foreach (var scene in approvedScript.Scenes.OrderBy(s => s.OrderIndex))
            {
                newFrames.Add(new StoryboardFrame
                {
                    Id = Guid.NewGuid(),
                    OrderIndex = frameIndex++,
                    ScriptSceneId = scene.Id,
                    ScriptSceneOrderIndex = scene.OrderIndex,
                    FramingIntent = FramingIntent.MediumShot,
                    CompositionIntent = "Vertical 9:16 framing",
                    CameraMotionIntent = CameraMotionIntent.Static,
                    Subject = "Scene visual concept",
                    Environment = "Clean modern tech studio",
                    StyleIntent = "Tech Minimalist 9:16",
                    VisualPrompt = scene.VisualPrompt,
                    NegativePrompt = "deformed, blurry, text artifacts",
                    AudioCue = scene.NarrationText,
                    EstimatedDurationSeconds = scene.EstimatedDurationSeconds > 0 ? scene.EstimatedDurationSeconds : 5.0,
                    OnScreenText = string.Empty,
                    TransitionIntent = TransitionIntent.Cut,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }
        }

        var totalEstimatedDuration = Math.Round(newFrames.Sum(f => f.EstimatedDurationSeconds), 1);

        var successorStoryboard = new Storyboard
        {
            Id = Guid.NewGuid(),
            ContentItemId = contentItemId,
            ChannelId = approvedScript.ChannelId,
            ScriptId = approvedScript.Id,
            ScriptVersionId = latestScriptVersion?.Id ?? Guid.NewGuid(),
            TruthSourceId = approvedTruthSource.Id,
            TruthSourceVersionId = latestTruthSourceVersion?.Id ?? Guid.NewGuid(),
            IsCurrent = true,
            ReconciledFromStoryboardId = existingStoryboard.Id,
            Title = $"{existingStoryboard.Title} (Reconciliado)",
            TargetDurationSeconds = approvedScript.TargetDurationSeconds,
            TotalEstimatedDurationSeconds = totalEstimatedDuration,
            Status = StoryboardStatus.Draft,
            Version = 1,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = actorEmail,
            UpdatedAtUtc = DateTime.UtcNow,
            UpdatedByEmail = actorEmail,
            Frames = newFrames
        };

        foreach (var f in successorStoryboard.Frames)
        {
            f.StoryboardId = successorStoryboard.Id;
        }

        var successorAssetPlan = new AssetPlan
        {
            Id = Guid.NewGuid(),
            StoryboardId = successorStoryboard.Id,
            ContentItemId = contentItemId,
            Status = AssetPlanStatus.Planned,
            Version = 1,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        foreach (var frame in successorStoryboard.Frames)
        {
            successorAssetPlan.Requirements.Add(new AssetRequirement
            {
                Id = Guid.NewGuid(),
                AssetPlanId = successorAssetPlan.Id,
                FrameId = frame.Id,
                FrameOrderIndex = frame.OrderIndex,
                AssetType = AssetType.AiImage,
                AspectRatio = "9:16",
                VisualPrompt = frame.VisualPrompt,
                NegativePrompt = frame.NegativePrompt,
                StyleIntent = frame.StyleIntent,
                MotionIntent = frame.CameraMotionIntent,
                TargetDurationSeconds = frame.EstimatedDurationSeconds,
                VoiceIntent = "Sober Spanish narrator",
                MusicMood = "Tech Ambient",
                SubtitleProfile = "Center-bottom kinetic captions",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        successorAssetPlan.Requirements.Add(new AssetRequirement
        {
            Id = Guid.NewGuid(),
            AssetPlanId = successorAssetPlan.Id,
            AssetType = AssetType.TtsVoiceover,
            AspectRatio = "N/A",
            VisualPrompt = "Full narration track",
            VoiceIntent = "Spanish neutral voiceover",
            TargetDurationSeconds = totalEstimatedDuration,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        successorAssetPlan.Requirements.Add(new AssetRequirement
        {
            Id = Guid.NewGuid(),
            AssetPlanId = successorAssetPlan.Id,
            AssetType = AssetType.SubtitleTrack,
            AspectRatio = "9:16",
            VisualPrompt = "Synchronized kinetic captions",
            SubtitleProfile = "Spanish formatted captions",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        successorStoryboard.AssetPlan = successorAssetPlan;

        var versionSnapshot = CreateVersionSnapshot(
            successorStoryboard,
            $"Reconciled successor storyboard created from predecessor {existingStoryboard.Id}",
            actorEmail);

        dbContext.Storyboards.Add(successorStoryboard);
        dbContext.StoryboardVersions.Add(versionSnapshot);

        contentItem.Stage = ContentItemStage.StoryboardDrafted;
        contentItem.UpdatedAtUtc = DateTime.UtcNow;
        contentItem.UpdatedByEmail = actorEmail;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.RecordAsync(
            "Storyboard.Reconciled",
            "Storyboard",
            successorStoryboard.Id.ToString(),
            $"Reconciled predecessor Storyboard {existingStoryboard.Id} into successor {successorStoryboard.Id}.",
            actorUserId: null,
            actorEmail: actorEmail,
            correlationId: null,
            cancellationToken: cancellationToken);

        return MapToDto(successorStoryboard, isStale: false, staleReason: null);
    }

    public async Task<ProductionEligibilityDto> CheckProductionEligibilityAsync(
        Guid contentItemId,
        CancellationToken cancellationToken = default)
    {
        var storyboard = await dbContext.Storyboards
            .AsNoTracking()
            .Include(s => s.Frames)
            .Include(s => s.AssetPlan!)
                .ThenInclude(ap => ap.Requirements)
            .FirstOrDefaultAsync(s => s.ContentItemId == contentItemId && s.IsCurrent, cancellationToken);

        if (storyboard == null)
        {
            return new ProductionEligibilityDto(
                ContentItemId: contentItemId,
                IsEligible: false,
                CurrentStoryboardExists: false,
                IsApproved: false,
                IsNotStale: false,
                IsAssetPlanComplete: false,
                IsUpstreamLineageCurrent: false,
                BlockerReason: "No current Storyboard exists for this ContentItem.",
                BlockerReasons: ["No current Storyboard exists for this ContentItem."],
                StoryboardId: null,
                StoryboardVersion: null,
                VisualRequirementCount: 0,
                AudioRequirementCount: 0,
                SubtitleRequirementCount: 0,
                StatusSummary: "NoStoryboard"
            );
        }

        var blockers = new List<string>();
        var isApproved = storyboard.Status == StoryboardStatus.Approved;
        if (!isApproved)
        {
            blockers.Add($"Current Storyboard is in status '{storyboard.Status}'. Storyboard must be Approved by an editorial operator.");
        }

        var (isStale, staleReason) = await EvaluateStaleLineageAsync(storyboard, cancellationToken);
        if (isStale)
        {
            blockers.Add($"Current Storyboard is stale: {staleReason}. Storyboard reconciliation required.");
        }

        var isAssetPlanComplete = storyboard.AssetPlan != null && storyboard.AssetPlan.Status == AssetPlanStatus.ReadyForGeneration;
        if (!isAssetPlanComplete)
        {
            blockers.Add("Approved Storyboard does not have an approved AssetPlan in ReadyForGeneration status.");
        }

        var visualCount = storyboard.AssetPlan?.Requirements.Count(r => r.AssetType == AssetType.AiImage || r.AssetType == AssetType.AiVideo || r.AssetType == AssetType.BRoll || r.AssetType == AssetType.GraphicOverlay) ?? 0;
        var audioCount = storyboard.AssetPlan?.Requirements.Count(r => r.AssetType == AssetType.TtsVoiceover || r.AssetType == AssetType.BackgroundMusic || r.AssetType == AssetType.SoundEffect) ?? 0;
        var subtitleCount = storyboard.AssetPlan?.Requirements.Count(r => r.AssetType == AssetType.SubtitleTrack) ?? 0;

        if (visualCount == 0)
        {
            blockers.Add("Production AssetPlan requires at least one visual asset specification (AiImage, AiVideo, BRoll, or GraphicOverlay).");
        }

        if (audioCount == 0)
        {
            blockers.Add("Production AssetPlan requires at least one audio asset specification (TtsVoiceover, BackgroundMusic, or SoundEffect).");
        }

        if (subtitleCount == 0)
        {
            blockers.Add("Production AssetPlan requires a subtitle asset specification (SubtitleTrack).");
        }

        var isEligible = isApproved && !isStale && isAssetPlanComplete && visualCount > 0 && audioCount > 0 && subtitleCount > 0;

        return new ProductionEligibilityDto(
            ContentItemId: contentItemId,
            IsEligible: isEligible,
            CurrentStoryboardExists: true,
            IsApproved: isApproved,
            IsNotStale: !isStale,
            IsAssetPlanComplete: isAssetPlanComplete,
            IsUpstreamLineageCurrent: !isStale,
            BlockerReason: blockers.Count > 0 ? string.Join("; ", blockers) : null,
            BlockerReasons: blockers,
            StoryboardId: storyboard.Id,
            StoryboardVersion: storyboard.Version,
            VisualRequirementCount: visualCount,
            AudioRequirementCount: audioCount,
            SubtitleRequirementCount: subtitleCount,
            StatusSummary: isEligible ? "Eligible" : "Blocked"
        );
    }

    private async Task<(bool IsStale, string? StaleReason)> EvaluateStaleLineageAsync(
        Storyboard storyboard,
        CancellationToken cancellationToken)
    {
        var approvedScript = await dbContext.Scripts
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ContentItemId == storyboard.ContentItemId && s.Status == ScriptStatus.Approved, cancellationToken);

        if (approvedScript == null)
        {
            return (true, "No approved Script found for this ContentItem.");
        }

        var latestScriptVersion = await dbContext.ScriptVersions
            .AsNoTracking()
            .Where(v => v.ScriptId == approvedScript.Id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestScriptVersion != null && storyboard.ScriptVersionId != latestScriptVersion.Id)
        {
            return (true, "The upstream approved Script has evolved to a newer version. Reconciliation required.");
        }

        var approvedTruthSource = await dbContext.TruthSources
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ContentItemId == storyboard.ContentItemId && t.Status == TruthSourceStatus.Approved, cancellationToken);

        if (approvedTruthSource == null)
        {
            return (true, "The TruthSource is no longer in Approved status.");
        }

        var latestTruthSourceVersion = await dbContext.TruthSourceVersions
            .AsNoTracking()
            .Where(v => v.TruthSourceId == approvedTruthSource.Id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestTruthSourceVersion != null && storyboard.TruthSourceVersionId != latestTruthSourceVersion.Id)
        {
            return (true, "The TruthSource factual foundation has evolved to a newer version. Reconciliation required.");
        }

        return (false, null);
    }

    private static StoryboardVersion CreateVersionSnapshot(Storyboard storyboard, string changeSummary, string actorEmail)
    {
        var snapshotDto = new
        {
            storyboard.Id,
            storyboard.ContentItemId,
            storyboard.ChannelId,
            storyboard.ScriptId,
            storyboard.ScriptVersionId,
            storyboard.TruthSourceId,
            storyboard.TruthSourceVersionId,
            storyboard.IsCurrent,
            storyboard.SupersededAtUtc,
            storyboard.ReconciledFromStoryboardId,
            storyboard.Title,
            storyboard.TargetDurationSeconds,
            storyboard.TotalEstimatedDurationSeconds,
            storyboard.Status,
            storyboard.RejectionReason,
            Frames = storyboard.Frames.OrderBy(f => f.OrderIndex).Select(f => new
            {
                f.Id,
                f.OrderIndex,
                f.ScriptSceneId,
                f.ScriptSceneOrderIndex,
                f.FramingIntent,
                f.CompositionIntent,
                f.CameraMotionIntent,
                f.Subject,
                f.Environment,
                f.StyleIntent,
                f.VisualPrompt,
                f.NegativePrompt,
                f.AudioCue,
                f.EstimatedDurationSeconds,
                f.OnScreenText,
                f.TransitionIntent,
                f.CreatedAtUtc,
                f.UpdatedAtUtc
            }).ToList(),
            AssetPlan = storyboard.AssetPlan == null ? null : new
            {
                storyboard.AssetPlan.Id,
                storyboard.AssetPlan.Status,
                storyboard.AssetPlan.Version,
                Requirements = storyboard.AssetPlan.Requirements.OrderBy(r => r.FrameOrderIndex ?? 999).Select(r => new
                {
                    r.Id,
                    r.FrameId,
                    r.FrameOrderIndex,
                    r.AssetType,
                    r.AspectRatio,
                    r.VisualPrompt,
                    r.NegativePrompt,
                    r.StyleIntent,
                    r.MotionIntent,
                    r.TargetDurationSeconds,
                    r.VoiceIntent,
                    r.MusicMood,
                    r.SoundEffectIntent,
                    r.SubtitleProfile,
                    r.OverlaySpecification,
                    r.CreatedAtUtc,
                    r.UpdatedAtUtc
                }).ToList()
            }
        };

        return new StoryboardVersion
        {
            Id = Guid.NewGuid(),
            StoryboardId = storyboard.Id,
            ContentItemId = storyboard.ContentItemId,
            ScriptId = storyboard.ScriptId,
            ScriptVersionId = storyboard.ScriptVersionId,
            TruthSourceId = storyboard.TruthSourceId,
            TruthSourceVersionId = storyboard.TruthSourceVersionId,
            VersionNumber = storyboard.Version,
            SnapshotJson = JsonSerializer.Serialize(snapshotDto, JsonOptions),
            ChangeSummary = changeSummary,
            Status = storyboard.Status,
            RejectionReason = storyboard.RejectionReason,
            TotalEstimatedDurationSeconds = storyboard.TotalEstimatedDurationSeconds,
            TotalFrameCount = storyboard.Frames.Count,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByEmail = actorEmail
        };
    }

    private static StoryboardDto MapToDto(Storyboard storyboard, bool isStale, string? staleReason) =>
        new(
            storyboard.Id,
            storyboard.ContentItemId,
            storyboard.ChannelId,
            storyboard.ScriptId,
            storyboard.ScriptVersionId,
            storyboard.TruthSourceId,
            storyboard.TruthSourceVersionId,
            storyboard.IsCurrent,
            storyboard.SupersededAtUtc,
            storyboard.ReconciledFromStoryboardId,
            storyboard.Title,
            storyboard.TargetDurationSeconds,
            storyboard.TotalEstimatedDurationSeconds,
            storyboard.Status,
            storyboard.RejectionReason,
            storyboard.RejectedAtUtc,
            storyboard.RejectedByEmail,
            storyboard.ApprovedAtUtc,
            storyboard.ApprovedByEmail,
            storyboard.SubmittedForReviewAtUtc,
            storyboard.SubmittedForReviewByEmail,
            isStale,
            staleReason,
            storyboard.Version,
            storyboard.CreatedAtUtc,
            storyboard.CreatedByEmail,
            storyboard.UpdatedAtUtc,
            storyboard.UpdatedByEmail,
            storyboard.Frames.OrderBy(f => f.OrderIndex).Select(MapToFrameDto).ToList(),
            storyboard.AssetPlan != null ? MapToAssetPlanDto(storyboard.AssetPlan) : null
        );

    private static StoryboardFrameDto MapToFrameDto(StoryboardFrame f) =>
        new(
            f.Id,
            f.StoryboardId,
            f.OrderIndex,
            f.ScriptSceneId,
            f.ScriptSceneOrderIndex,
            f.FramingIntent,
            f.CompositionIntent,
            f.CameraMotionIntent,
            f.Subject,
            f.Environment,
            f.StyleIntent,
            f.VisualPrompt,
            f.NegativePrompt,
            f.AudioCue,
            f.EstimatedDurationSeconds,
            f.OnScreenText,
            f.TransitionIntent,
            f.CreatedAtUtc,
            f.UpdatedAtUtc
        );

    private static AssetPlanDto MapToAssetPlanDto(AssetPlan ap) =>
        new(
            ap.Id,
            ap.StoryboardId,
            ap.ContentItemId,
            ap.Status,
            ap.Version,
            ap.CreatedAtUtc,
            ap.UpdatedAtUtc,
            ap.Requirements.OrderBy(r => r.FrameOrderIndex ?? 999).Select(MapToAssetRequirementDto).ToList()
        );

    private static AssetRequirementDto MapToAssetRequirementDto(AssetRequirement r) =>
        new(
            r.Id,
            r.AssetPlanId,
            r.FrameId,
            r.FrameOrderIndex,
            r.AssetType,
            r.AspectRatio,
            r.VisualPrompt,
            r.NegativePrompt,
            r.StyleIntent,
            r.MotionIntent,
            r.TargetDurationSeconds,
            r.VoiceIntent,
            r.MusicMood,
            r.SoundEffectIntent,
            r.SubtitleProfile,
            r.OverlaySpecification,
            r.CreatedAtUtc,
            r.UpdatedAtUtc
        );

    private static StoryboardVersionDto MapToVersionDto(StoryboardVersion v)
    {
        var assetCount = 0;
        try
        {
            if (!string.IsNullOrEmpty(v.SnapshotJson))
            {
                using var doc = JsonDocument.Parse(v.SnapshotJson);
                if (doc.RootElement.TryGetProperty("AssetPlan", out var apProp) &&
                    apProp.ValueKind == JsonValueKind.Object &&
                    apProp.TryGetProperty("Requirements", out var reqProp) &&
                    reqProp.ValueKind == JsonValueKind.Array)
                {
                    assetCount = reqProp.GetArrayLength();
                }
            }
        }
        catch
        {
            // fallback
        }

        return new StoryboardVersionDto(
            v.Id,
            v.StoryboardId,
            v.ContentItemId,
            v.ScriptId,
            v.ScriptVersionId,
            v.TruthSourceId,
            v.TruthSourceVersionId,
            v.VersionNumber,
            v.SnapshotJson,
            v.ChangeSummary,
            v.Status,
            v.RejectionReason,
            v.TotalEstimatedDurationSeconds,
            v.TotalFrameCount,
            assetCount,
            v.CreatedAtUtc,
            v.CreatedByEmail
        );
    }
}
