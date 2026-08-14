# Application Shell and Dashboard

## ADDED Requirements

### Requirement: Dashboard is the default operational control center

After authentication, the application SHALL open the dashboard as the primary operational view.

#### Scenario: Desktop control center
Given an authenticated operator
And viewport 1440x900 or larger
When the dashboard loads
Then factory health, channel state and current attention summary are visible without avoidable full-page vertical scrolling
And the layout uses available horizontal space
And no hero/oversized KPI treatment dominates the viewport.

#### Scenario: Mobile operational view
Given an authenticated operator on approximately 390x844 viewport
When the dashboard loads
Then factory health and attention information are prioritized
And the operator can reach frequent channel actions without desktop-only interaction assumptions.

### Requirement: Responsive themes

The shell SHALL support light and dark themes from the first release.

#### Scenario: Theme switch
Given the application is loaded
When the operator switches theme
Then all dashboard and channel-management controls remain readable and semantically consistent
And the preference persists for subsequent local sessions.

### Requirement: Dashboard composability

Dashboard implementation SHALL use independently maintainable widgets/sections so future slices can extend it without rewriting the page.

#### Scenario: Initial widget composition
When the first dashboard is rendered
Then factory health, channel summary and attention summary are separate composable units
And none assumes future metrics that do not yet exist.
