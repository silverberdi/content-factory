## Context

Content Factory was initially scaffolded with MySQL persistence assumptions (Pomelo EF Core provider, MySQL migrations, and environment templates expecting a local SSH tunnel on port 3307). However, MySQL was never deployed as a live application database.

The canonical database baseline is PostgreSQL. An existing PostgreSQL instance is already running on the authoritative Ubuntu server (`192.168.0.194`). This change aligns the entire codebase, configuration, migrations, templates, workspace context, and canonical documentation with the existing PostgreSQL instance.

See `proposal.md` for background and motivation.

## Goals / Non-Goals

**Goals:**
- Discover and reuse the existing PostgreSQL instance on `192.168.0.194` without deploying any new instances, containers, or compose services.
- Verify/create `content_factory_dev` and `content_factory_prod` databases and a scoped `content_factory_app` role with strictly scoped least privilege.
- Replace Pomelo MySQL provider with `Npgsql.EntityFrameworkCore.PostgreSQL` in `ContentFactory.Api`.
- Establish a clean, coherent PostgreSQL EF Core migration baseline representing all entities and constraints across CF-001 through CF-004.
- Configure direct LAN development connectivity from macOS to `192.168.0.194:<port>` without requiring SSH tunnels for application runtime.
- Update `.env.development`, tracked templates (`.env.example`, `env/.env.development.example`, `env/.env.production.example`), and `appsettings.json`.
- Enforce strict separation between production bootstrap (schema + canonical roles + SYSTEM_OWNER only) and development seed (demo sources, candidates, items, tasks, ideas).
- Update Dashboard / Factory Health diagnostics to reflect PostgreSQL connection state.
- Update all canonical documentation (`docs/canonical/*`), `.agents/CONTEXT.md`, `.agents/rules/*`, `.agents/skills/*`, synchronized specs (`openspec/specs/*`), bootstrap scripts, templates, README, and `openspec/config.yaml`.
- Execute a comprehensive consistency search ensuring zero active/authoritative MySQL references remain.
- Ensure all existing domain behaviors, entities, and tests remain intact.

**Non-Goals:**
- Implementing Script generation (CF-005) or any new editorial feature.
- Creating a new PostgreSQL container, service, or parallel installation.
- Exposing PostgreSQL to the public internet or through Cloudflare Tunnel.
- Modifying or deleting unrelated databases, roles, or services on `192.168.0.194`.
- Migrating live production data (since MySQL was never active in live production).
- Adopting PostgreSQL-specific `xmin` concurrency tokens (the application-managed `Version: long` token is preserved).
- Seeding demo/development data into `content_factory_prod`.

## Decisions

### 1. EF Core Provider: `Npgsql.EntityFrameworkCore.PostgreSQL`
- **Choice**: Use the official, standard `Npgsql.EntityFrameworkCore.PostgreSQL` package (v9.x/v10.x compatible with .NET 10).
- **Rationale**: It is the industry standard, actively maintained provider for PostgreSQL in .NET, offering full support for EF Core migrations, connection pooling, and retry policies.
- **Alternatives Considered**: Retaining MySQL/Pomelo (rejected: obsolete), SQLite (rejected: not canonical for Content Factory).

### 2. Infrastructure Reuse: Existing Server at 192.168.0.194
- **Choice**: Discover and reuse the PostgreSQL instance already operational on `192.168.0.194`.
- **Rationale**: Strictly complies with the project's infrastructure topology and prevents redundant resource usage or conflicting database services on the server.
- **Alternatives Considered**: Creating a new docker container or standalone postgres service (explicitly forbidden).

### 3. Direct LAN Development Connectivity
- **Choice**: Development workstations connect directly to `192.168.0.194:<LAN_PORT>` over the trusted local network.
- **Rationale**: The server resides on the trusted private LAN. Direct LAN access avoids brittle SSH tunnel dependencies during everyday development while maintaining network isolation from the public internet. SSH is reserved for administrative tasks.
- **Alternatives Considered**: Requiring persistent SSH tunnels for dev DB access (rejected: unnecessary complexity for LAN dev).

