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
- no RC PrimeNG baseline.
- route-level lazy loading where useful.
- no page reload navigation.
- async operations represented without freezing app.
- desktop viewport first.
- compact visual density.
- responsive behavior is designed, not merely stacked.
- mobile supports urgent actions.
- tablet preserves near-desktop capability.

# Review
At minimum test:
390x844, 768x1024, 1440x900, 1920x1080.
Test light and dark.
Flag avoidable full-page desktop scroll as a defect.

