# generation-job-persistence Specification

## ADDED Requirements

### Requirement: Persist the current generation Job model
The PostgreSQL schema SHALL match the durable generation `Job` fields used by the API and workers.

#### Scenario: Upgrade an existing jobs table
- **WHEN** EF Core applies `AddJobGenerationMetadata` after the previously recognized migrations
- **THEN** the existing `jobs.Capability` values are preserved in `jobs.JobType`
- **AND** `AutomaticRetriesRemaining`, `CandidateCount`, `GenerationRevision`, and nullable `ExecutionParametersSnapshot` exist
- **AND** existing rows receive defaults of 2, 1, and 1 for the three non-null integer fields, respectively
- **AND** the migration is recorded in `__EFMigrationsHistory`

#### Scenario: Start generation workers after migration
- **WHEN** the API starts against PostgreSQL after applying the migration chain
- **THEN** the visual and voice workers can query `jobs` without missing-column errors
