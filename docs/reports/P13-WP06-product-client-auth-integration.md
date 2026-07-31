# P13-WP06 — Product Client Auth Integration (Admin + MAUI/POS)

Phase marker: `P13-WP06-product-client-auth-integration`

Package: **P13-WP06 — Product Client Auth Integration (Admin + MAUI/POS)**
Prior tip: `0cad6f9674c0f0e98dadc06081efc9e951517702`

## Status

**Complete.** Platform-issued opaque Bearer access tokens, Admin session→product-entry token exchange, MAUI password login + Bearer attachment, and POS API bearer introspection with membership/product-access revalidation and product-local role resolution. Dev GUID/header paths remain Dev/Testing-only. **R-091 remains open** (MFA readiness, closeout, residual Production hardening).

Exact next: **P13-WP07 — MFA Readiness and Auth Hardening** when authorized (do **not** begin).

## 1. Delivered capability

| Area | Evidence |
|---|---|
| Access tokens | `platform_access_tokens` (hash only); issue password/session grants; bind org+product after evaluate Allowed; introspect; revoke |
| APIs | `POST /auth/token`, `/auth/token/bind`, `/auth/introspect`, `/auth/token/revoke` |
| Admin entry | `/admin/product-entry` issues session-grant token for selected org + `pinoy-business-pos` |
| MAUI | Username/password Platform token grant; SecureStorage access token; Bearer handler; Dev GUID fallback retained |
| POS API | `PosPlatformBearerMiddleware` introspects Platform; actor/org from bearer; commercial from evaluate snapshot; `PosRoleMatrix` after trusted actor |
| Invalidation | Password change/reset/admin set-password revoke sessions + access tokens; membership/org suspend clears bindings |

## 2. Locked access chain preserved

```text
Platform User → Active Organization Membership → Product Access / Entitlement → Product-Local Role
```

Bearer proves Platform identity; org bind requires active membership; product bind requires evaluate Allowed; POS roles remain product-local.

## 3. Explicit exclusions

MFA; external IdP/SSO; new POS business features; broad Authz redesign; closing D-P12-03 commercial transport (evaluate/introspect reused); claiming Production-ready / closing R-091 alone.

## 4. Validation

| Check | Result |
|---|---|
| Full Release tests | **1234 passed / 0 failed / 0 skipped** |
| Token issue/introspect/revoke | `ApiAccessTokenTests` |
| Migration apply/rollback/reapply | `AccessTokenMigrationTests` |

## Exact next work package

**P13-WP07 — MFA Readiness and Auth Hardening** when explicitly authorized. Do not begin P13-WP07.
