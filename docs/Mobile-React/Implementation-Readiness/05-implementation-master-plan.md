# POS-REACT-READINESS-05 — Implementation Master Plan

**Package:** POS-REACT-READINESS-05  
**Status:** Documentation only. **No implementation package in this queue is executed.**  
**Evidence base:** `origin/main` `5979a9ce008bb24a3257abd28ae79bc1a5a9b569`  
**Depends on:** [01](01-current-maui-implementation-refresh.md) … [04](04-pwa-offline-device-migration.md), [migration-testing-and-implementation-gates.md](../migration-testing-and-implementation-gates.md)

This file records the **future implementation order**. It uses existing Mobile React gates **A–K**. It does **not** invent a competing gate system.

Gate A (planning baseline) is already approved. This readiness queue is Gate B material (gap plan). **Gates C–K remain NOT AUTHORIZED.**

---

## 1. Recommended order (existing gates)

### GATE C — React client scaffold

Create sibling:

`src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Client/`

Only after Product Owner authorization. No MAUI deletion. No overwrite of `.Web`.

### GATE D — Browser / PWA foundation

Routing, providers (split by concern — not one giant MauiProgram clone), design tokens, `en` + `fil-PH`, System/Light/Dark, responsive shell, PWA static cache (Phase A), safe update lifecycle.

**Browser auth only after PWEB-20 contract review** (`PWEB20_CSRF_COMPATIBILITY_REVIEW_REQUIRED`).

### GATE E — First real vertical slice + visual review

Login/session, workspace/product context, selling shell, product browse/search, cart, **online cash checkout**, receipt, connectivity/sync indicator.

Required visual review: phone, tablet portrait, tablet landscape, desktop/PWA. Human Product Owner approval required. Tablet landscape is the reference selling layout. Phone must remain usable. Desktop/PWA must remain operational POS, not a Platform Admin clone.

No offline financial implementation in this slice unless a later authorized package explicitly includes it.

### GATE F — Offline parity

Do not begin until storage/encryption/outbox architecture is approved. Preserve current **cash-only** evidence unless product/backend changes.

### GATE G — Device/hardware parity

Only **current** capabilities required for parity (HID-as-keyboard, still-image QR, camera photos, share, secure storage, connectivity, device identity). New printer/NFC/terminal work requires separate authorization.

### GATE H — Capacitor Android

Same React app. Thin host. Native secure storage, camera/QR, share, connectivity, device identity, approved local persistence. Physical Android validation. Plugins must not own pricing/tax/sale completion/entitlements/roles/ledger/conflict policy.

### GATE I — Controlled acceptance

Named users/store/branch/device cohort. Rollback to MAUI. API compatibility window. No database migration solely because the client host changed.

### GATE J — MAUI retirement

Explicit Product Owner authorization only. Requires parity/disposition for **Personal Mobile**, **Owner essentials**, and **POS Operations**. Do **not** retire MAUI merely because PWA or APK builds.

### GATE K — iOS later

Separate authorization. Browser/PWA on iPhone is reachability, not Gate K.

---

## 2. Master package map (proposed future queue — DO NOT EXECUTE)

Allowed projects, unless a row says otherwise: `ExItS.PinoyBusinessPOS.Client` only (once created at Gate C).

Forbidden in every row unless explicitly listed: `ExItS.PinoyBusinessPOS.Maui` source changes, `.Web` rewrite, Platform/PLM business code, database migrations, deleting MAUI.

### POS-REACT-IMPL-01 — React scaffold

| Field | Value |
|---|---|
| Gate | **C** |
| Objective | Create `ExItS.PinoyBusinessPOS.Client` (React + TypeScript + Vite per MOBILE-D-010). App shell only. CI typecheck/lint/Vitest smoke. |
| Allowed projects | New `.Client` project + solution wiring docs/CI as needed |
| Forbidden projects | MAUI, POS API, Platform, PLM, `.Web`, LocalStore replacement |
| API dependencies | None (stubs only) |
| Tests | `tsc --noEmit`, lint, Vitest smoke |
| Visual checkpoint | None |
| Offline impact | None |
| Device impact | None |
| Security dependency | None |
| Stop conditions | PO has not authorized Gate C; attempt to delete MAUI; Tailwind on MAUI |
| Expected commit | `chore(pos-react): scaffold pinoy business pos client` |

### POS-REACT-IMPL-02 — PWA static shell (Phase A)

| Field | Value |
|---|---|
| Gate | **D** |
| Objective | Manifest, icons, standalone, safe-area, hashed assets, SW static cache, network-only API policy, update prompt, connectivity chrome. |
| Allowed | `.Client` |
| Forbidden | API cache-first, session cache, LocalStore, Capacitor |
| API dependencies | `/health` probes only |
| Tests | PWA tests: no API cache-first; update does not destroy cart (even if cart still empty) |
| Visual | Phone/desktop chrome |
| Offline | Phase A only |
| Device | None |
| Security | No token in SW cache |
| Stop | `PLM_PWA_PATTERN_REVIEW_REQUIRED` unresolved **and** SW copied unsafely from PLM business; API cache-first detected |
| Expected commit | `feat(pos-react): add pwa static shell` |

