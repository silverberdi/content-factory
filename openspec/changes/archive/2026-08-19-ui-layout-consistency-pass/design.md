# Technical Design: Canonical UI Layout System & Future-Screen Guardrails

## Context

Content Factory is an operational control center designed for high-density, desktop-first productivity across 1440×900 and 1920×1080 displays. Currently:
- Several operational screens (**Content Workspace**, **Editorial Attention**, **Content Detail**, **TruthSource Studio**, **Idea Matrix**, and **Script Studio**) were wrapped inside `max-w-7xl mx-auto` containers, restricting width to 80rem (1280px) and leaving massive empty side gutters on wider viewports.
- Other screens (**Discovery Triage**, **Discovery Sources**, **Channels**, **System**, **Dashboard**) used `max-w-full` but suffered from inconsistent header hierarchy, divergent toolbar control heights, uneven search input padding, and varied button placement.
- No shared Angular layout primitives existed, causing each new feature to write bespoke layout markup.

## Goals / Non-Goals

**Goals:**
- Establish the canonical full-width operational layout contract: 100% viewport width utilization minus compact 12–16px padding on desktop (>=1280px), with no arbitrary centered max-width wrappers.
- Introduce lightweight shared layout primitives and CSS utility classes (`PageHeaderComponent`, `PageToolbarComponent`, `.cf-page-container`, `.cf-card`, `.cf-toolbar-control`).
- Migrate all existing operational pages (Workspace, Attention, Detail, Studios, Triage, Sources, Channels, Dashboard, System) to the shared layout contract.
- Standardize UI language across operator-facing headings, toolbars, and action buttons.
- Encode durable future-screen guardrails in canonical UX documentation, `.agents/CONTEXT.md`, Definition of Done, and agent skills so future operational screens inherit full-width density by default.

**Non-Goals:**
- No backend, API, or database changes.
- No modifications to domain lifecycles (ContentItem, TruthSource, Idea, Script).
- No new external npm dependencies; no Angular 21 or PrimeNG 21 upgrades.
- No redesign of specialized studio internal workflows (split-pane evidence view, beat timeline, idea ranking).

## Decisions

### 1. Reusable Layout Mechanism: Hybrid Primitives + Utility Classes
- **Decision**: Create lightweight Angular standalone primitives (`PageHeaderComponent`, `PageToolbarComponent` in `src/web/src/app/shared/layout/`) coupled with standardized Tailwind/CSS utilities in `src/web/src/styles.css` (`.cf-page-container`, `.cf-card`, `.cf-table`, `.cf-toolbar-control`).
- **Rationale**: Standalone primitives enforce uniform visual hierarchy for page titles, subtitles, badges, and top-right primary actions without rigid templates. CSS utility classes provide seamless styling for tables, filters, and cards across diverse layouts.
- **Alternatives Considered**:
  - *Monolithic Page Template Component (`<app-operational-page>`)*: Rejected because specialized studios (TruthSource, Script Studio) require customized internal flex/grid compositions that a rigid template would artificially constrain.
  - *Tailwind Utility Classes Only (no Angular components)*: Rejected because header and toolbar markup would still be copy-pasted and diverge over time.

### 2. Desktop Width Contract (>=1280px, 1440x900, 1920x1080)
- **Decision**: Outer operational layout uses `w-full max-w-full` with shell padding `p-3 sm:p-5`. All `max-w-7xl mx-auto` wrappers are removed from operational surfaces. Tables, triage lists, and split panels expand to fill the full container width.
- **Rationale**: Desktop monitors gain usable horizontal real estate for multi-column data, side-by-side evidence inspection, and dense script beats, eliminating wasted blank margins.
- **Alternatives Considered**:
  - *Keeping `max-w-7xl` on list pages and only widening studios*: Rejected because data-heavy tables (Content Workspace, Attention, Channels) benefit significantly from full-width column distribution.

