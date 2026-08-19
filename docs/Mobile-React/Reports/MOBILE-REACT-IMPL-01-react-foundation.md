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
