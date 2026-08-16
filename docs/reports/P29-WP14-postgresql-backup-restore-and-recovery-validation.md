# P29-WP14 — PostgreSQL Backup, Restore & Recovery Procedure Validation

| Field | Value |
|---|---|
| Repository | `apps-eduard/ExItS-SaaS` |
| Branch | `main` |
| Starting HEAD / origin/main | `2dfb95da7ea572ede52d867938e789a59a201676` |
| Status | **Code Complete / Validation Evidence Recorded** |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Backup/Restore Proven | **No** |
| Production Ready | **No** |
| Production Payment Ready | **No** |

Related: [runbook](../runbooks/postgresql-backup-and-restore.md) · [WP09](P29-WP09-migration-backup-restore-and-db-operations.md) · [ops/backup](../../ops/backup/README.md) · [phase 29](../phases/phase-29-data-integrity-query-performance-and-database-hardening.md)

## Goal

Prove **development** recoverability for independent Platform and PinoyBusinessPOS PostgreSQL databases:

backup → clean PostgreSQL restore → migrate if needed → integrity validate → ExItS (EF) read → safe write smoke → documented repeatable procedure.

Does **not** claim Production Backup/Restore Proven or Production Ready (no production environment yet).

## Environment

| Item | Value |
|---|---|
| Host | Windows + Docker Desktop |
| PostgreSQL image | `postgres:16` |
| Source Platform | `exits-local-validation-platform-db` → host **15533**, DB `exits_platform` |
| Source POS | `exits-local-validation-pos-db` → host **15534**, DB `exits_pos` |
| Credentials | `deploy/docker/.env.local-validation` (`LOCAL_VALIDATION_*`) — **not committed** |
| Restore target | Disposable `exits-p29-wp14-restore-*` containers (preferred) + Testcontainers in CI/tests |
| Source DBs destroyed | **No** |

## Delivered

| Area | Change |
|---|---|
| Library | `RestoreValidator` — Phase 29 tables, named constraints (best-effort/require), inventory reservation invariants, critical fingerprints |
| Tests | `P29Wp14BackupRestoreRecoveryTests` A–D on `postgres:16` |
| CLI / scripts | `--docker-container` / `-DockerContainerId`; `Invoke-ExItsP29Wp14DevRecoveryDrill.ps1` |
| Runbook | [postgresql-backup-and-restore.md](../runbooks/postgresql-backup-and-restore.md) |

## Explicit exclusions

- Phase 30
- Production schedule / retention / off-site / encryption / RPO / RTO claims
- Destroying `exits-local-validation-*-db`
- Committing dump files or passwords
- Frontend / payment provider redesign
- HTTP API host smoke (validation uses EF Core Infrastructure + SQL against application schemas)
- PITR / WAL archiving

## Evidence table

| Check | Result |
|---|---|
| Platform backup created | **Yes** (Testcontainers + live drill) |
| POS backup created | **Yes** |
| Backup format | PostgreSQL custom (`pg_dump -Fc`) + SHA-256 manifest |
| Archive integrity (`VerifyArtifact` / sizes / optional `pg_restore -l`) | **Yes** |
| Platform clean restore (separate container/DB) | **Yes** |
| POS clean restore | **Yes** |
| Current backup → current restore | **Yes** (test A) |
| Older POS dump → restore → migrate latest | **Yes** (test B; pre-`HardenElectronicSalePaymentReservation`) |
| Row counts match expected fixture | **Yes** |
| Critical fingerprints match | **Yes** |
| Tenant org separation preserved | **Yes** |
| Phase 29 constraints present after restore | **Yes** (named objects required on latest) |
| Inventory reservation invariants | **Yes** (`reserved ≥ 0`, `reserved ≤ on_hand` where validated) |
| EF readback Platform (CanConnect + Migrate no-op + org/branch/policy) | **Yes** |
| EF readback POS (sale/order/inventory/payment attempt) | **Yes** |
| Post-restore write smoke (new GUID customer) | **Yes** (test C) |
| Sequence/identity collisions | **N/A for primary keys** (GUID identities); write smoke succeeded |
| Live disposable-container drill | **Yes** (CLI/Docker equivalent **PASS**; PS7 wrapper requires `pwsh`) |
| Production Backup/Restore Proven | **No** |

## Automated tests

| Suite | Filter | Result |
|---|---|---|
| `ExItS.BackupRestore.Tests` | `FullyQualifiedName~P29Wp14` | **PASS** — Failed 0 / Passed 4 / Skipped 0 (Release; ~21–23s) |

### Coverage

| Test | Intent |
|---|---|
| A | Latest Platform+POS backup → clean restore; row counts; fingerprints; Phase 29 constraints; tenant orgs; inventory invariants; EF readback |
| B | Older POS dump (`20260816121841_StrengthenCustomerOrderLineTenantForeignKeys`) → restore → migrate latest; `sales.stock_reservation_state` appears; rows preserved |
| C | Post-restore GUID customer insert smoke |
| D | Artifact size > 0 + `VerifyArtifactAsync`; optional `pg_restore -l` via Docker |

## Live drill (development)

| Item | Result |
|---|---|
| Script | `ops/backup/Invoke-ExItsP29Wp14DevRecoveryDrill.ps1` (**PowerShell 7+** — shared with other `ops/backup/*.ps1`) |
| Wrapper on Windows PS 5.1 without `pwsh` | **SKIPPED** (documented; `#Requires -Version 7.0`) |
| Equivalent CLI/Docker drill | **PASS** |
| Backup sizes (approx, redacted) | Platform ~506 188 bytes; POS ~267 066 bytes; both `VERIFY_OK` |
| Post-restore inline | `platform` + `pos` schemas present; legacy product/patient tables = 0 |
| Cleanup | Disposable restore containers removed; source local-validation left running |
| Artifacts | `ops/backup/local/p29-wp14-*` (gitignored) |

## Migration recovery

| Scenario | Proven |
|---|---|
| Current → restore (schema already current) | **Yes** |
| Older POS → restore → EF migrate to HEAD | **Yes** (meaningful Phase 29 predecessor migration) |

## Production future work (not done)

Automated schedule, retention, encryption at rest, off-site/immutable storage, secrets management, monitoring/alerts, periodic restore drills, RPO/RTO, cutover ownership — deferred to Phase 14 / production ops. **Production Backup/Restore Proven = No.**

## Builds

| Scope | Result |
|---|---|
| `ExItS.BackupRestore` + Platform/POS Infrastructure (via BackupRestore.Tests restore graph) | Release build **PASS** |
| MAUI / Device | **Not in scope** — Device Verified = **No** |

## Commits

| Commit | Message |
|---|---|
| `4b5de978` | `test(p29): add PostgreSQL backup restore recovery validation` |
| `1c3361bb` | `ops(db): add repeatable development backup restore tooling` |
| `adeb2835` | `docs(db): add PostgreSQL recovery runbook` |
| `852ab186` | `docs(p29): record WP14 recovery validation evidence` |
| `71ac3f75` | `docs(p29): stamp WP14 commit hashes` |

## Residual / exact next

- Keep **Production Backup/Restore Proven = No**
- Optional: install PowerShell 7 for one-command wrapper on Windows hosts without `pwsh`
- Broader WP08 load harness residual unchanged
- Do **not** open Phase 30; Phase 29 remains **Open / Partial Closeout**
