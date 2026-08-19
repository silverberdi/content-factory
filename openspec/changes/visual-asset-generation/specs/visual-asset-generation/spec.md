## Purpose

Defines the asynchronous visual asset generation capability (`generate_visual_asset`) that executes approved, provider-neutral visual `AssetRequirement` specifications from the active `Storyboard` into real media artifacts. Specifies the canonical `Job` domain and execution model, the provider-neutral `IVisualGenerationProvider` adapter boundary (supporting ComfyUI workflows and deterministic local development mock), MinIO runtime object storage conventions, immutable `GeneratedAsset` metadata and lineage tracking, multi-candidate variant generation, mandatory human visual QA review (`PendingReview`, `Approved`, `Rejected`), upstream staleness gating, bounded idempotent retries with error categorization, backend authorization (`EDITORIAL` vs `TECHNICAL`), and the Angular 21 Visual Asset Production Studio.

## ADDED Requirements

### Requirement: Canonical Job domain and asynchronous execution model

The system SHALL model all long-running asynchronous production operations using a unified `Job` aggregate. A `Job` SHALL record:
- `Id` (`Guid`, unique identifier);
- `ContentItemId` and `ChannelId` (parent operational thread);
- `JobType` / capability (e.g. `generate_visual_asset`);
- `SourceAssetRequirementId` (`Guid?`, reference to the originating asset requirement);
- `StoryboardId` and `StoryboardVersionId` (`Guid?`, exact authorizing lineage);
- `GenerationRevision` (`int`, 1-based revision counter distinguishing regeneration cycles);
- `Status` (`Queued`, `Running`, `Succeeded`, `FailedRetryable`, `FailedActionRequired`, `Cancelled`);
- `Provider` (e.g. `Comfy`, `Mock`);
- `ModelOrWorkflowIdentifier` (configured workflow template or checkpoint name);
- `AttemptCount` (`int`, total executed attempts);
- `MaxAttempts` (`int`, default 3);
- `AutomaticRetriesRemaining` (`int`, default 2);
- `CandidateCount` (`int`, 1, 2, or 4 variants);
- `StartedAtUtc`, `CompletedAtUtc`, and `DurationMs`;
- `EstimatedCostUsd` and `ActualCostUsd` (`decimal?`);
- `CorrelationId` (`string`, for distributed tracing);
- `ErrorCode` (`string?`, normalized failure category);
- `SanitizedErrorMessage` (`string?`, user-safe error description without credentials or stack traces);
- `IsRetryable` (`bool`);
- `IdempotencyKey` (`string`, SHA-256 batch generation intent hash);
- `CreatedByEmail`, `CreatedAtUtc` and `UpdatedAtUtc`.

Production generation requests SHALL be strictly non-blocking: the API endpoint SHALL validate eligibility, compute the batch idempotency key, create and persist the `Job` in status `Queued`, enqueue it for execution, and return HTTP `202 Accepted` with the `Job` representation. The system SHALL NOT maintain open HTTP connections while awaiting media synthesis. The background execution SHALL be processed by an asynchronous hosted queue worker (`VisualGenerationBackgroundWorker`) backed by the database using atomic claiming (`UPDATE ... WHERE Id = candidate.Id AND Status = 'Queued'`).

#### Scenario: Dispatch asynchronous visual generation job
- **WHEN** an operator requests generation for an approved visual AssetRequirement
- **THEN** the system validates eligibility, derives canonical GenerationRevision, computes the batch idempotency key, and persists a `Job` in status "Queued"
- **AND** the API immediately returns HTTP 202 Accepted containing the Job DTO and correlation ID
- **AND** the background worker claims the Job atomically, transitions it to "Running", and dispatches execution to the configured provider adapter.

#### Scenario: Accidental duplicate dispatch reuses active Job
- **WHEN** an operator or client accidentally dispatches the same requirement and candidate count while a Job is already "Queued" or "Running"
- **THEN** the system identifies the matching batch idempotency key and returns the existing active Job without queueing duplicate work.

