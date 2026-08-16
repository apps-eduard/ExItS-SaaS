# PostgreSQL Backup and Restore (ExItS)

Non-production drills and environment-owned Production operations. **No credentials** in this document.

Scripts: `ops/backup/`. Library: `src/Shared/ExItS.BackupRestore`. CLI: `tools/ExItS.BackupRestore.Cli`.

Related: [operations/backup-restore README](../operations/backup-restore/README.md) (P9-WP03) · [P29-WP14 report](../reports/P29-WP14-postgresql-backup-restore-and-recovery-validation.md).

## PURPOSE

Recover **Platform** and **PinoyBusinessPOS** PostgreSQL databases independently from logical backups (`pg_dump -Fc`).

## AUTHORITY BOUNDARIES

- Platform and POS are separate database authorities
- Restore each independently — **no** cross-database FKs or joins
- Local MAUI SQLite is **not** in server backup sets
- **Production Backup/Restore Proven = No** until Phase 14 production exit criteria are met

## PREREQUISITES

| Item | Notes |
|---|---|
| PostgreSQL | Prefer same major as source (dev drills use `postgres:16`) |
| Docker Desktop | Required for disposable restore containers / Docker `pg_dump` path |
| Tools | `pg_dump` / `pg_restore` / `psql` **or** Docker exec into containers |
| PowerShell | **7+** (`pwsh`) for `ops/backup/*.ps1` (`#Requires -Version 7.0`) |
| Env vars | Connection strings or `LOCAL_VALIDATION_*` from `.env.local-validation` — never commit |
| Disk | Space for dump artifacts (gitignored under `ops/backup/local/`) |

Placeholders (never commit secrets):

```text
$PLATFORM_CONNECTION_STRING
$POS_CONNECTION_STRING
$PGHOST / $PGPORT / $PGUSER / $PGPASSWORD
```

## BACKUP

### Pattern (Docker container path — preferred on Windows without host client tools)

```powershell
pwsh -File ops/backup/Backup-ExItsDatabase.ps1 `
  -DatabaseKind Platform `
  -ConnectionString $PLATFORM_CONNECTION_STRING `
  -OutputDirectory ops/backup/local `
  -EnvironmentClassification Testing `
  -DockerContainerId exits-local-validation-platform-db

pwsh -File ops/backup/Backup-ExItsDatabase.ps1 `
  -DatabaseKind PinoyBusinessPos `
  -ConnectionString $POS_CONNECTION_STRING `
  -OutputDirectory ops/backup/local `
  -EnvironmentClassification Testing `
  -DockerContainerId exits-local-validation-pos-db
```

### Artifact naming

```text
platform_YYYYMMDDTHHMMSSZ_{guid}.dump + .manifest.json
pos_YYYYMMDDTHHMMSSZ_{guid}.dump + .manifest.json
```

### Verification

```powershell
pwsh -File ops/backup/Verify-ExItsBackup.ps1 -ArtifactPath <dump> -ManifestPath <manifest.json>
```

Expect exit 0 / `VERIFY_OK`; non-empty dump; optional `pg_restore -l` TOC inspect.

## RESTORE

1. Create a **clean** target (new disposable PostgreSQL container or empty database) — do **not** restore over the only healthy source as the first step
2. Restore Platform and POS independently
3. Point application connection strings at the restore target
4. Run EF migrations if recovering an older dump (`dotnet ef database update` / `Database.MigrateAsync`)
5. Validate before any cutover

Refuse source == target where tooling detects it. Scripts accept `-DockerContainerId` / `--docker-container` for container-local restore.

## VALIDATION

| Layer | Checks |
|---|---|
| Archive | Size > 0, checksum/manifest, optional `pg_restore -l` |
| Schema | `platform` / `pos` schemas; `__EFMigrationsHistory` |
| Data | Expected row counts + critical fingerprints |
| Constraints | Phase 29 CHECKs / composite FKs / indexes (see RestoreValidator) |
| Tenant | Organization IDs preserved; no cross-org bleed |
| Application | EF CanConnect + Migrate no-op + known-record reads |
| Write smoke | Harmless insert (e.g. test customer) on **restore target only** |

Automated:

```powershell
dotnet test tests/ExItS.BackupRestore.Tests -c Release --filter FullyQualifiedName~P29Wp14
```

## Development recovery drill (P29-WP14)

Validates recoverability against **local-validation** sources without destroying them.

```powershell
pwsh -File ops/backup/Invoke-ExItsP29Wp14DevRecoveryDrill.ps1
```

| Switch | Meaning |
|---|---|
| `-SkipCleanup` | Leave disposable restore containers running |
| `-SkipDotnetTest` | Skip automated suite (inline psql checks still run) |
| `-KeepDumps` | Keep dumps under gitignored `ops/backup/local/` |
| `-PlatformConnectionString` / `-PosConnectionString` | Override env-built CS |

Flow: load env → backup Platform+POS → verify → start `exits-p29-wp14-restore-*` → restore → schema checks → optional `P29Wp14` tests → remove disposable containers.

## FAILURE HANDLING

| Failure | Action |
|---|---|
| Backup non-zero / empty dump | Do not promote; fix source connectivity/tools; retry |
| Verify checksum/manifest fail | Treat as corrupt; re-backup |
| Restore fail | Keep original backup; fix target emptiness/permissions; retry on clean target |
| Migration fail after older restore | Do not cut over; inspect migration history; restore again on fresh target |
| Integrity / fingerprint mismatch | Fail closed — do not use restored DB |
| Application/EF smoke fail | Fail closed — investigate schema/data before cutover |

## SAFETY

- Never overwrite the only healthy DB as the first recovery step
- Prefer isolated restore targets; verify before cutover
- Preserve original backup artifacts
- Never print passwords; redact connection strings in logs
- Do not commit `*.dump` or `ops/backup/local/`

## PRODUCTION FUTURE WORK (not implemented)

- Automated backup schedule and monitoring/alerts
- Retention policy with proven dry-run/delete gates
- Encryption at rest and key custody
- Off-site / immutable / versioned storage
- Secrets management
- Periodic restore drills with evidence
- RPO / RTO targets owned by operations
- Production cutover procedure and incident ownership

Until those exist and are rehearsed: **Production Backup/Restore Proven = No**.