Compare PLM H4 **engineering patterns only** after review. Do not copy PLM routes/auth.

### POS-REACT-IMPL-03 — Auth / workspace / product context

| Field | Value |
|---|---|
| Gate | **D** (browser auth) |
| Objective | Session shell, boot resolver, smart workspace (AMEND-03), product context skip/chooser, AppTopBar, Lock/Sign Out/Remove chrome (PIN policy may still be open). |
| Allowed | `.Client` |
| Forbidden | New identity; localStorage Bearer; Platform source change |
| API dependencies | Platform auth login/me/token/introspect/logout; orgs; organization-context; branches; access/evaluate |
| Tests | Playwright login; Vitest AMEND-03 resolver cases; no token in ordinary storage |
| Visual | Phone login/workspace |
| Offline | Restore rules only; no financial outbox |
| Device | None |
| Security | **PWEB20_CSRF_COMPATIBILITY_REVIEW_REQUIRED must PASS** before cookie mutations |
| Stop | CSRF review not done; token in localStorage; MOBILE-D-060 treated as closed without PO |
| Expected commit | `feat(pos-react): add browser session and workspace resolver` |

### POS-REACT-IMPL-04 — Selling visual slice (shell)

| Field | Value |
|---|---|
| Gate | **E** (partial) |
| Objective | POS sell-floor shell (tablet landscape reference, phone cart sheet, desktop operational POS). Role home entry. SellingMode analogue. |
| Allowed | `.Client` |
| Forbidden | Admin IA; Owner-without-role checkout |
| API dependencies | Role/session facts already loaded |
| Tests | Testing Library layout regions; Playwright viewports |
| Visual | **Required** tablet landscape + phone + desktop frames |
| Offline | Chrome only |
| Device | Keyboard/HID field present |
| Security | Server still denies unauthorized `CreateSale` |
| Stop | Desktop looks like Platform Admin; Owner without POS role can pay |
| Expected commit | `feat(pos-react): add pos sell-floor shell` |

### POS-REACT-IMPL-05 — Catalog search / cart

| Field | Value |
|---|---|
| Gate | **E** (partial) |
| Objective | Online product browse/search (barcode/SKU then name); session-persistent cart (memory); qty ±; unknown barcode error. |
| Allowed | `.Client` |
| Forbidden | Offline catalog as SoR; silent product create from sell floor |
| API dependencies | POS catalog GET list/search/by-sku/by-barcode |
| Tests | Vitest cart; Playwright search; contract tests for catalog GETs |
| Visual | Browse + cart on tablet landscape |
| Offline | Cart local; catalog **online** in this package |
| Device | HID into search |
| Security | Typed DTOs + contract tests (`TYPED_CLIENT_GENERATION_CONTRACT_MISSING` interim) |
| Stop | Cart cleared by category/orientation; unbounded load-all products |
| Expected commit | `feat(pos-react): add catalog search and session cart` |

### POS-REACT-IMPL-06 — Online cash checkout

| Field | Value |
|---|---|
| Gate | **E** (partial) |
| Objective | Online cash pay using existing `POST /api/v1/pos/sales` + idempotency + client SaleId. No offline queue. |
| Allowed | `.Client` |
| Forbidden | Offline cash; Manual GCash/Utang/card as first-slice pay; new tender types |
| API dependencies | POS sales POST/GET; shift/register context if current policy requires |
| Tests | API client idempotency; Playwright cash pay; problem+json |
| Visual | Checkout + totals EN + fil-PH |
| Offline | **None** (online only) |
| Device | None |
| Security | Idempotency headers; no pricing in JS as SoR |
| Stop | Offline finance sneaks in; non-cash treated as queued |
| Expected commit | `feat(pos-react): add online cash checkout` |

### POS-REACT-IMPL-07 — Receipt + share fallback

| Field | Value |
|---|---|
| Gate | **E** (close visual slice) |
| Objective | On-screen receipt; Web Share or copy fallback; not print success. Copy Diagnostics on runtime errors. |
| Allowed | `.Client` |
| Forbidden | Claiming thermal print; storing payment secrets |
| API dependencies | `GET /api/v1/pos/sales/{id}` |
| Tests | Playwright receipt; share degrade; diagnostics redaction |
| Visual | Receipt phone + tablet; Gate E screenshot matrix submit |
| Offline | Local receipt **not** required yet |
| Device | Share degrade |
| Security | MOBILE-D-059 redaction |
| Stop | Share reported as print success; Gate E self-approved by agent |
| Expected commit | `feat(pos-react): add sale receipt and share fallback` |

Gate E **human** visual approval happens after IMPL-04…07, not after scaffold.

### POS-REACT-IMPL-08 — Offline foundation (architecture + storage decision)

