# Development Governance

## Delivery chain

Roadmap → Wave → Value Slice → User Stories → OpenSpec Change → Tasks → Implementation → Verify → Human Test → Review → Archive.

A change may implement several specs/user stories when they form one coherent human-testable increment.

## Agent ownership

ChatGPT:
- canonical product/architecture context author;
- roadmap/backlog/US author;
- OpenSpec context/specification author;
- Antigravity execution context author.

Antigravity:
- primary implementer;
- must obey OpenSpec + canonical context;
- does not redesign product architecture while implementing.

DeepSeek:
- alternate reasoning provider for the product;
- independent code/spec reviewer when development changes are reviewed.

## Plan gate

Before implementation Antigravity must explicitly identify:
- canonical documents read;
- scope/non-goals;
- affected domain states;
- security boundaries;
- UX/dashboard impact;
- responsive behavior;
- migration/data impact;
- test strategy.

If the plan contradicts OpenSpec/canonical context, do not proceed.

## Scope control

Every change must state non-goals.
Out-of-scope opportunities are reported, not implemented silently.

## No speculative infrastructure

Do not add infrastructure "for future scale" unless the current change requires it.

## Documentation synchronization

A change that modifies a canonical decision must include explicit canonical-doc update authorized by the change.
Implementation cannot silently make documentation obsolete.
