# Discovery Candidate Triage Specification

## Purpose

Provides a unified discovery candidate triage workspace that ingests, normalizes, deduplicates, and evaluates incoming content leads from automated sources and manual URL/text submissions with full provenance tracking.

## Requirements

### Requirement: Mandatory provenance on unified candidate model

Every discovery candidate—regardless of whether it originates from automated source ingestion or manual URL/text submission—SHALL be assigned to a specific channel (`ChannelId` required) and record immutable provenance: origin source ID (if from catalog) or manual origin marker, external URL (nullable for text-only submissions), normalized URL (nullable for text-only submissions), original title, raw content snippet/summary, language, discovered timestamp, submitting actor or source provider, and channel assignment.

#### Scenario: Ingest automated source item into candidate
- **WHEN** an automated or manual sync processes an external discovery source containing a new item
- **THEN** a discovery candidate is created with status "PendingReview" and origin type "Automated"
- **AND** the candidate stores the source ID, original article URL, normalized URL, title, discovered date, author if present, and raw content summary
- **AND** channel attribution matches the parent discovery source.

#### Scenario: Ingest manual URL submission
- **WHEN** an operator submits an external URL with an optional note, title, and channel selection
- **THEN** a discovery candidate is created with status "PendingReview" and origin type "Manual"
- **AND** the URL is normalized with tracking parameters stripped
- **AND** the submitter's identity is recorded in the provenance metadata.

#### Scenario: Ingest manual text-only lead without URL
- **WHEN** an operator submits an editorial note or text lead without an external URL for a selected channel
- **THEN** a discovery candidate is created with status "PendingReview" and origin type "Manual"
- **AND** external URL and normalized URL are stored as null
- **AND** the text lead is preserved as the title/summary with full submitter provenance.

### Requirement: Candidate deduplication and normalization for URL leads

When an incoming candidate contains an external URL, the system SHALL normalize the URL (strip tracking query parameters, trim fragments, lowercase scheme/host) and prevent duplicate candidate creation within the same channel.

#### Scenario: Submitting duplicate URL within same channel
- **WHEN** a source sync or manual submission attempts to ingest a URL that already exists for that channel in any status
- **THEN** the system ignores the duplicate ingestion without error
- **AND** the existing candidate's last-seen timestamp is refreshed.

#### Scenario: Same URL in different channels
- **WHEN** the same URL is submitted to two distinct editorial channels
- **THEN** independent candidates are created for each channel to allow channel-specific editorial review and lifecycle progression.

#### Scenario: Text-only submissions bypass URL deduplication
- **WHEN** an operator submits multiple text-only notes without URLs
- **THEN** each note is persisted as a distinct manual candidate for the selected channel.

### Requirement: Candidate evaluation and triage lifecycle

The system SHALL allow EDITORIAL and TECHNICAL operators to evaluate discovery candidates in the triage workspace between PendingReview, Promoted, and Dismissed states.

#### Scenario: Promote candidate to editorial pipeline handoff
- **WHEN** an operator promotes a pending candidate with an optional editorial note
- **THEN** the candidate status transitions to "Promoted"
- **AND** the exact promotion handoff state is persisted with "PromotedAtUtc", "PromotedByEmail", and editorial notes
- **AND** full candidate provenance remains attached and immutable
- **AND** an audit event is logged with action "DiscoveryCandidate.Promoted"
- **AND** no downstream TruthSource or ContentIdea entities are implicitly created in this change.

#### Scenario: Dismiss candidate with reason
- **WHEN** an operator dismisses a candidate with reason "Irrelevant" or "Low Quality"
- **THEN** the candidate status transitions to "Dismissed"
- **AND** the dismissal reason and actor are persisted
- **AND** the item is removed from the active triage queue.

### Requirement: Responsive triage workspace and preview drawer

The discovery candidate triage workspace UI SHALL provide high-density scanning of pending leads and a slide-over preview drawer for reading article content/notes, viewing provenance metadata, and executing triage actions without full-page navigation.

#### Scenario: Rapid triage on desktop
- **WHEN** an operator reviews candidates in the desktop triage workspace
- **THEN** selecting a candidate opens a contextual preview drawer displaying the summary/text, original URL link (if present), provenance metadata, and action buttons (Promote, Dismiss, Edit)
- **AND** advancing or acting on the item automatically loads the next candidate in the queue.

#### Scenario: Mobile candidate triage
- **WHEN** an operator opens the discovery triage workspace on a mobile device
- **THEN** items are presented in compact cards with touch actions for Promote and Dismiss
- **AND** the full details drawer opens as a responsive bottom sheet or dialog.
