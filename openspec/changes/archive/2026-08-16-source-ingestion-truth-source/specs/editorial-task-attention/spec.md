# Editorial Task Attention Specification

## Purpose

Defines concrete human-action task modeling (`EditorialTask`) for review items requiring attention (such as TruthSource review), priority queues, assignments, deadlines, and dashboard attention integration without creating a generic task-management or inbox metaphor.

## ADDED Requirements

### Requirement: EditorialTask concrete human action modeling

The system SHALL model concrete editorial action items requiring human attention as `EditorialTask` records linked to a parent `ContentItem` and channel, with task type (`ReviewTruthSource`), priority (`Low`, `Normal`, `High`, `Urgent`), status (`Pending`, `InProgress`, `Completed`, `Cancelled`), assigned user email (optional), due date, created timestamp, and completion metadata. Editorial tasks SHALL NOT implement a generic task-management system or email-inbox metaphor; they serve strictly to surface operational attention and deep-link directly into the contextual Review Studio.

#### Scenario: Create editorial task for TruthSource review
- **WHEN** a TruthSource transitions to "UnderReview"
- **THEN** an `EditorialTask` of type "ReviewTruthSource" is created for the parent ContentItem with status "Pending" and default priority "Normal"
- **AND** the task is linked directly to the parent ContentItem.

#### Scenario: Complete editorial task on TruthSource resolution
- **WHEN** an operator approves or rejects a TruthSource
- **THEN** any pending `EditorialTask` of type "ReviewTruthSource" for that ContentItem is automatically updated to status "Completed"
- **AND** `CompletedAtUtc` and `CompletedByEmail` are persisted.

### Requirement: Task assignment and priority updates

Editorial and Technical operators SHALL be able to assign, reassign, or update priority and due dates on pending editorial tasks.

#### Scenario: Assign task to specific operator
- **WHEN** an operator assigns a pending task to "editor@silverman.pro"
- **THEN** `AssignedUserEmail` is updated
- **AND** the task status transitions to "InProgress"
- **AND** an audit event is logged with action "EditorialTask.Assigned".

### Requirement: Contextual review and dashboard attention integration

Dashboard Attention SHALL surface actionable `EditorialTask` items requiring decision, providing direct contextual deep-links to perform the review work inside the Content Workspace / Review Studio.

#### Scenario: Dashboard attention widget displays pending reviews
- **WHEN** one or more TruthSources require review
- **THEN** the dashboard Attention widget highlights the number of pending TruthSource reviews grouped by urgency
- **AND** clicking an item opens the TruthSource Review Studio directly to perform the editorial review.
