# MOBILE-REACT-IMPL-03A — Browser auth + Local Validation unblock

**Package:** MOBILE-REACT-IMPL-03A  
**Date:** 2026-08-19  
**Branch:** `feat/mobile-react-client`  
**Starting HEAD:** `6b24b18c7efbf191b2a4e2bd5326f669163ad4ef`  
**Starting `origin/main`:** `5979a9ce008bb24a3257abd28ae79bc1a5a9b569`

Does **not** rewrite DOC-08, AMEND-01/02/03, APPROVAL, MERGE-01, IMPL-01, or IMPL-02 reports.

---

## Status

| Item | Status |
|---|---|
| Original IMPL-03 | **BLOCKED** — API/session transport (direct Browser/PWA → Platform `:8091` cookie auth) |
| IMPL-03A | **Browser same-origin auth foundation** |
| Local Validation | Existing **Full** seed reused |
| Extra test fixture | **Not added** |
| Production cookie security | **Unchanged** (`Secure = true`) |
| Cross-origin CORS expansion | **NONE** |
| MOBILE-D-060 | **OPEN** |
| IMPL-03 | **NOT COMPLETE** — resume only after 03A approval |
| PWA production rollout | **NOT AUTHORIZED** |
| Capacitor / PIN / workspace UI / selling | **NOT IN THIS PACKAGE** |

---

## Gap (verified in source, not assumed from IMPL-03)

Direct cross-origin Browser/PWA auth was blocked:

1. React defaulted to `http://127.0.0.1:8091` (cross-origin from `:5175`).
2. Platform CORS allowlist is Admin `:8090`, Org `:8093`, Personal `:8094` only. No `:5175` / `:4175` / `10.0.2.2`.
3. `AuthEndpoints.AppendSessionCookie` set `Secure = !(Development || Testing)`. Local Validation runs **Staging over HTTP**, so the browser dropped `.ExItS.Platform.Auth`.
4. Existing Admin/Org/Personal hosts are BFFs using `ExItSLocalValidationCookies`. Platform API login cookies were not aligned with that helper.
5. Login JSON still returns `sessionToken`. A Browser/PWA client must not persist it.

---

## Architecture

```
BROWSER
  → SAME ORIGIN React host / `/platform-api/*` proxy
  → Platform API
  → HttpOnly `.ExItS.Platform.Auth` cookie
```

React JavaScript ignores login JSON `sessionToken`. It is never written to `localStorage`, `sessionStorage`, IndexedDB, Cache Storage, the URL, logs, or diagnostics.

Capacitor bearer / native secure storage remains a later separate concern.

### Local development proxy

Browser request:

`http://localhost:5175/platform-api/api/v1/platform/auth/login`

Vite proxy (loopback only):

`http://127.0.0.1:8091/api/v1/platform/auth/login`

Target comes from `EXITS_PLATFORM_API_PROXY_TARGET` (server-side, not `VITE_`). Default `http://127.0.0.1:8091`. Non-loopback destinations are rejected.

POS is not proxied.

### Future production host (not rolled out)

```
React/PWA HTTPS origin
  /                 → React static application
  /platform-api/*   → Platform API reverse proxy
```

This package adds the configuration seam (`/platform-api` default base URL). It does **not** deploy nginx, containers, or production PWA hosting.

---

## Local Validation cookie policy

Aligned with `ExItSLocalValidationCookies.AllowHttpAuthCookies`:

| Case | `Secure` |
|---|---|
| Production | `true` |
| HTTPS when HTTP cookies are allowed | `true` (SameAsRequest) |
| Explicit Local Validation HTTP (`LocalValidation:Enabled=true`, non-Production, HTTP) | `false` |
| Generic Staging HTTP without Local Validation | `true` (fail closed) |
| Development / Testing HTTP | `false` (existing AllowHttpAuthCookies) |

Unchanged: cookie name `.ExItS.Platform.Auth`, `HttpOnly`, `SameSite=Lax`, Path `/`, expiry, session validation.

---

## Seed

`tools/Start-LocalValidation.ps1 -SeedScope Full` catalog reused (Maria Santos, Luis Navarro, and the rest). No extra fixture. Password remains `LOCAL_VALIDATION_SHARED_PASSWORD` in the gitignored env. It is not printed, committed, or exposed through React.

Test User dropdown is **not** in 03A (leave for resumed IMPL-03). Username-only fill, never password/auto-login.

---

## Explicitly not delivered

- Workspace resolver / branch chooser / Personal Home
- PIN / trusted device
- Offline LocalStore / outbox / sync
- Cart / checkout / selling
- Capacitor / Android native / MAUI changes
- DB migration
- New Production authentication model
- Production PWA rollout
- Platform CORS expansion for React origins

---

## Validation

Evidence is recorded in the IMPL-03A closeout report in chat after restore/build/test. Required checks:

- Platform `PlatformSessionCookiePolicyTests` + `PlatformSessionCookieAppendTests`
- Client `typecheck`, `lint`, `format:check`, `test`, `build`, `test:pwa`, `test:e2e`
- Real Local Validation Maria + Luis password login through same-origin `/platform-api`
- Set-Cookie `.ExItS.Platform.Auth` HttpOnly, SameSite=Lax, not Secure on Local Validation HTTP
- `auth/me` after browser refresh; no `sessionToken` in JS storage or PWA Cache Storage
- `git diff --check` and `scripts/git/pre-commit-check.ps1`

Android emulator Chrome (`http://10.0.2.2:5175`) was not exercised in this package.

Queue: **STOPPED AFTER MOBILE-REACT-IMPL-03A**. Do not resume IMPL-03 until 03A is approved.
