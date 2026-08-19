## 1. Domain Models & Persistence Layer

- [x] 1.1 Define `Storyboard`, `StoryboardFrame`, `AssetPlan`, `AssetRequirement`, and `StoryboardVersion` entity classes in `ContentModels.cs` with full lineage, `IsCurrent`, `SupersededAtUtc`, `ReconciledFromStoryboardId`, provider-neutral asset requirements, duration aggregates, and optimistic concurrency (`Version`).
- [x] 1.2 Update `ContentLifecycleStage` enum (`StoryboardDrafted`, `StoryboardUnderReview`, `StoryboardApproved`) and `EditorialTaskType` enum (`ReviewStoryboard`).
- [x] 1.3 Configure EF Core entity mappings, indexes, foreign keys, and "One Current Storyboard" server-side invariant rules in `ContentDbContext`.
- [x] 1.4 Create and apply EF Core database migration (`AddStoryboardProductionPlanning`) against PostgreSQL dev database.
- [x] 1.5 Write unit tests for Storyboard domain invariants, frame-to-scene timing calculations, provider-agnostic asset requirement validation, and lineage validation.

## 2. AI Capabilities & Routing (`plan_storyboard` & `review_storyboard`)

- [x] 2.1 Register capabilities `plan_storyboard` and `review_storyboard` in `AiCapability` definitions and routing policies.
- [x] 2.2 Implement prompt templates for Spanish vertical 9:16 storyboard planning, extensible framing/motion intents, provider-agnostic asset requirements, and advisory visual critique with timing alignment checks.
- [x] 2.3 Implement deterministic mock adapter responses for `plan_storyboard` and `review_storyboard` in development mode.
- [x] 2.4 Write tests verifying AI provider routing, recommendation logging, timing checks, and development mock responses.

## 3. Backend Service & API Implementation

- [x] 3.1 Implement `IStoryboardService` and `StoryboardService` with generation, frame CRUD, reordering, splitting, asset requirements sync, dynamic upstream staleness evaluation (`IsStale`), and downstream eligibility evaluation (`GetProductionEligibilityAsync`).
- [x] 3.2 Implement immutable successor reconciliation (`ReconcileStoryboardAsync`) migrating compatible frame planning while maintaining historical storyboard records and enforcing the "One Current Storyboard" invariant.
- [x] 3.3 Implement full-spectrum optimistic concurrency (`expectedVersion`) and immutable `StoryboardVersion` snapshotting on all mutating operations.
- [x] 3.4 Implement single-gate review lifecycle transitions (`SubmitForReview`, `Approve`, `Reject` with mandatory reason, `Reopen`) and integrate `EditorialTask` creation and completion.
- [x] 3.5 Implement REST endpoints in `StoryboardController` with explicit `EDITORIAL` role authorization.
- [x] 3.6 Write integration tests for Storyboard API endpoints (CRUD, reconciliation, review lifecycle, 409 concurrency conflicts, 403 authorization, negative rejection validation, downstream eligibility).

## 4. Frontend API Client & State Management

- [x] 4.1 Define TypeScript models (`Storyboard`, `StoryboardFrame`, `AssetPlan`, `AssetRequirement`, `StoryboardVersion`, `StoryboardCritiqueResult`, `ProductionEligibility`) in `api.service.ts`.
- [x] 4.2 Add Storyboard API client methods (including reconcile, review, approve, reject, reopen) and reactive state signals in `ApiService`.
- [x] 4.3 Write unit tests for frontend Storyboard service methods and components.

## 5. Frontend Storyboard & Production Planning Studio

- [x] 5.1 Implement `StoryboardFrameCardComponent` displaying 9:16 vertical framing preview, framing intent selector, composition notes, camera motion selector, visual prompt editor, negative prompt, audio cues, on-screen text, frame duration with scene timing indicator, transition selector, and frame action controls.
- [x] 5.2 Implement `AssetPlanSummaryComponent` displaying provider-agnostic asset requirements summary (visual 9:16 prompts, voiceover profile, background music, sound effects, subtitles) and planning readiness.
- [x] 5.3 Implement `RejectStoryboardModalComponent` enforcing mandatory rejection reason.
- [x] 5.4 Implement `StoryboardVersionHistoryDrawerComponent` showing immutable version timeline, author attribution, and diffs.
- [x] 5.5 Implement `StoryboardStudioComponent` (`src/web/src/app/features/content/storyboard-studio.component.ts`) with header metrics (frame count, live duration bar, status badge), stale lineage reconciliation banner, script reference side-panel, AI generation modal, advisory AI review panel, and sticky action bar.
- [x] 5.6 Integrate Storyboard Studio tab into `ContentDetailComponent` and update lifecycle stages navigation.
- [x] 5.7 Update Dashboard Attention and Editorial Attention views with `ReviewStoryboard` task badges and deep links.
- [x] 5.8 Ensure full-width desktop layout (100% horizontal width) and responsive mobile stack (~390px).
- [x] 5.9 Write component and end-to-end frontend tests in `content.spec.ts` and `layout.spec.ts`.

## 6. Seed Data, Verification & Cross-Review

- [x] 6.1 Update database seed data with realistic approved scripts, draft storyboards, and approved storyboards for channel `IA Simple ES`.
- [x] 6.2 Execute automated test suite (backend dotnet test, frontend npm test, build verification).
- [x] 6.3 Perform manual human verification walkthrough of storyboard planning, editing, AI critique, review submission, approval, rejection, reopening, and reconciliation.
- [x] 6.4 Sync canonical documentation (`03_DOMAIN_MODEL_V2.md`, `18_BACKLOG.md`) and run DeepSeek cross-review.
