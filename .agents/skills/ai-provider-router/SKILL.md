---
name: ai-provider-router
description: Use for AI capability routing, model providers, AI recommendations, prompt/model configuration, budgets, or AI observability.
---


# Required reading
docs/canonical/09_AI_ROUTING_AND_REASONING.md

# Rules
- capability domain, provider configuration;
- Provider is configurable and never hard-coded;
- routing precedence channel > capability > global;
- seed working defaults;
- retain provider/model/policy/cost/latency metadata;
- no private chain-of-thought storage;
- support future provider adapters without domain rewrites.

