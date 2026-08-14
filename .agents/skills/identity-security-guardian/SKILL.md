---
name: identity-security-guardian
description: Use for authentication, authorization, invitations, users, roles, owner protection, public endpoints, secrets, Cloudflare exposure, or security-sensitive Content Factory changes.
---


# Required reading
docs/canonical/04_IDENTITY_SECURITY.md
docs/canonical/13_ENVIRONMENTS_DEPLOYMENT.md

# Invariants
- SYSTEM_OWNER is protected.
- production Google auth only.
- invitation-only access.
- development bypass is a separate provider.
- production refuses development bypass.
- authorization enforced backend-side.
- MySQL, MinIO and n8n remain private.
- no secret in code/repo/OpenSpec.

# Testing
Every allowed behavior needs the meaningful denied counterpart.
Security tests are outcome-based, not implementation-detail-only.