#### Scenario: Intentional regeneration creates new revision
- **WHEN** an operator intentionally requests "Regenerate" after a previous attempt completes
- **THEN** the system creates a new explicit `GenerationRevision` (e.g. revision 2) with a distinct idempotency key, allowing a fresh generation cycle while preserving historical candidates.

#### Scenario: Prevent competing async job abstractions
- **WHEN** any visual asset generation task is scheduled
- **THEN** it uses the unified canonical `Job` entity and database schema
- **AND** no second competing async-execution table or ad-hoc background tracker is created.

### Requirement: Pre-dispatch eligibility and upstream lineage revalidation

The system SHALL revalidate production eligibility immediately before creating and dispatching a visual generation `Job`. Generation SHALL be permitted IF AND ONLY IF:
1. The parent `ContentItem` has a current active `Storyboard` (`IsCurrent == true`);
2. The `Storyboard` is in status `Approved`;
3. The `Storyboard` is NOT stale (`IsStale == false`);
4. The embedded `AssetPlan` is in status `ReadyForGeneration`;
5. The specified `AssetRequirement` belongs to the approved `Storyboard` and is of a supported visual type (`AiImage`, `AiVideo`, `BRoll`, `GraphicOverlay`); and
6. The upstream `Script` and `TruthSource` lineages remain approved and active.

If any condition is violated, the system SHALL reject the dispatch request with HTTP 400 Bad Request or HTTP 409 Conflict, returning a structured actionable blocker message, and SHALL NOT create a `Job` or execute provider workflows against stale or unapproved planning intent.

#### Scenario: Generation dispatch succeeds for approved non-stale Storyboard
- **WHEN** an operator dispatches visual generation for an `AiImage` requirement on a ContentItem with `Storyboard.Status == Approved` and `Storyboard.IsStale == false`
- **THEN** pre-dispatch validation passes and the generation Job is created and queued.

#### Scenario: Generation dispatch blocked when Storyboard is unapproved
- **WHEN** an operator attempts to dispatch visual generation for an AssetRequirement on a Storyboard in "Draft" or "UnderReview"
- **THEN** the backend rejects the request with HTTP 400 Bad Request and error code "UNAPPROVED_STORYBOARD_BLOCKED"
- **AND** no Job record is created and no provider execution occurs.

#### Scenario: Generation dispatch blocked when upstream lineage is stale
- **WHEN** an operator attempts to dispatch visual generation on a Storyboard whose `IsStale` evaluates to true (e.g. because the Script version was revised)
- **THEN** the backend rejects the request with HTTP 409 Conflict and message "Cannot generate media from a stale Storyboard. Reconcile and approve the Storyboard first."
- **AND** no Job is queued.

### Requirement: Provider-neutral visual generation adapter boundary (`IVisualGenerationProvider`)

The media generation architecture SHALL isolate editorial planning from provider-specific execution graphs through an `IVisualGenerationProvider` abstraction. The service SHALL accept a provider-neutral generation request containing:
- `JobId` and `CorrelationId`;
- `AssetRequirementId`, `AssetType`, `AspectRatio` (e.g. "9:16"), target dimensions (e.g. 1080x1920), target duration in seconds (for video);
- `VisualPrompt`, `NegativePrompt`, `StyleIntent`, and `MotionIntent`;
- `CandidateCount` (1, 2, or 4);
- `ChannelId` and operational context.

The provider adapter SHALL be responsible for translating these neutral fields into provider-specific API payloads, executing or polling the provider, streaming or downloading produced binary streams, and returning standardized execution results. Editorial entities (`Storyboard`, `AssetPlan`, `AssetRequirement`) and domain models SHALL NOT store provider node graphs, ComfyUI workflow JSON, sampler settings, scheduler names, checkpoint hashes, or raw execution payloads.

