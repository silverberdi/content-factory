# Editorial Task Attention Specification

## MODIFIED Requirements

### Requirement: Contextual review and dashboard attention integration

Dashboard Attention and the dedicated Editorial Attention view (`editorial-tasks-list.component.ts`) SHALL surface actionable `EditorialTask` items requiring decision (including TruthSource reviews and Script reviews), providing direct contextual deep-links to perform the review work inside the TruthSource Review Studio or Script Studio. The Editorial Attention page SHALL adhere to the canonical full-width operational layout contract without arbitrary centered max-width constraints (such as `max-w-7xl mx-auto`), utilizing the full desktop width for high-density task triage and priority queues.

#### Scenario: Dashboard attention widget displays pending reviews
- **WHEN** one or more TruthSources require review
- **THEN** the dashboard Attention widget highlights the number of pending TruthSource reviews grouped by urgency
- **AND** clicking an item opens the TruthSource Review Studio directly to perform the editorial review.

#### Scenario: Dashboard attention widget displays pending script reviews
- **WHEN** one or more Scripts require editorial review
- **THEN** the dashboard Attention widget includes the count of pending Script reviews
- **AND** clicking an item opens the Script Studio directly on the relevant ContentItem.

#### Scenario: Full-width editorial attention queue on desktop
- **WHEN** an operator navigates to the Editorial Attention page at 1440x900 or 1920x1080
- **THEN** the task list, urgency filter tabs, and action cards span the full available horizontal viewport
- **AND** no centered max-width container restricts operational scanning density.
