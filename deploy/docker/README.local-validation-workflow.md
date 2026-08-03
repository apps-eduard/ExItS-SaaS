# Local Validation — local development (Docker DBs + host apps)

Personal Staging preview only. **Not Production.** Does **not** start P14-WP03.

## One command (preferred)

From the repository root:

```powershell
.\tools\Start-LocalValidation.ps1
```

Tailscale / LAN (bind `0.0.0.0`, print public URLs, CORS + AllowedHosts for the host):

```powershell
.\tools\Start-LocalValidation.ps1 -PublicHost 100.120.79.81
```

**Seed scope:** default is `PlatformAdministratorsOnly` (Olivia + Rafael only). Ordinary Start/restart does **not** restore the legacy eight-identity catalog. Use `-SeedScope Full` only when you explicitly want that catalog. Reset uses admins-only plus transactional purge.

Printed when `-PublicHost` is set:

- Admin: `http://100.120.79.81:8090`
- Platform API: `http://100.120.79.81:8091`
- POS API: `http://100.120.79.81:8092`

Kestrel always binds `http://0.0.0.0:8090|8091|8092` (localhost still works). Database connection strings stay `127.0.0.1:15533` / `127.0.0.1:15534`.

### Windows Firewall (apps only)

Allow inbound TCP **8090 / 8091 / 8092**. Do **not** open **15533 / 15534** (PostgreSQL stays local-only).

```powershell
New-NetFirewallRule -DisplayName "ExItS Local Validation Admin 8090" -Direction Inbound -Protocol TCP -LocalPort 8090 -Action Allow -Profile Any
New-NetFirewallRule -DisplayName "ExItS Local Validation Platform API 8091" -Direction Inbound -Protocol TCP -LocalPort 8091 -Action Allow -Profile Any
New-NetFirewallRule -DisplayName "ExItS Local Validation POS API 8092" -Direction Inbound -Protocol TCP -LocalPort 8092 -Action Allow -Profile Any
```

Requires an elevated PowerShell. The start script prints the same guidance after a successful launch.

This script:

1. Checks Docker Desktop
2. Starts **only** Platform + POS PostgreSQL (ports **15533** / **15534**); preserves volumes; never `down -v`
3. Stops stale repo-scoped `ExItS.Platform.Api` / `ExItS.PinoyBusinessPOS.Api` / `ExItS.Platform.Admin` processes
4. Starts, in order, with `dotnet watch` bound to **0.0.0.0**:
   - Platform API → http://localhost:8091 (or `http://<PublicHost>:8091`)
   - POS API → http://localhost:8092 (or `http://<PublicHost>:8092`)
   - Platform Admin → http://localhost:8090 (or `http://<PublicHost>:8090`)
5. Waits for ports, runs health checks, prints URLs
6. Configures CORS for `http://localhost:8090`, `http://127.0.0.1:8090`, and `http://<PublicHost>:8090` when `-PublicHost` is set

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
