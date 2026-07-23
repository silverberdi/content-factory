# W00 — Project Foundation User Stories


## us-w00-s01-001 — Govern the repository through waves, slices, pull requests, and protected branches

**Slice:** `W00-S01 — Repository Governance and OpenSpec Foundation`  
**Primary executor:** `CURSOR`  
**OpenSpec change:** `chg-w00-s01-repository-governance-and-openspec-foundation`

As an authorized Content Factory user, I want to govern the repository through waves, slices, pull requests, and protected branches, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Delivery hierarchy `Roadmap → Wave → Slice → User Stories → OpenSpec tasks` is documented and
   consistent across methodology, roadmap, backlog, and the w00 wave contract.
2. Branch/PR model documents `main` / `wave/*` / `slice/*`, slice PRs targeting `wave/*`, wave PRs
   targeting `main`, and Silverio manual wave→`main` merge.
3. Basic GitHub protection for `main` (reject direct pushes, reject force pushes/deletion, require
   PRs) and the slice→`wave/*`→`main` PR model are configured or verified with recorded evidence
   (or Silverio confirmation when authorization is required).
4. Complete-slice ownership and mandatory cross-review (`READY_TO_MERGE` /
   `CHANGES_REQUIRED`) are explicit in wave contract and current-state.
5. GitHub Actions checks, Nx validation, CI-driven merge gates, and fully automated slice
   auto-merge remain assigned to `w00-s04` and are excluded from this User Story.
6. Canonical documentation and context are synchronized; OpenSpec Verify returns exactly `PASS`.


## us-w00-s01-002 — Initialize OpenSpec with the expanded verified workflow

**Slice:** `W00-S01 — Repository Governance and OpenSpec Foundation`  
**Primary executor:** `CURSOR`  
**OpenSpec change:** `chg-w00-s01-repository-governance-and-openspec-foundation`

As an authorized Content Factory user, I want to initialize OpenSpec with the expanded verified workflow, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. OpenSpec `1.6.0` config and generated Cursor/Codex integrations are present, compatible, and
   cover the expanded verified workflow (propose → apply → verify → sync → archive).
2. Generated integrations under `.cursor/commands/`, `.cursor/skills/`, and `.codex/skills/` are
   validated only; regeneration uses official `openspec update` (no manual edits).
3. Methodology and deviation policy require Verify exactly `PASS` and synchronized US/backlog/
   wave/OpenSpec updates on deviations (`PASS WITH NOTES` is not closure).
4. This change remains bound to `w00` / `w00-s01` / `us-w00-s01-001`–`004` / `CURSOR` / `CODEX`
   with no S02–S04 or future-wave tasks.
5. A lightweight workflow contract check asserts propose→apply→verify-PASS→sync→archive
   expectations for `w00-s01`.
6. Canonical documentation and context are synchronized; OpenSpec Verify returns exactly `PASS`.


## us-w00-s01-003 — Provide canonical operating rules for Cursor and Codex

**Slice:** `W00-S01 — Repository Governance and OpenSpec Foundation`  
**Primary executor:** `CURSOR`  
**OpenSpec change:** `chg-w00-s01-repository-governance-and-openspec-foundation`

As an authorized Content Factory user, I want to provide canonical operating rules for Cursor and Codex, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. `AGENTS.md` states hierarchy, branch model, Verify `PASS`, deviation procedure, safety, and
   evidence standards as the shared Cursor/Codex contract.
2. `.cursor/rules/00-project-governance.mdc` and `.cursor/rules/30-delivery-evidence.mdc` require
   the context integrity check and forbid incomplete closure / future-wave scope.
3. Project-specific policy lives in `AGENTS.md`, `.cursor/rules/`, `openspec/config.yaml`, and
   canonical docs — not in hand-edited generated OpenSpec skill/command trees.
4. Safety clauses forbid secrets in Git/logs, destructive volume/DB resets, and delegating file
   edits to Silverio when agents can edit.
5. A contract check confirms required project-owned agent rule files exist and contain mandatory
   governance keywords/gates.
6. Canonical documentation and context are synchronized; OpenSpec Verify returns exactly `PASS`.


## us-w00-s01-004 — Generate and validate the current OpenSpec context pack

**Slice:** `W00-S01 — Repository Governance and OpenSpec Foundation`  
**Primary executor:** `CURSOR`  
**OpenSpec change:** `chg-w00-s01-repository-governance-and-openspec-foundation`

As an authorized Content Factory user, I want to generate and validate the current OpenSpec context pack, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. `scripts/context/generate-context-pack.mjs` emits pack + manifest from global sources plus
   active-wave sources only (no all-future-wave injection).
2. `scripts/context/check-context-pack.mjs` fails on stale sources, pack drift, or source-list
   mismatch.
3. Context docs document regenerate/check paths and regeneration cadence at each completed slice
   and wave.
