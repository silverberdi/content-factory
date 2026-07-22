# W10 — MVP Hardening and Acceptance Contract

## Objective

Prove all agreed MVP exit criteria through real end-to-end operation, security, recovery, documentation, and human acceptance.

## Prerequisite

W09 completed and all production dependencies configured.

The wave refuses to start when the prerequisite is not proven.

## Slices


### W10-S01 — Security and Governance Hardening

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `CHG-W10-S01-security-and-governance-hardening`
- User Stories: `US-W10-S01-001`, `US-W10-S01-002`, `US-W10-S01-003`
- Branch: `slice/w10-s01-security-and-governance-hardening`

### W10-S02 — End-to-End Music Acceptance

- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `CHG-W10-S02-end-to-end-music-acceptance`
- User Stories: `US-W10-S02-001`, `US-W10-S02-002`, `US-W10-S02-003`
- Branch: `slice/w10-s02-end-to-end-music-acceptance`

### W10-S03 — End-to-End Business Acceptance

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `CHG-W10-S03-end-to-end-business-acceptance`
- User Stories: `US-W10-S03-001`, `US-W10-S03-002`, `US-W10-S03-003`
- Branch: `slice/w10-s03-end-to-end-business-acceptance`

### W10-S04 — MVP Completion and Handoff

- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `CHG-W10-S04-mvp-completion-and-handoff`
- User Stories: `US-W10-S04-001`, `US-W10-S04-002`, `US-W10-S04-003`
- Branch: `slice/w10-s04-mvp-completion-and-handoff`


## Change map

| Slice | OpenSpec Change | Implementer | Reviewer |
|---|---|---|---|
| W10-S01 | CHG-W10-S01-security-and-governance-hardening | CURSOR | CODEX |
| W10-S02 | CHG-W10-S02-end-to-end-music-acceptance | CODEX | CURSOR |
| W10-S03 | CHG-W10-S03-end-to-end-business-acceptance | CURSOR | CODEX |
| W10-S04 | CHG-W10-S04-mvp-completion-and-handoff | CODEX | CURSOR |

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
