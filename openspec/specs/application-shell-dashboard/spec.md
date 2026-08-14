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
