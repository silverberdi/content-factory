## ADDED Requirements

### Requirement: Expanded verified OpenSpec lifecycle
Content Factory SHALL use the OpenSpec expanded verified workflow for slice changes: propose artifacts, apply tasks, verify with result exactly `PASS`, synchronize canonical docs and specs, then archive the change.

#### Scenario: Propose before apply
- **WHEN** a new slice change is started
- **THEN** proposal, design, specs, and tasks artifacts MUST be created until the change is apply-ready
- **AND** implementation MUST NOT begin before apply-required artifacts are complete

#### Scenario: Verify must be exact PASS
- **WHEN** implementation of a change is claimed complete
- **THEN** OpenSpec Verify MUST return exactly `PASS`
- **AND** results such as `PASS WITH NOTES` MUST be treated as non-closure

#### Scenario: Archive before merge eligibility
- **WHEN** a slice PR is to become merge-eligible
- **THEN** specs MUST already be synchronized
- **AND** the OpenSpec change MUST already be archived
- **AND** the archive MUST NOT leave hidden deferred acceptance criteria

### Requirement: Deviation synchronization
When an unplanned requirement, dependency, decision, or blocker appears, operators SHALL stop affected execution, analyze impact, update User Stories and affected contracts, update the OpenSpec change, validate consistency, then resume with the same operator. Missing work SHALL NOT be hidden as notes or informal debt.

#### Scenario: Unplanned requirement discovered mid-slice
- **WHEN** an unplanned requirement is discovered during implementation
- **THEN** affected execution MUST stop
- **AND** User Story, backlog, roadmap, wave contract, and OpenSpec artifacts MUST be synchronized before work resumes

#### Scenario: Hidden debt is invalid closure
- **WHEN** acceptance criteria remain unmet
- **THEN** the slice MUST NOT close by recording informal notes or deferred debt in place of synchronized User Stories and OpenSpec updates

### Requirement: Change identity and scope binding
Each active slice OpenSpec change SHALL bind to the declared wave ID, slice ID, included User Stories, implementer, and reviewer, and SHALL refuse future-wave scope.

#### Scenario: Change references slice contract
- **WHEN** proposal or tasks for a change are authored
- **THEN** they MUST reference the wave ID, slice ID, and every included User Story ID
- **AND** they MUST identify the primary implementer and mandatory cross-reviewer

#### Scenario: Out-of-slice scope is rejected
- **WHEN** a proposed task implements a later w00 slice or a later wave
- **THEN** the change MUST exclude that task from scope
- **AND** the excluded work MUST remain assigned to its declared future slice change

### Requirement: Generated integrations are validated not hand-edited
OpenSpec workflow adoption SHALL validate generated Cursor/Codex integrations for presence, OpenSpec `1.6.0` compatibility, and workflow coverage. Regeneration, when needed, SHALL use official `openspec update` only.

#### Scenario: Workflow coverage check without manual skill edits
- **WHEN** `w00-s01` confirms the expanded verified workflow is available to operators
- **THEN** presence and compatibility of generated integrations MUST be validated
- **AND** project-specific policy MUST remain in `AGENTS.md`, `.cursor/rules/`, `openspec/config.yaml`, and canonical docs
- **AND** generated skill/command trees MUST NOT be manually modified
