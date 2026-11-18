using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ContentFactory.Api.Infrastructure;
using ContentFactory.Api.Infrastructure.Storage;
using ContentFactory.Api.Modules.Ai;
using ContentFactory.Api.Modules.Channels;
using Microsoft.EntityFrameworkCore;

namespace ContentFactory.Api.Modules.Content;

public class VoiceGenerationService(
    AppDbContext dbContext,
    IStorageService storageService,
    IEnumerable<IVoiceGenerationProvider> providers,
    IConfiguration configuration,
    ILogger<VoiceGenerationService> logger) : IVoiceGenerationService
{
    private readonly string _configuredProviderName = configuration["VOICE_PROVIDER"] ?? configuration["VoiceGeneration:Provider"] ?? "Mock";
    private readonly string _environment = configuration["ENVIRONMENT"] ?? "development";

    private readonly TimingTolerancePolicy _timingPolicy = new(
        NoticeThresholdSeconds: double.TryParse(configuration["VoiceGeneration:NoticeThresholdSeconds"], out var n) ? n : 2.0,
        WarningThresholdSeconds: double.TryParse(configuration["VoiceGeneration:WarningThresholdSeconds"], out var w) ? w : 4.0
    );

    private IVoiceGenerationProvider ResolveProvider(string? preferredProvider = null)
    {
        var targetName = preferredProvider ?? _configuredProviderName;
        return providers.FirstOrDefault(p => string.Equals(p.ProviderName, targetName, StringComparison.OrdinalIgnoreCase))
            ?? providers.FirstOrDefault(p => string.Equals(p.ProviderName, "Mock", StringComparison.OrdinalIgnoreCase))
            ?? providers.First();
    }

    public async Task<DispatchVoiceGenerationResult> DispatchGenerationAsync(
        Guid contentItemId,
        Guid storyboardId,
        DispatchVoiceGenerationRequest request,
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
            return new DispatchVoiceGenerationResult(false, [], "Storyboard not found.");
        }

        if (!storyboard.IsCurrent)
        {
            return new DispatchVoiceGenerationResult(false, [], "Cannot generate voice from a superseded Storyboard.");
        }

        if (storyboard.Status != StoryboardStatus.Approved)
        {
            return new DispatchVoiceGenerationResult(false, [], "Storyboard must be Approved before voice generation can be dispatched.");
        }

        if (storyboard.AssetPlan == null || storyboard.AssetPlan.Status != AssetPlanStatus.ReadyForGeneration)
        {
            return new DispatchVoiceGenerationResult(false, [], "AssetPlan is not in ReadyForGeneration status.");
        }

        // Verify upstream script staleness
        var script = await dbContext.Scripts
            .Include(s => s.Scenes)
            .FirstOrDefaultAsync(s => s.Id == storyboard.ScriptId, cancellationToken);

        if (script == null || script.Status != ScriptStatus.Approved)
        {
            return new DispatchVoiceGenerationResult(false, [], "Upstream Script is not approved.");
        }

        var activeScriptVersion = await dbContext.ScriptVersions
            .Where(v => v.ScriptId == script.Id && v.Status == ScriptStatus.Approved)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeScriptVersion != null && activeScriptVersion.Id != storyboard.ScriptVersionId)
        {
            return new DispatchVoiceGenerationResult(false, [], "Cannot generate voice from a stale Storyboard. Reconcile and approve the Storyboard first.");
        }

        var activeStoryboardVersion = await dbContext.StoryboardVersions
            .Where(v => v.StoryboardId == storyboard.Id && v.Status == StoryboardStatus.Approved)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var storyboardVersionId = activeStoryboardVersion?.Id ?? Guid.NewGuid();

        // Identify TtsVoiceover requirements
        var activeProvider = ResolveProvider();
        var candidateRequirements = new List<AssetRequirement>();

        if (request.AssetRequirementId.HasValue)
        {
            var req = storyboard.AssetPlan.Requirements.FirstOrDefault(r => r.Id == request.AssetRequirementId.Value);
            if (req == null)
            {
                return new DispatchVoiceGenerationResult(false, [], "Specified AssetRequirement was not found on this Storyboard.");
            }

            if (req.AssetType != AssetType.TtsVoiceover)
            {
                return new DispatchVoiceGenerationResult(false, [], $"Asset requirement type '{req.AssetType}' is not a TtsVoiceover requirement.");
            }

            candidateRequirements.Add(req);
        }
        else
        {
            candidateRequirements = storyboard.AssetPlan.Requirements
                .Where(r => r.AssetType == AssetType.TtsVoiceover)
                .ToList();

            if (candidateRequirements.Count == 0)
            {
                return new DispatchVoiceGenerationResult(false, [], "No TtsVoiceover AssetRequirements found on this Storyboard.");
            }
        }

        // Resolve Channel and VoiceProfile
        var channel = await dbContext.Channels.FirstOrDefaultAsync(c => c.Id == storyboard.ChannelId, cancellationToken);
        var voiceProfile = VoiceProfiles.GetProfileForChannel(channel?.Slug, request.VoiceProfileKey);

        // Normalize narration text from approved script scenes
        var normalizedNarration = ExtractNormalizedNarration(script, activeScriptVersion);
        var candidateCount = Math.Clamp(request.CandidateCount > 0 ? request.CandidateCount : 1, 1, 2);
        var jobs = new List<Job>();

        foreach (var req in candidateRequirements)
        {
            int revision;
            if (request.GenerationRevision.HasValue)
            {
                revision = request.GenerationRevision.Value;
            }
            else
            {
                var existingMaxRevision = await dbContext.Jobs
                    .Where(j => j.StoryboardVersionId == storyboardVersionId && j.SourceAssetRequirementId == req.Id && j.JobType == JobTypes.GenerateVoiceAsset)
                    .Select(j => (int?)j.GenerationRevision)
                    .MaxAsync(cancellationToken) ?? 0;

                var activeForReq = await dbContext.Jobs
                    .Include(j => j.Attempts)
                    .Where(j => j.StoryboardVersionId == storyboardVersionId && j.SourceAssetRequirementId == req.Id &&
                                j.JobType == JobTypes.GenerateVoiceAsset &&
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
            var configFingerprint = ComputeSha256($"{normalizedNarration}:{SerializeVoiceProfileCanonical(voiceProfile)}:{request.PronunciationGuidance}:{activeProvider.ProviderName}");

            // Canonical batch idempotency key
            var idempotencyKey = ComputeSha256($"{storyboardVersionId}:{req.Id}:{candidateCount}:{revision}:{configFingerprint}");

            // Idempotency check: reuse active job if same key
            var existingActiveJob = await dbContext.Jobs
                .Include(j => j.Attempts)
                .FirstOrDefaultAsync(j => j.IdempotencyKey == idempotencyKey &&
                    (j.Status == JobStatus.Queued || j.Status == JobStatus.Running), cancellationToken);

            if (existingActiveJob != null)
            {
                jobs.Add(existingActiveJob);
                continue;
            }

            var executionSnapshot = JsonSerializer.Serialize(new
            {
                profileKey = voiceProfile.ProfileKey,
                profile = voiceProfile,
                pronunciationGuidance = request.PronunciationGuidance,
                narrationFingerprint = ComputeSha256(normalizedNarration)
            });

            var job = new Job
            {
                Id = Guid.NewGuid(),
                ContentItemId = contentItemId,
                ChannelId = storyboard.ChannelId,
                JobType = JobTypes.GenerateVoiceAsset,
                SourceAssetRequirementId = req.Id,
                StoryboardId = storyboard.Id,
                StoryboardVersionId = activeStoryboardVersion?.Id,
                GenerationRevision = revision,
                Status = JobStatus.Queued,
                Provider = activeProvider.ProviderName,
                ModelOrWorkflowIdentifier = activeProvider.ModelOrWorkflowIdentifier,
                AttemptCount = 0,
                MaxAttempts = 3,
                AutomaticRetriesRemaining = 2,
                CandidateCount = candidateCount,
                CorrelationId = Guid.NewGuid().ToString("N"),
                ExecutionParametersSnapshot = executionSnapshot,
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
        return new DispatchVoiceGenerationResult(true, jobDtos, null);
    }

    public async Task<VoiceProductionOverviewDto?> GetProductionOverviewAsync(
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

        var script = await dbContext.Scripts
            .Include(s => s.Scenes)
            .FirstOrDefaultAsync(s => s.Id == storyboard.ScriptId, cancellationToken);

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

        var requirements = storyboard.AssetPlan?.Requirements
            .Where(r => r.AssetType == AssetType.TtsVoiceover)
            .OrderBy(r => r.FrameOrderIndex ?? 0)
            .ToList() ?? [];

        var reqIds = requirements.Select(r => r.Id).ToList();

        var jobs = await dbContext.Jobs
            .Include(j => j.Attempts)
            .Where(j => j.StoryboardId == storyboardId && j.SourceAssetRequirementId.HasValue && reqIds.Contains(j.SourceAssetRequirementId.Value) && j.JobType == JobTypes.GenerateVoiceAsset)
            .OrderByDescending(j => j.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var generatedAssets = await dbContext.GeneratedAssets
            .Where(ga => ga.StoryboardId == storyboardId && reqIds.Contains(ga.AssetRequirementId))
            .OrderBy(ga => ga.VariantIndex)
            .ToListAsync(cancellationToken);

        var channel = await dbContext.Channels.FirstOrDefaultAsync(c => c.Id == storyboard.ChannelId, cancellationToken);
        var effectiveProfile = VoiceProfiles.GetProfileForChannel(channel?.Slug);

        var reqDtos = new List<VoiceRequirementProductionDto>();
        int generatedCount = 0;
        int approvedCount = 0;
        int pendingReviewCount = 0;

        double? overallMeasuredDuration = null;

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
            var approvedCandidate = candidates.FirstOrDefault(c => c.Status == GeneratedAssetStatus.Approved);
            var candidateForTiming = selectedCandidate ?? approvedCandidate ?? candidates.LastOrDefault();

            double? timingDeviation = null;
            string timingStatus = "Normal";

            if (candidateForTiming?.DurationSeconds != null && script != null)
            {
                var estDuration = script.EstimatedDurationSeconds > 0 ? script.EstimatedDurationSeconds : 45.0;
                timingDeviation = Math.Round(candidateForTiming.DurationSeconds.Value - estDuration, 1);
                timingStatus = ClassifyTimingDeviation(timingDeviation.Value, _timingPolicy);
                overallMeasuredDuration = candidateForTiming.DurationSeconds.Value;
            }

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

            reqDtos.Add(new VoiceRequirementProductionDto(
                Requirement: reqDto,
                FrameOrderIndex: req.FrameOrderIndex ?? linkedFrame?.OrderIndex,
                ScriptSceneName: linkedFrame != null ? $"Scene {linkedFrame.ScriptSceneOrderIndex}" : null,
                EstimatedDurationSeconds: script?.EstimatedDurationSeconds ?? 45.0,
                ActiveJob: activeJob != null ? MapJobForEditorialOverview(activeJob) : null,
                Candidates: candidates,
                SelectedCandidate: selectedCandidate,
                TimingDeviationSeconds: timingDeviation,
                TimingStatus: timingStatus
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

        var scriptScenesSummary = script?.Scenes
            .OrderBy(s => s.OrderIndex)
            .Select(s => new VoiceScriptSceneSummaryDto(
                SceneId: s.Id,
                OrderIndex: s.OrderIndex,
                SceneType: s.SceneType,
                NarrationText: s.NarrationText,
                WordCount: s.WordCount,
                EstimatedDurationSeconds: s.EstimatedDurationSeconds
            ))
            .ToList() ?? [];

        double? overallDeviation = null;
        string overallTimingStatus = "Normal";
        if (overallMeasuredDuration.HasValue && script != null)
        {
            var estDuration = script.EstimatedDurationSeconds > 0 ? script.EstimatedDurationSeconds : 45.0;
            overallDeviation = Math.Round(overallMeasuredDuration.Value - estDuration, 1);
            overallTimingStatus = ClassifyTimingDeviation(overallDeviation.Value, _timingPolicy);
        }

        var normalizedNarration = ExtractNormalizedNarration(script, activeScriptVersion);

        return new VoiceProductionOverviewDto(
            ContentItemId: contentItemId,
            ChannelId: storyboard.ChannelId,
            ScriptId: storyboard.ScriptId,
            ScriptVersionId: storyboard.ScriptVersionId,
            StoryboardId: storyboard.Id,
            StoryboardVersionId: storyboardVersionId,
            StoryboardVersion: storyboard.Version,
            NarrationFingerprint: ComputeSha256(normalizedNarration),
            ScriptEstimatedDurationSeconds: script?.EstimatedDurationSeconds ?? 45.0,
            MeasuredVoiceDurationSeconds: overallMeasuredDuration,
            TimingDeviationSeconds: overallDeviation,
            TimingStatus: overallTimingStatus,
            EffectiveVoiceProfile: effectiveProfile,
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
            Requirements: reqDtos,
            ScriptScenes: scriptScenesSummary
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
        if (asset.AssetType != AssetType.TtsVoiceover)
        {
            throw new InvalidOperationException("Voice review is only supported for TtsVoiceover assets.");
        }

        if (asset.Status != GeneratedAssetStatus.PendingReview)
        {
            throw new InvalidOperationException("Only PendingReview voice candidates can be reviewed.");
        }

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

        return MapGeneratedAssetToDto(asset, activeVersion?.Id ?? Guid.Empty, true);
    }

    public async Task<GeneratedAssetDto?> SelectCandidateForAssemblyAsync(
        Guid generatedAssetId,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var asset = await dbContext.GeneratedAssets.FirstOrDefaultAsync(a => a.Id == generatedAssetId, cancellationToken);
        if (asset == null) return null;
        if (asset.AssetType != AssetType.TtsVoiceover)
        {
            throw new InvalidOperationException("Voice selection is only supported for TtsVoiceover assets.");
        }

        if (asset.Status != GeneratedAssetStatus.Approved)
        {
            throw new InvalidOperationException("Only Approved voice candidates can be selected for assembly.");
        }

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

        return MapGeneratedAssetToDto(asset, activeVersion?.Id ?? Guid.Empty, true);
    }

    public async Task<JobDto?> GetJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await dbContext.Jobs
            .Include(j => j.Attempts)
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        return job == null ? null : MapJobToDto(job);
    }

    public async Task<JobDto?> RetryJobAsync(
        Guid jobId,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var job = await dbContext.Jobs
            .Include(j => j.Attempts)
            .FirstOrDefaultAsync(j => j.Id == jobId && j.JobType == JobTypes.GenerateVoiceAsset, cancellationToken);

        if (job == null) return null;

        if (job.Status != JobStatus.FailedRetryable && job.Status != JobStatus.FailedActionRequired)
        {
            throw new InvalidOperationException($"Cannot retry job in status '{job.Status}'.");
        }

        job.Status = JobStatus.Queued;
        job.AutomaticRetriesRemaining = 2;
        job.StartedAtUtc = null;
        job.CompletedAtUtc = null;
        job.ErrorCode = null;
        job.SanitizedErrorMessage = null;
        job.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapJobToDto(job);
    }

    private static readonly object _inMemoryLock = new();

    public async Task<Job?> TryClaimNextJobAsync(CancellationToken cancellationToken = default)
    {
        var candidate = await dbContext.Jobs
            .Where(j => j.Status == JobStatus.Queued && j.JobType == JobTypes.GenerateVoiceAsset)
            .OrderBy(j => j.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (candidate == null) return null;

        var now = DateTime.UtcNow;

        if (dbContext.Database.IsRelational())
        {
            var affected = await dbContext.Jobs
                .Where(j => j.Id == candidate.Id && j.Status == JobStatus.Queued && j.JobType == JobTypes.GenerateVoiceAsset)
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
                    .FirstOrDefault(j => j.Id == candidate.Id && j.Status == JobStatus.Queued && j.JobType == JobTypes.GenerateVoiceAsset);

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

        var storyboard = await dbContext.Storyboards.FirstOrDefaultAsync(s => s.Id == job.StoryboardId, cancellationToken);
        var script = storyboard != null ? await dbContext.Scripts.Include(s => s.Scenes).FirstOrDefaultAsync(s => s.Id == storyboard.ScriptId, cancellationToken) : null;
        var activeScriptVersion = script != null ? await dbContext.ScriptVersions.Where(v => v.ScriptId == script.Id && v.Status == ScriptStatus.Approved).OrderByDescending(v => v.VersionNumber).FirstOrDefaultAsync(cancellationToken) : null;

        var normalizedNarration = ExtractNormalizedNarration(script, activeScriptVersion);
        
        VoiceProfile voiceProfile = null!;
        string? pronunciationGuidanceText = null;

        if (!string.IsNullOrWhiteSpace(job.ExecutionParametersSnapshot))
        {
            try
            {
                using var doc = JsonDocument.Parse(job.ExecutionParametersSnapshot);
                if (doc.RootElement.TryGetProperty("profile", out var profEl))
                {
                    voiceProfile = JsonSerializer.Deserialize<VoiceProfile>(profEl.GetRawText())!;
                }
                if (doc.RootElement.TryGetProperty("pronunciationGuidance", out var pgEl))
                {
                    pronunciationGuidanceText = pgEl.GetString();
                }
            }
            catch
            {
                // Fallback
            }
        }

        if (voiceProfile == null)
        {
            var channel = await dbContext.Channels.FirstOrDefaultAsync(c => c.Id == job.ChannelId, cancellationToken);
            voiceProfile = VoiceProfiles.GetProfileForChannel(channel?.Slug);
        }

        var provider = ResolveProvider(job.Provider);

        var request = new VoiceGenerationRequest(
            JobId: job.Id,
            CorrelationId: job.CorrelationId,
            ContentItemId: job.ContentItemId,
            ChannelId: job.ChannelId,
            ScriptId: script?.Id ?? Guid.Empty,
            ScriptVersionId: activeScriptVersion?.Id ?? Guid.Empty,
            StoryboardId: job.StoryboardId ?? Guid.Empty,
            StoryboardVersionId: job.StoryboardVersionId ?? Guid.Empty,
            AssetRequirementId: requirement.Id,
            Language: voiceProfile.Language,
            ConfiguredLocale: voiceProfile.ConfiguredLocale,
            NarrationText: normalizedNarration,
            VoiceProfile: voiceProfile,
            PronunciationGuidance: pronunciationGuidanceText,
            CandidateCount: job.CandidateCount > 0 ? job.CandidateCount : 1,
            TargetAudioFormat: "audio/wav",
            ChannelSlug: null
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
            var result = await provider.GenerateVoiceAssetAsync(request, cancellationToken);
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
                    var objectKey = storageService.GenerateAudioObjectKey(
                        _environment,
                        job.ChannelId,
                        job.ContentItemId,
                        job.StoryboardVersionId ?? Guid.Empty,
                        requirement.Id,
                        assetId,
                        output.FileExtension
                    );

                    using var mediaStream = new MemoryStream(output.AudioBytes);
                    var upload = await storageService.UploadAsync(objectKey, mediaStream, output.ContentType, cancellationToken);

                    var completeSnapshot = JsonSerializer.Serialize(new
                    {
                        language = voiceProfile.Language,
                        configuredLocale = voiceProfile.ConfiguredLocale,
                        profileKey = voiceProfile.ProfileKey,
                        profileName = voiceProfile.ProfileName,
                        speakingStyle = voiceProfile.SpeakingStyle,
                        tone = voiceProfile.Tone,
                        warmth = voiceProfile.Warmth,
                        energy = voiceProfile.Energy,
                        pace = voiceProfile.Pace,
                        pronunciationHints = voiceProfile.PronunciationHints,
                        pronunciationGuidance = pronunciationGuidanceText,
                        narrationFingerprint = ComputeSha256(normalizedNarration),
                        sampleRateHz = output.SampleRateHz,
                        channels = output.Channels,
                        bitDepth = output.BitDepth,
                        durationSeconds = output.DurationSeconds
                    });

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
                        AssetType = AssetType.TtsVoiceover,
                        MediaType = "Audio",
                        StorageProvider = "MinIO",
                        StorageKey = objectKey,
                        ContentType = output.ContentType,
                        FileSizeBytes = upload.FileSizeBytes,
                        DurationSeconds = output.DurationSeconds,
                        ChecksumSha256 = upload.ChecksumSha256,
                        Provider = provider.ProviderName,
                        ProviderModelOrWorkflow = output.ProviderModel,
                        GenerationParametersSnapshot = completeSnapshot,
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
                attempt.ErrorMessage = result.SanitizedErrorMessage;

                job.ErrorCode = result.ErrorCode;
                job.SanitizedErrorMessage = result.SanitizedErrorMessage;
                job.IsRetryable = result.IsRetryable;

                if (result.IsRetryable && job.AutomaticRetriesRemaining > 0)
                {
                    job.AutomaticRetriesRemaining--;
                    job.Status = JobStatus.Queued;
                    logger.LogInformation("Job {JobId} scheduled for automatic retry. Remaining: {Remaining}",
                        job.Id, job.AutomaticRetriesRemaining);
                }
                else if (result.IsRetryable)
                {
                    job.Status = JobStatus.FailedRetryable;
                    job.CompletedAtUtc = DateTime.UtcNow;
                }
                else
                {
                    job.Status = JobStatus.FailedActionRequired;
                    job.CompletedAtUtc = DateTime.UtcNow;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            attempt.Status = JobStatus.Cancelled;
            attempt.CompletedAtUtc = DateTime.UtcNow;
            job.Status = JobStatus.Cancelled;
            job.CompletedAtUtc = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error executing voice Job {JobId}", job.Id);
            attempt.Status = JobStatus.FailedActionRequired;
            attempt.ErrorCode = "INTERNAL_EXECUTION_ERROR";
            attempt.ErrorMessage = "An internal error occurred during voice generation execution.";
            attempt.CompletedAtUtc = DateTime.UtcNow;

            job.Status = JobStatus.FailedActionRequired;
            job.ErrorCode = "INTERNAL_EXECUTION_ERROR";
            job.SanitizedErrorMessage = "An internal error occurred during voice generation execution.";
            job.CompletedAtUtc = DateTime.UtcNow;
        }

        dbContext.JobAttempts.Add(attempt);
        job.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string ExtractNormalizedNarration(Script? script, ScriptVersion? version)
    {
        if (version != null && !string.IsNullOrWhiteSpace(version.SnapshotJson) && version.SnapshotJson != "{}")
        {
            try
            {
                using var doc = JsonDocument.Parse(version.SnapshotJson);
                if (doc.RootElement.TryGetProperty("Scenes", out var scenesEl) || doc.RootElement.TryGetProperty("scenes", out scenesEl))
                {
                    var parsedScenes = new List<(int OrderIndex, string Narration)>();
                    foreach (var s in scenesEl.EnumerateArray())
                    {
                        var order = s.TryGetProperty("OrderIndex", out var o) ? o.GetInt32() : (s.TryGetProperty("orderIndex", out o) ? o.GetInt32() : 0);
                        var text = s.TryGetProperty("NarrationText", out var n) ? n.GetString() : (s.TryGetProperty("narrationText", out n) ? n.GetString() : null);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            parsedScenes.Add((order, text.Trim()));
                        }
                    }
                    if (parsedScenes.Count > 0)
                    {
                        return string.Join("\n\n", parsedScenes.OrderBy(p => p.OrderIndex).Select(p => p.Narration));
                    }
                }
            }
            catch
            {
                // Fallback to live script.Scenes
            }
        }

        if (script == null) return string.Empty;
        var scenes = script.Scenes.OrderBy(s => s.OrderIndex).ToList();
        if (scenes.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        foreach (var scene in scenes)
        {
            if (!string.IsNullOrWhiteSpace(scene.NarrationText))
            {
                if (sb.Length > 0) sb.Append("\n\n");
                sb.Append(scene.NarrationText.Trim());
            }
        }

        return sb.ToString();
    }

    private static string ClassifyTimingDeviation(double deviationSeconds, TimingTolerancePolicy policy)
    {
        var abs = Math.Abs(deviationSeconds);
        if (abs <= policy.NoticeThresholdSeconds) return "Normal";
        if (abs <= policy.WarningThresholdSeconds) return "Notice";
        return "Warning";
    }

    private static string ComputeSha256(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexStringLower(bytes);
    }

    private static string SerializeVoiceProfileCanonical(VoiceProfile profile)
    {
        var hints = profile.PronunciationHints ?? [];
        var canonicalHints = string.Join(",", hints.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{JsonSerializer.Serialize(pair.Key)}:{JsonSerializer.Serialize(pair.Value)}"));
        return $"{{\"profileKey\":{JsonSerializer.Serialize(profile.ProfileKey)},\"profileName\":{JsonSerializer.Serialize(profile.ProfileName)},\"language\":{JsonSerializer.Serialize(profile.Language)},\"configuredLocale\":{JsonSerializer.Serialize(profile.ConfiguredLocale)},\"speakingStyle\":{JsonSerializer.Serialize(profile.SpeakingStyle)},\"tone\":{JsonSerializer.Serialize(profile.Tone)},\"warmth\":{JsonSerializer.Serialize(profile.Warmth)},\"energy\":{JsonSerializer.Serialize(profile.Energy)},\"pace\":{JsonSerializer.Serialize(profile.Pace)},\"pronunciationHints\":{{{canonicalHints}}}}}";
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

    private static JobDto MapJobForEditorialOverview(Job job) => MapJobToDto(job) with
    {
        CorrelationId = string.Empty,
        ErrorCode = null,
        SanitizedErrorMessage = null,
        Attempts = []
    };

    private static GeneratedAssetDto MapGeneratedAssetToDto(GeneratedAsset asset, Guid activeStoryboardVersionId, bool isStoryboardActive)
    {
        var isEligible = isStoryboardActive &&
                         asset.Status == GeneratedAssetStatus.Approved &&
                         asset.IsSelectedForAssembly &&
                         asset.StoryboardVersionId == activeStoryboardVersionId;

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
