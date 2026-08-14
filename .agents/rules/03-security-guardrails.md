# Security Guardrails
Never commit credentials.
Never expose MySQL, MinIO or n8n merely to simplify development.
Never permit development-bypass authentication in production.
Authorization belongs in backend policy/capability checks.
SYSTEM_OWNER protections are invariants.
