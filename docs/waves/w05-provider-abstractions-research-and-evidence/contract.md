# W05 — Provider Abstractions, Research, and Evidence Contract

## Objective

Integrate interchangeable providers and evidence-aware research without pretending to browse or retaining unnecessary copyrighted material.

## Prerequisite

w04 completed and asset storage operational.

The wave refuses to start when the prerequisite is not proven.

## Slices


### W05-S01 — Provider Capability Registry

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `chg-w05-s01-provider-capability-registry`
- User Stories: `us-w05-s01-001`, `us-w05-s01-002`, `us-w05-s01-003`, `us-w05-s01-004`
- Branch: `slice/w05-s01-provider-capability-registry`

### W05-S02 — Research Modes and Source Evidence

- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `chg-w05-s02-research-modes-and-source-evidence`
- User Stories: `us-w05-s02-001`, `us-w05-s02-002`, `us-w05-s02-003`, `us-w05-s02-004`
- Branch: `slice/w05-s02-research-modes-and-source-evidence`

### W05-S03 — Research Retention

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `chg-w05-s03-research-retention`
- User Stories: `us-w05-s03-001`, `us-w05-s03-002`, `us-w05-s03-003`
- Branch: `slice/w05-s03-research-retention`


## Change map

| Slice | OpenSpec Change | Implementer | Reviewer |
|---|---|---|---|
| w05-s01 | chg-w05-s01-provider-capability-registry | CURSOR | CODEX |
| w05-s02 | chg-w05-s02-research-modes-and-source-evidence | CODEX | CURSOR |
| w05-s03 | chg-w05-s03-research-retention | CURSOR | CODEX |

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
