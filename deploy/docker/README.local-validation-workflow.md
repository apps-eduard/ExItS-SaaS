# Local Validation — host and Docker workflows

Personal Staging preview only. **Not Production.** Does **not** start P14-WP03.

## Fast local development (separate ports, same databases)

For day-to-day code changes, use `.\tools\Start-LocalDev.ps1`. It reuses `platform-db` and `pos-db` and does not rebuild images. The public preview can stay up on the same databases. Details and the port table are in [README.local-validation.md](README.local-validation.md). Refresh the preview images with `.\tools\Start-DockerLocalValidation.ps1 -Build`.

## FAST host mode (same ports as Docker validation)

From the repository root:

```powershell
.\tools\Start-LocalValidation.ps1
```

FAST mode runs PostgreSQL and Mailpit in Docker and the five .NET applications under local
`dotnet watch`, plus the React Admin production image on 8095.

Mode switching is automatic: FAST startup stops FULL mode's app containers
before it starts host apps, then starts React Admin on 8095. PostgreSQL, Mailpit, and database volumes remain in place.

## FULL Docker mode

Use FULL mode to validate application images and Docker service wiring:

```powershell
.\tools\Start-DockerLocalValidation.ps1
```

FULL startup stops only this repository's host app processes, verifies that no unknown
process owns ports 8091-8095, starts infrastructure, then starts the app containers
(React Platform Admin on 8095).
It does not replace FAST mode as the normal coding workflow.

Build controls:

```powershell
# Rebuild images as part of startup
.\tools\Start-DockerLocalValidation.ps1 -Build

# Rebuild all app images without cache, then start them
.\tools\Start-DockerLocalValidation.ps1 -CleanBuild
```

Both preserve PostgreSQL volumes. API startup continues to run the existing
LocalValidation migration hosted services; the launcher does not add startup migration
calls to application code.

Stop only Docker apps (infrastructure keeps running):

```powershell
.\tools\Stop-DockerLocalValidation.ps1
```

Stop Docker apps, PostgreSQL, and Mailpit while retaining volumes:

```powershell
.\tools\Stop-DockerLocalValidation.ps1 -StopInfrastructure
```

Tailscale / LAN (bind `0.0.0.0`, print public URLs, CORS + AllowedHosts for the host):

```powershell
.\tools\Start-LocalValidation.ps1 -PublicHost 100.120.79.81
```

**Seed scope:** default is `PlatformAdministratorsOnly` (Olivia + Rafael only). Ordinary Start/restart does **not** restore the legacy eight-identity catalog and **decommissions** those Full-catalog fixture accounts if they still exist. Owner-created users are kept. Use `-SeedScope Full` only when you explicitly want that catalog. Reset uses admins-only plus transactional purge (volume wipe). Quick login is database-backed: see [Reset-LocalValidation.md](../../Reset-LocalValidation.md).

Printed when `-PublicHost` is set:

- Admin: `http://127.0.0.1:8095/admin` (canonical React sign-in)
- Platform API: `http://100.120.79.81:8091`
- POS API: `http://100.120.79.81:8092`
- Personal, Organization, and POS: `http://127.0.0.1:5177` (React)
- Platform Admin is the React `admin-web` service on port 8095.

Host APIs bind `http://0.0.0.0:8091|8092`. The React client uses port `5177`. Platform Admin is published on `8095`. Database connection strings stay `127.0.0.1:15533` / `127.0.0.1:15534`. These local ports are internal; production public entry is HTTPS :443.

If you omit `-PublicHost`, Start still tries (in order): `LOCAL_VALIDATION_PUBLIC_HOST` in `.env.local-validation`, the last saved PublicHost, then an active Tailscale `100.x` address. Plain `.\tools\Start-LocalValidation.ps1` should keep Tailscale AllowedHosts working once that value is known.

### Windows Firewall (apps only)

Allow inbound TCP **8091 / 8092 / 8095 / 5177**. Mailpit UI **8025** is optional for Tailscale
devices. Do **not** open **15533 / 15534** (PostgreSQL stays local-only). Prefer the **Private**
profile. Do not use Profile Any.

