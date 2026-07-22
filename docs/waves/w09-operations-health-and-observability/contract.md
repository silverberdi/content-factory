# W09 — Operations, Health, and Observability Contract

## Objective

Provide dependency health, selective pause/resume, safe logs, durable operational events, and basic consumption visibility.

## Prerequisite

W08 completed with real external integrations exercised.

The wave refuses to start when the prerequisite is not proven.

## Slices


### W09-S01 — Dependency Health Console

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `CHG-W09-S01-dependency-health-console`
- User Stories: `US-W09-S01-001`, `US-W09-S01-002`, `US-W09-S01-003`
- Branch: `slice/w09-s01-dependency-health-console`

### W09-S02 — Selective Pause and Recovery

- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `CHG-W09-S02-selective-pause-and-recovery`
- User Stories: `US-W09-S02-001`, `US-W09-S02-002`, `US-W09-S02-003`
- Branch: `slice/w09-s02-selective-pause-and-recovery`

### W09-S03 — Logging and Operational Events

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `CHG-W09-S03-logging-and-operational-events`
- User Stories: `US-W09-S03-001`, `US-W09-S03-002`, `US-W09-S03-003`, `US-W09-S03-004`
- Branch: `slice/w09-s03-logging-and-operational-events`

### W09-S04 — Basic Operational Analytics

- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `CHG-W09-S04-basic-operational-analytics`
- User Stories: `US-W09-S04-001`, `US-W09-S04-002`, `US-W09-S04-003`, `US-W09-S04-004`
- Branch: `slice/w09-s04-basic-operational-analytics`


## Change map

| Slice | OpenSpec Change | Implementer | Reviewer |
|---|---|---|---|
| W09-S01 | CHG-W09-S01-dependency-health-console | CURSOR | CODEX |
| W09-S02 | CHG-W09-S02-selective-pause-and-recovery | CODEX | CURSOR |
| W09-S03 | CHG-W09-S03-logging-and-operational-events | CURSOR | CODEX |
| W09-S04 | CHG-W09-S04-basic-operational-analytics | CODEX | CURSOR |

## Exclusions

- No future-wave implementation.
- No direct merge to `main`.
- No hidden deferred acceptance criteria.
- No manual file-editing work delegated to Silverio.

## Human validation

Required where acceptance is functional/visual/operational.

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
