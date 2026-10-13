# ExItS Local Validation — overview (P16-WP11)

Production-equivalent **local deployment** for validation. Same application code as Production; only local configuration differs (ports, secrets, seed flag, TLS off for host-run apps).

**Not** packaging (`compose.yaml`). Does **not** close Phase 16 or start Phase 17. Production topology template remains `compose.production.yaml`.

.NET MAUI and its isolated Local Validation stack are retired. See [ADR-024](../../docs/decisions/ADR-024-react-only-client-standard-and-legacy-ui-retirement.md).

## Fast local development (no image rebuild)

Use this for normal `.cs`, `.ts`, `.tsx`, and CSS edits. PostgreSQL stays in the existing Local Validation containers. The APIs and React apps run on the host.

```powershell
.\tools\Start-LocalDev.ps1
```

Stop the apps without stopping PostgreSQL:

```powershell
.\tools\Stop-LocalDev.ps1
```

| Surface | URL | Notes |
| --- | --- | --- |
| Platform API | http://127.0.0.1:5288 | `dotnet watch`, Development |
| POS API | http://127.0.0.1:5290 | `dotnet watch`, Development |
| Personal, Organization, and POS | http://127.0.0.1:5178 | Vite HMR |
| Platform Admin | http://127.0.0.1:5195 | Vite HMR |
| PinoyLoanManager | http://127.0.0.1:5176 | only with `-IncludeLoanManager` |
| Platform PostgreSQL | 127.0.0.1:15533 | volume `exits_local_validation_platform_db_data` |
| POS PostgreSQL | 127.0.0.1:15534 | volume `exits_local_validation_pos_db_data` |
| Mailpit | http://127.0.0.1:8025 | SMTP 1025 |

Host processes connect with `Host=127.0.0.1` and the published database ports. Dockerized APIs keep using `Host=platform-db` / `Host=pos-db` and container port 5432. There is no second database and no second volume.

Google sign-in on Personal uses this redirect URI, which must be listed on the same OAuth client as `LOCAL_VALIDATION_GOOGLE_CLIENT_ID`:

`http://127.0.0.1:5178/platform-api/api/v1/platform/auth/external/google/callback`

PayMongo checkout on fast dev uses `LOCAL_VALIDATION_PAYMONGO_SECRET_KEY` and `LOCAL_VALIDATION_PAYMONGO_WEBHOOK_SECRET` from the gitignored env file. The return address is `http://127.0.0.1:5178`. Leave those keys empty and checkout stays unavailable.

Public preview and fast local dev can run at the same time. They use the same PostgreSQL volumes, so users and business data match. A sign-in on one site does not carry the browser session to the other site. Neither mode runs EF migrations on startup.

The sign-in Local Validation panel follows the page you opened. On `http://127.0.0.1:5178` it restarts the fast-dev apps on 5288, 5290, 5178, and 5195. On the public preview (`http://127.0.0.1:5177`) it restarts 8091, 8092, 5177, and 8095. Shared PostgreSQL on 15533 and 15534, and Mailpit on 8025, stay shared. Restart the loopback supervisor on 8099 after this change so the login buttons use the new profile.

The preview site keeps serving the last built images. Refresh those images without stopping local dev:

```powershell
.\tools\Start-DockerLocalValidation.ps1 -Build
```

Service Pro is not a canonical client in this repository. Retired MAUI and Blazor hosts are not started.

Google and Facebook callbacks stay on the origins configured for that provider. Fast dev does not change production cookie, CSRF, or CORS policy. The launcher adds only the loopback origins above.

### When to rebuild images

Rebuild application images only when a Dockerfile changes, NuGet or npm dependencies change, the runtime base image changes, or you are intentionally validating the production-like containers:

```powershell
.\tools\Start-DockerLocalValidation.ps1 -Build
```

That rebuilds the preview images and restarts the preview containers. Fast local dev and the database volumes keep running. Normal source edits show up in local dev without that rebuild.

## FAST host mode (same ports as Docker validation)

From repository root:

```powershell
.\tools\Start-LocalValidation.ps1
```

