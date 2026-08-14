---
name: backup-drive-guardian
description: Use for database backup, MinIO archive, Google Drive off-site copies, retention, backup health, restore validation, or backup-related dashboard work.
---


# Required reading
docs/canonical/11_BACKUP_GOOGLE_DRIVE.md

# Rules
Backup is a product feature.
Google Drive is off-site storage, not runtime primary storage.
Every backup/archive is a Job with observable state.
Failure creates an Attention item.
Avoid undocumented manual-only backup procedures.
Include restore/integrity thinking.

