# Editorial Task Attention Specification

## MODIFIED Requirements

### Requirement: EditorialTask concrete human action modeling

The system SHALL model concrete editorial action items requiring human attention as `EditorialTask` records linked to a parent `ContentItem` and channel, with task type (`ReviewTruthSource`, `ReviewScript`), priority (`Low`, `Normal`, `High`, `Urgent`), status (`Pending`, `InProgress`, `Completed`, `Cancelled`), assigned user email (optional), due date, created timestamp, and completion metadata. Editorial tasks SHALL NOT implement a generic task-management system or email-inbox metaphor; they serve strictly to surface operational attention and deep-link directly into the contextual Review Studio or Script Studio.

#### Scenario: Create editorial task for TruthSource review
- **WHEN** a TruthSource transitions to "UnderReview"
- **THEN** an `EditorialTask` of type "ReviewTruthSource" is created for the parent ContentItem with status "Pending" and default priority "Normal"
- **AND** the task is linked directly to the parent ContentItem.

#### Scenario: Complete editorial task on TruthSource resolution
- **WHEN** an operator approves or rejects a TruthSource
- **THEN** any pending `EditorialTask` of type "ReviewTruthSource" for that ContentItem is automatically updated to status "Completed"
- **AND** `CompletedAtUtc` and `CompletedByEmail` are persisted.

#### Scenario: Create editorial task for Script review
- **WHEN** a Script transitions to "UnderReview"
- **THEN** an `EditorialTask` of type "ReviewScript" is created for the parent ContentItem with status "Pending" and default priority "Normal"
- **AND** the task is linked directly to the parent ContentItem and channel.

#### Scenario: Complete editorial task on Script approval or rejection
- **WHEN** an operator approves or rejects a Script
- **THEN** any pending `EditorialTask` of type "ReviewScript" for that ContentItem is automatically updated to status "Completed"
- **AND** `CompletedAtUtc` and `CompletedByEmail` are persisted.

### Requirement: Contextual review and dashboard attention integration

Dashboard Attention SHALL surface actionable `EditorialTask` items requiring decision (including TruthSource reviews and Script reviews), providing direct contextual deep-links to perform the review work inside the TruthSource Review Studio or Script Studio.

#### Scenario: Dashboard attention widget displays pending reviews
- **WHEN** one or more TruthSources require review
- **THEN** the dashboard Attention widget highlights the number of pending TruthSource reviews grouped by urgency
- **AND** clicking an item opens the TruthSource Review Studio directly to perform the editorial review.

#### Scenario: Dashboard attention widget displays pending script reviews
- **WHEN** one or more Scripts require editorial review
- **THEN** the dashboard Attention widget includes the count of pending Script reviews
- **AND** clicking an item opens the Script Studio directly on the relevant ContentItem.