#### Scenario: Provider adapter translates neutral request to technical execution
- **WHEN** the generation service dispatches an `AiImage` requirement with `VisualPrompt: "Cyberpunk neon street"` and `AspectRatio: "9:16"` to `IVisualGenerationProvider`
- **THEN** the active provider adapter translates the request into its technical payload (e.g. mapping 9:16 to 1080x1920, selecting the configured SDXL/Flux checkpoint and sampler)
- **AND** the originating `AssetRequirement` record in the database remains unmodified and free of provider-specific parameters.

### Requirement: ComfyUI visual provider adapter

The system SHALL include a `ComfyVisualGenerationProvider` implementing `IVisualGenerationProvider` for ComfyUI / Comfy Cloud execution. The Comfy adapter SHALL:
1. Load workflow templates by capability, asset type (`AiImage` vs `AiVideo`), and optional channel override from configuration (`appsettings.json` / environment);
2. Inject dynamic prompts (`VisualPrompt`, `NegativePrompt`), aspect ratio dimensions, random seeds, and duration frames into mapped node inputs;
3. Submit the prompt execution graph via Comfy API (`/prompt`);
4. Track execution status (`/history/{prompt_id}` or WebSocket) and handle provider progress;
5. Retrieve generated image/video binaries from Comfy output endpoints (`/view`);
6. Capture provider execution telemetry (model, checkpoint, sampling steps, generation duration, estimated cost if available).

Workflow templates and node identifiers SHALL reside exclusively in configuration files or technical service layers, never in the editorial domain.

#### Scenario: Comfy provider generates image and returns binary stream
- **WHEN** `ComfyVisualGenerationProvider` processes a valid visual generation request
- **THEN** it populates the configured image workflow template with prompt, negative prompt, and 1080x1920 dimensions
- **AND** posts the prompt to ComfyUI, retrieves the resulting image bytes upon completion, and returns a successful `VisualGenerationResult` containing output streams and telemetry.

### Requirement: Deterministic development mock visual provider

The system SHALL provide a `MockVisualGenerationProvider` for local development, testing, and offline environments where live ComfyUI instances are unavailable. The mock provider SHALL:
1. Accept generation requests without requiring external network connectivity;
2. Simulate realistic asynchronous progression with configurable delay (e.g. 500ms - 2000ms);
3. Deterministically generate valid SVG or PNG placeholder image streams incorporating the frame sequence number, asset type, aspect ratio ("9:16"), visual style, and timestamp;
4. Support deterministic test failure triggers via prompt tokens (e.g. `[mock:retryable-failure]`, `[mock:action-required-failure]`, `[mock:timeout]`) to validate retry loops and error handling;
5. Populate realistic telemetry (mock duration, mock cost $0.002, mock provider "MockComfy").

Mock mode SHALL be clearly indicated in diagnostics and `Job.Provider` ("Mock") so synthetic assets are never mistaken for production outputs.

#### Scenario: Mock provider generates valid visual candidate in development
- **WHEN** running in development mode and visual generation is triggered for an AssetRequirement
- **THEN** `MockVisualGenerationProvider` processes the job, simulates asynchronous progress, and outputs a valid 1080x1920 image binary stream
- **AND** the Job succeeds with provider identifier "Mock" and realistic duration/cost telemetry.

#### Scenario: Deterministic simulation of retryable provider error
- **WHEN** visual generation is dispatched with prompt containing "[mock:retryable-failure]"
- **THEN** `MockVisualGenerationProvider` simulates a transient HTTP 503 Provider Unavailable error
- **AND** the Job transitions to `FailedRetryable` with normalized error code "PROVIDER_TRANSIENT_503".

### Requirement: MinIO runtime object storage and deterministic key convention

Generated media binaries (images, videos) SHALL be stored in the canonical MinIO object storage instance (`content-factory-assets` bucket). The system SHALL NOT store raw media binaries in PostgreSQL.

The system SHALL enforce a deterministic, safe, hierarchical object key structure:
`content-factory/{environment}/channels/{channelId}/content/{contentItemId}/storyboard/{storyboardVersionId}/visual/{assetRequirementId}/{generatedAssetId}.{ext}`

