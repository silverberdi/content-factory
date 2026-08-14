# Identity and Access

## ADDED Requirements

### Requirement: Development GOD authentication

The system SHALL provide a development-only authentication provider that signs the local developer in as the canonical SYSTEM_OWNER with TECHNICAL and EDITORIAL roles.

#### Scenario: Local development bypass
Given the backend environment is Development
And AUTH_MODE is development-bypass
When the operator opens the application
Then the application authenticates as silverio.bernal@gmail.com
And the principal has SYSTEM_OWNER protection
And TECHNICAL and EDITORIAL capabilities are available.

#### Scenario: Production bypass is rejected
Given the backend environment is Production
And AUTH_MODE is development-bypass
When the backend starts
Then startup fails
And no HTTP application endpoint becomes usable.

### Requirement: Invitation-only Google identity

The production identity model SHALL permit application activation only for a Google identity matching a valid pending invitation, except the canonical owner bootstrap.

#### Scenario: Invited identity activates
Given a TECHNICAL user invited user@example.com with EDITORIAL role
And the invitation is valid
When Google authenticates exactly user@example.com
Then the application user is activated
And receives the invited role assignment
And the activation is audited.

#### Scenario: Uninvited identity denied
Given no valid invitation exists for unknown@example.com
When Google authenticates unknown@example.com
Then application access is denied
And no active user is created.

### Requirement: Owner protection

The system SHALL treat silverio.bernal@gmail.com as protected SYSTEM_OWNER state.

#### Scenario: Technical user cannot remove owner
Given a TECHNICAL user other than the owner
When that user attempts to disable, delete, or remove protected owner access
Then the operation is rejected
And owner state is unchanged.

### Requirement: Assignable roles

TECHNICAL users SHALL be able to assign TECHNICAL and/or EDITORIAL roles to non-owner users.

#### Scenario: Assign both roles
Given an active non-owner user
And an authorized TECHNICAL operator
When both roles are assigned
Then authorization reflects both capability sets
And the role change is audited.
