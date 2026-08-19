## Why

Content Factory now produces fully approved, non-stale vertical Storyboards with provider-neutral AssetPlans (`AiImage`, `AiVideo`, `BRoll`, `GraphicOverlay`). However, the platform currently stops at planning specification (WHAT media is needed) and has no operational capability to execute real media production (HOW media is produced).

To transition from editorial planning into real audiovisual production without compromising the domain boundary established by `storyboard-production-planning`, Content Factory requires an asynchronous visual generation execution engine. This engine converts approved, provider-neutral `AssetRequirement` records into trackable `Job` units, dispatches them through a provider-neutral adapter layer (`IVisualGenerationProvider`, supporting ComfyUI and a deterministic development mock), stores raw binary artifacts in existing MinIO object storage, persists immutable `GeneratedAsset` metadata, and enforces mandatory human visual QA review (Approve/Reject/Select) before generated assets can qualify for downstream video assembly.

## What Changes

- **Canonical Job Domain & Async Execution**: Introduces the canonical `Job` domain entity and background worker (`VisualGenerationBackgroundWorker`) to manage asynchronous production work (`generate_visual_asset`) with canonical lifecycle states (`Queued`, `Running`, `Succeeded`, `FailedRetryable`, `FailedActionRequired`, `Cancelled`), bounded exponential backoff retries, and sanitized error telemetry.
- **Provider-Neutral Visual Adapter Boundary**: Implements `IVisualGenerationProvider` isolating editorial planning from provider-specific execution graphs. Includes `ComfyVisualGenerationProvider` (translating prompts, negative prompts, 9:16 aspect ratio, and style intents into Comfy workflow payloads) and `MockVisualGenerationProvider` (deterministic local development mock with fixture generation and failure simulation).
- **MinIO Object Storage Integration**: Implements `IStorageService` integrating with existing MinIO infrastructure to persist binary image/video assets under deterministic, non-leaking object paths (`content-factory/{environment}/channels/{channelId}/content/{contentItemId}/storyboard/{storyboardVersionId}/visual/{assetRequirementId}/{generatedAssetId}.{ext}`) with SHA-256 checksums and MIME verification.
- **Generated Asset Domain & Immutable Lineage**: Introduces `GeneratedAsset` recording complete lineage (`ContentItemId`, `ChannelId`, `StoryboardId`, `StoryboardVersionId`, `AssetRequirementId`, `JobId`), media dimensions, durations, MinIO storage references, generation parameter snapshots, and human QA review status (`PendingReview`, `Approved`, `Rejected`).
- **Generation Variants & Human Visual QA Gate**: Supports generating 1 to 4 candidate variations per `AssetRequirement`. Enforces mandatory human visual review: generated candidates are NEVER automatically approved. Operators can inspect candidate previews, reject with reason, or approve/select the authoritative candidate for downstream assembly.
- **Upstream Staleness Gating**: Revalidates Storyboard lineage immediately before dispatch (blocking generation on unapproved or stale storyboards). Preserves historical `GeneratedAsset` metadata lineage in PostgreSQL (while raw binary retention in object storage is governed by configurable retention policies), and marks stale assets ineligible for future final video assembly unless explicitly reconciled.
- **Role-Based Operational Authorization**: Enforces `EDITORIAL` role for generation requests and candidate approval/rejection, and `TECHNICAL` role for inspecting technical provider payloads and triggering operational retries.
- **Angular 21 Visual Asset Production Studio**: Adds a full-width operational studio within Content Detail / Storyboard featuring high-density candidate comparison, live job status monitoring, candidate zoom preview, approve/reject/retry action bars, and mobile-responsive drawer triage.

## Capabilities

### New Capabilities
- `visual-asset-generation`: Covers asynchronous visual asset production (`generate_visual_asset`), canonical `Job` execution and retry resilience, `IVisualGenerationProvider` adapter boundary (ComfyUI & Dev Mock), MinIO object storage persistence, `GeneratedAsset` candidate lifecycle and lineage, upstream staleness gating, human visual QA approval/rejection, and the Angular 21 Visual Asset Production Studio.

### Modified Capabilities
<!-- No requirement changes to existing capability specs. storyboard-production-planning downstream gating is satisfied as designed. -->

## Impact

- **Backend**:
  - New entities: `Job`, `JobAttempt`, `GeneratedAsset` in `ContentModels.cs` / `JobModels.cs`.
  - New services & adapters: `IVisualGenerationService`, `VisualGenerationService`, `IVisualGenerationProvider`, `ComfyVisualGenerationProvider`, `MockVisualGenerationProvider`, `IStorageService`, `MinioStorageService`, `MockStorageService`, `VisualGenerationBackgroundWorker`.
  - Database: EF Core PostgreSQL migration for `jobs`, `job_attempts`, `generated_assets` tables.
  - Endpoints: `POST/GET /api/content-items/{id}/storyboards/{storyboardId}/visual-generation`, `GET /api/jobs/{id}`, `POST /api/jobs/{id}/retry`, `POST /api/generated-assets/{id}/review`, `POST /api/generated-assets/{id}/select`.
- **Frontend**:
  - New components: `VisualAssetStudioComponent`, `VisualCandidateCardComponent`, `VisualCandidatePreviewModalComponent`, `JobStatusBadgeComponent`, `RejectCandidateModalComponent`.
  - State & Services: `VisualGenerationService`, `JobService` with polling/reactive status updates.
- **Infrastructure**:
  - Existing MinIO bucket verification (`content-factory-assets`).
  - Configuration entries for `Comfy` endpoint / workflow templates and MinIO client credentials in `appsettings.json` and `.env.development.example`.
- **Non-Goals / Preserved Boundaries**:
  - No TTS audio synthesis, music, sound effects, audio mixing, subtitle rendering, or final video assembly in this change.
  - No modification to `AssetRequirement` schema to hold provider-specific fields.
