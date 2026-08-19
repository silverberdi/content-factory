## Context

Content Factory enforces a strict separation between editorial planning and production execution. Under `storyboard-production-planning`, `Storyboard`, `StoryboardFrame`, `AssetPlan`, and `AssetRequirement` define WHAT media needs to be produced using provider-neutral editorial descriptors (prompts, framing intents, style intents, aspect ratio "9:16", and frame durations).

This change introduces the HOW layer for visual media (`AiImage`, `AiVideo`, `BRoll`, `GraphicOverlay`). It transitions approved visual requirements into asynchronous execution units (`Job`), delegates generation to a provider-neutral adapter (`IVisualGenerationProvider`), stores produced media binaries in existing MinIO object storage, captures immutable metadata in `GeneratedAsset`, and routes candidates to human editorial operators for visual review and selection.

## Goals / Non-Goals

**Goals:**
- Implement a unified canonical `Job` domain model and database-backed hosted queue worker (`VisualGenerationBackgroundWorker`) for asynchronous visual generation.
- Implement the `IVisualGenerationProvider` adapter boundary with two concrete implementations:
  1. `ComfyVisualGenerationProvider`: Maps provider-neutral visual requirements to configured ComfyUI workflows and streams generated outputs.
  2. `MockVisualGenerationProvider`: Deterministic local development adapter generating valid visual placeholder fixtures and simulating transient or permanent failures.
- Integrate MinIO runtime object storage using deterministic, non-leaking object key conventions with SHA-256 integrity checks and authenticated media streaming endpoints.
- Model `GeneratedAsset` with immutable lineage (`ContentItemId`, `ChannelId`, `StoryboardId`, `StoryboardVersionId`, `AssetRequirementId`, `JobId`), technical metadata (dimensions, file size, duration, checksum), and human QA state (`PendingReview`, `Approved`, `Rejected`).
- Support multi-candidate generation (1 to 4 variants per requirement) with atomic single-candidate assembly selection (`IsSelectedForAssembly`).
- Enforce strict pre-dispatch eligibility and upstream staleness gating: never generate from unapproved/stale storyboards; preserve historical generated assets upon upstream revisions while marking them ineligible for final assembly.
- Enforce error classification (`retryable-transient`, `action-required`, `non-retryable-input`) with bounded exponential retries and sanitized error messages.
- Deliver an Angular 21 full-width Visual Asset Production Studio inside Content Detail for high-density candidate comparison, zoom preview, and human QA decision-making.

**Non-Goals:**
- No TTS voice synthesis, audio generation, music, sound effects, or audio mixing (deferred to audio generation changes).
- No subtitle rendering or burning into media.
- No final video timeline assembly or video rendering (deferred to assembly engine change).
- No publication or platform distribution.
- No new message brokers (RabbitMQ, Kafka, Redis) or additional database/MinIO instances.
- No generic Digital Asset Management (DAM) system.

## Decisions

### 1. Canonical Job Domain Model & Database-Backed Atomic Claiming

**Decision:** Implement the canonical `Job` aggregate directly in EF Core PostgreSQL, processed by an ASP.NET Core `IHostedService` background queue worker (`VisualGenerationBackgroundWorker`).

```csharp
public class Job
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContentItemId { get; set; }
    public Guid ChannelId { get; set; }
    public string JobType { get; set; } = "generate_visual_asset";
    public string Capability { get; set; } = "generate_visual_asset";
    public Guid? SourceAssetRequirementId { get; set; }
    public Guid? StoryboardId { get; set; }
    public Guid? StoryboardVersionId { get; set; }
    public int GenerationRevision { get; set; } = 1;
    public string Status { get; set; } = JobStatus.Queued;
    public string Provider { get; set; } = string.Empty;
    public string ModelOrWorkflowIdentifier { get; set; } = string.Empty;
    public int AttemptCount { get; set; } = 0;
    public int MaxAttempts { get; set; } = 3;
    public int AutomaticRetriesRemaining { get; set; } = 2;
    public int CandidateCount { get; set; } = 1;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public long DurationMs { get; set; }
    public decimal? EstimatedCostUsd { get; set; }
    public decimal? ActualCostUsd { get; set; }
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
    public string? ErrorCode { get; set; }
    public string? SanitizedErrorMessage { get; set; }
    public bool IsRetryable { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string CreatedByEmail { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<JobAttempt> Attempts { get; set; } = [];
}
```

*Atomic Database Job Claiming Invariant:*
The background worker acquires `Queued` jobs atomically without external brokers (RabbitMQ/Redis/Kafka) using PostgreSQL/EF Core conditional update semantics:
```csharp
var affected = await dbContext.Jobs
    .Where(j => j.Id == candidate.Id && j.Status == JobStatus.Queued)
    .ExecuteUpdateAsync(s => s
        .SetProperty(j => j.Status, JobStatus.Running)
        .SetProperty(j => j.StartedAtUtc, now)
        .SetProperty(j => j.UpdatedAtUtc, now), cancellationToken);
```
If multiple concurrent worker threads/processes attempt to claim the same job, exactly one receives `affected == 1` and proceeds with execution, while all others receive `affected == 0`.

