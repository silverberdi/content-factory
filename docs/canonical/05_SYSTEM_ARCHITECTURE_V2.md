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
- MySQL persistence using a .NET-10/EF-compatible official/stable provider selected at implementation time; do not use a prerelease provider in production without an explicit canonical decision.

## Persistence

Existing Ubuntu MySQL instance.

Application databases:
- `content_factory_dev`
- `content_factory_prod`

Development runs on macOS and reaches `content_factory_dev` through an SSH tunnel. Never expose MySQL publicly.

Domain state lives in MySQL.
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
