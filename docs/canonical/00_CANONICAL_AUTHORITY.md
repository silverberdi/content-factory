# Canonical Authority

## Status

Normative. This document governs interpretation of all other project artifacts.

## Rule of precedence

1. Business Intent
2. Product Vision
3. Security / Architecture / UX Constitutions
4. Domain Model and lifecycle invariants
5. Roadmap / Wave intent
6. User Stories and acceptance scenarios
7. OpenSpec proposal/specs/design/tasks
8. Implementation

A lower layer may add detail but MUST NOT change the intent of a higher layer.

## Sacred-context rule

Agents MUST NOT:
- silently reinterpret a canonical decision;
- replace a chosen technology because another is more familiar;
- invent product behavior to fill a gap;
- expand scope because a refactor appears convenient;
- weaken UX, security, audit, lineage or responsive requirements;
- create an alternative architecture inside a feature implementation.

When a genuine contradiction exists, stop planning that conflicting part and report:
1. the conflicting statements;
2. the affected change;
3. the smallest decision required.

Do not resolve contradictions through guesswork.

## Change discipline

OpenSpec is the execution contract.
A change may contain several specs when that creates one coherent, human-testable increment.
OpenSpec MUST accelerate delivery, not become a ceremony bottleneck.

Every change must maximize usable value while remaining reviewable.

## Definition of implementation success

A change is not complete because code compiles.
It is complete only when the global Definition of Done and its own acceptance criteria pass.
