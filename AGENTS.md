# Content Factory Agent Contract

Applies to Codex and all coding agents operating in this repository, including Cursor.

## Hierarchy

`Roadmap → Wave → Slice → User Stories → OpenSpec tasks`

- A wave delivers one major objective and cannot close partially.
- A slice is the complete implementation and integration unit assigned to one operator.
- User Stories remain the functional backlog units inside the slice.
- Tasks are derived and maintained inside the OpenSpec change for that slice.
- One slice normally maps to one OpenSpec change.

## Start of every implementation or review

1. Run the context integrity check:
   `node scripts/context/check-context-pack.mjs`
2. Read `docs/context/generated/current-context-pack.md`.
3. Read the active wave contract, active slice assignment, included User Stories, and
   `AGENTS.md`.
4. Use the assigned OpenSpec change as the implementation contract.

If the context check fails, regenerate with `node scripts/context/generate-context-pack.mjs`,
re-check, and only then proceed.

## Complete-slice ownership

- Own one complete assigned slice end to end.
- Never divide a User Story or slice across implementers.
- Never abandon a slice because it is difficult.
- Difficulty is not a reason to reassign work.
- Stop only for a real external blocker; pause under the same operator.
- Never implement future-wave scope.
- Never hide deferred acceptance criteria, notes, or informal debt to force closure.

## Mandatory cross-review

- The non-implementing operator performs mandatory cross-review.
- Cross-review verdict is exactly `READY_TO_MERGE` or `CHANGES_REQUIRED`.
- Resolve all `CHANGES_REQUIRED` findings before merge eligibility.

## Branch and PR model

- `main` represents the last fully completed wave.
- `wave/*` branches integrate all slices of one wave.
- `slice/*` branches are created from the active wave branch.
- Slice pull requests target `wave/*`.
- Wave pull requests target `main`.
- Never push directly to protected wave branches or `main`.

## Slice auto-merge rules

- Slice → wave may auto-merge after required checks pass and cross-review is `READY_TO_MERGE`.
- Prefer squash merge for slice PRs into the wave branch.

## Wave manual merge rule

- Wave → `main` requires Silverio's manual merge after wave completion evidence is
  `READY_FOR_MAIN`.
- Prefer merge commit for wave PRs into `main`.

## Deviation synchronization procedure

When an unplanned requirement, dependency, decision, or blocker appears:

1. Stop affected execution.
2. Analyze impact.
3. Create or update the User Story.
4. Synchronize roadmap, backlog, User Stories, and affected wave contracts.
5. Create or update the OpenSpec change.
6. Validate consistency.
7. Resume with the same operator.

Do not hide a deviation as a note or debt.

## OpenSpec Verify

- Verify must be exactly `PASS`.
- `PASS WITH NOTES` is forbidden and is not closure.

## Deployment, smoke tests, and Silverio GO

When the wave or slice contract requires human validation:

1. Deploy safely and non-destructively.
2. Run health checks and smoke tests.
3. Provide clear test instructions for Silverio.
4. Obtain explicit Silverio `GO` before closure.

## Sync and archive

Synchronize docs and archive the OpenSpec change only after the whole slice contract is satisfied:

- acceptance criteria satisfied;
- automated checks `PASS`;
- OpenSpec Verify exactly `PASS`;
- cross-review `READY_TO_MERGE`;
- documentation synchronized;
- context pack regenerated and validated;
- deployment, smoke tests, and Silverio `GO` when applicable;
- no blockers or hidden deferred work.

## Mandatory context regeneration

Regenerate and validate the current context pack at every completed slice and every completed wave:

- `node scripts/context/generate-context-pack.mjs`
- `node scripts/context/check-context-pack.mjs`

Canonical generated path: `docs/context/generated/current-context-pack.md`.

## Evidence and completion report

Record wave, slice, User Stories, implementer, reviewer, change ID, Verify `PASS`, automated
checks, acceptance evidence, deployment/smoke evidence, Silverio `GO` when applicable,
documentation/context hashes, PR result, and confirmation of zero hidden scope.

Wave completion reports use exactly one final state: `READY_FOR_MAIN`, `BLOCKED`, or `INCOMPLETE`.

## Safety and repository integrity

- Never expose or commit secrets.
- Preserve `.env`, volumes, PostgreSQL data, MinIO objects, and n8n configuration.
- Never use destructive reset, volume deletion, or unsafe schema commands.
- Never delegate repository file edits to Silverio when the agent can perform them.
