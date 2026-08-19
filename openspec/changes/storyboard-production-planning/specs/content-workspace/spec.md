## MODIFIED Requirements

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

## ADDED Requirements

### Requirement: ContentItem storyboard management, stale lineage detection, and media production gating

The Content Workspace and Content Detail view SHALL provide direct management and visualization of `Storyboard` and `AssetPlan` entities linked to the active `ContentItem`, displaying storyboard status badges, total frame count, estimated duration, and stale lineage alerts. The system SHALL enforce the invariant that exactly one Storyboard is current per ContentItem. If a storyboard's foundation is superseded (because the script or truth source changed), the workspace SHALL flag the storyboard as stale and display a reconciliation notice with a one-click successor derivation action. Future downstream media production steps (ComfyUI image/video generation, TTS audio synthesis, video rendering) SHALL be strictly gated: no media production step can proceed without an active `Approved` Storyboard that is NOT stale.

#### Scenario: View storyboard summary and stale alert in ContentItem detail
- **WHEN** an operator views a ContentItem with an active storyboard
- **THEN** a "Storyboard" tab displays the storyboard status badge, target duration, frame count, asset readiness overview, and stale reconciliation banner if upstream script foundation changed
- **AND** clicking navigates directly into the Storyboard & Production Planning Studio.

#### Scenario: Downstream media production gated on approved, non-stale storyboard
- **WHEN** a production readiness query evaluates a ContentItem whose Storyboard is missing, in "Draft", "UnderReview", "Rejected", or flagged as "Stale"
- **THEN** the eligibility query returns that downstream media generation is blocked until an approved, non-stale Storyboard with a complete AssetPlan is present.
