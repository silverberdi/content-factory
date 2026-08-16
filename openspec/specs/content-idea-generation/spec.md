# Content Idea Generation Specification

## Purpose

Defines the `ContentIdea` editorial entity, immutable lineage to the exact approved `TruthSourceVersion`, AI idea generation capability (`generate_ideas`) via configured provider routing, deterministic duplicate and near-duplicate prevention, lightweight version history (`ContentIdeaVersion`), human curation lifecycle (`Proposed`, `Selected`, `Dismissed`), single active selection replacement semantics, full-spectrum mutation concurrency, backend authorization, and the downstream progression gate to scriptwriting.

## Requirements

### Requirement: Structured ContentIdea schema and immutable TruthSourceVersion lineage

A `ContentIdea` SHALL represent a creative editorial angle derived from an approved `TruthSource` and linked to a parent `ContentItem`. Each `ContentIdea` SHALL record immutable lineage: `ContentItemId`, `TruthSourceId`, and the exact `TruthSourceVersionId` (or approved version token) that served as its factual basis. Each `ContentIdea` SHALL include: a title/headline, angle (perspective or framing), hook strategy (pattern interrupt for 0-3s retention), audience value (viewer benefit/takeaway), format (e.g. "YouTube Short 30-60s", "Reel"), intended outcome (e.g. "Educational", "Curiosity", "Actionable Tip"), freshness class (`Breaking`, `Timely`, `Evergreen`), priority (`Low`, `Normal`, `High`, `Urgent`), rationale, status (`Proposed`, `Selected`, `Dismissed`), creator/generator attribution, and a `Version` (`long`) concurrency token. Later edits or new versions of the TruthSource SHALL NOT alter the factual basis or lineage of existing ideas.

#### Scenario: ContentIdea schema and lineage completeness
- **WHEN** an idea proposal is generated or created
- **THEN** it contains all canonical fields: title, angle, hook strategy, audience value, format, intended outcome, freshness class, priority, rationale, status "Proposed", and `Version: 1`
- **AND** it is explicitly associated with `ContentItemId`, `TruthSourceId`, and the exact approved `TruthSourceVersionId`.

#### Scenario: Manual idea creation against approved TruthSource version
- **WHEN** an operator manually creates a ContentIdea for a ContentItem with an approved TruthSource
- **THEN** the system validates required fields (title, angle, hook strategy), links the idea to the currently approved `TruthSourceVersionId`, and persists the idea in "Proposed" status with operator attribution
- **AND** an audit event is logged with action "ContentIdea.Created".

### Requirement: AI-assisted idea generation (`generate_ideas`) and AIRecommendation audit

The system SHALL support invoking the capability `generate_ideas` through `IAiProviderRouter` to produce multiple diverse, creative `ContentIdea` proposals from the parent `ContentItem`'s approved `TruthSource`. AI execution SHALL record an `AIRecommendation` audit record capturing `ContentItemId`, exact input `TruthSourceVersionId`, capability ("generate_ideas"), provider, model, prompt-policy version, structured ideas output, token usage, latency, estimated cost, and accepted/rejected state without requesting or persisting private chain-of-thought.

#### Scenario: AI generates multiple distinct idea proposals from approved TruthSource
- **WHEN** an operator triggers "Generate Ideas" on a ContentItem with an approved TruthSource
- **THEN** `IAiProviderRouter` routes the prompt to the configured provider (defaulting to DeepSeek reasoning or development mock adapter)
- **AND** the AI synthesizes at least 3 distinct idea proposals with varied angles, hook styles, and audience hooks
- **AND** the ideas are persisted in "Proposed" status linked to the ContentItem and `TruthSourceVersionId`
- **AND** an `AIRecommendation` telemetry record is stored linking to the input `TruthSourceVersionId`.

#### Scenario: AI idea generation blocked without approved TruthSource
- **WHEN** an operator attempts to trigger AI idea generation on a ContentItem whose TruthSource is missing, "Draft", "UnderReview", or "Rejected"
- **THEN** the system rejects the request with error "ContentIdea generation requires an approved TruthSource"
- **AND** no AI call is dispatched.

#### Scenario: Deterministic development mock idea generation
- **WHEN** running in development environment without live provider credentials
- **THEN** `IAiProviderRouter` uses a deterministic mock adapter to generate valid Spanish AI/Tech short-form ideas for channel "IA Simple ES".

### Requirement: Deterministic duplicate and near-duplicate idea prevention

The system SHALL implement a deterministic, application-level similarity algorithm across normalized fields (Title, Angle, HookStrategy, AudienceValue) to prevent duplicate and obviously equivalent idea proposals from being persisted within the same `ContentItem`. Candidate ideas whose similarity score or normalized key token sets match an existing active idea on the same `ContentItem` SHALL be filtered out prior to persistence (for AI generation) or rejected on manual creation with a clear validation conflict.

#### Scenario: Filter duplicate and near-duplicate generated proposals
- **WHEN** AI idea generation produces an idea proposal that is identical or materially equivalent in angle, hook, and audience value to an existing proposal on the ContentItem
- **THEN** the system filters out the duplicate candidate without error and persists only the novel proposals.

#### Scenario: Reject duplicate or near-equivalent manual idea creation
- **WHEN** an operator attempts to manually save an idea whose title, angle, or hook strategy is materially equivalent to an existing idea on that ContentItem
- **THEN** the system rejects the creation with validation error "An idea with a materially equivalent angle or hook already exists for this piece".

