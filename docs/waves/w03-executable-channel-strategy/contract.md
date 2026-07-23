# W03 — Executable Channel Strategy Contract

## Objective

Create structured AI-assisted channel strategies that become executable only after editorial review and technical approval.

## Prerequisite

w02 completed with configured editorial lines and channels.

The wave refuses to start when the prerequisite is not proven.

## Slices


### W03-S01 — Strategy Authoring

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `chg-w03-s01-strategy-authoring`
- User Stories: `us-w03-s01-001`, `us-w03-s01-002`, `us-w03-s01-003`
- Branch: `slice/w03-s01-strategy-authoring`

### W03-S02 — Strategy Governance

- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `chg-w03-s02-strategy-governance`
- User Stories: `us-w03-s02-001`, `us-w03-s02-002`, `us-w03-s02-003`
- Branch: `slice/w03-s02-strategy-governance`

### W03-S03 — Calendar, Seasons, and Special Dates

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `chg-w03-s03-calendar-seasons-and-special-dates`
- User Stories: `us-w03-s03-001`, `us-w03-s03-002`, `us-w03-s03-003`
- Branch: `slice/w03-s03-calendar-seasons-and-special-dates`


## Change map

| Slice | OpenSpec Change | Implementer | Reviewer |
|---|---|---|---|
| w03-s01 | chg-w03-s01-strategy-authoring | CURSOR | CODEX |
| w03-s02 | chg-w03-s02-strategy-governance | CODEX | CURSOR |
| w03-s03 | chg-w03-s03-calendar-seasons-and-special-dates | CURSOR | CODEX |

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
