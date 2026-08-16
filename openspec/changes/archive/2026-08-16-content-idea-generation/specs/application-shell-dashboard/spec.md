# Application Shell and Dashboard Specification

## MODIFIED Requirements

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
