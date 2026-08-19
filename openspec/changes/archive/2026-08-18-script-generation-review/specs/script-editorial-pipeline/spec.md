# Script Editorial Pipeline Specification

## Purpose

Defines the `Script`, `ScriptScene`, and `ScriptSceneEvidenceReference` domain entities, configurable speaking-rate pacing estimation, explicit factual claim traceability to the approved `TruthSourceVersion`, upstream lineage invalidation / stale-script semantics, advisory AI script synthesis (`generate_script`) and critique (`review_script`) via configured provider routing, immutable `ScriptVersion` snapshot history, unambiguous review/rejection/reopen lifecycle (`Draft`, `UnderReview`, `Approved`, `Rejected`, `Reopen`), strict rejection reason auditing, full-spectrum optimistic concurrency, backend authorization, and the Angular 21 Script Studio.

## ADDED Requirements

### Requirement: Structured Script schema, configurable pacing, and explicit factual claim lineage

A `Script` SHALL represent the concrete editorial production script for a parent `ContentItem`, derived from the piece's active `Selected` `ContentIdea` and its approved `TruthSource`. Each `Script` SHALL record immutable lineage: `ContentItemId`, `ChannelId`, `ContentIdeaId`, `ContentIdeaVersionId`, `TruthSourceId`, and the exact `TruthSourceVersionId` that established its factual boundary. Each `Script` SHALL include: a title, target duration seconds (default 30-60s for short-form video), effective `PacingWpm` (words per minute, resolved from channel/format configuration, default 140 WPM for `IA Simple ES`), estimated duration seconds (`Math.Round((TotalWordCount / (PacingWpm / 60.0)), 1)`), total word count, language code (e.g. "es-ES"), status (`Draft`, `UnderReview`, `Approved`, `Rejected`), rejection reason (when rejected), creator/editor attribution, and a `Version` (`long`) optimistic concurrency token. Changing the channel's configured WPM SHALL NOT require modifying Script domain logic. Spoken duration SHALL be modeled strictly as an editorial estimate, decoupled from future measured TTS/audio duration.

A `Script` SHALL contain an ordered collection of `ScriptScene` (Beat) items (`OrderIndex`, `SceneType` [`Hook`, `Problem`, `Insight`, `Climax`, `CallToAction`], `NarrationText`, `VisualPrompt`, `EstimatedDurationSeconds`, `WordCount`). To preserve explicit factual lineage without heavyweight citation infrastructure, each `ScriptScene` MAY contain one or more lightweight `ScriptSceneEvidenceReference` items recording: `TruthSourceClaimId` (or stable claim identifier), referenced factual statement, and an optional concise editorial note.

#### Scenario: Script schema, configurable pacing, and claim lineage completeness
- **WHEN** a script is created or generated for a ContentItem with an active Selected idea and approved TruthSource
- **THEN** the script record is initialized with status "Draft", `Version: 1`, effective `PacingWpm` resolved from channel settings, and ordered scenes with scene types, narration text, visual cues, and structured claim references linking back to approved TruthSource claims
- **AND** it is explicitly associated with `ContentItemId`, `ChannelId`, `ContentIdeaId`, `ContentIdeaVersionId`, `TruthSourceId`, and `TruthSourceVersionId`.

#### Scenario: Script creation blocked without active Selected idea or approved TruthSource
- **WHEN** an operator attempts to create or generate a script on a ContentItem that has no active "Selected" ContentIdea or whose TruthSource is not "Approved"
- **THEN** the backend rejects the request with HTTP 400 Bad Request and message "Script creation requires an active selected ContentIdea and an approved TruthSource".

### Requirement: Upstream lineage invalidation and stale-script semantics

The system SHALL enforce strict upstream lineage traceability. If, after a `Script` exists:
1. Another `ContentIdea` becomes the current `Selected` idea for the parent `ContentItem`; or
2. The factual/editorial foundation changes to a newer approved `TruthSourceVersion` that supersedes the script's origin `TruthSourceVersionId`;

