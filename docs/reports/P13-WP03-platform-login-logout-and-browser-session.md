# P13-WP03 — Platform Login, Logout, and Browser Session

Phase marker: `P13-WP03-platform-login-logout-and-browser-session`

Package: **P13-WP03 — Platform Login, Logout, and Browser Session**
Prior tip: `2bec853da67114905bbdf690a0fe2b3568ffc728`
Feature tip: _(recorded after commit)_

## Status

**Complete.** Platform browser login/logout, server-side hashed session store, HttpOnly session cookie + header forwarding, sliding/absolute expiry, security-stamp invalidation, trusted authenticated actor from session, Admin login UI (Production-required), and authentication audit events. **R-091 remains open** until remaining Phase 13 WPs close the full production-auth gap (password lifecycle UX, API actor enforcement beyond Dev Operator, product client wiring, MFA readiness, closeout).

Exact next: **P13-WP04 — Password Lifecycle, Lockout, and Verification** when authorized (do **not** begin).

## 1. Delivered capability

| Area | Evidence |
|---|---|
| Session aggregate | `PlatformAuthSession` + `platform.platform_auth_sessions` (`AddPlatformAuthSessions`) |
| Token handling | Opaque token once at login; SHA-256 hash stored; never logged |
| Login / logout / me | `POST/GET /api/v1/platform/auth/login\|logout\|me` |
| Cookie | `.ExItS.Platform.Auth` HttpOnly, SameSite=Lax, Secure outside Dev/Testing |
| Cross-origin Admin | Session token also returned in JSON; Admin cookie + `X-ExItS-Session-Token` forwarder |
| Renewal / expiry | Sliding idle (default 30m) capped by absolute lifetime (default 12h) |
| Invalidation | Logout revoke; stamp mismatch after password change; expired/revoked rejected |
| Trusted actor | Authenticated `PlatformSession` principal preferred by `DevelopmentPlatformActorAccessor` |
| Admin UI | `/admin/login`, logout routes; Production fallback requires authenticated user |
| Audit | `platform.auth.login_succeeded`, `login_failed`, `logout` (no secrets) |
| Rate limit | `auth-login` 20 / 15 min / IP |

## 2. Locked access chain preserved

```text
Platform User → Organization Membership → Product Access → Product-Local Role
```

Login authenticates Platform User only. No org switching, product launch protection, or product-local roles.

## 3. Explicit exclusions

Password reset/change UX delivery; email verification delivery; MFA; bearer/refresh tokens for MAUI; org context switching; broad Admin permission-matrix rewrite; product launch gates.

## 4. Security limitations (honest)

- Dev/Testing Admin remains usable without login (Dev Operator); Production Admin requires cookie auth.
- Platform APIs still allow DevelopmentOperator full access in Development/Testing without a session.
- R-091 not closed by this WP alone.

## 5. Validation

| Check | Result |
|---|---|
| Full Release tests | **1215 passed / 0 failed / 0 skipped** |
| Migration apply/rollback/reapply | `AuthSessionMigrationTests` |
| Login/me/logout/suspended | `ApiSessionAuthTests` |
| Portfolio independence | Pass |

## Exact next work package

**P13-WP04 — Password Lifecycle, Lockout, and Verification** when explicitly authorized. Do not begin P13-WP04.
