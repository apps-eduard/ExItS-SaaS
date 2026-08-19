# Mobile React — PWA and Capacitor Delivery

**Status:** Documentation only. Implementation is **NOT AUTHORIZED**.  
**Package:** MOBILE-REACT-DOC-04  
**Depends on:** [frontend-architecture-and-reuse.md](frontend-architecture-and-reuse.md), [product-surfaces-and-ux.md](product-surfaces-and-ux.md)

One React client (`ExItS.PinoyBusinessPOS.Client`, not created) can be delivered as browser, PWA, and Capacitor packages.
**PWA capability is not Capacitor capability.** Do not claim identical device APIs, background behavior, or hardware support.

PWA production rollout and Capacitor production rollout remain **NOT AUTHORIZED**. This file defines how they would work when separately approved.

---

## 1. Delivery model

```text
React client (one codebase)
   |
   +-- Browser
   |     (any supported desktop or mobile browser; not “installed”)
   |
   +-- PWA
   |     +-- desktop install / standalone window
   |     +-- Android browser / install (Chrome and equivalents)
   |     +-- iPhone / iPad Safari / Home Screen where the OS allows it
   |
   +-- Capacitor
        +-- Android first (signed package)
        +-- iOS later (App Store / TestFlight)
```

| Delivery | What it is | What it is not |
|---|---|---|
| Browser | HTTPS web app behind the existing reverse-proxy model | An offline financial database |
| PWA | Same app + installability + **static app cache** + standalone display | A native Play/App Store binary; not equivalent to Capacitor plugins |
| Capacitor Android | Native WebView host + device adapters + store/sideload package | A place to put POS business rules |
| Capacitor iOS | Later native packaging | Available at the same time as Android unless separately scheduled |
| Windows | Browser / PWA by default | First-class Capacitor Windows packaging (not claimed) |

Current production clients remain MAUI + existing Blazor web hosts until explicit cutover (MOBILE-D-002 / D-003).

---

## 2. Two kinds of “offline” (must not be mixed)

| Layer | Name | Purpose | Must never |
|---|---|---|---|
| **STATIC APP CACHE** | Service worker / HTTP cache of hashed JS, CSS, fonts, icons, shell HTML policy | Start the UI when the network is slow or briefly gone | Store sales, payments, entitlements, or outbox payloads as source of truth |
| **AUTHORITATIVE LOCAL OFFLINE DATA** | Dedicated client persistence/sync layer (current MAUI: `LocalStore` SQLite + encrypted outbox) | Queue allowed operations; project catalog/cash snapshots per existing product rules | Live inside Cache Storage / “cache-first API” |

Service-worker cache **is not** the financial offline database.

Dynamic API data (sales, payments, shifts, inventory mutations, entitlement checks) uses network + TanStack Query. Query cache is a UX cache, not SoR. Offline selling follows existing LocalStore/outbox semantics, not Cache API.

Contrast: Platform Admin React planning uses **no service worker by default**. This client may use a SW **only** for the static shell, under the rules below. That is a different application.

---

## 3. PWA strategy

### 3.1 Installability and chrome

Planning requirements when PWA is authorized:

- Web app **manifest** (`name`, `short_name`, `start_url`, `display: standalone`, `theme_color` / `background_color` from ExItS tokens, language)
- **Icons** at the sizes required by target browsers (maskable where Android expects it)
- HTTPS origin (production reverse proxy already requires TLS)
- Install prompt is optional UX; never block selling if the user stays in the browser tab
- Standalone display must keep safe areas, status, and offline/sync header visible (DOC-02)

Browser delivery without install remains fully supported. PWA is an enhancement, not a second product.

### 3.2 Service-worker responsibilities

Allowed:

- Precache **content-hashed** static assets (JS, CSS, fonts, static images)
- Keep a small **app shell** so routes can render an offline/reconnect UI
- Revalidate **entry HTML** (and SW itself) so deploys are picked up without “clear your cache”
- Skip waiting / activate only after a safe update path (see §3.4)

Forbidden:

- Cache-first (or immutable long cache) for Platform/POS **API** JSON
- Caching `Authorization` / session bodies
- Treating Cache Storage as LocalStore
- Aggressive prefetch of financial lists “for offline”
- Silent SW takeover that destroys an in-progress cart/checkout

API strategy: **network (and existing Query/offline layers)**. If the network fails, the feature uses the dedicated offline coordinator or a clear online-required message — not a stale SW copy of a sale.

### 3.3 Version, cache invalidation, safe deploy

Align with the Admin hashed-asset idea, adapted for a SW:

| Asset | Cache |
|---|---|
| Entry HTML / bootstrap | Revalidate; must not be long-lived immutable |
| Content-hashed JS/CSS | Immutable; new build → new filename |
| Manifest / icons | Versioned or hashed paths |
| API | No SW cache-first |

Deployment:

- Atomic or equivalent release of a consistent asset set
- Keep previous hashed chunks long enough for open tabs / lazy routes
- Reverse proxy must not pin stale `index.html`
- Users are **never** told to manually clear cache as the normal update procedure

### 3.4 Stale-version detection and unsaved work