Storage operations SHALL:
1. Verify MIME content type (e.g. `image/png`, `image/webp`, `video/mp4`);
2. Calculate and verify SHA-256 checksum during upload;
3. Record object file size in bytes;
4. Ensure buckets and objects are private by default;
5. Expose media to authenticated frontend clients via secure backend streaming proxy endpoints (`GET /api/generated-assets/{id}/stream` or `GET /api/generated-assets/{id}/thumbnail`) or time-limited pre-signed URLs without leaking MinIO master credentials.

#### Scenario: Binary uploaded to MinIO with deterministic object path
- **WHEN** a provider adapter completes generation of an image binary
- **THEN** the storage service streams the binary to MinIO under key `content-factory/development/channels/{channelId}/content/{contentItemId}/storyboard/{storyboardVersionId}/visual/{assetRequirementId}/{generatedAssetId}.png`
- **AND** computes SHA-256 hash and verifies exact file size in bytes
- **AND** persists the object key and metadata in the `GeneratedAsset` entity.

#### Scenario: Media streaming proxy enforces authentication
- **WHEN** an authenticated operator requests a preview thumbnail or media stream for a GeneratedAsset
- **THEN** the backend streams the media from MinIO through the authorized endpoint with correct Content-Type and caching headers
- **AND** non-authenticated requests are rejected with HTTP 401 Unauthorized.

### Requirement: `GeneratedAsset` entity and immutable lineage tracking

The system SHALL model produced media artifacts as `GeneratedAsset` entities. A `GeneratedAsset` SHALL record:
- `Id` (`Guid`, unique identifier);
- `ContentItemId` and `ChannelId`;
- `StoryboardId` and `StoryboardVersionId` (authorizing planning snapshot);
- `AssetRequirementId` (authorizing requirement);
- `JobId` (originating execution job);
- `VariantIndex` (`int`, 1-based index when generating multiple candidate variants);
- `AssetType` (`AiImage`, `AiVideo`, `BRoll`, `GraphicOverlay`);
- `MediaType` (`Image`, `Video`);
- `StorageProvider` ("MinIO");
- `StorageKey` (deterministic object storage path);
- `ContentType` (e.g. `image/png`, `image/webp`, `video/mp4`);
- `FileSizeBytes` (`long`);
- `Width` and `Height` (`int?`, e.g. 1080 x 1920);
- `DurationSeconds` (`double?`, for video assets);
- `ChecksumSha256` (`string`);
- `Provider` (e.g. "Comfy", "Mock");
- `ProviderModelOrWorkflow` (`string`);
- `GenerationParametersSnapshot` (`string`, JSON snapshot of prompts, seeds, sampler, aspect ratio);
- `Status` (`PendingReview`, `Approved`, `Rejected`);
- `RejectionReason` (`string?`);
- `ReviewedAtUtc` and `ReviewedByEmail` (`string?`);
- `IsSelectedForAssembly` (`bool`, indicates if this candidate is the active selection for final render assembly);
- `CreatedAtUtc` and `UpdatedAtUtc`.

Lineage identifiers (`ContentItemId`, `StoryboardId`, `StoryboardVersionId`, `AssetRequirementId`, `JobId`) SHALL be immutable once written.

#### Scenario: GeneratedAsset created upon successful Job execution
- **WHEN** a generation Job finishes successfully
- **THEN** a `GeneratedAsset` record is persisted in status "PendingReview" with `IsSelectedForAssembly: false`
- **AND** all lineage IDs, MinIO storage keys, dimensions, checksums, and generation parameters are permanently recorded.

### Requirement: Generation variants and candidate batching

The system SHALL support generating multiple candidate variants (1, 2, or 4 candidates) for a single `AssetRequirement` in a single generation request. Each output SHALL be created as an independent `GeneratedAsset` record linked to the same `AssetRequirementId` and batch `JobId`, with unique `VariantIndex` (1, 2, ...).

An `AssetRequirement` MAY accumulate multiple historical candidates over successive generation attempts.

