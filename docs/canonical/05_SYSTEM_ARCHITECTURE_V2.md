# System Architecture v2

## Architectural style

Modular monolith for product domain + external orchestration.

Do not introduce microservices, Kubernetes, Redis, an event bus or distributed complexity without measured need.

Expected first-year concurrency: fewer than 10 concurrent users.

## Frontend

- Angular 21 stable line
- standalone components
- signals for local/application reactive state where appropriate
- RxJS for asynchronous streams where it adds value
- PrimeNG 21 stable line
- Tailwind CSS 4
- official PrimeNG/Tailwind integration where useful
- Angular PWA/service worker
- Angular CDK selectively
- modern browsers only

## Backend

- ASP.NET Core on .NET 10 LTS
- REST/JSON API
- OpenAPI contract
- modular feature boundaries
- asynchronous Job model for long operations
- PostgreSQL persistence using official `Npgsql.EntityFrameworkCore.PostgreSQL` provider.

## Persistence

Existing PostgreSQL instance on Ubuntu server (`192.168.0.194`).

Application databases:
- `content_factory_dev`
- `content_factory_prod`

Scoped application role:
- `content_factory_app`

Development runs on macOS and reaches `content_factory_dev` directly over the trusted private LAN (`192.168.0.194:5432`). SSH access is administrative only. Never expose PostgreSQL publicly.

Domain state lives in PostgreSQL.
Media/assets live in MinIO.
Google Drive is off-site backup/archive, not primary runtime storage.

## Orchestration

One n8n production workflow set only.
There is no dev/prod duplication of n8n workflows.

Development must use:
- mocks/adapters by default;
- real n8n only through explicit safe operations.

n8n:
- orchestrates deterministic integrations and long-running workflows;
- receives/returns explicit contracts;
- never becomes the canonical domain database;
- never owns authorization policy.

## Public host defaults

Frontend: `factory.silverman.pro`
Backend: `factory-core.silverman.pro`

These are configuration, but they are the current canonical defaults.

## AI

Provider abstraction by capability.
DeepSeek is default reasoning provider initially.
Gemini is first alternate/multimodal provider.
Local models may later handle cheap preprocessing.

## Core interaction

PWA → Backend → domain state/jobs → n8n/provider adapters → callbacks/results → Backend → PWA.

The UI must never wait synchronously for a long AI/media operation.
