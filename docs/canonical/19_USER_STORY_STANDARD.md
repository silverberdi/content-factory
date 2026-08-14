# Canonical User Story Standard

Every implementation-ready story must include all applicable sections.

## Identity

ID  
Title  
Wave / Value Slice  
Business capability  
Actor

## Intent

User value  
Business reason  
Observable outcome

## Preconditions

Required state/permissions/data.

## Trigger

What starts the behavior.

## Main flow

Numbered user/system interaction.

## Alternative / exception flows

Include recoverable and blocking cases.

## Authorization

Required capability/role.
Negative cases.

## State transitions

Before → action → after.

## Data / audit

Entities created/changed.
Versioning.
Audit events.

## UX behavior

Entry point.
Dashboard impact.
Primary actions.
Dialog/drawer/page behavior.
Feedback.

## Responsive behavior

Desktop.
Tablet.
Mobile.

## Async behavior

Job creation/status if applicable.
Never leave unspecified long waits.

## Error / empty / loading

Explicit.

## API contract

Endpoints/commands/events affected.

## Non-goals

Explicitly forbidden scope.

## Acceptance scenarios

Given/When/Then, objective and testable.

## Automated tests

Required test classes.

## Human test script

Short reproducible verification including target viewport/device.

## Observability

Logs/metrics/audit/correlation expected.

## Dependencies

Only real dependencies.
