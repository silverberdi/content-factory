## 1. Infrastructure Discovery & Database Provisioning

- [x] 1.1 Inspect PostgreSQL deployment on `192.168.0.194` via administrative SSH (`silverman@192.168.0.194`) to determine host vs containerized, service name, version, and LAN port.
- [x] 1.2 Verify or create `content_factory_dev` and `content_factory_prod` databases on the existing PostgreSQL instance without creating any new instance, container, or service.
- [x] 1.3 Verify or create scoped least-privilege role `content_factory_app` with explicit database `CONNECT`, schema `public` usage/create, and table/sequence DDL/DML privileges on Content Factory databases (no superuser, CREATEDB, CREATEROLE, or cluster-wide privileges).
- [x] 1.4 Validate direct LAN network connectivity from macOS development workstation to `192.168.0.194:5432`.

## 2. EF Core Provider & Backend Realignment

- [x] 2.1 Update `ContentFactory.Api.csproj` to remove `Pomelo.EntityFrameworkCore.MySql` and add `Npgsql.EntityFrameworkCore.PostgreSQL`.
- [x] 2.2 Update `Program.cs` to configure `Npgsql` with connection retry policy, `POSTGRES_*` environment variables, and in-memory test fallback.
- [x] 2.3 Update `AppDbContext.cs` and `AppDbContextFactory.cs` for PostgreSQL provider configuration while preserving `Version: long` optimistic concurrency.
- [x] 2.4 Update `DashboardService.cs` and health check diagnostics to report PostgreSQL connection status.

## 3. PostgreSQL Migrations & Clean Production Initialization

- [x] 3.1 Remove obsolete MySQL-specific migrations from `src/api/ContentFactory.Api/Migrations/`.
- [x] 3.2 Generate a unified, clean EF Core PostgreSQL baseline migration capturing all CF-001 through CF-004 entities, relationships, indexes, and concurrency tokens.
- [x] 3.3 Apply migration to `content_factory_dev` and verify schema structure and development seed initialization.
- [x] 3.4 Apply migration to `content_factory_prod` and verify clean production bootstrap (schema + canonical roles + SYSTEM_OWNER only; zero demo content).

## 4. Configuration & Environment Realignment

- [x] 4.1 Update `.env.development` with active `DB_PROVIDER=postgresql`, `POSTGRES_HOST=192.168.0.194`, `POSTGRES_PORT`, `POSTGRES_DATABASE=content_factory_dev`, `POSTGRES_USER=content_factory_app`, and local secret.
- [x] 4.2 Update `.env.example`, `env/.env.development.example`, `env/.env.production.example`, and `appsettings.json` to canonical PostgreSQL templates.
- [x] 4.3 Remove or supersede obsolete active `MYSQL_*` configuration variables across all active configuration files.

## 5. Authoritative Documentation, Specs & Workspace Synchronization

- [x] 5.1 Update canonical documents `05_SYSTEM_ARCHITECTURE_V2.md`, `11_BACKUP_GOOGLE_DRIVE.md`, `13_ENVIRONMENTS_DEPLOYMENT.md`, `15_DEVELOPMENT_GOVERNANCE.md`, `22_TECH_STACK_LOCK.md`, and `23_DECISION_REGISTER.md` to establish PostgreSQL baseline.
- [x] 5.2 Update `.agents/CONTEXT.md`, `.agents/rules/*`, `.agents/skills/*` (including `backup-drive-guardian`) to reflect PostgreSQL persistence.
- [x] 5.3 Update `openspec/config.yaml`, current synchronized specs in `openspec/specs/*`, bootstrap scripts, and `README.md`.

## 6. Verification, Active-Reference Audit & Quality Gate

- [x] 6.1 Run full backend test suite (`dotnet test`) and verify all tests pass.
- [x] 6.2 Start API and verify Dashboard / Factory Health returns `Connected (PostgreSQL/content_factory_dev)`.
- [x] 6.3 Execute repository-wide consistency search for `mysql`, `pomelo`, `MYSQL_`, `3306`, and `3307`, classifying all matches as ACTIVE/OBSOLETE (corrected to 0) or HISTORICAL/NON-AUTHORITATIVE (archived only).
- [x] 6.4 Validate OpenSpec change status (`openspec validate --all`) and confirm readiness for closure.
