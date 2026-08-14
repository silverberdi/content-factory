# Identity and Access Specification

## Purpose

Provides user authentication, role-based access control, Google Identity invitation activation, and SYSTEM_OWNER protections.

## Requirements

### Requirement: Development GOD authentication

The system SHALL provide a development-only authentication provider that signs the local developer in as the canonical SYSTEM_OWNER with TECHNICAL and EDITORIAL roles.

#### Scenario: Local development bypass
- **WHEN** backend environment is Development and AUTH_MODE is development-bypass
- **THEN** the application authenticates as silverio.bernal@gmail.com
- **AND** the principal has SYSTEM_OWNER protection
- **AND** TECHNICAL and EDITORIAL capabilities are available.

#### Scenario: Production bypass is rejected
- **WHEN** backend environment is Production and AUTH_MODE is development-bypass
- **THEN** startup fails immediately
- **AND** no HTTP application endpoint becomes usable.

### Requirement: Invitation-only Google identity

The production identity model SHALL permit application activation only for a Google identity matching a valid pending invitation, except the canonical owner bootstrap.

#### Scenario: Invited identity activates
- **WHEN** Google authenticates an email matching a valid pending invitation with EDITORIAL role
- **THEN** the application user is activated
- **AND** receives the invited role assignment
- **AND** the activation is audited.

#### Scenario: Uninvited identity denied
- **WHEN** Google authenticates an email with no valid pending invitation
- **THEN** application access is denied
- **AND** no active user is created.

### Requirement: Owner protection

The system SHALL treat silverio.bernal@gmail.com as protected SYSTEM_OWNER state.

#### Scenario: Technical user cannot remove owner
- **WHEN** a non-owner TECHNICAL user attempts to disable, delete, or remove protected owner access
- **THEN** the operation is rejected
- **AND** owner state remains unchanged.

### Requirement: Assignable roles

TECHNICAL users SHALL be able to assign TECHNICAL and/or EDITORIAL roles to non-owner users.

#### Scenario: Assign both roles
- **WHEN** an authorized TECHNICAL operator assigns both TECHNICAL and EDITORIAL roles to an active non-owner user
- **THEN** authorization reflects both capability sets
- **AND** the role change is audited.