| Field | Value |
|---|---|
| Gate | **F** (foundation) |
| Objective | Approve physical storage/encryption/outbox design for PWA and/or Capacitor. Logical contracts = current LocalStore. **Select engines only in this authorized package.** |
| Allowed | `.Client` docs + spike tests; still no MAUI deletion |
| Forbidden | Shipping financial offline before approval; SW as database |
| API dependencies | Sync GETs + mutation idempotency (design) |
| Tests | Isolation, encryption, crash-recovery spikes |
| Visual | None |
| Offline | Foundation only |
| Device | Key storage candidates |
| Security | Key outside plaintext DB |
| Stop | Library chosen without proving encryption/quota/isolation; SQLCipher assumed already done |
| Expected commit | `feat(pos-react): define offline storage foundation` |

### POS-REACT-IMPL-09 — Offline cash parity

| Field | Value |
|---|---|
| Gate | **F** |
| Objective | Encrypted outbox FIFO, cash-only offline sale, catalog projection, BlockedByAccess, reconnect sync. Cash only. |
| Allowed | `.Client` |
| Forbidden | Offline Manual GCash/Utang/card; inventory mutation outbox; silent financial rewrite |
| API dependencies | `POST /api/v1/pos/sales` replay; `/api/v1/pos/sync/*` as applicable |
| Tests | Sync/outbox; network-loss; OD-10 retention; conflict retain-for-review |
| Visual | Pending/Failed chrome |
| Offline | Phase B |
| Device | Secure key storage |
| Security | Access revalidation before process |
| Stop | Non-cash queued; SW cache used as proof of offline selling |
| Expected commit | `feat(pos-react): add offline cash checkout parity` |

Personal Utang / customer-credit offline remain later F-follow-ons, still required before Gate J for those experiences.

### POS-REACT-IMPL-10 — Capacitor shell

| Field | Value |
|---|---|
| Gate | **H** (shell) |
| Objective | Thin Android Capacitor host of the **same** React app. Packaged assets. Independent release channel. |
| Allowed | `.Client` Capacitor config + Android wrapper |
| Forbidden | Business rules in Java/Kotlin; OTA live update without security review; MAUI overwrite |
| API dependencies | Same as PWA |
| Tests | Android emulator smoke |
| Visual | Native WebView chrome/safe-area |
| Offline | Uses approved Phase B if already present |
| Device | Host only |
| Security | Native secure storage **plugin still must be selected in an authorized package** |
| Stop | Capacitor before Gate E visual pass; plugin owns sale completion |
| Expected commit | `feat(pos-react): add capacitor android host` |

### POS-REACT-IMPL-11 — Device adapters (current capabilities)

| Field | Value |
|---|---|
| Gate | **G** + **H** |
| Objective | Adapters: connectivity, camera/QR still-image, share, device identity, secure storage. HID remains keyboard. |
| Allowed | `.Client` adapters |
| Forbidden | Printer/NFC/terminal products; manufacturer checkout SDK |
| API dependencies | Platform POS device register; QR resolve |
| Tests | Physical Android for camera/share/secure storage |
| Visual | QR scan degrade paths |
| Offline | None new |
| Device | Current MAUI set only |
| Security | No PAN/PIN/CVV/GCash secrets |
| Stop | Blocking Gate E on absent printers |
| Expected commit | `feat(pos-react): add capacitor device adapters` |

### POS-REACT-IMPL-12 — Controlled cutover

| Field | Value |
|---|---|
| Gate | **I** |
| Objective | Named cohort; rollback to MAUI; API compatibility window; no DB migration for host swap. |
| Allowed | Release/runbooks; `.Client` production config |
| Forbidden | Deleting MAUI; silent all-tenant cutover |
| API dependencies | Compatibility window only |
| Tests | Rollback drill; cohort checklist |
| Visual | Gate I remaining screenshot combinations |
| Offline | Devices must sync or accept LOCAL-UNSYNCED risk before host swap |
| Device | Cohort devices |
| Security | Production host/reverse-proxy decided |
| Stop | Personal/Owner/POS disposition incomplete for the **in-scope cohort**; if cohort is POS-only, Personal/Owner remain on MAUI (must be written) |
| Expected commit | `feat(pos-react): run controlled mobile client cutover` |

### Later packages (named, not sequenced in this map)

| ID (indicative) | Gate | Objective |
|---|---|---|
| POS-REACT-IMPL-P* | after E | Personal Mobile parity |
| POS-REACT-IMPL-O* | after E | Owner essentials parity |
| POS-REACT-IMPL-OPS* | after E | Catalog admin, inventory, customers/credit, shifts, purchasing, reports |
| POS-REACT-IMPL-J | **J** | MAUI retirement after PO + full disposition |
| POS-REACT-IMPL-K | **K** | Capacitor iOS |

Those Personal/Owner/ops packages are **required for Gate J** even if omitted from the first twelve IDs.

---

## 3. Coexistence reminder

Until Gate J, MAUI remains the production-path Mobile Client. Organization Web stays non-checkout. Platform Admin stays Web-only. This plan does not split Personal / Owner / POS into three products.

---

## 4. Authorization lock

| Item | Status |
|---|---|
| Execute IMPL-01…12 | **NO** |
| Create `.Client` now | **NO** |
| MAUI retirement | **NO** |
| Main merge of implementation | **NO** |
