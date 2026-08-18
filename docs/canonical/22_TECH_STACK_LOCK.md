# Technology Stack Lock

This is normative unless a later canonical decision explicitly changes it.

## Frontend
- Angular 21 stable
- TypeScript strict
- standalone components
- Angular Signals where appropriate
- RxJS where stream semantics are appropriate
- PrimeNG 21 stable
- Tailwind CSS 4
- PrimeNG/Tailwind integration
- Angular PWA/service worker
- Angular CDK selectively
- Playwright for browser/e2e visual-flow verification
- Angular test tooling selected from current official Angular defaults at scaffold time

Do not use PrimeNG 22 release candidates for the production baseline.

## Backend
- .NET 10 LTS
- ASP.NET Core
- OpenAPI
- PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- structured logging
- modular monolith

## Storage/orchestration
- existing PostgreSQL on Ubuntu (`192.168.0.194`)
- MinIO
- single production n8n workflow set
- Google Drive off-site backup/archive

## AI
- DeepSeek default reasoning provider
- Gemini configurable alternate/multimodal provider
- provider abstraction from first AI capability

## Development
- Antigravity
- OpenSpec
- DeepSeek cross-review
