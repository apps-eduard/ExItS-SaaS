# MOBILE-REACT-IMPL-01 — React Mobile Client foundation

**Package:** MOBILE-REACT-IMPL-01  
**Date:** 2026-08-19  
**Branch:** `feat/mobile-react-client`  
**Starting `origin/main`:** `5979a9ce008bb24a3257abd28ae79bc1a5a9b569`

Does **not** rewrite DOC-08, AMEND-01/02/03, APPROVAL, or MERGE-01 reports.

---

## Delivered

Gate C scaffold at:

`src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Client/`

- React + TypeScript strict + Vite + Tailwind
- React Router, TanStack Query, Lucide
- Vitest, Testing Library, Playwright, axe
- Shared AppShell / AppTopBar
- System default theme; Light / Dark
- `en` default; `fil-PH` secondary
- Comfortable page density; compact cashier chrome; touch targets ≥ 44 CSS px
- Typed Platform + POS HTTP stubs (correlation ID, problem+json, AbortSignal; no token storage)
- Allowlist Copy Diagnostics + ErrorBoundary
- Foundation Home placeholder (not live POS)

Preview screenshots (not Gate E):

`docs/Mobile-React/Reports/impl-01-previews/`

---

## Explicitly not delivered

- Authentication / PIN
- Workspace chooser / product launcher
- Selling / cart / checkout
- Offline database / outbox
- PWA manifest / service worker
- Capacitor / Android
- MAUI, backend, DB/migrations, POS domain, PLM

Gate D+ remain **NOT AUTHORIZED**.

MOBILE-D-060 remains **OPEN**.

---

## MOBILE-REACT-IMPL-01A — Diagnostics / connectivity correction

**Status:** COMPLETE
**Date:** 2026-08-19
**Starting HEAD:** `b476c8fb956c6d9ad37775e575c83e710bd53fb4`

Correction before IMPL-02. Does **not** authorize Gate D+.

### Connectivity

- AppTopBar no longer claims Online / Offline / Syncing.
- Neutral **Preview** label only — not operational API health.
- Foundation sample Online chip removed.
- `navigator.onLine` is modeled as browser network reachability only and is not treated as ExItS API health.
- Full API-reachability monitoring is **not** implemented in this package.

### Diagnostics

- Copy Diagnostics is allowlist-only. Arbitrary `Error.message`, API problem title/detail, bodies, payloads, and stacks are **not ingested**.
- Runtime/API copied messages are generic controlled strings.
- Safe fields only: category, HTTP status, namespaced `errorCode`, request correlation ID, error reference, pathname (no query/hash), app/build version, locale/theme, compact platform class, timestamp.
- Independent sentinel tests cover email, phone, customer name, GCash/financial text, PIN, token, session secret, and raw stack dump.

Gate D+ remains **NOT AUTHORIZED**.