```powershell
New-NetFirewallRule -DisplayName "ExItS Local Validation Admin 8095" -Direction Inbound -Protocol TCP -LocalPort 8095 -Action Allow -Profile Private
New-NetFirewallRule -DisplayName "ExItS Local Validation Platform API 8091" -Direction Inbound -Protocol TCP -LocalPort 8091 -Action Allow -Profile Private
New-NetFirewallRule -DisplayName "ExItS Local Validation POS API 8092" -Direction Inbound -Protocol TCP -LocalPort 8092 -Action Allow -Profile Private
New-NetFirewallRule -DisplayName "ExItS Local Validation React app 5177" -Direction Inbound -Protocol TCP -LocalPort 5177 -Action Allow -Profile Private
New-NetFirewallRule -DisplayName "ExItS Local Validation React Admin 8095" -Direction Inbound -Protocol TCP -LocalPort 8095 -Action Allow -Profile Private
New-NetFirewallRule -DisplayName "ExItS Local Validation Mailpit 8025" -Direction Inbound -Protocol TCP -LocalPort 8025 -Action Allow -Profile Private
```

Requires an elevated PowerShell. The start script prints the same guidance after a successful launch.

The FAST host launcher:

1. Checks Docker Desktop
2. Stops FULL mode's app containers only
3. Starts Platform + POS PostgreSQL and Mailpit; preserves volumes
4. Stops stale repo-scoped `ExItS.Platform.Api` / `ExItS.PinoyBusinessPOS.Api` processes
5. Starts, in order:
   - Platform API → http://localhost:8091 (or `http://<PublicHost>:8091`)
   - POS API → http://localhost:8092 (or `http://<PublicHost>:8092`)
   - Platform Admin (Docker React `admin-web`) → http://127.0.0.1:8095
   - Personal, Organization, and POS React client → http://127.0.0.1:5177
6. Waits for ports, runs health checks, prints URLs
7. Configures CORS for Admin and the React client localhost/public origins

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

This stops apps/DBs, removes **only** `exits_local_validation_*_db_data` volumes, starts Local Validation again, and verifies **two** seed identities (Olivia + Rafael). Production is rejected. Ordinary startup never performs this wipe. Root cheat sheets: [Start-LocalValidation.md](../../Start-LocalValidation.md), [Reset-LocalValidation.md](../../Reset-LocalValidation.md).

## Prerequisites

```powershell
cd deploy\docker
Copy-Item .env.local-validation.example .env.local-validation
# Fill REPLACE_* (DB passwords + LOCAL_VALIDATION_SHARED_PASSWORD, min 12 chars). Never commit.
```

## FAST mode shape

```text
Docker
├── Platform PostgreSQL  :15533
├── POS PostgreSQL       :15534
└── Mailpit              :8025 / :1025

Local (dotnet watch)
├── Platform API         :8091
├── POS API              :8092
├── Platform Admin       :8095  (React admin-web)
└── React Personal, Organization, and POS :5177
```

## Data Protection (Admin)

Keys live under:

`%LOCALAPPDATA%\ExItS\LocalValidation\DataProtectionKeys`

Created automatically. Outside the repo. Live-preview / Staging only (not Production).

If an old antiforgery cookie still fails: use Incognito or clear localhost site data once.

## Open

**http://127.0.0.1:8095/admin/login** → use the Local Validation identity selector (or manual credentials). Sign-in is Platform `/api/v1/platform/auth/login`. Password comes from `LOCAL_VALIDATION_SHARED_PASSWORD` (never commit; never shown in the browser).

## Related

- [README.local-validation.md](README.local-validation.md) — compose overview
- `tools/Start-LocalValidation.ps1` / `tools/Stop-LocalValidation.ps1` (FAST host mode)
- `tools/Start-DockerLocalValidation.ps1` / `tools/Stop-DockerLocalValidation.ps1` (FULL Docker mode)
- [README.cloudflare-local-preview.md](README.cloudflare-local-preview.md) — optional Cloudflare Tunnel preview; not the daily workflow and not Production
- [P14-WP02A report (historical; superseded by Local Validation)](../../docs/reports/P14-WP02A-live-preview-test-users-and-quick-login.md)
- [P16-WP11 Local Validation report](../../docs/reports/P16-WP11-local-validation-replaces-live-preview.md)
