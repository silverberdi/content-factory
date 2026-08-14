## Context

See `proposal.md` for business and editorial motivation.

The Content Factory application operates as a modular monolith on ASP.NET Core .NET 10 with MySQL persistence and an Angular 21 PWA frontend. Wave 0 established the application shell, security, channel registry, dashboard foundation, and audit event logging. This design details the implementation of the Discovery Source Registry and Candidate Triage workspace (Wave 1 intake foundation).

## Goals / Non-Goals

**Goals:**
- Provide a structured catalog entity (`DiscoverySource`) for channel-scoped external sources across multiple types (Feed/RSS, Web, Podcast, Curated, Manual).
- Ingest and normalize leads from both automated source adapters and manual submissions into a single canonical `DiscoveryCandidate` entity with mandatory channel attribution and provenance.
- Support manual submissions with external URLs OR pure text/notes (with nullable `ExternalUrl` and `NormalizedUrl`).
- Implement uniform URL normalization and per-channel candidate deduplication when a URL is present.
- Expose RESTful API endpoints for source management, manual candidate submission, and triage lifecycle transitions (`PendingReview`, `Promoted`, `Dismissed`).
- Persist the exact handoff state upon `Promoted` transition (`PromotedAtUtc`, `PromotedByEmail`, `EditorialNotes`), preserving immutable provenance for future pipeline stages.
- Provide a responsive Angular PWA Discovery workspace with high-density Candidate Triage view, quick preview slide-over drawer, and Source Catalog table.
- Integrate Discovery attention counters (pending unreviewed candidates, failing sources) and a "Quick Submit" modal ("Add a URL or note for discovery") into the Dashboard.
- Seed default AI/Tech discovery sources and initial candidates for the pilot channel `IA Simple ES`.

**Non-Goals:**
- Global discovery sources or candidates in CF-002 (all sources and candidates are explicitly channel-scoped via mandatory `ChannelId`).
- Implicitly generating downstream `TruthSource` or `ContentIdea` entities upon candidate promotion (deferred to subsequent editorial pipeline slices CF-010/CF-011).
- Automatic AI scoring or topic clustering (deferred to Wave 2 CF-022 / CF-023).
- Introducing external n8n workflows for feed parsing (kept safely in-process to avoid dev/prod n8n duplication and speculative infrastructure).
- Inventing speculative content hashing algorithms for text-only manual submissions in CF-002.

## Decisions

### 1. Modular Backend Architecture (`Modules/Discovery`)

We encapsulate discovery domain logic in `src/api/ContentFactory.Api/Modules/Discovery`:
- **Entities**:
  - `DiscoverySource`: represents a monitored content source. Fields: `Id` (Guid), `ChannelId` (Guid, required), `Name` (string), `OriginUrl` (string), `SourceType` (`Feed`, `Web`, `Podcast`, `Curated`, `Manual`, `ProviderApi`), `Language` (string), `PollingIntervalMinutes` (int), `Status` (`Active`, `Paused`, `Error`), `LastSyncAtUtc` (DateTime?), `NextSyncAtUtc` (DateTime?), `FailureCount` (int), `LastErrorMessage` (string?), `CreatedAtUtc` (DateTime), `UpdatedAtUtc` (DateTime).
  - `DiscoveryCandidate`: represents an ingested content lead. Fields: `Id` (Guid), `ChannelId` (Guid, required), `DiscoverySourceId` (Guid?, nullable for manual submissions), `ExternalUrl` (string?, nullable for text-only leads), `NormalizedUrl` (string?, nullable for text-only leads), `Title` (string), `Summary` (string?), `RawContent` (string?), `Language` (string), `Author` (string?), `DiscoveredAtUtc` (DateTime), `Status` (`PendingReview`, `Promoted`, `Dismissed`), `OriginType` (`Automated`, `Manual`), `SubmitterEmail` (string?), `DismissalReason` (string?), `EditorialNotes` (string?), `PromotedAtUtc` (DateTime?), `PromotedByEmail` (string?), `CreatedAtUtc` (DateTime).
- **Source Adapter Architecture**:
  - `ISourceSyncAdapter`: interface defining `Task<IReadOnlyList<DiscoveredItemDto>> FetchAsync(DiscoverySource source, CancellationToken ct)`.
  - `FeedSyncAdapter`: in-process syndication/feed implementation using `System.ServiceModel.Syndication` / `XmlReader` with strict HTTP timeouts (10s), custom user-agent, and error handling.
  - `SourceSyncService`: coordinates adapter resolution by `SourceType` and handles candidate persistence and deduplication.
  - `DiscoveryBackgroundSyncService`: background `IHostedService` executing periodic polling for active sources due for sync.

### 2. Candidate Promotion Semantics & Pipeline Boundary

The canonical editorial pipeline order is:
`Discovery → Source Ingestion → Truth Source → Content Idea → Script`

For this change:
- `Promoted` is an explicit triage state indicating an operator has evaluated and accepted a `DiscoveryCandidate` for entry into the editorial production backlog.
- **Persisted handoff state**: `Status = Promoted`, `PromotedAtUtc = DateTime.UtcNow`, `PromotedByEmail = <operator email>`, `EditorialNotes = <optional string>`.
- The candidate preserves all original provenance and raw content immutably.
- **Boundary rule**: Promotion in this slice does NOT create `TruthSource` or `ContentIdea` records. Future Wave 1 slices (CF-010/CF-011) will ingest promoted candidates to instantiate `TruthSource`.

