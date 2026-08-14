# Channel Management Specification

## Purpose

Provides editorial channel portfolio management, niche definitions, lifecycle transitions, and efficient responsive channel operations.

## Requirements

### Requirement: Technical channel management

A TECHNICAL operator SHALL be able to create, edit and change lifecycle status of editorial channels.

#### Scenario: Create pilot channel
- **WHEN** a TECHNICAL operator creates a channel with name, language, niche and pilot status
- **THEN** it is persisted
- **AND** appears in channel management
- **AND** dashboard channel summary updates
- **AND** an audit event is recorded.

#### Scenario: Editorial-only user denied
- **WHEN** an operator with EDITORIAL but not TECHNICAL attempts channel creation
- **THEN** the backend denies the operation
- **AND** no channel is persisted.

### Requirement: Initial channel

Development seed SHALL include IA Simple ES as a pilot channel.

#### Scenario: Seeded pilot visible
- **WHEN** the operator opens Channels in a reset development environment
- **THEN** IA Simple ES is visible with Spanish language, AI/future-of-work niche and pilot status.

### Requirement: Efficient responsive management

Channel management SHALL not require a long full-page CRUD form for routine create/edit behavior unless an implementation constraint is documented.

#### Scenario: Desktop edit
- **WHEN** an operator edits a channel on desktop viewport
- **THEN** editing occurs through a compact dialog/drawer or equally efficient contextual interaction
- **AND** the dashboard context is recoverable without unnecessary navigation.

#### Scenario: Tablet/mobile edit
- **WHEN** an operator performs supported channel editing on tablet or mobile viewport
- **THEN** controls remain touch-usable and content remains readable without horizontal page overflow.
