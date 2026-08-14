---
name: openspec-change-author
description: Use when creating, updating, reviewing, or decomposing OpenSpec changes/specs/tasks for Content Factory; enforces value-oriented changes and precise acceptance scenarios.
---


# Goal
Produce OpenSpec artifacts that leave minimal interpretation to the implementing agent.

# Required artifact content
Proposal:
- observable user/business outcome;
- included capabilities;
- non-goals;
- risk/rollback consideration.

Specs:
- normative requirements;
- Given/When/Then scenarios;
- authorization;
- state transitions;
- UX/dashboard behavior;
- responsive behavior;
- async/error behavior.

Design:
- domain boundaries;
- API/data changes;
- security;
- frontend composition;
- migrations;
- observability;
- alternatives intentionally rejected.

Tasks:
- coherent order;
- tests embedded with implementation;
- seed data;
- human test evidence;
- documentation sync.

# Value rule
Prefer one coherent change with several specs over tiny entity-by-entity changes when the larger change remains human-testable and reviewable.

# Non-goal
Do not make OpenSpec ceremony itself the deliverable.

