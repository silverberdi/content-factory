## 1. Backend Domain Models, EF Core Migrations and Seed Data

- [x] 1.1 Define domain models: `ContentItem`, `ContentItemEvidence` (with status `Captured`, `CaptureFailed`, `Excluded`), `TruthSource` (with `Version: long`), `TruthSourceVersion` (with supporting evidence IDs), `EditorialTask`, `AiRecommendation`, and status/role enums.
- [x] 1.2 Configure EF Core entity mappings in `AppDbContext`, setting up foreign key relations referencing `ContentItemId`, explicit concurrency tokens (`.IsConcurrencyToken()` on `Version`), and content hash columns.
- [x] 1.3 Create and apply database migration for content workspace and truth source tables.
- [x] 1.4 Implement `IEvidenceCaptureService` for in-process URL text extraction, SHA-256 hash calculation, direct text lead capture, truthful failure logging, and retry logic.
- [x] 1.5 Implement realistic seed data for `IA Simple ES` including sample ContentItems with immutable captured evidence snapshots, draft and approved TruthSources, and editorial tasks.
- [x] 1.6 Add unit tests for domain invariants: `Version: long` optimistic concurrency token checks, mandatory rejection reasons, duplicate ContentItem prevention from promoted candidates, truthful capture failure handling, and downstream progression gating on unapproved TruthSources.

## 2. AI Provider Router and TruthSource Generation Service

- [x] 2.1 Implement `IAiProviderRouter` with capability routing (`build_truth_source`), seeded DeepSeek reasoning default, Gemini configuration stub, and offline deterministic development mock.
- [x] 2.2 Implement `build_truth_source` prompt synthesis and structured JSON parsing producing draft proposals only (with claims citing captured evidence IDs, risk notes, do-not-say constraints, possible angles, and localization notes).
- [x] 2.3 Persist structured `AiRecommendation` records capturing provider, model, latency, token usage, estimated cost, confidence, and audit rationale (excluding private chain-of-thought).
- [x] 2.4 Add automated tests for AI provider routing, JSON schema parsing, fallback handling, and recommendation audit logging.

## 3. Backend Content & TruthSource API Controllers and Services

- [x] 3.1 Implement `IContentService` and `ContentController` (`/api/content-items`) for CRUD, channel/stage filtering, status tracking, and evidence capture/attachment/exclusion/retry.
- [x] 3.2 Implement `ITruthSourceService` and `TruthSourceController` (`/api/content-items/{id}/truth-source`) for retrieval, AI draft generation trigger, versioned editing with `expectedVersion` check, review submission, human approval (EDITORIAL role), rejection with mandatory reason, and version history.
- [x] 3.3 Implement `IEditorialTaskService` and `EditorialTaskController` (`/api/editorial-tasks`) for managing review tasks, auto-creating tasks on `UnderReview`, and resolving tasks on approval/rejection without generic inbox metaphor.
- [x] 3.4 Extend `DiscoveryCandidateController` with downstream continuation endpoints to start a new `ContentItem` from a promoted candidate (with duplicate prevention) or attach candidate evidence to an existing `ContentItem`.
- [x] 3.5 Add integration tests covering the full end-to-end backend flow: candidate promotion → ContentItem creation → evidence capture → AI draft synthesis → human edit with concurrency check → 409 conflict test → rejection with reason → re-review → human approval.

## 4. Frontend Content Workspace and TruthSource Studio

- [x] 4.1 Create Content feature state, TypeScript models, and API client services (`ContentService`, `TruthSourceService`, `EditorialTaskService`).
- [x] 4.2 Build `ContentListComponent` with high-density layout, channel selector, lifecycle stage chips, status filters, and search bar.
- [x] 4.3 Build `ContentDetailComponent` with operational header ("where is this piece?"), multi-evidence provenance panel (hashes, capture status, retry button for failed captures, and source links), and lifecycle navigation.
- [x] 4.4 Build `TruthSourceReviewStudioComponent` featuring side-by-side desktop layout (captured evidence vs structured claims & guardrails), inline edit mode with `expectedVersion` optimistic locking conflict dialog (409 reconciliation), and version history diff viewer.
- [x] 4.5 Build Approval and Rejection modal/drawer in Review Studio with mandatory rejection reasoning and role-aware action buttons.
- [x] 4.6 Add frontend unit tests for Content Workspace and TruthSource Review Studio components.

## 5. Candidate Triage & Dashboard Control Center Integration

- [x] 5.1 Update Discovery Candidate Triage drawer with downstream actions to start a new ContentItem or attach evidence to an existing ContentItem.
- [x] 5.2 Add Content Workspace and Editorial Tasks navigation links and pending review badge to the application shell navigation.
- [x] 5.3 Augment Dashboard Control Center with Content Pipeline stage summary widget and Attention items for pending TruthSource reviews with one-click review drawer.
- [x] 5.4 Verify responsive behavior on desktop (>=1440px), tablet, and mobile (~390px) with light and dark theme consistency.

## 6. Verification, Canonical Sync, Cross-Review and Quality Gate

- [x] 6.1 Run automated test suite (backend .NET tests and Angular frontend tests) and fix any regressions.
- [x] 6.2 Execute human verification walkthrough on live dev server (testing candidate promotion, ContentItem creation, evidence capture & retry, AI draft synthesis, versioned editing, 409 conflict handling, rejection with reason, and approval).
- [x] 6.3 Synchronize canonical documentation (`docs/canonical/18_BACKLOG.md` and related context if applicable).
- [x] 6.4 Prepare DeepSeek cross-review package and validate findings against canonical context.
- [x] 6.5 Run `openspec validate` and complete the Quality Gate checklist.