#### Scenario: Multi-candidate generation produces distinct GeneratedAsset records
- **WHEN** an operator requests 2 candidate variants for an AssetRequirement
- **THEN** the system generates 2 candidate media outputs
- **AND** creates 2 distinct `GeneratedAsset` records sharing the same `AssetRequirementId` and `JobId`, with `VariantIndex: 1` and `VariantIndex: 2`
- **AND** both are set to status "PendingReview".

### Requirement: Mandatory human visual QA review and authoritative selection

No generated media asset SHALL become automatically approved or selected for downstream video assembly. The system SHALL require explicit human visual QA review for every `GeneratedAsset`:
1. `PendingReview` -> `Approved`: An operator with `EDITORIAL` role reviews the candidate preview and approves it. The system marks `Status = Approved`, records `ReviewedAtUtc` and `ReviewedByEmail`, and sets `IsSelectedForAssembly = true`. Atomically, any previously approved candidate for the same `AssetRequirementId` has `IsSelectedForAssembly` set to `false`.
2. `PendingReview` -> `Rejected`: An operator with `EDITORIAL` role rejects the candidate, providing a mandatory non-empty `RejectionReason`. The system marks `Status = Rejected`, records `RejectionReason`, `ReviewedAtUtc`, and `ReviewedByEmail`, and ensures `IsSelectedForAssembly = false`.
3. `Select Candidate`: An operator can explicitly switch the active assembly candidate among approved variants for a given requirement (`IsSelectedForAssembly = true`), updating parent selection atomically.

#### Scenario: Human editor approves candidate for assembly
- **WHEN** an operator with EDITORIAL role approves a candidate in "PendingReview"
- **THEN** the `GeneratedAsset` status transitions to "Approved" with reviewer email and timestamp recorded
- **AND** `IsSelectedForAssembly` becomes true
- **AND** any other candidate for that AssetRequirement has `IsSelectedForAssembly` set to false
- **AND** an audit event is logged with action "GeneratedAsset.Approved".

#### Scenario: Reject candidate requires mandatory reason
- **WHEN** an operator with EDITORIAL role rejects a candidate providing reason "Visual prompt hallucinated distorted hands"
- **THEN** the `GeneratedAsset` status transitions to "Rejected" with the rejection reason recorded
- **AND** `IsSelectedForAssembly` is false.

#### Scenario: Reject candidate without reason fails validation
- **WHEN** an operator attempts to reject a candidate with an empty reason string
- **THEN** the backend rejects the request with HTTP 400 Bad Request and message "Rejection reason is required".

### Requirement: Upstream lineage invalidation and stale asset preservation

If an upstream `Script` or `Storyboard` is modified, regenerated, reopened, or superseded by a newer approved version:
1. All existing `GeneratedAsset` metadata records in PostgreSQL SHALL be preserved permanently for audit, compliance, and lineage inspection; raw media binaries in object storage SHALL be managed according to storage retention policies (active and approved assets retained while required, intermediate candidate binaries subject to configurable retention);
2. Historical `GeneratedAsset` records SHALL dynamically evaluate `IsEligibleForAssembly = false` because their `StoryboardVersionId` does not match the active current approved `StoryboardVersionId`;
3. Downstream assembly and video render pipelines SHALL refuse to consume generated assets associated with superseded or stale storyboards.

Lineage identifiers on existing `GeneratedAsset` records SHALL NEVER be overwritten to retroactively point to newer storyboard versions.

#### Scenario: Upstream storyboard update invalidates assembly eligibility but preserves assets
- **WHEN** a Storyboard is reconciled to version 2 after script changes
- **THEN** existing `GeneratedAsset` records linked to Storyboard version 1 remain in the database and MinIO intact
- **AND** queries evaluating assembly eligibility return `IsEligibleForAssembly: false` for version 1 assets
- **AND** the operator is prompted to generate fresh visual assets for the version 2 AssetRequirements.

### Requirement: Error classification, sanitized telemetry, and bounded retries

The system SHALL classify all generation failures into canonical failure categories:
1. `retryable-transient`: Provider timeouts, HTTP 502/503/504, temporary network dropouts, provider rate limits.
2. `action-required`: Invalid workflow configuration, authentication/credential failures, unsupported model/resolution, permanent provider 4xx rejections.
3. `non-retryable-input`: Malformed request payloads, missing prompts, invalid aspect ratios rejected before queueing.

