# Mobile React — Offline, Sync, Auth, and Client Security

**Status:** Documentation only. Implementation is **NOT AUTHORIZED**.  
**Package:** MOBILE-REACT-DOC-05  
**Depends on:** [pwa-and-capacitor-delivery.md](pwa-and-capacitor-delivery.md), [frontend-architecture-and-reuse.md](frontend-architecture-and-reuse.md)

The future React client must preserve **existing POS financial and offline rules**. This file records those rules from current LocalStore / MAUI / POS API evidence. It does not authorize a new LocalStore, a new payment method, or production PWA/Capacitor.

STATIC APP CACHE (DOC-04 service worker) remains separate from AUTHORITATIVE LOCAL OFFLINE DATA (this file).

---

## 0. Current-system audit (do not guess)

### 0.1 LocalStore

`ExItS.PinoyBusinessPOS.LocalStore` is Microsoft.Data.Sqlite, per-context files, schema versioning (v9 in current docs/code comments).

Evidence:

- Generic encrypted outbox `offline_operations` (`OfflineOperationQueue`)
- AES-GCM row/payload encryption; key in SecureStorage `pos.local.payload.key` — **not** SQLCipher (deferred)
- FIFO by `CreatedUtc` then `OperationId`
- Crash recovery of abandoned `Syncing` claims
- `BlockedByAccess` retained, reclaimed when access returns — **not** deleted (OD-10)
- Encrypted customer/credit/repayment projections
- Local selling catalog + **cash** sale store (`LocalSellingCatalogAndCashSaleStore`)
- Personal Utang local store
- Selective connected-supplier linked products and local PO **drafts** (drafts are not submitted)

Queue states (code `OfflineQueueState`):  
`Pending` · `Syncing` · `Succeeded` · `RetryableFailure` · `PermanentFailure` · `Conflict` · `BlockedByAccess`

### 0.2 What is actually queueable today

| Operation | Evidence | Offline? |
|---|---|---|
| Cash checkout `sale.checkout` | `SaleCheckoutOfflineDispatcher` **rejects non-Cash**; action `sale.checkout.cash` is Queueable | **Yes, cash only** |
| Manual GCash / Utang / Card / electronic GCash sale | `SaleNonCashPayment` OnlineRequired; `SaleCheckout.razor` `offlineCashReady` excludes Utang, ManualGCash, Card, electronic GCash | **No** |
| Customer create/update, credit create, repayment/reversal/due-date | P7-WP03/WP04 + dispatchers | **Yes**, with capability revalidation |
| Catalog product create | `catalog.product.create` Queueable; metadata first, photos as files not SQLite bytes | **Yes (metadata)** |
| Inventory **management** (counts, transfers, adjust) | `InventoryManage` OnlineRequired; `/inventory` OnlineRequired | **No** |
| Local catalog inventory **projection** after cash sale | `ILocalSellingCatalogStore.ApplyLocalInventoryDeductionAsync` | Local preview only; not inventory SoR |
| Connected PO submit | OnlineRequired; local draft save is not a queued submit | **No submit** |
| Personal Utang contacts/entries | Queueable routes under `/personal/utang/people|lent|borrowed` | **Yes** (Personal context) |

Product requirements still say cash **and** manually confirmed GCash *may* be recorded offline. **Current MAUI/API path does not queue Manual GCash.** The future client must not treat Manual GCash as implemented-offline until a later authorized package matches the product rule. Until then, preserve **current** cash-only offline checkout.

### 0.3 Identifiers and server idempotency

- Client-generated **SaleId** on offline cash checkout; dispatcher uses durable SaleId for replay (`SaleCheckoutOfflineDispatcher`)
- Generic envelope: `OperationId`, `IdempotencyKey`, payload hash, device/user/org/product
- POS API: `PosIdempotencyService` — identity is organization + product + operation type + idempotency key
  - First success stored
  - Exact replay returns stored outcome
  - Same key + different payload hash → **conflict**
  - Serializable transaction
- HTTP: `Idempotency-Key`, `X-Pos-Payload-Hash`, `X-Pos-Operation-Id`, `X-Pos-Operation-Type` (`PosMutationIdempotencyHelper`)
- Checkout POST wrapped in `PosIdempotencyEndpointHelper.ExecuteMutationAsync` for `sale.checkout`

At-least-once sync is expected. Duplicates must converge to one financial record via this idempotency, not by silent rewrite.

### 0.4 GCash reference (evidence vs requirement)

**Requirement** ([pinoy-business-pos-requirements.md](../product/pinoy-business-pos-requirements.md)): local duplicate check where possible + server check on sync; conflicts must not silently change financial records.

