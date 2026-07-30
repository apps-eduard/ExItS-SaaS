# ExItS Backup & Restore (P9-WP03)

Operational tooling for **independent** Platform and PinoyBusinessPOS PostgreSQL logical backups.

## Provisional objectives (not SLAs)

| Objective | Target |
|---|---|
| RPO | Latest successfully completed scheduled backup |
| Platform restore-test RTO | 30 minutes |
| POS restore-test RTO | 30 minutes |
| Complete MVP data service RTO | 60 minutes |

## Scripts

| Script | Purpose |
|---|---|
| `Backup-ExItsDatabase.ps1` | Create `pg_dump` custom-format artifact + manifest |
| `Verify-ExItsBackup.ps1` | SHA-256 + manifest Completeness check |
| `Restore-ExItsDatabase.ps1` | Restore with non-empty DB protection |
| `Invoke-ExItsRetentionCleanup.ps1` | Retention decisions (**dry-run by default**) |
| `Protect-ExItsBackup.ps1` | AES-256-GCM envelope using key file (key never beside artifact) |
| `Invoke-ExItsRecoveryDrill.ps1` | Orchestrates backup → verify → restore guidance |

Connection secrets must come from environment variables (`EXITS_PLATFORM_DATABASE`, `EXITS_POS_DATABASE`) or a `.pgpass` file — never committed.

## Artifact layout

```text
{outputDir}/
  platform_YYYYMMDDTHHMMSSZ_{guid}.dump
  platform_YYYYMMDDTHHMMSSZ_{guid}.manifest.json
  pos_YYYYMMDDTHHMMSSZ_{guid}.dump
  pos_YYYYMMDDTHHMMSSZ_{guid}.manifest.json
```

Manifests never contain passwords, connection strings, tokens, or row payloads.

## Safety

- Separate artifact per database (independently restorable)
- Refuse overwrite of existing artifact/manifest
- Refuse restore into non-empty DB unless `-AllowDestructiveRestore -DestructiveConfirmation DESTROY_AND_RESTORE`
- Retention never deletes the latest complete backup; dry-run is default
- Do not commit dump files
- Local MAUI SQLite is **not** part of server backup sets

## PITR

Point-in-time recovery (WAL archiving) is **explicitly deferred** for P9-WP03. Logical dump/restore is the MVP path.

## Schedules

Sample timer definitions under `schedules/` are **disabled by default**.
