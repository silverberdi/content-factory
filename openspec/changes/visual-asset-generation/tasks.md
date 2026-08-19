## 1. Domain & Persistence

- [x] 1.1 Model canonical `Job` and `JobAttempt` entities in `JobModels.cs` with statuses, attempt tracking, error sanitization, and idempotency key.
- [x] 1.2 Model `GeneratedAsset` entity in `ContentModels.cs` with immutable lineage (`ContentItemId`, `StoryboardId`, `StoryboardVersionId`, `AssetRequirementId`, `JobId`), storage keys, dimensions, checksum, variant index, and review status.
- [x] 1.3 Update `AppDbContext` entity configurations, indexes (idempotency, lineage, status), and navigation properties.
- [x] 1.4 Generate and apply EF Core PostgreSQL migration `AddVisualAssetGenerationJobAndMediaTables`.
- [x] 1.5 Write unit tests in `JobAndGeneratedAssetDomainTests.cs` validating domain invariants, status transitions, lineage immutability, and single-selection rules.

## 2. MinIO Storage & Provider Adapters

- [x] 2.1 Implement `IStorageService` and `MinioStorageService` with deterministic object key generation, SHA-256 calculation, and media streaming.
- [x] 2.2 Define `IVisualGenerationProvider` interface, neutral request/result records, and output media stream definitions.
- [x] 2.3 Implement `MockVisualGenerationProvider` with deterministic SVG/PNG fixture generation, async delay simulation, and deterministic failure triggers (`[mock:retryable-failure]`, `[mock:action-required-failure]`).
- [x] 2.4 Implement `ComfyVisualGenerationProvider` with configurable workflow template resolution, prompt mapping, 9:16 aspect ratio handling, and output retrieval.
- [x] 2.5 Write unit tests in `VisualGenerationProviderAndStorageTests.cs` verifying MinIO key conventions, Mock provider outputs, and Comfy payload mapping.

## 3. Background Execution & Services

- [x] 3.1 Implement `IVisualGenerationService` and `VisualGenerationService` managing pre-dispatch eligibility revalidation, idempotency checking, job dispatch, and candidate creation.
- [x] 3.2 Implement `VisualGenerationBackgroundWorker` (`IHostedService`) processing queued jobs with exponential backoff retries, failure classification, and error sanitization.
- [x] 3.3 Implement candidate review logic (Approve with atomic assembly selection, Reject with mandatory reason, Select variant).
- [x] 3.4 Implement upstream staleness and assembly eligibility query evaluation.
- [x] 3.5 Write service-level unit tests in `VisualGenerationServiceTests.cs` covering eligibility gating, idempotency, retry handling, and candidate approval/rejection.

## 4. REST API & Controllers

- [x] 4.1 Implement `VisualGenerationController` with endpoints for generation dispatch (202 Accepted), listing visual assets, inspecting jobs, retrying jobs, reviewing candidates, selecting variants, and streaming media proxy.
- [x] 4.2 Enforce explicit backend role-based authorization (`EDITORIAL` for generation/QA, `TECHNICAL` for retry/diagnostics) and development GOD mode bypass.
- [x] 4.3 Update Dashboard and Attention query services to surface `FailedActionRequired` jobs and unreviewed candidate batches.
- [x] 4.4 Write integration tests in `VisualGenerationApiIntegrationTests.cs` verifying end-to-end API flows, 202 async response, idempotency, authorization rejections, and media streaming.

## 5. Frontend Angular Visual Studio

- [x] 5.1 Implement `VisualGenerationService` and `JobService` in Angular frontend for API interaction, polling, and candidate state management.
- [x] 5.2 Create `VisualCandidateCardComponent` with 9:16 aspect ratio preview container, status badges, assembly star indicator, and quick review triggers.
- [x] 5.3 Create `VisualCandidatePreviewModalComponent` supporting high-res zoom, side-by-side comparison, prompt details, and reject reason modal.
- [x] 5.4 Create `JobDiagnosticsDrawerComponent` for inspecting technical execution logs, attempts, error details, and triggering retries.
- [x] 5.5 Create `VisualAssetStudioComponent` integrating requirement cards, batch progress bar, batch generation trigger, and full-width layout compliance.
- [x] 5.6 Integrate Visual Asset Studio into Content Detail tabs and add Dashboard Attention card deep-links.
- [x] 5.7 Write frontend unit tests in `visual-asset-studio.spec.ts` covering rendering, generation dispatch, candidate approval/rejection, and responsive drawer behavior.

## 6. Verification, Seed Data & Quality Gate

- [x] 6.1 Update database seed data (`DatabaseInitializer.cs`) with realistic visual requirements and mock generated candidate assets for "IA Simple ES" channel.
- [x] 6.2 Execute human-assisted validation script testing full workflow (generation, review, retryable/action-required failures, stale invalidation).
- [x] 6.3 Synchronize canonical documentation (`docs/canonical/03_DOMAIN_MODEL_V2.md`, `12_OBSERVABILITY_COST_JOBS.md`, `17_ROADMAP.md`, `18_BACKLOG.md`).
- [x] 6.4 Run full automated test suite (`dotnet test` and `npm test`) and verify zero regressions.
- [x] 6.5 Execute DeepSeek cross-review and OpenSpec verification gate before completion.
