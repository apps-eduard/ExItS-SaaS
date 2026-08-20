# PLM-PWA-H2 — Install and update lifecycle hardening

**Package:** PLM-PWA-H2
**Date:** 2026-08-20
**Branch:** `feat/plm-pwa-hardening`
**Starting SHA:** `46ca3dfd4c78b5c00a81fceddf9fa8236da4361f` (H1)

Hardens PWA install metadata and prompt-based updates. Updates remain user-triggered. Registration failure does not crash the product shell.

---

## Status

| Item | Status |
|---|---|
| PLM-PWA-H2 | **COMPLETE** after validation |
| Physical device validation | **NOT CLAIMED** |
| Gate E | **BLOCKED** |
| Capacitor | **NOT STARTED** |
| Lending | **ABSENT** |

---

## Delivered

- `PwaUpdateHost` swallows `virtual:pwa-register` / `registerSW` failures
- User Refresh applies at most once per notice (no repeated update storms)
- Generic update guard seam unchanged (no fake lending dirty-form state)
- Visible focus ring on shared buttons
- Playwright: update notice can be shown, EN copy, no token persistence, axe
- Manifest installability remains covered by existing PWA e2e

---

## Explicitly NOT delivered

- Custom browser-independent Install App UI
- Silent force refresh
- Offline financial unsaved-state machinery
- Android / Capacitor install validation

---

## Evidence

- Vitest: `PwaUpdateNotice` EN + fil-PH; apply guard default-allow and blocked; host apply-once; custom refresh event
- Playwright: `e2e/pwa-update.spec.ts` (notice, Refresh, no `sessionToken` persistence, axe, fil-PH copy)
- Existing `e2e/pwa.spec.ts` installable manifest (name, short_name, start_url, standalone, 192/512, maskable)
- Physical device / Lighthouse: **not run**
- Live Platform API `:8091`: **not started** (`LIVE_PLATFORM_VALIDATION_DEFERRED_FOR_PARALLEL_SAFETY`)

