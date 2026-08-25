# Mobile React — Device and Payment Integration Architecture

**Status:** Documentation only. Implementation is **NOT AUTHORIZED**.  
**Package:** MOBILE-REACT-DOC-06  
**Depends on:** [frontend-architecture-and-reuse.md](frontend-architecture-and-reuse.md), [pwa-and-capacitor-delivery.md](pwa-and-capacitor-delivery.md), [offline-sync-auth-and-security.md](offline-sync-auth-and-security.md), [product-surfaces-and-ux.md](product-surfaces-and-ux.md)

This file defines **future** hardware and payment-adapter boundaries for Web/PWA and Capacitor. It does **not** authorize plugins, vendor SDKs, a real payment provider, or MAUI retirement.

Do **not** claim hardware that current MAUI/API code does not implement.

---

## 0. Current-system audit (do not guess)

### 0.1 What exists today

| Area | Evidence | Current capability |
|---|---|---|
| Product barcode field | Catalog barcode 8–14 digits; unique per org when present (`PosDbContext` `ux_products_org_barcode`) | Data + lookup, not a hardware driver |
| Checkout product lookup | `SaleCheckout.razor` `DebouncedSearchAsync`: exact barcode/SKU first, then name; search field never disabled | Keyboard / HID wedge can type into that field |
| Camera QR scan | `IQrCodeScanService` / `MauiQrCodeScanService`: MediaPicker still image + ZXing; **QR_CODE only**; Android decode; **no live camera view** | Identity / buyer QR, not product barcode scan |
| QR render | `LocalQrCodeRenderer` (QRCoder) for Personal, Business, and POS device-registration payloads | Public reference QR only |
| Product camera | `IProductImagePicker` / `MauiProductImagePicker` for catalog primary image | Photo capture/pick, not scanning |
| Receipt UI | `SaleReceipt.razor` on-screen summary; reprint uses `GetSaleAsync` (P19-WP05) | No thermal printer |
| Share / handoff | `IDocumentHandoffService` / `MauiDocumentHandoffService` (`Share.Default`); reports **initiated**, never print/save success | Share sheet, not print confirmation |
| Connectivity | `IConnectivityService` / `MauiConnectivityService`: OS network access, **not** POS API health | Coarse online/offline |
| Device identity | `IDeviceIdentityProvider`: stable local installation id in secure store; Platform `PosDevice` + registration QR | Logical device, not a printer/NFC serial |
| Register | Domain `Register`: named sales station; **not** a drawer, printer, terminal, or OS device (P10-WP07) | Logical station |
| Cash drawer (domain) | `CashierShiftMovement`: immutable cash movement on an Open shift | Logical cash, **not** kick hardware |
| Electronic payments | `IPaymentGateway` + `FakePaymentGateway` (`ProviderCode` = `Fake`); `PaymentProvider.Fake` / `Manual` / `None` | Simulation / manual only |
| Pending electronic checkout | `MauiPendingPaymentStore`: sale/attempt/idempotency/org/method **identifiers only** | No card/PIN/OTP |

### 0.2 What was **not** found

No implementation of:

- Live camera product-barcode scanning
- Dedicated HID/USB scanner SDK
- Thermal / ESC/POS printer
- Bluetooth printer transport
- USB printer transport
- Browser `window.print` receipt path
- Physical cash-drawer kick (ESC/POS drawer pulse or equivalent)
- NFC reader / tap-to-pay
- Real card terminal / acquirer SDK
- Direct GCash API, automatic GCash QR generation, or gateway-verified retail GCash

Product requirements explicitly **defer** cash-drawer hardware, direct GCash API, automatic QR, cards as MVP verification, and related processor features. P19-WP05 residual: **dedicated thermal printer integration out of scope**.

### 0.3 QR vs barcode (do not collapse)

| Flow | Current host behavior |
|---|---|
| Product barcode | Typed (or HID-as-keyboard) into catalog/checkout search |
| ExItS identity QR | Camera still-image or gallery pick; purpose-guarded (`Personal`, `Organization`, `PosDeviceRegistration`, sale-customer) |
| Simulated electronic GCash | Fake gateway session may include `QrPayload` / checkout URL / deep link — **Development/Testing simulation**, not a live wallet |
| Manual GCash | Cashier confirms outside the app; **reference required**; no QR/gateway verification (`SalePaymentMethod.ManualGCash`) |

