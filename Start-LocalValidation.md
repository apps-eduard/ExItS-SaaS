# Start / stop Local Validation apps

**Local Validation only. Not Production.**  
Docker DBs + React Platform Admin, plus host Platform API, POS API, and Personal Web. The Organization experience is the React client on port 5177.

## Start (Tailscale / LAN PublicHost)

From the repository root:

```powershell
cd C:\Users\speed\Desktop\ExItS-SaaS
.\tools\Start-LocalValidation.ps1 -PublicHost 100.120.79.81
```

Replace `100.120.79.81` with your current Tailscale or LAN host if it changed.

### Host PowerShell windows (expected)

Start opens separate PowerShell windows for the host apps. Platform Admin is the Docker `admin-web` container on port 8095, not one of those windows.

| Window title | App | Port |
|---|---|---|
| ExItS LocalValidation - Platform API | Platform API | 8091 |
| ExItS LocalValidation - POS API | POS API | 8092 |
| ExItS LocalValidation - Personal Web | Personal Web | 8094 |

That is normal. Leave those windows open while you work; closing one stops that app.

**There is no `-SingleWindow` switch.** This parameter does **not** exist and must not be used:

```powershell
# NOT supported — do not run
.\tools\Start-LocalValidation.ps1 -PublicHost 100.120.79.81 -SingleWindow
```

### Restart

Run Start again. It stops stale repo-scoped apps first, then opens the host windows again:

```powershell
.\tools\Start-LocalValidation.ps1 -PublicHost 100.120.79.81
```

Or stop explicitly, then start:

```powershell
.\tools\Stop-LocalValidation.ps1
.\tools\Start-LocalValidation.ps1 -PublicHost 100.120.79.81
```

### Printed URLs (example)

- Admin: `http://127.0.0.1:8095/admin` (canonical React sign-in)
- Platform API: `http://100.120.79.81:8091`
- POS API: `http://100.120.79.81:8092`
- Organization app: `http://127.0.0.1:5177` (React)
- Personal Web: `http://100.120.79.81:8094`

Host Kestrel binds `0.0.0.0:8091|8092|8094`. The React Organization client uses port `5177`. Platform Admin is published on `8095`. DB ports stay `127.0.0.1:15533` / `15534`. These local ports are **not** public production ports; production uses HTTPS :443 via reverse proxy.

### If you omit `-PublicHost`

Start resolves, in order:

1. `LOCAL_VALIDATION_PUBLIC_HOST` in `deploy/docker/.env.local-validation`
2. Last saved PublicHost from launcher state
3. An active Tailscale `100.x` address when present

## Default seed scope

Ordinary Start uses **`PlatformAdministratorsOnly`** (Olivia + Rafael). It does **not** wipe existing orgs/products or owner-created users.

It **does** decommission leftover Full-catalog demo identities (Maria, Carlo, Ana, Daniel, Luis, Sofia) so quick login does not keep surprising historic fixtures.

Quick login lists **current database accounts**, not a static menu of every name in source code. Baseline entries are labeled `Baseline ·`.

For a full clean wipe (2 admins + empty POS + cleared templates):

→ [Reset-LocalValidation.md](Reset-LocalValidation.md)

To restore the eight-identity demo catalog **on purpose**:

```powershell
.\tools\Start-LocalValidation.ps1 -SeedScope Full
```

## Stop

Apps only (DBs keep running):

```powershell
.\tools\Stop-LocalValidation.ps1
```

Apps + DB containers (volumes preserved):

```powershell
.\tools\Stop-LocalValidation.ps1 -StopDatabases
```

## Windows Firewall (physical device / Tailscale)

Allow inbound TCP **8091 / 8092 / 8094 / 8095 / 5177**. Do **not** open **15533 / 15534**.

```powershell
New-NetFirewallRule -DisplayName "ExItS Local Validation Admin 8095" -Direction Inbound -Protocol TCP -LocalPort 8095 -Action Allow -Profile Any
New-NetFirewallRule -DisplayName "ExItS Local Validation Platform API 8091" -Direction Inbound -Protocol TCP -LocalPort 8091 -Action Allow -Profile Any
New-NetFirewallRule -DisplayName "ExItS Local Validation POS API 8092" -Direction Inbound -Protocol TCP -LocalPort 8092 -Action Allow -Profile Any
New-NetFirewallRule -DisplayName "ExItS Local Validation Personal Web 8094" -Direction Inbound -Protocol TCP -LocalPort 8094 -Action Allow -Profile Any
New-NetFirewallRule -DisplayName "ExItS Local Validation React app 5177" -Direction Inbound -Protocol TCP -LocalPort 5177 -Action Allow -Profile Any
```

## Related

- [ADR-024](docs/decisions/ADR-024-react-only-client-standard-and-legacy-ui-retirement.md) — MAUI is retired
- [deploy/docker/README.local-validation-workflow.md](deploy/docker/README.local-validation-workflow.md)
