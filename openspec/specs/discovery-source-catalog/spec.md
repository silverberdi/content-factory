# Discovery Source Catalog Specification

## Purpose

Provides a centralized discovery source catalog to register, configure, monitor, and synchronize channel-scoped external content sources (RSS/Atom feeds, web publications, podcast feeds, curated channels) assigned to editorial channels.

## Requirements

### Requirement: Discovery source registration and management

The system SHALL allow authenticated TECHNICAL and EDITORIAL operators to register and manage external discovery sources with name, origin URL, source type (Feed, Web, Podcast, Curated, Manual), mandatory channel attribution, language, and polling interval.

#### Scenario: Register an external discovery source for a channel
- **WHEN** a TECHNICAL or EDITORIAL operator creates a discovery source with name "TechCrunch AI", origin URL "https://techcrunch.com/category/artificial-intelligence/feed/", source type "Feed", channel "IA Simple ES", language "es", and polling interval 60 minutes
- **THEN** the discovery source is persisted with status "Active" and assigned to the specified channel
- **AND** an audit event is recorded with action "DiscoverySource.Created"
- **AND** the source appears in the discovery catalog list for that channel.

#### Scenario: Missing channel rejection
- **WHEN** an operator attempts to register a discovery source without specifying a valid channel
- **THEN** the backend rejects the request with a validation error
- **AND** no source is persisted.

#### Scenario: Duplicate source origin URL prevention within channel
- **WHEN** an operator attempts to register a discovery source with an origin URL and channel combination that already exists
- **THEN** the backend rejects the request with a conflict error
- **AND** no duplicate source is created.

### Requirement: Source lifecycle and health tracking

The system SHALL track operational status (Active, Paused, Error), last sync timestamp, next scheduled sync, fetch failure count, and last error message for every registered discovery source.

#### Scenario: Pause and resume discovery source
- **WHEN** an operator toggles an active discovery source to paused
- **THEN** the status updates to "Paused"
- **AND** automated polling skips the source until resumed
- **AND** resuming sets status back to "Active".

#### Scenario: Source sync failure recording
- **WHEN** an automated or manual sync fails due to network or source parsing errors
- **THEN** the source records the sanitized error message and failure timestamp
- **AND** the source status transitions to "Error" if the failure threshold is reached
- **AND** the error is visible in the source detail and dashboard health indicators.

### Requirement: Manual source sync trigger

The system SHALL allow authorized operators to trigger an immediate on-demand synchronization for any active discovery source without blocking the user interface.

#### Scenario: Trigger manual source sync
- **WHEN** an operator clicks "Sync Now" for a registered source
- **THEN** the backend initiates the ingestion task using the appropriate source adapter
- **AND** newly discovered items are parsed into discovery candidates for that channel
- **AND** the source's last sync timestamp updates upon completion
- **AND** the UI indicates sync progress and outcome without page refresh.

### Requirement: Responsive source catalog UX

The discovery source catalog UI SHALL support viewing, filtering (by channel, status, source type), creating, and editing sources across desktop, tablet, and mobile viewports.

#### Scenario: Desktop source management
- **WHEN** an operator accesses the source catalog on a desktop viewport
- **THEN** sources are displayed in a high-density data view with inline health badges and quick actions (Sync, Pause, Edit)
- **AND** create/edit operations open in a contextual drawer or dialog without losing list context.

#### Scenario: Mobile source monitoring
- **WHEN** an operator views the source catalog on a mobile viewport
- **THEN** key source cards show name, channel tag, health status, and last sync time without horizontal overflow
- **AND** touch-friendly actions allow trigger sync or status change.