### 0.4 Registers, devices, drawers (do not collapse)

| Concept | What it is | What it is not |
|---|---|---|
| **Register** | Named org sales station (`REG-NNNNNN`) | Hardware, printer, drawer, payment terminal |
| **PosDevice** | Platform-registered POS endpoint (durable `InstallationDeviceId`). Customer Device Management lists **Active** devices only. Capacity = count(Active). Revoke/Remove is soft-state (record retained + audit); revoked devices leave the normal UI and free a slot. Browser/OS strings are metadata only. | Bluetooth MAC, USB VID/PID, NFC chip; browser name as the subscription unit |
| **CashierShift / cash drawer movement** | Logical cash authority and recorded movements | Solenoid / printer-kick hardware |
| **DeviceIdentity adapter (future)** | Install id + host labels for registration and LocalStore context | Proof of payment or entitlement |

### 0.5 Canonical POS device management rules (React + Platform)

1. Device capacity counts **ACTIVE** POS devices only.
2. Normal Device Management shows **ACTIVE** devices only (`GET .../pos-devices`).
3. Revoked devices disappear from the normal customer UI immediately after Remove/Revoke.
4. Revocation is soft-state (`PosDeviceStatus.Revoked`) — **not** physical deletion.
5. Revoked rows remain in the database for audit/history (`RevokedAtUtc` / `RevokedByUserId` + Platform audit events).
6. Immutable audit history uses Platform audit actions (`platform.pos_device.registered` / `.revoked` / rename). Reactivation may clear current-state revoke fields; audit events must still retain history.
7. Browser/client information is device metadata, not the subscription-count identity.
8. Login and POS-device authorization remain separate concepts.
9. Only registered **active** devices may execute POS sales (see device registration simplification / sales execution gate).
10. Registration-code UX is not part of the normal React customer flow; MAUI may still use token APIs for compatibility.
11. Governing/support history including revoked devices: `GET .../pos-devices/history` (edit-org authority).
12. Login may succeed from any permitted endpoint without consuming a device slot.
13. Unregistered endpoints may use authorized read/management surfaces; POS money execution is blocked client-side and server-side (`application.pos_device.registration_required`).
14. React primary action is **Register this device** (`POST .../pos-devices/register`); do not auto-register on login.
15. Capacity-consuming registration (register + MAUI redeem) runs under organization advisory lock so concurrent final-slot races cannot over-allocate Active devices.
16. Web cannot expose permanent physical hardware identity across browsers; durable `InstallationDeviceId` is the registration identity (documented limitation).

---

## 1. Device abstraction (future)

Features must not call browser APIs, Capacitor plugins, or vendor SDKs directly.

```text
React feature
    |
Device service contract
    |
+-----------------------+
| Web/PWA adapter       |
| Capacitor adapter     |
+-----------------------+
    |
browser API / native plugin / vendor SDK
```

Unimplemented adapters return an explicit **unavailable** result. The UI degrades (manual barcode, on-screen receipt, share). Adapters must **not** invent a terminal, NFC product, or printer brand.

This package does **not** create TypeScript interfaces or pin plugin libraries.

### 1.1 Categories

| Category | Responsibility | Current MAUI analogue |
|---|---|---|
| **Scanner** | Deliver a decoded string into a **common product lookup** (or identity-QR resolver when the flow is QR) | Search field + `IQrCodeScanService` (QR only) |
| **Camera** | Capture/pick images (catalog photo; still-image QR) | `IProductImagePicker`, MediaPicker |
| **Printer** | Transport already-rendered receipt bytes/commands | **None** |
| **CashDrawer** | Open physical drawer when a later package authorizes it | **None** (logical shift movements only) |
| **NFC** | Read/write or tap-to-pay **if** a later package authorizes it | **None** |
| **PaymentTerminal** | Start/query a provider session; never handle PAN/PIN/CVV in ExItS | `IPaymentGateway` (Fake only) |
| **Share** | OS/browser handoff of text/file | `IDocumentHandoffService` |
| **Connectivity** | OS reachability + (separately) API call outcomes | `IConnectivityService` |
| **DeviceIdentity** | Stable install id for registration / LocalStore context | `IDeviceIdentityProvider` |

