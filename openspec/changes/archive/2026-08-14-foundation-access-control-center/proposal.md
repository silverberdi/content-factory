# Proposal — Foundation Access Control Center

## Business outcome

Deliver the first usable Content Factory increment: a secure, responsive PWA control center that can run locally on macOS, authenticate through development GOD mode, represent the real owner/role model, show a meaningful factory dashboard, and create/manage the first editorial channel.

This change establishes a human-testable product, not only infrastructure.

## In scope

- repository/solution scaffold;
- Angular 21 PWA;
- PrimeNG 21 stable + Tailwind 4;
- light/dark themes;
- responsive application shell;
- .NET 10 backend;
- OpenAPI;
- MySQL application persistence;
- dev/prod configuration;
- development-only GOD auth provider;
- production Google-auth provider boundary/configuration;
- SYSTEM_OWNER bootstrap;
- TECHNICAL and EDITORIAL roles/capabilities;
- invitation and role-management baseline;
- audit events for identity/channel mutations;
- Channel domain CRUD appropriate to product;
- pilot channel seed `IA Simple ES`;
- initial dashboard control center;
- dashboard health/channel/attention widgets based on currently available data;
- internal notification/attention model foundation only to the extent required by this change;
- seed/demo data;
- automated and human tests.

## Out of scope

- real n8n workflow execution;
- Source/TruthSource/ContentIdea production;
- AI provider API calls;
- video production;
- publication API automation;
- metrics ingestion;
- Google Drive backup implementation;
- rich dashboard personalization;
- push notifications.

## Success

A human can run the app locally, enter GOD mode without Google, use the dashboard and channel management at desktop/tablet/mobile sizes, switch themes, and verify role/owner behaviors using seeded data.

Production configuration must demonstrate that development bypass cannot run in Production.
