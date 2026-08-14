---
name: canonical-context-guard
description: Use before planning or implementing any Content Factory feature to resolve authoritative project context and prevent architecture, UX, security, or product drift.
---


# Goal
Load the minimum complete canonical context for the requested task.

# Instructions
1. Read `docs/canonical/00_CANONICAL_AUTHORITY.md`.
2. Identify active OpenSpec change.
3. Map change concerns to canonical files.
4. State the files read in the implementation plan.
5. Extract hard constraints and non-goals.
6. If artifacts conflict, stop the conflicting portion and report it.

# Constraints
- Do not use stale/legacy project documents as authority.
- Do not fill product gaps with generic SaaS conventions.
- Do not reinterpret a canonical requirement because a library defaults differently.

