# Truth Source Evidence Specification

## MODIFIED Requirements

### Requirement: Downstream progression gate on Approved TruthSource

Only a `TruthSource` in "Approved" status SHALL be eligible for downstream continuation into subsequent editorial stages (including `ContentIdea` generation and manual idea creation). Any draft, under-review, rejected, or superseded TruthSource version SHALL strictly block idea generation and subsequent downstream progression.

#### Scenario: Unapproved TruthSource blocks downstream progression
- **WHEN** any system check verifies downstream eligibility for a ContentItem whose TruthSource is in "Draft", "UnderReview", or "Rejected" status
- **THEN** the system reports downstream progression as blocked, enforcing the canonical invariant "No downstream progression without approved TruthSource".

#### Scenario: Approved TruthSource unlocks idea generation
- **WHEN** a TruthSource reaches "Approved" status
- **THEN** downstream idea generation (`generate_ideas`) and manual idea creation are unlocked for that ContentItem.
