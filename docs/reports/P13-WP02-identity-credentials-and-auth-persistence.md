# P13-WP02 — Identity Credentials and Auth Persistence

Phase marker: `P13-WP02-identity-credentials-and-auth-persistence`

Package: **P13-WP02 — Identity Credentials and Auth Persistence**
Prior tip: `407d913484c15a76858ea8152e87a6b0b5bbabcd`
Feature tip (initial delivery): `367defab6fcf523086d7b13d98e1d04579b250ef`
Security-hardening tip: `51ace5b90fc6c0bcb33fe483826481529bdfeb77`

## Status

**Complete (security-hardened).** Platform credential persistence, ASP.NET Core `PasswordHasher<TUser>` wrapping (no custom PBKDF2 algorithm), lockout/email-verified state, hardened first-admin bootstrap, and audit-safe credential APIs. **No login pages, cookies, bearer tokens, password-reset delivery, MFA, Admin route protection, or product launch authentication.**

Exact next: **P13-WP03 — Platform Login, Logout, and Browser Session** when authorized (do **not** begin).

**R-091 remains open** (no production login/session yet).

## Security review findings (post-delivery)

| Finding | Verdict | Remediation |
|---|---|---|
| Custom `Pbkdf2PlatformPasswordHasher` | **Did not meet** full framework-primitive bar (no versioned hash format, no configurable work factor via hasher options, no rehash-needed detection) | Replaced with `AspNetCorePlatformPasswordHasher` → `PasswordHasher<PlatformPasswordUser>` |
| `POST /api/v1/platform/auth/bootstrap` | Could have become an anonymous enable-and-call path if misconfigured | Hardened: disabled by default, shared secret, Production refuse (startup + runtime), rate limit, concurrency/idempotency, no secret logging |

## 1. Delivered capability

| Area | Evidence |
|---|---|
| Domain credential aggregate | `PlatformUserCredential` (hash, algorithm `ASPNET-CORE-IDENTITY-V3`, security stamp, lockout, email verified) |
| Persistence | `platform.platform_user_credentials` + migration `AddPlatformUserCredentials` |
| Hasher | `AspNetCorePlatformPasswordHasher` wraps ASP.NET Core `PasswordHasher<TUser>` — random per-user salt, versioned format, configurable work factor (`PasswordHasherOptions`), constant-time verify, `SuccessRehashNeeded` → auto-rehash on verify |
| Use cases | Set/get status, unlock, mark email verified, verify password (no session; rehash on need), bootstrap first admin |
| APIs | `GET/PUT .../credentials`, unlock, email-verified; `POST /api/v1/platform/auth/bootstrap` |
| Audit | `platform.user.password_set`, `credential_unlocked`, `email_verified`, `auth.bootstrap_completed` (no passwords/secrets) |
| Config | `PlatformAuthentication:Password\|Lockout\|Bootstrap` (bootstrap **Enabled: false**, empty `SharedSecret` by default) |

## 2. Bootstrap protection

| Control | Implementation |
|---|---|
| Disabled by default | `appsettings.json` → `Bootstrap:Enabled: false` |
| Explicit secure config | Requires `Enabled=true` **and** `SharedSecret` ≥ 32 chars + admin identity fields |
| One-time trusted secret | Header `X-ExItS-Bootstrap-Secret`; constant-time compare via `BootstrapSecretComparer` |
| Refuse after eligible Platform admin exists | Active `PlatformAdministrator` count → `BootstrapAlreadyCompleted` |
| Concurrency-safe / idempotent | Conflict on second call; `PersistenceConflictException` → already-completed |
| Rate limiting | Named policy `auth-bootstrap`: 5 requests / 15 min / IP |
| No secret logging | Audit summary states password/secret not recorded; failures return generic unauthorized |
| Unsafe Production config rejected | Startup throws if `Bootstrap:Enabled` in non-Dev/Testing; runtime `IsProduction()` → 403 |
| Tests | Secret missing/wrong → 403; Production env → 403; Production+Enabled startup fails; one-shot success then conflict; disabled path |

## 3. Locked access chain preserved

```text
Platform User → Organization Membership → Product Access → Product-Local Role
```

Credentials attach to Platform User only. Bootstrap grants **PlatformAdministrator** (Platform Authz), not product-local roles.

## 4. Explicit exclusions

Login/logout UI; cookies; bearer/refresh tokens; email delivery; MFA; Admin auth middleware; product client auth; SSO/AD.

## 5. Validation

| Check | Result |
|---|---|
| Full Release tests | **1208 passed / 0 failed / 0 skipped** |
| Migration apply/rollback/reapply | Credential migration tests |
| Hasher uses `PasswordHasher<TUser>` | Architecture + unit tests |
| Bootstrap hardening | Integration + architecture tests |
| No foreign product tree | Pass |
| Portfolio independence | Pass |

## 6. Open decisions / risks

| ID | State |
|---|---|
| **R-091** | Open — login/session not implemented |
| **D-P12-03** | Open — commercial transport |
| **D-P12-05** | Open — honest Dev/Production language |
| **D-P13-01…06** | Closed (architecture) |

## Exact next work package

**P13-WP03 — Platform Login, Logout, and Browser Session** when explicitly authorized. Do not begin P13-WP03.
