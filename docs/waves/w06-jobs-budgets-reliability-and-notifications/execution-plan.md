# W06 Execution Plan

Wave branch: `wave/w06-jobs-budgets-reliability-and-notifications`

## Ordered slices

1. `W06-S01` — Persistent Job Orchestration — `CURSOR`
2. `W06-S02` — Budgets and Cost Control — `CODEX`
3. `W06-S03` — Retry and Failure Handling — `CURSOR`
4. `W06-S04` — Notification Center and Web Push — `CODEX`

Parallel execution is allowed only when context validation confirms no dependency, file, module,
migration, schema, or contract collision.
