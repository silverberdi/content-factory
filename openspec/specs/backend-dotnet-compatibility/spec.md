# backend-dotnet-compatibility Specification

## Purpose

Establece la compatibilidad verificable del backend y su suite de pruebas con .NET 10 LTS, preservando los contratos, el modelo relacional y el comportamiento existente del sistema.

## Requirements

### Requirement: Backend projects target the canonical runtime

The API project and its backend test project MUST target `net10.0`. Direct ASP.NET Core, Entity Framework Core, and PostgreSQL provider dependencies MUST be stable versions compatible with .NET 10, and already-conforming dependencies MUST remain unchanged unless a real compatibility issue is demonstrated.

#### Scenario: 1:1 Initial inventory preserves conforming components
- **WHEN** the API and test project files and their direct dependencies are inventoried before the change
- **THEN** components already compatible with .NET 10 are recorded as preserved and only incompatible target frameworks or dependency versions are selected for alignment

#### Scenario: 1:2 API and tests are aligned
- **WHEN** the compatibility alignment is complete
- **THEN** both projects target `net10.0`, ASP.NET Core and EF Core test/runtime dependencies are compatible with .NET 10, and `Npgsql.EntityFrameworkCore.PostgreSQL` is a stable compatible version

### Requirement: Existing backend contracts and relational state are preserved

The compatibility alignment MUST preserve existing API contracts, entities, relationships, historical migrations, persisted data, and backend behavior. It MUST NOT apply migrations to production.

#### Scenario: 2:1 Existing backend behavior remains available
- **WHEN** the aligned backend is exercised through its existing automated API and domain tests
- **THEN** existing functional contracts continue to pass and no endpoint or entity behavior is changed solely because of the runtime alignment

#### Scenario: 2:2 Relational model has no unrelated schema change
- **WHEN** the EF Core model is validated and migration SQL is generated in an isolated environment
- **THEN** the generated result contains no schema change unrelated to .NET 10 compatibility and no production migration is executed

### Requirement: Compatibility evidence is reproducible and bounded

The change MUST provide real evidence from restore, build, and backend tests in a reproducible isolated environment, including the effective ASP.NET Core/EF Core/Npgsql compatibility. The evidence MUST NOT claim deployment or production database validation that was not performed.

#### Scenario: 3:1 Backend regression checks pass
- **WHEN** restore, build, and the configured backend test suite are run in the isolated verification environment
- **THEN** affected tests pass, existing functional contracts are preserved, and any regression attributable to alignment is corrected before completion

#### Scenario: 3:2 Documentation remains scoped
- **WHEN** the change is documented for closure
- **THEN** only directly affected canonical references or local instructions are updated, the diff remains bounded to .NET 10 compatibility, and commands/results plus dependency compatibility evidence are recorded without production validation claims
