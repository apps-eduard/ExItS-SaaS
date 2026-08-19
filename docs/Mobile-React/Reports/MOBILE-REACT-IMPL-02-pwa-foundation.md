# MOBILE-REACT-IMPL-02 — PWA static shell foundation

**Package:** MOBILE-REACT-IMPL-02  
**Date:** 2026-08-19  
**Branch:** `feat/mobile-react-client`  
**Starting HEAD:** `4a5217bd8e76d6019aa6fef948a88077b718cb75`  
**Starting `origin/main`:** `5979a9ce008bb24a3257abd28ae79bc1a5a9b569`

Does **not** rewrite DOC-08, AMEND-01/02/03, APPROVAL, MERGE-01, or IMPL-01 reports.

---

## Authorization

| Item | Status |
|---|---|
| Gate C React foundation | **COMPLETE** (IMPL-01 / IMPL-01A) |
| Gate D PWA foundation | **AUTHORIZED + COMPLETE** after validation |
| PWA production rollout | **NOT AUTHORIZED** |
| Gate E+ | **NOT AUTHORIZED** |
| Capacitor | **NOT AUTHORIZED** |
| MAUI retirement | **NOT AUTHORIZED** |
| MOBILE-D-060 | **OPEN** |

---

## Delivered

Installable PWA static shell on the existing React client:

`src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Client/`

- Web app manifest (`ExItS Mobile` / `ExItS Mobile`, `display: standalone`, `start_url: /`)
- Restrained ExItS theme/background (`#166534` / `#eef3f0`)
- 192 / 512 PNG icons plus maskable variants
- `viewport-fit=cover` and safe-area padding on the shared shell
- Production service worker via `vite-plugin-pwa` (`registerType: prompt`)
- Precache of hashed JS/CSS/static assets and app-shell HTML
- Compact **New version available** / **Refresh** notice (EN + fil-PH)
- No surprise reload; apply is explicit; a guard hook exists for later dirty-state blocking
- Build/release identifier remains in Copy Diagnostics (`VITE_APP_VERSION` / `0.0.1-impl-02`)

Browser use remains first-class. No custom Install App button.

---

## Service-worker boundary

The service worker may cache **static application assets only**.

It does **not** cache as data/offline SoR:

- `/api/**`
- Platform API (`:8091`) or POS API (`:8092`) responses
- auth/session, sales, payments, cart, customers, inventory, entitlements, workspace, outbox, or financial payloads

No Background Sync financial queue. No IndexedDB financial store. No Cache API business records. No TanStack Query persistence. No offline mutation replay.

The service worker is **not** LocalStore.

---

## Explicitly not delivered

- Authentication / PIN / trusted-device enrollment
- Workspace or product chooser
- Cart / checkout / selling
- Offline LocalStore / outbox / sync
- PWA production rollout
- Capacitor / Android / iOS project
- MAUI, backend, DB/migrations, POS domain, PLM

Gate E visual approval is **not** claimed.

---

## Validation

From the Client package: typecheck, lint, format:check, Vitest, production build, `npm run test:pwa`, Playwright (including axe). See the IMPL-02 closeout report in chat for counts and SHAs.
