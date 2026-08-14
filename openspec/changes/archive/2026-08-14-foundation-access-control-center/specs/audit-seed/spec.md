# Audit and Seed Foundation

## ADDED Requirements

### Requirement: Auditable mutations

Identity/role/invitation/channel mutations in this change SHALL create audit records with actor, timestamp, action, target and correlation context.

#### Scenario: Channel update audit
Given a TECHNICAL operator edits a channel
When the mutation succeeds
Then a corresponding audit event exists and identifies the actor and target.

### Requirement: Reproducible development seed

The change SHALL provide a documented repeatable mechanism to recreate required development seed data.

#### Scenario: Fresh development seed
Given an empty content_factory_dev schema
When migrations and development seed run
Then canonical owner, roles/capabilities, IA Simple ES and representative dashboard test data exist
And the operator can execute the human test without manual database preparation.

### Requirement: Production seed safety

Production bootstrap SHALL NOT create fake operational content.

#### Scenario: Production initialization
Given an empty content_factory_prod schema
When production migrations/bootstrap run
Then required system/owner/role defaults exist
And demo channel/attention records are not inserted unless explicitly marked canonical production defaults.
