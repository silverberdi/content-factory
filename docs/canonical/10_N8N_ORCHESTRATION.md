# n8n Orchestration

## Environment rule

There is exactly one production n8n workflow set for Content Factory.

No dev/prod workflow duplication.

## Responsibility

n8n may:
- call AI/media providers;
- move files;
- run deterministic integration workflows;
- coordinate long-running external operations;
- invoke TTS/subtitles/render;
- prepare publication packages;
- execute scheduled operational workflows.

n8n may not:
- become the domain database;
- own authorization decisions;
- expose credentials to the frontend;
- contain duplicated channel-specific business policy when configuration can represent it.

## Contracts

Backend ↔ n8n communication must:
- use explicit versioned payload contracts;
- include correlation/job id;
- be idempotent for side effects;
- authenticate every non-private endpoint;
- classify failures;
- support bounded retry;
- return results through backend-owned state.

## Development safety

Local development should not invoke production workflows by accident.

Provider interface must support:
- fake/local adapter;
- explicit production n8n adapter.

Any real production invocation from development requires an intentional configuration switch and must be visibly marked.

## Dashboard

n8n internal execution noise is not the UX.
Backend translates relevant workflow/job state into operator-facing health and actionable exceptions.
