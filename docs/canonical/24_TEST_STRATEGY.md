# Test Strategy

## Test pyramid adapted to product risk

Backend:
- domain/unit tests for invariants;
- API integration tests;
- authorization tests;
- persistence integration tests for migrations/queries;
- idempotency/concurrency tests where applicable.

Frontend:
- component tests for critical interaction logic;
- service/state tests;
- Playwright e2e for value flows;
- responsive visual evidence for critical screens.

## Mandatory UX viewports for relevant changes

- 390×844 mobile baseline;
- 768×1024 tablet baseline;
- 1440×900 desktop baseline;
- 1920×1080 desktop baseline.

Exact device emulation may vary; behavior must satisfy the UX constitution.

## Theme

Critical paths tested in light and dark.

## Security

Tests must prove forbidden actions, not only successful actions.

Examples:
- EDITORIAL cannot perform TECHNICAL-only administration;
- invited-but-not-activated identity cannot use the application;
- non-invited Google identity cannot gain access;
- production cannot start with development bypass.

## Human test

Each OpenSpec change includes a short reproducible human test script and required seed data.
