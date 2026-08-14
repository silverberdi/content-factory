---
description: Final merge-readiness assessment for a completed Content Factory change.
---

Do not modify feature scope.

Confirm:
- OpenSpec validate PASS;
- OpenSpec verify no CRITICAL;
- tests PASS;
- build/lint PASS;
- security gate PASS where applicable;
- UI gate PASS where applicable;
- DeepSeek verdict READY_TO_MERGE or all objections reconciled;
- docs synchronized;
- human test script exists;
- no secret committed;
- tasks checked.

Output `READY_TO_MERGE` only when every applicable condition is satisfied.
Otherwise output `NOT_READY_TO_MERGE` and exact blockers.
