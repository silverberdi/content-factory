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

#### Scenario: Factory health reports PostgreSQL connectivity
- **WHEN** an authenticated operator views the dashboard with an active relational database connection
- **THEN** the factory health widget displays database status indicating PostgreSQL connectivity (e.g. `Connected (PostgreSQL/content_factory_dev)` or `Connected (PostgreSQL/content_factory_prod)`)
- **AND** when running against in-memory fallback, it displays `InMemory (Test/Fallback)`
- **AND** no obsolete MySQL provider labels or references are displayed.

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

### Requirement: Content Workspace navigation in application shell

The application shell SHALL provide dedicated navigation items for the Content Workspace (Content Items, Truth Sources, Editorial Tasks) accessible to all authenticated operators.

#### Scenario: Shell navigation contains Content links
- **WHEN** an authenticated operator navigates using the sidebar or mobile navigation menu
- **THEN** a "Content" navigation section with links to "Content Items" and "Editorial Tasks" is available
- **AND** a badge on the Editorial Tasks link indicates the count of pending tasks awaiting review.

### Requirement: Content and TruthSource dashboard widgets

The dashboard control center SHALL display content pipeline stage distribution (including `TruthSourceApproved` awaiting ideas, and `IdeaSelected`) and pending editorial review and idea generation items in the Attention widget with one-click review actions.

#### Scenario: Dashboard content pipeline stage summary
- **WHEN** an operator views the dashboard
- **THEN** a pipeline health summary displays the count of ContentItems in each lifecycle stage (DraftingEvidence, TruthSourceApproved, IdeaSelected, etc.)
- **AND** clicking a stage navigates to the filtered Content Workspace view.

#### Scenario: Dashboard TruthSource attention action
- **WHEN** one or more TruthSources are in "UnderReview" status
- **THEN** the Attention widget displays the top urgent review items
- **AND** clicking an item opens the TruthSource Review Studio drawer directly from the dashboard.

#### Scenario: Dashboard Idea attention action
- **WHEN** a ContentItem has an approved TruthSource but zero generated ideas or no selected idea
- **THEN** the Attention widget surfaces an actionable suggestion to generate or select ideas for that production thread.
