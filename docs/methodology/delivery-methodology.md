# Delivery Methodology

## Hierarchy

`Roadmap → Wave → Slice → User Stories → OpenSpec tasks`

- A wave delivers a major objective and cannot close partially.
- A slice is the complete implementation and integration unit assigned to one operator
  (`CURSOR` or `CODEX`).
- User Stories remain the functional backlog units inside the slice.
- Tasks are derived and maintained inside the OpenSpec change for that slice.
- One slice normally maps to one OpenSpec change.
- The wave contract and backlog identify exactly one expected OpenSpec change ID per slice;
  User Stories for that slice reference the same change ID.

## Sequence

No wave closes partially. No later wave starts before the previous wave is `COMPLETED`.
Future-wave scope is out of scope for the active change and must not appear in proposal, design,
specs, or tasks.

States: `PLANNED`, `READY`, `IN_PROGRESS`, `BLOCKED`, `VALIDATING`, `COMPLETED`.

## Operator model

One operator implements the whole slice end to end; the other performs mandatory cross-review with
verdict exactly `READY_TO_MERGE` or `CHANGES_REQUIRED`. Difficulty does not allow reassignment or
abandonment. A genuine external blocker pauses the same operator.

## OpenSpec closure

A slice must have:

- OpenSpec Verify exactly `PASS` (`PASS WITH NOTES` is not closure);
- automated checks `PASS`;
- acceptance criteria satisfied;
- review `READY_TO_MERGE`;
- documentation synchronized;
- context pack regenerated and validated;
- deployment, smoke tests, and Silverio `GO` when human validation applies;
- synchronized and archived OpenSpec change;
- no hidden deferred work.

Bootstrap presence of docs, rules, or scripts is candidate implementation only and never marks a
User Story or slice completed by itself.

## Branch and pull-request model

- `main` represents the last fully completed wave.
- `wave/*` branches integrate all slices of one wave.
- `slice/*` branches are created from the active wave branch.
- Slice pull requests target `wave/*` (never `main` for slice integration).
- Wave pull requests target `main`.
- Direct pushes to protected `wave/*` branches or `main` are invalid; recover via the PR model.
- Silverio manually merges completed waves into `main` after wave evidence is `READY_FOR_MAIN`.

## GitHub protection vs CI automation

Basic repository protection (reject direct pushes to `main`, reject force pushes and deletion on
`main`, require pull requests for `main`, and enforce the slice→`wave/*`→`main` PR targeting model)
is established and evidenced in `w00-s01`.

GitHub Actions checks, Nx validation, CI-driven merge gates, and fully automated slice auto-merge
are owned by `w00-s04` and are out of scope for `w00-s01`. Methodology may describe the intended
auto-merge eligibility model; `w00-s01` does not implement CI automation.

## Git merge preferences

- Slice → wave: prefer squash merge; may auto-merge after gates and `READY_TO_MERGE` once
  `w00-s04` automation exists.
- Wave → main: prefer merge commit; Silverio manually merges after `READY_FOR_MAIN`.
