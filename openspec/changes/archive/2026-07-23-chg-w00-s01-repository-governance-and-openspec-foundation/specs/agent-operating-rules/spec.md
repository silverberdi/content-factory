## ADDED Requirements

### Requirement: Canonical shared agent contract
The repository SHALL provide a single shared operating contract for Cursor and Codex covering complete-slice ownership, cross-review, OpenSpec Verify exactly `PASS`, documentation/context synchronization, safety, and evidence standards. Project-specific operating rules SHALL live in `AGENTS.md`, `.cursor/rules/`, `openspec/config.yaml`, and canonical docs.

#### Scenario: AGENTS.md is present and authoritative
- **WHEN** an operator starts implementation or review
- **THEN** `AGENTS.md` MUST exist and state the delivery hierarchy, branch model, Verify rule, deviation procedure, and safety constraints
- **AND** both Cursor and Codex MUST treat it as binding operating policy

#### Scenario: Cursor rules enforce governance gates
- **WHEN** Cursor begins implementation or review
- **THEN** project governance and delivery-evidence rules MUST require running the context integrity check and reading the current context pack
- **AND** they MUST forbid future-wave scope and incomplete Verify closure

#### Scenario: Project-owned files hold policy
- **WHEN** project-specific operating policy is added or corrected
- **THEN** the change MUST be made in `AGENTS.md`, `.cursor/rules/`, `openspec/config.yaml`, and/or canonical docs
- **AND** MUST NOT be made by hand-editing generated OpenSpec skill or command files

### Requirement: Generated OpenSpec integrations are immutable
OpenSpec-generated integrations under `.cursor/commands/`, `.cursor/skills/`, and `.codex/skills/` SHALL be treated as immutable generated artifacts. The change MAY validate their presence, OpenSpec `1.6.0` compatibility, and configured workflow coverage, and MAY regenerate them only through the official `openspec update` command. Manual modification of those trees is forbidden.

#### Scenario: Presence and compatibility are validated
- **WHEN** `w00-s01` evaluates OpenSpec operator integrations
- **THEN** evidence MUST confirm the generated command/skill trees are present
- **AND** MUST confirm OpenSpec `1.6.0` compatibility and required workflow coverage

#### Scenario: Regeneration uses official command only
- **WHEN** generated OpenSpec integrations are missing or incompatible
- **THEN** the operator MUST regenerate them with official `openspec update`
- **AND** MUST NOT manually edit files under `.cursor/commands/`, `.cursor/skills/`, or `.codex/skills/`

### Requirement: Safety and non-destructive operations
Agent operating rules SHALL forbid exposing or committing secrets, destructive volume/database resets, unsafe schema commands, and delegation of repository file edits to Silverio when the agent can perform them.

#### Scenario: Secrets are never committed
- **WHEN** an operator prepares a commit or PR
- **THEN** secrets, tokens, cookies, and sensitive bodies MUST NOT be included in Git history or logs

#### Scenario: Destructive resets are forbidden
- **WHEN** an operator considers recovering from a failure
- **THEN** the operating rules MUST forbid deleting persistent volumes or resetting shared databases
- **AND** recovery MUST use non-destructive, reversible steps

#### Scenario: Agents edit files themselves
- **WHEN** a required repository file change is within agent capability and is not an immutable generated OpenSpec integration
- **THEN** the agent MUST perform the edit
- **AND** MUST NOT ask Silverio to edit the file manually

### Requirement: Evidence standard for slice completion
Operators SHALL record wave, slice, User Stories, implementer, reviewer, change ID, Verify `PASS`, automated checks, acceptance evidence, human validation evidence when applicable, documentation/context hashes, PR result, and confirmation of zero hidden scope before claiming slice completion.

#### Scenario: Completion report fields are present
- **WHEN** a slice claims readiness to merge
- **THEN** evidence MUST include Verify exactly `PASS`, cross-review `READY_TO_MERGE`, synchronized docs/context, and confirmation of zero hidden scope
- **AND** missing required evidence MUST block closure
