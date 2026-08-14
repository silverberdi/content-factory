# Audit and Seed Foundation Specification

## Purpose

Provides immutable security and mutation audit logging, reproducible development database seeding, and production-safe bootstrapping.

## Requirements

### Requirement: Auditable mutations

Identity/role/invitation/channel mutations in this change SHALL create audit records with actor, timestamp, action, target and correlation context.

#### Scenario: Channel update audit
- **WHEN** a TECHNICAL operator successfully edits a channel
- **THEN** a corresponding audit event exists and identifies the actor, target, timestamp, and mutation context.

### Requirement: Reproducible development seed

The change SHALL provide a documented repeatable mechanism to recreate required development seed data.

#### Scenario: Fresh development seed
- **WHEN** migrations and development seed execute on an empty content_factory_dev schema
- **THEN** canonical owner, roles/capabilities, IA Simple ES and representative dashboard test data exist
- **AND** the operator can execute the human test without manual database preparation.

### Requirement: Production seed safety

Production bootstrap SHALL NOT create fake operational content.

#### Scenario: Production initialization
- **WHEN** production migrations and bootstrap execute on an empty content_factory_prod schema
- **THEN** required system/owner/role defaults exist
- **AND** demo channel/attention records are not inserted unless explicitly marked canonical production defaults.
