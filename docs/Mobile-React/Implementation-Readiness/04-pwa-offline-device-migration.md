# POS-REACT-READINESS-04 — PWA, Offline, and Device Migration Sequence

**Package:** POS-REACT-READINESS-04  
**Status:** Documentation only. No service worker, Capacitor, or LocalStore replacement.  
**Evidence base:** `origin/main` `5979a9ce008bb24a3257abd28ae79bc1a5a9b569`  
**Depends on:** [03-api-auth-security-readiness.md](03-api-auth-security-readiness.md), [pwa-and-capacitor-delivery.md](../pwa-and-capacitor-delivery.md), [offline-sync-auth-and-security.md](../offline-sync-auth-and-security.md), [device-and-payment-integration.md](../device-and-payment-integration.md)

This file turns accepted offline/device **principles** into an **implementation sequence**. It does not select storage libraries or Capacitor plugins.

---

## 0. Two independent offline layers (must stay independent)

| Layer | Name | Allowed contents | Forbidden contents |
|---|---|---|---|
| **Phase A** | STATIC APP CACHE | Hashed JS/CSS/fonts/icons, small app shell, HTML revalidation | Sales, payments, entitlements, tokens, outbox payloads |
| **Phase B** | AUTHORITATIVE LOCAL OFFLINE DATA | Encrypted outbox + projections equivalent to current LocalStore | Service-worker Cache Storage |

**Service worker cache ≠ financial LocalStore.**

---

## 1. First PWA layer (Gate D, after React scaffold)

Implement **only** this static layer before any financial offline work.

| Item | Requirement |
|---|---|
| Manifest | `name`, `short_name`, `start_url`, `display: standalone`, theme/background from ExItS tokens, language |
| Icons | Browser-required sizes including maskable where Android expects them |
| Standalone | Safe-area, status, offline/sync header remain visible (DOC-02) |
| Safe-area | Honor env(safe-area-inset-*); no second status-bar spacer (current MAUI uses host `SafeAreaEdges="Container"`) |
| Static shell | Enough chrome to show reconnect / Internet-required / empty sell floor |
| Hashed assets | Content-hashed JS/CSS; entry HTML revalidated (not long-lived immutable) |
| Update prompt | “New version available”; never silent reload with a non-empty cart/checkout |
| Network-only API policy | Service worker must **not** cache-first Platform or POS API JSON |
| No auth/session cache | Do not cache `Authorization` bodies or session documents |
| Connectivity UX | Header Online/Offline/Pending; ordinary vs sensitive Internet-required (MOBILE-D-058) |
| Install prompt | Optional; must not block selling in a normal browser tab |

Browser without install remains fully supported. PWA is an enhancement, not a second product.

iPhone/iPad may use this layer as **reachability** before Gate K. Do not promise background sync, NFC, printers, or store-install UX on iOS PWA (MOBILE-D-030).

---

## 2. PLM proving-ground checkpoint

Another branch currently validates PWA engineering patterns: `feat/plm-pwa-hardening`.

**PLM_PWA_PATTERN_REVIEW_REQUIRED**

This queue:

- does **not** depend on that source tree
- does **not** merge that branch
- does **not** copy PLM business routes, branding, API contracts, or authorization

After PLM H4 review, compare **only** reusable engineering patterns:

- service-worker safety (no API cache-first)
- update lifecycle (skip waiting vs cart-safe prompt)
- connectivity UX
- PWA tests
- responsive test approach

Pinoy Business POS must keep POS/Platform contracts, LocalStore semantics, and ExItS Mobile identity (not PLM IA).

---

## 3. Offline migration

### 3.1 PHASE A — PWA static/offline shell only

**May begin** with Gate D (after authorization).

Includes: manifest, SW static cache, hashed assets, update prompt, network-only APIs, connectivity chrome.

Does **not** include: encrypted outbox, cash-offline sale, catalog projection, customer/credit projections, Personal Utang store.

Gate E first selling slice stays **online cash** unless a later authorized package explicitly includes offline finance.

### 3.2 PHASE B — POS authoritative local offline parity (Gate F)

**Must not begin** until storage/encryption/outbox architecture is approved.

Preserve current cash-only evidence: `sale.checkout` dispatcher rejects non-Cash. Manual GCash/Utang/card remain online until product/backend change.

Map current LocalStore concepts 1:1 into a future coordination layer (physical engine unselected):

| Current LocalStore concept | Evidence | Phase B requirement |
|---|---|---|
| Per-context persistence | `LocalContextManager`: hash of userId + organizationId + productCode; Personal uses isolation marker GUID; **single active context** | Same isolation. No shared DB across users/orgs |
| Encrypted payloads | AES-GCM (`AesGcmLocalPayloadProtector`); key in SecureStorage, **not** SQLCipher | Encryption-at-rest equivalent; key outside plaintext store |
| Outbox | `offline_operations` ciphertext/nonce/tag, hash, idempotency_key, depends_on, queue_state, attempts | Same envelope fields and states |
| FIFO | Claim `ORDER BY created_utc ASC, operation_id ASC`; `ix_offline_ops_fifo` | Same order; no overtaking |
| Idempotency | Local key + payload hash; replay `Idempotency-Key` / operation headers | At-least-once; conflict on same key + different hash |
| Retry classes | Transient / AccessBlocked / Conflict / Permanent; max 8 attempts, exp backoff ≤5m + jitter | Same classifier; do not last-write-wins |
| BlockedByAccess | Retain queue row; reclaim → Pending when access returns (OD-10) | Never silently delete pending work on logout |
| Cash offline sale | `LocalSellingCatalogAndCashSaleStore` — **Cash only** | Same restriction until evidence changes |
| Catalog projection | local category/product/units + open-shift snapshot; inventory deduction is preview | Not inventory SoR |
| Customer/credit | Encrypted projections + balances + download checkpoints | Server remains authoritative |
| Personal projections | `LocalPersonalUtangStore` in Personal context | Separate context; required for Personal Gate J |
| Reconnect sync | `GET /api/v1/pos/sync/*` + outbox processor + connected-supplier sync tables | Pull then FIFO replay |
| Crash recovery | Abandoned `Syncing` claims recovered | Required |
| Schema versioning | Migrator v1–v9 | Versioned migrations on whatever engine is later chosen |

