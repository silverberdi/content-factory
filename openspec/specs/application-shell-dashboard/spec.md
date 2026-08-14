# Application Shell and Dashboard Specification

## Purpose

Provides the unified responsive application shell, theme preference persistence, and the primary operations cockpit dashboard.

## Requirements

### Requirement: Dashboard is the default operational control center

After authentication, the application SHALL open the dashboard as the primary operational view.

#### Scenario: Desktop control center
- **WHEN** an authenticated operator opens the dashboard on a viewport 1440x900 or larger
- **THEN** factory health, channel state and current attention summary are visible without avoidable full-page vertical scrolling
- **AND** the layout uses available horizontal space
- **AND** no hero/oversized KPI treatment dominates the viewport.

#### Scenario: Mobile operational view
- **WHEN** an authenticated operator opens the dashboard on approximately 390x844 viewport
- **THEN** factory health and attention information are prioritized
- **AND** the operator can reach frequent channel actions without desktop-only interaction assumptions.

### Requirement: Responsive themes

The shell SHALL support light and dark themes from the first release.

#### Scenario: Theme switch
- **WHEN** the application is loaded and the operator switches theme
- **THEN** all dashboard and channel-management controls remain readable and semantically consistent
- **AND** the preference persists for subsequent local sessions.

### Requirement: Dashboard composability

Dashboard implementation SHALL use independently maintainable widgets/sections so future slices can extend it without rewriting the page.

#### Scenario: Initial widget composition
- **WHEN** the first dashboard is rendered
- **THEN** factory health, channel summary and attention summary are separate composable units
- **AND** none assumes future metrics that do not yet exist.

### Requirement: Discovery navigation in application shell

The application shell SHALL provide dedicated navigation items for the Discovery module (Source Catalog and Candidate Triage) accessible to all authenticated operators.

#### Scenario: Shell navigation contains Discovery links
- **WHEN** an authenticated operator navigates using the sidebar or mobile navigation menu
- **THEN** a "Discovery" navigation section with links to "Triage" and "Sources" is available
- **AND** a badge on the Triage link indicates the count of pending candidates awaiting review.

### Requirement: Discovery operational cockpit integration

The dashboard control center SHALL display discovery attention metrics (pending candidate triage queue, active vs error sources count) and provide a Quick Submit trigger to add a URL or note for discovery without leaving the dashboard.

#### Scenario: Dashboard discovery attention widget
- **WHEN** an operator views the dashboard with pending discovery candidates and active sources
- **THEN** the attention widget highlights unreviewed candidates with a direct link to the triage workspace
- **AND** a source health indicator shows total active vs failing discovery sources.

#### Scenario: Quick Submit from dashboard
- **WHEN** an operator clicks the "Quick Submit" action button on the dashboard
- **THEN** a modal or drawer opens with prompt "Add a URL or note for discovery" allowing instant URL or text note entry and channel selection
- **AND** submitting creates a candidate for the selected channel and updates dashboard pending counters immediately.