Resilience rules:
- When a `retryable-transient` failure occurs during execution, the background worker SHALL decrement `AutomaticRetriesRemaining`, increment attempt counter, and retry with exponential backoff up to `MaxAttempts` (default 3).
- If automatic retries are exhausted, the Job status transitions to `FailedRetryable` or `FailedActionRequired`.
- When an `action-required` failure occurs, the Job immediately transitions to `FailedActionRequired` without automatic retry.
- All stored error messages (`SanitizedErrorMessage`) SHALL be sanitized to exclude API tokens, passwords, private internal URLs, or raw stack traces.
- Prior `JobAttempt` records SHALL NEVER be overwritten or erased upon automatic or manual retry; each attempt is preserved as immutable execution evidence.
- Operators with `TECHNICAL` role SHALL be able to manually trigger a retry on `FailedRetryable` and `FailedActionRequired` jobs once underlying issues are corrected, which resets the automatic retry budget without deleting prior attempt records.

#### Scenario: Transient provider error triggers automatic bounded retry
- **WHEN** a generation job encounters a temporary provider connection timeout on attempt 1
- **THEN** the worker logs attempt 1, decrements `AutomaticRetriesRemaining`, and schedules a retry
- **AND** if attempt 2 succeeds, attempt 2 is recorded alongside attempt 1, and the Job status transitions to "Succeeded".

#### Scenario: Action-required failure halts immediately with sanitized error
- **WHEN** a generation job fails due to an invalid Comfy workflow node configuration
- **THEN** the Job transitions to `FailedActionRequired` with `ErrorCode: "INVALID_WORKFLOW_CONFIGURATION"`
- **AND** `SanitizedErrorMessage` displays a user-safe message "Configured visual workflow template contains invalid node definitions. Technical review required."
- **AND** no automatic retry loop is initiated.

#### Scenario: Technical operator retries failed job
- **WHEN** a TECHNICAL operator resolves a configuration issue and triggers retry on a `FailedActionRequired` job
- **THEN** the system resets the Job status to `Queued`, resets attempt counters, and enqueues it for re-execution.

### Requirement: Idempotency and duplicate dispatch prevention

The system SHALL prevent duplicate active jobs for the same generation intent caused by network retries, UI double-clicks, or repeated dispatch.

The system SHALL compute a deterministic idempotency key for each generation request:
`SHA256(StoryboardVersionId + ":" + AssetRequirementId + ":" + VariantIndex + ":" + ConfigurationFingerprint)`

If an active `Job` (in status `Queued` or `Running`) already exists with the same idempotency key, the backend SHALL NOT create a duplicate job; instead, it SHALL return HTTP 200 OK with the existing active `Job` record and an informational notice.

#### Scenario: Duplicate generation click returns existing active Job
- **WHEN** an operator triggers generation for an AssetRequirement while a Job for the same requirement is already in "Running" status
- **THEN** the backend detects the active idempotency key and returns the existing Job representation without enqueuing a duplicate task.

### Requirement: Operational attention and failure observability

Failed production jobs requiring human intervention (`FailedActionRequired`, exhausted `FailedRetryable`) and unreviewed candidate batches awaiting human QA SHALL surface in Dashboard Attention:
1. `FailedActionRequired` jobs surface with high urgency linking directly to the affected ContentItem and Job diagnostics drawer;
2. Generated candidates in `PendingReview` surface in editorial attention when all visual requirements for an approved storyboard have completed generation.

The system SHALL NOT create an `EditorialTask` for every queued or running job; attention items SHALL be strictly reserved for actionable human decision states.

#### Scenario: Generation failure surfaces in dashboard attention
- **WHEN** a generation job fails with status `FailedActionRequired`
- **THEN** the Dashboard Attention widget reflects the operational failure count
- **AND** clicking the item navigates directly to the Content Detail Visual Production Studio with the error drawer open.

