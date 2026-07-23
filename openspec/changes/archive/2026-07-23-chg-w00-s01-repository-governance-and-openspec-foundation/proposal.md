## Why

Wave `w00` / slice `w00-s01` must establish enforceable repository governance and an OpenSpec-first operating foundation before any application, container, or CI slice proceeds. Bootstrap docs, agent rules, and context scripts already exist as candidate artifacts; they are not completed delivery until this change adopts, corrects, verifies, and archives them under User Stories `us-w00-s01-001` through `us-w00-s01-004`.

## What Changes

- Formalize wave → slice → User Story → OpenSpec-task governance with protected `main` / `wave/*` branches and slice PRs targeting the active wave branch.
- Establish and evidence actual basic GitHub repository protection for `main` and the slice→`wave/*`→`main` PR model; leave GitHub Actions checks, Nx validation, CI merge gates, and automated slice auto-merge to `w00-s04`.
- Adopt and correct the OpenSpec `1.6.0` expanded verified workflow (propose → apply → verify exactly `PASS` → sync → archive) as the mandatory change lifecycle.
- Adopt and correct project-specific Cursor/Codex operating rules in `AGENTS.md`, `.cursor/rules/`, `openspec/config.yaml`, and canonical docs. OpenSpec-generated integrations under `.cursor/commands/`, `.cursor/skills/`, and `.codex/skills/` are immutable; this change may only validate presence/compatibility/workflow coverage and regenerate them via official `openspec update`.
- Adopt and correct context-pack generation and integrity validation (`scripts/context/*`, generated pack path and manifest) as a required gate before implementation or review.
- Synchronize canonical methodology, roadmap, backlog, wave contract, and current-state docs so they match the verified governance model.
- Record closure evidence for `w00-s01` with unambiguous merge-eligibility ordering: a draft slice PR may open early for visibility, but merge eligibility requires automated checks `PASS`, Verify exactly `PASS`, required human validation, synced specs, archived change, regenerated context pack with integrity `PASS`, and final CODEX `READY_TO_MERGE`.

## Capabilities

### New Capabilities

- `repository-governance`: Branch protection, wave/slice PR targeting, operator assignment, and delivery hierarchy enforcement for Content Factory.
- `openspec-workflow`: Expanded verified OpenSpec lifecycle, including Verify exactly `PASS`, sync, archive, and deviation handling.
- `agent-operating-rules`: Shared Cursor/Codex operating contract for complete-slice ownership, mandatory cross-review, safety, and evidence standards.
- `context-pack`: Generation, integrity check, regeneration cadence, and active-scope scoping of the current OpenSpec context pack.

### Modified Capabilities

- (none — `openspec/specs/` has no baseline capabilities yet)

## Impact

- **Wave / slice:** `w00` — Project Foundation / `w00-s01` — Repository Governance and OpenSpec Foundation.
- **User Stories:** `us-w00-s01-001`, `us-w00-s01-002`, `us-w00-s01-003`, `us-w00-s01-004`.
- **Implementer / reviewer:** `CURSOR` implements; `CODEX` performs mandatory cross-review (`READY_TO_MERGE` or `CHANGES_REQUIRED`).
- **Branches:** Expected wave branch `wave/w00-project-foundation`; slice branch `slice/w00-s01-repository-governance-and-openspec-foundation`; slice PR targets the wave branch (never direct push to protected wave branches or `main`).
- **Affected artifacts:** `AGENTS.md`, `.cursor/rules/`, `openspec/config.yaml`, `docs/methodology/*`, `docs/governance/*`, `docs/context/*`, `docs/waves/w00-project-foundation/*`, `docs/backlog/*`, `docs/roadmap/*`, `scripts/context/*`, bootstrap notes. Generated OpenSpec command/skill trees are read-only except via `openspec update`.
- **Business value:** A single governed operating system so later w00 slices (Nx, containers, CI) and all future waves can execute without ambiguous process, incomplete context, or unverified closure.
- **Prerequisites (satisfied):** Public repo initialized; canonical project definition imported; OpenSpec `1.6.0` installed; Cursor/Codex OpenSpec integrations generated.
- **Dependencies:** None on later w00 slices; this slice is the first ordered slice in the w00 execution plan.
- **Explicit exclusions:** No Nx/Angular/Nest/Prisma/Docker implementation (`w00-s02`–`w00-s03`); no GitHub Actions checks, Nx CI validation, CI-driven merge gates, or fully automated slice auto-merge (`w00-s04`); no future-wave scope; no marking US/slice completed by bootstrap presence alone; no manual edits to generated OpenSpec skills/commands; no pretending GitHub protection exists without recorded evidence or Silverio confirmation when authorization is required.
- **Human validation:** Required where governance/process acceptance cannot be proven by automation alone (including GitHub protection settings that need Silverio authorization). Deploy/smoke/Silverio `GO` apply only if the slice contract requires runtime validation for a given acceptance item.
- **Risks / blockers:** Drift between candidate bootstrap files and acceptance criteria; incomplete context-pack integrity; Verify weaker than exact `PASS`; GitHub settings that require Silverio authorization before protection can be evidenced. No other known external blockers.
- **Rollback / recovery:** Governance is documentation, rules, scripts, GitHub settings, and process contracts—no runtime data or application deployment surface in this slice. Recovery is revert the slice PR on the wave branch, restore previous docs/rules/scripts via Git, reverse any GitHub setting changes with recorded evidence, and re-run context integrity checks; do not use destructive resets.
