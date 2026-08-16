# Content Workspace Specification

## Purpose

Provides the operational identity `ContentItem` and central workspace for editorial production threads, linking in-process evidence capture, immutable multi-source evidence snapshots, lifecycle stage progression, and operational status tracking.

## ADDED Requirements

### Requirement: ContentItem operational identity with channel scoping

Every production thread SHALL be identified by a `ContentItem` entity assigned to a specific channel (`ChannelId` required), with a unique identifier, title, slug/identifier, current lifecycle stage (`DraftingEvidence`, `TruthSourceApproved`), operational status (`Active`, `Paused`, `Completed`, `Cancelled`), `Version` (`long`) concurrency token, and created/updated attribution. `ContentItem` SHALL maintain clear module boundaries and reference `ContentItemId` rather than loading or locking downstream pipeline entities.

#### Scenario: Create new ContentItem for a channel
- **WHEN** an operator creates a new ContentItem with a title and channel assignment
- **THEN** a ContentItem record is persisted with lifecycle stage "DraftingEvidence", status "Active", version 1, and creator attribution
- **AND** an audit log event is recorded with action "ContentItem.Created".

#### Scenario: ContentItem requires valid channel
- **WHEN** an API request attempts to create a ContentItem without a valid `ChannelId`
- **THEN** the request is rejected with a validation error "ChannelId is required".

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

The content workspace UI SHALL provide high-density scanning, filtering (by channel, lifecycle stage, status, and search query), and detail navigation to answer "where is this piece?" in the editorial lifecycle.

#### Scenario: Filter content items by channel and stage
- **WHEN** an operator selects channel "IA Simple ES" and filters by stage "DraftingEvidence"
- **THEN** the workspace displays matching items with title, linked evidence count, current truth source state, last updated time, and quick action buttons
- **AND** the view adapts to full desktop width without avoidable vertical scroll.

#### Scenario: ContentItem detail drill-down
- **WHEN** an operator opens a ContentItem detail view
- **THEN** the view displays the operational header, the multi-evidence provenance panel (with SHA-256 hashes, capture status, retry button for failed captures, and source links), the TruthSource panel, and actions to generate draft or review evidence.
