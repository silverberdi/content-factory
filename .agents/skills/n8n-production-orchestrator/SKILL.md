---
name: n8n-production-orchestrator
description: Use for any n8n workflow, webhook, callback, orchestration, long-running external operation, or development-to-n8n integration.
---


# Required reading
docs/canonical/10_N8N_ORCHESTRATION.md

# Hard rule
There is one production workflow set only.

# Development
Default to fake adapter.
Real n8n invocation from local development must be explicit and safe.

# Contracts
Every workflow interaction has:
- version;
- job/correlation id;
- authentication;
- idempotency for side effects;
- bounded retries;
- classified result/error.

Do not move domain authority into n8n.