4. Automated tests cover generate/check success and at least one stale-pack failure path.
5. Regenerated `docs/context/generated/current-context-pack.md` passes
   `node scripts/context/check-context-pack.mjs`.
6. Canonical documentation and context are synchronized; OpenSpec Verify returns exactly `PASS`.


## us-w00-s02-001 — Create the Nx monorepo and enforce module boundaries

**Slice:** `W00-S02 — Nx Application Foundation`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `chg-w00-s02-nx-application-foundation`

As an authorized Content Factory user, I want to create the Nx monorepo and enforce module boundaries, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## us-w00-s02-002 — Create the Angular 22 PWA console foundation

**Slice:** `W00-S02 — Nx Application Foundation`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `chg-w00-s02-nx-application-foundation`

As an authorized Content Factory user, I want to create the Angular 22 PWA console foundation, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## us-w00-s02-003 — Integrate PrimeNG, PrimeIcons, responsive layout, and accessible design tokens

**Slice:** `W00-S02 — Nx Application Foundation`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `chg-w00-s02-nx-application-foundation`

As an authorized Content Factory user, I want to integrate PrimeNG, PrimeIcons, responsive layout, and accessible design tokens, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## us-w00-s02-004 — Support light, dark, and system appearance modes

**Slice:** `W00-S02 — Nx Application Foundation`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `chg-w00-s02-nx-application-foundation`

As an authorized Content Factory user, I want to support light, dark, and system appearance modes, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## us-w00-s02-005 — Create NestJS Fastify API and FFmpeg media-worker foundations

**Slice:** `W00-S02 — Nx Application Foundation`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `chg-w00-s02-nx-application-foundation`

As an authorized Content Factory user, I want to create NestJS Fastify API and FFmpeg media-worker foundations, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## us-w00-s03-001 — Define Docker Compose services without host runtime dependencies

**Slice:** `W00-S03 — Container and Data Foundation`  
**Primary executor:** `CURSOR`  
**OpenSpec change:** `chg-w00-s03-container-and-data-foundation`

As an authorized Content Factory user, I want to define Docker Compose services without host runtime dependencies, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## us-w00-s03-002 — Configure the dedicated PostgreSQL database boundary

**Slice:** `W00-S03 — Container and Data Foundation`  
**Primary executor:** `CURSOR`  
**OpenSpec change:** `chg-w00-s03-container-and-data-foundation`

As an authorized Content Factory user, I want to configure the dedicated PostgreSQL database boundary, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## us-w00-s03-003 — Configure the dedicated MinIO bucket and credential boundary

**Slice:** `W00-S03 — Container and Data Foundation`  
**Primary executor:** `CURSOR`  
**OpenSpec change:** `chg-w00-s03-container-and-data-foundation`

As an authorized Content Factory user, I want to configure the dedicated MinIO bucket and credential boundary, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## us-w00-s03-004 — Provide environment templates, health checks, and non-destructive deployment scripts

**Slice:** `W00-S03 — Container and Data Foundation`  
**Primary executor:** `CURSOR`  
**OpenSpec change:** `chg-w00-s03-container-and-data-foundation`

As an authorized Content Factory user, I want to provide environment templates, health checks, and non-destructive deployment scripts, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## us-w00-s04-001 — Run required GitHub Actions checks on pull requests

**Slice:** `W00-S04 — Continuous Integration and Quality Gates`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `chg-w00-s04-continuous-integration-and-quality-gates`

As an authorized Content Factory user, I want to run required GitHub Actions checks on pull requests, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## us-w00-s04-002 — Validate OpenSpec, documentation, context, tests, builds, workflows, and secrets

**Slice:** `W00-S04 — Continuous Integration and Quality Gates`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `chg-w00-s04-continuous-integration-and-quality-gates`

As an authorized Content Factory user, I want to validate OpenSpec, documentation, context, tests, builds, workflows, and secrets, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## us-w00-s04-003 — Support cross-review evidence and slice auto-merge eligibility

**Slice:** `W00-S04 — Continuous Integration and Quality Gates`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `chg-w00-s04-continuous-integration-and-quality-gates`

As an authorized Content Factory user, I want to support cross-review evidence and slice auto-merge eligibility, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.


## us-w00-s04-004 — Generate formal wave completion evidence before main integration

**Slice:** `W00-S04 — Continuous Integration and Quality Gates`  
**Primary executor:** `CODEX`  
**OpenSpec change:** `chg-w00-s04-continuous-integration-and-quality-gates`

As an authorized Content Factory user, I want to generate formal wave completion evidence before main integration, so that the
active slice delivers its agreed business value safely and consistently.

### Acceptance criteria

1. Behavior is limited to the declared slice scope.
2. Authorization and validation are enforced where applicable.
3. Tests cover relevant success and failure paths.
4. Canonical documentation and context are synchronized.
5. OpenSpec Verify returns exactly `PASS`.
6. Deployment and Silverio GO are completed when the wave contract requires human validation.
