# W01 — Identity, Access, and Session Security Contract

## Objective

Allow only approved Google identities to access the console with role-aware capabilities and secure browser/PWA session behavior.

## Prerequisite

w00 completed and foundations operational.

The wave refuses to start when the prerequisite is not proven.

## Slices


### W01-S01 — Google OIDC and Allowlist

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `chg-w01-s01-google-oidc-and-allowlist`
- User Stories: `us-w01-s01-001`, `us-w01-s01-002`, `us-w01-s01-003`
- Branch: `slice/w01-s01-google-oidc-and-allowlist`

### W01-S02 — Secure Session Lifecycle

- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `chg-w01-s02-secure-session-lifecycle`
- User Stories: `us-w01-s02-001`, `us-w01-s02-002`, `us-w01-s02-003`
- Branch: `slice/w01-s02-secure-session-lifecycle`

### W01-S03 — Session Administration and Authorization

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `chg-w01-s03-session-administration-and-authorization`
- User Stories: `us-w01-s03-001`, `us-w01-s03-002`, `us-w01-s03-003`
- Branch: `slice/w01-s03-session-administration-and-authorization`


## Change map

| Slice | OpenSpec Change | Implementer | Reviewer |
|---|---|---|---|
| w01-s01 | chg-w01-s01-google-oidc-and-allowlist | CURSOR | CODEX |
| w01-s02 | chg-w01-s02-secure-session-lifecycle | CODEX | CURSOR |
| w01-s03 | chg-w01-s03-session-administration-and-authorization | CURSOR | CODEX |

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
