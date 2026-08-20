# POS React Mobile Sign-In Consistency Audit

**Branch:** `feat/pos-react-client`  
**Scope:** Duplicate/stale mobile sign-in UI + Android emulator login failure investigation  
**Not in scope:** WP06 / cash checkout / POST `/sales` / Capacitor / MAUI

## Inventory — sign-in implementations found

### A (canonical — live)

- **Path:** `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Client/src/features/auth/SignInPage.tsx`
- **Route:** `/sign-in` (only route entry in `src/app/router.tsx`)
- **Used by:** `GuestOnly` guest tree; all desktop + mobile browsers loading this app
- **Helpers:** `TestUserSelector.tsx` (conditional Development Tools)

### B (not a sign-in page)

- **Path:** `src/features/foundation/FoundationPage.tsx`
- **Route:** none (not wired in `appRoutes`)
- **Used by:** unused foundation leftover — not an alternate login

**Duplicate SignIn component:** NO  
**Duplicate `/sign-in` route:** NO

English and Filipino both use the same `SignInPage` component; only `t(...)` strings change.

---

## ROOT CAUSE OF TWO DIFFERENT UIs

**Primary:** Stale **service worker / Cache Storage** on the Android emulator origin (`http://10.0.2.2:5177`) serving an **older precached shell** (often after a prior `vite preview` / production build visit, or a previously registered worker), while desktop Chrome on `http://127.0.0.1:5177` loads live Vite modules.

Evidence:

1. Repository has a **single** `/sign-in` → `SignInPage` route (no mobile alternate).
2. Live Vite on `127.0.0.1:5177` serves current worktree source (`feat/pos-react-client`).
3. Before this fix, Vite **SPA-fallback returned `index.html` for `/sw.js` (HTTP 200)**, which is unsafe if any worker update cycle targets `/sw.js` during development.
4. `vite-plugin-pwa` has `devOptions.enabled: false` (no intentional DEV SW), but `PwaUpdateHost` previously still imported `registerSW` on every boot; leftover **production** workers for the same origin are not cleared automatically.
5. Development Tools missing on one “UI” is expected when Local Validation identities are unavailable **or** when an older bundle without `TestUserSelector` is served — not a second React page.

**Contributing visual difference (same component):** locale `en` vs `fil-PH` changes copy only.

| Probe | Result |
|---|---|
| Duplicate SignIn component | NO |
| Duplicate route | NO |
| Stale service worker / cache | YES (emulator-side; addressed) |
| Stale build (wrong worktree) | NO for current `5177` Vite process |
| Different dev server | NO — PID serves this client on `127.0.0.1:5177` |
| Mobile API config (absolute localhost in client) | NO — relative `/platform-api` |

---

## LOGIN FAILURE ROOT CAUSE

**Primary (proven with live successful login probes):** Platform API `Set-Cookie` for `.ExItS.Platform.Auth` includes the **`Secure`** flag (live-preview container is not ASP.NET “Development”, so cookies are marked Secure).

| Browser origin | HTTP + `Secure` cookie |
|---|---|
| `http://localhost:5177` / `http://127.0.0.1:5177` | Chrome **accepts** (localhost exception) → PC login works |
| `http://10.0.2.2:5177` | Chrome **rejects** Secure cookie on plain HTTP → session cookie never stored → subsequent `/me` / workspace calls fail |

Same React source and same login endpoint (`POST /platform-api/.../login` returns **200** for all three Host headers). The failure is **cookie acceptance**, not a second SignIn page.

**Secondary (earlier):** Stale service worker / cache could also serve old JS; addressed separately.

**Hardening:**

- `cookieDomainRewrite: ""` on Platform/POS Vite proxies
- Vite proxy strips `; Secure` from `Set-Cookie` in local HTTP proxy responses so emulator `10.0.2.2` can persist the session cookie

---

## Fixes delivered

1. DEV boot: unregister leftover service workers + clear Cache Storage; one-time reload (`dev-service-worker-guard.ts`)
2. DEV Vite middleware: **404** for `/sw.js`, `/dev-sw.js`, `/workbox-*.js` (no HTML SPA fallback)
3. `PwaUpdateHost`: do **not** call `registerSW` when `import.meta.env.DEV`
4. Vite `allowedHosts` includes `10.0.2.2`
5. Proxy `cookieDomainRewrite: ""` for Platform + POS proxies
6. Canonical sign-in tests + audit evidence

Production/preview PWA behavior remains enabled on build/preview (`4177`).

---

## Canonical references

| Item | Value |
|---|---|
| Canonical SignIn path | `.../src/features/auth/SignInPage.tsx` |
| Canonical route | `/sign-in` |
| Desktop frontend URL | `http://127.0.0.1:5177` |
| Mobile emulator frontend URL | `http://10.0.2.2:5177` |
| API from mobile | same-origin `http://10.0.2.2:5177/platform-api/...` → Vite proxy → `127.0.0.1:8091` |
| Desktop + mobile same component | YES |
| EN + fil-PH same component | YES |

## Automated validation (this change)

| Check | Result |
|---|---|
| `npm run typecheck` | PASS |
| `npm run lint` | PASS (0 errors; existing react-refresh warnings only) |
| `npm run format:check` | PASS |
| `npm run test` (Vitest) | PASS — 23 files / 77 tests |
| Playwright `sign-in-canonical` + `auth-session` + `foundation` | PASS — 16 tests |
| `npm run build` | PASS |
| DEV `GET /sw.js` | **404** (blocked; no SPA HTML fallback) |
| `git diff --check` | PASS |

Canonical sign-in e2e covers EN/fil-PH same `data-testid="sign-in-page"`, Development Tools when LV fixtures are present, and viewports 320 / 375 / 1440.

## Emulator validation checklist (manual)

1. Restart Vite after pulling this commit (`npm run dev`)
2. In emulator Chrome for `http://10.0.2.2:5177`: first load should auto-unregister leftover SW + clear caches + reload once; otherwise Site settings → clear data
3. Confirm modern `SignInPage` + Development Tools (when Local Validation enabled)
4. Toggle EN ↔ Filipino — same layout
5. Sign in → workspace → shell → refresh → sign out → Back

**Same source/build:** YES — emulator `10.0.2.2:5177` → host `127.0.0.1:5177` Vite DEV for this worktree.

## WP06

**Not started.** Pay remains disabled. No POST `/sales`.
