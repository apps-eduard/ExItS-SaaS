# PLATFORM-WEB-AUTH-MAILPIT-01 — React registration, activation, password reset, Mailpit

**Status:** Implemented on `fix/platform-admin-react-auth-mailpit` (not merged)  
**Authorization:** `AGENT_3_LOCAL_ACCESS_FIX=APPROVED`  
**Starting HEAD:** `ecc20df4940afbbf01609af0b8e47eea2ccde812`  
**Worktree:** `C:\Users\speed\Desktop\ExItS-SaaS-PlatformWeb-local-access`

## Delivered

Public React Platform Admin auth now uses the existing Platform API contracts:

| Route | Page |
|---|---|
| `/admin/login` | Sign In (unchanged links + Local Validation test-user tools) |
| `/admin/register` | Personal account registration (display name + email) |
| `/admin/activate-account?token=` | Set password and activate |
| `/admin/forgot-password` | Enumeration-safe reset request |
| `/admin/reset-password?token=` | Set new password |

Create Account remains **Personal** `PendingVerification` → activation email → password → `Active`. It does not self-register Platform Administrator, Organization Administrator, or staff roles.

Anonymous calls use same-origin `/api` (`platformRequest`). Browser runtime does not use `http://localhost:8091`.

## Mailpit / email links

Local Validation SMTP remains the existing PlatformEmail + `SmtpPlatformAuthOutboundMessageSink` + `PlatformAuthOutboundEmailComposer`. No second mail stack.

Launchers now set:

`PlatformEmail__AdminPublicBaseUrl` = React origin (`$publicAdminWebReactUrl` / `LOCAL_VALIDATION_ADMIN_WEB_REACT_ORIGIN`, default `http://localhost:8095`).

Compose no longer points auth email links at Blazor `:8090`.

The composer itself is host-agnostic. It builds:

- `{baseUrl}/admin/activate-account?token=...`
- `{baseUrl}/admin/reset-password?token=...`

Detected Tailscale/public host is launcher configuration only. Production compose does not include Mailpit or a hardcoded Tailscale IP.

**Live stack note:** while this package was implemented, the already-running Platform API still emitted Mailpit links with origin `http://<detected-host>:8090`. Source/config is `:8095`. Recycle Local Validation (or recreate the Platform API container with the new env) before owner Tailscale email-link proof.

## Owner validation

### Registration

1. Open http://localhost:8095/admin/register
2. Register a new temporary email (display name + email)
3. Open http://localhost:8025
4. Open the activation message
5. Set password
6. Sign in at http://localhost:8095/admin/login

### Password reset

1. Open http://localhost:8095/admin/forgot-password
2. Enter the account email
3. Open http://localhost:8025
4. Open the reset message
5. Set a new password
6. Sign in

### Tailscale equivalents

Use the detected public host printed by the launcher (do not hardcode an IP in source):

- Register: `http://<detected-host>:8095/admin/register`
- Forgot password: `http://<detected-host>:8095/admin/forgot-password`
- Mailpit: `http://<detected-host>:8025`
- Email links: `http://<detected-host>:8095`

Optional Windows Firewall for Mailpit: inbound TCP 8025, **Private** profile only. Launchers do not create firewall rules. Do not use Profile Any.

Local Validation success screens may show **Open Mailpit** using `window.location.hostname:8025`. Production does not enable that flag.

## Security

- Forgot-password UI always uses the generic acknowledgement, including unknown emails.
- **REGISTRATION_API_ENUMERATION=CLOSED (AUTH-MAILPIT-02):** `POST /api/v1/platform/auth/register` returns the same generic acknowledgement for new eligible emails, existing Active accounts, existing PendingVerification Personal accounts, and other existing/ineligible addresses. It does not return `application.user.email_conflict`, “already exists”, account status, or user/profile existence. HTTP 200 + uniform body shape when `ExposeDebugTokens` is false (Production).
- **PendingVerification reissue:** when the normalized email belongs to an existing Personal `PendingVerification` account (`HomeOrganizationId` null), registration invalidates the prior active `EmailVerification` token, issues a fresh token, sends a fresh activation email, and returns the same generic acknowledgement without creating a duplicate user or Personal profile. Active or otherwise ineligible duplicates receive the generic acknowledgement with no account mutation and no outbound email.
- Activation/reset tokens stay in the query string only long enough to submit. They are not stored in localStorage/sessionStorage and are not copied into diagnostics.
- Debug tokens from the API are stripped in the React client (and omitted entirely when `ExposeDebugTokens` is false).
- Password reset continues to revoke sessions/access/recovery credentials in `ResetPasswordWithToken`.
- Production cookie Secure policy is unchanged. Local Validation HTTP cookies remain the previous local-access package.

## AUTH-MAILPIT-02 — registration enumeration closure

**Starting HEAD:** `b6efcde096abcea3d6af61dad6deb3f7805a239d`  
**Scope:** backend `RegisterPersonalAccount` + React removal of `EmailConflict` client workaround + tests.

Generic acknowledgement (all public success branches):

`If the email is eligible, a verification message was sent. Open the message to activate your Personal Account.`

**Live runtime refresh (approved):** React `:8095` image `exits/platform-admin-web:auth-mailpit` with runtime `buildSha: b6efcde0`. Platform API recycled with `PlatformEmail__AdminPublicBaseUrl=http://<detected-host>:8095` (not `:8090`). Mailpit reset links verified at `:8095/admin/reset-password?token=...`.

## Explicit exclusions

- No merge to `main`, `feat/platform-admin-web-v2`, `feat/platform-admin-pa-com-04`, or `feat/pos-react-client`
- No POS React changes
- No Agent 2 commercial/subscription files
- Recovery-email confirmation and staff invitation accept pages remain out of scope (those emails now also inherit React `:8095` as AdminPublicBaseUrl)
- `TAILSCALE_DEVICE_VERIFIED=NO` (not physically tested from another device)

## Tests

| Gate | Result |
|---|---|
| `npm run typecheck` | PASS |
| `npm run lint` | PASS |
| `npm run format:check` | PASS |
| `npm test` (Vitest) | PASS — 52 files / 282 tests |
| `npm run build` | PASS |
| Playwright `public-auth` + `local-validation-test-user` | PASS — 8 tests |
| Identity unit tests (`FullyQualifiedName~Identity`) | PASS — 193 |
| `LocalValidationPackagingArchitectureTests` | PASS — 4 |
| Full `ExItS.ArchitectureTests` | Not claimed — unrelated pre-existing failures remain on this worktree |
| Live LV register → Mailpit → activate → login → forgot → reset | PASS against `127.0.0.1:8095/api` and Mailpit `8025` |
| Live email link origin before API recycle | `http://<detected-host>:8090` (old runtime env) |

Do not fabricate a physical Tailscale-device PASS.