### Requirement: Role-based authorization (`EDITORIAL` vs `TECHNICAL`)

The backend SHALL enforce explicit role-based access control on all visual generation endpoints:
- `EDITORIAL` role (or development GOD mode) is REQUIRED to:
  - Trigger visual asset generation from approved storyboards;
  - View generated candidate previews;
  - Approve, reject, or select generated candidates.
- `TECHNICAL` role (or development GOD mode) is REQUIRED to:
  - Inspect technical provider execution payloads, node graphs, and raw telemetry;
  - Trigger manual retries on failed jobs;
  - Update visual provider configurations or workflow templates.
- Non-authorized users SHALL be rejected with HTTP 403 Forbidden.

#### Scenario: Editorial operator triggers generation and approves candidate
- **WHEN** an authenticated user with EDITORIAL role requests generation and subsequently approves a candidate
- **THEN** both requests succeed with HTTP 202 Accepted and HTTP 200 OK respectively.

#### Scenario: Editorial operator cannot access technical provider settings
- **WHEN** a user with EDITORIAL role (and without TECHNICAL role) attempts to access technical provider configuration endpoints
- **THEN** the backend rejects the request with HTTP 403 Forbidden.

### Requirement: Angular 21 Visual Asset Production Studio

The frontend SHALL provide a dedicated Visual Asset Production Studio within the Content Detail view (under Storyboard / Production section) built on Angular 21, PrimeNG 21, and Tailwind CSS 4 adhering to the canonical full-width operational layout contract:
1. **Header & Progress Toolbar**: Displays total visual requirements count, generated count, approved count, pending review count, active running jobs indicator, and a "Generate All Visual Assets" batch action.
2. **Visual Requirement Cards / Timeline**: High-density grid of visual requirements organized by storyboard frame sequence, displaying:
   - Originating storyboard frame sequence, script scene beat badge (Hook, Problem, Insight, Climax, CTA), and frame duration;
   - Provider-neutral prompt, negative prompt, aspect ratio ("9:16"), and style intent;
   - Live generation status badge (`NotGenerated`, `Queued`, `Running`, `Completed`, `Failed`);
   - Candidate variant thumbnail grid (1-4 candidates) with review status badges (`PendingReview`, `Approved`, `Rejected`) and "Selected for Assembly" star/badge;
   - Per-requirement action triggers ("Generate 1 Variant", "Generate 2 Variants", "Regenerate").
3. **Candidate Inspection & QA Modal/Drawer**: Full-resolution 9:16 preview container with zoom, side-by-side candidate comparison, prompt metadata drawer, "Approve & Select", "Reject" (with reason input dialog), and "Download" buttons.
4. **Technical Job Diagnostics Drawer**: Expandable drawer for TECHNICAL operators showing attempt history, duration, cost, provider name, workflow ID, and sanitized error messages with "Retry Job" trigger.
5. **Full-Width Desktop Layout (>= 1280px)**: Spans 100% available viewport width minus compact padding (12-16px / `p-3 sm:p-5`), showing multi-column requirement cards and side-by-side candidate previews without arbitrary max-width restrictions.
6. **Responsive Mobile Layout (~390px)**: Reflows requirement cards into a vertical stack with swipeable candidate previews, bottom sticky action sheet for candidate approval/rejection, and drawer-based job telemetry without horizontal page overflow.

#### Scenario: Full-width visual candidate comparison on desktop
- **WHEN** an operator opens the Visual Production Studio on desktop (viewport >= 1280px)
- **THEN** requirement cards and candidate comparison grids utilize the full available horizontal width
- **AND** high-resolution 9:16 candidate previews display side-by-side with immediate Approve/Reject controls.

#### Scenario: Responsive candidate review on mobile
- **WHEN** an operator opens the Visual Production Studio on a mobile device (~390px)
- **THEN** candidate cards stack cleanly with touch-friendly approve/reject buttons
- **AND** opening candidate details opens a responsive full-screen modal with zoomable preview and rejection reason prompt.