Web/PWA and Capacitor **may use different physical plugins**. They should share the **same contracts**. Do not promise browser/PWA direct hardware parity with Capacitor (MOBILE-D-027, D-030).

POS business rules (pricing, entitlements, sale completion, outbox) stay in .NET APIs and the shared client coordination layer — **not** in native plugin code (MOBILE-D-031).

---

## 2. Barcode / scanner

### 2.1 Supported future paths

1. **Keyboard / HID barcode scanner** — wedge devices already work wherever a text field has focus. No manufacturer SDK required.
2. **Camera barcode scanning** — live or still decode of product codes (EAN/UPC/Code 128, etc.) where the host permits camera access.
3. **Native scanner plugin** — Capacitor-only when a later package proves camera/HID is insufficient (dedicated hardware).

All three must emit the same result: a **normalized scan string** into the **existing product lookup** (exact barcode, then SKU, then name search). Checkout must not import a scanner vendor.

Identity QR remains a **separate** flow (purpose envelope + Platform resolve). Do not feed a Personal/Business/Device QR into product lookup.

### 2.2 Current vs future

| Path | Current MAUI | Future React planning |
|---|---|---|
| HID / keyboard wedge | Implicit via checkout search input | **Preserve** as the always-available path |
| Camera product barcode | **Not implemented** | Adapter; degrade to type/HID |
| Camera ExItS QR | Still-image, QR only, Android decode | Same contract; live view is optional later |
| Manufacturer SDK | **Not present** | Forbidden as a checkout dependency |

Unknown barcode: clear error, offer search; do not silently create a product from the sell floor (DOC-02).

---

## 3. Printing and cash drawer

### 3.1 Separate concerns

| Layer | Owns | Must not own |
|---|---|---|
| Receipt **rendering** | Header/footer, lines, totals, tender, GCash reference, disclaimer, locale/currency | Socket, Bluetooth, USB, ESC/POS bytes |
| Printer **transport** | Send rendered output to a destination | Sale completion, tax, inventory |

A successful sale must not depend on a successful print. Share initiated ≠ printed (current `DocumentHandoffResult` already encodes that).

### 3.2 Future transport possibilities (not current products)

| Transport | Typical host | Planning status |
|---|---|---|
| On-screen receipt + reprint | All | **Current** behavior |
| Share / download | All | **Current** MAUI share; Web Share / blob download as adapters |
| Browser print dialog | Browser / PWA | Optional fallback; not ESC/POS; **PLATFORM_DEPENDENT** |
| Network printer | Any host that can reach it | **FUTURE**; vendor/protocol not selected |
| Bluetooth printer | Capacitor | **NATIVE_REQUIRED** + **FUTURE**; no vendor selected |
| USB / native integration | Capacitor (and later true native Windows if ever evaluated) | **NATIVE_REQUIRED** + **FUTURE** |
| Vendor SDK adapter | Capacitor | **FUTURE**; must not become a second POS engine |

Do **not** promise that browser/PWA can drive Bluetooth/USB thermal printers.

### 3.3 Cash drawer

Physical kick is commonly routed through a printer (ESC/POS drawer pulse) or a dedicated kick device.

Planning rule: **CashDrawer** is an adapter. When authorized, it may call Printer transport or a dedicated plugin. Until then:

- Logical shift cash movements remain the operational cash-drawer **domain**
- Hardware open is **NOT_ASSUMED**
- Requirements still list cash-drawer hardware as deferred MVP

A failed drawer open must not void or rewrite a completed sale.

---

## 4. Payments

### 4.1 Preserve current POS retail boundaries

