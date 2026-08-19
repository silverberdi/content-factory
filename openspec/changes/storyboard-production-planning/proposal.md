## Why

An approved editorial script cannot directly enter automated or semi-automated media generation without a concrete visual and audio production specification. Moving from narration beats to short vertical video (9:16) requires decomposing each script scene into structured visual frames, provider-neutral image/video prompts, visual framing intents, audio cues, on-screen text overlays, and an asset requirements plan (specifying visual, voiceover, music, and subtitle assets) ready for future production pipelines.

Introducing the Storyboard & Production Planning capability establishes the bridge between editorial scriptwriting and future media production, providing AI-assisted visual planning, frame-level prompt and intent engineering, provider-agnostic asset requirements planning, and a single human editorial review gate before downstream generation capabilities are unlocked.

## What Changes

- **Domain Model & Entities**:
  - Add `Storyboard` aggregate root with `StoryboardFrame` collection linked to parent `ContentItem`, `ChannelId`, `ScriptId`, `ScriptVersionId`, `TruthSourceId`, and `TruthSourceVersionId`.
  - Add `AssetPlan` and `AssetRequirement` models specifying required visual assets (AI image/video prompts, style intents, 9:16 aspect ratio, camera motion intents), audio assets (voice profile intent, pacing WPM, background music mood, sound effect intents), and subtitle styling intents.
  - Keep asset specifications provider-agnostic: no ComfyUI workflow IDs, samplers, checkpoints, node graphs, or execution parameters in the domain.
  - Keep asset planning strictly bounded to specification: no runtime execution states (`Generating`, `Generated`, `Failed`) and no runtime artifact references (MinIO keys, errors, runtime costs, execution durations).
  - Add `StoryboardVersion` immutable snapshot entity capturing full frame and asset requirement state on every mutation with optimistic concurrency.
  - Enforce upstream lineage and staleness rules: a Storyboard is marked `IsStale` / `Superseded` if the underlying Script is modified, superseded, or reverted from `Approved`.
  - Enforce immutable reconciliation semantics: reconciling a stale storyboard creates an immutable successor `Storyboard` (in `Draft`) linked to the new `ScriptVersionId`, optionally reusing compatible editorial frame planning and recording provenance (`ReconciledFromStoryboardId`), without overwriting historical storyboard lineage.
  - Enforce "One Current Storyboard" invariant: exactly one active, non-superseded Storyboard per `ContentItem` at any time; prior storyboards are retained immutably in history.
  - Enforce single editorial approval gate: `AssetPlan` is an integral part of the `Storyboard` specification; approving a `Storyboard` approves the entire visual and asset specification in one atomic editorial action.
  - Add explicit review lifecycle: `Draft` -> `UnderReview` -> `Approved` / `Rejected` (with mandatory rejection reason), and `Reopen` -> `Draft`.

- **AI Provider Routing & Capabilities**:
  - Register capability `plan_storyboard` to synthesize structured storyboard frames, visual generation prompts, framing/motion intents, audio cues, and asset requirements from an approved script.
  - Register capability `review_storyboard` for advisory AI critique of visual coherence, pacing, 9:16 composition, scene-to-frame timing alignment, and prompt quality.
  - Support deterministic mock adapters in development when live AI provider credentials are not configured.

- **Backend API & Orchestration**:
  - Implement `IStoryboardService` and REST endpoints for storyboard generation, manual frame editing, reordering, splitting, asset requirements updates, AI critique, review submission, approval, rejection, reopening, and immutable reconciliation.
  - Implement explicit backend authorization requiring `EDITORIAL` role for all mutation operations.
  - Advance `ContentItem` lifecycle stage across `StoryboardDrafted`, `StoryboardUnderReview`, and `StoryboardApproved`.
  - Create and complete `EditorialTask` of type `ReviewStoryboard` during review lifecycle transitions.
  - Define the domain downstream eligibility contract for future media generation capabilities.