- Ship a build/release identifier (visible in diagnostics)
- Detect a newer frontend (SW update, or a small version document fetched with the HTML policy)
- Prompt: **New version available** — apply on idle, or on explicit Refresh
- If the session has unsaved work (non-empty cart, in-progress checkout, unsent form), **do not** silently reload
- After apply: TanStack Query invalidation as needed; do not invent “replay all API GETs from SW”

### 3.5 PWA offline shell (not POS offline SoR)

When offline, the PWA may show the existing reconnect / offline banners and allow **only** operations the dedicated offline layer already permits (today: policy-gated offline cash, etc.). If that layer is unavailable in the browser host, the UI says so. Do not fake a full cashier database from the service worker.

---

## 4. iOS interim strategy (PWA / browser before native iOS)

PWA or Safari browser can provide **earlier iPhone/iPad availability** than Capacitor iOS. That is a reachability tactic, not feature parity.

Recorded limitations (do not promise to close them in PWA):

| Area | Typical PWA / Safari limit vs Capacitor iOS later |
|---|---|
| Installation UX | Add to Home Screen; no App Store listing; icons/splash differ |
| Device APIs | Camera, files, and share vary by iOS/Safari version |
| Background | Unreliable background sync; no assumption of MAUI-like reconnect workers |
| Hardware | Barcode wedge may work; dedicated scanners, Bluetooth printers, NFC, payment terminals generally **need native later** |
| Push | Not assumed |
| Storage | Origin quota; not equivalent to Android Capacitor secure storage + SQLite |
| Standalone bugs | Viewport / keyboard / safe-area quirks differ from Android Chrome |

Payment/NFC/device integrations that exceed browser capability wait for **Capacitor iOS** (or remain Android-native only until iOS packaging exists). DOC-02 payment methods are unchanged: no live card collection invented here.

---

## 5. Capacitor strategy

### 5.1 Targets

- **Android:** first native packaging target (Play / enterprise sideload as later release process defines)
- **iOS:** later target (TestFlight / App Store when authorized)

Capacitor is a **thin** native delivery and device-integration layer around the same React app.

### 5.2 What native code may do

Native plugins / small platform code may provide adapters when the browser is insufficient:

- Camera
- Scanner (camera or hardware)
- Secure storage
- File / share
- Printer / Bluetooth (when a later package authorizes a vendor)
- NFC (when authorized; no product invented here)
- Vendor SDK integration (must not become a second POS engine)

**Do not** move pricing, entitlements, role grants, sale completion rules, or outbox conflict policy into plugin Java/Kotlin/Swift. Those stay on .NET APIs and the shared TypeScript coordination layer that already mirrors LocalStore *semantics*.

### 5.3 Packaged assets vs PWA

Capacitor ships a **snapshot** of web assets inside the native package. Updates normally follow the **Android/iOS release channel**, not the website’s service worker.

- Do not assume website SW updates the store app
- Do not assume Capacitor Live Update / OTA web-asset sync; that would need a later explicit security review (financial client)
- Adapter implementations differ (MOBILE-D-025); selling UI should not fork

Current MAUI Debug APK install docs remain the MAUI path. Capacitor packaging is a future host, not a rename of MAUI.

---

## 6. Windows / desktop

**Default desktop path: browser or desktop PWA.**

Do **not** claim Capacitor provides first-class Windows native packaging in this track.

If true native Windows (WinUI / store package / kiosk shell) becomes a requirement, evaluate it as a **separate** work package. Until then, desktop selling and Owner/Personal on large screens use DOC-02 desktop/PWA layouts over HTTPS.

---

## 7. Release / update model (independent channels)

| Channel | How it ships | How it updates |
|---|---|---|
| **Web / PWA** | Web deployment on the HTTPS origin (reverse proxy; hashed assets + HTML revalidation) | Next page load / SW update prompt; no store review |
| **Android Capacitor** | Signed application package + defined release channel | Store or MDM/sideload of a new package |
| **iOS Capacitor** | Later App Store / TestFlight | Apple’s process when that target is authorized |
| **MAUI (current)** | Existing APK / install docs | Unchanged until cutover |

Frontend versions must tolerate a **supported API compatibility window**:

- Additive Platform/POS API changes preferred
- Old clients on a still-supported version must fail closed on unknown required fields, not corrupt money
- Rolling deploys must not require mixed old HTML + new hashed chunks
- Compatibility window is a release requirement, not a license to skip server validation

Diagnostics: each delivery shows enough version identity (web build id, native app version, API compatibility note) for support. No secrets in that UI.

---

## 8. Authorization reminder

| Item | Status |
|---|---|
| Documenting this delivery model | This package |
| PWA production rollout | **NOT AUTHORIZED** |
| Capacitor production rollout | **NOT AUTHORIZED** |
| iOS native packaging | Later, separately authorized |
| Windows native Capacitor | Not in scope |
| Service worker as financial DB | **Forbidden** |

---

## 9. Non-goals

- Implementing a service worker or Capacitor project
- Replacing MAUI install runbooks
- Promising iOS PWA = Android Capacitor
- Enabling OTA live updates for the native binary
- Changing payment or LocalStore rules
