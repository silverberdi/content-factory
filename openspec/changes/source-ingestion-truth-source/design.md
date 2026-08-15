## Context

The previous change established the Discovery layer (Source Catalog and Candidate Triage) where raw leads and external feeds are ingested, normalized, and triaged. To progress into Wave 1 of the Content Factory roadmap, the system must establish the `ContentItem` operational identity, perform real in-process evidence capture (URL/text), persist immutable evidence snapshots, synthesize structured draft proposals via the AI Provider Router (`build_truth_source`), provide versioned human editorial editing with MySQL-compatible optimistic concurrency (`Version: long`), and enforce strict approval/rejection gates before downstream script generation can begin.

See `proposal.md` for motivation and background context.

## Goals / Non-Goals

**Goals:**
- Implement the `ContentItem` operational identity to track one editorial content effort per channel.
- Maintain clear modular boundaries for `ContentItem` (referencing `ContentItemId`) rather than creating a monolithic aggregate that loads/locks the entire future pipeline.
- Implement real in-process evidence capture (`IEvidenceCaptureService`) for URL and text leads, computing SHA-256 content hashes, extracting text, storing large payloads in MinIO when appropriate, and recording truthful failure states on fetch errors without fabricating content.
- Preserve the established semantics of `DiscoveryCandidate.Promoted` (operator accepted candidate for continuation), treating ContentItem creation or evidence attachment as distinct downstream operations with duplicate prevention.
- Enforce non-destructive evidence history: uncommitted evidence can be detached, while evidence contributing to a `TruthSourceVersion` is preserved historically (setting status to `Excluded` if removed from current working set).
- Implement MySQL-compatible optimistic concurrency using an explicit application-managed `Version: long` token configured as an EF Core concurrency token, returning machine-readable HTTP 409 Conflict on stale edits.
- Provide a capability-based AI Provider Router with seeded DeepSeek reasoning default and offline deterministic development mock to synthesize structured `TruthSource` *draft proposals* via `build_truth_source`.
- Store structured `AiRecommendation` telemetry (provider, model, token usage, latency, estimated cost, structured recommendation, concise rationale) without persisting raw chain-of-thought.
- Enforce the canonical `TruthSource` schema: summary, key ideas, verifiable claims with citations, evidence references (supporting evidence IDs), risk notes, do-not-say constraints, possible angles, and Spanish localization notes.
- Require authorized human approval (`EDITORIAL` role) for a TruthSource to transition to `Approved`; require non-empty reasons for rejection; keep rejected/superseded drafts traceable.
- Enforce the downstream gate: only an `Approved` TruthSource is eligible for future downstream generation.
- Model concrete human-action `EditorialTask` items for TruthSource reviews and integrate them into dashboard Attention without building a generic task management or inbox product.
- Build the responsive Angular PWA Content Workspace and TruthSource Review Studio with 409 conflict reconciliation.

**Non-Goals:**
- ContentIdea generation (CF-012) – explicitly out of scope.
- Script generation (CF-012) – explicitly out of scope.
- Video production, Comfy rendering, TTS, or YouTube publishing – reserved for Waves 3 & 4.
- Generic scraping platform or external crawler cluster.
- Generic task-management product or email/inbox metaphor.
- Monolithic EF aggregate loading all downstream pipeline state.
- Database-provider-specific rowversion abstractions (SQL Server timestamp / rowversion).

## Decisions

### Decision 1: Entity Boundaries and Immutable Evidence Snapshots
- **Choice**: `ContentItem` serves as the operational identity of one editorial effort. Evidence items are attached as immutable `ContentItemEvidence` records referencing `ContentItemId`.
- **Schema**:
  - `content_items` (`id`, `channel_id`, `title`, `stage`, `status`, `created_at_utc`, `created_by_email`, `updated_at_utc`, `version`)
  - `content_item_evidence` (`id`, `content_item_id`, `discovery_candidate_id`, `origin_url`, `title`, `role`, `status`, `raw_content`, `object_storage_key`, `extracted_text`, `content_hash`, `error_message`, `captured_at_utc`, `created_by_email`)
- **Evidence Capture Boundary**: `IEvidenceCaptureService` performs in-process HTTP fetching and basic article text extraction for URLs. Text leads are stored directly without external HTTP. SHA-256 hash is computed for all captured payloads. Large payloads are stored in MinIO with object key recorded in `object_storage_key`.
- **Failure Handling**: If URL extraction fails, `status` is set to `CaptureFailed`, storing `error_message` and preserving candidate provenance. No AI or synthetic content is fabricated. A retry endpoint (`POST /api/content-items/{id}/evidence/{evidenceId}/retry`) allows re-attempting capture.
- **Traceability**: If an evidence item contributed to an existing `TruthSourceVersion`, removing it from the active bundle updates `status` to `Excluded` rather than deleting the row.

