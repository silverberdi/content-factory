using System.Security.Cryptography;
using System.Text;
using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Infrastructure.Storage;
using ContentFactory.Api.Modules.Ai;
using Microsoft.EntityFrameworkCore;

namespace ContentFactory.Api.Modules.Content;

public class VisualGenerationService(
    AppDbContext dbContext,
    IStorageService storageService,
    IEnumerable<IVisualGenerationProvider> providers,
    IConfiguration configuration,
    ILogger<VisualGenerationService> logger) : IVisualGenerationService
{
    private readonly string _configuredProviderName = configuration["VISUAL_PROVIDER"] ?? configuration["VisualGeneration:Provider"] ?? "Mock";
    private readonly string _environment = configuration["ENVIRONMENT"] ?? "development";

    private IVisualGenerationProvider ResolveProvider(string? preferredProvider = null)
    {
        var targetName = preferredProvider ?? _configuredProviderName;
        return providers.FirstOrDefault(p => string.Equals(p.ProviderName, targetName, StringComparison.OrdinalIgnoreCase))
            ?? providers.FirstOrDefault(p => string.Equals(p.ProviderName, "Mock", StringComparison.OrdinalIgnoreCase))
            ?? providers.First();
    }

    public async Task<DispatchVisualGenerationResult> DispatchGenerationAsync(
        Guid contentItemId,
        Guid storyboardId,
        DispatchVisualGenerationRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var storyboard = await dbContext.Storyboards
            .Include(s => s.Frames)
            .Include(s => s.AssetPlan!)
                .ThenInclude(ap => ap.Requirements)
            .FirstOrDefaultAsync(s => s.Id == storyboardId && s.ContentItemId == contentItemId, cancellationToken);

        if (storyboard == null)
        {
            return new DispatchVisualGenerationResult(false, [], "Storyboard not found.");
        }

        if (!storyboard.IsCurrent)
        {
            return new DispatchVisualGenerationResult(false, [], "Cannot generate media from a superseded Storyboard.");
        }

        if (storyboard.Status != StoryboardStatus.Approved)
        {
            return new DispatchVisualGenerationResult(false, [], "Storyboard must be Approved before visual generation can be dispatched.");
        }

        if (storyboard.AssetPlan == null || storyboard.AssetPlan.Status != AssetPlanStatus.ReadyForGeneration)
        {
            return new DispatchVisualGenerationResult(false, [], "AssetPlan is not in ReadyForGeneration status.");
        }

        // Verify upstream script staleness
        var script = await dbContext.Scripts.FirstOrDefaultAsync(s => s.Id == storyboard.ScriptId, cancellationToken);
        if (script == null || script.Status != ScriptStatus.Approved)
        {
            return new DispatchVisualGenerationResult(false, [], "Upstream Script is not approved.");
        }

        var activeScriptVersion = await dbContext.ScriptVersions
            .Where(v => v.ScriptId == script.Id && v.Status == ScriptStatus.Approved)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeScriptVersion != null && activeScriptVersion.Id != storyboard.ScriptVersionId)
        {
            return new DispatchVisualGenerationResult(false, [], "Cannot generate media from a stale Storyboard. Reconcile and approve the Storyboard first.");
        }

        var activeStoryboardVersion = await dbContext.StoryboardVersions
            .Where(v => v.StoryboardId == storyboard.Id && v.Status == StoryboardStatus.Approved)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var storyboardVersionId = activeStoryboardVersion?.Id ?? Guid.NewGuid();

        // Identify target requirements
        var activeProvider = ResolveProvider();
        var candidateRequirements = new List<AssetRequirement>();

        if (request.AssetRequirementId.HasValue)
        {
            var req = storyboard.AssetPlan.Requirements.FirstOrDefault(r => r.Id == request.AssetRequirementId.Value);
            if (req == null)
            {
                return new DispatchVisualGenerationResult(false, [], "Specified AssetRequirement was not found on this Storyboard.");
            }

            if (!activeProvider.SupportedAssetTypes.Contains(req.AssetType))
            {
                return new DispatchVisualGenerationResult(false, [], $"Asset requirement type '{req.AssetType}' is not supported by visual generation provider '{activeProvider.ProviderName}'.");
            }

            candidateRequirements.Add(req);
        }
        else
        {
            var visualTypes = new[] { AssetType.AiImage, AssetType.AiVideo, AssetType.BRoll, AssetType.GraphicOverlay };
            candidateRequirements = storyboard.AssetPlan.Requirements
                .Where(r => visualTypes.Contains(r.AssetType) && activeProvider.SupportedAssetTypes.Contains(r.AssetType))
                .ToList();

            if (candidateRequirements.Count == 0)
            {
                return new DispatchVisualGenerationResult(false, [], "No eligible visual AssetRequirements found on this Storyboard.");
            }
        }

        var candidateCount = Math.Clamp(request.CandidateCount > 0 ? request.CandidateCount : 1, 1, 4);
        var jobs = new List<Job>();

        foreach (var req in candidateRequirements)
        {

            // Determine canonical GenerationRevision
            int revision;
            if (request.GenerationRevision.HasValue)
            {
                revision = request.GenerationRevision.Value;
            }
            else
            {
                var existingMaxRevision = await dbContext.Jobs
                    .Where(j => j.StoryboardVersionId == storyboardVersionId && j.SourceAssetRequirementId == req.Id)
                    .Select(j => (int?)j.GenerationRevision)
                    .MaxAsync(cancellationToken) ?? 0;

                // Check if active job exists for latest revision
                var activeForReq = await dbContext.Jobs
                    .Include(j => j.Attempts)
                    .Where(j => j.StoryboardVersionId == storyboardVersionId && j.SourceAssetRequirementId == req.Id &&
                                (j.Status == JobStatus.Queued || j.Status == JobStatus.Running))
                    .OrderByDescending(j => j.GenerationRevision)
                    .FirstOrDefaultAsync(cancellationToken);

                if (activeForReq != null)
                {
                    jobs.Add(activeForReq);
                    continue;
                }

                revision = existingMaxRevision + 1;
            }

            // Canonical batch configuration fingerprint
            var configFingerprint = ComputeSha256($"{req.VisualPrompt}:{req.NegativePrompt}:{req.StyleIntent}:{req.MotionIntent}:{req.AspectRatio}:{activeProvider.ProviderName}");

            // Canonical batch idempotency key identifies the entire generation intent
            var idempotencyKey = ComputeSha256($"{storyboardVersionId}:{req.Id}:{candidateCount}:{revision}:{configFingerprint}");

            // Idempotency check: if active job exists with same key, reuse it
            var existingActiveJob = await dbContext.Jobs
                .Include(j => j.Attempts)
                .FirstOrDefaultAsync(j => j.IdempotencyKey == idempotencyKey &&
                    (j.Status == JobStatus.Queued || j.Status == JobStatus.Running), cancellationToken);

            if (existingActiveJob != null)
            {
                jobs.Add(existingActiveJob);
                continue;
            }

            var job = new Job
            {
                Id = Guid.NewGuid(),
                ContentItemId = contentItemId,
                ChannelId = storyboard.ChannelId,
                JobType = JobTypes.GenerateVisualAsset,
                Capability = JobTypes.GenerateVisualAsset,
                SourceAssetRequirementId = req.Id,
                StoryboardId = storyboard.Id,
                StoryboardVersionId = storyboardVersionId,
                GenerationRevision = revision,
                Status = JobStatus.Queued,
                Provider = activeProvider.ProviderName,
                ModelOrWorkflowIdentifier = req.AssetType == AssetType.AiVideo ? "flux-video-schnell" : "flux-image-schnell",
                AttemptCount = 0,
                MaxAttempts = 3,
                AutomaticRetriesRemaining = 2,
                CandidateCount = candidateCount,
                CorrelationId = Guid.NewGuid().ToString("N"),
                IdempotencyKey = idempotencyKey,
                CreatedByEmail = actorEmail,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            dbContext.Jobs.Add(job);
            jobs.Add(job);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var jobDtos = jobs.Select(MapJobToDto).ToList();
        return new DispatchVisualGenerationResult(true, jobDtos, null);
    }

    public async Task<VisualProductionOverviewDto?> GetProductionOverviewAsync(
        Guid contentItemId,
        Guid storyboardId,
        CancellationToken cancellationToken = default)
    {
        var storyboard = await dbContext.Storyboards
            .Include(s => s.Frames)
            .Include(s => s.AssetPlan!)
                .ThenInclude(ap => ap.Requirements)
            .FirstOrDefaultAsync(s => s.Id == storyboardId && s.ContentItemId == contentItemId, cancellationToken);

        if (storyboard == null) return null;

        var script = await dbContext.Scripts.FirstOrDefaultAsync(s => s.Id == storyboard.ScriptId, cancellationToken);
        var activeScriptVersion = await dbContext.ScriptVersions
            .Where(v => v.ScriptId == storyboard.ScriptId && v.Status == ScriptStatus.Approved)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var isStale = (activeScriptVersion != null && activeScriptVersion.Id != storyboard.ScriptVersionId) ||
                      (script != null && script.Status != ScriptStatus.Approved);

        var activeStoryboardVersion = await dbContext.StoryboardVersions
            .Where(v => v.StoryboardId == storyboard.Id && v.Status == StoryboardStatus.Approved)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var storyboardVersionId = activeStoryboardVersion?.Id ?? Guid.Empty;

        var visualTypes = new[] { AssetType.AiImage, AssetType.AiVideo, AssetType.BRoll, AssetType.GraphicOverlay };
        var requirements = storyboard.AssetPlan?.Requirements
            .Where(r => visualTypes.Contains(r.AssetType))
            .OrderBy(r => r.FrameOrderIndex ?? 0)
            .ToList() ?? [];

        var reqIds = requirements.Select(r => r.Id).ToList();

        var jobs = await dbContext.Jobs
            .Include(j => j.Attempts)
            .Where(j => j.StoryboardId == storyboardId && j.SourceAssetRequirementId.HasValue && reqIds.Contains(j.SourceAssetRequirementId.Value))
            .OrderByDescending(j => j.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var generatedAssets = await dbContext.GeneratedAssets
            .Where(ga => ga.StoryboardId == storyboardId && reqIds.Contains(ga.AssetRequirementId))
            .OrderBy(ga => ga.VariantIndex)
            .ToListAsync(cancellationToken);

        var reqDtos = new List<VisualRequirementProductionDto>();
        int generatedCount = 0;
        int approvedCount = 0;
        int pendingReviewCount = 0;

        foreach (var req in requirements)
        {
            var linkedFrame = storyboard.Frames.FirstOrDefault(f => f.Id == req.FrameId);
            var reqJobs = jobs.Where(j => j.SourceAssetRequirementId == req.Id).ToList();
            var activeJob = reqJobs.FirstOrDefault(j => j.Status == JobStatus.Running || j.Status == JobStatus.Queued)
                ?? reqJobs.FirstOrDefault();

            var candidates = generatedAssets
                .Where(ga => ga.AssetRequirementId == req.Id)
                .Select(ga => MapGeneratedAssetToDto(ga, storyboardVersionId, !isStale))
                .ToList();

            var selectedCandidate = candidates.FirstOrDefault(c => c.IsSelectedForAssembly);

            if (candidates.Count > 0) generatedCount++;
            if (candidates.Any(c => c.Status == GeneratedAssetStatus.Approved)) approvedCount++;
            if (candidates.Any(c => c.Status == GeneratedAssetStatus.PendingReview)) pendingReviewCount++;

            var reqDto = new AssetRequirementDto(
                Id: req.Id,
                AssetPlanId: req.AssetPlanId,
                FrameId: req.FrameId,
                FrameOrderIndex: req.FrameOrderIndex,
                AssetType: req.AssetType,
                AspectRatio: req.AspectRatio,
                VisualPrompt: req.VisualPrompt,
                NegativePrompt: req.NegativePrompt,
                StyleIntent: req.StyleIntent,
                MotionIntent: req.MotionIntent,
                TargetDurationSeconds: req.TargetDurationSeconds,
                VoiceIntent: req.VoiceIntent,
                MusicMood: req.MusicMood,
                SoundEffectIntent: req.SoundEffectIntent,
                SubtitleProfile: req.SubtitleProfile,
                OverlaySpecification: req.OverlaySpecification,
                CreatedAtUtc: req.CreatedAtUtc,
                UpdatedAtUtc: req.UpdatedAtUtc
            );

            reqDtos.Add(new VisualRequirementProductionDto(
                Requirement: reqDto,
                FrameOrderIndex: req.FrameOrderIndex ?? linkedFrame?.OrderIndex ?? 0,
                FramingIntent: linkedFrame?.FramingIntent ?? FramingIntent.MediumShot,
                ScriptSceneName: $"Scene {linkedFrame?.ScriptSceneOrderIndex ?? 1}",
                EstimatedDurationSeconds: linkedFrame?.EstimatedDurationSeconds ?? 3.0,
                ActiveJob: activeJob != null ? MapJobToDto(activeJob) : null,
                Candidates: candidates,
                SelectedCandidate: selectedCandidate
            ));
        }

        var isEligible = storyboard.IsCurrent &&
                         storyboard.Status == StoryboardStatus.Approved &&
                         !isStale &&
                         storyboard.AssetPlan?.Status == AssetPlanStatus.ReadyForGeneration;

        string? ineligibilityReason = null;
        if (!storyboard.IsCurrent) ineligibilityReason = "Storyboard has been superseded.";
        else if (storyboard.Status != StoryboardStatus.Approved) ineligibilityReason = "Storyboard is not approved.";
        else if (isStale) ineligibilityReason = "Storyboard is stale relative to approved Script.";
        else if (storyboard.AssetPlan?.Status != AssetPlanStatus.ReadyForGeneration) ineligibilityReason = "AssetPlan is not ready for generation.";

        var activeJobsCount = jobs.Count(j => j.Status == JobStatus.Queued || j.Status == JobStatus.Running);

        return new VisualProductionOverviewDto(
            ContentItemId: contentItemId,
            ChannelId: storyboard.ChannelId,
            StoryboardId: storyboard.Id,
            StoryboardVersionId: storyboardVersionId,
            StoryboardVersion: storyboard.Version,
            IsStoryboardCurrent: storyboard.IsCurrent,
            IsStoryboardApproved: storyboard.Status == StoryboardStatus.Approved,
            IsStoryboardStale: isStale,
            TotalRequirementsCount: requirements.Count,
            GeneratedCount: generatedCount,
            ApprovedCount: approvedCount,
            PendingReviewCount: pendingReviewCount,
            ActiveJobsCount: activeJobsCount,
            IsEligibleForGeneration: isEligible,
            IneligibilityReason: ineligibilityReason,
            Requirements: reqDtos
        );
    }

    public async Task<GeneratedAssetDto?> ReviewCandidateAsync(
        Guid generatedAssetId,
        ReviewGeneratedAssetRequest request,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var asset = await dbContext.GeneratedAssets.FirstOrDefaultAsync(a => a.Id == generatedAssetId, cancellationToken);
        if (asset == null) return null;

        if (!string.IsNullOrWhiteSpace(request.ExpectedStatus) &&
            !string.Equals(asset.Status, request.ExpectedStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Candidate status conflict: Expected '{request.ExpectedStatus}' but current status is '{asset.Status}'.");
        }

        if (string.Equals(request.Status, GeneratedAssetStatus.Approved, StringComparison.OrdinalIgnoreCase))
        {
            asset.Status = GeneratedAssetStatus.Approved;
            asset.RejectionReason = null;
            asset.ReviewedAtUtc = DateTime.UtcNow;
            asset.ReviewedByEmail = actorEmail;
            asset.IsSelectedForAssembly = true;
            asset.UpdatedAtUtc = DateTime.UtcNow;

            // Atomically unselect all other candidates for this requirement
            var siblings = await dbContext.GeneratedAssets
                .Where(a => a.AssetRequirementId == asset.AssetRequirementId && a.Id != asset.Id && a.IsSelectedForAssembly)
                .ToListAsync(cancellationToken);

            foreach (var sibling in siblings)
            {
                sibling.IsSelectedForAssembly = false;
                sibling.UpdatedAtUtc = DateTime.UtcNow;
            }
        }
        else if (string.Equals(request.Status, GeneratedAssetStatus.Rejected, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.RejectionReason))
            {
                throw new ArgumentException("Rejection reason is required.");
            }

            asset.Status = GeneratedAssetStatus.Rejected;
            asset.RejectionReason = request.RejectionReason.Trim();
            asset.ReviewedAtUtc = DateTime.UtcNow;
            asset.ReviewedByEmail = actorEmail;
            asset.IsSelectedForAssembly = false;
            asset.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            throw new ArgumentException($"Invalid review status: {request.Status}");
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var activeVersion = await dbContext.StoryboardVersions
            .Where(v => v.StoryboardId == asset.StoryboardId && v.Status == StoryboardStatus.Approved)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var activeVersionId = activeVersion?.Id ?? Guid.Empty;
        return MapGeneratedAssetToDto(asset, activeVersionId, true);
    }

    public async Task<GeneratedAssetDto?> SelectCandidateForAssemblyAsync(
        Guid generatedAssetId,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var asset = await dbContext.GeneratedAssets.FirstOrDefaultAsync(a => a.Id == generatedAssetId, cancellationToken);
        if (asset == null) return null;

        if (string.Equals(asset.Status, GeneratedAssetStatus.Rejected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("A Rejected candidate asset cannot be selected for assembly. Approve it first.");
        }

        asset.Status = GeneratedAssetStatus.Approved;
        asset.ReviewedAtUtc = DateTime.UtcNow;
        asset.ReviewedByEmail = actorEmail;
        asset.RejectionReason = null;
        asset.IsSelectedForAssembly = true;
        asset.UpdatedAtUtc = DateTime.UtcNow;

        var siblings = await dbContext.GeneratedAssets
            .Where(a => a.AssetRequirementId == asset.AssetRequirementId && a.Id != asset.Id && a.IsSelectedForAssembly)
            .ToListAsync(cancellationToken);

        foreach (var sibling in siblings)
        {
            sibling.IsSelectedForAssembly = false;
            sibling.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var activeVersion = await dbContext.StoryboardVersions
            .Where(v => v.StoryboardId == asset.StoryboardId && v.Status == StoryboardStatus.Approved)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var activeVersionId = activeVersion?.Id ?? Guid.Empty;
        return MapGeneratedAssetToDto(asset, activeVersionId, true);
    }

    public async Task<JobDto?> GetJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await dbContext.Jobs
            .Include(j => j.Attempts)
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        return job == null ? null : MapJobToDto(job);
    }

    public async Task<JobDto?> RetryJobAsync(Guid jobId, string actorEmail, CancellationToken cancellationToken = default)
    {
        var job = await dbContext.Jobs
            .Include(j => j.Attempts)
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        if (job == null) return null;

        if (job.Status != JobStatus.FailedRetryable && job.Status != JobStatus.FailedActionRequired)
        {
            throw new InvalidOperationException($"Only failed jobs may be retried. Current job status is '{job.Status}'.");
        }

        // Manual retry resets automated retry budget and returns job to Queued without erasing prior attempts
        job.Status = JobStatus.Queued;
        job.AutomaticRetriesRemaining = 2;
        job.ErrorCode = null;
        job.SanitizedErrorMessage = null;
        job.IsRetryable = false;
        job.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapJobToDto(job);
    }

    private static readonly object _inMemoryLock = new();

    public async Task<Job?> TryClaimNextJobAsync(CancellationToken cancellationToken = default)
    {
        var candidate = await dbContext.Jobs
            .Where(j => j.Status == JobStatus.Queued)
            .OrderBy(j => j.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (candidate == null) return null;

        var now = DateTime.UtcNow;

        if (dbContext.Database.IsRelational())
        {
            var affected = await dbContext.Jobs
                .Where(j => j.Id == candidate.Id && j.Status == JobStatus.Queued)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(j => j.Status, JobStatus.Running)
                    .SetProperty(j => j.StartedAtUtc, now)
                    .SetProperty(j => j.UpdatedAtUtc, now), cancellationToken);

            if (affected == 1)
            {
                return await dbContext.Jobs
                    .Include(j => j.Attempts)
                    .FirstOrDefaultAsync(j => j.Id == candidate.Id, cancellationToken);
            }

            return null;
        }
        else
        {
            lock (_inMemoryLock)
            {
                var job = dbContext.Jobs
                    .Include(j => j.Attempts)
                    .FirstOrDefault(j => j.Id == candidate.Id && j.Status == JobStatus.Queued);

                if (job != null)
                {
                    job.Status = JobStatus.Running;
                    job.StartedAtUtc = now;
                    job.UpdatedAtUtc = now;
                    dbContext.SaveChanges();
                    return job;
                }

                return null;
            }
        }
    }

    public async Task ProcessQueuedJobsAsync(CancellationToken cancellationToken = default)
    {
        Job? job;
        while ((job = await TryClaimNextJobAsync(cancellationToken)) != null)
        {
            await ExecuteClaimedJobAsync(job, cancellationToken);
        }
    }

    private async Task ExecuteClaimedJobAsync(Job job, CancellationToken cancellationToken)
    {
        job.AttemptCount++;
        var attemptNumber = job.Attempts.Count + 1;

        var requirement = job.SourceAssetRequirementId.HasValue
            ? await dbContext.AssetRequirements.FirstOrDefaultAsync(r => r.Id == job.SourceAssetRequirementId.Value, cancellationToken)
            : null;

        if (requirement == null)
        {
            job.Status = JobStatus.FailedActionRequired;
            job.ErrorCode = "MISSING_ASSET_REQUIREMENT";
            job.SanitizedErrorMessage = "The referenced AssetRequirement could not be found.";
            job.CompletedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var provider = ResolveProvider(job.Provider);
        var request = new VisualGenerationRequest(
            JobId: job.Id,
            CorrelationId: job.CorrelationId,
            ContentItemId: job.ContentItemId,
            ChannelId: job.ChannelId,
            StoryboardId: job.StoryboardId ?? Guid.Empty,
            StoryboardVersionId: job.StoryboardVersionId ?? Guid.Empty,
            AssetRequirementId: requirement.Id,
            AssetType: requirement.AssetType,
            AspectRatio: requirement.AspectRatio,
            TargetWidth: 1080,
            TargetHeight: 1920,
            TargetDurationSeconds: requirement.TargetDurationSeconds,
            VisualPrompt: requirement.VisualPrompt,
            NegativePrompt: requirement.NegativePrompt,
            StyleIntent: requirement.StyleIntent,
            MotionIntent: requirement.MotionIntent,
            CandidateCount: job.CandidateCount > 0 ? job.CandidateCount : 1
        );

        var attempt = new JobAttempt
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            AttemptNumber = attemptNumber,
            StartedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };

        try
        {
            var result = await provider.GenerateVisualAssetAsync(request, cancellationToken);
            attempt.CompletedAtUtc = DateTime.UtcNow;
            attempt.DurationMs = result.ExecutionDurationMs;
            attempt.EstimatedCostUsd = result.EstimatedCostUsd;
            attempt.ActualCostUsd = result.ActualCostUsd;

            if (result.Success)
            {
                attempt.Status = JobStatus.Succeeded;

                foreach (var output in result.Outputs)
                {
                    var assetId = Guid.NewGuid();
                    var objectKey = storageService.GenerateObjectKey(
                        _environment,
                        job.ChannelId,
                        job.ContentItemId,
                        job.StoryboardVersionId ?? Guid.Empty,
                        requirement.Id,
                        assetId,
                        output.FileExtension
                    );

                    using var mediaStream = new MemoryStream(output.MediaBytes);
                    var upload = await storageService.UploadAsync(objectKey, mediaStream, output.ContentType, cancellationToken);

                    var generatedAsset = new GeneratedAsset
                    {
                        Id = assetId,
                        ContentItemId = job.ContentItemId,
                        ChannelId = job.ChannelId,
                        StoryboardId = job.StoryboardId ?? Guid.Empty,
                        StoryboardVersionId = job.StoryboardVersionId ?? Guid.Empty,
                        AssetRequirementId = requirement.Id,
                        JobId = job.Id,
                        VariantIndex = output.VariantIndex,
                        AssetType = requirement.AssetType,
                        MediaType = requirement.AssetType == AssetType.AiVideo ? "Video" : "Image",
                        StorageProvider = "MinIO",
                        StorageKey = upload.ObjectKey,
                        ContentType = upload.ContentType,
                        FileSizeBytes = upload.FileSizeBytes,
                        Width = output.Width,
                        Height = output.Height,
                        DurationSeconds = output.DurationSeconds,
                        ChecksumSha256 = upload.ChecksumSha256,
                        Provider = provider.ProviderName,
                        ProviderModelOrWorkflow = output.ProviderModelOrWorkflow,
                        GenerationParametersSnapshot = output.GenerationParametersSnapshot,
                        Status = GeneratedAssetStatus.PendingReview,
                        IsSelectedForAssembly = false,
                        CreatedAtUtc = DateTime.UtcNow,
                        UpdatedAtUtc = DateTime.UtcNow
                    };

                    dbContext.GeneratedAssets.Add(generatedAsset);
                }

                job.Status = JobStatus.Succeeded;
                job.CompletedAtUtc = DateTime.UtcNow;
                job.DurationMs = result.ExecutionDurationMs;
                job.EstimatedCostUsd = result.EstimatedCostUsd;
                job.ActualCostUsd = result.ActualCostUsd;
                job.ErrorCode = null;
                job.SanitizedErrorMessage = null;
            }
            else
            {
                attempt.Status = result.IsRetryable ? JobStatus.FailedRetryable : JobStatus.FailedActionRequired;
                attempt.ErrorCode = result.ErrorCode;
                attempt.ErrorMessage = result.ErrorMessage;

                if (result.IsRetryable && job.AutomaticRetriesRemaining > 0)
                {
                    job.AutomaticRetriesRemaining--;
                    job.Status = JobStatus.Queued; // Return to Queued for automatic retry
                    job.IsRetryable = true;
                    job.ErrorCode = result.ErrorCode;
                    job.SanitizedErrorMessage = SanitizeErrorMessage(result.ErrorMessage);
                }
                else if (!result.IsRetryable)
                {
                    job.Status = JobStatus.FailedActionRequired;
                    job.IsRetryable = false;
                    job.ErrorCode = result.ErrorCode;
                    job.SanitizedErrorMessage = SanitizeErrorMessage(result.ErrorMessage);
                }
                else
                {
                    job.Status = JobStatus.FailedActionRequired;
                    job.IsRetryable = false;
                    job.ErrorCode = result.ErrorCode;
                    job.SanitizedErrorMessage = $"Maximum automated retry attempts ({job.MaxAttempts}) reached. {SanitizeErrorMessage(result.ErrorMessage)}";
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error processing Job {JobId}", job.Id);
            attempt.CompletedAtUtc = DateTime.UtcNow;
            attempt.Status = JobStatus.FailedActionRequired;
            attempt.ErrorCode = "JOB_EXECUTION_EXCEPTION";
            attempt.ErrorMessage = ex.Message;

            job.Status = JobStatus.FailedActionRequired;
            job.ErrorCode = "JOB_EXECUTION_EXCEPTION";
            job.SanitizedErrorMessage = "An unexpected error occurred during job execution.";
            job.CompletedAtUtc = DateTime.UtcNow;
        }

        dbContext.JobAttempts.Add(attempt);
        job.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    private static string SanitizeErrorMessage(string? error)
    {
        if (string.IsNullOrWhiteSpace(error)) return "Unknown operational error.";
        var sanitized = error.Replace("Bearer ", "Bearer [REDACTED]")
                             .Replace("ApiKey ", "ApiKey [REDACTED]");
        return sanitized.Length > 512 ? sanitized[..512] + "..." : sanitized;
    }

    private static JobDto MapJobToDto(Job job)
    {
        return new JobDto(
            Id: job.Id,
            ContentItemId: job.ContentItemId,
            ChannelId: job.ChannelId,
            JobType: job.JobType,
            Capability: job.Capability,
            SourceAssetRequirementId: job.SourceAssetRequirementId,
            StoryboardId: job.StoryboardId,
            StoryboardVersionId: job.StoryboardVersionId,
            GenerationRevision: job.GenerationRevision,
            Status: job.Status,
            Provider: job.Provider,
            ModelOrWorkflowIdentifier: job.ModelOrWorkflowIdentifier,
            AttemptCount: job.AttemptCount,
            MaxAttempts: job.MaxAttempts,
            AutomaticRetriesRemaining: job.AutomaticRetriesRemaining,
            CandidateCount: job.CandidateCount,
            StartedAtUtc: job.StartedAtUtc,
            CompletedAtUtc: job.CompletedAtUtc,
            DurationMs: job.DurationMs,
            EstimatedCostUsd: job.EstimatedCostUsd,
            ActualCostUsd: job.ActualCostUsd,
            CorrelationId: job.CorrelationId,
            ErrorCode: job.ErrorCode,
            SanitizedErrorMessage: job.SanitizedErrorMessage,
            IsRetryable: job.IsRetryable,
            CreatedByEmail: job.CreatedByEmail,
            CreatedAtUtc: job.CreatedAtUtc,
            UpdatedAtUtc: job.UpdatedAtUtc,
            Attempts: job.Attempts.Select(a => new JobAttemptDto(
                Id: a.Id,
                JobId: a.JobId,
                AttemptNumber: a.AttemptNumber,
                StartedAtUtc: a.StartedAtUtc,
                CompletedAtUtc: a.CompletedAtUtc,
                DurationMs: a.DurationMs,
                Status: a.Status,
                ErrorCode: a.ErrorCode,
                ErrorMessage: a.ErrorMessage,
                EstimatedCostUsd: a.EstimatedCostUsd,
                ActualCostUsd: a.ActualCostUsd
            )).ToList()
        );
    }

    private static GeneratedAssetDto MapGeneratedAssetToDto(GeneratedAsset asset, Guid activeStoryboardVersionId, bool isStoryboardActive)
    {
        var isEligible = isStoryboardActive &&
                         asset.StoryboardVersionId == activeStoryboardVersionId &&
                         asset.Status == GeneratedAssetStatus.Approved &&
                         asset.IsSelectedForAssembly;

        return new GeneratedAssetDto(
            Id: asset.Id,
            ContentItemId: asset.ContentItemId,
            ChannelId: asset.ChannelId,
            StoryboardId: asset.StoryboardId,
            StoryboardVersionId: asset.StoryboardVersionId,
            AssetRequirementId: asset.AssetRequirementId,
            JobId: asset.JobId,
            VariantIndex: asset.VariantIndex,
            AssetType: asset.AssetType,
            MediaType: asset.MediaType,
            StorageProvider: asset.StorageProvider,
            StorageKey: asset.StorageKey,
            ContentType: asset.ContentType,
            FileSizeBytes: asset.FileSizeBytes,
            Width: asset.Width,
            Height: asset.Height,
            DurationSeconds: asset.DurationSeconds,
            ChecksumSha256: asset.ChecksumSha256,
            Provider: asset.Provider,
            ProviderModelOrWorkflow: asset.ProviderModelOrWorkflow,
            GenerationParametersSnapshot: asset.GenerationParametersSnapshot,
            Status: asset.Status,
            RejectionReason: asset.RejectionReason,
            ReviewedAtUtc: asset.ReviewedAtUtc,
            ReviewedByEmail: asset.ReviewedByEmail,
            IsSelectedForAssembly: asset.IsSelectedForAssembly,
            CreatedAtUtc: asset.CreatedAtUtc,
            UpdatedAtUtc: asset.UpdatedAtUtc,
            IsEligibleForAssembly: isEligible
        );
    }
}
