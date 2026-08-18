---
name: backup-drive-guardian
description: Use for database backup, MinIO archive, Google Drive off-site copies, retention, backup health, restore validation, or backup-related dashboard work.
---


# Required reading
docs/canonical/11_BACKUP_GOOGLE_DRIVE.md

# Rules
Backup is a product feature.
Target database is canonical PostgreSQL (`content_factory_dev` / `content_factory_prod`).
Google Drive is off-site storage, not runtime primary storage.
Backups must use PostgreSQL backup tooling (`pg_dump`) and restoration validation (`pg_restore` / `psql`).
Every backup/archive is a Job with observable state.
Failure creates an Attention item.
Avoid undocumented manual-only backup procedures.
Include restore/integrity thinking.
