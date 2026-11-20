# Design

## Context

`AddProviderNatureMetadata` already contains the intended two nullable PostgreSQL boolean additions, but lacked the EF Core migration identifier and target-model metadata. Separately, the current `Job` EF model has moved beyond the latest prior migration: the stored `Capability` field is now `JobType`, and four generation metadata fields are queried by the workers.

## Decisions

1. Keep `AddProviderNatureMetadata` as a standalone migration with the existing timestamp and exactly two `ADD COLUMN` operations. Its generated designer provides the EF `Migration` and `DbContext` metadata, and its target model records the two nullable fields.
2. Add `AddJobGenerationMetadata` after it as a separate migration. Rename `jobs.Capability` to `jobs.JobType` so existing values survive. Add `AutomaticRetriesRemaining` (default 2), `CandidateCount` (default 1), `GenerationRevision` (default 1), and nullable `ExecutionParametersSnapshot`.
3. Use EF Core's migration list/script tooling, then start the API in Production mode against a disposable PostgreSQL 16 container. The API's canonical startup path calls `Database.MigrateAsync`; verify health, migration history, columns, and that background workers emit no missing-column errors.

## Boundaries

- PostgreSQL remains the only durable application database.
- Tests use a disposable database and test-only credentials. No production database is touched.
- No new runtime service, API endpoint, or behavior is introduced.
- The database change preserves existing job type values and does not rewrite existing recommendation or asset rows; nullable `IsSimulated` remains unknown for historical records.

## Risks

- Applying the migration to a database whose job columns were changed manually would cause a conflict. Before any production rollout, compare production schema and `__EFMigrationsHistory` with this migration chain; this change itself does not deploy or migrate production.
- Integer defaults for existing jobs intentionally match the `Job` entity defaults.
