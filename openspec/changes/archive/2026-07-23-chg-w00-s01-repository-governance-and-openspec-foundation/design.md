## Context

Slice `w00-s01` establishes the governed operating system for Content Factory before Nx, containers, or CI slices begin. The repository already contains bootstrap candidates: methodology docs, wave contracts, backlog/User Stories, `AGENTS.md`, Cursor rules, OpenSpec `1.6.0` integrations, and context-pack scripts. Lifecycle state is `PRE_COMMIT_BOOTSTRAP_RECONCILED`; no User Story is `COMPLETED`.

Stakeholders: implementer `CURSOR`, cross-reviewer `CODEX`, owner Silverio for wave→`main` merges, GitHub-setting authorization when required, and any required human `GO`.

Constraints: adopt/correct candidates rather than rewrite from scratch; refuse future-wave and later-w00-slice scope; Verify must be exactly `PASS`; no secrets or destructive recovery; no runtime application stack in this slice; never manually edit OpenSpec-generated command/skill trees.

## Goals / Non-Goals

**Goals:**

- Make wave/slice/PR/branch governance explicit, consistent, and enforceable in canonical docs and agent rules.
- Establish and evidence actual basic GitHub repository protection for `main` and the slice→`wave/*`→`main` PR model.
- Lock the OpenSpec expanded verified lifecycle (propose → apply → verify `PASS` → sync → archive) as mandatory.
- Provide one shared Cursor/Codex operating contract in project-owned files (`AGENTS.md`, `.cursor/rules/`, `openspec/config.yaml`, canonical docs) with evidence and safety rules.
- Validate OpenSpec-generated integrations for presence, `1.6.0` compatibility, and workflow coverage; regenerate only through official `openspec update` when needed.
- Provide generate/check context-pack automation scoped to active wave sources.
- Produce slice evidence that candidates were adopted, corrected where needed, verified, synchronized, and archived, with unambiguous merge-eligibility ordering.

**Non-Goals:**

- Nx monorepo, Angular/PrimeNG console, NestJS API, Prisma schema, or Docker Compose (`w00-s02`–`w00-s03`).
- GitHub Actions checks, Nx CI validation, CI-driven merge gates, or fully automated slice auto-merge (`w00-s04`).
- Manual edits under `.cursor/commands/`, `.cursor/skills/`, or `.codex/skills/`.
- Identity, editorial lines, pipelines, or any w01+ product behavior.
- Runtime deploy of application services unless a specific governance acceptance item explicitly requires human validation evidence.
- Replacing OpenSpec CLI or inventing a parallel change-management system.

## Decisions

### D1 — Adopt bootstrap candidates; treat generated OpenSpec integrations as immutable
**Choice:** Review and correct existing governance docs, project rules, scripts, and `openspec/config.yaml`. Validate that generated Cursor/Codex OpenSpec commands and skills are present and compatible; if regeneration is required, use only `openspec update`. Never hand-edit generated skill/command trees.  
**Rationale:** Project-specific policy belongs in owned files; generated integrations drift if edited manually and are owned by the OpenSpec CLI.  
**Alternatives considered:** (a) Hand-align Codex/Cursor skill markdown — rejected (immutable generated artifacts). (b) Mark candidates completed without Verify — rejected (violates closure).

### D2 — Four capability specs aligned to four User Stories
**Choice:** Specs `repository-governance`, `openspec-workflow`, `agent-operating-rules`, `context-pack`.  
**Rationale:** One-to-one mapping keeps Verify and tasks traceable to `us-w00-s01-001`–`004`.  
**Alternatives considered:** Single mega-capability — rejected (weak Verify granularity). Merge rules+workflow — rejected (different acceptance surfaces).

### D3 — Context pack is generated + hashed, never hand-authored
**Choice:** Keep `scripts/context/generate-context-pack.mjs` and `check-context-pack.mjs` as the only writers/validators of `docs/context/generated/*`, sourcing global docs plus active-wave contract/plan/US catalog from `current-state.md`.  
**Rationale:** Prevents silent drift and enforces active-scope packing.  
**Alternatives considered:** Fully static pack — rejected (stale). Include all waves/US catalogs — rejected (context bloat / future-wave leakage).

### D4 — Basic GitHub protection in w00-s01; CI automation in w00-s04
**Choice:** `w00-s01` must configure or verify, and record evidence for, basic repository protection:

- `main` rejects direct pushes;
- `main` rejects force pushes and deletion;
- pull requests are required for `main`;
- `wave/*` follows the slice-to-wave pull-request model;
- slice PRs target `wave/*`;
- wave PRs target `main`;
- Silverio manually merges completed waves to `main`.

