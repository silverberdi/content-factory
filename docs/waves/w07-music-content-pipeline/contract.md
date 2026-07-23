# W07 — Music Content Pipeline Contract

## Objective

Deliver a real AI music cycle from idea through a roughly four-minute song, visual output, review, scheduling, YouTube publication, and manual distributor package.

## Prerequisite

w06 completed and providers, jobs, budgets, notifications, and assets operational.

The wave refuses to start when the prerequisite is not proven.

## Slices


### W07-S01 — Song Planning and Generation

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `chg-w07-s01-song-planning-and-generation`
- User Stories: `us-w07-s01-001`, `us-w07-s01-002`, `us-w07-s01-003`
- Branch: `slice/w07-s01-song-planning-and-generation`

### W07-S02 — Music Video Assembly

- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `chg-w07-s02-music-video-assembly`
- User Stories: `us-w07-s02-001`, `us-w07-s02-002`, `us-w07-s02-003`
- Branch: `slice/w07-s02-music-video-assembly`

### W07-S03 — Music Review and Scheduling

- Implementer: `CURSOR`
- Reviewer: `CODEX`
- Change: `chg-w07-s03-music-review-and-scheduling`
- User Stories: `us-w07-s03-001`, `us-w07-s03-002`, `us-w07-s03-003`
- Branch: `slice/w07-s03-music-review-and-scheduling`

### W07-S04 — YouTube and Distribution Package

- Implementer: `CODEX`
- Reviewer: `CURSOR`
- Change: `chg-w07-s04-youtube-and-distribution-package`
- User Stories: `us-w07-s04-001`, `us-w07-s04-002`, `us-w07-s04-003`, `us-w07-s04-004`
- Branch: `slice/w07-s04-youtube-and-distribution-package`


## Change map

| Slice | OpenSpec Change | Implementer | Reviewer |
|---|---|---|---|
| w07-s01 | chg-w07-s01-song-planning-and-generation | CURSOR | CODEX |
| w07-s02 | chg-w07-s02-music-video-assembly | CODEX | CURSOR |
| w07-s03 | chg-w07-s03-music-review-and-scheduling | CURSOR | CODEX |
| w07-s04 | chg-w07-s04-youtube-and-distribution-package | CODEX | CURSOR |

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
