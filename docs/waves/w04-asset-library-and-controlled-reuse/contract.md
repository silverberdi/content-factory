# W04 — Asset Library and Controlled Reuse Contract

## Objective

Provide a secure platform-wide asset library with origin, rights, usage policy, validation, and MinIO-backed uploads.

## Prerequisite

W03 completed and channel context available.

The wave refuses to start when the prerequisite is not proven.

## Slices


### W04-S01 — Asset Upload and Validation

- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `CHG-W04-S01-asset-upload-and-validation`
- User Stories: `US-W04-S01-001`, `US-W04-S01-002`, `US-W04-S01-003`, `US-W04-S01-004`
- Branch: `slice/w04-s01-asset-upload-and-validation`

### W04-S02 — Asset Catalog and Provenance

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `CHG-W04-S02-asset-catalog-and-provenance`
- User Stories: `US-W04-S02-001`, `US-W04-S02-002`, `US-W04-S02-003`
- Branch: `slice/w04-s02-asset-catalog-and-provenance`

### W04-S03 — AI Usage and Reuse Policy

- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `CHG-W04-S03-ai-usage-and-reuse-policy`
- User Stories: `US-W04-S03-001`, `US-W04-S03-002`, `US-W04-S03-003`, `US-W04-S03-004`
- Branch: `slice/w04-s03-ai-usage-and-reuse-policy`


## Change map

| Slice | OpenSpec Change | Implementer | Reviewer |
|---|---|---|---|
| W04-S01 | CHG-W04-S01-asset-upload-and-validation | CODEX | CURSOR |
| W04-S02 | CHG-W04-S02-asset-catalog-and-provenance | CURSOR | CODEX |
| W04-S03 | CHG-W04-S03-ai-usage-and-reuse-policy | CODEX | CURSOR |

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