### Decision 2: MySQL-Compatible Optimistic Concurrency
- **Choice**: Use an explicit application-managed `Version: long` on `TruthSource` (and `ContentItem`), configured in EF Core with `.IsConcurrencyToken()`.
- **Rationale**: MySQL does not have native byte[] rowversion like SQL Server. Explicit `Version: long` is database-provider agnostic, completely transparent, and maps cleanly to JSON API contracts (`expectedVersion` field).
- **Concurrency Workflow**:
  1. Frontend loads TruthSource with `version: 1`.
  2. Operator submits update with `{ ..., expectedVersion: 1 }`.
  3. Service checks `entity.Version == expectedVersion` and increments `entity.Version = expectedVersion + 1`.
  4. If mismatch or EF Core concurrency exception occurs, API returns HTTP 409 Conflict with `{ "code": "CONCURRENCY_CONFLICT", "message": "The TruthSource was modified by another operator. Please reload latest version.", "currentVersion": N }`.
  5. Frontend Review Studio catches 409 and prompts operator to reload and reconcile changes.

### Decision 3: TruthSource Flow: Human-Approved Truth (Not AI Truth)
- **Choice**: Flow: Raw Evidence → Extracted/Captured Evidence (`IEvidenceCaptureService`) → AI-assisted draft synthesis (`build_truth_source`) → Draft TruthSource → Human review/edit → Approved TruthSource.
- **Rationale**: AI generation only creates a draft proposal. Authorized human review (`EDITORIAL` role) is strictly required to reach `Approved` status. Edits create a new `TruthSourceVersion` record (recording snapshot JSON, author email, and list of supporting `ContentItemEvidence` IDs). Rejections require mandatory explanation, and rejected drafts remain queryable in history.

### Decision 4: Capability-Based AI Router with Development Mock
- **Choice**: Implement `IAiProviderRouter` with capability `build_truth_source`. Seed `DeepSeek` as global default reasoning provider (`deepseek-chat` / `deepseek-reasoner`), allow `Gemini` configuration, and provide an offline deterministic mock adapter when API keys are absent.
- **Rationale**: Adheres to canonical AI architecture ("Capability is domain; provider is configuration") and allows automated tests and human verification without requiring live external API credentials.

### Decision 5: Structured Decision Telemetry (`AiRecommendation`)
- **Choice**: Table `ai_recommendations` records: `content_item_id`, `capability`, `provider`, `model`, `prompt_version`, `structured_output_json`, `confidence`, `rationale`, `latency_ms`, `tokens_in`, `tokens_out`, `estimated_cost_usd`, and `accepted_state`. Never requests or stores private chain-of-thought.

### Decision 6: EditorialTask as Operational Attention
- **Choice**: Table `editorial_tasks` records concrete human action items (e.g. `ReviewTruthSource`) linked to `ContentItemId`. Surfaced in Dashboard Attention; review execution occurs contextually inside the Review Studio.

## Risks / Trade-offs

- **[Risk] Remote URL extraction failure or anti-scraping blocks** → *Mitigation*: In-process HTTP client extracts main text; on failure, records truthful `CaptureFailed` status with error details, allowing manual text fallback or retry without blocking the pipeline or fabricating false evidence.
- **[Risk] LLM hallucination or ungrounded claims in generated TruthSource draft** → *Mitigation*: AI synthesis prompt explicitly instructs model to produce verifiable claims tied strictly to evidence items; Review Studio presents evidence side-by-side; approval strictly requires human operator sign-off (`EDITORIAL` role).
- **[Risk] Missing API keys during local development or CI testing** → *Mitigation*: `IAiProviderRouter` automatically uses a deterministic mock adapter in development mode, ensuring offline verification works seamlessly.
- **[Risk] Concurrent edits by multiple editors** → *Mitigation*: Explicit `Version: long` token check returns HTTP 409 Conflict with actionable recovery dialog in Review Studio to reload and reconcile changes.

## Migration Plan

1. **Database Schema**:
   - Apply EF Core migration to create `content_items`, `content_item_evidence`, `truth_sources`, `truth_source_versions`, `editorial_tasks`, and `ai_recommendations` tables with `Version` (`long`) columns.
2. **Seed Data**:
   - Seed sample `ContentItem` records with attached captured evidence snapshots (URL & text), draft/approved `TruthSource` records, and `EditorialTask` items for `IA Simple ES`.
3. **Rollback Strategy**:
   - Migrations are reversible via standard EF Core down-migrations without affecting existing tables.
