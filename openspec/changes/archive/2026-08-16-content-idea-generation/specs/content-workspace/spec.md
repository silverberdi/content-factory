# Content Workspace Specification

## MODIFIED Requirements

### Requirement: ContentItem operational identity with channel scoping

Every production thread SHALL be identified by a `ContentItem` entity assigned to a specific channel (`ChannelId` required), with a unique identifier, title, slug/identifier, current lifecycle stage (`DraftingEvidence`, `TruthSourceApproved`, `IdeaSelected`), operational status (`Active`, `Paused`, `Completed`, `Cancelled`), `Version` (`long`) concurrency token, and created/updated attribution. `ContentItem` SHALL maintain clear module boundaries and reference `ContentItemId` rather than loading or locking downstream pipeline entities.

#### Scenario: Create new ContentItem for a channel
- **WHEN** an operator creates a new ContentItem with a title and channel assignment
- **THEN** a ContentItem record is persisted with lifecycle stage "DraftingEvidence", status "Active", version 1, and creator attribution
- **AND** an audit log event is recorded with action "ContentItem.Created".

#### Scenario: ContentItem requires valid channel
- **WHEN** an API request attempts to create a ContentItem without a valid `ChannelId`
- **THEN** the request is rejected with a validation error "ChannelId is required".

## ADDED Requirements

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
