## Why

Content Factory currently retains active references, configuration, NuGet dependencies, EF Core provider code, and migrations targeting MySQL, based on an earlier assumption. However, MySQL was never connected as a live production database for Content Factory, and the canonical application database is PostgreSQL.

To eliminate architecture drift, provider mismatch, and unexecutable configuration before progressing to Script generation (CF-005), this change executes a transversal persistence realignment across implementation, configuration, migrations, templates, workspace context, skills, rules, and canonical documentation to establish PostgreSQL as the sole canonical database baseline.

## What Changes

- **PostgreSQL EF Core Provider**: Replace `Pomelo.EntityFrameworkCore.MySql` with `Npgsql.EntityFrameworkCore.PostgreSQL` in `ContentFactory.Api.csproj`.
- **Infrastructure Discovery & Reuse**: Discover and reuse the existing PostgreSQL instance on Ubuntu server `192.168.0.194` without creating new instances, containers, or services.
- **Database & Scoped Role Provisioning**: Verify/create `content_factory_dev` and `content_factory_prod` databases on the existing PostgreSQL instance, along with a scoped least-privilege application role (`content_factory_app`) owning its database objects without superuser, CREATEDB, CREATEROLE, or cluster-wide privileges.
- **Direct LAN Development Connectivity**: Configure direct LAN connectivity (`192.168.0.194:<port>`) for local development without requiring an SSH tunnel for normal application DB access (SSH retained for administration only).
- **Environment & Configuration Realignment**: Update `.env.development`, tracked examples (`.env.example`, `env/.env.development.example`, `env/.env.production.example`), and `appsettings.json` to canonical PostgreSQL settings (`DB_PROVIDER=postgresql`, `POSTGRES_*`), removing obsolete active MySQL variables.
- **Clean PostgreSQL Migration Baseline**: Replace obsolete MySQL-specific migrations with a clean, coherent EF Core PostgreSQL migration baseline representing the complete domain schema (CF-001 through CF-004) while preserving `Version: long` optimistic concurrency.
- **Production Initialization Cleanliness**: Ensure `content_factory_prod` receives only schema migrations, canonical roles/permissions, and essential SYSTEM_OWNER bootstrap, strictly excluding demo/development seed data (which is restricted to `content_factory_dev`).
- **Factory Health & Dashboard Diagnostics**: Update backend health checks, startup diagnostics, and dashboard health reporting to accurately display PostgreSQL connection and database status.
- **Backup Architecture Alignment**: Update backup documentation and operational definitions to PostgreSQL semantics (`pg_dump`/restore + Google Drive off-site archive).
- **Authoritative Context & Zero Active Leaks**: Update all active documentation (`docs/canonical/*`, `.agents/CONTEXT.md`, `.agents/rules/*`, `.agents/skills/*`, `openspec/config.yaml`, `openspec/specs/*`, `bootstrap/*`, `env/*`, `README.md`, `src/*`, `tests/*`, and tools) ensuring zero active MySQL references remain in authoritative contexts.

## Capabilities

### New Capabilities
- None (transversal persistence baseline realignment).

### Modified Capabilities
- `application-shell-dashboard`: Update Factory Health reporting requirement to reflect PostgreSQL database provider status and connectivity diagnostics.

## Impact

- **Dependencies**: `ContentFactory.Api.csproj` (remove Pomelo, add Npgsql).
- **Application Backend**: `Program.cs`, `AppDbContext.cs`, `AppDbContextFactory.cs`, `DatabaseInitializer.cs`, `DashboardService.cs`.
- **Database Schema & Migrations**: `src/api/ContentFactory.Api/Migrations/*` regenerated for PostgreSQL.
- **Configuration & Secrets**: Real local `.env.development`, `.env.example`, `env/.env.development.example`, `env/.env.production.example`.
- **Canonical Authority & Agent Context**: `docs/canonical/*`, `.agents/CONTEXT.md`, `.agents/rules/*`, `.agents/skills/*`, `openspec/config.yaml`, `openspec/specs/*`, `README.md`.
- **Infrastructure**: Existing PostgreSQL instance on `192.168.0.194` (databases `content_factory_dev`, `content_factory_prod` and scoped role `content_factory_app`).
