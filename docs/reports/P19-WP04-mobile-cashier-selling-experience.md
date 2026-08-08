# P19-WP04 — Mobile Cashier Selling Experience

| Field | Value |
|---|---|
| Status | **Code Complete** |
| Phase | [Phase 19](../phases/phase-19-mobile-pos-operations-and-cashier-experience.md) — **Open** |
| Commit | 94a354d |
| Production-ready | **No** |
| Device Verified | **No** |
| Date | 2026-08-04 |

## 1. Objective

Complete cashier-first selling workflow: shift-gated checkout, search/scan, category filter, product tiles, cart, cash tender/change, confirm, navigate to receipt.

## 2. Existing reuse

Sale checkout APIs, catalog lookup/list, cart service, shift current endpoint, payment method rules.

## 3. Delivered

- SaleCheckout: open-shift requirement with CTA to open shift
- Category filter + browse tiles (active products) + search/scan add-to-cart
- Quantity adjust, cart review, cash tender + change preview, GCash/Utang paths retained
- **Visual system (follow-on):** landscape three-pane + portrait sticky cart sheet; `QuantityStepper`; `--pos-*` tokens — see [mobile-production-ui-redesign](mobile-production-ui-redesign.md) (phone **Retest**; not Device Verified)
- **Card / GCash (simulated):** checkout → `AwaitingPayment` → payment-attempt panel (secure checkout URL, QR/deep link, Dev simulate); see [P19-card-gcash-payment-ui-and-simulation](P19-card-gcash-payment-ui-and-simulation.md)
- Optional cash customer selection UI (session-only; server still accepts CustomerId on Utang only)
- Post-checkout navigation to `/sales/{id}/receipt`

## 4. Residuals

- Cash sale CustomerId not accepted by existing backend contract — optional cash customer is UI-only until a future API decision
- Hardware barcode scanners rely on keyboard wedge into lookup field
- Offline cash checkout / cold-start PIN is documented separately: [P19-offline-operability-foundation](P19-offline-operability-foundation.md) (physical A–S incomplete; Not Device Verified)
- Route/action offline vs online policy: [P19-offline-connectivity-capability-matrix](P19-offline-connectivity-capability-matrix.md)

## 5. Tests

`SalesCashierPageGuardTests` — checkout workflow surfaces and receipt navigation.

## 6. Authorization

CreateSale (and CreateCredit for Utang). Shift open required for checkout when shift policy applies.

## 7. Status

**Code Complete.** Phase 19 remains **Open**. Not Device Verified.
