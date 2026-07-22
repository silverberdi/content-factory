# Architecture

## Monorepo

Nx monorepo with:

- `apps/console`: Angular 22 PWA.
- `apps/api`: NestJS with Fastify.
- `apps/media-worker`: Node/TypeScript FFmpeg worker.
- Capability-focused libraries with enforced Nx boundaries.

## UI

PrimeNG and PrimeIcons are mandatory. The UI is Spanish-first, translation-ready, responsive,
mobile-first, and accessible. It supports `light`, `dark`, and `system`; system mode respects
`prefers-color-scheme`, and user choice is persisted.

## Data and runtime

- Prisma with dedicated PostgreSQL database.
- Exclusive MinIO bucket.
- n8n as orchestrator; PostgreSQL as canonical job/schedule state.
- Docker containers; no host runtime installation.
- Cloudflare Tunnel routes `/` to the console and `/api/*` to the API.
