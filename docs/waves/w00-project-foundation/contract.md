# W00 — Project Foundation Contract

## Objective

Establish the governed monorepo, canonical documentation, OpenSpec workflow, CI, container foundations, and Angular/PrimeNG application shell.

## Prerequisite

Proven before w00 formal execution:

- Public Content Factory repository initialized (not empty).
- Canonical project definition imported into the repository.
- OpenSpec `1.6.0` installed.
- Cursor and Codex OpenSpec integrations generated (commands and skills present).

The wave refuses to start when the prerequisite is not proven.

## Bootstrap note

Canonical planning docs, agent governance files, context scripts, and related repository
artifacts already present in the tree are **pre-existing candidate implementation**. They are not
completed w00 delivery. They must be adopted, reviewed, corrected, verified, synchronized, and
archived through `chg-w00-s01-repository-governance-and-openspec-foundation`. No User Story or
slice is marked completed by the presence of these bootstrap artifacts.

## Wave state

`IN_PROGRESS`

## Slices


### W00-S01 — Repository Governance and OpenSpec Foundation

- State: `IN_PROGRESS`
- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `chg-w00-s01-repository-governance-and-openspec-foundation`
- User Stories: `us-w00-s01-001`, `us-w00-s01-002`, `us-w00-s01-003`, `us-w00-s01-004`
- Branch: `slice/w00-s01-repository-governance-and-openspec-foundation`

### W00-S02 — Nx Application Foundation

- State: `PLANNED`
- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `chg-w00-s02-nx-application-foundation`
- User Stories: `us-w00-s02-001`, `us-w00-s02-002`, `us-w00-s02-003`, `us-w00-s02-004`, `us-w00-s02-005`
- Branch: `slice/w00-s02-nx-application-foundation`

### W00-S03 — Container and Data Foundation

- State: `PLANNED`
- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `chg-w00-s03-container-and-data-foundation`
- User Stories: `us-w00-s03-001`, `us-w00-s03-002`, `us-w00-s03-003`, `us-w00-s03-004`
- Branch: `slice/w00-s03-container-and-data-foundation`

### W00-S04 — Continuous Integration and Quality Gates

- State: `PLANNED`
- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `chg-w00-s04-continuous-integration-and-quality-gates`
- User Stories: `us-w00-s04-001`, `us-w00-s04-002`, `us-w00-s04-003`, `us-w00-s04-004`
- Branch: `slice/w00-s04-continuous-integration-and-quality-gates`


## Change map

| Slice | OpenSpec Change | Implementer | Reviewer | State |
|---|---|---|---|---|
| w00-s01 | chg-w00-s01-repository-governance-and-openspec-foundation | CURSOR | CODEX | IN_PROGRESS |
| w00-s02 | chg-w00-s02-nx-application-foundation | CODEX | CURSOR | PLANNED |
| w00-s03 | chg-w00-s03-container-and-data-foundation | CURSOR | CODEX | PLANNED |
| w00-s04 | chg-w00-s04-continuous-integration-and-quality-gates | CODEX | CURSOR | PLANNED |

## Complete-slice ownership

Each slice is owned end to end by one implementer (`CURSOR` or `CODEX`). The non-implementing
operator performs mandatory cross-review with verdict exactly `READY_TO_MERGE` or
`CHANGES_REQUIRED`. Difficulty does not authorize reassignment or abandonment. Only a genuine
external blocker may pause work under the same operator.

## Basic GitHub protection (w00-s01)

`w00-s01` must establish and evidence:

- `main` rejects direct pushes;
- `main` rejects force pushes and deletion;
- pull requests are required for `main`;
- `wave/*` follows the slice-to-wave pull-request model;
- slice PRs target `wave/*`;
- wave PRs target `main`;
- Silverio manually merges completed waves to `main`.

## Exclusions

- No future-wave implementation.
- No direct merge to `main`.
- No hidden deferred acceptance criteria.
- No manual file-editing work delegated to Silverio.
- No GitHub Actions checks, Nx validation, CI-driven merge gates, or fully automated slice
  auto-merge in `w00-s01` (owned by `w00-s04`).

## Human validation

Required where acceptance is functional/visual/operational. For `w00-s01`, includes GitHub
protection confirmation when operator authorization is insufficient to evidence settings.

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