### 3. Intake Normalization and URL Deduplication

Manual URL/text submissions and automated source fetches share the same intake pipeline:
- Both flow through `IDiscoveryService.IngestCandidateAsync()`.
- **URL Leads**: When an external URL is provided:
  - Normalize URL: lowercase scheme and host, strip tracking query parameters (`utm_*`, `fbclid`, `gclid`, `ref`, etc.), trim trailing slashes and fragment identifiers.
  - Deduplication: enforce uniqueness per `(ChannelId, NormalizedUrl)`. If a candidate already exists in the channel, refresh its `DiscoveredAtUtc` without creating duplicate records.
- **Text-Only Leads**: When an operator enters a manual note/text without a URL:
  - `ExternalUrl = null` and `NormalizedUrl = null`.
  - Stored as a valid `Manual` `DiscoveryCandidate` with `SubmitterEmail`, `DiscoveredAtUtc`, and title/summary preserved.
  - No hashing or artificial deduplication is applied to text leads in CF-002.
- Cross-channel submissions of the same URL remain independent because all candidates are strictly scoped by `ChannelId`.

### 4. API Contract & Security

Endpoints in `DiscoveryController`:
- `GET /api/discovery/sources`: List sources (filter by channel, status). Requires `TECHNICAL` or `EDITORIAL`.
- `POST /api/discovery/sources`: Register source for a channel (`ChannelId` required). Requires `TECHNICAL` or `EDITORIAL`.
- `PUT /api/discovery/sources/{id}`: Update source config/status. Requires `TECHNICAL` or `EDITORIAL`.
- `DELETE /api/discovery/sources/{id}`: Delete source. Requires `TECHNICAL`.
- `POST /api/discovery/sources/{id}/sync`: Trigger immediate on-demand sync. Requires `TECHNICAL` or `EDITORIAL`.
- `GET /api/discovery/candidates`: List candidates with pagination and filters (`channelId` required or queried, `status`, `sourceId`, `search`).
- `POST /api/discovery/candidates/manual`: Quick Submit manual URL or text note (`ChannelId` required). Requires `TECHNICAL` or `EDITORIAL`.
- `POST /api/discovery/candidates/{id}/triage`: Transition candidate state (`Promoted` or `Dismissed` with optional reason/editorial note).
- Audit events logged for: `DiscoverySource.Created`, `DiscoverySource.Updated`, `DiscoverySource.Deleted`, `DiscoverySource.Synced`, `DiscoveryCandidate.Submitted`, `DiscoveryCandidate.Promoted`, `DiscoveryCandidate.Dismissed`.

### 5. Frontend Workspace & Responsive UX

- Routing in Angular:
  - `/discovery/triage`: Primary operational triage workspace with filter pills (All, Pending, Promoted, Dismissed), channel dropdown, search bar, and candidate cards/table.
  - `/discovery/sources`: Discovery source catalog with health badges, last sync timestamps, manual sync buttons, and source creation drawer.
- UI Components (PrimeNG + Tailwind):
  - `CandidatePreviewDrawer`: Slide-over drawer on desktop and bottom sheet on mobile for instant article summary inspection, provenance metadata review, and single-click Promote / Dismiss.
  - `QuickSubmitModal`: Accessible globally from shell header and dashboard attention widget with label "Quick Submit" and prompt "Add a URL or note for discovery".
  - Signal-based state management in `DiscoveryStateService` for responsive UI updates without full reloads.

### 6. n8n Boundary Compliance

In accordance with canonical system architecture:
- RSS and web source ingestion is executed in-process via `FeedSyncAdapter`.
- Zero external n8n dev workflows are introduced.
- Future complex orchestrations (such as headless browser scraping or external AI web crawling) will integrate through backend job adapters when defined by future changes.

### 7. Seed Data Policy

`DatabaseInitializer` seeds:
- Spanish-language AI/Tech discovery sources explicitly mapped to `IA Simple ES` (e.g. Xataka IA, Genbeta IA, OpenAI News, MIT Tech Review ES).
- 5-8 pre-seeded discovery candidates (both URL leads and manual note leads) for `IA Simple ES` to allow immediate human validation upon first launch.

## Risks / Trade-offs

- **[Risk] External feed timeouts or malformed XML crashing background worker**
  → *Mitigation*: Wrap feed requests in 10-second timeout cancellation tokens and resilient try/catch blocks that record `LastErrorMessage` and increment `FailureCount` without crashing the host.
- **[Risk] Triage queue overload from high-frequency sources**
  → *Mitigation*: Configurable polling intervals (default 60 mins), candidate deduplication on URLs, and batch dismiss/triage options.
- **[Risk] UI clutter on mobile during rapid triage**
  → *Mitigation*: High-density mobile cards with prominent touch actions and full-screen bottom-sheet preview.

## Migration Plan

1. Add `DbSet<DiscoverySource>` and `DbSet<DiscoveryCandidate>` to `AppDbContext`.
2. Configure schema constraints, foreign keys (`ChannelId` required), and indices in `OnModelCreating`.
3. Update `DatabaseInitializer` to apply migrations or `EnsureCreated()` and seed default sources and candidates for `IA Simple ES`.
4. Verify backward compatibility with existing channels and users.
