---
name: quality-gate
description: Use before declaring any Content Factory OpenSpec change complete, ready for human test, archive, or merge.
---


# Required reading
docs/canonical/16_DEFINITION_OF_DONE.md
docs/canonical/24_TEST_STRATEGY.md

# Execution
Run all applicable gates.
Do not downgrade failures to notes.
Do not declare complete with missing responsive/theme/security checks.

# Verdict
Use:
READY_TO_HUMAN_TEST
or
BLOCKED

For final merge review:
READY_TO_MERGE
or
NOT_READY_TO_MERGE