the existing `Script` SHALL NOT silently remain eligible for downstream production. The system SHALL mark or calculate the script's lineage state as `Stale` / `Superseded`, preserve the historical script and all `ScriptVersion` snapshots without destructive deletion or automatic silent regeneration, display a clear reconciliation alert in the Content Workspace and Script Studio, and prevent an `Approved` but stale script from satisfying downstream production gates (storyboard/rendering) until explicitly regenerated or reconciled against the current upstream foundation.

#### Scenario: Script marked stale when active Selected idea changes
- **WHEN** an operator selects a different ContentIdea on a ContentItem that already has an existing Script
- **THEN** the existing Script is preserved in historical records with its immutable lineage intact
- **AND** the system evaluates the Script lineage state as "Stale" because its `ContentIdeaVersionId` does not match the active selected idea
- **AND** the Content Workspace surfaces a "Lineage Superseded / Reconciliation Required" alert
- **AND** downstream production gates block progression until the script is regenerated or reconciled.

#### Scenario: Script marked stale when TruthSource version advances
- **WHEN** an operator approves a new version of the TruthSource on a ContentItem with an existing Script
- **THEN** the Script lineage state is evaluated as "Stale" because its `TruthSourceVersionId` does not match the current approved TruthSource version
- **AND** downstream production remains blocked until explicit editorial reconciliation.

### Requirement: AI-assisted script generation (`generate_script`) with claim references

The system SHALL support invoking the capability `generate_script` through `IAiProviderRouter` to synthesize a structured multi-scene short-form script in Spanish adapted for the channel ("IA Simple ES"). The prompt SHALL incorporate the approved `TruthSource` (claims, key ideas, evidence references, and do-not-say constraints) and the active `ContentIdea` (angle, hook strategy, format). The generated output SHALL provide: structured scenes (Hook 0-3s, Problem, Insight, Climax, CTA), visual cues, narration text conforming to the channel's target pacing, and structured `ScriptSceneEvidenceReference` mappings for factual assertions. AI execution SHALL record an `AIRecommendation` telemetry record (tokens, latency, estimated cost, prompt policy version, without private chain-of-thought). No AI-generated statement SHALL become factual authority merely because the model generated it.

#### Scenario: AI generates structured script with claim traceability
- **WHEN** an operator triggers "Generate Script" on a ContentItem with an approved TruthSource and active Selected Idea
- **THEN** `IAiProviderRouter` routes the request to the configured reasoning provider (DeepSeek default or development mock)
- **AND** the AI generates a multi-scene script with timing tailored to the configured WPM, visual directions, and explicit claim references linking narration to TruthSource claims
- **AND** the script is persisted in "Draft" status with `Version: 1` and effective `PacingWpm`
- **AND** an `AIRecommendation` record is logged.

#### Scenario: Deterministic development mock script generation
- **WHEN** running in development environment without live AI provider credentials
- **THEN** `IAiProviderRouter` uses a deterministic mock adapter to generate realistic Spanish short-form AI/Tech scripts for channel "IA Simple ES" with valid claim references.

### Requirement: Advisory AI-assisted script critique (`review_script`)

The system SHALL support invoking the capability `review_script` through `IAiProviderRouter` to analyze an existing script draft against its approved `TruthSource` claims, do-not-say constraints, and creative parameters. The AI reviewer SHALL evaluate: factual fidelity against approved claims, constraint violations, hook retention strength (0-3s), audience takeaway clarity, narrative pacing, and spoken duration bounds. The evaluation SHALL return structured findings (`Pass`, `Warning`, `Critical`, dimension scores, specific scene critique notes, and actionable recommendations) persisted as an `AIRecommendation` record. AI critique SHALL be strictly advisory: AI critique findings SHALL NOT automatically approve or reject the script; human `EDITORIAL` approval remains the sole authoritative gate.

#### Scenario: AI script critique returns advisory findings
- **WHEN** an operator triggers "AI Critique" on a Script draft
- **THEN** `IAiProviderRouter` dispatches the review prompt comparing current narration text against approved TruthSource claims, evidence references, and constraints
- **AND** the system returns structured advisory critique results (`Pass`, `Warning`, or `Critical`) with specific scene notes
- **AND** an `AIRecommendation` telemetry record is stored
- **AND** the script status remains unchanged until human editorial action.

