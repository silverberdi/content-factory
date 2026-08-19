# Application Shell and Dashboard Specification

## Purpose

Provides the unified responsive application shell, theme preference persistence, and the primary operations cockpit dashboard.

## Requirements

### Requirement: Dashboard is the default operational control center

After authentication, the application SHALL open the dashboard as the primary operational view. All operational views (Dashboard, Triage, Sources, Workspace, Detail, Attention, Channels, System, and specialized studios) SHALL adhere to the canonical full-width operational layout contract.

#### Scenario: Desktop control center
- **WHEN** an authenticated operator opens the dashboard on a viewport 1440x900 or larger
- **THEN** factory health, channel state and current attention summary are visible without avoidable full-page vertical scrolling
- **AND** the layout uses available horizontal space without arbitrary centered max-width containers
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

### Requirement: Canonical full-width operational layout contract and shared page primitives

The application shell and all operational feature pages SHALL adhere to a unified page composition contract:
1. **Desktop Viewport Width**: For viewport widths >= 1280px (including 1440×900 and 1920×1080), operational pages SHALL NOT apply arbitrary centered max-width constraints (such as `max-w-7xl mx-auto`). Page content width SHALL span 100% of the viewport width minus compact page padding (12–16px / `p-3 sm:p-5`). Large screens SHALL gain expanded operational columns, data grids, and contextual panes rather than empty side gutters.
2. **Page Composition Hierarchy**:
   - `PageHeader` (`app-page-header` / `.cf-page-header`): standardized title, optional contextual subtitle, status badges, and top-right primary/secondary action buttons.
   - `PageToolbar` (`app-page-toolbar` / `.cf-page-toolbar`): uniform search input, filter pills, stage selectors, and view options with consistent control height (32–36px) and alignment.
   - `PageContent` (`.cf-page-content`): standardized top vertical rhythm (12–16px gap) and full-width data presentation.
3. **Card & Surface Semantics**: Card containers (`var(--app-card-bg)`) SHALL be used only for semantic grouping, never as default outer wrappers.
4. **Future Screen Inheritance**: Every newly created operational screen in Content Factory SHALL inherit the canonical full-width layout contract and shared primitives by default without bespoke page-level width CSS.

#### Scenario: Full-width operational layout on desktop at 1440x900 and 1920x1080
- **WHEN** an operator views any operational page (Overview, Triage, Sources, Workspace, Attention, Channels, System, or Detail) on a desktop display (1440×900 or 1920×1080)
- **THEN** the page utilizes the available horizontal width minus compact 12–16px padding
- **AND** no large empty gutters flank the operational content
- **AND** primary data tables and operational grids span 100% width.

#### Scenario: Shared page header and action placement
- **WHEN** an operator navigates across different operational modules
- **THEN** page titles, contextual subtitles, and primary action buttons maintain identical vertical rhythm, typography hierarchy, and top-right action placement.

#### Scenario: Shared toolbar alignment and control sizing
- **WHEN** an operator interacts with search inputs, status filters, or channel selectors on any page toolbar
- **THEN** all controls share uniform heights (32–36px), border styling (`var(--app-card-border)`), and spacing gaps.

#### Scenario: Responsive adaptation across tablet and mobile
- **WHEN** viewing operational pages on a tablet (768×1024) or mobile device (390×844)
- **THEN** toolbars wrap gracefully without horizontal page scroll
- **AND** primary actions remain reachable via sticky or top-level action bars
- **AND** tables allow smooth horizontal scroll or stacked card reflow.

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
