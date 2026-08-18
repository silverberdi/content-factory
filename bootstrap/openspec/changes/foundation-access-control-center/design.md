# Design — Foundation Access Control Center

## Solution

Recommended repository shape:

- `src/web/` Angular PWA
- `src/api/` ASP.NET Core
- `tests/` if not colocated by framework convention
- `docs/`
- `openspec/`
- `.agents/`

Do not create microservices.

## Backend modules

- Identity
- Authorization
- Channels
- Audit
- Dashboard
- SharedKernel only for genuinely cross-cutting primitives

## Authentication

Define an authentication provider boundary.

Providers:
- DevelopmentBypassAuthenticationProvider
- GoogleAuthenticationProvider

Production startup guard rejects DevelopmentBypass.

Development GOD identity:
- email silverio.bernal@gmail.com
- SYSTEM_OWNER = true
- TECHNICAL
- EDITORIAL

Google integration may require real credentials after implementation, but route/config/provider shape and production safety must exist.

## Authorization

Model capabilities/policies explicitly.

At minimum:
- channel.manage
- users.invite
- users.roles.manage
- editorial.video.approve
- publication.execute
- costs.view

Do not rely only on frontend visibility.

## Persistence

Use existing PostgreSQL.
Database selection is environment configuration.

Use migrations.
Provider/package must be stable and compatible with .NET 10 and selected EF Core version at implementation time; do not adopt prerelease dependencies silently.

Use optimistic concurrency on mutable aggregate records where supported in this slice.

## Frontend

Application shell:
- compact header;
- grouped navigation;
- main operational workspace;
- responsive transformation.

Dashboard:
- initial factory health;
- channel summary;
- attention summary using seeded/real available state;
- compact quick actions.

No fake metrics pretending production exists.
Seeded development examples must be visually marked as development/demo data where ambiguity could mislead.

Channel management:
- efficient data grid/list;
- create/edit in dialog or drawer where appropriate;
- statuses and language/niche visible;
- avoid full-page CRUD form unless justified.

## Theme

Light/dark immediately.
Persist user preference locally initially; backend preference can come later.

## Responsive

Desktop primary dashboard information fits useful viewport at 1440x900 and 1920x1080 unless a documented exception exists.
Tablet remains near desktop.
Mobile prioritizes health, attention and quick channel actions.

## Audit

Record:
- invitation creation/revoke;
- role change;
- channel create/update/status change;
- owner-protection violations when meaningful.

## Seed

Development reset produces:
- owner;
- roles/capabilities;
- IA Simple ES channel;
- representative non-destructive attention/demo state sufficient to validate dashboard composition.

Production bootstrap produces only canonical system data, not fake content.

## Tests

Backend:
- auth-mode startup guard;
- owner protection;
- role authorization;
- invitation exact-email behavior;
- channel API;
- audit.

Frontend/e2e:
- GOD mode entry;
- dashboard;
- channel create/edit;
- dark/light;
- responsive viewports;
- permission-driven controls with mocked role variations.
