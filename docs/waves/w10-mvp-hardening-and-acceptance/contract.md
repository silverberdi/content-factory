# W10 — MVP Hardening and Acceptance Contract

## Objective

Prove all agreed MVP exit criteria through real end-to-end operation, security, recovery, documentation, and human acceptance.

## Prerequisite

w09 completed and all production dependencies configured.

The wave refuses to start when the prerequisite is not proven.

## Slices


### W10-S01 — Security and Governance Hardening

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `chg-w10-s01-security-and-governance-hardening`
- User Stories: `us-w10-s01-001`, `us-w10-s01-002`, `us-w10-s01-003`
- Branch: `slice/w10-s01-security-and-governance-hardening`

### W10-S02 — End-to-End Music Acceptance

- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `chg-w10-s02-end-to-end-music-acceptance`
- User Stories: `us-w10-s02-001`, `us-w10-s02-002`, `us-w10-s02-003`
- Branch: `slice/w10-s02-end-to-end-music-acceptance`

### W10-S03 — End-to-End Business Acceptance

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `chg-w10-s03-end-to-end-business-acceptance`
- User Stories: `us-w10-s03-001`, `us-w10-s03-002`, `us-w10-s03-003`
- Branch: `slice/w10-s03-end-to-end-business-acceptance`

### W10-S04 — MVP Completion and Handoff

- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `chg-w10-s04-mvp-completion-and-handoff`
- User Stories: `us-w10-s04-001`, `us-w10-s04-002`, `us-w10-s04-003`
- Branch: `slice/w10-s04-mvp-completion-and-handoff`


## Change map

| Slice | OpenSpec Change | Implementer | Reviewer |
|---|---|---|---|
| w10-s01 | chg-w10-s01-security-and-governance-hardening | CURSOR | CODEX |
| w10-s02 | chg-w10-s02-end-to-end-music-acceptance | CODEX | CURSOR |
| w10-s03 | chg-w10-s03-end-to-end-business-acceptance | CURSOR | CODEX |
| w10-s04 | chg-w10-s04-mvp-completion-and-handoff | CODEX | CURSOR |

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
