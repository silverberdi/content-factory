# Delivery Methodology

## Hierarchy

`Roadmap → Wave → Slice → User Stories → OpenSpec tasks`

- A wave delivers a major objective.
- A slice is the complete unit assigned to Cursor or Codex.
- User Stories remain the functional backlog units.
- One slice normally maps to one OpenSpec change.

## Sequence

No wave closes partially. No later wave starts before the previous wave is `COMPLETED`.

States: `PLANNED`, `READY`, `IN_PROGRESS`, `BLOCKED`, `VALIDATING`, `COMPLETED`.

## Operator model

One operator implements the whole slice; the other performs cross-review. Difficulty does not allow
reassignment. A genuine external blocker pauses the same operator.

## OpenSpec closure

A slice must have:

- OpenSpec Verify exactly `PASS`;
- automated checks `PASS`;
- acceptance criteria satisfied;
- review `READY_TO_MERGE`;
- documentation synchronized;
- context pack regenerated and validated;
- deployment, smoke tests, and Silverio `GO` when human validation applies;
- synchronized and archived OpenSpec change;
- no hidden deferred work.

## Git

- `slice/*` PRs target `wave/*` and may auto-merge after gates.
- `wave/*` PRs target `main`.
- Silverio manually merges waves into `main`.