- **Frontend Angular 21 PWA**:
  - Add dedicated Storyboard & Production Planning Studio within the Content Detail view (`src/web/src/app/features/content/storyboard-studio.component.ts`).
  - High-density frame cards displaying visual preview/placeholder, 9:16 aspect badge, framing/shot intent, visual prompt editor, negative prompt, duration badge with scene timing comparison, audio cue & voiceover snippet, on-screen text editor, and transition intent selector.
  - Asset Plan summary panel detailing required visual assets, audio profile, subtitle configuration, and generation readiness.
  - Frame timeline management: reordering, addition, splitting, deletion, and real-time total duration calculation with scene-level duration alignment feedback.
  - Advisory AI critique panel and AI generation modal.
  - Storyboard version history and diff drawer.
  - Contextual action bar: "Submit for Review", "Approve", "Reject" (with reason modal), "Reopen", and "Reconcile" (creates successor storyboard for stale items).
  - Full mobile responsiveness (~390px) supporting complete review and decision workflows on touch viewports.

- **Dashboard Attention & Tasks**:
  - Update Dashboard Attention widget and Editorial Attention view with `ReviewStoryboard` task badges and direct deep links into Storyboard Studio.

- **Non-Goals**:
  - ComfyUI GPU workflow execution or video rendering pipelines (CF-031 / CF-033).
  - TTS speech synthesis audio rendering or audio mixing execution (CF-032).
  - Media asset storage / MinIO file uploads for generated media.
  - Platform publication operations (Wave 4 CF-040).

## Capabilities

### New Capabilities
- `storyboard-production-planning`: Core storyboard and provider-agnostic asset requirements domain, frame-level visual/audio specifications, AI storyboard planning and critique, immutable versioning and reconciliation, single-gate review/rejection/reopen lifecycle, downstream generation eligibility contract, backend API, and Angular 21 Storyboard Studio.

### Modified Capabilities
- `content-workspace`: Extend `ContentItem` lifecycle stages (`StoryboardDrafted`, `StoryboardUnderReview`, `StoryboardApproved`), integrate Storyboard Studio tab navigation in Content Detail, enforce "one current storyboard" invariant, and expose downstream production eligibility.
- `editorial-task-attention`: Add `ReviewStoryboard` task type to `EditorialTask`, automate task creation on submission for review and completion on approval/rejection, and integrate into dashboard attention widgets.

## Impact

- **Backend Models & Persistence**:
  - New entities: `Storyboard`, `StoryboardFrame`, `AssetPlan`, `AssetRequirement`, `StoryboardVersion`.
  - Updated enum: `ContentLifecycleStage` with `StoryboardDrafted`, `StoryboardUnderReview`, `StoryboardApproved`.
  - Updated enum: `EditorialTaskType` with `ReviewStoryboard`.
  - EF Core migrations for PostgreSQL `content_factory_dev` and `content_factory_prod`.
- **Backend Services & API**:
  - `IStoryboardService` and `StoryboardService` in `src/api/ContentFactory.Api/Modules/Content/`.
  - New controllers/endpoints under `/api/content/{id}/storyboard`.
  - Updates to `IEditorialTaskService` and `IContentService`.
  - Updates to `IAiProviderRouter` for `plan_storyboard` and `review_storyboard`.
- **Frontend Components**:
  - New `storyboard-studio.component.ts`, `storyboard-frame-card.component.ts`, `asset-plan-summary.component.ts`, `reject-storyboard-modal.component.ts`, `storyboard-version-history-drawer.component.ts`.
  - Updates to `content-detail.component.ts`, `editorial-tasks-list.component.ts`, `dashboard.component.ts`, `content.service.ts`.
- **Testing & Seed Data**:
  - Unit/integration tests in backend and frontend.
  - Seed data with realistic Spanish short-form storyboards and asset plans for channel `IA Simple ES`.