### 4. Migration Strategy: Clean PostgreSQL Baseline
- **Choice**: Replace old MySQL-specific migration files with a unified PostgreSQL baseline migration (`CF001_to_CF004_PostgreSqlBaseline`) that accurately models all existing aggregates: Identity, Channels, Audit, Discovery, ContentItem, TruthSource, ContentIdea, EditorialTasks, and AiRecommendations.
- **Rationale**: Because MySQL was never connected as a production database, removing stale provider-specific migrations and establishing a clean PostgreSQL baseline eliminates migration noise, invalid charset annotations, and dialect incompatibilities.
- **Alternatives Considered**: Attempting to incrementally migrate MySQL migrations to PostgreSQL (rejected: messy and error-prone).

### 5. Preservation of `Version: long` Concurrency Token
- **Choice**: Maintain explicit application-managed `Version: long` property configured as `.IsConcurrencyToken()` across domain aggregates (`ContentItem`, `TruthSource`, `ContentIdea`).
- **Rationale**: Guarantees domain portability, predictable incrementing logic, and explicit audit trail alignment without coupling to PostgreSQL internal system columns (e.g. `xmin`).
- **Alternatives Considered**: PostgreSQL `xmin` system column (rejected: couples domain model to provider internals).

### 6. Application Role & Security Model (`content_factory_app`)
- **Choice**: Scope `content_factory_app` strictly to Content Factory databases (`content_factory_dev`, `content_factory_prod`) and schema `public`.
  - The role receives database `CONNECT`, schema `public` usage/create, and full table/sequence DDL/DML permissions required to execute EF Core migrations and runtime queries.
  - The role **MUST NOT** be superuser, have `CREATEDB`, have `CREATEROLE`, manage unrelated roles, access unrelated databases, or hold cluster-wide privileges.
  - Administrative provisioning (database creation, role creation, grants) is performed once via the administrative SSH path (`silverman@192.168.0.194`).
  - Normal development and production application runtimes connect exclusively with `content_factory_app`.
- **Rationale**: Enforces least privilege, prevents blast-radius risk to unrelated server databases, and avoids overcomplicated multi-role setups.

### 7. Clean Production Initialization vs. Development Seed
- **Choice**: Enforce strict environment-based initialization in `DatabaseInitializer`:
  - `content_factory_prod`: Applies EF Core migrations, seeds canonical roles/permissions, and verifies/seeds the protected `SYSTEM_OWNER` record. Zero demo/development content is created.
  - `content_factory_dev`: Applies EF Core migrations, initializes system/owner defaults, and additionally seeds reproducible representative test data (pilot channel `ia-simple-es`, DiscoverySources, DiscoveryCandidates, ContentItems, Evidences, TruthSources, EditorialTasks, ContentIdeas).
- **Rationale**: Keeps production clean and unpolluted while preserving instant out-of-the-box local development testability.

### 8. Repository Consistency Search & Active-Reference Cleanup Rule
- **Choice**: Perform a mandatory repository-wide scan for `mysql`, `pomelo`, `MYSQL_`, `3306`, and `3307`.
  - Every match must be classified as:
    - **ACTIVE/OBSOLETE**: Corrected immediately in this change across canonical docs, agent rules/skills, synchronized specs, bootstrap, env files, config, code, tests, and scripts.
    - **HISTORICAL/NON-AUTHORITATIVE**: Allowed only in archived changes (`openspec/changes/archive/*`) where preserving historical context does not influence current implementation.
  - Zero active authoritative MySQL references may remain.

## Risks / Trade-offs

- **[Risk: Dialect differences in SQL/types (UUID, DateTime UTC, text/JSON)]**  
  *Mitigation*: EF Core maps .NET `Guid` natively to PostgreSQL `uuid`, `DateTime` (UTC) to `timestamp with time zone`, and JSON columns via string/jsonb where configured. Verified through unit and integration tests.
- **[Risk: Obsolete MySQL environment variables causing confusion]**  
  *Mitigation*: Replace active `MYSQL_*` variables with `POSTGRES_*` and `DB_PROVIDER=postgresql` across all environment files and code fallbacks.
- **[Risk: Interruption to In-Memory unit testing]**  
  *Mitigation*: Retain `USE_IN_MEMORY_DB=true` support so isolated unit tests execute quickly without external network dependencies.
