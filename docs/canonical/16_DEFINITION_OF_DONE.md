# Global Definition of Done

A change closes only when all applicable gates pass.

## Functional

- acceptance criteria PASS;
- state transitions/invariants PASS;
- no hidden manual step;
- seeded/test data supports human verification.

## OpenSpec

- artifacts internally coherent;
- `openspec validate <change>` PASS;
- `/opsx:verify <change>` has zero CRITICAL findings;
- warnings are resolved or explicitly accepted with rationale;
- completed tasks checked.

## Code

- build PASS;
- automated tests PASS;
- static/lint checks PASS;
- DeepSeek cross-review READY_TO_MERGE;
- no unauthorized scope changes.

## Security

When applicable:
- authorization tests;
- negative permission tests;
- secret scan/no committed secrets;
- production bypass protection;
- input validation;
- audit behavior.

## UX

When frontend changes:
- desktop 1440×900 review;
- desktop 1920×1080 review;
- tablet review;
- mobile review;
- light theme;
- dark theme;
- loading/empty/error states;
- keyboard/accessibility baseline;
- no unnecessary full-page desktop scroll;
- dashboard density and hierarchy follow constitution;
- no arbitrary centered `max-w-7xl` container constraints on desktop operational pages;
- tables, grids, and workspaces span 100% width using `.cf-page-container` and shared layout primitives (`PageHeaderComponent`, `PageToolbarComponent`).

## Performance/behavior

- long operations do not freeze UI;
- repeated navigation does not full-page reload;
- relevant async work represented as Jobs.

## Documentation

- affected canonical docs synchronized;
- OpenAPI updated if API changed;
- migration/setup notes updated;
- walkthrough contains human test procedure and evidence.
