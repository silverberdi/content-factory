# Application Shell and Dashboard Specification

## ADDED Requirements

### Requirement: Content Workspace navigation in application shell

The application shell SHALL provide dedicated navigation items for the Content Workspace (Content Items, Truth Sources, Editorial Tasks) accessible to all authenticated operators.

#### Scenario: Shell navigation contains Content links
- **WHEN** an authenticated operator navigates using the sidebar or mobile navigation menu
- **THEN** a "Content" navigation section with links to "Content Items" and "Editorial Tasks" is available
- **AND** a badge on the Editorial Tasks link indicates the count of pending tasks awaiting review.

### Requirement: Content and TruthSource dashboard widgets

The dashboard control center SHALL display content pipeline stage distribution and pending TruthSource review items in the Attention widget with one-click review actions.

#### Scenario: Dashboard content pipeline stage summary
- **WHEN** an operator views the dashboard
- **THEN** a pipeline health summary displays the count of ContentItems in each lifecycle stage (DraftingEvidence, TruthSourceApproved, etc.)
- **AND** clicking a stage navigates to the filtered Content Workspace view.

#### Scenario: Dashboard TruthSource attention action
- **WHEN** one or more TruthSources are in "UnderReview" status
- **THEN** the Attention widget displays the top urgent review items
- **AND** clicking an item opens the TruthSource Review Studio drawer directly from the dashboard.
