# P14-WP02A — Live Preview Test Users and Quick Login

Phase marker: `P14-WP02A-live-preview-test-users-and-quick-login`

Package: **P14-WP02A Gap Fix — Quick Login and Login CSS**
Prior tip: `a0c7aaa2cf5154d096d45b8b978426ff122c814b`
Feature tip: `5f2ffabc75645e583eb4c12be9217b90abff9d0c`

## Status

**Complete.** Live-preview login at `http://localhost:8090/admin/login` loads CSS/JS anonymously, lists allowlisted test identities, and creates a real Platform Admin cookie session via HTTP form POST (membership/org context preserved). **Not Production.** **P14-WP03 not started.**

## Root cause (gap fix)

| Defect | Cause |
|---|---|
| Broken login CSS / dead Blazor | `FallbackPolicy` + `MapStaticAssets()` required auth → `/app.css`, fingerprinted CSS, and `_framework/blazor.web.js` redirected to `/admin/login` |
| Quick-login did not sign in | Interactive Server `SignInAsync` cannot set cookies after the response has started; with assets blocked, the circuit never started |

## Fix

| Change | Evidence |
|---|---|
| Anonymous static assets | `MapStaticAssets().AllowAnonymous()` |
| Cookie login via HTTP POST | `POST /admin/login/credentials`, `POST /admin/login/live-preview` → Platform API session + Admin cookie → redirect `/admin` |
| Login UI | SSR forms (no InteractiveServer on login); antiforgery token; identities still from Platform API |
| DataProtection (live preview) | File-system key ring under `DataProtection:KeysPath` (`/tmp/exits-admin-dp-keys` in compose) |

## Validation

| Check | Result |
|---|---|
| `GET` fingerprinted CSS / `blazor.web.js` | **200** (no login redirect) |
| Identities in login HTML | 5 allowlisted options |
| `POST /admin/login/live-preview` (`platform-admin`) | **302** → `/admin` + `.ExItS.Admin.Auth` cookie |
| Dashboard as platform-admin | **200**; chip `preview-platform-admin`; org context **No organization** |
| Dashboard as org-admin | **200**; org context **Preview Organization A** |
| Full Release tests | **1267 passed / 0 failed / 0 skipped** |

## Explicit exclusions

- P14-WP03 TLS/proxy
- Production `LivePreview:Enabled`
- Packaging compose port/DB changes
- Phase 15 Admin nav/user UX

## Exact next

**P14-WP03 — Reverse Proxy, TLS, and Network Hardening** when authorized.
