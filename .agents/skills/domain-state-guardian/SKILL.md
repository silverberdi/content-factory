---
name: domain-state-guardian
description: Use when modifying ContentItem, Source, TruthSource, ContentIdea, Script, EditorialTask, Job, Publication, metrics, versions, lineage, state transitions, or persistence.
---


# Required reading
docs/canonical/03_DOMAIN_MODEL_V2.md

# Invariants
- backend authoritative;
- published lineage immutable;
- edits versioned;
- multi-source provenance;
- approval gates enforced;
- rejection reasons;
- optimistic concurrency;
- audit attribution.

# Constraint
Do not let database schema convenience redefine domain behavior.

