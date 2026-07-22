# Operations

MVP deployment is initiated by Silverio and executed through repository scripts by the assigned
operator. It must be idempotent, non-destructive, preserve data and configuration, avoid
`docker compose down -v`, avoid Prisma reset/db push, use `prisma migrate deploy` only after checks,
and finish with health and smoke tests.

Technical logs use structured JSON, rotation, one-to-seven-day retention, safe categories, and no
sensitive data. Durable operational events live in PostgreSQL.
