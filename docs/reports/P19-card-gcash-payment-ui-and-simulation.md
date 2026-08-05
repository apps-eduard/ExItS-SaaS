# P19 — Card / GCash Payment UI and Simulation

| Field | Value |
|---|---|
| Status | **Code Complete** · Card/GCash phone scenarios **Retest** |
| Phase 19 | [Phase 19](../phases/phase-19-mobile-pos-operations-and-cashier-experience.md) — **Open** |
| Phase 20 | [Phase 20](../phases/phase-20-global-product-catalog-and-business-template-onboarding.md) — **Open** |
| Device Verified | **No** |
| Production Ready | **No** |
| Processing | **Simulated only** — `FakePaymentGateway`; no live payment provider connected |
| Date | 2026-08-05 |
| Commit | `4818e31` |

## 1. Objective

Deliver provider-ready POS Card and GCash checkout UX on Mobile (`SaleCheckout`) with a **simulated** electronic payment pipeline (`IPaymentGateway` → `FakePaymentGateway` → signed webhook). Preserve the existing operator-confirmed Manual GCash sale path and add an API-level Manual GCash transfer fallback for environments that enable it. **No real card, CVV, OTP, PIN, or wallet credentials are collected or stored.**

## 2. Explicit status (do not over-claim)

| Claim | Value |
|---|---|
| Phase 19 | **Open** |
| Phase 20 | **Open** |
| Device Verified | **No** |
| Production Ready | **No** |
| Card / GCash scenarios | **Retest** (phone + Local Validation) |
| Live provider | **None** — all electronic settlement is simulated |
| Real credentials | **None** — no card numbers, CVV, OTP, PIN, or wallet secrets |

Phase 19 and Phase 20 remain **Open** until explicit user phone confirmation per [P19-WP08](P19-WP08-end-to-end-validation-and-closeout.md) and [P20-WP08](P20-WP08-end-to-end-validation-and-user-closeout.md).

## 3. Architecture

### 3.1 Layering

| Layer | Responsibility |
|---|---|
| **Domain** | `PaymentAttempt`, enums (`PaymentAttemptMethod`, `PaymentAttemptStatus`, `PaymentProvider`); `SaleStatus.AwaitingPayment`; `Sale.FinalizeAfterPayment` |
| **Application** | `IPaymentGateway`, `FakePaymentGateway`, use cases (`CreatePaymentAttempt`, `CancelPaymentAttempt`, `GetPaymentAttempt`, `ProcessPaymentWebhook`, `SimulatePaymentOutcome`, `VerifyManualGCashTransfer`) |
| **Infrastructure** | `payment_attempts` EF mapping; `AddSingleton<IPaymentGateway, FakePaymentGateway>()` |
| **API** | `PaymentAttemptEndpoints` — REST + unsigned webhook ingress with HMAC validation |
| **MAUI** | `SaleCheckout.razor`, `IPosPaymentAttemptClient`, `MauiPendingPaymentStore` |

Gateway types live in **Application** (not Domain). Architecture tests forbid Stripe/PayMongo strings in `FakePaymentGateway` and assert `payment_attempts` persistence without a separate `sale_payments` table.

### 3.2 `IPaymentGateway`

Provider-neutral contract (`ExItS.PinoyBusinessPOS.Application.Payments`):

- `CreateSessionAsync` — returns `ProviderReference`, optional `CheckoutUrl` (Card), `DeepLink` / `QrPayload` (GCash), `ExpiresAtUtc`
- `ValidateWebhookSignature` — HMAC-SHA256 over raw body
- `ParseWebhook` — maps provider payload to `PaymentWebhookEvent`

Real providers plug in later by replacing the DI registration; Domain and use cases stay unchanged.

### 3.3 `FakePaymentGateway` (Development / Testing only)

| Property | Value |
|---|---|
| `ProviderCode` | `Fake` |
| Dev signing key | `exits-fake-payment-dev` (constant in code — **not** a production secret) |
| Card session | `https://payments.fake.local/checkout/{reference}` |
| GCash session | deep link `exits-fake-gcash://pay/{reference}`; QR `EXITS-FAKE-GCASH\|{reference}\|{amount}\|{currency}` |
| Reference format | `fake_{paymentAttemptId:N}` |
| Expiry | 15 minutes |

