# Content Workspace Specification

## Purpose

Provides the operational identity `ContentItem` and central workspace for editorial production threads, linking in-process evidence capture, immutable multi-source evidence snapshots, lifecycle stage progression, and operational status tracking.

## Requirements

### Requirement: ContentItem operational identity with channel scoping

Every production thread SHALL be identified by a `ContentItem` entity assigned to a specific channel (`ChannelId` required), with a unique identifier, title, slug/identifier, current lifecycle stage (`DraftingEvidence`, `TruthSourceApproved`, `IdeaSelected`, `ScriptDrafted`, `ScriptUnderReview`, `ScriptApproved`, `StoryboardDrafted`, `StoryboardUnderReview`, `StoryboardApproved`), operational status (`Active`, `Paused`, `Completed`, `Cancelled`), `Version` (`long`) concurrency token, and created/updated attribution. `ContentItem` SHALL maintain clear module boundaries and reference `ContentItemId` rather than loading or locking downstream pipeline entities.

#### Scenario: Create new ContentItem for a channel
- **WHEN** an operator creates a new ContentItem with a title and channel assignment
- **THEN** a ContentItem record is persisted with lifecycle stage "DraftingEvidence", status "Active", version 1, and creator attribution
- **AND** an audit log event is recorded with action "ContentItem.Created".

#### Scenario: ContentItem requires valid channel
- **WHEN** an API request attempts to create a ContentItem without a valid `ChannelId`
- **THEN** the request is rejected with a validation error "ChannelId is required".

#### Scenario: Lifecycle stage advancement to ScriptDrafted
- **WHEN** a Script is first created, generated, or reopened for a ContentItem in "IdeaSelected" or "ScriptDrafted"
- **THEN** the parent ContentItem lifecycle stage reflects "ScriptDrafted".

#### Scenario: Lifecycle stage advancement to ScriptUnderReview
- **WHEN** a Script is submitted for editorial review
- **THEN** the parent ContentItem lifecycle stage automatically transitions to "ScriptUnderReview".

#### Scenario: Lifecycle stage advancement to ScriptApproved
- **WHEN** a Script is approved by an editorial operator
- **THEN** the parent ContentItem lifecycle stage automatically transitions to "ScriptApproved".

#### Scenario: Lifecycle stage advancement to StoryboardDrafted
- **WHEN** a Storyboard is first created, generated, or reopened for a ContentItem in "ScriptApproved" or "StoryboardDrafted"
- **THEN** the parent ContentItem lifecycle stage reflects "StoryboardDrafted".

#### Scenario: Lifecycle stage advancement to StoryboardUnderReview
- **WHEN** a Storyboard is submitted for editorial review
- **THEN** the parent ContentItem lifecycle stage automatically transitions to "StoryboardUnderReview".

#### Scenario: Lifecycle stage advancement to StoryboardApproved
- **WHEN** a Storyboard is approved by an editorial operator
- **THEN** the parent ContentItem lifecycle stage automatically transitions to "StoryboardApproved".

### Requirement: Real evidence capture and immutable snapshots

When a promoted candidate or manual lead is attached to a `ContentItem`, the system SHALL capture source material through an in-process extraction boundary (`IEvidenceCaptureService`) and persist an immutable `ContentItemEvidence` snapshot with full provenance, SHA-256 content hash, capture timestamp, and captured text.

#### Scenario: Ingest and capture URL evidence
- **WHEN** an operator initiates a ContentItem or attaches a promoted candidate with an external URL
- **THEN** the system fetches the source material, extracts readable text, computes a SHA-256 content hash, and persists an immutable `ContentItemEvidence` record with status "Captured"
- **AND** large/raw payloads are stored in MinIO with the object reference persisted in the evidence snapshot
- **AND** original DiscoveryCandidate provenance and origin URL are preserved.

#### Scenario: Ingest manual text-only evidence
- **WHEN** an operator attaches a text-only discovery candidate or manual note without an external URL
- **THEN** the submitted text itself is persisted directly as valid captured evidence with status "Captured"
- **AND** a SHA-256 hash of the submitted text is recorded without requiring an external URL.

#### Scenario: Handle failed URL evidence capture truthfully
- **WHEN** remote URL fetch/extraction fails due to timeout or network error
- **THEN** the system preserves the candidate provenance and records an evidence record with status "CaptureFailed" and the sanitized error message
- **AND** the system does not fabricate evidence or generate substitute content
- **AND** an authorized operator can trigger a retry on the failed evidence capture.

#### Scenario: Prevent duplicate ContentItem creation from same candidate
- **WHEN** an operator attempts to create a new ContentItem from a candidate that is already linked as PrimaryLead to an existing active ContentItem
- **THEN** the system rejects the duplicate creation or returns the existing ContentItem ID, preventing accidental duplicate production threads.

### Requirement: Non-destructive evidence exclusion and historical traceability

Evidence snapshots contributing to a `TruthSource` version SHALL remain permanently reproducible in history. The system SHALL support non-destructive removal from the active working set.

#### Scenario: Detach uncommitted evidence
- **WHEN** an operator removes an attached evidence item that has NOT contributed to any generated or approved TruthSource version
- **THEN** the evidence association is detached
- **AND** an audit event is logged with action "ContentItem.EvidenceDetached".

#### Scenario: Exclude evidence that contributed to a TruthSource version
- **WHEN** an operator removes an evidence item that contributed to an existing TruthSource version
- **THEN** the evidence snapshot is NOT physically deleted
- **AND** its association status is updated to "Excluded"
- **AND** the historical TruthSourceVersion remains linked to the immutable evidence snapshot.