*Job Attempt History Invariant:*
`JobAttempt` records are immutable evidence of past execution. Retries (whether automatic retry from transient failures or manual retry by an operator) MUST NEVER overwrite or erase prior `JobAttempt` records. Retries increment `AttemptNumber = job.Attempts.Count + 1` and preserve the full historical attempt trajectory.

*Alternatives Considered:*
- *External Message Broker (RabbitMQ/Redis)*: Rejected as speculative infrastructure violating canonical modular monolith guidelines.
- *In-memory Channel without DB persistence*: Rejected because jobs must survive application restarts and provide auditability.

### 2. Provider Adapter Architecture (`IVisualGenerationProvider`)

**Decision:** Create a clean adapter interface `IVisualGenerationProvider` that accepts a provider-neutral `VisualGenerationRequest`, declares `SupportedAssetTypes`, and returns a `VisualGenerationResult`.

```csharp
public interface IVisualGenerationProvider
{
    string ProviderName { get; }
    IReadOnlyList<string> SupportedAssetTypes { get; }
    Task<VisualGenerationResult> GenerateVisualAssetAsync(
        VisualGenerationRequest request,
        CancellationToken cancellationToken = default);
}

public record VisualGenerationRequest(
    Guid JobId,
    string CorrelationId,
    Guid ContentItemId,
    Guid ChannelId,
    Guid StoryboardId,
    Guid StoryboardVersionId,
    Guid AssetRequirementId,
    string AssetType,
    string AspectRatio,
    int TargetWidth,
    int TargetHeight,
    double? TargetDurationSeconds,
    string VisualPrompt,
    string NegativePrompt,
    string StyleIntent,
    string MotionIntent,
    int CandidateCount
);
```

*Capability Validation Boundary:*
Providers explicitly declare supported asset types (e.g. `Mock` supports `[AiImage, AiVideo, GraphicOverlay]`, `Comfy` supports `[AiImage]`). Dispatching non-visual or unsupported types is rejected before dispatch.

### 3. MinIO Storage Service & Key Structure

**Decision:** Store all generated media in the canonical MinIO bucket (`content-factory-assets`) under deterministic hierarchical keys:

```
content-factory/{environment}/channels/{channelId}/content/{contentItemId}/storyboard/{storyboardVersionId}/visual/{assetRequirementId}/{generatedAssetId}.{ext}
```

*Key Properties & Retention Boundaries:*
- Deterministic, traceable to exact storyboard version and asset requirement.
- Zero secret credentials or sensitive tokens in paths, telemetry, or parameters snapshots.
- Authenticated proxy streaming endpoints (`/api/generated-assets/{id}/stream` and `/api/generated-assets/{id}/thumbnail`) ensure MinIO buckets remain private and unexposed.
- **Binary Retention Semantics**: Metadata in PostgreSQL and lineage records are permanent audit records. Raw binary media in MinIO is subject to configurable storage retention policies: active and approved assets are preserved while required, but rejected or intermediate candidate binaries are not mandated to be kept permanently forever.

### 4. Generated Asset Domain & Human Visual QA Gate

**Decision:** Persist media results in a new `GeneratedAsset` entity and enforce explicit human QA review (`PendingReview` -> `Approved` / `Rejected`).

```csharp
public class GeneratedAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContentItemId { get; set; }
    public Guid ChannelId { get; set; }
    public Guid StoryboardId { get; set; }
    public Guid StoryboardVersionId { get; set; }
    public Guid AssetRequirementId { get; set; }
    public Guid JobId { get; set; }
    public int VariantIndex { get; set; } = 1;
    public string AssetType { get; set; } = string.Empty;
    public string MediaType { get; set; } = "Image";
    public string StorageProvider { get; set; } = "MinIO";
    public string StorageKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = "image/png";
    public long FileSizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public double? DurationSeconds { get; set; }
    public string ChecksumSha256 { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ProviderModelOrWorkflow { get; set; } = string.Empty;
    public string GenerationParametersSnapshot { get; set; } = "{}";
    public string Status { get; set; } = GeneratedAssetStatus.PendingReview;
    public string? RejectionReason { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewedByEmail { get; set; }
    public bool IsSelectedForAssembly { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
```

*Invariants:*
- Generated assets NEVER become automatically approved or selected.
- Exactly ONE candidate can have `IsSelectedForAssembly = true` per `AssetRequirementId` at any given time. Approving or selecting a candidate atomically clears `IsSelectedForAssembly` on sibling candidates.
- Rejecting a candidate requires a non-empty `RejectionReason`.
- Candidate reviews support optimistic concurrency guards via `ExpectedStatus`.

### 5. Upstream Staleness & Lineage Gating

**Decision:** Dynamic evaluation of asset eligibility based on current storyboard lineage.

When evaluating whether a `GeneratedAsset` is eligible for downstream assembly:
`IsEligibleForAssembly = (asset.Status == Approved && asset.IsSelectedForAssembly && storyboard.IsCurrent && !storyboard.IsStale && storyboard.VersionId == asset.StoryboardVersionId)`

