# Content Workspace Specification

## MODIFIED Requirements

### Requirement: ContentItem operational identity with channel scoping

Every production thread SHALL be identified by a `ContentItem` entity assigned to a specific channel (`ChannelId` required), with a unique identifier, title, slug/identifier, current lifecycle stage (`DraftingEvidence`, `TruthSourceApproved`, `IdeaSelected`, `ScriptDrafted`, `ScriptUnderReview`, `ScriptApproved`), operational status (`Active`, `Paused`, `Completed`, `Cancelled`), `Version` (`long`) concurrency token, and created/updated attribution. `ContentItem` SHALL maintain clear module boundaries and reference `ContentItemId` rather than loading or locking downstream pipeline entities.

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

## ADDED Requirements

### Requirement: ContentItem script management, stale lineage detection, and production gating

The Content Workspace and Content Detail view SHALL provide direct management and visualization of `Script` entities linked to the active `ContentItem`, displaying script status badges, total scene count, word count, estimated duration (using channel configured WPM), and stale lineage alerts. If a script's foundation is superseded (because the selected idea changed or a newer TruthSource version was approved), the workspace SHALL flag the script as stale and display a reconciliation notice. Downstream production steps (storyboard, voiceover, video rendering) SHALL be strictly gated: no production step can proceed without an active `Approved` script that is NOT stale.

#### Scenario: View script summary and stale alert in ContentItem detail
- **WHEN** an operator views a ContentItem with an active script
- **THEN** a "Script" tab displays the script status badge, target duration, current estimated duration, word count, and stale reconciliation banner if upstream foundation changed
- **AND** clicking navigates directly into the Script Studio.

#### Scenario: Downstream video production gated on approved, non-stale script
- **WHEN** a system component or operator attempts to initiate video rendering or storyboard generation for a ContentItem whose script is missing, in "Draft", "UnderReview", "Rejected", or flagged as "Stale"
- **THEN** the request is rejected with validation error "Downstream production requires an approved, non-stale Script".
