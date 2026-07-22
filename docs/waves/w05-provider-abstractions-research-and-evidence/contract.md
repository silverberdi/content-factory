# W05 — Provider Abstractions, Research, and Evidence Contract

## Objective

Integrate interchangeable providers and evidence-aware research without pretending to browse or retaining unnecessary copyrighted material.

## Prerequisite

W04 completed and asset storage operational.

The wave refuses to start when the prerequisite is not proven.

## Slices


### W05-S01 — Provider Capability Registry

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `CHG-W05-S01-provider-capability-registry`
- User Stories: `US-W05-S01-001`, `US-W05-S01-002`, `US-W05-S01-003`, `US-W05-S01-004`
- Branch: `slice/w05-s01-provider-capability-registry`

### W05-S02 — Research Modes and Source Evidence

- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `CHG-W05-S02-research-modes-and-source-evidence`
- User Stories: `US-W05-S02-001`, `US-W05-S02-002`, `US-W05-S02-003`, `US-W05-S02-004`
- Branch: `slice/w05-s02-research-modes-and-source-evidence`

### W05-S03 — Research Retention

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `CHG-W05-S03-research-retention`
- User Stories: `US-W05-S03-001`, `US-W05-S03-002`, `US-W05-S03-003`
- Branch: `slice/w05-s03-research-retention`


## Change map

| Slice | OpenSpec Change | Implementer | Reviewer |
|---|---|---|---|
| W05-S01 | CHG-W05-S01-provider-capability-registry | CURSOR | CODEX |
| W05-S02 | CHG-W05-S02-research-modes-and-source-evidence | CODEX | CURSOR |
| W05-S03 | CHG-W05-S03-research-retention | CURSOR | CODEX |

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