If a storyboard is revised or reconciled to a new version, old `GeneratedAsset` records remain preserved in PostgreSQL and MinIO for audit history, but `IsEligibleForAssembly` dynamically resolves to `false`.

### 6. Job / Batch Idempotency & Bounded Retry Strategy

**Decision:** A visual generation `Job` produces a generation BATCH (1, 2, or 4 candidates). Therefore, Job idempotency identifies the generation BATCH intent, not an individual `VariantIndex`.

`VariantIndex` belongs to each individual `GeneratedAsset` output, NOT `Job` identity.

The canonical batch idempotency key is computed as:
```
ConfigFingerprint = SHA256($"{VisualPrompt}:{NegativePrompt}:{StyleIntent}:{MotionIntent}:{AspectRatio}:{ProviderName}")
IdempotencyKey = SHA256($"{StoryboardVersionId}:{AssetRequirementId}:{CandidateCount}:{GenerationRevision}:{ConfigFingerprint}")
```

- **Accidental Duplicate Dispatch Guard**: If an active (`Queued` or `Running`) Job exists with the same idempotency key, dispatch returns/reuses the existing active Job.
- **Intentional "Regenerate"**: Creates an explicit new `GenerationRevision` (e.g. `Revision = maxRevision + 1`), ensuring distinct batch identity and preserving historical generated candidates.
- **Error Classification**:
  - `retryable-transient`: Automatic bounded retry with exponential backoff up to `MaxAttempts` (3) using `AutomaticRetriesRemaining`.
  - `action-required`: Immediate halt in `FailedActionRequired` with sanitized error message for operator attention.
  - `non-retryable-input`: Rejected immediately with HTTP 400.

### 7. Authorization Matrix

| Operation | Required Role | Non-Authorized Response |
| :--- | :--- | :--- |
| Dispatch Generation Job | `EDITORIAL` (or GOD mode) | HTTP 403 Forbidden |
| Review Candidate (Approve / Reject) | `EDITORIAL` (or GOD mode) | HTTP 403 Forbidden |
| Select Candidate for Assembly | `EDITORIAL` (or GOD mode) | HTTP 403 Forbidden |
| View Media Stream / Thumbnail | `EDITORIAL` or `TECHNICAL` | HTTP 401 / 403 |
| Inspect Technical Telemetry / Payloads | `TECHNICAL` (or GOD mode) | HTTP 403 Forbidden |
| Retry Failed Job | `TECHNICAL` (or GOD mode) | HTTP 403 Forbidden |

### 8. Frontend Composition: Angular 21 Visual Asset Production Studio

**Decision:** Integrate the Visual Production Studio directly inside the Content Detail view (`features/content/visual-asset-studio/`) with full-width layout:
- Adheres to canonical full-width layout contract (100% width on desktop >=1280px without `max-w-7xl` containers).
- Component decomposition:
  - `VisualAssetStudioComponent`: Main container, batch progress bar, requirement cards list.
  - `VisualRequirementCardComponent`: Frame context, prompt preview, generation trigger, candidate grid.
  - `VisualCandidateCardComponent`: 9:16 thumbnail preview, review status badge, assembly selector, quick approve/reject buttons.
  - `VisualCandidatePreviewModalComponent`: High-res zoom preview, side-by-side variant comparison, metadata drawer, rejection modal.
  - `JobDiagnosticsDrawerComponent`: Technical job execution telemetry, attempt logs, retry trigger.

## Risks / Trade-offs

- **[Risk] Long generation durations blocking HTTP calls** → *Mitigation:* Pure asynchronous execution via `Job` entity, HTTP `202 Accepted`, and database-backed hosted queue worker with reactive polling/status endpoints.
- **[Risk] Offline development blocked by lack of local ComfyUI GPU environment** → *Mitigation:* `MockVisualGenerationProvider` provides deterministic SVG/PNG placeholder generation, realistic telemetry, and deterministic failure simulation.
- **[Risk] Storage growth in MinIO** → *Mitigation:* Deterministic object keys structured by channel, content item, and storyboard version allow future retention and lifecycle policies.
- **[Risk] Multiple candidates created without editorial control** → *Mitigation:* Mandatory human QA gate (`PendingReview` -> `Approved` / `Rejected`) ensures no AI output automatically reaches assembly.

## Migration Plan

1. Create EF Core migration (`AddVisualAssetGenerationJobAndMediaTables`) adding `jobs`, `job_attempts`, and `generated_assets` tables with appropriate indexes on `content_item_id`, `storyboard_id`, `storyboard_version_id`, `asset_requirement_id`, `status`, and `idempotency_key`.
2. Seed initial visual generation routing configuration (`Comfy` with fallback to `Mock` in development).
3. Verify MinIO bucket connectivity (`content-factory-assets`).
4. Rollback: Drop newly created tables and migration record; domain planning entities remain unaffected.

## Open Questions

None. All technical boundaries and domain requirements are resolved.
