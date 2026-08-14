# Identity and Security Constitution

## Users

Initial owner:
`silverio.bernal@gmail.com`

Owner property:
`SYSTEM_OWNER`

SYSTEM_OWNER is protected system state, not an assignable role.

## Roles

`TECHNICAL`
`EDITORIAL`

A user may hold either or both.

### EDITORIAL capabilities

At minimum:
- review/approve videos;
- approve publication;
- execute/register publication;
- view costs;
- perform editorial review actions assigned by capability.

### TECHNICAL capabilities

At minimum:
- manage channels/configuration;
- manage provider routing;
- manage invitations/users/roles;
- manage infrastructure-facing configuration;
- inspect operational failures;
- all technical administration.

Capabilities should be explicit. Avoid scattered `if role == ...` authorization.

## Owner protection

No application user may:
- delete SYSTEM_OWNER;
- disable SYSTEM_OWNER;
- change the protected owner identity;
- remove owner access through role administration.

TECHNICAL may change roles of every other account.

## Invitation model

- no open registration;
- TECHNICAL invites a Google email;
- invitation includes intended roles;
- invitation is pending and expires;
- account activates only after that exact Google identity authenticates;
- invitations are auditable and revocable.

## Authentication

Production: Google OAuth/OIDC.

Development: explicit development-only bypass ("GOD mode") for local macOS development.

Development bypass MUST:
- authenticate as SYSTEM_OWNER + TECHNICAL + EDITORIAL;
- be implemented as a dedicated auth provider;
- be allowed only when environment is Development;
- cause production startup to fail if configured in Production.

## Break-glass recovery

No web backdoor.
Recovery occurs only from authorized server/CLI access.

Recovery tooling may:
- restore owner access;
- restore roles;
- revoke sessions;
- disable non-owner users.

Every action logs an audit event.

## Exposure

Public:
- PWA frontend through Cloudflare Tunnel;
- backend endpoints required by the PWA through Cloudflare Tunnel;
- authenticated inbound callbacks only when explicitly designed.

Not public:
- MySQL;
- MinIO;
- n8n management UI/webhooks unless a specific authenticated endpoint is required;
- internal service ports.

## Secrets

Initial development secrets may be supplied via uncommitted `.env`.
No secret in Git, OpenSpec artifacts, agent prompts or exported n8n JSON.
Architecture must permit later migration to a dedicated secret store without business-code changes.