This keeps PostgreSQL and Mailpit in Docker while the five .NET apps run with `dotnet watch`,
React Platform Admin on 8095 (`admin-web`), and
canonical React POS Vite on `:5177` (`src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React`).

After start, the launcher prints:

```text
Platform Admin React:
  Local:     http://localhost:8095/admin/login
  Tailscale: http://<tailscale-ip>:8095/admin/login   (omitted if Tailscale is unavailable)
  API:       same-origin /api
  Local Validation tools: Enabled
```

## FULL Docker mode

Use the production-shaped container topology for end-to-end image validation:

```powershell
.\tools\Start-DockerLocalValidation.ps1 -Build
```

This runs the full application stack in Docker (Platform API `:8091`, POS API `:8092`, React Admin `:8095`, org/personal web, React POS `:5177`, PostgreSQL, Mailpit).

Optional external preview of that same stack (not the daily command, not Production): [README.cloudflare-local-preview.md](README.cloudflare-local-preview.md).

The launcher automatically stops repo-scoped host apps before claiming ports 8091-8095 (and React POS `:5177` when that service is included).
Use `-Build` to rebuild changed images during startup, or `-CleanBuild` for a no-cache image
build. Neither option removes database volumes.

```powershell
.\tools\Start-DockerLocalValidation.ps1 -CleanBuild
```

Stop Docker apps while leaving PostgreSQL and Mailpit running:

```powershell
.\tools\Stop-DockerLocalValidation.ps1
```

Add `-StopInfrastructure` to stop PostgreSQL and Mailpit too; volumes are still preserved.

Full operator guide: [`README.local-validation-workflow.md`](README.local-validation-workflow.md).

## Destructive reset (Local Validation only)

When the database has obsolete orgs/users (for example leftover Sampaguita/Mabuhay or `.exits.test` identities), wipe **only** Local Validation volumes and reseed:

```powershell
.\tools\Reset-LocalValidation.ps1 -ConfirmReset
```

Requires explicit `-ConfirmReset`. Rejects Production environment / Production-looking connection strings. Removes only:

- `exits_local_validation_platform_db_data`
- `exits_local_validation_pos_db_data`

Never place broad deletion in ordinary application startup.

## Target shapes

```text
FAST host mode
Docker
├── Platform PostgreSQL  (host port 15533)
├── POS PostgreSQL       (host port 15534)
└── Mailpit              UI http://localhost:8025 · SMTP 1025

Local .NET (dotnet watch)
├── Platform API         http://localhost:8091  (PlatformEmail → Mailpit SMTP :1025)
└── POS API              http://localhost:8092

Docker (FAST also starts this image)
└── Platform Admin       http://127.0.0.1:8095  (React admin-web)

Canonical Personal, Organization, and POS React client:
└── React app            http://127.0.0.1:5177

**Auth / Mailpit:** `PlatformEmail__AdminPublicBaseUrl` must be the canonical Admin origin
(`http://127.0.0.1:8095` / `LOCAL_VALIDATION_ADMIN_ORIGIN`).
Activation and password-reset emails open `/admin/activate-account` and `/admin/reset-password` on that host.
Running Platform API **without** `PlatformEmail__*` silently drops outbound mail (null sink) while register/forgot still return success.

API-only helper with Mailpit + React Admin links: `.\tools\Start-PlatformApiOnly.ps1`.

FULL Docker mode
Docker Compose
├── Platform/POS PostgreSQL + Mailpit
└── Platform API, POS API, React Admin (8095), Personal Web, React app (:5177)

React POS Docker notes:
- Image: `deploy/docker/Dockerfile.pos-react` (nginx static SPA)
- Same-origin proxies: `/platform-api` → Platform API, `/pos-api` → POS API
- HTTP Local Validation strips `Secure` from Set-Cookie (parity with Vite DEV proxy)
- Emulator: `http://10.0.2.2:5177` or `adb reverse tcp:5177 tcp:5177` → `http://127.0.0.1:5177`
- Do not run `npm run dev` and Docker React POS on `:5177` at the same time

