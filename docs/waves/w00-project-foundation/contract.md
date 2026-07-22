# W00 — Project Foundation Contract

## Objective

Establish the governed monorepo, canonical documentation, OpenSpec workflow, CI, container foundations, and Angular/PrimeNG application shell.

## Prerequisite

Proven before W00 formal execution:

- Public Content Factory repository initialized (not empty).
- Canonical project definition imported into the repository.
- OpenSpec `1.6.0` installed.
- Cursor and Codex OpenSpec integrations generated (commands and skills present).

The wave refuses to start when the prerequisite is not proven.

## Bootstrap note

Canonical planning docs, agent governance files, context scripts, and related repository
artifacts already present in the tree are **pre-existing candidate implementation**. They are not
completed W00 delivery. They must be adopted, reviewed, corrected, verified, synchronized, and
archived through `CHG-W00-S01-repository-governance-and-openspec-foundation`. No User Story or
slice is marked completed by the presence of these bootstrap artifacts.

## Wave state

`READY`

## Slices


### W00-S01 — Repository Governance and OpenSpec Foundation

- State: `READY`
- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `CHG-W00-S01-repository-governance-and-openspec-foundation`
- User Stories: `US-W00-S01-001`, `US-W00-S01-002`, `US-W00-S01-003`, `US-W00-S01-004`
- Branch: `slice/w00-s01-repository-governance-and-openspec-foundation`

### W00-S02 — Nx Application Foundation

- State: `PLANNED`
- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `CHG-W00-S02-nx-application-foundation`
- User Stories: `US-W00-S02-001`, `US-W00-S02-002`, `US-W00-S02-003`, `US-W00-S02-004`, `US-W00-S02-005`
- Branch: `slice/w00-s02-nx-application-foundation`

### W00-S03 — Container and Data Foundation

- State: `PLANNED`
- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `CHG-W00-S03-container-and-data-foundation`
- User Stories: `US-W00-S03-001`, `US-W00-S03-002`, `US-W00-S03-003`, `US-W00-S03-004`
- Branch: `slice/w00-s03-container-and-data-foundation`

### W00-S04 — Continuous Integration and Quality Gates

- State: `PLANNED`
- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `CHG-W00-S04-continuous-integration-and-quality-gates`
- User Stories: `US-W00-S04-001`, `US-W00-S04-002`, `US-W00-S04-003`, `US-W00-S04-004`
- Branch: `slice/w00-s04-continuous-integration-and-quality-gates`


## Change map

| Slice | OpenSpec Change | Implementer | Reviewer | State |
|---|---|---|---|---|
| W00-S01 | CHG-W00-S01-repository-governance-and-openspec-foundation | CURSOR | CODEX | READY |
| W00-S02 | CHG-W00-S02-nx-application-foundation | CODEX | CURSOR | PLANNED |
| W00-S03 | CHG-W00-S03-container-and-data-foundation | CURSOR | CODEX | PLANNED |
| W00-S04 | CHG-W00-S04-continuous-integration-and-quality-gates | CODEX | CURSOR | PLANNED |

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
