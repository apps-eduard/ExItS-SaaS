# PLM-CLIENT-GATE-D0 — Browser session auth transport

**Package:** PLM-CLIENT-GATE-D0  
**Date:** 2026-08-19  
**Branch:** `feat/plm-react-client`  
**Starting SHA:** `1a62b063cb61f1fdf2521a0a139d8aab7d2651a3`

Prepares PinoyLoanManager Browser/PWA for secure Platform cookie authentication. Transport only. No auth UI.

---

## Status

| Item | Status |
|---|---|
| PLM-CLIENT-GATE A | **APPROVED** |
| PLM-CLIENT-GATE B | **APPROVED** |
| PLM-CLIENT-GATE C | **APPROVED** |
| PLM-CLIENT-GATE D0 | **COMPLETE** after validation |
| PLM-CLIENT-GATE D1 | **NOT STARTED** (Sign In/session UI + Local Validation Test User) |
| PLM-CLIENT-GATE D2 | **NOT STARTED** (Register/Activate/Forgot/Reset + Mailpit PLM callback routing) |
| R-091 | **OPEN** |
| Capacitor auth | **NOT STARTED** |
| PinoyBusinessPOS | **UNCHANGED** |
| PLM loan .NET code | **UNCHANGED** |
| Platform change | Cookie Secure policy only |
| DB/migrations | **NONE** |

---

## Delivered

- Same-origin Vite **dev (5176)** and **preview (4176)** proxy: `/platform-api` → loopback Platform API
- Browser API base is relative `/platform-api` (never `http://localhost:8091` from JS)
- Proxy target from server-side `EXITS_PLATFORM_API_PROXY_TARGET` (not `VITE_*`); non-loopback targets rejected
- Platform session cookie: `Secure=false` only when HTTP auth cookies are allowed **and** the request is HTTP (Local Validation Staging HTTP, plus existing Development/Testing). Production and generic Staging remain `Secure=true`. HttpOnly and SameSite=Lax unchanged
- Browser-facing mapping strips `sessionToken`; tests assert it is not stored in web storage
- PWA Gate C preserved: `/api/*` and `/platform-api/*` NetworkOnly; no Background Sync

## Email callback gap (verified, not fixed)

Platform outbound registration/reset emails currently build links to:

- `/admin/activate-account`
- `/admin/reset-password`

using `PlatformEmail:AdminPublicBaseUrl`. Follow-up is **PLM-CLIENT-GATE-D2**. PLM registration/reset is not ready.

## Explicit non-goals

- Sign In / Register / Reset UI, Test User, logout UI, session provider, organization chooser
- Google/Facebook
- Capacitor
- Loan features
- CORS expansion for 5176/4176
- Changing email architecture

## Exact next package

**STOPPED AFTER PLM-CLIENT-GATE-D0.** Do not start D1 or D2 from this package.
