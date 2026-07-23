# repository-governance Specification

## Purpose

Branch protection, wave/slice PR targeting, operator assignment, and delivery hierarchy
enforcement for Content Factory.

## Requirements

### Requirement: Delivery hierarchy is authoritative
The repository SHALL govern delivery through the hierarchy `Roadmap → Wave → Slice → User Stories → OpenSpec tasks`, with one slice normally mapping to one OpenSpec change.

#### Scenario: Slice maps to a single OpenSpec change
- **WHEN** an active slice is assigned for implementation
- **THEN** the wave contract and backlog SHALL identify exactly one expected OpenSpec change ID for that slice
- **AND** User Stories for that slice SHALL reference the same change ID

#### Scenario: Future-wave work is refused
- **WHEN** an operator attempts to implement scope belonging to a later wave than the active wave
- **THEN** the governance contract SHALL treat that work as out of scope
- **AND** the change SHALL not include that scope in proposal, design, specs, or tasks

### Requirement: Branch and pull-request model protects integration
The repository SHALL use `main` for the last fully completed wave, `wave/*` for active-wave integration, and `slice/*` branches created from the active wave branch. Slice pull requests SHALL target `wave/*`. Wave pull requests SHALL target `main`. Direct pushes to protected `wave/*` branches or `main` SHALL be forbidden. Silverio SHALL manually merge completed waves to `main`.

#### Scenario: Slice PR targets wave branch
- **WHEN** a slice branch is ready for integration
- **THEN** its pull request MUST target the active `wave/*` branch
- **AND** MUST NOT target `main` as the merge base for slice integration

#### Scenario: Wave merge requires Silverio
- **WHEN** a wave is ready to enter `main`
- **THEN** the wave pull request MUST target `main`
- **AND** merge into `main` MUST require Silverio's manual merge after wave completion evidence is `READY_FOR_MAIN`

#### Scenario: Direct push to protected branches is invalid
- **WHEN** an operator attempts to push commits directly to a protected `wave/*` branch or `main`
- **THEN** the governance contract SHALL classify the action as invalid
- **AND** recovery SHALL use a pull request on the correct branch model instead

### Requirement: Basic GitHub repository protection is evidenced in w00-s01
Slice `w00-s01` SHALL establish and provide acceptance evidence that basic GitHub repository protection is actually configured or verified for:

- `main` rejects direct pushes;
- `main` rejects force pushes and deletion;
- pull requests are required for `main`;
- `wave/*` follows the slice-to-wave pull-request model;
- slice PRs target `wave/*`;
- wave PRs target `main`;
- Silverio manually merges completed waves to `main`.

GitHub Actions checks, Nx validation, CI-driven merge gates, and fully automated slice auto-merge SHALL remain out of scope for `w00-s01` and assigned to `w00-s04`. If repository settings require Silverio authorization, the operator SHALL prepare exact command or UI verification steps and record Silverio confirmation; the slice MUST NOT claim protection exists without that evidence.

#### Scenario: Main branch basic protections are evidenced
- **WHEN** `w00-s01` claims repository governance acceptance for GitHub protection
- **THEN** evidence MUST show `main` rejects direct pushes, rejects force pushes and deletion, and requires pull requests
- **AND** the evidence MUST be from repository settings verification, CLI output, or recorded Silverio confirmation

#### Scenario: Wave and slice PR targeting model is evidenced
- **WHEN** `w00-s01` claims PR-model acceptance
- **THEN** evidence MUST show slice PRs target `wave/*`, wave PRs target `main`, and Silverio manually merges completed waves to `main`
- **AND** `wave/*` MUST follow the slice-to-wave pull-request model

#### Scenario: Missing Silverio authorization blocks false claims
- **WHEN** applying GitHub protection settings requires Silverio authorization the operator does not have
- **THEN** the operator MUST prepare exact CLI or UI verification steps for Silverio
- **AND** MUST record Silverio confirmation before claiming the protection exists
- **AND** MUST NOT pretend the protection is in place without that confirmation

#### Scenario: CI automation remains excluded from w00-s01
- **WHEN** tasks or acceptance criteria for `w00-s01` are authored
- **THEN** they MUST NOT require GitHub Actions checks, Nx validation, CI-driven merge gates, or fully automated slice auto-merge
- **AND** that work MUST remain assigned to `w00-s04`

### Requirement: Complete-slice ownership and cross-review
Each slice SHALL be owned end-to-end by one implementer (`CURSOR` or `CODEX`). The non-implementing operator SHALL perform mandatory cross-review with verdict exactly `READY_TO_MERGE` or `CHANGES_REQUIRED`. Difficulty SHALL NOT authorize reassignment or abandonment.

#### Scenario: Cross-review gate
- **WHEN** implementation claims a slice is merge-eligible
- **THEN** the non-implementing operator MUST record a cross-review verdict of `READY_TO_MERGE` or `CHANGES_REQUIRED`
- **AND** `CHANGES_REQUIRED` findings MUST be resolved before merge eligibility

#### Scenario: Difficulty does not reassign
- **WHEN** a slice becomes difficult or complex during implementation
- **THEN** ownership MUST remain with the assigned implementer
- **AND** only a genuine external blocker MAY pause work under the same operator

### Requirement: Wave and slice closure evidence
A slice SHALL close only when acceptance criteria are satisfied, automated checks `PASS`, OpenSpec Verify is exactly `PASS`, documentation and context are synchronized, the OpenSpec change is synchronized and archived, the context pack is regenerated with integrity `PASS`, cross-review is `READY_TO_MERGE`, and no hidden deferred work remains. Human validation (deploy, smoke, Silverio `GO`, and GitHub-setting confirmation when required) SHALL apply when the wave or slice contract requires it.

#### Scenario: Incomplete verification blocks closure
- **WHEN** OpenSpec Verify returns any result other than exactly `PASS`
- **THEN** the slice MUST NOT be marked completed
- **AND** User Stories in the slice MUST NOT be marked completed

#### Scenario: Bootstrap artifacts are not completion
- **WHEN** governance docs, rules, or scripts are present from bootstrap before Verify `PASS`
- **THEN** those artifacts MUST be treated as candidate implementation only
- **AND** no User Story or slice MAY be marked completed solely because candidates exist

#### Scenario: Draft PR is not merge-eligible early
- **WHEN** a draft slice PR is opened for review visibility before archival and final gates
- **THEN** the PR MUST remain non-merge-eligible
- **AND** merge eligibility MUST wait until automated checks `PASS`, Verify is exactly `PASS`, required human validation is complete, specs are synchronized, the change is archived, the context pack is regenerated, context integrity passes, and final CODEX cross-review is `READY_TO_MERGE`
