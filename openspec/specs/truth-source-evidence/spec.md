# Truth Source Evidence Specification

## Purpose

Defines the structured editorial evidence layer `TruthSource`, automated AI draft synthesis (`build_truth_source`) via configured provider routing, structured `AIRecommendation` tracking, MySQL-compatible `Version: long` optimistic concurrency, version history tracking, and the downstream approval gate.

## Requirements

### Requirement: Structured TruthSource schema and human approval requirement

A `TruthSource` SHALL be linked 1:1 with a parent `ContentItem` and represent human-reviewed, human-approved factual evidence adhering to the canonical schema: summary, key ideas (list), verifiable claims with source citations (list), evidence references (list of supporting `ContentItemEvidence` IDs), risk notes, do-not-say constraints (list, e.g. anti-hype, no sensational claims), possible angles (list), Spanish localization/adaptation notes, and `Version` (`long`). A TruthSource SHALL NOT become approved automatically through AI generation; approval SHALL require an explicit authorized human action.

#### Scenario: TruthSource schema completeness
- **WHEN** a TruthSource draft is generated or edited
- **THEN** it contains all canonical fields: summary, key ideas, verifiable claims, evidence references, risk notes, do-not-say constraints, possible angles, localization notes, and the current `Version: long`
- **AND** verifiable claims explicitly cite contributing evidence item IDs / URLs.

### Requirement: AI-assisted draft synthesis (`build_truth_source`) and AIRecommendation telemetry

The system SHALL support invoking the capability `build_truth_source` through `IAiProviderRouter` to synthesize a draft TruthSource proposal from all active, successfully captured evidence items attached to the parent `ContentItem`. AI execution SHALL record an `AIRecommendation` audit record capturing ContentItemId, capability ("build_truth_source"), provider, model, prompt-policy version, structured output, confidence when supplied, concise rationale/evidence references, usage/cost/latency, and accepted/rejected state. The system SHALL NEVER request or persist private chain-of-thought.

#### Scenario: AI synthesis produces draft proposal only
- **WHEN** an operator triggers "Generate TruthSource" on a ContentItem with active captured evidence
- **THEN** `IAiProviderRouter` routes the request to the configured provider (defaulting to DeepSeek or development mock adapter)
- **AND** the AI response is parsed into a structured TruthSource draft with status "Draft" and version 1
- **AND** the TruthSource is NOT approved automatically
- **AND** an `AIRecommendation` record is persisted with structured telemetry and rationale without private chain-of-thought.

#### Scenario: AI synthesis on ContentItem with no captured evidence
- **WHEN** an operator attempts to trigger AI synthesis on a ContentItem without any successfully captured evidence items
- **THEN** the system rejects the request with error "At least one successfully captured evidence item is required for TruthSource synthesis".

#### Scenario: Development AI behavior with offline mock
- **WHEN** running in development environment without live provider credentials
- **THEN** `IAiProviderRouter` uses a deterministic development mock adapter to synthesize a valid Spanish AI/Tech TruthSource draft
- **AND** human verification can proceed locally without external API dependencies.

### Requirement: TruthSource editorial lifecycle and approval gates

A `TruthSource` SHALL progress through defined lifecycle states: `Draft`, `UnderReview`, `Approved`, and `Rejected`. Approval SHALL require the `EDITORIAL` role. Rejection SHALL require a mandatory non-empty rejection reason. Rejected or superseded drafts SHALL remain historically traceable.

#### Scenario: Submit TruthSource for editorial review
- **WHEN** an operator submits a draft TruthSource for review
- **THEN** its status transitions to "UnderReview"
- **AND** an audit event is logged with action "TruthSource.SubmittedForReview".

#### Scenario: Approve TruthSource by EDITORIAL operator
- **WHEN** a user with the `EDITORIAL` role approves an "UnderReview" TruthSource
- **THEN** its status transitions to "Approved"
- **AND** the parent ContentItem lifecycle stage advances to "TruthSourceApproved"
- **AND** `ApprovedAtUtc` and `ApprovedByEmail` are persisted
- **AND** an audit event is logged with action "TruthSource.Approved".

#### Scenario: Reject TruthSource with mandatory reason
- **WHEN** an operator rejects an "UnderReview" TruthSource providing reason "Contains unverified claims regarding job displacement"
- **THEN** its status transitions to "Rejected"
- **AND** the rejection reason, rejected timestamp, and actor email are persisted
- **AND** an audit event is logged with action "TruthSource.Rejected".

#### Scenario: Reject TruthSource without reason is blocked
- **WHEN** an operator attempts to reject a TruthSource with an empty or whitespace-only reason
- **THEN** the request is rejected with validation error "Rejection reason is required".

### Requirement: Downstream progression gate on Approved TruthSource

Only a `TruthSource` in "Approved" status SHALL be eligible for downstream continuation into subsequent editorial stages (including `ContentIdea` generation and manual idea creation). Any draft, under-review, rejected, or superseded TruthSource version SHALL strictly block idea generation and subsequent downstream progression.

#### Scenario: Unapproved TruthSource blocks downstream progression
- **WHEN** any system check verifies downstream eligibility for a ContentItem whose TruthSource is in "Draft", "UnderReview", or "Rejected" status
- **THEN** the system reports downstream progression as blocked, enforcing the canonical invariant "No downstream progression without approved TruthSource".

#### Scenario: Approved TruthSource unlocks idea generation
- **WHEN** a TruthSource reaches "Approved" status
- **THEN** downstream idea generation (`generate_ideas`) and manual idea creation are unlocked for that ContentItem.

### Requirement: MySQL-compatible optimistic concurrency and versioned human editing

Every human edit to a `TruthSource` SHALL provide the current expected `Version: long` token. The system SHALL increment `Version` on save, verify concurrency using EF Core concurrency token checking, and persist an immutable `TruthSourceVersion` snapshot capturing the edited fields, contributing evidence IDs, diff summary, author email, and change timestamp.

#### Scenario: Edit TruthSource increments version snapshot
- **WHEN** an operator edits the claims or do-not-say constraints of a TruthSource at version 1
- **THEN** the TruthSource record is updated with version 2
- **AND** a `TruthSourceVersion` record for version 1 is preserved in historical lineage along with supporting evidence IDs
- **AND** the author email and updated timestamp are recorded.

#### Scenario: Reject stale concurrent write with 409 Conflict
- **WHEN** an operator attempts to save changes with version 1 when the database has already progressed to version 2
- **THEN** the update is rejected with HTTP 409 Conflict and a machine-readable error code "CONCURRENCY_CONFLICT"
- **AND** the frontend Review Studio surfaces the conflict and allows the operator to reload and reconcile changes.

### Requirement: TruthSource Review Studio UI

The frontend SHALL provide a dedicated TruthSource Review Studio with a side-by-side layout (immutable evidence panels on one side, structured TruthSource sections on the other), inline rich field editing, version comparison, 409 conflict reconciliation, and contextual Approve/Reject actions.

#### Scenario: Side-by-side review on desktop
- **WHEN** an operator opens the TruthSource Review Studio on desktop (viewport >= 1280px)
- **THEN** the left panel displays all linked evidence snapshots and raw excerpts
- **AND** the right panel displays the structured TruthSource sections with clear badge highlighting for claims and constraints
- **AND** one-click actions for "Edit", "Approve", and "Reject" are pinned in the action header.

#### Scenario: Mobile review drawer
- **WHEN** an operator reviews a TruthSource on mobile (viewport ~390px)
- **THEN** tabs allow toggling between "Evidence Sources" and "Truth Source"
- **AND** an approval/rejection action bar is anchored for quick one-tap decisions.