### Requirement: Single active idea selection and atomic replacement semantics

A `ContentItem` MAY have multiple `Proposed` and `Dismissed` ideas, but SHALL have AT MOST ONE active `Selected` idea at any given time. Selecting a `ContentIdea` SHALL mark it as the sole active creative foundation for subsequent scripting and advance the parent `ContentItem` lifecycle stage to `IdeaSelected`. If another idea was previously selected, selecting a new idea SHALL atomically replace the active selection within the same operation: the previously selected idea transitions back to `Proposed` and the new idea becomes `Selected`, with `ContentIdeaVersion` history snapshots recorded for both affected entities.

#### Scenario: Select first idea for downstream scripting
- **WHEN** an operator selects a "Proposed" ContentIdea for a ContentItem in "TruthSourceApproved" providing the expected version
- **THEN** the idea status transitions to "Selected"
- **AND** `SelectedAtUtc` and `SelectedByEmail` are persisted
- **AND** the parent ContentItem lifecycle stage advances to "IdeaSelected"
- **AND** an audit event is logged with action "ContentIdea.Selected".

#### Scenario: Replace active selected idea atomically
- **WHEN** an operator selects a new "Proposed" idea while another idea on the ContentItem is already "Selected"
- **THEN** the previously selected idea is reverted to "Proposed" and the new idea becomes "Selected" within the same atomic transaction
- **AND** `ContentIdeaVersion` snapshots are recorded for both affected ideas
- **AND** the ContentItem remains in stage "IdeaSelected" with exactly one active creative foundation
- **AND** an audit event is logged with action "ContentIdea.SelectionReplaced".

#### Scenario: Dismiss unviable idea with notes
- **WHEN** an operator dismisses a "Proposed" ContentIdea providing its expected version and optional dismissal notes
- **THEN** the idea status transitions to "Dismissed"
- **AND** a `ContentIdeaVersion` snapshot is recorded
- **AND** dismissal notes and actor email are recorded
- **AND** an audit event is logged with action "ContentIdea.Dismissed".

#### Scenario: Re-open dismissed idea
- **WHEN** an operator restores a "Dismissed" ContentIdea providing its expected version
- **THEN** the idea status reverts to "Proposed"
- **AND** a `ContentIdeaVersion` snapshot is recorded
- **AND** it becomes eligible for selection.

### Requirement: Full-spectrum mutation concurrency and immutable editorial history

The system SHALL require the current `expectedVersion` concurrency token for ALL mutable editorial transitions (Update, Select/Replace, Dismiss, Reopen). If the database version does not match `expectedVersion`, the backend SHALL reject the request with machine-readable HTTP 409 Conflict (`CONCURRENCY_CONFLICT`), applying NO mutations and creating NO version snapshots. For valid mutations, the system SHALL increment `Version: long` and persist an immutable `ContentIdeaVersion` snapshot recording the full state, change summary, author, and timestamp.

#### Scenario: Version increment and snapshot on idea edit
- **WHEN** an operator updates the fields of a ContentIdea at version 1 providing expectedVersion 1
- **THEN** the idea record is updated with version 2
- **AND** a `ContentIdeaVersion` snapshot of version 1 is preserved in historical lineage
- **AND** the updated timestamp and actor email are recorded.

#### Scenario: Stale write rejection with 409 Conflict across all mutation endpoints
- **WHEN** an operator attempts an Update, Select, Dismiss, or Reopen action providing version 1 when the database has progressed to version 2
- **THEN** the update is rejected with HTTP 409 Conflict and code "CONCURRENCY_CONFLICT"
- **AND** the database state remains unchanged and no spurious version history is logged
- **AND** the frontend surfaces the conflict for user reload/reconciliation.

### Requirement: Backend authorization for content idea editorial actions

All mutation endpoints for ContentIdea (AI idea generation, manual creation, editing, dismissal, re-opening, and selection/replacement) SHALL be explicitly authorized on the backend requiring the `EDITORIAL` role (or development GOD mode).

#### Scenario: Non-editorial user cannot generate or select ideas
- **WHEN** a user without the `EDITORIAL` role attempts to invoke idea generation or idea selection
- **THEN** the backend rejects the request with HTTP 403 Forbidden.

### Requirement: Angular 21 Idea Matrix & Review UI

The frontend SHALL provide an Idea Matrix within the Content Workspace / Detail view on Angular 21 (PrimeNG 21, Tailwind CSS 4) featuring side-by-side card scanning, hook strategy highlight badges, AI generation modal, inline editing drawer, version history viewer, and one-click selection/dismissal actions.

#### Scenario: High-density idea comparison on desktop
- **WHEN** an operator views the Ideas tab of a ContentItem on desktop (viewport >= 1280px)
- **THEN** ideas are displayed in a responsive grid comparing angles, hook techniques, format, and audience value
- **AND** the currently selected idea (if any) is clearly highlighted with an active badge
- **AND** action buttons for "Select for Scripting", "Edit", and "Dismiss" are easily accessible on each card.

#### Scenario: Responsive mobile idea card actions
- **WHEN** an operator accesses the Ideas view on a mobile viewport (~390px)
- **THEN** ideas render as compact stacked cards with clear touch actions for rapid evaluation and selection.
