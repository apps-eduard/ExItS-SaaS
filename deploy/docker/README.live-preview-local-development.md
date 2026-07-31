# Live Preview — local development (Docker DBs + host apps)

Personal Staging preview only. **Not Production.** Does **not** start P14-WP03.

## One command (preferred)

From the repository root:

```powershell
.\tools\Start-LivePreviewLocal.ps1
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
.\tools\Stop-LivePreviewLocal.ps1
```

Stop apps and DB containers (volumes kept):

```powershell
.\tools\Stop-LivePreviewLocal.ps1 -StopDatabases
```

## Prerequisites

```powershell
cd deploy\docker
Copy-Item .env.live-preview.example .env.live-preview
# Fill REPLACE_* (DB passwords + LIVE_PREVIEW_SHARED_PASSWORD, min 12 chars). Never commit.
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

`%LOCALAPPDATA%\ExItS\LivePreview\DataProtectionKeys`

Created automatically. Outside the repo. Live-preview / Staging only (not Production).

If an old antiforgery cookie still fails: use Incognito or clear localhost site data once.

## Open

**http://localhost:8090/** → `/admin/login` → Live Preview Test User.

## Related

- [README.live-preview.md](README.live-preview.md) — compose overview
- `tools/Start-LivePreviewLocal.ps1` / `tools/Stop-LivePreviewLocal.ps1`
- [P14-WP02A report](../../docs/reports/P14-WP02A-live-preview-test-users-and-quick-login.md)
