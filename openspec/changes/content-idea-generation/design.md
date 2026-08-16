## Context

In the Content Factory Wave 1 editorial workflow, the pipeline transitions from factual source evidence (`TruthSource`) to creative video production. Once a `TruthSource` is approved by human editorial review, the operator must explore, generate, and select creative angles, hook strategies, and format concepts before initiating scriptwriting.

This design establishes the `ContentIdea` aggregate, exact `TruthSourceVersion` lineage, `ContentIdeaVersion` historical tracking, deterministic duplicate and near-duplicate prevention, single active selection replacement semantics, full-spectrum mutation concurrency, `generate_ideas` AI capability routing, and the interactive Idea Matrix in the Angular 21 PWA.

## Goals / Non-Goals

**Goals:**
- Implement `ContentIdea` domain entity linked to `ContentItem` with immutable foreign key references to `TruthSourceId` and the exact approved `TruthSourceVersionId`.
- Support AI-driven idea generation (`generate_ideas`) producing multiple diverse proposals (angles, hook techniques, audience value, format, intended outcome, freshness, priority, rationale) with `AIRecommendation` audit records linking to the input `TruthSourceVersionId`.
- Implement deterministic lightweight duplicate AND near-duplicate prevention (normalized token sets / n-gram overlap across Title, Angle, HookStrategy, and AudienceValue) filtering equivalent proposals and preventing duplicate manual entries without vector databases.
- Strictly enforce domain gating: idea generation and manual creation are blocked unless the parent ContentItem has an `Approved` TruthSource.
- Enforce single active selection semantics: exactly one idea can be active `Selected` at a time. Selecting a new idea atomically replaces the previous selection (preserving historical lineage in `ContentIdeaVersion` for both entities) and advances ContentItem stage to `IdeaSelected`.
- Enforce full-spectrum optimistic concurrency: `Version: long` (`expectedVersion`) is required for ALL mutable transitions (Update, Select/Replace, Dismiss, Reopen), rejecting stale requests with HTTP 409 Conflict (`CONCURRENCY_CONFLICT`).
- Preserve immutable editorial history via `ContentIdeaVersion` snapshots on every mutation.
- Authorize all idea editorial mutations on the backend requiring the `EDITORIAL` role.
- Deliver a high-density, responsive Idea Matrix in Angular 21 PWA (PrimeNG 21, Tailwind CSS 4) with card scanning, hook badges, generation modal, edit drawer, version history viewer, and one-click selection.

**Non-Goals:**
- Script generation, scriptwriting, and script review (deferred to subsequent change `script-generation-review`). Selecting an idea only establishes downstream creative foundation eligibility; it does NOT create a `Script`.
- Storyboarding, ComfyUI visual assets, and TTS rendering (deferred to Wave 3).
- Vector databases, embeddings, or clustering infrastructure for deduplication.
- Framework version upgrades (the frontend remains locked to Angular 21, PrimeNG 21, Tailwind CSS 4).

## Decisions

### 1. Lineage to Exact Approved `TruthSourceVersionId`

**Decision**: `ContentIdea` persists `ContentItemId`, `TruthSourceId`, and `TruthSourceVersionId`.

**Rationale**: Later edits to the TruthSource that produce new TruthSource versions must never retroactively or silently mutate the factual basis of previously generated ideas. Every idea remains permanently anchored to the exact approved TruthSource snapshot from which it was synthesized.

### 2. Single Active Selected Idea & Atomic Replacement Semantics

**Decision**: Enforce at most one `ContentIdea` in status `Selected` per `ContentItem`.

**Rationale**: While multiple proposals can coexist in `Proposed` or `Dismissed` statuses, editorial pipeline progression requires a single unambiguous creative foundation for subsequent scriptwriting. When an operator selects idea B while idea A is currently selected:
1. Idea A's status reverts to `Proposed` and a `ContentIdeaVersion` snapshot logs the unselection.
2. Idea B's status becomes `Selected` and a `ContentIdeaVersion` snapshot logs the selection.
3. Both updates are executed within the same atomic database transaction.
4. The parent `ContentItem.LifecycleStage` remains `IdeaSelected`.

### 3. Full-Spectrum Concurrency (`Version: long`) and `ContentIdeaVersion` History

**Decision**:
- `Version: long` on `ContentIdea` is configured as an EF Core concurrency token (`.IsConcurrencyToken()`).
- All mutation endpoints (`Update`, `Select`, `Dismiss`, `Reopen`) require `expectedVersion`.
- If `idea.Version != request.ExpectedVersion`, the operation immediately returns `409 Conflict` (`CONCURRENCY_CONFLICT`), modifying no database state and creating no snapshots.
- A lightweight `ContentIdeaVersion` entity stores immutable historical snapshots of the idea's fields, status, author, and timestamp upon every valid state change.

### 4. Deterministic Duplicate AND Near-Duplicate Prevention

**Decision**: Implement a lightweight token-based similarity check in `ContentIdeaService`:
- Text is normalized (lowercased, punctuation stripped, whitespace collapsed, stopwords removed).
- Key token sets and Jaccard / Dice overlap are calculated across `Title`, `Angle`, `HookStrategy`, and `AudienceValue`.
- AI generation automatically filters out candidate ideas that have an exact or high similarity overlap (>= 0.70) with existing active ideas on the same `ContentItem`.
- Manual creation rejects candidate ideas that match existing active ideas with a clear 400 Bad Request / 409 validation response.

### 5. Backend Editorial Authorization

**Decision**: Decorate idea endpoints (`/api/content-items/{id}/ideas/*`) with `[Authorize(Policy = "RequireEditorial")]`.

**Rationale**: Protects AI generation, manual creation, editing, dismissal, and selection operations on the backend, while development GOD bypass continues to support end-to-end local testing.

### 6. AI Capability Routing for `generate_ideas`

**Decision**: Add capability `AiCapabilities.GenerateIdeas` (`"generate_ideas"`) to `IAiProviderRouter` with DeepSeek as default reasoning provider, Gemini configurable alternate, and deterministic offline development mock.

**Rationale**: Prompts inject the approved TruthSource summary, key ideas, verifiable claims, and do-not-say constraints, requesting a structured JSON array of 3-5 distinct creative ideas with diverse hook strategies. Telemetry records `TruthSourceVersionId`, latency, and tokens in `AIRecommendation` without private chain-of-thought.

### 7. Angular 21 PWA Version Lock and Idea Matrix UX

**Decision**: Build `ContentIdeasComponent` / `IdeaMatrixComponent` on Angular 21, PrimeNG 21, and Tailwind CSS 4 using zoneless Signals (`signal<ContentIdeaDto[]>`).

**Rationale**: Adheres strictly to the project's technology stack without framework drift, guaranteeing zero-lag reactivity, high information density, and seamless mobile responsiveness.

## Risks / Trade-offs

- **[Risk] Generic or repetitive AI ideas** → *Mitigation*: Prompt requires varied angles and specific hook strategies tailored to `IA Simple ES`, combined with deterministic near-duplicate filtering.
- **[Risk] Downstream drift before TruthSource approval** → *Mitigation*: Backend domain services strictly enforce `TruthSource.Status == Approved` before allowing idea generation or manual creation.
- **[Risk] Concurrent edits across any state transition** → *Mitigation*: Full-spectrum `expectedVersion` checking on all update, select, dismiss, and reopen mutations.
