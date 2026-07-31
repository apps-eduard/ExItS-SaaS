# P13-WP02 — Identity Credentials and Auth Persistence

Phase marker: `P13-WP02-identity-credentials-and-auth-persistence`

Package: **P13-WP02 — Identity Credentials and Auth Persistence**
Prior tip: `407d913484c15a76858ea8152e87a6b0b5bbabcd`
Docs tip: `367defab6fcf523086d7b13d98e1d04579b250ef`

## Status

**Complete.** Platform credential persistence, PBKDF2 password hashing, lockout/email-verified state, first-admin bootstrap controls, and audit-safe credential APIs. **No login pages, cookies, bearer tokens, password-reset delivery, MFA, Admin route protection, or product launch authentication.**

Exact next: **P13-WP03 — Platform Login, Logout, and Browser Session** when authorized (do **not** begin).

**R-091 remains open** (no production login/session yet).

## 1. Delivered capability

| Area | Evidence |
|---|---|
| Domain credential aggregate | `PlatformUserCredential` (hash, algorithm, security stamp, lockout, email verified) |
| Persistence | `platform.platform_user_credentials` + migration `AddPlatformUserCredentials` |
| Hasher | `Pbkdf2PlatformPasswordHasher` (PBKDF2-SHA256, 100k iterations) — not ASP.NET Identity |
| Use cases | Set/get status, unlock, mark email verified, verify password (no session), bootstrap first admin |
| APIs | `GET/PUT .../credentials`, unlock, email-verified; `POST /api/v1/platform/auth/bootstrap` |
| Audit | `platform.user.password_set`, `credential_unlocked`, `email_verified`, `auth.bootstrap_completed` (no secrets) |
| Config | `PlatformAuthentication:Password|Lockout|Bootstrap` (bootstrap disabled by default) |

## 2. Locked access chain preserved

```text
Platform User → Organization Membership → Product Access → Product-Local Role
```

Credentials attach to Platform User only. Bootstrap grants **PlatformAdministrator** (Platform Authz), not product-local roles.

## 3. Explicit exclusions

Login/logout UI; cookies; bearer/refresh tokens; email delivery; MFA; Admin auth middleware; product client auth; SSO/AD.

## 4. Validation

| Check | Result |
|---|---|
| Full Release tests | **1201 passed / 0 failed / 0 skipped** |
| Migration apply/rollback/reapply | Credential migration tests |
| No HealthCare product tree | Pass |
| Portfolio independence | Pass |

## 5. Open decisions / risks

| ID | State |
|---|---|
| **R-091** | Open — login/session not implemented |
| **D-P12-03** | Open — commercial transport |
| **D-P12-05** | Open — honest Dev/Production language |
| **D-P13-01…06** | Closed (architecture) |

## Exact next work package

**P13-WP03 — Platform Login, Logout, and Browser Session** when explicitly authorized. Do not begin P13-WP03.
