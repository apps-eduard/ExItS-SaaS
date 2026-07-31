# P13-WP08 — Google and Facebook External Authentication

Phase marker: `P13-WP08-google-facebook-external-authentication`

Package: **P13-WP08 — Google and Facebook External Authentication**
Prior tip: `e973efaecc656966d7fddcc6196c84680b30e1f0`
Feature tip: `FEATURE_TIP_PLACEHOLDER`

## Status

**Complete.** Google and Facebook external authentication create or link a Platform User and issue the existing browser session. Social login never grants Platform Administrator, membership, entitlement, or product-local roles. **R-091 remains open** (Phase 13 closeout).

Exact next: **P13-WP09 — Phase 13 Closeout** when authorized (do **not** begin).

## 1. Delivered capability

| Area | Evidence |
|---|---|
| Persistence | `platform_external_logins` (provider + subject unique → user); migration apply/rollback/reapply |
| Use case | `CompleteExternalLogin` — find/link/create user; external-only credential stamp; session issue; verified email required |
| OAuth | Google/Facebook packages; challenge + complete; disabled by default; Production requires secrets when Enabled |
| Testing | Dev/Testing `POST /auth/external/testing/complete` (Production forbidden) |
| Admin | Login Google/Facebook links; `/admin/external-login-callback` establishes Admin cookie from session token |
| Policy | No bootstrap, no role grant, no membership/org/product access on social |

## 2. Locked access chain preserved

```text
Platform User → Organization Membership → Product Access / Entitlement → Product-Local Role
```

External auth proves Platform identity only.

## 3. Explicit exclusions

Utang product features; automatic organization creation; trials/subscriptions; product roles; MFA enrollment; Phase 13 closeout; claiming Production-ready / closing R-091 alone.

## 4. Validation

| Check | Result |
|---|---|
| Full Release tests | **1250 passed / 0 failed / 0 skipped** |
| Unit | `PlatformExternalLoginTests` |
| Integration | `ApiExternalAuthTests`, `ExternalLoginMigrationTests` |

## Exact next work package

**P13-WP09 — Phase 13 Closeout** when explicitly authorized. Do not begin P13-WP09.
