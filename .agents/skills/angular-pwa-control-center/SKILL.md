---
name: angular-pwa-control-center
description: Use for any Angular, PrimeNG, Tailwind, dashboard, responsive, PWA, frontend architecture, or UI implementation in Content Factory.
---


# Required reading
- docs/canonical/06_UX_UI_CONSTITUTION.md
- docs/canonical/07_DASHBOARD_CONTROL_CENTER.md
- docs/canonical/08_DESIGN_SYSTEM.md
- docs/canonical/22_TECH_STACK_LOCK.md

# Implementation rules
- Angular standalone architecture.
- PrimeNG 21 stable components first when they fit.
- Tailwind 4 for layout/composition.
- Shared layout primitives: every operational page MUST use `.cf-page-container` and `PageHeaderComponent`/`PageToolbarComponent`.
- Full available desktop width: for viewport >=1280px, operational content spans 100% width with 12-16px outer padding (`px-3 sm:px-4 md:px-5`). Never impose `max-w-7xl` or artificial center-column constraints on operational surfaces.
- No RC PrimeNG baseline.
- Route-level lazy loading where useful.
- No page reload navigation.
- Async operations represented without freezing app.
- Desktop viewport first.
- Compact visual density.
- Responsive behavior is designed, not merely stacked.
- Mobile supports urgent actions.
- Tablet preserves near-desktop capability.

# Review
At minimum test:
390x844, 768x1024, 1440x900, 1920x1080.
Test light and dark.
Flag avoidable full-page desktop scroll and arbitrary centered max-width gutters on >=1280px screens as defects.

