# Proposal: UI Layout Consistency Pass & Future-Screen Guardrails

## Why

Content Factory is an operational control center designed for data-dense, desktop-first productivity across 1440×900 and 1920×1080 displays. Over recent feature increments (Discovery, TruthSource, Ideas, Scripts), individual screens evolved divergent layout constraints:
- Screens such as **Content Workspace**, **Editorial Attention**, **Content Detail**, **TruthSource Studio**, **Idea Matrix**, and **Script Studio** were accidentally bounded inside centered `max-w-7xl mx-auto` containers, wasting significant horizontal space and introducing wide empty side gutters on desktop monitors.
- Screens such as **Discovery Triage**, **Discovery Sources**, **Channels**, **System**, and **Dashboard** used wider `max-w-full` layouts, but differed in page header typography, toolbar control heights, filter button padding, search input alignment, and action button placement.
- UI labels developed mixed-language inconsistencies across adjacent modules (e.g. English shell navigation next to Spanish-only page headings and action labels).

This change establishes the **canonical operational layout system**, introduces reusable layout primitives and tokens, migrates all existing operational screens to this contract, and encodes durable frontend and agent guardrails so that all future screens (Storyboard, Production Planning, Rendering, Assembly, QA, Publication, Analytics) automatically inherit full-width operational density by default.

## What Changes

- **Canonical Desktop Width Contract**:
  - For viewports >= 1280px, operational pages SHALL NOT apply arbitrary centered max-width wrappers (e.g., `max-w-7xl mx-auto`). Page content width spans full viewport width minus compact shell/page padding (12–16px / `p-3 sm:p-5`).
  - Tables, grids, split workspaces, and operational surfaces utilize 100% of available horizontal space.
- **Shared Page Composition Primitives & Utilities**:
  - Establish lightweight, reusable Angular layout primitives / CSS component patterns:
    - `PageHeader` (`app-page-header` / `.cf-page-header`): consistent title, subtitle, badges, and upper-right primary/secondary actions.
    - `PageToolbar` (`app-page-toolbar` / `.cf-page-toolbar`): standardized search input, filter pills, view toggles, and contextual actions with uniform heights and alignment.
    - `PageContent` (`.cf-page-content`): standardized top vertical rhythm and density.
  - Reusable layout utility classes in Tailwind 4 / styles for consistent card padding, section gaps, borders, and typography hierarchy.
- **Existing Screens Migration**:
  - **Content Workspace** (`content-list.component.ts`): Remove `max-w-7xl mx-auto`, adopt canonical header and filter toolbar, expand data table and summary grid across full viewport.
  - **Editorial Attention & Tasks** (`editorial-tasks-list.component.ts`): Remove `max-w-7xl mx-auto`, expand task list and urgency groups.
  - **Content Detail** (`content-detail.component.ts`): Remove `max-w-7xl mx-auto`, expand 2-column layout to utilize extra desktop width for evidence and script previews.
  - **Specialized Studios** (`truth-source-review-studio.component.ts`, `content-ideas.component.ts`, `script-studio.component.ts`): Remove `max-w-7xl mx-auto` constraints; preserve specialized internal workflows (split panes, beat cards, evidence drawers, advisory review panels) while inheriting canonical outer shell width and header alignment.
  - **Discovery Triage, Discovery Sources, Channels, Dashboard, System**: Normalize headers, toolbars, search inputs, and table padding against the shared primitives.
- **Language & Typography Normalization**:
  - Normalize operator-facing headings and action labels to consistent product language across modules.
- **Durable Future-Screen Guardrails**:
  - Update canonical UX documentation (`docs/canonical/06_UX_UI_CONSTITUTION.md`, `docs/canonical/08_DESIGN_SYSTEM.md`, `docs/canonical/16_DEFINITION_OF_DONE.md`), `.agents/CONTEXT.md`, and frontend skills (`angular-pwa-control-center`) to mandate that every new operational screen MUST use the shared full-width layout contract and pass visual compliance at 1440×900 and 1920×1080.

## Capabilities

### Modified Capabilities
- `application-shell-dashboard`: Define canonical full-width operational layout contract, shared page header/toolbar/content composition rules, desktop width utilization (>=1280px without `max-w-7xl` constraints), compact padding tokens, and normalized operator UI hierarchy.
- `content-workspace`: Update Content Workspace, Content Detail, and specialized studios (TruthSource, Ideas, Scripts) to inherit canonical full-width layout, remove accidental `max-w-7xl` wrappers, and align headers/toolbars with shared primitives.
- `editorial-task-attention`: Update Editorial Attention to inherit canonical full-width layout, remove accidental `max-w-7xl` wrappers, and align urgency filters and action cards with shared primitives.

## Impact

- **Affected Code**:
  - `src/web/src/app/shared/components/` or `src/web/src/app/shared/layout/`: Lightweight shared layout components/primitives (`PageHeaderComponent`, `PageToolbarComponent` or directive/CSS utilities).
  - `src/web/src/styles.css`: Canonical layout utility classes, spacing tokens, and table/toolbar presets.
  - `src/web/src/app/features/content/`: `content-list.component.ts`, `editorial-tasks-list.component.ts`, `content-detail.component.ts`, `truth-source-review-studio.component.ts`, `content-ideas.component.ts`, `script-studio.component.ts`.
  - `src/web/src/app/features/discovery/`: `discovery-triage.component.ts`, `discovery-sources.component.ts`.
  - `src/web/src/app/features/channels/`: `channels.component.ts`.
  - `src/web/src/app/features/dashboard/`: `dashboard.component.ts`.
  - `src/web/src/app/features/system/`: `system.component.ts`.
  - `docs/canonical/`: `06_UX_UI_CONSTITUTION.md`, `08_DESIGN_SYSTEM.md`, `16_DEFINITION_OF_DONE.md`.
  - `.agents/`: `CONTEXT.md`, `skills/angular-pwa-control-center/SKILL.md`.
- **APIs & Backend**: **Zero changes** (purely frontend transversal layout, CSS/Angular architecture, and durable governance).
- **Dependencies**: No new npm dependencies, no Angular or PrimeNG version changes.
