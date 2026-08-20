# PLM-PWA-H4 — Production-preview PWA reliability

**Package:** PLM-PWA-H4
**Date:** 2026-08-20
**Branch:** `feat/plm-pwa-hardening`
**Worktree:** `C:/Users/speed/Desktop/ExItS-SaaS-plm-pwa`

Starting D3 SHA: `ebffebc00d68f48cbdfe25801b98622c2c4cdb6c`

| Package | SHA |
|---|---|
| H1 | `46ca3dfd4c78b5c00a81fceddf9fa8236da4361f` |
| H2 | `a3758434f3ac978f936a479ac68bee071b781392` |
| H3 | `44d230dd9695f15d77f94b90388159c8d288db55` |
| H4 | recorded after this commit |

---

## Status board

| Item | Result |
|---|---|
| PWA installability | PASS |
| Service worker registration | PASS |
| Static shell caching | PASS |
| `/platform-api` cache | ABSENT |
| `/api` cache | ABSENT |
| Auth/session cache | ABSENT |
| Background Sync | ABSENT |
| IndexedDB financial store | ABSENT |
| LocalStore | ABSENT |
| Offline financial posting | ABSENT |
| Offline command replay | ABSENT |
| Offline stale authorization | ABSENT |
| Offline stale product access | ABSENT |
| Connectivity UX | PASS |
| Update prompt | PASS |
| EN | PASS |
| fil-PH | PASS |
| 320 | PASS |
| 375 | PASS |
| Tablet | PASS |
| Desktop | PASS |
| Axe | PASS |
| D3 regression | PASS |
| D1/D2 regression | PASS |
| R-091 | OPEN |
| D-P12-03 | OPEN |
| Gate E | BLOCKED |
| Gate E blocker | REAL_LENDING_CONTRACT_MISSING |
| Capacitor | NOT STARTED |
| PLM-13 | NOT STARTED |
| PWEB20_CSRF_COMPAT_RECHECK_REQUIRED | YES |
| Production Ready | NO |
| Cutover | NOT AUTHORIZED |

---

## Evidence

Production preview: `http://127.0.0.1:4176` (Playwright). Dev: `5176` (proxy-only tests).

Live Platform API `:8091`: **not started** (`LIVE_PLATFORM_VALIDATION_DEFERRED_FOR_PARALLEL_SAFETY`). Playwright mocks `/platform-api`.

Lighthouse: **not run**. Physical Android: **not run**.

Screenshots: `Docs/Reports/impl-pwa-hardening/`

- `01-online-workspace-375x812.png`
- `02-offline-fail-closed-375x812.png`
- `03-back-online-375x812.png` (fresh production-preview load with mocked live session/access)
- `04-update-available-375x812.png`
- `05-offline-desktop-1440x900.png`

Same-tab session memory is not treated as authorization after an offline API abort. Online recovery uses a new load against mocked live `/platform-api` results. Mutations are not replayed.

---

## Explicitly NOT delivered

- Lending
- Capacitor / Android
- Gate E
- CSRF invention (PWEB-20 recheck required)
