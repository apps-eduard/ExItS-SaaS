# POS-REACT-IMPL-02 — PWA Static Shell

**Package:** POS-REACT-IMPL-02  
**Status:** Complete on `feat/pos-react-client`  
**Gate:** D Phase A only (static cache). Full Gate D / browser auth is **not** complete.  
**Phase 1 SHA:** `13fbd8db23fed8908172d8bfb579694f3fb2c561`  
**Commit:** `feat(pos-react): add pwa static shell`

## Installability

- Manifest `name`: Pinoy Business POS
- `short_name`: ExItS POS (MAUI `ApplicationTitle`)
- `start_url`: `/`
- `display`: `standalone`
- `theme_color`: `#166534`
- `background_color`: `#f3f6f4`
- Icons: 192 / 512 plus maskable variants, generated from the existing MAUI/ExItS green `#166534` mark (not a new company logo)
- Browser tab remains fully supported. No custom install modal.

## Service worker

- `vite-plugin-pwa` `generateSW`, `registerType: prompt`, `injectRegister: false`
- Filename: `sw.js`
- Precache: hashed JS/CSS, HTML, icons, manifest
- Runtime: Google Fonts `CacheFirst` in `pos-static-fonts` only
- `/api/` and `/(auth|session)/` : **NetworkOnly**
- Platform/POS reverse-proxy aliases: **not configured** in this package; `/platform-api` is absent on purpose
- Background Sync: **absent**
- IndexedDB / OPFS / SQLite / LocalStore in the worker: **absent**

## Connectivity

Advisory browser online/offline only. English: “You're offline” / “Reconnect to continue.” Filipino: “Wala kang koneksyon” / “Kumonekta ulit para magpatuloy.” This is not Offline POS mode, authentication, or entitlement truth.

Offline reload keeps the neutral foundation shell. It does not claim the user is authenticated or that selling/checkout is available.

## Update lifecycle

Prompt-style “Update available” / “Refresh”. No silent force refresh. `canApplyPwaUpdate()` is the future cart/checkout guard seam; it currently returns `true` because no cart exists. Repeated Refresh clicks do not re-enter apply. Registration failure does not crash the app.

## Storage

Persistent UI preferences only: `exits.pos-client.ui-preferences.v1` (theme/locale). No password, session, refresh, or Bearer tokens.

## Ports and APIs

Preview evidence: `127.0.0.1:4177`. Dev remains `5177`. Platform API 8091 and POS API 8092 were **not** started. No live API claims.

## Evidence

`docs/Mobile-React/Reports/impl-pos-react-02-pwa-static-shell/`

- `01-online-shell-375x812.png`
- `02-offline-shell-375x812.png`
- `03-update-available-375x812.png`
- `04-online-shell-1440x900.png`
- `05-offline-shell-1440x900.png`

No secrets. No fake financial data.

## Tests

| Command | Result |
|---|---|
| `npm ci` | PASS |
| `npm run typecheck` | PASS |
| `npm run lint` | PASS (0 errors) |
| `npm run format:check` | PASS |
| `npm run test` | PASS — 25 Vitest tests |
| `npm run build` | PASS |
| `npm run test:pwa` | PASS |
| `npm run test:e2e` | PASS — Playwright including manifest, SW, static cache, no API URLs in Cache Storage, offline/reconnect, update notice, EN/fil-PH, 320/375/768/1440, axe serious/critical = none |

## Scope

| Surface | Status |
|---|---|
| POS React Client | Changed |
| docs/Mobile-React | Narrow report/status/evidence |
| MAUI / Organization Web / POS API / backend / LocalStore | Unchanged |
| Platform / PWEB / PLM | Unchanged |
| DB/migrations | None |
| Capacitor / Android | Absent |
| Authentication | Not implemented |
| Financial offline / command replay | Absent |
| Background Sync | Absent |

## Pattern checkpoint

`PLM_PWA_PATTERN_REVIEW_REQUIRED`: **SATISFIED FOR ENGINEERING PATTERNS** (static cache safety, update lifecycle, connectivity, responsive/PWA tests). PLM auth, routes, and business logic were not copied.

## Open blockers (unchanged)

- `PWEB20_CSRF_COMPATIBILITY_REVIEW_REQUIRED`: YES — do not start POS-REACT-IMPL-03
- `TYPED_CLIENT_GENERATION_CONTRACT_MISSING`: OPEN
- `MOBILE-D-060`: OPEN
