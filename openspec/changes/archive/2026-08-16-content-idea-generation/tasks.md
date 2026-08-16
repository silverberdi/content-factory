## 1. Backend Domain Models, EF Core Migrations and Seed Data

- [x] 1.1 Define `ContentIdea` and `ContentIdeaVersion` domain entities with creative properties (`Title`, `Angle`, `HookStrategy`, `AudienceValue`, `Format`, `IntendedOutcome`, `FreshnessClass`, `Priority`, `Rationale`, `Status`), `Version: long` concurrency token, and foreign key references to `ContentItemId`, `TruthSourceId`, and the exact approved `TruthSourceVersionId`.
- [x] 1.2 Configure EF Core entity mappings in `AppDbContext`, setting up foreign key relations, `.IsConcurrencyToken()` on `Version`, indexes on `[ContentItemId, Status]`, and stage enum update (`IdeaSelected`).
- [x] 1.3 Create and apply database migration for content idea and content idea version tables.
- [x] 1.4 Implement realistic seed data for `IA Simple ES` including sample ContentIdeas across `Proposed` and `Selected` statuses linked to approved TruthSource versions.
- [x] 1.5 Add backend unit tests for domain invariants: `TruthSourceVersionId` lineage preservation, `Version: long` optimistic concurrency token checks, gating against unapproved TruthSources, and stage advancement to `IdeaSelected`.

## 2. AI Provider Router Idea Generation Service

- [x] 2.1 Add capability `AiCapabilities.GenerateIdeas` (`"generate_ideas"`) to `IAiProviderRouter` with DeepSeek reasoning prompt template, Gemini alternate configuration, and deterministic offline development mock.
- [x] 2.2 Implement structured prompt synthesis injecting approved TruthSource claims/constraints and parsing JSON array of 3-5 distinct creative idea proposals with varied hook styles.
- [x] 2.3 Persist structured `AIRecommendation` telemetry for idea generation (ContentItemId, TruthSourceVersionId, provider, model, token usage, latency, cost, rationale) without private chain-of-thought.
- [x] 2.4 Add automated tests for idea generation AI capability routing, JSON response parsing, error handling, and recommendation audit logging with `TruthSourceVersionId` linkage.

## 3. Backend Content Idea API Controllers and Services

- [x] 3.1 Implement deterministic duplicate AND near-duplicate similarity check in `ContentIdeaService` (normalized token sets / overlap across Title, Angle, HookStrategy, AudienceValue) to filter equivalent AI proposals and validate manual creations.
- [x] 3.2 Implement `IContentIdeaService` and `ContentIdeaController` (`/api/content-items/{id}/ideas`) for retrieval, AI generation trigger, manual creation, and full-spectrum versioned mutation endpoints (Update, Select/Replace, Dismiss, Reopen) requiring `expectedVersion`.
- [x] 3.3 Apply backend authorization policies (`[Authorize(Policy = "RequireEditorial")]`) to all idea mutation endpoints.
- [x] 3.4 Implement atomic single active selection replacement semantics in `ContentIdeaService` ensuring at most one idea is `Selected` and recording version snapshots for both affected entities.
- [x] 3.5 Add integration tests covering the complete idea lifecycle: approved TruthSource → AI idea generation → near-duplicate filtering → manual idea addition → full-spectrum 409 conflict checks on update/select/dismiss/reopen → atomic selection replacement → negative authorization test → ContentItem stage advances to `IdeaSelected`.

## 4. Frontend Idea Matrix & Ideas Workspace (Angular 21 Lock)

- [x] 4.1 Create ContentIdea and ContentIdeaVersion TypeScript models and API client methods in `ApiService` (`getContentIdeas`, `generateIdeas`, `createIdea`, `updateIdea`, `selectIdea`, `dismissIdea`, `reopenIdea`, `getIdeaVersions`) with `expectedVersion` parameters on all mutations.
- [x] 4.2 Build `ContentIdeasComponent` / `IdeaMatrixComponent` in Angular 21 (PrimeNG 21, Tailwind CSS 4) featuring high-density grid comparison, hook strategy badges, freshness/priority indicators, and status filtering (`Proposed`, `Selected`, `Dismissed`).
- [x] 4.3 Build `GenerateIdeasModalComponent` with configurable focus options (e.g. angle styles, target audience) and instant generation loading states.
- [x] 4.4 Build `IdeaEditDrawerComponent` for manual idea authoring and versioned editing with optimistic locking conflict resolution.
- [x] 4.5 Build `IdeaVersionHistoryDrawerComponent` for viewing immutable historical versions of an idea.
- [x] 4.6 Add frontend unit tests for Idea Matrix, generation modal, selection replacement, and version history.

## 5. Content Workspace & Dashboard Attention Integration

- [x] 5.1 Integrate Ideas tab and idea summary metrics into `ContentDetailComponent` with operational lifecycle breadcrumbs and single active selected idea badge.
- [x] 5.2 Augment Dashboard Attention widget to surface ContentItems with approved TruthSources requiring idea generation or idea selection.
- [x] 5.3 Verify responsive behavior on desktop (>=1440px), tablet, and mobile (~390px) with light/dark theme consistency on Angular 21.

## 6. Verification, Canonical Sync, Cross-Review and Quality Gate

- [x] 6.1 Run full automated test suite (backend .NET tests and Angular frontend tests) and resolve any regressions.
- [x] 6.2 Execute human verification walkthrough on live dev server (testing approved TruthSource gating, AI idea synthesis, near-duplicate filtering, manual idea creation, full-spectrum 409 conflict handling, dismissal, and atomic idea selection/replacement advancing ContentItem stage to `IdeaSelected`).
- [x] 6.3 Synchronize canonical documentation (`docs/canonical/18_BACKLOG.md` and related documents).
- [x] 6.4 Prepare DeepSeek cross-review package and validate findings against canonical context.
- [x] 6.5 Run `openspec validate` and complete Quality Gate checklist.
