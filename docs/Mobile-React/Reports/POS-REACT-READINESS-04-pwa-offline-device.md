# POS-REACT-READINESS-04 — PWA, Offline, and Device Report

**Package:** POS-REACT-READINESS-04  
**Branch:** `docs/pos-react-implementation-readiness`  
**Worktree:** `C:/Users/speed/Desktop/ExItS-SaaS-pos-react-docs`  
**Base `origin/main`:** `5979a9ce008bb24a3257abd28ae79bc1a5a9b569`  
**Depends on:** POS-REACT-READINESS-03 `35bc9514b1a58bc0ec215c9258f77b68923f55e6`  
**Status:** Documentation complete for this package. Implementation is **NOT AUTHORIZED**.

Canonical deliverable: [04-pwa-offline-device-migration.md](../Implementation-Readiness/04-pwa-offline-device-migration.md)

---

## 1. Sequence recorded

| Phase | Content | Gate |
|---|---|---|
| A | PWA static shell: manifest, icons, standalone, safe-area, hashed assets, update prompt, network-only APIs, connectivity UX | D |
| B | LocalStore-equivalent encrypted outbox + cash-only offline sale + projections | F, after storage architecture approval |
| Devices | Map current → PWA degrade → Capacitor; absent MAUI hardware is not a blocker | G |
| Capacitor | Thin host after React/PWA maturity | H |

Service worker cache is **not** the financial store. No Platform/POS API cache-first. No auth/session cache.

---

## 2. Checkpoints

| Checkpoint | Status |
|---|---|
| `PLM_PWA_PATTERN_REVIEW_REQUIRED` | **YES** — compare SW safety, update lifecycle, connectivity UX, PWA tests, responsive tests only |
| Storage engine selection | **OPEN** |
| Capacitor secure-storage plugin | **OPEN** |
| Capacitor SQLite/storage | **OPEN** |

Did not merge `feat/plm-pwa-hardening`. Did not copy PLM business/authorization.

---

## 3. Hardware truth carried forward

Implemented today: HID-as-keyboard, still-image ExItS QR, camera photos, Share, SecureStorage, Connectivity, filesystem.

Absent today (not parity blockers): thermal printer, physical drawer, NFC, real payment terminal, live product-barcode camera.

---

## 4. Authorization lock

PWA production **NO**. Offline replacement **NO**. Capacitor production **NO**. No library selection.

---

## 5. Next package

POS-REACT-READINESS-05: implementation master plan, open decisions, final report.
