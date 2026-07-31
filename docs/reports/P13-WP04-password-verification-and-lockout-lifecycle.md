# P13-WP04 — Password, Verification, and Lockout Lifecycle

Phase marker: `P13-WP04-password-verification-and-lockout-lifecycle`

Package: **P13-WP04 — Password Lifecycle, Lockout, and Verification**
Prior tip: `cdf109232ccb7e75bfd9443a0ec221b2150acdfc`
Feature tip: `65b261eca7353a7efea2f8f1899c252f0dcee6dc`

## Status

**Complete.** Authenticated password change, forgot/reset with hashed one-time tokens, email-verification token workflow (delivery boundary documented), lockout/unlock evidence, session revocation after sensitive credential changes, Admin UI, audits, and tests. **R-091 remains open** (bearer tokens, product-client wiring, MFA, closeout remain).

Exact next: **P13-WP05 — Trusted API Actor and Organization Context** when authorized (do **not** begin).

## 1. Delivered capability

| Area | Evidence |
|---|---|
| Change password | `POST /api/v1/platform/auth/change-password` (session required; current password checked; sessions revoked) |
| Forgot / reset | `POST .../forgot-password`, `POST .../reset-password`; tokens in `platform_credential_tokens` (hash only, single-use, TTL) |
| Email verification | `POST .../email-verification/request\|confirm` + admin mark-verified retained |
| Lockout / unlock | Login lockout audit; admin unlock API + Users UI |
| Session invalidation | `RevokeAllActiveForUserAsync` after change/reset/admin set-password; stamp rotation retained |
| Outbound delivery boundary | `IPlatformAuthOutboundMessageSink` default no-op (no email vendor); `ExposeDebugTokens` Dev/Testing only (Production startup rejects) |
| Admin UI | Change password, forgot/reset pages; Users credential panel; login forgot link |
| Audit | password_changed, password_reset_*, email_verification_*, lockout_started, session_revoked |
| Rate limits | `auth-password-reset` 10 / 15 min / IP |

## 2. Email delivery boundary (honest)

No SMTP/vendor integration is selected. Tokens are created and hashed; outbound sink logs kind/user/expiry without secrets. Non-Production may expose `debugToken` when configured. Production forbids debug token exposure.

## 3. Locked access chain preserved

```text
Platform User → Organization Membership → Product Access → Product-Local Role
```

## 4. Explicit exclusions

MFA; bearer tokens; org switching; product launch protection; external IdP; broad Authz matrix rewrite; real email vendor.

## 5. Validation

| Check | Result |
|---|---|
| Full Release tests | **1220 passed / 0 failed / 0 skipped** |
| Migration apply/rollback/reapply | `CredentialTokenMigrationTests` |
| Change/reset/verify/lockout | `ApiCredentialLifecycleTests` |

## Exact next work package

**P13-WP05 — Trusted API Actor and Organization Context** when explicitly authorized. Do not begin P13-WP05.
