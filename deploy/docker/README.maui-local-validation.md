# Legacy MAUI / Blazor Local Validation isolation (LEGACY-MAUI-ISO-01)

Isolates the **old MAUI + Blazor** Local Validation environment from the **React** Local Validation environment so both can run at the same time.

## Two stacks

| | React (unchanged) | Legacy MAUI/Blazor |
|---|---|---|
| Compose project | `exits-local-validation` | `exits-maui-local-validation` |
| Platform API | `8091` | `8191` |
| POS API | `8092` | `8192` |
| Admin | React Vite `8095` | Blazor `8190` |
| Org / Personal | (React flows) | Blazor `8193` / `8194` |
| React POS | `5177` | — |
| Platform DB host | `15533` → `exits_platform` | `16533` → `exits_maui_platform` |
| POS DB host | `15534` → `exits_pos` | `16534` → `exits_maui_pos` |
| Mailpit | `8025` / `1025` | `8125` / `1125` |
| Volumes | `exits_local_validation_*` | `exits_maui_local_validation_*` |
| Network | `exits-local-validation` | `exits-maui-local-validation` |

## Start / stop

```powershell
# React (unchanged)
.\tools\Start-ReactIntegrationLocalValidation.ps1
.\tools\Start-ReactIntegrationLocalValidation.ps1 -PublicHost 100.x.x.x

# Legacy MAUI/Blazor (this package) — safe while React is running
.\tools\Start-MauiLegacyLocalValidation.ps1
.\tools\Start-MauiLegacyLocalValidation.ps1 -PublicHost 100.x.x.x
.\tools\Stop-MauiLegacyLocalValidation.ps1
```

Env file: copy `deploy/docker/.env.maui-local-validation.example` → `.env.maui-local-validation` (gitignored).

Compose: `deploy/docker/compose.maui-local-validation.yaml`

**Never** run `docker compose down -v` against either stack unless you intentionally wipe that stack’s volumes.

### Tailscale / LAN (`-PublicHost`)

When `-PublicHost <ip-or-hostname>` is supplied:

- Blazor Admin / Org / Personal public URLs use `http://<PublicHost>:8190|8193|8194`
- Platform/POS APIs accept that host in AllowedHosts; CORS includes PublicHost origins
- localhost / `127.0.0.1` and Android emulator `10.0.2.2` remain supported
- PostgreSQL host ports stay loopback-published only (`16533` / `16534`) — do not Tailscale-publish DB ports
- Launchers print both LOCAL and PUBLIC URL lists

Physical device MAUI Debug overlay: set `LocalValidation.PublicHost` and API base URLs to `http://<PublicHost>:8191` / `:8192` in `appsettings.LocalValidation.PhysicalDevice.json`.

## Client URLs (Local Validation / Debug only)

Desktop / browser:

- Platform API `http://127.0.0.1:8191`
- POS API `http://127.0.0.1:8192`
- Admin `http://127.0.0.1:8190`
- Organization `http://127.0.0.1:8193`
- Personal `http://127.0.0.1:8194`

Android emulator (MAUI Debug Emulator target):

- Platform API `http://10.0.2.2:8191`
- POS API `http://10.0.2.2:8192`

Physical device Debug overlay still uses Tailscale/LAN `PublicHost` but **ports 8191/8192**.

Default seed scope is **`Full`** (fixture orgs ABC Sari-Sari / XYZ Mini Grocery + org users).  
Kizy Store is **not** a MAUI seed identity — it lives on the React DB. A MAUI app that can sign in as Kizy is talking to React `:8091` by mistake.

### Registration / password-reset email (Mailpit)

MAUI Platform API (`:8191`) sends mail to **MAUI Mailpit** only (`UI :8125`, SMTP `:1125` → container `maui-mailpit`).  
React Platform API (`:8091`) uses React Mailpit (`:8025` / `:1025`).

| POS client talks to | Open Mailpit at |
|---|---|
| MAUI `:8191` | **http://127.0.0.1:8125** |
| React `:8091` | http://127.0.0.1:8025 |

If registration “succeeds” but nothing appears in `:8025`, you are looking at the wrong stack — check `:8125`.

Production / Release API URLs are unchanged.
