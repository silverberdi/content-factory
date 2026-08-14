# Content Factory — Persistent Antigravity Context

Read this before planning or implementing any task.

## Canonical authority

All files under `docs/canonical/` are normative.
Start with:
1. `00_CANONICAL_AUTHORITY.md`
2. the business/product documents;
3. the architecture/security/UX documents relevant to the task;
4. the active OpenSpec change.

Do not reinterpret canonical decisions.

## Product identity

Content Factory is a private editorial operating system.
The dashboard is its operational control center.
It is not an email inbox and not a generic CRUD admin panel.

## Hard architecture

- Angular 21 + PrimeNG 21 stable + Tailwind 4 PWA.
- .NET 10 LTS backend.
- MySQL on existing Ubuntu instance.
- Mac development; Ubuntu production.
- one production n8n workflow set only.
- MinIO runtime assets.
- Google Drive automated off-site backup/archive.
- DeepSeek default reasoning provider; Gemini alternate.
- Cloudflare Tunnel for public frontend/backend.
- modular monolith.

## Hard security

- production Google auth, invitation-only;
- SYSTEM_OWNER = silverio.bernal@gmail.com;
- owner cannot be deleted/disabled/degraded;
- TECHNICAL and EDITORIAL roles assign independently;
- development-only GOD auth provider;
- production MUST refuse development bypass;
- MySQL/MinIO/n8n are not public;
- no secrets committed.

## Hard UX

- dashboard from first slice;
- full useful viewport on desktop;
- page-level desktop scroll only by exception;
- high information density with clear hierarchy;
- no giant headings/cards/hero layouts;
- light/dark from first slice;
- tablet near-desktop;
- mobile handles urgent/frequent actions;
- use PrimeNG and Tailwind consistently;
- no universal table/CRUD template.

## OpenSpec execution

OpenSpec artifacts are binding.
Before code, produce/review a plan that names:
- canonical context read;
- scope and non-goals;
- UX/dashboard impact;
- security boundaries;
- state/data changes;
- test strategy.

Do not implement out-of-scope improvements.

## Completion

Follow `docs/canonical/16_DEFINITION_OF_DONE.md`.
A compile is not completion.
