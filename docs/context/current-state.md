# Current State

## Lifecycle

- Repository lifecycle: `PRE_COMMIT_BOOTSTRAP_RECONCILED`
- Active wave state: `READY`
- Active slice state: `READY`
- No User Story is `COMPLETED`
- No slice is `COMPLETED`
- No wave is `COMPLETED`

## Active scope

| Field | Value |
|---|---|
| Active wave | `W00 — Project Foundation` |
| Active wave ID | `W00` |
| Active wave directory | `docs/waves/w00-project-foundation` |
| Active slice | `W00-S01 — Repository Governance and OpenSpec Foundation` |
| Active slice ID | `W00-S01` |
| Assigned implementer | `CURSOR` |
| Cross-reviewer | `CODEX` |
| Expected OpenSpec change | `CHG-W00-S01-repository-governance-and-openspec-foundation` |
| Expected wave branch | `wave/w00-project-foundation` |
| Expected slice branch | `slice/w00-s01-repository-governance-and-openspec-foundation` |

## Bootstrap facts

- Public repository is initialized and is not empty.
- Canonical project definition is imported.
- OpenSpec `1.6.0` is installed.
- Cursor and Codex OpenSpec integrations are generated.
- Present governance docs, rules, scripts, and indexes are pre-existing candidate implementation.
- Those candidates must be adopted, reviewed, corrected, verified, synchronized, and archived
  through `CHG-W00-S01-repository-governance-and-openspec-foundation`.

## Human validation

Required for functional, visual, or operational acceptance when the active wave or slice contract
says so. For `W00-S01`, human validation applies where governance/process acceptance cannot be
proven by automation alone; deploy/smoke/Silverio `GO` apply when the slice contract requires them.

## Explicit next action

1. Create wave branch `wave/w00-project-foundation` when formal W00 execution starts.
2. Create slice branch `slice/w00-s01-repository-governance-and-openspec-foundation` from the wave
   branch.
3. Create OpenSpec change `CHG-W00-S01-repository-governance-and-openspec-foundation`.
4. Adopt and correct the pre-existing bootstrap artifacts against US-W00-S01-001 through
   US-W00-S01-004 without marking any US completed until Verify is exactly `PASS` and the full
   slice contract is satisfied.
