# Spec Delta

## ADDED Requirements

### Requirement: Editorial Studio shell access

The authenticated application shell SHALL provide one discreet, recognizable navigation access to the independent Editorial Studio at `/editorial/studio), without removing, renaming, or changing existing navigation entries.

#### Scenario: Authenticated operator sees Editorial Studio access
- **WHEN** an authenticated operator opens the desktop sidebar or mobile navigation
- **THEN** an Editorial Studio entry is visible and navigates to `/editorial/studio`
- **AND** the existing Discovery, Content, and specialized-studio entries remain available and unchanged.

#### Scenario: Existing shell routes remain regression-safe
- **WHEN** an operator uses any pre-existing shell navigation entry after the Editorial Studio access is added
- **THEN** the original destination and authorization behavior remain unchanged
- **AND** no existing entry is redirected through the Editorial Studio.

