# W08 — Business Short-Video Pipeline Contract

## Objective

Deliver evidence-aware, platform-adapted vertical business content from topic to real multi-platform publication.

## Prerequisite

W07 completed and the first real pipeline proven.

The wave refuses to start when the prerequisite is not proven.

## Slices


### W08-S01 — Topic, Research, and Script

- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `CHG-W08-S01-topic-research-and-script`
- User Stories: `US-W08-S01-001`, `US-W08-S01-002`, `US-W08-S01-003`, `US-W08-S01-004`
- Branch: `slice/w08-s01-topic-research-and-script`

### W08-S02 — Voice, Visuals, and Assembly

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `CHG-W08-S02-voice-visuals-and-assembly`
- User Stories: `US-W08-S02-001`, `US-W08-S02-002`, `US-W08-S02-003`
- Branch: `slice/w08-s02-voice-visuals-and-assembly`

### W08-S03 — Platform Adaptation

- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `CHG-W08-S03-platform-adaptation`
- User Stories: `US-W08-S03-001`, `US-W08-S03-002`, `US-W08-S03-003`
- Branch: `slice/w08-s03-platform-adaptation`

### W08-S04 — Real Multi-platform Publication

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `CHG-W08-S04-real-multi-platform-publication`
- User Stories: `US-W08-S04-001`, `US-W08-S04-002`, `US-W08-S04-003`, `US-W08-S04-004`
- Branch: `slice/w08-s04-real-multi-platform-publication`


## Change map

| Slice | OpenSpec Change | Implementer | Reviewer |
|---|---|---|---|
| W08-S01 | CHG-W08-S01-topic-research-and-script | CODEX | CURSOR |
| W08-S02 | CHG-W08-S02-voice-visuals-and-assembly | CURSOR | CODEX |
| W08-S03 | CHG-W08-S03-platform-adaptation | CODEX | CURSOR |
| W08-S04 | CHG-W08-S04-real-multi-platform-publication | CURSOR | CODEX |

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