**Code audit:** `Sale.NormalizeGCashReference` (required for ManualGCash, max 64, trim). Stored on the sale. **No unique index** on `gcash_reference` was found in `PosDbContext`. **No dedicated duplicate-GCash finder** was found in Application/Infrastructure during this audit.

Future work must **implement** local + server duplicate checks when Manual GCash is (re)enabled for offline — not claim they already exist. Until then, Manual GCash remains **online** checkout.

Do **not** store GCash PIN/OTP/account secrets (requirement + current electronic pending store stores identifiers only).

### 0.5 Inventory sync

There is **no** generic offline inventory mutation outbox. Inventory pages and `InventoryManage` are online-required. Offline cash sale may deduct from the **local selling catalog projection** for browse consistency. Server inventory is updated when `sale.checkout` syncs (online checkout deducts atomically — `SaleEndpoints` comment).

### 0.6 Customer credit

Offline credit/repay uses the same queue. Server remains authoritative (`UtangCapability.CreateCredit`, `customer-credit-create`, trial expiry). Local acceptance does not authorize later processing (`OfflineAccessRevalidator.RevalidateOperationAsync` maps operation type → capability). No time-based offline entitlement grace (R-022 still open in Phase 7 docs).

Projected outstanding = confirmed + pending credit − pending repayment (never below zero locally). No silent merge of conflicts.

### 0.7 Auth as implemented

| Host | Mechanism (evidence) |
|---|---|
| MAUI → POS API | `Authorization: Bearer` (`PlatformBearerHandler`); POS `PosPlatformBearerMiddleware` introspects via Platform `POST /api/v1/platform/auth/introspect` |
| Tokens | `ISecureTokenStore` / `MauiSecureTokenStore` (MAUI SecureStorage). Comment: **never passwords; never Preferences/localStorage for tokens** |
| Session facts | Access token, platform session token, grants, org/branch/device ids in SecureStorage |
| Offline PIN | Offline operating grant stored via SecureTokenStore; cold start may restore mutation rights without extending grant expiry |
| Browser Org/Personal Web | Cookie/session (ADR-022); not LocalStore |
| Dev headers | Dev/Testing only; Production fail-closed |

Electronic in-flight attempt IDs may live in MAUI **Preferences** (`MauiPendingPaymentStore`) — identifiers only, never card data. Access tokens must not follow that pattern.

Shell policy: protected POS after online validation; mid-session offline continues only while that **continuous process session** remains. Restart while offline: reconnect or PIN grant; cache must not unlock the prior context by itself.

---

## 1. Target conceptual architecture

```text
React UI
   |
Local client data (projections / catalogs / entity states)
   |
Local command / outbox (encrypted envelope)
   |
Sync engine (FIFO, retry class, access revalidate)
   |
POS API  (+ Platform API for identity/entitlement introspect)
   |
PostgreSQL POS / Platform databases
```

Principles (must match current subsystem):

- Stable client-generated IDs where the server already accepts them (SaleId, operation id, credit ids)
- Idempotent server processing (org + product + type + key + payload hash)
- At-least-once tolerant sync
- No duplicate financial records on retry
- Explicit queue + UX sync state
- Retry only transient failures; validation/authz/conflict/financial invariant → retain for review
- Durable pending commands (OD-10: not silently deleted on logout)
- Deterministic conflict: retain both sides; user Retry / Review / Refresh / Discard-local-never-confirmed — **no last-write-wins**
- **No silent alteration of completed financial records**

Unknown routes/actions fail closed to **online-required** (current `PosOfflineCapabilityPolicy`).

---

## 2. Local storage abstraction

Share **logical** repositories (outbox, cash sale, customer/credit, personal, selling catalog). Do **not** force one physical engine.

| Host | Physical strategy (planning) | Constraint |
|---|---|---|
| **PWA / browser** | Browser-durable storage capable of encryption-at-rest semantics equivalent to AES-GCM payloads + key outside plaintext DB | Origin quota; not a second SoR; **not** the service worker Cache API |
| **Capacitor** | Native-capable local DB/files + OS secure storage for keys/tokens (current MAUI analogue: Sqlite + SecureStorage) | Per user/org/product isolation like `ILocalContextManager` |

Do **not** finalize IndexedDB vs OPFS vs SQLite-wasm vs Capacitor SQLite **libraries** in this DOC. Choose at authorized implementation with validation (quota, encryption, crash recovery, iOS Safari limits).

PWA static cache ≠ this layer (DOC-04).

---

## 3. Financial offline rules (preserve)

