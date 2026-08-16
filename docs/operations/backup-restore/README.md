# Backup and Restore Runbooks (P9-WP03)

Phase marker: `P9-WP03-backup-and-restore`

These runbooks are for **non-production drills and environment-owned Production operations**. They contain no credentials. Scripts live in `ops/backup/`. CLI: `tools/ExItS.BackupRestore.Cli`.

**Role placeholder:** Backup Operator (environment-owned).

**Provisional engineering targets (not SLAs):** RPO = latest successful scheduled backup; Platform/POS restore-test RTO ≤ 30 min each; full MVP data service ≤ 60 min.

**PITR:** Explicitly **deferred**. Logical `pg_dump`/`pg_restore` only.

**Local MAUI SQLite:** Not part of server backup sets. Unsynced local ops cannot be reconstructed from PostgreSQL backups (release risk; R-129 governs encryption).

---

## 1. Scheduled backup

| Item | Detail |
|---|---|
| Prerequisites | Host has PostgreSQL client tools or Docker; `EXITS_*_DATABASE` set; writable output dir; schedule **disabled by default** (`ops/backup/schedules/`) |
| Commands | Cron/systemd calls `Backup-ExItsDatabase.ps1` per database, then `Verify-ExItsBackup.ps1` |
| Expected | Exit 0; `BackupSetId=...`; `VERIFY_OK` |
| Failure | Non-zero exit; incomplete artifacts must not be promoted |
| Rollback | N/A (read-only of live DB); delete incomplete set |
| Evidence | Manifest JSON (no secrets), duration, size, SHA-256, UTC timestamp |

## 2. Manual backup

Same as scheduled. Set `EnvironmentClassification` appropriately (`Development`/`Testing`/`Staging`/`Production`). Pass `--commit` Git SHA when known.

## 3. Artifact verification

```powershell
.\ops\backup\Verify-ExItsBackup.ps1 -ArtifactPath <dump> -ManifestPath <manifest.json>
```

Fails on checksum mismatch, incomplete status, or secret-like content in manifest text.

## 4. Retention cleanup

```powershell
.\ops\backup\Invoke-ExItsRetentionCleanup.ps1 -BackupDirectory <dir>           # dry-run
.\ops\backup\Invoke-ExItsRetentionCleanup.ps1 -BackupDirectory <dir> -ExecuteDeletes
```

Provisional: daily 14d / weekly 8w / monthly 12m. Never deletes latest complete backup. Dry-run default.

## 5. Platform-only restore

1. Provision **empty** disposable DB (or approved empty target).
2. Set `EXITS_PLATFORM_DATABASE`.
3. Verify artifact.
4. `Restore-ExItsDatabase.ps1 -DatabaseKind Platform ...` (add `-AllowDestructiveRestore -DestructiveConfirmation DESTROY_AND_RESTORE` only when overwriting approved non-empty).
5. Run `RestoreValidator.ValidatePlatformAsync` / integration drill tests.
6. **Do not** auto-point Production traffic.

## 6. POS-only restore

Same pattern with `PinoyBusinessPos` / `EXITS_POS_DATABASE`. Independent of Platform artifact.

## 7. Full MVP restore

Order:

1. Restore Platform
2. Restore POS
3. Validate schemas/migrations
4. Start APIs against restored environment
5. Integrity checks
6. Functional smoke
7. Cutover decision (manual; never automatic)

No cross-database FKs to repair.

## 8. Corrupted artifact handling

- Verification fails on SHA-256 mismatch → quarantine set; do not restore
- Select prior complete verified backup
- Log backup-set ID and failure UTC (no paths with secrets)

## 9. Failed restore handling

- Capture redacted error message and duration
- Target DB may be partial → rebuild empty target and retry from verified artifact
- Do not cut over
- Escalate to Backup Operator + App Owner

## 10. Credential / key handling

- Connection strings via env / `.pgpass` / secret store — never CLI argv in logs
- AES-256-GCM via `Protect-ExItsBackup.ps1` + `EXITS_BACKUP_KEY_FILE` (32 bytes); **keys never stored beside artifacts**
- Repo contains no real keys or production dumps
- Encryption-at-rest for off-host storage is a **production release requirement**

## 11. Post-restore validation

Structural (schemas, tables, migration history, indexes/constraints via restore, legacy product absence) + Platform/POS integrity + recalculated invariants where applicable. Mismatches **fail** validation — never silent repair.

Automated evidence: `dotnet test tests/ExItS.BackupRestore.Tests -c Release`

**P29-WP14 development drill** (local-validation → disposable restore containers): see [postgresql-backup-and-restore runbook](../../runbooks/postgresql-backup-and-restore.md) and `ops/backup/Invoke-ExItsP29Wp14DevRecoveryDrill.ps1`. Does **not** prove Production backup/restore.

## 12. Service cutover decision

Manual approval only after validation + smoke. Never auto-repoint Production. Rollback point = previous live databases/credentials retained until sign-off.

## 13. Incident evidence collection

Retain: backup-set IDs, manifests (safe metadata), timings, sizes, checksum results, restore duration, validation findings, operator identity/time, environment classification. Exclude: passwords, connection strings, dump contents, encryption keys.