#### Scenario: Critical AI findings displayed prominently without overriding human authority
- **WHEN** AI critique reports "Critical" findings (e.g. violation of a do-not-say constraint)
- **THEN** the Script Studio highlights the critical warnings with high visibility
- **AND** the system does not automatically reject the script; the human editor decides whether to revise, approve, or reject.

### Requirement: Script versioning (`ScriptVersion`) and full-spectrum optimistic concurrency

The system SHALL treat human edits as first-class, immutable version snapshots rather than destructive in-place overwrites. Every mutation (editing scenes/narration/cues/claim references, submitting for review, approving, rejecting, reopening) SHALL require the client to supply `expectedVersion: long`. If `expectedVersion` does not match the database version, the backend SHALL reject the request with HTTP 409 Conflict (`CONCURRENCY_CONFLICT`), applying NO changes and logging NO history. For valid mutations, the system SHALL increment `Version`, update aggregates using the effective `PacingWpm`, and persist an immutable `ScriptVersion` snapshot capturing the complete script state, scenes, claim references, effective pacing WPM, change summary, author, and timestamp.

#### Scenario: Edit script scenes with version increment and history snapshot
- **WHEN** an operator updates narration text, visual prompts, or claim references on a Script at version 1 providing expectedVersion 1
- **THEN** the script record is updated with version 2 and updated word count / estimated duration
- **AND** an immutable `ScriptVersion` snapshot of version 1 is persisted in the history log
- **AND** an audit event is logged with action "Script.Updated".

#### Scenario: Concurrency conflict rejection across all script mutations
- **WHEN** an operator attempts an Update, SubmitForReview, Approve, Reject, or Reopen action providing expectedVersion 1 when the database has progressed to version 2
- **THEN** the backend rejects the request with HTTP 409 Conflict and code "CONCURRENCY_CONFLICT"
- **AND** the database state remains untouched and no spurious version history is recorded
- **AND** the frontend prompts the user to refresh and reconcile changes.

### Requirement: Unambiguous editorial review, approval, rejection, and reopen lifecycle

A `Script` SHALL follow an explicit, unambiguous human review lifecycle:
1. `Draft` -> `UnderReview` (via `SubmitForReview`): Submits the script for editorial review, creating a `ReviewScript` task.
2. `UnderReview` -> `Approved` (via `Approve`): Requires `EDITORIAL` role, verifies that the underlying `TruthSource` remains `Approved` and the script is not `Stale`, sets `ApprovedAtUtc` and `ApprovedByEmail`, advances the script to `Approved`, advances parent `ContentItem` stage to `ScriptApproved`, and completes pending `ReviewScript` tasks.
3. `UnderReview` -> `Rejected` (via `Reject`): Requires `EDITORIAL` role and a mandatory non-empty `rejectionReason`, sets `RejectedAtUtc` and `RejectedByEmail`, sets script status to persistent `Rejected` state, records an immutable `ScriptVersion` snapshot containing the rejection reason, and completes pending `ReviewScript` tasks.
4. `Rejected` -> `Draft` (via `Reopen` / `Revise`): Explicit editorial transition requiring `expectedVersion`, clears the active rejection block for continued editing while preserving historical rejection evidence, reverts script status to `Draft`, records an immutable `ScriptVersion` snapshot with change summary "Reopened for revision", and logs an audit event.

#### Scenario: Submit script for editorial review
- **WHEN** an operator submits a "Draft" script for review providing expectedVersion
- **THEN** the script status transitions to "UnderReview"
- **AND** a `ScriptVersion` snapshot is recorded
- **AND** the parent ContentItem stage transitions to "ScriptUnderReview"
- **AND** an audit event is logged with action "Script.SubmittedForReview".

#### Scenario: Approve script with approved TruthSource verification
- **WHEN** an operator with EDITORIAL role approves a script currently in "UnderReview" with expectedVersion
- **THEN** the backend verifies that the linked TruthSource is in "Approved" state and the script is not Stale
- **AND** the script status transitions to "Approved" with `ApprovedAtUtc` and `ApprovedByEmail`
- **AND** the parent ContentItem lifecycle stage advances to "ScriptApproved"
- **AND** an audit event is logged with action "Script.Approved".

