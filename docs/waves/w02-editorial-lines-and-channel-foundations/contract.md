# W02 — Editorial Lines and Channel Foundations Contract

## Objective

Enable administrators to create editorial lines and exclusive external-account channel records with language, audience, brand, and lifecycle foundations.

## Prerequisite

w01 completed and role enforcement verified.

The wave refuses to start when the prerequisite is not proven.

## Slices


### W02-S01 — Editorial Line Management

- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `chg-w02-s01-editorial-line-management`
- User Stories: `us-w02-s01-001`, `us-w02-s01-002`, `us-w02-s01-003`
- Branch: `slice/w02-s01-editorial-line-management`

### W02-S02 — Channel Domain and Lifecycle

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `chg-w02-s02-channel-domain-and-lifecycle`
- User Stories: `us-w02-s02-001`, `us-w02-s02-002`, `us-w02-s02-003`, `us-w02-s02-004`
- Branch: `slice/w02-s02-channel-domain-and-lifecycle`

### W02-S03 — Channel Brand Identity

- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `chg-w02-s03-channel-brand-identity`
- User Stories: `us-w02-s03-001`, `us-w02-s03-002`, `us-w02-s03-003`
- Branch: `slice/w02-s03-channel-brand-identity`

### W02-S04 — Multilingual Editorial Families

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `chg-w02-s04-multilingual-editorial-families`
- User Stories: `us-w02-s04-001`, `us-w02-s04-002`, `us-w02-s04-003`
- Branch: `slice/w02-s04-multilingual-editorial-families`


## Change map

| Slice | OpenSpec Change | Implementer | Reviewer |
|---|---|---|---|
| w02-s01 | chg-w02-s01-editorial-line-management | CODEX | CURSOR |
| w02-s02 | chg-w02-s02-channel-domain-and-lifecycle | CURSOR | CODEX |
| w02-s03 | chg-w02-s03-channel-brand-identity | CODEX | CURSOR |
| w02-s04 | chg-w02-s04-multilingual-editorial-families | CURSOR | CODEX |

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
