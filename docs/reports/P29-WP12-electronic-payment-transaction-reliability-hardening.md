# P29-WP12 — Electronic Payment Transaction Reliability Hardening

| Field | Value |
|---|---|
| Status | **Code Complete / Validation Evidence Recorded** |
| Phase | Phase 29 (Open / Partial Closeout — continued; not Phase 30) |
| Starting SHA | `73b3f06e27f669c6193d332fb1ff63a03dd37338` |
| Feature commits | `b8bcb21c`, `d5b102ce`, `863c533e` |
| Docs commit | `fc8b8c8c` |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |
| Production Payment Ready | **No** (`FakePaymentGateway` only) |
| Real Provider Integrated | **No** |
| Real Money Tested | **No** |

## Why Phase 29 / WP12

Phase 29 remained **Open / Partial**. This work fits WP03 (Financial & Transaction Integrity), WP04 (Inventory & Reservation), and WP08 (Concurrency & Reliability). No Phase 30 was created.

## CURRENT FLOW BEFORE

```
Electronic checkout → Sale AwaitingPayment (no stock hold)
  → CreatePaymentAttempt calls FakePaymentGateway BEFORE durable row
  → Paid webhook → DeductForSale → Completed
```

Risk: another checkout could consume the last unit while a customer was still paying.

## TARGET FLOW AFTER

```
Electronic checkout → Sale AwaitingPayment + Reserve (AvailableQuantity↓)
  → durable PaymentAttempt Created + COMMIT
  → FakePaymentGateway session (outside DB txn)
  → Paid → ConsumeReserved exactly once → Completed
  → Failed / Cancelled / Expired → Release exactly once
```

## Delivered

### Schema / domain
- `Sale.StockReservationState`: `None` | `Reserved` | `Released` | `Consumed`
- Migration `20260816123935_HardenElectronicSalePaymentReservation`
- Provider-backed Card/GCash only; Cash / Utang / ManualGCash unchanged at checkout deduct

### PaymentAttempt / gateway
- Durable `Created` attempt persisted **before** gateway I/O
- Stable idempotency identity reused for session create/recover
- `IPaymentGateway.GetSessionAsync` + Fake deterministic behaviors (`Success`, `DefiniteFailure`, `TimeoutBeforeCreate`, `TimeoutAfterCreate`)
- Additive `POST /api/v1/pos/payment-attempts/{id}/reconcile`
- Authoritative provider **Paid** may override Failed/Cancelled/Expired when event sequence advances

### Inventory
- Reserve at electronic checkout under product advisory locks
- Cash `DeductForSale` checks `AvailableQuantity` (cannot steal reserved stock)
- Consume on Paid; release on terminal non-Paid; late Paid after release uses deduct fallback then marks Consumed

## Explicit exclusions (remain FUTURE)

- Real payment providers / credentials / live settlement
- Chargebacks / electronic refund provider APIs
- CustomerOrder → Sale settlement
- Frontend redesign
- Production Payment Ready / Production Ready claims

## Validation evidence

| Suite | Result |
|---|---|
| Unit `SaleStockReservationStateTests` + `FakePaymentGatewayTests` | **PASS** (9) |
| Integration `FullyQualifiedName~P29Wp12` | **PASS** (11) |
| Integration `PosPaymentAttemptApiTests` | **PASS** (re-run after WP12) |
| Pos.Api Release build | **PASS** |

| Scenario | Result |
|---|---|
| Last-stock reserve blocks cash | **PASS** |
| Paid consumes once | **PASS** |
| Decline releases; cash can buy | **PASS** |
| Duplicate Paid | **PASS** |
| Timeout-after-create recovery | **PASS** |
| Definite gateway failure releases | **PASS** |
| Paid after Cancel (provider wins) | **PASS** |
| Paid after Expire (provider wins) | **PASS** |
| Reconcile recovers Created after restart seam | **PASS** |
| Cash path no reservation | **PASS** |

Concurrent Task.WhenAll race harnesses for Paid-vs-Cancel were not added as separate load tests; sequential provider-authority outcomes above are covered. Full WP08 load harness remains Partial.

## Exact next

Optional SMOKE EXPLAIN / concurrency load; keep Phase 14 Production backup incomplete; do **not** integrate a real payment provider in the next package without explicit authorization.