### Requirement: Content workspace high-density management UI

The content workspace UI SHALL provide high-density scanning, filtering (by channel, lifecycle stage, status, and search query), and detail navigation to answer "where is this piece?" in the editorial lifecycle. The workspace and detail views SHALL follow the canonical full-width operational layout contract without arbitrary centered max-width constraints (such as `max-w-7xl mx-auto`), spanning 100% available horizontal space on desktop viewports (>=1280px).

#### Scenario: Filter content items by channel and stage
- **WHEN** an operator selects channel "IA Simple ES" and filters by stage "DraftingEvidence"
- **THEN** the workspace displays matching items with title, linked evidence count, current truth source state, last updated time, and quick action buttons
- **AND** the view adapts to full desktop width without avoidable vertical scroll.

#### Scenario: ContentItem detail drill-down
- **WHEN** an operator opens a ContentItem detail view
- **THEN** the view displays the operational header, the multi-evidence provenance panel (with SHA-256 hashes, capture status, retry button for failed captures, and source links), the TruthSource panel, and actions to generate draft or review evidence.

#### Scenario: Full-width workspace data table on desktop
- **WHEN** an operator opens the Content Workspace at 1440x900 or 1920x1080
- **THEN** the content table and filter toolbar span the full available viewport width minus compact padding
- **AND** no centered max-width constraint restricts horizontal density.

#### Scenario: Specialized editorial studios inherit canonical outer shell
- **WHEN** an operator navigates to TruthSource Review Studio, Idea Matrix, or Script Studio
- **THEN** the studio utilizes the full horizontal viewport width for split-pane evidence inspection, idea cards, and scene timelines
- **AND** the outer header and toolbar align consistently with shared page primitives while preserving internal specialized workflows.

### Requirement: ContentItem ideas management and drill-down

The Content Workspace and Content Detail view SHALL provide direct management and visualization of `ContentIdea` entities linked to the active `ContentItem`, displaying current idea count, the sole active selected idea (if any), and direct action triggers.

#### Scenario: View linked ideas in ContentItem detail
- **WHEN** an operator views a ContentItem with generated or manual ideas
- **THEN** an "Ideas" tab or section displays the list of ideas with status badges, angles, and hook strategies
- **AND** if an idea is selected, it is highlighted as the sole active creative lead for the piece.

#### Scenario: Lifecycle stage advancement to IdeaSelected on idea promotion
- **WHEN** a ContentIdea is marked as "Selected" for a ContentItem currently in "TruthSourceApproved"
- **THEN** the parent ContentItem lifecycle stage automatically transitions to "IdeaSelected"
- **AND** the workspace reflects the updated stage immediately.

### Requirement: ContentItem script management, stale lineage detection, and production gating

The Content Workspace and Content Detail view SHALL provide direct management and visualization of `Script` entities linked to the active `ContentItem`, displaying script status badges, total scene count, word count, estimated duration (using channel configured WPM), and stale lineage alerts. If a script's foundation is superseded (because the selected idea changed or a newer TruthSource version was approved), the workspace SHALL flag the script as stale and display a reconciliation notice. Downstream production steps (storyboard, voiceover, video rendering) SHALL be strictly gated: no production step can proceed without an active `Approved` script that is NOT stale.

#### Scenario: View script summary and stale alert in ContentItem detail
- **WHEN** an operator views a ContentItem with an active script
- **THEN** a "Script" tab displays the script status badge, target duration, current estimated duration, word count, and stale reconciliation banner if upstream foundation changed
- **AND** clicking navigates directly into the Script Studio.

#### Scenario: Downstream video production gated on approved, non-stale script
- **WHEN** a system component or operator attempts to initiate video rendering or storyboard generation for a ContentItem whose script is missing, in "Draft", "UnderReview", "Rejected", or flagged as "Stale"
- **THEN** the request is rejected with validation error "Downstream production requires an approved, non-stale Script".

### Requirement: ContentItem storyboard management, stale lineage detection, and media production gating

The Content Workspace and Content Detail view SHALL provide direct management and visualization of `Storyboard` and `AssetPlan` entities linked to the active `ContentItem`, displaying storyboard status badges, total frame count, estimated duration, and stale lineage alerts. The system SHALL enforce the invariant that exactly one Storyboard is current per ContentItem. If a storyboard's foundation is superseded (because the script or truth source changed), the workspace SHALL flag the storyboard as stale and display a reconciliation notice with a one-click successor derivation action. Future downstream media production steps (ComfyUI image/video generation, TTS audio synthesis, video rendering) SHALL be strictly gated: no media production step can proceed without an active `Approved` Storyboard that is NOT stale.

#### Scenario: View storyboard summary and stale alert in ContentItem detail
- **WHEN** an operator views a ContentItem with an active storyboard
- **THEN** a "Storyboard" tab displays the storyboard status badge, target duration, frame count, asset readiness overview, and stale reconciliation banner if upstream script foundation changed
- **AND** clicking navigates directly into the Storyboard & Production Planning Studio.

#### Scenario: Downstream media production gated on approved, non-stale storyboard
- **WHEN** a production readiness query evaluates a ContentItem whose Storyboard is missing, in "Draft", "UnderReview", "Rejected", or flagged as "Stale"
- **THEN** the eligibility query returns that downstream media generation is blocked until an approved, non-stale Storyboard with a complete AssetPlan is present.

