# W06 — Jobs, Budgets, Reliability, and Notifications Contract

## Objective

Run persistent, resumable, budget-aware jobs with safe retries, dependency pauses, and role-specific PWA notifications.

## Prerequisite

w05 completed with provider and research boundaries.

The wave refuses to start when the prerequisite is not proven.

## Slices


### W06-S01 — Persistent Job Orchestration

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `chg-w06-s01-persistent-job-orchestration`
- User Stories: `us-w06-s01-001`, `us-w06-s01-002`, `us-w06-s01-003`
- Branch: `slice/w06-s01-persistent-job-orchestration`

### W06-S02 — Budgets and Cost Control

- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `chg-w06-s02-budgets-and-cost-control`
- User Stories: `us-w06-s02-001`, `us-w06-s02-002`, `us-w06-s02-003`, `us-w06-s02-004`
- Branch: `slice/w06-s02-budgets-and-cost-control`

### W06-S03 — Retry and Failure Handling

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `chg-w06-s03-retry-and-failure-handling`
- User Stories: `us-w06-s03-001`, `us-w06-s03-002`, `us-w06-s03-003`
- Branch: `slice/w06-s03-retry-and-failure-handling`

### W06-S04 — Notification Center and Web Push

- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `chg-w06-s04-notification-center-and-web-push`
- User Stories: `us-w06-s04-001`, `us-w06-s04-002`, `us-w06-s04-003`
- Branch: `slice/w06-s04-notification-center-and-web-push`


## Change map

| Slice | OpenSpec Change | Implementer | Reviewer |
|---|---|---|---|
| w06-s01 | chg-w06-s01-persistent-job-orchestration | CURSOR | CODEX |
| w06-s02 | chg-w06-s02-budgets-and-cost-control | CODEX | CURSOR |
| w06-s03 | chg-w06-s03-retry-and-failure-handling | CURSOR | CODEX |
| w06-s04 | chg-w06-s04-notification-center-and-web-push | CODEX | CURSOR |

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
