# Implementation Tasks: UI Layout Consistency Pass & Future-Screen Guardrails

## 1. Shared Layout Primitives & Spacing Tokens

- [x] 1.1 Create `PageHeaderComponent` (`src/web/src/app/shared/layout/page-header.component.ts`) with standardized title, subtitle, status badges, and top-right action slots.
- [x] 1.2 Create `PageToolbarComponent` (`src/web/src/app/shared/layout/page-toolbar.component.ts`) with uniform search input styling, filter pill groups, and action slots.
- [x] 1.3 Define canonical CSS layout classes and tokens in `src/web/src/styles.css` (`.cf-page-container`, `.cf-card`, `.cf-table`, `.cf-toolbar-control`, uniform padding and border variables).

## 2. Migrate Core Operational Screens

- [x] 2.1 Migrate **Content Workspace** (`content-list.component.ts`) to full-width `cf-page-container`, remove `max-w-7xl mx-auto`, and adopt canonical `PageHeader` and `PageToolbar`.
- [x] 2.2 Migrate **Editorial Attention & Tasks** (`editorial-tasks-list.component.ts`) to full-width `cf-page-container`, remove `max-w-7xl mx-auto`, and adopt canonical `PageHeader` and `PageToolbar`.
- [x] 2.3 Migrate **Content Detail** (`content-detail.component.ts`) to full-width `cf-page-container`, remove `max-w-7xl mx-auto`, and adopt canonical `PageHeader`.
- [x] 2.4 Migrate **Channels** (`channels.component.ts`) and **System** (`system.component.ts`) to canonical `PageHeader` and `PageToolbar`.
- [x] 2.5 Migrate **Discovery Triage** (`discovery-triage.component.ts`) and **Discovery Sources** (`discovery-sources.component.ts`) to canonical `PageHeader` and `PageToolbar`.
- [x] 2.6 Migrate **Dashboard Overview** (`dashboard.component.ts`) to canonical `PageHeader` and normalized widget spacing.

## 3. Migrate Specialized Editorial Studios

- [x] 3.1 Remove `max-w-7xl mx-auto` and align outer header/toolbar in **TruthSource Review Studio** (`truth-source-review-studio.component.ts`) while preserving split-pane evidence inspection.
- [x] 3.2 Remove `max-w-7xl mx-auto` and align outer header/toolbar in **Idea Matrix** (`content-ideas.component.ts`) while preserving idea workflow cards.
- [x] 3.3 Remove `max-w-7xl mx-auto` and align outer header/toolbar in **Script Studio** (`script-studio.component.ts`) while preserving beat timeline, side context panels, and sticky action bar.

## 4. UI Language Normalization & Automated Test Suite

- [x] 4.1 Normalize visible operator UI labels and headings across modules for consistent product language.
- [x] 4.2 Update and execute frontend unit test suite (`npm test`) covering shared primitives and migrated screens; verify production build (`npm run build`).

## 5. Durable Canonical Guardrails & Verification Gate

- [x] 5.1 Update canonical UX docs (`06_UX_UI_CONSTITUTION.md`, `08_DESIGN_SYSTEM.md`, `16_DEFINITION_OF_DONE.md`), `.agents/CONTEXT.md`, and `.agents/skills/angular-pwa-control-center/SKILL.md` with the mandatory full-width operational layout rule.
- [x] 5.2 Perform human visual verification across target viewports (1440×900, 1920×1080, 768×1024, 390×844) in both light and dark modes.
- [x] 5.3 Run DeepSeek cross-review and OpenSpec verification gate before completion.