#### Scenario: Reject script with mandatory rejection reason
- **WHEN** an operator with EDITORIAL role rejects a script currently in "UnderReview" providing expectedVersion and a descriptive rejection reason
- **THEN** the script status transitions to persistent "Rejected" state with `RejectionReason`, `RejectedAtUtc`, and `RejectedByEmail`
- **AND** a `ScriptVersion` snapshot is recorded containing the rejection reason
- **AND** an audit event is logged with action "Script.Rejected".

#### Scenario: Rejecting script without reason fails validation
- **WHEN** an operator attempts to reject a script with an empty or whitespace-only rejection reason
- **THEN** the backend rejects the request with HTTP 400 Bad Request and validation error "Rejection reason is required".

#### Scenario: Reopen rejected script for editorial revision
- **WHEN** an operator executes "Reopen" on a "Rejected" script providing expectedVersion
- **THEN** the script status transitions to "Draft"
- **AND** a `ScriptVersion` snapshot is recorded documenting the reopening
- **AND** historical rejection records in `ScriptVersion` history remain intact
- **AND** the parent ContentItem stage reflects "ScriptDrafted"
- **AND** an audit event is logged with action "Script.Reopened".

### Requirement: Backend authorization for script editorial operations

All script mutation endpoints (AI generation, advisory AI critique, manual creation, editing scenes/claims, submitting for review, approving, rejecting, reopening) SHALL be explicitly authorized on the backend, requiring the `EDITORIAL` role (or development GOD mode).

#### Scenario: Non-editorial user cannot edit, approve, or reject scripts
- **WHEN** a user without the `EDITORIAL` role attempts to edit, generate, approve, reject, or reopen a script
- **THEN** the backend rejects the request with HTTP 403 Forbidden.

### Requirement: Angular 21 Script Studio & Review UI

The frontend SHALL provide a dedicated Script Studio inside the Content Detail view on Angular 21 (PrimeNG 21, Tailwind CSS 4) featuring:
1. Header metrics: total word count, live spoken duration meter based on effective channel `PacingWpm` (with 30-60s target range indicators and 130-150 WPM guidance), status badge, version token, and stale lineage warning banner if upstream foundation changed.
2. Scene/Beat timeline editor: visual cards for each scene (Hook, Problem, Insight, Climax, CTA) with color-coded scene type badges, live scene duration, spoken narration textarea, visual cue/b-roll notes, structured claim reference tags, and scene reordering/addition/deletion.
3. Inherited constraints bar: display of TruthSource "Do-Not-Say" constraints and verifiable claims for continuous editorial context.
4. AI Generation modal: modal for configuring generation tone, emphasis, and target pacing.
5. Advisory AI Review panel: collapsible panel presenting AI critique findings (`Pass`/`Warning`/`Critical`), retention assessment, pacing feedback, and actionable suggestion cards.
6. Version history & diff drawer: timeline of previous `ScriptVersion` snapshots with narration diffs, rejection notes, and author attribution.
7. Decision action bar: contextual actions ("Submit for Review", "Approve", "Reject" with reason modal, "Reopen Script" for rejected scripts, "Regenerate/Reconcile" for stale scripts).
8. Responsive mobile layout: stacked card view with touch-friendly actions supporting full script review, AI critique inspection, and decision workflows on mobile viewports (~390px).

#### Scenario: High-density script editing and timing feedback on desktop
- **WHEN** an operator views the Script tab of a ContentItem on desktop (viewport >= 1280px)
- **THEN** scenes are rendered with live duration feedback based on channel configured WPM
- **AND** TruthSource do-not-say constraints and claim references are visible alongside narration
- **AND** total duration visual meter indicates whether the piece falls within the 30-60s target range.

#### Scenario: Responsive mobile script review and decision
- **WHEN** an operator opens the Script Studio on a mobile device (~390px)
- **THEN** scenes render cleanly as stacked collapsible cards
- **AND** the sticky action bar allows one-touch AI critique review, approval, rejection with reason input, or reopening.
