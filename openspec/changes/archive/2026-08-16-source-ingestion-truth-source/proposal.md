## Why

To transform raw discovered leads and external URLs into high-quality original short-form video scripts, Content Factory operators need a structured evidence layer grounded in verified facts, real captured evidence snapshots, and editorial safety constraints. Building the `ContentItem` operational identity, in-process evidence capture (with raw/extracted text, SHA-256 hashes, and MinIO storage for large artifacts), automated AI-assisted evidence synthesis (`build_truth_source`) producing draft proposals, MySQL-compatible optimistic concurrency (`Version: long`), and human review/approval workflow establishes the authoritative truth foundation required before downstream script generation, ensuring no video script or ideas can be drafted or approved without an approved `TruthSource`.

## What Changes

- **ContentItem Operational Identity**: Introduces the `ContentItem` entity as the operational anchor for one editorial content effort linking evidence snapshots, TruthSource, editorial tasks, and AI recommendations without becoming a monolithic aggregate across future pipeline stages.
- **Real Evidence Capture & Ingestion**: Implements an in-process evidence capture boundary (`IEvidenceCaptureService`) that captures source material for URL leads (with raw payload, extracted text, SHA-256 hash, and MinIO object storage for large payloads) and direct text leads (hashing submitted content without requiring URLs). Ingestion failure records a truthful operational failure state with retry capability without fabricating evidence.
- **Immutable Evidence Snapshots & Non-Destructive Association**: Preserves immutable evidence snapshots (`ContentItemEvidence`). Unused evidence can be detached, while evidence contributing to a `TruthSourceVersion` remains permanently traceable in history; exclusion from active working set uses non-destructive status (`Active`, `Excluded`).
- **Preserved DiscoveryCandidate Promotion Semantics**: Keeps `DiscoveryCandidate.Promoted` strictly as "accepted for continuation into the editorial pipeline". Initiating a new `ContentItem` or attaching candidate evidence to an existing one is a separate downstream operation with duplicate/retry protection.
- **AI-Assisted Draft Synthesis (`build_truth_source`)**: Capability-based AI routing (`IAiProviderRouter` with seeded DeepSeek default, Gemini configuration, and local deterministic mock) synthesizes structured *draft* proposals only. AI output never automatically becomes an approved TruthSource.
- **Structured `AIRecommendation` Audit Log**: Captures structured AI decision telemetry (capability, provider, model, prompt version, structured recommendation, confidence, evidence references, usage/cost/latency, accepted/rejected state) without requesting or storing private chain-of-thought.
- **Human-Approved TruthSource with MySQL Concurrency**: Implements the canonical `TruthSource` schema. Concurrency uses an explicit application-managed `Version: long` token (EF Core concurrency token returning HTTP 409 Conflict on stale writes). Human edits create versioned snapshots (`TruthSourceVersion`) preserving the exact supporting evidence IDs. Approval requires an authorized human action (`EDITORIAL` role).
- **Downstream Approval Gate**: Establishes the core invariant that only an `Approved` TruthSource is eligible for downstream generation (ContentIdea generation is explicitly out of scope for this change).
- **EditorialTask & Dashboard Attention**: Models concrete human action items (`EditorialTask`) for TruthSource review without generic task management or email inbox metaphors, surfacing critical items in Dashboard Attention while keeping review execution contextual within the Content Workspace / Review Studio.

## Capabilities

### New Capabilities
- `content-workspace`: Operational workspace for `ContentItem` production threads, lifecycle stage tracking, in-process evidence capture (URL/text), and immutable evidence snapshot management.
- `truth-source-evidence`: Structured editorial evidence layer, AI-assisted `build_truth_source` draft generation, MySQL-compatible `Version: long` concurrency, versioned human editing, and formal approval/rejection lifecycle.
- `editorial-task-attention`: Human-action task management (`EditorialTask`) for TruthSource review assignments, deadlines, priority, and completion tracking.

### Modified Capabilities
- `discovery-candidate-triage`: Adds actions to initiate a new `ContentItem` from a promoted candidate or attach promoted candidate evidence to an existing `ContentItem` while preserving `Promoted` domain semantics and preventing duplicate ContentItems.
- `application-shell-dashboard`: Integrates Content Workspace navigation in the shell and adds TruthSource review attention items and pipeline stage metrics to the dashboard control center.

## Impact

- **Backend**:
  - New database tables: `content_items`, `content_item_evidence`, `truth_sources`, `truth_source_versions`, `editorial_tasks`, and `ai_recommendations`.
  - Concurrency token: `Version` (`long`) on mutable entities with EF Core concurrency checking.
  - In-process evidence capture service with raw/extracted text, SHA-256 hash calculation, and optional MinIO storage.
  - New API endpoints under `/api/content-items`, `/api/truth-sources`, `/api/editorial-tasks`, and `/api/ai/truth-source-generation`.
  - `IAiProviderRouter` with seeded DeepSeek reasoning default and offline deterministic mock adapter.
  - Audit logging for all lifecycle transitions (`Draft`, `UnderReview`, `Approved`, `Rejected`).
- **Frontend (Angular PWA)**:
  - New Content Workspace module with list, filter, and detail views for `ContentItem`.
  - Side-by-side TruthSource Review Studio comparing immutable source evidence vs structured claims/constraints, with 409 Conflict reload/reconciliation handling.
  - Interactive approval/rejection drawer with mandatory rejection reasoning.
  - Dashboard Attention widget extension showing pending TruthSource review tasks.
- **Security & Authorization**:
  - `EDITORIAL` operators can create content items, trigger AI synthesis, edit truth sources, and approve/reject evidence.
  - `TECHNICAL` operators can configure AI provider routing and monitor generation latency/costs.