| Method | Domain | Client meaning | Sensitive data |
|---|---|---|---|
| **Cash** | POS operational payment | Tendered / change; offline queue only where current policy allows (cash-only today) | None |
| **Manual GCash** | POS operational payment | Cashier confirms outside the app; **reference required**; not gateway-verified | No GCash PIN/OTP/account secrets |
| **Customer credit (Utang)** | POS obligation / payment domain | Entitlement + expiry rules; online-required in current selling UI | No payment credentials |
| **Electronic Card / GCash** (`SalePaymentMethod.Card` / `GCash`) | Simulated `IPaymentGateway` | **Development/Testing** Fake provider; `AwaitingPayment` until Paid | Identifiers only; **no PAN/PIN/CVV** |
| **Platform SaaS payments** | Platform commercial | Subscription billing (including Platform GCash) | Separate entities; **must not** reuse POS retail payment rows |

Platform GCash (business pays ExItS) ≠ POS GCash (customer pays the store).

Do **not** add wallets, split tender, refund-to-original-channel, or live card collection in this track (MOBILE-D-020).

### 4.2 Development / Test payments

`FakePaymentGateway` is labeled Development/Testing and never used for real card or GCash credentials. Simulation endpoints are disabled outside Development/Testing (`PaymentAttemptUseCases`). Platform `LocalValidationTestPayments` is Platform Admin, not POS retail.

Future React must keep Test Payments **Development/Testing only**. Do not ship Fake as a production provider.

### 4.3 Real provider / terminal (not selected)

This documentation **does not choose** a payment provider, acquirer, or terminal brand.

If a later authorized package adds terminals/cards/NFC:

```text
PaymentService
   |
Provider adapter
   |
approved payment terminal / provider SDK
```

Rules:

- The **provider/terminal** handles sensitive card interaction (PAN, PIN, CVV, track data).
- ExItS receives only **permitted result/reference** data (status, provider reference, optional brand/last-four if the provider supplies them — matching current `PaymentGatewayResult` / webhook shape).
- Do **not** store raw card number, PIN, CVV, track data, or equivalent.
- Do **not** log payment secrets or full PAN.
- Idempotency keys and client sale ids remain required (DOC-05).
- Unknown / timed-out / `AwaitingPayment` / `Processing` is **not** success.

Current recovery pattern to preserve: `GetSessionAsync` after ambiguous timeout; pending store holds identifiers; sale completes only on **Paid**.

### 4.4 NFC

NFC is a **future adapter** with no current product. It is **NOT_ASSUMED**. If later authorized, it plugs in as Scanner (identity tap) or PaymentTerminal (tap-to-pay via **provider** SDK) — never as a place to store card data in ExItS.

---

## 5. Capability matrix

Status vocabulary:

| Token | Meaning |
|---|---|
| **AVAILABLE** | Usable with current-class APIs / existing host patterns; still needs implementation in the future React host |
| **PLATFORM_DEPENDENT** | Possible on some browsers/OS versions; must detect and degrade |
| **NATIVE_REQUIRED** | Not a realistic browser/PWA capability; needs Capacitor (or other native) |
| **FUTURE** | Not in current MAUI/API; later authorized work package |
| **NOT_ASSUMED** | Must not be promised in UX, store listings, or rollout plans |

