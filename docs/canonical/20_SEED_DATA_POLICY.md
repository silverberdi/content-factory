# Seed and Demo Data Policy

Seed data is part of the relevant implementation change, never an afterthought.

## Development

Must be reproducible and resettable.

Initial seed includes where applicable:
- SYSTEM_OWNER `silverio.bernal@gmail.com`;
- TECHNICAL role;
- EDITORIAL role;
- owner assignments;
- pilot channel `IA Simple ES`;
- provider registry;
- DeepSeek global default;
- Gemini alternate entry;
- representative dashboard health/status data;
- representative notifications/attention examples when the feature exists;
- feature flags.

## Production

Bootstrap only required canonical system data:
- roles/capabilities;
- owner identity;
- required defaults.

Never seed fake operational content in production.

## Human testing

Every change must create the data needed to exercise its acceptance criteria without lengthy manual preparation.
