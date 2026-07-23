# w06 Execution Plan

Wave branch: `wave/w06-jobs-budgets-reliability-and-notifications`

## Ordered slices

1. `w06-s01` — Persistent Job Orchestration — `CURSOR`
2. `w06-s02` — Budgets and Cost Control — `CODEX`
3. `w06-s03` — Retry and Failure Handling — `CURSOR`
4. `w06-s04` — Notification Center and Web Push — `CODEX`

Parallel execution is allowed only when context validation confirms no dependency, file, module,
migration, schema, or contract collision.