### 3. Specialized Studio Composition Protection
- **Decision**: Studios (TruthSource Review Studio, Idea Matrix, Script Studio) inherit the canonical outer shell, `PageHeader`, and `PageToolbar`, but retain their specialized internal layouts (e.g. split evidence pane, beat timeline, sticky governance action bars).
- **Rationale**: Preserves domain-optimized workflows while ensuring visual alignment with the rest of the application.

### 4. Durable Guardrails Strategy
- **Decision**: Update `docs/canonical/06_UX_UI_CONSTITUTION.md`, `08_DESIGN_SYSTEM.md`, `16_DEFINITION_OF_DONE.md`, `.agents/CONTEXT.md`, and `.agents/skills/angular-pwa-control-center/SKILL.md`. Mandate that every new screen must use the shared layout primitives and verify 1440×900 and 1920×1080 full-width utilization before passing the Quality Gate.
- **Rationale**: Changes in agent instructions and canonical docs prevent future implementation agents from reintroducing arbitrary centered max-width containers.

## Screen Migration Mapping

| Screen / Component | Current State | Target State |
| :--- | :--- | :--- |
| **Workspace** (`content-list.component.ts`) | `max-w-7xl mx-auto` | Full-width `cf-page-container`, canonical `PageHeader` + `PageToolbar`, full-width table & stage filters |
| **Attention** (`editorial-tasks-list.component.ts`) | `max-w-7xl mx-auto` | Full-width `cf-page-container`, canonical `PageHeader` + `PageToolbar`, full-width task cards |
| **Content Detail** (`content-detail.component.ts`) | `max-w-7xl mx-auto` | Full-width `cf-page-container`, canonical `PageHeader`, expanded 2-column operational layout |
| **TruthSource Studio** (`truth-source-review-studio.component.ts`) | `max-w-7xl mx-auto` | Full-width shell, canonical header, expanded split evidence/claim editor |
| **Idea Matrix** (`content-ideas.component.ts`) | `max-w-7xl mx-auto` | Full-width shell, canonical header, expanded idea grid |
| **Script Studio** (`script-studio.component.ts`) | `max-w-7xl mx-auto` | Full-width shell, canonical header, expanded beat timeline & side context panels |
| **Triage** (`discovery-triage.component.ts`) | `max-w-full`, bespoke header/toolbar | Canonical `PageHeader` + `PageToolbar`, unified search & filter styling |
| **Sources** (`discovery-sources.component.ts`) | `max-w-full`, bespoke header/toolbar | Canonical `PageHeader` + `PageToolbar`, unified table & modal trigger |
| **Channels** (`channels.component.ts`) | `max-w-full`, bespoke header/toolbar | Canonical `PageHeader` + `PageToolbar`, unified table styling |
| **Dashboard** (`dashboard.component.ts`) | `max-w-full`, bespoke header | Canonical `PageHeader`, normalized widget gaps and card borders |
| **System** (`system.component.ts`) | `max-w-full`, bespoke header | Canonical `PageHeader`, normalized tab and log layout |

## Risks / Trade-offs

- **[Risk] Wide Tables with Sparse Columns**: Tables with only 3–4 columns might feel overly stretched at 1920×1080.
  - *Mitigation*: Distribute column widths intentionally (e.g. fixed widths on badges/actions, generous flex on descriptive titles/summaries) and use subtle zebra/hover row styling.
- **[Risk] Mobile Layout Regression**: Widening desktop layouts could accidentally cause horizontal overflow on mobile viewports.
  - *Mitigation*: Preserve responsive breakpoints (`sm:`, `md:`, `lg:`), flex-wrapping toolbars, and verify 390×844 on all screens.
- **[Risk] Agent Drift in Future Changes**: Future agents might ignore shared primitives and write bespoke layouts.
  - *Mitigation*: Encode explicit rules in `CONTEXT.md`, `06_UX_UI_CONSTITUTION.md`, and `16_DEFINITION_OF_DONE.md`.