**Never** used for real card or GCash credentials. Signature header: `X-ExItS-Payment-Signature` or `X-Fake-Payment-Signature`.

### 3.4 `payment_attempts` (PostgreSQL `pos` schema)

Migration: `20260805034213_AddPosPaymentAttempts`.

| Column group | Notes |
|---|---|
| Identity | `id`, `organization_id`, `sale_id`, `idempotency_key` (unique per org) |
| Method | `Cash`, `Card`, `GCash`, `ManualGCashTransfer` |
| Provider | `None`, `Fake`, `Manual` |
| Status | `Created` … `Expired`, `PendingManualVerification`, `Refunded` |
| Provider session | `provider_reference` (unique), `checkout_url`, `deep_link`, `qr_payload` |
| Safe metadata | `card_brand`, `card_last_four` only after Paid webhook — **never** PAN/CVV |
| Manual fallback | `external_reference`, `verified_by`, `verification_reason` |
| Events | `provider_event_sequence` for idempotent webhook ordering |

Constraints: positive amount; FK to `sales`; one active attempt per sale enforced in application logic.

### 3.5 End-to-end flow (Card / GCash electronic)

```text
Cashier checkout (Card or GCash)
  → POST /api/v1/pos/sales/checkout  → Sale status = AwaitingPayment (no stock deduction yet)
  → POST /api/v1/pos/sales/{saleId}/payment-attempts
  → FakePaymentGateway.CreateSessionAsync → RequiresCustomerAction + checkout URL / QR / deep link
  → Customer action (simulated in Dev via MAUI simulate buttons or POST .../simulate)
  → SimulatePaymentOutcome builds signed webhook body → ProcessPaymentWebhook
  → Attempt Paid → Sale.FinalizeAfterPayment + ISaleStockService.DeductForSaleAsync (once)
  → MAUI navigates to /sales/{id}/receipt
```

**Paid is never set by the client directly.** Only signed webhooks (or Dev simulation that posts through the same handler) may mark an attempt Paid and finalize the sale.

### 3.6 Sale `AwaitingPayment` rules

| Rule | Behavior |
|---|---|
| Entry | Checkout with `SalePaymentMethod.Card` or `SalePaymentMethod.GCash` creates sale in `AwaitingPayment` |
| Stock | **Not** deducted at checkout; deducted once when leaving `AwaitingPayment` via authoritative Paid attempt |
| Payment attempts | Only while `sale.Status == AwaitingPayment` |
| Active attempt | At most one non-terminal active attempt per sale; duplicate create → `409 pos.payment_attempt.conflict` |
| Method match | Attempt method must match sale payment method (Card sale → Card attempt; GCash sale → GCash attempt) |
| Finalize | `FinalizeAfterPayment(providerSafeReference)` → `Completed`; idempotent if already Completed |
| Void | Voided sales cannot be finalized from a payment attempt |
| Terminal attempt failure | Sale stays `AwaitingPayment`; cashier may cancel/retry with new idempotency key |

Cash and legacy Manual GCash checkout still complete immediately (`Completed`) without payment attempts.

### 3.7 Manual GCash transfer fallback (API)

Separate from legacy **Manual GCash** sale checkout (operator confirms reference on the sale record):

| Step | API |
|---|---|
| Create | `POST .../sales/{saleId}/payment-attempts` with `manualGCashTransfer: true`, `externalReference`, `method: "GCash"` |
| Gate | `PosPayments:EnableManualGCashTransfer` (default **off**; `true` in `appsettings.Development.json`) |
| Status | `PendingManualVerification` — sale remains `AwaitingPayment` |
| Verify | `POST /api/v1/pos/payment-attempts/{id}/verify-manual-gcash` — Owner / Admin / Store Manager |
| Duplicate ref | Rejected org-wide (`pos.payment_attempt.external_reference.duplicate`) |

