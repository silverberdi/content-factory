# Content Factory — Antigravity Bootstrap

This repository bootstrap is the canonical starting point for the Content Factory implementation.

## Purpose

Content Factory is a private, multi-user, multi-channel editorial operating system that discovers useful source material, transforms it into original short-form audiovisual content, produces and reviews assets, publishes through controlled platform integrations, and learns from performance.

The implementation MUST follow the canonical documents under `docs/canonical/`.

## Authority

Order of authority:

1. `docs/canonical/00_CANONICAL_AUTHORITY.md`
2. Business intent and product vision
3. Architecture, security and UX constitutions
4. Domain model and state rules
5. Roadmap and backlog
6. OpenSpec change artifacts
7. Implementation tasks
8. Source code

A lower level MUST NOT contradict a higher level.

## OpenSpec

Do not manually edit OpenSpec-generated Antigravity integration assets after initialization.
Custom project instructions belong under `.agents/`.

Setup is performed using the separate `CONTENT_FACTORY_OPENSPEC_COMMANDS.md` file delivered with this package.

## First implementation target

The prepared first change is:

`foundation-access-control-center`

It is intentionally a vertical, human-testable increment. When complete, an operator must be able to:

- run the application locally on macOS;
- enter through development-only GOD mode;
- use a responsive PWA shell with light/dark mode;
- see a real operational dashboard;
- create and manage the first editorial channel;
- exercise the initial identity/authorization model;
- see seeded representative operational data;
- verify the feature through automated and human UX tests.

Production Google authentication is prepared in the same architecture but secrets are supplied via environment configuration.

## Non-negotiable product quality

The application is not a CRUD admin panel and not an inbox.
The dashboard is the operational control center.
Desktop uses the full useful viewport.
Full-page desktop vertical scroll is allowed only when genuinely necessary.
Every change must preserve responsive behavior, security boundaries, auditability and traceability.
