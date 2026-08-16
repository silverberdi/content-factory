## Why

Following the canonical Wave 1 editorial loop (`Source` → `TruthSource` → `ContentIdea` → `Script`), once a `TruthSource` is approved by human editorial review, the operator needs structured creative angles, hook strategies, and editorial formats to transform verified factual evidence into compelling short-form video concepts.

Currently, the pipeline stops at `TruthSourceApproved`. This change introduces the `ContentIdea` layer, immutable lineage to the exact approved `TruthSourceVersion`, and automated AI idea generation (`generate_ideas`), enabling operators to explore multiple creative proposals from a single truth source, edit and evaluate concepts with immutable version history, and select exactly one active creative foundation to unlock subsequent scripting.

## What Changes

- **Domain Model & Immutable Lineage**: Introduce `ContentIdea` and lightweight `ContentIdeaVersion` entities linked to a parent `ContentItem` and explicitly referencing the exact approved `TruthSourceId` and `TruthSourceVersionId`. Later TruthSource edits will not alter the factual basis of existing ideas.
- **Precondition & Gating**: Strictly enforce the canonical invariant *"No downstream progression without approved TruthSource"*: idea generation and manual idea creation are rejected unless the parent ContentItem has an `Approved` TruthSource.
- **AI Idea Generator (`generate_ideas`)**: Implement the `generate_ideas` capability in `IAiProviderRouter` (DeepSeek reasoning default, Gemini configurable alternate, deterministic development mock) producing distinct proposals and persisting auditable `AIRecommendation` telemetry linked to the exact `TruthSourceVersionId` (without private chain-of-thought).
- **Deterministic Duplicate and Near-Duplicate Prevention**: Implement a lightweight, deterministic application-level similarity algorithm (token set / n-gram overlap across Title, Angle, HookStrategy, and AudienceValue) that filters both exact duplicates and obviously equivalent proposals before persistence, and validates manual entries without introducing vector databases, embeddings, or secondary AI pipelines.
- **Single Active Selection & Atomic Replacement Semantics**: Multiple ideas can exist as `Proposed` or `Dismissed`, but exactly ONE `ContentIdea` may be the active `Selected` idea for a ContentItem at any given time. Selecting an idea when one was already selected atomically replaces the previous selection within a consistent transaction, recording `ContentIdeaVersion` snapshots for both affected ideas and setting the ContentItem lifecycle stage to `IdeaSelected`.
- **Full-Spectrum Optimistic Concurrency & Version History**: `Version: long` (`expectedVersion`) protects ALL mutable transitions (Update, Select/Replace, Dismiss, Reopen), rejecting stale requests with machine-readable HTTP 409 Conflict (`CONCURRENCY_CONFLICT`), while `ContentIdeaVersion` preserves immutable snapshots for every state change.
- **Backend Authorization**: Protect all idea generation, creation, editing, dismissal, reopening, and selection endpoints with explicit editorial policies (`RequireEditorial`).
- **Frontend Version Lock & Idea Matrix**: Built on Angular 21, PrimeNG 21, and Tailwind CSS 4 (no framework upgrades), delivering a high-density Idea Matrix in the Content Workspace with card scanning, hook badges, generation modal, edit drawer, version history drawer, and one-click selection.
- **Dashboard Integration**: Add Content Pipeline counters for `TruthSourceApproved` and `IdeaSelected` stages, plus Attention alerts for items pending idea selection.

## Capabilities

### New Capabilities
- `content-idea-generation`: Defines `ContentIdea`, `ContentIdeaVersion`, exact `TruthSourceVersion` lineage, AI `generate_ideas` capability routing, deterministic duplicate/near-duplicate prevention, single active selection replacement semantics, full-spectrum mutation concurrency, backend authorization, and optimistic concurrency.

### Modified Capabilities
- `content-workspace`: Updates Content Workspace and Detail views to display and manage ideas, link ideas to parent ContentItems, and advance the lifecycle stage to `IdeaSelected` when an idea is selected.
- `truth-source-evidence`: Extends downstream progression validation so an approved TruthSource directly feeds the idea generation stage while unapproved TruthSources block it.
- `application-shell-dashboard`: Adds idea pipeline counters and attention items for pieces pending idea generation or editorial selection.

## Impact

- **Backend (`ContentFactory.Api`)**:
  - New entities `ContentIdea` and `ContentIdeaVersion` mapped in `AppDbContext` with `Version: long` concurrency token and index on `[ContentItemId, Status]`.
  - EF Core database migration for content idea and content idea version tables.
  - New service `IContentIdeaService` and endpoints under `/api/content-items/{id}/ideas` protected by `RequireEditorial`.
  - Extension of `IAiProviderRouter` with capability `AiCapabilities.GenerateIdeas` (`generate_ideas`).
- **Frontend (`src/web`)**:
  - Angular 21 standalone components: `ContentIdeasComponent` / `IdeaMatrixComponent`, `GenerateIdeasModalComponent`, `IdeaEditDrawerComponent`, `IdeaVersionHistoryDrawerComponent`.
  - Content Detail tabs/navigation augmented with Ideas view.
  - Updated API client models and reactive Signals state.
- **Seed Data & Tests**:
  - Realistic seed data with approved TruthSources and sample ContentIdeas for channel `IA Simple ES`.
  - Backend domain and integration tests for lineage, gating, near-duplicate filtering, full mutation concurrency (409 on update/select/dismiss/reopen), replacement semantics, and authorization.
  - Frontend component unit tests.