| Capability | Web Browser | PWA | Capacitor Android | Capacitor iOS later | Fallback | Status |
|---|---|---|---|---|---|---|
| Barcode HID (keyboard wedge) | Works as keyboard input | Same | Same | Same | Manual type | **AVAILABLE** |
| Camera product barcode scan | Barcode Detection / getUserMedia where allowed | Same; iOS Safari limits apply | Native camera plugin **FUTURE** (current MAUI has QR still-image only) | Later native; do not promise parity | Type / HID | **PLATFORM_DEPENDENT** + **FUTURE** |
| Camera ExItS QR | File input / camera if permitted | Same | Native camera; current analogue is still-image | Later | Manual public-id entry | **PLATFORM_DEPENDENT** |
| Bluetooth printer | No | No | Plugin + vendor **FUTURE** | Plugin + vendor **FUTURE**; no parity promise | On-screen + share | **NATIVE_REQUIRED** + **FUTURE** |
| Network printer | Possible if a later protocol is chosen | Same | Same | Same | Browser print / share | **FUTURE** |
| USB printer | No (typical) | No | Native **FUTURE** | Native **FUTURE** | Share / browser print | **NATIVE_REQUIRED** + **FUTURE** |
| Browser print dialog | Optional | Optional | Optional (WebView) | Optional | On-screen receipt | **PLATFORM_DEPENDENT** |
| Cash drawer hardware | No | No | Via printer/device adapter **FUTURE** | Same; no parity promise | Manual drawer; logical shift cash | **NATIVE_REQUIRED** + **FUTURE** |
| NFC | Typically no | Typically no | **FUTURE** if authorized | **FUTURE**; no parity promise | Manual QR / typed id / cash | **NOT_ASSUMED** |
| Payment terminal | No | No | Provider SDK adapter **FUTURE**; none selected | Same; no parity promise | Cash / Manual GCash / Utang | **NOT_ASSUMED** |
| File / share | Web Share **or** download | Same | Native share sheet (current MAUI pattern) | Native share when packaged | Copy to clipboard / download | **PLATFORM_DEPENDENT** |
| Offline local storage | Browser-capable durable store (DOC-05) | Same; SW cache is **not** LocalStore | Native-capable DB/secure store (DOC-05) | Later; no background/storage parity promise | Online-required flows | **PLATFORM_DEPENDENT** (see DOC-05) |

iPhone/iPad **before** Capacitor iOS: browser/PWA only — do not promise printers, NFC, terminals, or background hardware (MOBILE-D-030).

---

## 6. Failure UX

Users must see a **clear, non-success** state. Hardware failure must not look like payment success.

| Situation | UX | Must not |
|---|---|---|
| Printer unavailable | Sale can complete; offer on-screen receipt, reprint later, share | Block checkout solely because print failed; claim “printed” from share-initiated |
| Scanner unavailable | Keep search focused; offer type-in; explain camera permission if denied | Hide product lookup |
| Bluetooth disconnected | Show printer/drawer disconnected; retry; fallback to on-screen/share | Auto-complete a payment; spin forever |
| Terminal timeout / network drop mid-pay | Status **unknown / pending**; recover via provider session lookup (current `GetSessionAsync` pattern) | Treat as Paid |
| Payment declined | Failed; allow another method or cancel per existing sale rules | Store PAN; retry with altered completed records |
| Payment status unknown | Stay on `AwaitingPayment` / Processing / pending-review until the server says Paid, Failed, Cancelled, or Expired | Treat unknown as successful |
| Receipt reprint | Reload authoritative sale (`GetSaleAsync`); render again; transport is optional | Invent a second financial document |
| Offline mode | Follow DOC-05: cash queue where allowed; Manual GCash/Utang/card remain online in **current** code | Enable hardware-terminal pay while offline unless a later package proves the provider supports it |

**Never treat unknown payment status as successful.**

Electronic attempts today: sale is receipt-eligible only when **Completed** after **Paid** (P19-WP05). Created, Pending, RequiresCustomerAction, Processing, PendingManualVerification, and timeout-before/after-create Fake behaviors are not completion.

---

## 7. Security constraints (device + payments)

- No payment credentials, GCash PIN/OTP, raw card data, CVV, or track data in local storage, logs, URLs, or receipts beyond permitted references (GCash reference, provider reference, optional last-four from the **provider**).
- Pending payment persistence: identifiers only (current `MauiPendingPaymentStore`).
- Device identity is not an authorization proof.
- Vendor SDKs stay behind adapters; they must not receive more data than the provider requires.
- Server remains authoritative for sale completion and entitlements (DOC-05).

---

## 8. Explicitly out of scope

- Selecting or integrating a real payment provider or terminal brand
- Implementing Capacitor plugins, ESC/POS, Bluetooth, USB, or NFC
- Claiming PWA hardware parity with Android Capacitor
- Changing current MAUI, POS API payment methods, or Fake gateway behavior
- Treating simulated Card/GCash as production UX (MOBILE-D-020)

**Implementation: NOT STARTED.**

---

## 9. Related decisions

MOBILE-D-020, D-025, D-030, D-031, D-035, D-039 through D-044 in [decisions.md](decisions.md).
