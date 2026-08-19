# Content Workspace Specification

## MODIFIED Requirements

### Requirement: Content workspace high-density management UI

The content workspace UI SHALL provide high-density scanning, filtering (by channel, lifecycle stage, status, and search query), and detail navigation to answer "where is this piece?" in the editorial lifecycle. The workspace and detail views SHALL follow the canonical full-width operational layout contract without arbitrary centered max-width constraints (such as `max-w-7xl mx-auto`), spanning 100% available horizontal space on desktop viewports (>=1280px).

#### Scenario: Filter content items by channel and stage
- **WHEN** an operator selects channel "IA Simple ES" and filters by stage "DraftingEvidence"
- **THEN** the workspace displays matching items with title, linked evidence count, current truth source state, last updated time, and quick action buttons
- **AND** the view adapts to full desktop width without avoidable vertical scroll.

#### Scenario: ContentItem detail drill-down
- **WHEN** an operator opens a ContentItem detail view
- **THEN** the view displays the operational header, the multi-evidence provenance panel (with SHA-256 hashes, capture status, retry button for failed captures, and source links), the TruthSource panel, and actions to generate draft or review evidence.

#### Scenario: Full-width workspace data table on desktop
- **WHEN** an operator opens the Content Workspace at 1440x900 or 1920x1080
- **THEN** the content table and filter toolbar span the full available viewport width minus compact padding
- **AND** no centered max-width constraint restricts horizontal density.

#### Scenario: Specialized editorial studios inherit canonical outer shell
- **WHEN** an operator navigates to TruthSource Review Studio, Idea Matrix, or Script Studio
- **THEN** the studio utilizes the full horizontal viewport width for split-pane evidence inspection, idea cards, and scene timelines
- **AND** the outer header and toolbar align consistently with shared page primitives while preserving internal specialized workflows.
