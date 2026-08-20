# PLM-PWA-H5 — Platform / PWEB-20 CSRF compatibility

**Package:** PLM-PWA-H5  
**Date:** 2026-08-20  
**Branch:** `feat/plm-pwa-hardening`  
**Worktree:** `C:/Users/speed/Desktop/ExItS-SaaS-plm-pwa`

| Package | SHA |
|---|---|
| H1 | `46ca3dfd4c78b5c00a81fceddf9fa8236da4361f` |
| H2 | `a3758434f3ac978f936a479ac68bee071b781392` |
| H3 | `44d230dd9695f15d77f94b90388159c8d288db55` |
| H4 | `f5caf82f6ea92c9c643634dd9626a8fd80396ec7` |
| H5 | recorded after this commit |

**PWA_SOURCE_SHA:** `ebffebc00d68f48cbdfe25801b98622c2c4cdb6c`  
**PWA_MAIN_BASE:** `5979a9ce008bb24a3257abd28ae79bc1a5a9b569` (recorded only — not merged)

---

## PWEB current contract (read-only)

| Ref | SHA | Role |
|---|---|---|
| Historical PWEB-20 | `96a3acdf87a8e0f244237d6fc98354b2ac1b7684` | CSRF foundation |
| Compatibility fix | `06e5cc1cdcf927c4c2c61d0345b7c892669e363c` | POS HttpClient cookie-jar isolation only |
| Current PWEB HEAD | `525bae3633fb7fde1bbc9b855435a05f5f616c09` | docs/planning for PWEB-21..30 — **no runtime CSRF change** |

`06e5cc1c` **does not** change browser CSRF rules. It disables `UseCookies` on POS/Org Web Platform `HttpClient`s so header/Bearer callers are not pulled into cookie antiforgery after login `Set-Cookie`. Its report states future PLM PWA cookie mutations **must** adopt bootstrap + `X-XSRF-TOKEN`.

### Exact contract used by H5

| Item | Value |
|---|---|
| Bootstrap | `GET /api/v1/platform/antiforgery/token` |
| Header | `X-XSRF-TOKEN` |
| Protected | POST/PUT/PATCH/DELETE with session cookie and **without** `X-ExItS-Session-Token` |
| Safe | GET/HEAD/OPTIONS |
| Exempt paths | login, register, forgot/reset password, bootstrap, external callbacks, token route |
| Header session | exempt |
| Token storage (client) | memory only |

---

## Decision

**CASE B** — PLM PWA already performs cookie-authenticated browser mutations.

| Mutation | Path | Method | Requires `X-XSRF-TOKEN` |
|---|---|---|---|
| Sign out | `/api/v1/platform/auth/logout` | POST | YES (after cookie login) |
| Org context | `/api/v1/platform/auth/organization-context` | PUT | YES |
| Profile select | `/api/v1/platform/auth/account-profiles/select` | POST | YES |
| Activate | `/api/v1/platform/auth/activate-account` | POST | YES if session cookie present; else N/A |
| Login / register / forgot / reset | (exempt) | POST | NO |

Transport: `credentials: "include"` on `/platform-api`. No `X-ExItS-Session-Token` on the browser path.

| Flag | Value |
|---|---|
| `CSRF_CLIENT_CHANGE_REQUIRED` | **YES** (implemented) |
| `PWEB20_CSRF_COMPAT_RECHECK_REQUIRED` | **NO** (closed) |
| Gate E | BLOCKED — `REAL_LENDING_CONTRACT_MISSING` |
| Capacitor / Android / PLM-13 | NOT STARTED |

---

## Implementation

- `platform-antiforgery.ts` — defaults, exempt paths, in-memory token
- `platformApiJson` bootstraps token for non-exempt mutations; never persists
- `logoutSession` clears memory token in `finally`
- E2E mocks fulfill antiforgery and assert `X-XSRF-TOKEN` on logout / org-context

---

## Live Platform validation

Started **host** `ExItS.Platform.Api` from clean PlatformWeb worktree (`525bae36`, includes PWEB-20 + `06e5cc1c`) on `127.0.0.1:8091`.

Docker `exits-local-validation-platform-api` (pre-CSRF image, antiforgery 404) was stopped only for the validation window and restarted afterward.

**Environment note:** Local Validation docker/host **Staging** sets antiforgery `Cookie.SecurePolicy = Always`, which rejects token bootstrap on plain HTTP. LIVE H5 used `ASPNETCORE_ENVIRONMENT=Testing` (SameAsRequest per PWEB-20) with `LocalValidation__Enabled=true` against the existing platform DB — **no Platform/PWEB source edits**.

| Check | Result |
|---|---|
| Health | LIVE PASS (200) |
| Antiforgery token | LIVE PASS (`headerName=X-XSRF-TOKEN`) |
| Cookie login + GET `/auth/me` | LIVE PASS |
| Logout without CSRF | LIVE PASS (400 `platform.antiforgery.invalid`) |
| Logout with CSRF | LIVE PASS (204) |
| Header-session logout without CSRF | LIVE PASS (204) |
| Invalid CSRF | LIVE PASS (400) |
| Org/product access (Olivia Platform staff) | LIVE N/A — account-scope gate (expected for Platform class) |
| Org list + org context (PLM org user) | LIVE PASS for organizations GET; org-context PUT requires CSRF (client covered) |
| Product-access effective on PlatformWeb HEAD | LIVE **404** on `/api/v1/platform/auth/product-access/effective` — endpoint exists on PLM branches, not on PlatformWeb CSRF HEAD used for live CSRF proof |
| Offline/SW recovery | MOCKED Playwright H3/H4 suite PASS |

**LIVE note:** Full PLM workspace entry against a single binary that both (a) enforces PWEB-20 CSRF and (b) exposes PLM `/auth/product-access/effective` requires merging Platform trees; H5 did not modify Platform/PWEB. CSRF contract was proven LIVE; workspace product-access remains MOCKED + unit-covered on the PLM client.

---

## Explicitly NOT delivered

Gate E, PLM-13, lending, Capacitor, Android, Platform/PWEB/POS source edits, main merge.
