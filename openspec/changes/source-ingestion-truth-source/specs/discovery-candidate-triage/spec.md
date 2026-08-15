# Discovery Candidate Triage Specification

## ADDED Requirements

### Requirement: Distinct downstream continuation actions from Promoted candidate

The candidate triage and discovery views SHALL preserve the established domain meaning of `DiscoveryCandidate.Promoted` (the operator accepted the candidate for continuation into the editorial pipeline) and provide distinct downstream actions to: (1) start a new `ContentItem` from a promoted candidate, or (2) attach the promoted candidate's evidence to an existing active `ContentItem`. Downstream actions SHALL enforce idempotency to prevent creating accidental duplicate `ContentItem` records on retries or repeated UI actions.

#### Scenario: Initiate new ContentItem from promoted candidate
- **WHEN** an operator initiates a new ContentItem from a candidate in "Promoted" status
- **THEN** a new `ContentItem` is created for that channel with stage "DraftingEvidence"
- **AND** the candidate's immutable evidence snapshot is attached as `ContentItemEvidence` with role "PrimaryLead"
- **AND** the candidate remains in "Promoted" status
- **AND** subsequent attempts to start another ContentItem from the same candidate return the existing ContentItem ID or warn the operator, preventing duplicate creation.

#### Scenario: Attach promoted candidate evidence to existing ContentItem
- **WHEN** an operator selects "Attach to Existing ContentItem" on a promoted candidate and selects a target ContentItem ID
- **THEN** a new immutable `ContentItemEvidence` snapshot record is added to the target ContentItem with role "SupportingEvidence"
- **AND** an audit event is logged with action "ContentItem.EvidenceAttached".
