# Channel Management

## ADDED Requirements

### Requirement: Technical channel management

A TECHNICAL operator SHALL be able to create, edit and change lifecycle status of editorial channels.

#### Scenario: Create pilot channel
Given a TECHNICAL operator
When a channel is created with name, language, niche and pilot status
Then it is persisted
And appears in channel management
And dashboard channel summary updates
And an audit event is recorded.

#### Scenario: Editorial-only user denied
Given an operator with EDITORIAL but not TECHNICAL
When channel creation is attempted
Then the backend denies the operation
And no channel is persisted.

### Requirement: Initial channel

Development seed SHALL include IA Simple ES as a pilot channel.

#### Scenario: Seeded pilot visible
Given a reset development database
When the operator opens Channels
Then IA Simple ES is visible with Spanish language, AI/future-of-work niche and pilot status.

### Requirement: Efficient responsive management

Channel management SHALL not require a long full-page CRUD form for routine create/edit behavior unless an implementation constraint is documented.

#### Scenario: Desktop edit
Given desktop viewport
When the operator edits a channel
Then editing occurs through a compact dialog/drawer or equally efficient contextual interaction
And the dashboard context is recoverable without unnecessary navigation.

#### Scenario: Tablet/mobile edit
Given tablet or mobile viewport
When the operator performs supported channel editing
Then controls remain touch-usable and content remains readable without horizontal page overflow.
