# Design System Principles

## Libraries

PrimeNG is a first-class UI component foundation, not a token dependency.
Use its mature components where they match the use case:
- Table/DataView;
- Dialog/Drawer;
- Menu/ContextMenu;
- Toast/Message;
- Badge/Tag;
- Tabs/Stepper;
- Select/Autocomplete;
- Date controls;
- Tooltip;
- chart integration where appropriate.

Tailwind provides layout/composition and utility styling.
Prefer official PrimeNG/Tailwind integration over ad-hoc duplication.

## Layout System & Shared Primitives

All operational screens build on standard layout primitives:
- **`PageHeaderComponent`** (`src/web/src/app/shared/layout/page-header.component.ts`): Standalone header containing title, subtitle, optional status badge, optional back navigation link, `[meta]` metadata slot, and `[actions]` action buttons slot.
- **`PageToolbarComponent`** (`src/web/src/app/shared/layout/page-toolbar.component.ts`): Standalone toolbar providing a `[start]` slot for search inputs and filter selectors, and an `[end]` slot for auxiliary actions.
- **Canonical Utility Classes** (`src/web/src/styles.css`):
  - `.cf-page-container`: Full-width responsive container (`w-full max-w-full px-3 sm:px-4 md:px-5 py-2 sm:py-3 space-y-4`).
  - `.cf-card`: Standardized semantic card container (`bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-xl shadow-2xs`).
  - `.cf-toolbar-control`: Uniform input/select control for operational filters (`h-8 text-xs px-2.5 rounded-lg bg-[var(--app-card-bg)] border border-[var(--app-card-border)] text-[var(--app-text)]`).
  - `.cf-btn-primary`: Consistent primary button (`h-8 px-3.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-white font-bold text-xs flex items-center gap-1.5 cursor-pointer shadow-2xs`).
  - `.cf-btn-secondary`: Consistent secondary button (`h-8 px-3 py-1.5 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-card-bg)] hover:bg-[var(--app-surface-hover)] text-[var(--app-text)] text-xs font-semibold flex items-center gap-1.5 cursor-pointer shadow-2xs`).

## Theme

Light and dark from day one.

Use semantic tokens:
- surface;
- text;
- muted;
- primary;
- success;
- warning;
- danger;
- info.

Never encode meaning only through color.

## Density

Default desktop density: compact/comfortable operational density.
Touch devices increase interaction target size without turning every control into a giant card.

## Status vocabulary

Factory health:
- healthy
- degraded
- attention-required

Jobs:
- queued
- running
- succeeded
- failed-retryable
- failed-action-required
- cancelled

Editorial:
- draft
- needs-review
- approved
- rejected
- rewrite-requested

Channel:
- idea
- setup-pending
- pilot
- active
- scaling
- paused
- archived

Status appearance must be consistent application-wide.

## UX states

Every asynchronous view/action must define:
- loading;
- empty;
- success feedback;
- recoverable error;
- blocking error;
- disabled/permission state.

Use skeletons for structured loading where helpful; do not lock the full application for background Jobs.