Queue states to preserve: `Pending` · `Syncing` · `Succeeded` · `RetryableFailure` · `PermanentFailure` · `Conflict` · `BlockedByAccess`.

### 3.3 Physical storage candidates (compare only — no selection)

Approved decision MOBILE-D-038: PWA and Capacitor may differ physically; they share **logical** contracts. Libraries remain unpinned.

| Host | Candidate families | Must prove before selection |
|---|---|---|
| PWA / browser | IndexedDB, OPFS, WASM-capable SQLite-class options | Encryption, quota, crash recovery, private-mode behavior, iOS Safari limits |
| Capacitor | Native SQLite-capable options + OS secure storage for keys | Per-context files, encryption key storage, background constraints |

Selection criteria (all required):

- encryption
- secure key storage
- crash recovery
- quota behavior
- per-user / org / product isolation
- migration / versioning
- idempotent replay

**No implementation and no library selection in this package.**

---

## 4. Device migration map

Parity is against **current MAUI capability**. Absent MAUI hardware is **not** a Gate E/F blocker.

| Capability | Current MAUI | Browser / PWA | Capacitor Android | Parity note |
|---|---|---|---|---|
| HID barcode | Typed search / keyboard wedge | **Same** (focus + keyboard) | **Same** | READY; no SDK |
| Camera product barcode | **Absent** | Optional later `BarcodeDetector`/getUserMedia; degrade to type | Optional later plugin | DEFERRED; not a blocker |
| ExItS QR decode | MediaPicker still-image + ZXing QR only | File input / getUserMedia still; live view optional | Camera plugin analogue | CAPACITOR_REQUIRED for MAUI-level camera; PWA degrade |
| QR generate | QRCoder PNG | Canvas/library in app | Same JS | READY |
| Image capture | MediaPicker product image | `<input type=file>` / capture | Camera plugin | PWA degrade; Capacitor for parity |
| Share | `Share.Default` initiated ≠ print | Web Share API or clipboard copy | Share plugin | PWA degrade; Capacitor for sheet parity |
| Secure storage | MAUI SecureStorage | HttpOnly cookie / Web Crypto — **not** localStorage tokens | Native secure storage (**plugin unselected**) | Package 03 |
| Connectivity | OS `Connectivity` + API reachability | `navigator.onLine` + API probe | Network plugin + API probe | Radio ≠ API health |
| Device identity | Secure install id + Platform PosDevice | Origin-scoped id is weaker; registration still Platform | Native install id analogue | Capacitor closer to MAUI |
| Printer | **Absent** | Optional `window.print` (not ESC/POS) | **Not assumed** | DEFERRED; separate authorization |
| Cash drawer | Logical shift cash only | Same software | Physical kick **not assumed** | DEFERRED |
| NFC | **Absent** | Generally unavailable | **Not assumed** | DEFERRED |
| Payment terminal | Fake/manual status UX only | Same | Real terminal **not assumed** | DEFERRED; no PAN/PIN/CVV in ExItS |

---

## 5. Capacitor order

Capacitor happens **after** the React/PWA application reaches sufficient feature maturity (existing Gate H, after C–G as applicable).

```text
React/PWA app (same codebase)
    → thin Capacitor Android host
        → adapters only
```

Native plugin code must **not** own:

- pricing
- tax
- sale completion
- entitlements
- roles
- financial ledger
- offline conflict policy

Capacitor ships a **snapshot** of web assets. Do not assume website service-worker updates the store binary. Do not assume OTA live update (MOBILE-D-031).

Physical Android validation is required for camera/QR, share, secure storage, connectivity, and (when Phase B exists) approved local persistence.

iOS remains Gate K.

---

## 6. Sequence (documentation order only)

```text
1. Gate D  Phase A PWA static layer
2.          PLM_PWA_PATTERN_REVIEW_REQUIRED (engineering patterns only)
3. Gate E  Online selling visual slice (no Phase B finance)
4.          Storage/encryption/outbox architecture approval
5. Gate F  Phase B LocalStore-equivalent parity (cash-only checkout)
6. Gate G  Device adapters for current capabilities only
7. Gate H  Capacitor thin host
```

Do not execute these steps in this documentation queue.

---

## 7. Authorization lock

| Item | Status |
|---|---|
| PWA production | **NOT AUTHORIZED** |
| Offline LocalStore replacement | **NOT AUTHORIZED** |
| Capacitor production | **NOT AUTHORIZED** |
| Printer/NFC/terminal products | **NOT AUTHORIZED** |
| Storage/plugin selection | **NOT MADE** |
