# W06 — Jobs, Budgets, Reliability, and Notifications Contract

## Objective

Run persistent, resumable, budget-aware jobs with safe retries, dependency pauses, and role-specific PWA notifications.

## Prerequisite

W05 completed with provider and research boundaries.

The wave refuses to start when the prerequisite is not proven.

## Slices


### W06-S01 — Persistent Job Orchestration

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `CHG-W06-S01-persistent-job-orchestration`
- User Stories: `US-W06-S01-001`, `US-W06-S01-002`, `US-W06-S01-003`
- Branch: `slice/w06-s01-persistent-job-orchestration`

### W06-S02 — Budgets and Cost Control

- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `CHG-W06-S02-budgets-and-cost-control`
- User Stories: `US-W06-S02-001`, `US-W06-S02-002`, `US-W06-S02-003`, `US-W06-S02-004`
- Branch: `slice/w06-s02-budgets-and-cost-control`

### W06-S03 — Retry and Failure Handling

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `CHG-W06-S03-retry-and-failure-handling`
- User Stories: `US-W06-S03-001`, `US-W06-S03-002`, `US-W06-S03-003`
- Branch: `slice/w06-s03-retry-and-failure-handling`

### W06-S04 — Notification Center and Web Push

- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `CHG-W06-S04-notification-center-and-web-push`
- User Stories: `US-W06-S04-001`, `US-W06-S04-002`, `US-W06-S04-003`
- Branch: `slice/w06-s04-notification-center-and-web-push`


## Change map

| Slice | OpenSpec Change | Implementer | Reviewer |
|---|---|---|---|
| W06-S01 | CHG-W06-S01-persistent-job-orchestration | CURSOR | CODEX |
| W06-S02 | CHG-W06-S02-budgets-and-cost-control | CODEX | CURSOR |
| W06-S03 | CHG-W06-S03-retry-and-failure-handling | CURSOR | CODEX |
| W06-S04 | CHG-W06-S04-notification-center-and-web-push | CODEX | CURSOR |

## Exclusions

- No future-wave implementation.
- No direct merge to `main`.
- No hidden deferred acceptance criteria.
- No manual file-editing work delegated to Silverio.

## Human validation

Only when explicitly marked by the slice contract.

## Exit contract

- All User Stories completed.
- All OpenSpec Verify results exactly `PASS`.
- All planned changes synchronized and archived.
- Required builds, lint, type checks, tests, and contract checks pass.
- Documentation and context pack synchronized.
- Cross-review `READY_TO_MERGE`.
- Deployment, smoke tests, and Silverio GO completed when applicable.
- No blockers or hidden deferred work.
- Completion report state `READY_FOR_MAIN`.

## Guarantees to the next wave

The objective is operational and verified; the next wave may depend on its published contracts.
