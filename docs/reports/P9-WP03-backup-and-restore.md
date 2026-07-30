# P9-WP03 — Backup and Restore

Phase marker: `P9-WP03-backup-and-restore`

## Status

**Complete with documented risks.** Delivered independent Platform and PinoyBusinessPOS PostgreSQL logical backup/restore with manifests, SHA-256 verification, restore safeguards, retention dry-run, encryption integration point, runbooks, and Testcontainers recovery drills proving **recoverability**. **No new business features.** **P9-WP01 security and P9-WP02 health/reliability preserved.** **Not production-ready** while R-091, R-109, R-129, and related blockers remain open. **P9-WP04 was not started.**

Feature commit: 3bbb0c716da60bd7d87a191c35bd0eced1bde380

## Delivered

| Area | Delivered |
|---|---|
| Library | `ExItS.BackupRestore` — manifest, checksum, redaction, retention, `pg_dump`/`pg_restore`, AES-256-GCM protect, restore validation |
| CLI / ops | `tools/ExItS.BackupRestore.Cli`; `ops/backup/*.ps1`; schedules disabled by default |
| Artifacts | Separate custom-format dump + safe manifest per database; refuse silent overwrite |
| Restore safety | Refuse non-empty restore without `DESTROY_AND_RESTORE`; checksum + kind checks |
| Retention | Provisional 14d/8w/12m; dry-run default; never delete latest complete |
| Encryption | Optional AES-256-GCM envelope; keys via file env; no keys beside artifacts; off-host encryption is production requirement |
| Drill | Testcontainers Platform + POS backup → empty-target restore → structural validation |
| Docs | Runbooks under `docs/operations/backup-restore/`; this report |
| PITR | **Explicitly deferred** (no WAL archiving claimed) |
| Local/offline | SQLite/SecureStorage **not** in server backups; unsynced local loss documented |

## Backup architecture

- PostgreSQL-native logical backups (`pg_dump -Fc`) per database
- Independent restore order: Platform → POS → validate → start services → smoke → manual cutover
- No cross-database FKs; HealthCare excluded
- Passwords via `PGPASSWORD` env / Docker `-e`; not argv logs
- UTC timestamps; unique backup-set IDs

## Provisional RPO/RTO (not SLAs)

| Objective | Target | Drill evidence |
|---|---|---|
| RPO | Latest successfully completed scheduled backup | Logical dump at drill time |
| Platform restore-test RTO | 30 min | Testcontainers restore completed in seconds (dev volume) |
| POS restore-test RTO | 30 min | Same |
| Complete MVP data service | 60 min | Not a Production cutover drill |

Measured artifact sizes/durations are environment- and data-volume-specific; full MVP volumes were **not** seeded in this drill. Targets met for the disposable Testcontainers scenario only.

## Recovery drill results

Automated: `PosBackupRestoreDrillTests`, `PlatformBackupRestoreDrillTests`.

1. Migrate + seed representative rows (org / customer + idempotency)
2. Backup with Docker `pg_dump` inside container
3. Verify manifest + SHA-256
4. Reject corrupt artifact and wrong-kind manifest
5. Refuse restore over non-empty without confirmation
6. Restore into **empty** disposable peer database
7. Validate schema, migration history, required tables, HealthCare absence, row-count sanity

APIs were not auto-cutover against restored DBs in CI (no Production traffic). Smoke against restored DBs remains operator-runbook step.

## Encryption status

| Item | Status |
|---|---|
| In-repo dumps | None (gitignore + architecture guard) |
| AES-256-GCM helper | Implemented (`Protect-ExItsBackup.ps1` / CLI `encrypt`) |
| Production encryption-at-rest | **Required** before off-host storage; not claimed delivered for Production |
| Key storage | Never beside backup; `EXITS_BACKUP_KEY_FILE` |

## Retention policy (provisional)

- Daily: 14 days
- Weekly: 8 weeks (Sunday UTC keeps)
- Monthly: 12 months (1st UTC keeps)
- Incomplete: not promotable; eligible for cleanup
- Latest complete: never deleted

## Local / offline limitations (release risk)

- Device SQLite is cache/queue — not authoritative backup source
- Reinstall/device loss may lose **unsynced** local operations
- Server-confirmed data recoverable from PostgreSQL only
- No automatic local SQLite restore across users/orgs/devices
- Full-database local encryption remains R-129

## Health / monitoring

Backup freshness is an **operational readiness signal**, not application liveness. `/health` and `/health/ready` behavior from P9-WP02 unchanged. Documented signals for operators: last successful backup UTC, last verified UTC, age, verification status, last restore-test UTC (environment-owned recording). Do not expose storage credentials or dump contents.

## Explicit exclusions

- New business features; HealthCare changes
- Combined non-independently-restorable dump
- Committed dumps/secrets/keys
- Production DR/PITR claims beyond tested scenarios
- External paid backup services
- Mobile SQLite as server backup
- P9-WP04 or later

## Build / test evidence

| Check | Result |
|---|---|
| `dotnet build ExItS.slnx -c Release` | Succeeded (0 errors; known NU1903/NU1510 warnings retained) |
| `dotnet test ExItS.slnx -c Release` | **931 / 0 / 0** (baseline 915) |
| `ExItS.BackupRestore.Tests` | 10 / 0 / 0 |
| Android Release (`net10.0-android`) | Succeeded (NU1903 warnings retained as R-129) |

## Security

No secrets in manifests or committed artifacts. Connection strings not logged. Destructive restore requires explicit token. P9-WP01 Production guards preserved.

## HealthCare freeze

Unchanged: ignored, untracked, outside `ExItS.slnx`.

## Unresolved risks / release blockers

- R-091 production auth
- R-109 interactive Android validation
- R-129 SQLCipher / local DB encryption
- Production off-host encrypted backup storage + scheduling not environment-provisioned here
- PITR deferred
- Unsynced MAUI local data not recoverable from server backups
- Full MVP data-volume restore not measured in this environment

## Exact next work package

**P9-WP04 — Accessibility, Localization and Theme QA** (do not begin until authorized)

## Files / docs changed

See Git feature and documentation commits for this WP.

## Git evidence

| Item | Value |
|---|---|
| Feature commit | 3bbb0c716da60bd7d87a191c35bd0eced1bde380 |
| Docs commit | _(recorded after docs commit)_ |
| Tests | 931 / 0 / 0 |
| Exact next WP | **P9-WP04 — Accessibility, Localization and Theme QA** |
