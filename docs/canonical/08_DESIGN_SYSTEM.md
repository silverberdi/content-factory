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
