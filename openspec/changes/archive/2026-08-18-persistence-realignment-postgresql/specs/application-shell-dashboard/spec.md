# Application Shell and Dashboard Specification

## MODIFIED Requirements

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
