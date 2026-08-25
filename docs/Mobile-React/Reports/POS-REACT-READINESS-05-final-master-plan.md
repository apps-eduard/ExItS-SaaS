# POS-REACT-READINESS-05 — Final Master Plan Report

**Package:** POS-REACT-READINESS-05  
**Branch:** `docs/pos-react-implementation-readiness`  
**Worktree:** `C:/Users/speed/Desktop/ExItS-SaaS-pos-react-docs`  
**Status:** Documentation readiness plan complete. Implementation is **NOT AUTHORIZED**.

Canonical plan: [05-implementation-master-plan.md](../Implementation-Readiness/05-implementation-master-plan.md)  
Open items: [06-open-decisions-and-blockers.md](../Implementation-Readiness/06-open-decisions-and-blockers.md)

---

## Base main SHA

`5979a9ce008bb24a3257abd28ae79bc1a5a9b569`

## Documentation branch

`docs/pos-react-implementation-readiness`

---

## Current MAUI summary

`ExItS.PinoyBusinessPOS.Maui` is an Android-first MAUI Blazor Hybrid host (`net10.0-android`, min API 24, RIDs `android-arm64;android-x64`). One `BlazorWebView` serves **Auth**, **Personal Mobile**, **Organization Owner Mobile**, and **POS Operations**. Three shells (Auth, Personal, Pos). Owner is capability inside PosShell, not a fourth host. **171** `@page` templates (not a parity score). LocalStore encrypted SQLite outbox. Hardware: still-image QR, camera photos, Share, SecureStorage, Connectivity. **No** thermal printer, physical drawer, NFC, or real terminal.

## React migration recommendation

**Yes:** one React + TypeScript codebase as the **future candidate** host (`ExItS.PinoyBusinessPOS.Client/`, not created). Coexist with MAUI until Gate J. Do not overwrite `.Maui` or `.Web`.

## Why PWA first

Browser/PWA delivers the same UI over the existing HTTPS reverse-proxy model, proves routing/theme/i18n/sell-floor without the MAUI Android workload, and keeps service-worker cache limited to a **static shell**. Cashiers and Owners can be reached on desktop/tablet browsers before a store package exists.

## Why Capacitor later

Capacitor is a **thin native wrap** of that same app. Current MAUI device behaviour (camera QR, native share, SecureStorage, install identity) needs native adapters. Packaging before Gate E visual maturity would freeze an incomplete sell floor. Plugins must not own money rules.

## First implementation slice

Auth/session shell → workspace resolver → product context → POS sell-floor shell → browse/search → session cart → **online cash checkout** → receipt/share fallback → connectivity/sync chrome. **No offline finance** in that slice. Tablet landscape is the reference selling layout.

## Largest migration risk

Browser auth + CSRF + origin model (PWEB-20) combined with **offline financial parity** (encryption, isolation, idempotent replay) and the temptation to treat checkout-only PWA as MAUI retirement. Second-order: composition-root complexity and porting Domain rules into JavaScript.

## Offline readiness

Phase A (static PWA) is specified. Phase B (LocalStore-equivalent) is **blocked** on storage/encryption/outbox architecture approval. Current cash-only checkout evidence must be preserved.

## Hardware readiness

Parity target = current MAUI, not an invented catalog. HID keyboard READY. QR/camera/share need Capacitor for MAUI-level behaviour; PWA can degrade. Printer/drawer/NFC/terminal absent today.

## API readiness

Existing Platform + POS contracts can support the client **without new authority**. Typed `ApiClient` is the contract. **TYPED_CLIENT_GENERATION_CONTRACT_MISSING.**

## Browser auth readiness

Target: browser-safe session; no reusable token in ordinary storage. **Blocked** on `PWEB20_CSRF_COMPATIBILITY_REVIEW_REQUIRED`. Same-origin/BFF preferred over broad CORS.

## Capacitor readiness

Direction accepted; host not created; plugins unselected; production **NO**.

## Personal parity status

Present on current MAUI. **DEFERRED** from first slice. **Required** for Gate J unless Product Owner splits the host.

## Owner parity status

Present as practical essentials on current MAUI. **DEFERRED** from first slice. **Required** for Gate J unless explicit Web-only disposition.

## POS parity status

Selling is the recommended first vertical slice (online cash). Remaining ops (catalog admin, inventory, customers/credit, shifts, purchasing, reports) stay tracked. Checkout parity **alone** does not retire MAUI.

## Open decisions

See [06-open-decisions-and-blockers.md](../Implementation-Readiness/06-open-decisions-and-blockers.md). **MOBILE-D-060 remains OPEN.**

## External integration checkpoints

| Checkpoint | Status |
|---|---|
| PWEB20_CSRF_COMPATIBILITY_REVIEW_REQUIRED | YES |
| PLM_PWA_PATTERN_REVIEW_REQUIRED | YES |
| TYPED_CLIENT_GENERATION_CONTRACT_MISSING | YES |

## Authorization lock

| Item | Status |
|---|---|
| Implementation authorized | **NO** |
| MAUI retirement authorized | **NO** |
| PWA production authorized | **NO** |
| Capacitor production authorized | **NO** |
| Main merge authorized | **NO** |

QUEUE: **STOPPED AFTER DOCUMENTATION READINESS PLAN**