**MAUI today:** cashier checkout uses legacy **Manual GCash** (immediate completion with operator checkbox). The `ManualGCashTransfer` payment-attempt path is **API-ready** for manager verification workflows; MAUI wiring is a future increment.

## 4. APIs

| Method | Route | Auth / gate | Purpose |
|---|---|---|---|
| `POST` | `/api/v1/pos/sales/{saleId}/payment-attempts` | Org scope + `CreateSale` | Create electronic or manual-transfer attempt |
| `GET` | `/api/v1/pos/payment-attempts/{id}` | Org scope + `CreateSale` | Poll status; auto-expire if due |
| `POST` | `/api/v1/pos/payment-attempts/{id}/cancel` | Org scope + `CreateSale` | Cancel active attempt |
| `POST` | `/api/v1/pos/payment-attempts/{id}/simulate` | Org scope + `CreateSale`; **404 in Production/Release** | Dev-only outcome → webhook path |
| `POST` | `/api/v1/pos/payment-attempts/{id}/verify-manual-gcash` | Org scope + `CreateSale` + Owner/Admin/StoreManager | Verify manual transfer |
| `POST` | `/api/v1/pos/payment-webhooks/{provider}` | HMAC signature (no bearer) | Provider callback (`Fake` today) |

Request bodies: `CreatePaymentAttemptRequest` (`method`, `idempotencyKey`, optional `externalReference`, `manualGCashTransfer`); `SimulatePaymentRequest` (`outcome`: `success` \| `decline` \| `cancel` \| `expire`).

Idempotency: `Idempotency-Key` header + body key; replay returns same attempt.

## 5. MAUI UX (`SaleCheckout.razor`)

| Payment method | UX |
|---|---|
| **Cash** | Tender + change; immediate receipt |
| **Manual GCash** | Warning banner; reference field; operator confirm checkbox; immediate completion (legacy) |
| **Card** | Checkout → awaiting panel: amount, status badge, provider ref, expiry countdown, “Open secure checkout” (Browser), Dev simulate Success/Decline when not Production |
| **GCash (electronic)** | QR payload display, “Open GCash” deep link, “I have paid” refresh poll, retry/cancel |
| **Utang** | Unchanged Product-Based Utang path |

Cross-cutting:

- **Online-only** — no offline sale queue; electronic attempts require connectivity
- **Shift-gated** — open shift required when shift policy applies
- **Pending resume** — `MauiPendingPaymentStore` persists in-flight attempt across back navigation / app restart
- **Dev simulate** — shown when `AppInfo.EnvironmentName` is not Production/Release (mirrors API Production block)

Localization: `Sales_EPayment_*`, `Sales_GCash_*` keys in `PosResources` (EN + fil-PH).

## 6. Security

| Topic | Posture |
|---|---|
| Credentials | **Never** store or transmit card numbers, CVV, OTP, PIN, wallet passwords, or gateway API secrets |
| DTO surface | Only `cardBrand`, `cardLastFour`, `providerReference`, `externalReference` (safe refs) |
| Webhook | HMAC-SHA256 required; invalid signature → `400` |
| Simulation | Disabled outside Development/Testing/Local; endpoint returns **404** in Production/Release |
| Fake signing key | Hard-coded dev constant — must be replaced with real provider secret management before any live gateway |
| Audit | Do not log webhook raw bodies with sensitive fields (none expected from Fake gateway) |
| Platform boundary | POS retail payments remain in `ExItS_PinoyBusinessPOS`; distinct from Platform SaaS billing |

See also [security.md](../engineering/security.md) invariant **53** and [data-classification-matrix.md](../engineering/data-classification-matrix.md).

## 7. Authorization

| Action | Requirement |
|---|---|
| Create / get / cancel / simulate attempt | Organization scope + commercial access + **`CreateSale`** (`UtangCapability.CreateSale` / `store-sales-create`) |
| Verify manual GCash transfer | Above + **Owner**, **Admin**, or **Store Manager** POS role |
| Payment webhook | No user session; provider + valid signature only |
| Cashier without CreateSale | Blocked at API and MAUI capability gate |

