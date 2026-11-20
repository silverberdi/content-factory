# Tasks

- [x] 1.1 Restore EF Core migration metadata for `AddProviderNatureMetadata` without changing its two intended nullable column operations.
- [x] 1.2 Add the missing `Job` schema migration with a data-preserving `Capability` to `JobType` rename and model-aligned defaults.
- [x] 2.1 Confirm `dotnet ef migrations list` discovers both new migration identifiers and `has-pending-model-changes` reports no model delta.
- [x] 2.2 Inspect generated SQL: CF-04 adds only its two nullable boolean columns; the Job migration renames and adds only the current persisted Job fields.
- [x] 3.1 Apply the full migration chain through API startup against a disposable PostgreSQL 16 database; confirm health, migration history, resulting columns, and no worker missing-column errors.
