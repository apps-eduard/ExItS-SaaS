# ExItS Local Validation — overview (P16-WP11)

Production-equivalent **local deployment** for validation. Same application code as Production; only local configuration differs (ports, secrets, seed flag, TLS off for host-run apps).

**Not** packaging (`compose.yaml`). Does **not** close Phase 16 or start Phase 17. Production topology template remains `compose.production.yaml`.

## FAST host mode (preferred daily command)

From repository root:

```powershell
.\tools\Start-LocalValidation.ps1
```

This keeps PostgreSQL and Mailpit in Docker while the five .NET apps run with `dotnet watch`
and the React Admin production image listens on 8095 (parallel to Blazor Admin on 8090).

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
.\tools\Start-DockerLocalValidation.ps1
```

The launcher automatically stops repo-scoped host apps before claiming ports 8090-8095.
Use `-Build` to rebuild changed images during startup, or `-CleanBuild` for a no-cache image
build. Neither option removes database volumes.

```powershell
.\tools\Start-DockerLocalValidation.ps1 -Build
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
├── Platform API         http://localhost:8091  (PlatformEmail → Mailpit)
├── POS API              http://localhost:8092
├── Platform Admin Web   http://localhost:8090  (existing Blazor Admin)
├── Organization Web     http://localhost:8093
└── Personal Web         http://localhost:8094

Docker (FAST also starts this production image)
└── React Platform Admin http://localhost:8095  (parallel; not a cutover)

FULL Docker mode
Docker Compose
├── Platform/POS PostgreSQL + Mailpit
└── Platform API, POS API, Blazor Admin (8090), Organization Web, Personal Web, React Admin (8095)
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
