# PLM-PWA-H3 — Connectivity and fail-closed offline UX

**Package:** PLM-PWA-H3
**Date:** 2026-08-20
**Branch:** `feat/plm-pwa-hardening`
**Starting SHA:** `a3758434f3ac978f936a479ac68bee071b781392` (H2)

Adds advisory connectivity UX for the online-first Pinoy Loan Manager PWA. Browser `navigator.onLine` is UX-only. Server/API results remain authoritative. No offline operations, command queues, or Background Sync.

---

## Status

| Item | Status |
|---|---|
| PLM-PWA-H3 | **COMPLETE** after validation |
| Offline financial operations | **NOT IMPLEMENTED** |
| Offline financial posting | **PROHIBITED** |
| PLM-13 | **NOT STARTED** |
| Background Sync | **ABSENT** |
| Command replay | **ABSENT** |
| Gate E | **BLOCKED** |
| Capacitor | **NOT STARTED** |

---

## Delivered

- Global `ConnectivityHost` / `ConnectivityNotice` (EN + fil-PH)
- Copy: “You're offline” / “Reconnect to continue.” (no operational “Offline mode”)
- Online event hides the notice; no POST/PUT/PATCH/DELETE replay
- Offline reload: static shell may load; previous `allowed` workspace is not restored from API/SW cache
- Unit coverage for initial online/offline, events, and listener cleanup

---

## Explicitly NOT delivered

- Lending or financial offline stores
- Mutation queues / Background Sync
- Treating browser online as authorization or commercial access truth

---

## Evidence notes

Live Platform API on `:8091` was **not** started (`LIVE_PLATFORM_VALIDATION_DEFERRED_FOR_PARALLEL_SAFETY`).
Physical device validation: **not claimed**.
