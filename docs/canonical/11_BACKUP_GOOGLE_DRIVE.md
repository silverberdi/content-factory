# Backup and Google Drive Archive

## Product requirement

Backup/archive is a product feature, not a manual checklist item.

Google Drive is the off-site destination for:
- application database backups;
- critical configuration/documentation backup packages;
- selected retained files copied outside MinIO;
- published-content archive policy where configured.

MinIO remains primary runtime object storage.

## Job types

- database-backup
- minio-archive-copy
- critical-config-backup
- restore-validation (later / scheduled policy)

## Requirements

Each run records:
- job id;
- type;
- start/end;
- source scope;
- destination;
- file count/size;
- checksum/integrity result where applicable;
- status;
- failure class;
- retry count;
- retention metadata.

## Scheduling

The system must support scheduled backups.
Critical backup triggers may also be event-driven.

Do not rely on an undocumented cron entry as the only implementation.

## UX

Dashboard health should compactly show last successful backup.
A healthy backup consumes minimal emphasis.
Failure becomes an Attention item.

Provide operator ability to:
- inspect backup history;
- understand failure reason;
- retry when safe;
- initiate an authorized manual backup.

## Retention

Published lineage artifacts are preserved according to archival policy.
Rejected/intermediate assets may have configurable 30/60/90-day retention later.

## Restore

A backup is not considered trustworthy merely because upload succeeded.
Document restoration and add periodic restoration/integrity validation as the platform matures.
