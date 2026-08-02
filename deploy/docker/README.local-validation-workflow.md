# Local Validation — local development (Docker DBs + host apps)

Personal Staging preview only. **Not Production.** Does **not** start P14-WP03.

## One command (preferred)

From the repository root:

```powershell
.\tools\Start-LocalValidation.ps1
```

This script:

1. Checks Docker Desktop
2. Starts **only** Platform + POS PostgreSQL (ports **15533** / **15534**); preserves volumes; never `down -v`
3. Stops stale repo-scoped `ExItS.Platform.Api` / `ExItS.PinoyBusinessPOS.Api` / `ExItS.Platform.Admin` processes
4. Starts, in order, with `dotnet watch`:
   - Platform API → http://localhost:8091
   - POS API → http://localhost:8092
   - Platform Admin → http://localhost:8090
5. Waits for ports, runs health checks, prints URLs

Stop local apps (DBs keep running):

```powershell
.\tools\Stop-LocalValidation.ps1
```

Stop apps and DB containers (volumes kept):

```powershell
.\tools\Stop-LocalValidation.ps1 -StopDatabases
```

## Destructive reset and reseed (Local Validation volumes only)

When seed data is wrong or obsolete:

```powershell
.\tools\Reset-LocalValidation.ps1 -ConfirmReset
```

This stops apps/DBs, removes **only** `exits_local_validation_*_db_data` volumes, starts Local Validation again, and verifies eight seed identities. Production is rejected. Ordinary startup never performs this wipe.

## Prerequisites

```powershell
cd deploy\docker
Copy-Item .env.local-validation.example .env.local-validation
# Fill REPLACE_* (DB passwords + LOCAL_VALIDATION_SHARED_PASSWORD, min 12 chars). Never commit.
```

## Shape

```text
Docker
├── Platform PostgreSQL  :15533
└── POS PostgreSQL       :15534

Local (dotnet watch)
├── Platform API         :8091
├── POS API              :8092
└── Platform Admin       :8090
```

## Data Protection (Admin)

Keys live under:

`%LOCALAPPDATA%\ExItS\LocalValidation\DataProtectionKeys`

Created automatically. Outside the repo. Live-preview / Staging only (not Production).

If an old antiforgery cookie still fails: use Incognito or clear localhost site data once.

## Open

**http://localhost:8090/** → `/admin/login` → use the Local Validation identity dropdown (or manual credentials). Sign-in is normal Platform `/auth/login` on the Admin server; password from `LOCAL_VALIDATION_SHARED_PASSWORD` (never commit; never shown in the browser).

## Related

- [README.local-validation.md](README.local-validation.md) — compose overview
- `tools/Start-LocalValidation.ps1` / `tools/Stop-LocalValidation.ps1`
- [P14-WP02A report (historical; superseded by Local Validation)](../../docs/reports/P14-WP02A-live-preview-test-users-and-quick-login.md)
- [P16-WP11 Local Validation report](../../docs/reports/P16-WP11-local-validation-replaces-live-preview.md)