| Rule | Preserve |
|---|---|
| Cash offline | Where current policy allows (`sale.checkout.cash` + open-shift snapshot + catalog projection) |
| Manual GCash offline | **Where allowed** — product may allow it; **current code does not queue it**. Do not enable in React ahead of MAUI/API |
| GCash reference | Required, normalized, no secrets. Duplicate check: requirement to implement on both client (pending+local) and server when that path is authorized; not evidenced as unique-index today |
| Customer credit | Entitlement + expiry + role gates on enqueue **and** on sync; trial block on new credit |
| Completed records | Conflict → review; never silent rewrite of completed sale/payment/credit |
| Electronic Card/GCash | Online; simulated gateway; identifiers-only pending store |
| Inventory ops | Online |
| Split tender | Not supported |

Offline cash line totals are **immutable snapshots**; server validates arithmetic and does not replace those prices from live catalog on that path (`SaleEndpoints` comment). Online carts still price from live catalog.

---

## 4. Connectivity and transaction visibility

Map current `PosSyncStatusKind` plus per-record entity states into UX the user can always read.

| UX state | Meaning |
|---|---|
| **Online** | API reachable (device radio is not enough — current `NotifyApiReachability`) |
| **Offline** | No usable API; local-only work if policy allows |
| **Syncing** | Outbox item claimed |
| **Pending changes** | Queue has unsynced work |
| **Sync delayed** | Retryable failure / backoff (`RetryableFailure`, next_attempt) |
| **Conflict / review required** | `Conflict`, `PermanentFailure`, `RecoveryRequired` (incl. missing payload key) |
| **Authentication expired** | Token/session invalid; mutations blocked; reconnect |
| **Server unavailable** | Transient 5xx/timeout; retry class Transient |

Every financial row must show one of:

| Label | Meaning |
|---|---|
| **LOCAL ONLY** | Never queued / not a server entity (e.g. cart, PO draft) |
| **PENDING SYNC** | In outbox (`Pending` / `Syncing` / retryable) |
| **SYNCED** | ServerConfirmed / `Succeeded` + server reference |
| **FAILED / REVIEW REQUIRED** | Permanent, conflict, blocked-by-access, key recovery |

Do not display SYNCED for unsynced local cash.

---

## 5. Auth + security (future hosts)

Server remains the authorization authority. UI gates are convenience.

| Delivery | Session model |
|---|---|
| **Browser / PWA** | Browser-safe session: prefer HttpOnly cookie on the web origin when compatible (existing web hosts). If Bearer is used in the browser, it must live in Web Crypto / credential-style storage — **never ordinary localStorage**. CSRF gap for cookie mutations remains an integration gate (DOC-03). |
| **Capacitor** | Native secure storage for Bearer (and related session keys), analogue of `MauiSecureTokenStore`. Introspect on POS API as today. |

Rules:

- Never put access tokens, payload keys, or PINs in URLs, logs, analytics, or problem-details copies shown to support without redaction
- Never store passwords
- Offline permission **snapshots** (feature grants on session) must not permanently override server state
- On reconnect: **server authority wins** — re-introspect, revalidate capabilities, reclaim or block queue (`BlockedByAccess`), never process with a revoked grant
- App restart offline: do not auto-unlock POS from cache; PIN grant or online reconnect (current shell policy)

---

## 6. Data security

- **Minimum necessary** local data: projections needed to sell/queue; not full Platform Admin data; not full connected supplier catalogs
- **Logout / revoke:** clear session keys from secure storage; **do not** silently delete encrypted outbox (OD-10). Processing waits for the same user/org/product reauthorization
- **Cache cleanup:** selling catalog and thumbs are disposable; pending financial ops are not “cache”
- **Device loss:** local DB is not the backup SoR (`LOCAL-UNSYNCED` in production deployment architecture). Disclose unsynced loss risk
- **No** payment credentials, GCash PIN/OTP, raw card/CVV, or wallet secrets on device
- **Sensitive logging prohibition:** no decrypted payloads, tokens, or customer PII in logs
- Key loss: fail closed; keep ciphertext; localized recovery; no overwrite (current protector policy)

---

## 7. Future test strategy

Required scenarios (implementation gate, not this DOC):

- Airplane mode sell (cash) / blocked non-cash
- Network flapping during sync
- Duplicate retries / same idempotency key
- App kill/restart mid-`Syncing` (abandoned claim recovery)
- Device reboot + PIN vs reconnect
- Outbox recovery after logout/login same context
- Expired / introspect-inactive session
- Server 403/409/validation rejection
- Payload-hash conflict
- Partial FIFO (dependency: credit after local customer)
- Clock skew (CreatedUtc / NextAttemptUtc)
- Duplicate GCash refs (local pending + server) **when** that path exists
- Inventory page remains blocked offline
- SW cache must not resurrect a completed sale as SYNCED

---

## 8. Non-goals

- Replacing LocalStore in this package
- Enabling Manual GCash or Utang offline checkout in React first
- SQLCipher decision
- Pinning IndexedDB/SQLite npm libraries
- Treating PWA Cache Storage as the outbox