Document the model in methodology, `AGENTS.md`, and `docs/governance/github-governance.md`. If settings require Silverio authorization, prepare exact CLI/UI verification steps and record Silverio confirmation—never pretend protection exists. GitHub Actions checks, Nx validation, CI-driven merge gates, and fully automated slice auto-merge remain `w00-s04`.  
**Rationale:** Protection without evidence is false closure; CI automation is a later slice.  
**Alternatives considered:** Document-only policy without GitHub evidence — rejected (insufficient for us-w00-s01-001). Implement Actions/CI gates in S01 — rejected (S04 scope).

### D5 — No application runtime components in this slice
**Choice:** No Angular, NestJS, Prisma, MinIO, n8n, worker, or Docker changes.  
**Rationale:** Module boundaries for the app stack do not exist yet; introducing them here would violate slice completeness and pollute S02/S03.  
**Security/ops note:** Secret-handling and non-destructive recovery rules are documented in agent contracts only; no session/auth code lands here.

### D6 — Testing and evidence strategy
**Choice:**

- Automated: context generate/check scripts; machine-ID validation; OpenSpec Validate/Verify for this change; doc consistency checks against wave contract and US IDs; presence/compatibility checks for generated OpenSpec integrations.
- Manual/human: Silverio confirmation for GitHub protection settings when operator authorization is insufficient; other process acceptance where automation cannot prove governance behavior; deploy/smoke/`GO` only if a contract item truly requires runtime proof.
- Evidence pack: wave/slice/US IDs, implementer/reviewer, change ID, Verify `PASS`, check outputs, GitHub protection evidence (or Silverio confirmation), doc/context hashes, draft vs merge-eligible PR state, zero hidden scope confirmation.

### D7 — Closure and merge-eligibility ordering
**Choice:** A draft slice PR may open before final archival for review visibility. The PR MUST NOT become merge-eligible until, in order:

1. automated checks `PASS`;
2. OpenSpec Verify is exactly `PASS`;
3. required human validation is complete;
4. specs are synchronized;
5. the change is archived;
6. the context pack is regenerated;
7. context integrity passes;
8. final CODEX cross-review is `READY_TO_MERGE`.

**Rationale:** Prevents merge of unarchived or unverified governance work while still allowing early review visibility.  
**Alternatives considered:** Archive only after merge — rejected (merge-eligible without archived change violates closure contract).

## Risks / Trade-offs

- **[Risk] Candidate docs diverge from specs** → Mitigation: gap review task against each US/spec scenario; correct docs before Verify.
- **[Risk] Operators treat bootstrap presence as completion** → Mitigation: current-state and bootstrap docs explicitly forbid US/slice completion until Verify `PASS`.
- **[Risk] Context pack includes future-wave leakage** → Mitigation: generator resolves only active wave sources; check fails on source-list drift.
- **[Risk] Scope creep into S02–S04** → Mitigation: proposal exclusions and tasks refuse Nx/Docker/CI work.
- **[Risk] GitHub protection requires Silverio authorization** → Mitigation: prepare exact verification steps; record confirmation; do not claim protection without evidence.
- **[Risk] Manual edits to generated OpenSpec skills** → Mitigation: tasks and specs forbid it; regenerate only via `openspec update`.
- **[Trade-off] Process-heavy first slice** → Accepted: later slices depend on unambiguous governance.

## Migration Plan

1. Create/confirm wave branch `wave/w00-project-foundation` and slice branch `slice/w00-s01-repository-governance-and-openspec-foundation`.
2. Apply OpenSpec tasks: adopt/correct governance docs, project agent rules, OpenSpec config/workflow validation, context scripts, and GitHub protection evidence.
3. Optionally open a draft slice PR targeting `wave/w00-project-foundation` for review visibility (not merge-eligible yet).
4. Complete automated checks, OpenSpec Verify exactly `PASS`, and any required Silverio human validation (including GitHub settings confirmation when needed).
5. Synchronize specs; archive the change; regenerate and integrity-check the context pack.
6. Obtain final CODEX cross-review `READY_TO_MERGE`; only then mark the slice PR merge-eligible.
7. **Rollback:** Revert the slice PR on the wave branch; restore previous docs/rules/scripts via Git; reverse evidenced GitHub setting changes if needed; regenerate context pack. No volumes, databases, or MinIO objects are touched.

## Open Questions

- Whether current GitHub org/repo permissions allow CURSOR to apply `main` protection settings, or Silverio must execute the prepared UI/CLI steps and return confirmation during apply.