Manual GCash **sale** checkout (legacy) uses the same CreateSale gate. Verify-manual is intentionally manager-only.

## 8. Test coverage

| Suite | Coverage |
|---|---|
| `PosPaymentAttemptApiTests` (Integration, PostgreSQL) | Card success → finalize + stock once; decline/cancel/retry; GCash QR/deep link; webhook idempotency; out-of-order Paid after Failed ignored; expiry; org isolation; bad webhook signature; manual transfer enable/duplicate/disabled; Production simulate blocked; DTO excludes forbidden sensitive field names |
| `PosPaymentAttemptClientTests` | Route mapping; offline short-circuit on create |
| `PosSalesScopeArchitectureTests` | `payment_attempts` table; `IPaymentGateway` / `FakePaymentGateway`; no Stripe/PayMongo; online-only checkout guards |
| MAUI guards | `SalesCashierPageGuardTests` — checkout surfaces (Cash path); electronic paths rely on integration + manual retest |

Run with Testcontainers PostgreSQL for relational behavior. Do not use EF InMemory as proof of PostgreSQL constraints.

## 9. Phone validation checklist (Retest — do not mark Device Verified)

Perform on PhysicalDevice Local Validation (see [P19-WP08](P19-WP08-end-to-end-validation-and-closeout.md)) after APIs restarted.

### Card (simulated)

- [ ] Open shift → add product → select **Card** → confirm checkout
- [ ] Sale enters awaiting-payment panel; secure checkout URL opens in browser (fake host)
- [ ] Dev **Simulate success** → receipt; sale **Completed**; stock reduced
- [ ] New sale → **Simulate decline** → retry → **Simulate success** → receipt
- [ ] Cancel in-flight attempt → change payment method or retry with new attempt

### GCash (simulated electronic)

- [ ] Select **GCash** (electronic, not Manual GCash) → checkout → QR / deep link shown
- [ ] **I have paid** refresh while pending does not complete sale prematurely
- [ ] Dev simulate success → receipt; provider reference visible on attempt
- [ ] Expired/cancel paths leave sale awaitable for retry

### Manual GCash (legacy operator confirm)

- [ ] Select **Manual GCash** → enter reference → confirm received → immediate receipt (no payment attempt)
- [ ] Unconfirmed checkbox blocks checkout

### Regression / auth

- [ ] Cashier without CreateSale cannot reach checkout confirm
- [ ] Offline blocks electronic checkout (warning shown)
- [ ] App back during electronic payment → resume pending attempt on return
- [ ] Phase 20 imported product sells via Card/GCash paths unchanged ([P20-WP08](P20-WP08-end-to-end-validation-and-user-closeout.md) cashier section)

## 10. Explicit exclusions

- Live Stripe, PayMongo, Maya, or GCash API integration
- Card entry fields, 3-D Secure, or wallet PIN/OTP capture in MAUI
- Production webhook endpoints without real provider secrets and auth hardening
- MAUI UI for `verify-manual-gcash` manager workflow (API only today)
- Offline electronic payment queue
- Refunds/chargebacks through payment attempts (returns/refunds remain separate tender-matched flows)
- Claiming Device Verified, Production Ready, or Phase 19/20 Complete

## 11. Related documents

- [P19-WP04 — Mobile Cashier Selling](P19-WP04-mobile-cashier-selling-experience.md)
- [P19-WP05 — Sales and Receipt](P19-WP05-mobile-sales-and-receipt-history-ui.md)
- [P19-WP08 — E2E validation checklist](P19-WP08-end-to-end-validation-and-closeout.md)
- [P20-WP07 — Catalog + cashier integration](P20-WP07-mobile-catalog-and-cashier-integration.md)
- [07-mobile-and-cashier-experience](../specs/product-catalog/07-mobile-and-cashier-experience.md)

## 12. Status

**Code Complete.** Card/GCash simulated payment UI and API pipeline are implemented with `FakePaymentGateway` only. **Retest** on phone. Phase 19 **Open**. Phase 20 **Open**. **Not Device Verified.** **Not production-ready.**