```

Tailscale/LAN: pass `-PublicHost <tailscale-ip>` to either start launcher. Firewall and
CORS details: [`README.local-validation-workflow.md`](README.local-validation-workflow.md).

### Personal Account registration and password reset (React + Mailpit)

Owner validation uses the React Platform Admin on port **8095**. Email links are built from
`PlatformEmail:AdminPublicBaseUrl`, which Local Validation sets to the React origin
(`http://localhost:8095` or `http://<detected-host>:8095`). Do not hardcode a Tailscale IP.

**Registration**

1. Open [http://localhost:8095/admin/register](http://localhost:8095/admin/register).
2. Register a **new temporary** Personal email (display name + email only; no password yet).
3. Open [http://localhost:8025](http://localhost:8025) (Mailpit).
4. Open the activation message and follow **Activate your account**.
5. Set a password. The account becomes **Active**.
6. Sign in at [http://localhost:8095/admin/login](http://localhost:8095/admin/login).

**Password reset**

1. Open [http://localhost:8095/admin/forgot-password](http://localhost:8095/admin/forgot-password).
2. Enter the account email or username. The UI always shows the same generic confirmation.
3. Open [http://localhost:8025](http://localhost:8025).
4. Open the reset message and follow **Reset password**.
5. Set a new password, then sign in. The previous password must fail.

**Tailscale equivalents** (use the detected public host from the launcher, not a hardcoded IP):

- React register: `http://<detected-host>:8095/admin/register`
- React forgot password: `http://<detected-host>:8095/admin/forgot-password`
- Mailpit: `http://<detected-host>:8025`
- Activation/reset links in email use `http://<detected-host>:8095`

Mailpit is only the Local Validation catcher; tokens, activation, and authorization are real
application behavior. Production builds must not show Mailpit links.

Optional Windows Firewall for Mailpit on Tailscale: inbound TCP 8025, **Private** profile only.
This launcher does not create firewall rules. Do not use Profile Any.

See also
[`docs/Platform-Admin-Web/Reports/PLATFORM-WEB-AUTH-MAILPIT-01-registration-password-reset.md`](../../docs/Platform-Admin-Web/Reports/PLATFORM-WEB-AUTH-MAILPIT-01-registration-password-reset.md).

## One-time setup

```powershell
cd deploy\docker
Copy-Item .env.local-validation.example .env.local-validation
# Fill REPLACE_* values. Never commit .env.local-validation.
```

Sign in on Admin via the Local Validation identity dropdown (server-side normal `POST /auth/login`) or manual credentials.

### Organizations (exactly 2)

- **ABC Sari-Sari Store** (`abc-sari-sari`) — Maria Santos (Owner / POS Owner), Carlo Reyes (Member / POS Cashier)
- **XYZ Mini Grocery** (`xyz-mini-grocery`) — Ana Cruz (Owner / POS Owner), Daniel Garcia (Member / POS Cashier)

### Platform (exactly 2)

- Olivia Mendoza — Platform Administrator
- Rafael Torres — Platform Support

### Personal (exactly 2)

- Luis Navarro
- Sofia Ramos

Password from `LOCAL_VALIDATION_SHARED_PASSWORD` env only (never commit; never exposed in the browser).

Dataset version: `2026-08-02-abc-xyz-v1`. Personal Utang seed creates Luis→Sofia (₱5,000 loan, ₱1,500 payment) and Sofia→Luis (₱1,000 loan) with ledger-derived balances and reminders.

Obsolete seed orgs (`sampaguita-store`, `mabuhay-mini-mart`, `phase16-seed-org`) and `.exits.test` identities are closed/decommissioned on seed. Prefer `Reset-LocalValidation.ps1 -ConfirmReset` for a clean database.

## Migration from Live Preview

- Rename `.env.live-preview` → `.env.local-validation` and replace `LIVE_PREVIEW_` with `LOCAL_VALIDATION_`.
- Docker project/volumes are now `exits-local-validation*` (prior `exits-live-preview*` volumes are not attached automatically).
