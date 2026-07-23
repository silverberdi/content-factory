## 1. Branch and baseline readiness

- [x] 1.1 Confirm wave branch `wave/w00-project-foundation` exists (create from current governed base if missing)
- [x] 1.2 Create or confirm slice branch `slice/w00-s01-repository-governance-and-openspec-foundation` from the wave branch
- [x] 1.3 Run `node scripts/context/check-context-pack.mjs` and regenerate/recheck if needed before implementation edits
- [x] 1.4 Inventory bootstrap candidates (`AGENTS.md`, `.cursor/rules/`, `openspec/config.yaml`, methodology docs, governance docs, context scripts, and presence of generated OpenSpec integrations) against us-w00-s01-001–004 without manually editing `.cursor/commands/`, `.cursor/skills/`, or `.codex/skills/`

## 2. Repository governance (us-w00-s01-001)

- [x] 2.1 Adopt and correct delivery hierarchy docs (`docs/methodology/delivery-methodology.md`, roadmap, backlog, w00 contract/execution plan) for wave → slice → US → OpenSpec tasks
- [x] 2.2 Adopt and correct branch/PR model docs in methodology, `AGENTS.md`, and `docs/governance/github-governance.md` for `main` / `wave/*` / `slice/*`, including Silverio manual wave→`main` merge
- [x] 2.3 Configure or verify basic GitHub protection for `main`: reject direct pushes; reject force pushes and deletion; require pull requests
- [x] 2.4 Configure or verify that `wave/*` follows the slice-to-wave PR model, slice PRs target `wave/*`, and wave PRs target `main`
- [x] 2.5 If GitHub settings require Silverio authorization, prepare exact CLI/UI verification steps, obtain Silverio confirmation, and record it; do not claim protection without evidence
- [x] 2.6 Record acceptance evidence for all basic GitHub protections and PR-target rules above (settings export, CLI output, screenshots, or Silverio confirmation)
- [x] 2.7 Adopt and correct complete-slice ownership and mandatory cross-review wording in wave contract and current-state
- [x] 2.8 Add or update automated/doc contract checks that fail on future-wave scope markers or missing change-ID binding for w00-s01 User Stories
- [x] 2.9 Explicitly exclude GitHub Actions checks, Nx validation, CI-driven merge gates, and fully automated slice auto-merge from this slice (owned by w00-s04)

## 3. OpenSpec expanded verified workflow (us-w00-s01-002)

- [x] 3.1 Validate OpenSpec `1.6.0` config and generated Cursor/Codex integrations for presence, compatibility, and expanded verified workflow coverage; regenerate only with official `openspec update` if needed
- [x] 3.2 Adopt and correct methodology/deviation-policy docs so Verify must be exactly `PASS` and deviations force synchronized US/backlog/wave/OpenSpec updates
- [x] 3.3 Ensure this change's proposal/design/specs/tasks remain bound to w00 / w00-s01 / us-w00-s01-001–004 / CURSOR / CODEX with no S02–S04 or future-wave tasks
- [x] 3.4 Add a lightweight workflow contract test or script assertion covering propose→apply→verify-PASS→sync→archive expectations for w00-s01

## 4. Agent operating rules (us-w00-s01-003)

- [x] 4.1 Adopt and correct `AGENTS.md` for hierarchy, branch model, Verify `PASS`, deviation procedure, safety, and evidence standards
- [x] 4.2 Adopt and correct `.cursor/rules/00-project-governance.mdc` and `.cursor/rules/30-delivery-evidence.mdc` to require context integrity check and forbid incomplete closure
- [x] 4.3 Keep project-specific operating policy in `AGENTS.md`, `.cursor/rules/`, `openspec/config.yaml`, and canonical docs; do not manually edit `.cursor/commands/`, `.cursor/skills/`, or `.codex/skills/` (regenerate only via `openspec update` when required)
- [x] 4.4 Verify safety clauses forbid secrets in Git/logs, destructive volume/DB resets, and delegating file edits to Silverio when agents can edit
- [x] 4.5 Add or update a contract check that required project-owned agent rule files exist and contain the mandatory governance keywords/gates

## 5. Context pack automation (us-w00-s01-004)

- [x] 5.1 Adopt and correct `scripts/context/generate-context-pack.mjs` to emit pack + manifest from global sources plus active-wave sources only
- [x] 5.2 Adopt and correct `scripts/context/check-context-pack.mjs` to fail on stale sources, pack drift, or source-list mismatch
- [x] 5.3 Adopt and correct `docs/context/openspec-context-index.md` and related context docs for regenerate/check paths and cadence
- [x] 5.4 Add automated tests for generate/check success path and at least one stale-pack failure path
- [x] 5.5 Regenerate `docs/context/generated/current-context-pack.md` and validate with `node scripts/context/check-context-pack.mjs`

## 6. Synchronization, verification, and closure gates

- [x] 6.1 Synchronize roadmap, backlog, w00 User Stories, w00 wave contract/execution plan, current-state, and decision register as affected by adoption/corrections
- [x] 6.2 Optionally open a draft slice PR targeting `wave/w00-project-foundation` for review visibility; keep it non-merge-eligible
- [x] 6.3 Run automated checks applicable to this slice (context scripts/tests, machine-ID validation, and any doc contract checks) to `PASS`
- [x] 6.4 Run OpenSpec Verify for this change and obtain result exactly `PASS` (not `PASS WITH NOTES`)
- [x] 6.5 Complete any required Silverio human validation (GitHub protection confirmation and any other contract-required `GO`) with recorded evidence
- [x] 6.6 Synchronize delta specs into main specs as applicable for this change
- [x] 6.7 Archive `chg-w00-s01-repository-governance-and-openspec-foundation` after sync, with no hidden deferred acceptance criteria
- [x] 6.8 Regenerate the context pack and confirm `node scripts/context/check-context-pack.mjs` passes
- [x] 6.9 Prepare final slice evidence pack (wave/slice/US IDs, implementer/reviewer, change ID, Verify `PASS`, GitHub protection evidence, check outputs, doc/context hashes, zero hidden scope)
- [ ] 6.10 Request final mandatory CODEX cross-review and resolve all `CHANGES_REQUIRED` findings until verdict is `READY_TO_MERGE`
- [ ] 6.11 Only after 6.3–6.10 succeed, mark the slice PR merge-eligible for `wave/w00-project-foundation` (never push directly to protected wave/`main`)
- [ ] 6.12 Update current-state only when us-w00-s01-001–004 and the slice are truly complete
