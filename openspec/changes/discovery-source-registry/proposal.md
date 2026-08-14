## Why

Content Factory operators need a structured, high-signal discovery layer to register monitored external sources, accept manual URL and text/note submissions, and evaluate content leads in an operational triage workspace per channel (starting with IA Simple ES). Establishing a unified discovery source catalog and candidate triage workspace provides the normalized evidence intake and provenance required before downstream editorial stages (TruthSource, ContentIdea, Script) in Wave 1.

## What Changes

- **Discovery Source Catalog**: Technical and editorial operators can register, edit, pause, and monitor external discovery sources (supporting source types such as Feed, Web, Curated, and Manual) with strict channel-specific attribution (`ChannelId` required), polling schedules, health metrics, and audit logging. Ingestion is handled via a provider-agnostic source adapter architecture, with an in-process syndication/feed adapter as the first concrete implementation.
- **Discovery Candidate Ingestion & Triage Workspace**: Normalization and review workspace for incoming content candidates from both automated source polling and manual URL or text/note submissions. Every candidate is channel-scoped and preserves mandatory provenance (origin URL when present, normalized URL when present, source type, language, discovered timestamp, submitting actor or source provider, snippet/content).
- **Manual URL or Text Intake**: Manual submissions support external URLs OR pure text/note leads. For URL leads, canonical tracking parameters are stripped and per-channel URL deduplication applies. For text-only leads with no URL, candidates are persisted with full provenance and submitter identity without requiring a URL or speculative hashing.
- **Explicit Candidate Promotion Semantics**: An operator can evaluate candidates and transition them between `PendingReview`, `Dismissed` (with reason), and `Promoted`. In this change, `Promoted` explicitly means the candidate is accepted for continuation into the editorial pipeline with full provenance attached; it records `PromotedAtUtc`, `PromotedByEmail`, and optional editorial notes, leaving the candidate in a persisted handoff state ready for subsequent editorial changes without creating downstream `TruthSource` or `ContentIdea` entities in this slice.
- **Dashboard & Shell Integration**: Extends the application shell with Discovery navigation and augments the dashboard with Discovery attention metrics (unreviewed candidate counts, source health) and a "Quick Submit" action (adding a URL or note for discovery) distinct from persistent source catalog creation.
- **n8n Boundary Compliance**: Ingestion parsing remains in-process for this slice, adhering to canonical architecture with zero development/production n8n workflow duplication.
- **Realistic Seed Data**: Seeds verified AI/Tech discovery sources and initial discovery candidates mapped to the default channel `IA Simple ES` (Spanish language).

## Capabilities

### New Capabilities
- `discovery-source-catalog`: Registry and lifecycle management of channel-scoped external discovery sources (Feed, Web, Curated, Manual), polling health tracking, and operational management.
- `discovery-candidate-triage`: Channel-scoped triage workspace and normalization engine for discovery candidates with mandatory provenance, manual URL/text submission intake, and operational triage (PendingReview, Promoted, Dismissed).

### Modified Capabilities
- `application-shell-dashboard`: Adds Discovery navigation in the application shell and incorporates Discovery attention metrics (unreviewed candidate counts, source health) and a Quick Submit action ("Add a URL or note for discovery") into the dashboard control center.

## Impact

- **Backend**:
  - New database tables: `discovery_sources` and `discovery_candidates` (both requiring `channel_id`).
  - `discovery_candidates.external_url` and `normalized_url` are nullable to support text-only leads.
  - New API endpoints under `/api/discovery/sources` and `/api/discovery/candidates` with role-based authorization (`TECHNICAL` / `EDITORIAL`).
  - Source-type agnostic architecture with an in-process syndication feed adapter.
  - Audit logging for all source registrations, updates, and candidate triage actions (`Promoted`, `Dismissed`).
- **Frontend (Angular PWA)**:
  - New Discovery feature module with Source Registry and Candidate Triage workspace views.
  - Candidate preview and triage drawer utilizing PrimeNG components and Tailwind responsive layout.
  - "Quick Submit" modal ("Add a URL or note for discovery") accessible globally and from dashboard.
  - Dashboard Attention widget extension showing pending discovery items.
- **Security & Authorization**:
  - `EDITORIAL` operators can register sources, submit manual candidates, and triage/promote candidates.
  - `TECHNICAL` operators can configure sync intervals, pause/resume sources, and view feed errors.
  - Development GOD mode retains full access.
