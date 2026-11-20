# provider-result-persistence Specification

## ADDED Requirements

### Requirement: Persist provider simulation nature
The PostgreSQL schema SHALL represent the optional simulation nature of AI recommendations and generated assets without assigning a false value to historical rows.

#### Scenario: Apply provider nature migration
- **WHEN** EF Core applies `AddProviderNatureMetadata`
- **THEN** `ai_recommendations.IsSimulated` and `generated_assets.IsSimulated` exist as nullable PostgreSQL `boolean` columns
- **AND** the migration is present in EF Core's discovered migration list and is recorded in `__EFMigrationsHistory`
- **AND** existing rows remain valid with `NULL` for the new metadata
