## ADDED Requirements

### Requirement: Current context pack generation
The repository SHALL provide an automated generator that builds `docs/context/generated/current-context-pack.md` and a companion manifest from canonical global sources plus active-wave sources only.

#### Scenario: Generate writes pack and manifest
- **WHEN** an operator runs `node scripts/context/generate-context-pack.mjs`
- **THEN** the generator MUST write `docs/context/generated/current-context-pack.md`
- **AND** MUST write `docs/context/generated/context-manifest.json` with source paths and hashes

#### Scenario: Active wave scoping
- **WHEN** the generator resolves sources from `docs/context/current-state.md`
- **THEN** it MUST include the active wave contract, execution plan, and active-wave User Story catalog
- **AND** MUST NOT inject all future wave contracts or all User Story catalogs into the pack

### Requirement: Context pack integrity validation
The repository SHALL provide an integrity check that fails when the generated pack or manifest is stale relative to current sources. Operators SHALL run the check before implementation or review.

#### Scenario: Current pack passes
- **WHEN** sources, pack, and manifest are synchronized
- **AND** an operator runs `node scripts/context/check-context-pack.mjs`
- **THEN** the check MUST exit successfully and report that the context pack is current

#### Scenario: Stale pack fails
- **WHEN** a tracked source file changes without regenerating the pack
- **AND** an operator runs `node scripts/context/check-context-pack.mjs`
- **THEN** the check MUST fail
- **AND** MUST identify stale paths

#### Scenario: Failed check requires regenerate-then-recheck
- **WHEN** the integrity check fails
- **THEN** the operator MUST regenerate with `node scripts/context/generate-context-pack.mjs`
- **AND** MUST re-run the integrity check successfully before continuing implementation or review

### Requirement: Regeneration cadence
The current context pack SHALL be regenerated and validated at every completed slice and every completed wave.

#### Scenario: Slice completion regenerates context
- **WHEN** a slice reaches synchronized closure
- **THEN** the context pack MUST be regenerated
- **AND** the integrity check MUST pass before the slice is treated as complete

#### Scenario: Manual edits to generated pack are invalid
- **WHEN** an operator finds the generated pack incorrect
- **THEN** the operator MUST correct canonical sources and regenerate
- **AND** MUST NOT hand-edit `docs/context/generated/current-context-pack.md` as the source of truth
