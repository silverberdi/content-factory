## ADDED Requirements

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
