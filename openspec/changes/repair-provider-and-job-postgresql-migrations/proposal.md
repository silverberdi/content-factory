# Proposal

## Why

The deployed .NET 10 API expects PostgreSQL columns that its migration history does not fully describe. EF Core did not list `AddProviderNatureMetadata` because its generated migration metadata was missing. A disposable PostgreSQL run also showed that the current `jobs` model contains fields that were added after the last recognized migration; generation workers then fail when querying those absent columns.

## What Changes

- Restore EF Core discovery metadata for `AddProviderNatureMetadata` while keeping its SQL limited to nullable `IsSimulated` columns on `ai_recommendations` and `generated_assets`.
- Add a separate migration for the existing `Job` model changes: rename `Capability` to `JobType` without losing values and add the persisted generation fields with defaults matching the model.
- Verify migration discovery, generated SQL, and application of the complete migration chain on a disposable PostgreSQL database using the API's production startup path.

## Non-Goals

- No API or provider-routing behavior changes.
- No migration or data changes against development or production databases.
- No unrelated model, schema, or dependency cleanup.

## Impact

This aligns the PostgreSQL schema with the current EF Core model and allows generation workers to query jobs after startup. Existing job type values are preserved by a column rename; newly added integer metadata uses the current model defaults (`2`, `1`, `1`).
