# RMAP-B03 — Final closeout (commercial discount payment boundaries)

## Status

**FINAL CLOSED**

Package: `POS-REACT-RMAP-B03-CLOSEOUT-01`  
Branch: `feat/pos-react-client`

## Baseline

| Item | Value |
|------|-------|
| Starting SHA | `5abf33d2f4ada1f39f498922f0ced9bd325b40c1` |
| Original B03 implementation | `431e51040539bb4fcaba03e935df4b46c60fed3a` |
| Zero-total electronic safety repair | `4af3aec63ac5457bdce75afd7217b7de23d06b94` |
| Contract | [POS-REACT-RMAP-B03-sale-discount-contract.md](./POS-REACT-RMAP-B03-sale-discount-contract.md) |

## Current payment product rule

| User-facing label | Internal domain | Product status |
|-------------------|-----------------|----------------|
| Cash | `SalePaymentMethod.Cash` | **CURRENT** |
| GCash | `SalePaymentMethod.ManualGCash` | **CURRENT** (manual confirmation; not a gateway) |
| Utang | `SalePaymentMethod.Utang` | **CURRENT** |
| — | `SalePaymentMethod.Card` | **FUTURE** provider/API infrastructure only |
| — | `SalePaymentMethod.GCash` | **FUTURE** provider/API infrastructure only |

Cashiers and customers should see **GCash**, not `ManualGCash`. `ManualGCash` is an internal distinction only.

Do **not** expose Card or provider/API GCash in current React checkout UX.

## Zero-total matrix (Amount to Pay = ₱0 after commercial discount)

| Method | Result | Error (if reject) |
|--------|--------|-------------------|
| Cash | **ALLOW** → Completed | — |
| GCash / ManualGCash | **ALLOW** → Completed (no PaymentAttempt) | — |
| Utang | **DENY** | `pos.sale.utang.total_must_be_positive` |
| Card | **DENY** (defensive) | `pos.sale.electronic.total_must_be_positive` |
| provider/API GCash | **DENY** (defensive) | `pos.sale.electronic.total_must_be_positive` |

Utang with a **positive** remaining Amount to Pay after discount remains **valid** (debt = net Total, not gross).

## API proofs (POST `/api/v1/pos/sales`)

| Case | Result |
|------|--------|
| Full discount Cash | Completed; GrossSubtotal > 0; DiscountTotal = GrossSubtotal; Total = 0; inventory issued |
| Full discount ManualGCash | Completed; Total = 0; no provider PaymentAttempt path |
| Discounted Utang ₱1000 − ₱200 | Completed; Total = 800; credit Amount = 800; UnitPrice unchanged; qty issued |
| Full discount Utang | Rejected; no sale; no credit; no inventory deduction |
| Full discount Card / provider GCash | Rejected before AwaitingPayment; no durable sale; on-hand unchanged |

## Friendly money wording (documentation lock — no React UI in this package)

| Domain / API | User UI |
|--------------|---------|
| GrossSubtotal | Total Amount |
| DiscountTotal | Discount |
| Total | Amount to Pay |
| AmountTendered | Cash Received |
| ChangeAmount | Change |

Fully discounted example:

```text
Total Amount      ₱500.00
Discount         -₱500.00
Amount to Pay       ₱0.00
No payment required
```

Do not show ordinary users: Amount Tendered, Net Subtotal, Commercial Adjustment, Settlement Amount (unless a specialized admin/accounting screen requires it).

## Roadmap

- **RMAP-11b — Commercial Discount UX** formally defined after RMAP-11 Checkout / Sale and before RMAP-12 Payments / Void.
- Stale authoritative references to **RMAP-09b** for discount UX are corrected to **RMAP-11b**.
- RMAP-11b / RMAP-08 / RMAP-B04 / RMAP-TAX: **NOT STARTED**.

## Migration

New migration: **NONE**  
Existing: `AddPosCommercialSaleDiscounts` / `20260820214748`

## Exclusions

- React discount / checkout UI
- Card or provider GCash product UX
- RMAP-08, RMAP-B04, RMAP-TAX
- Promotions, coupons, regulatory discounts
- Price override (RMAP-B01 / RMAP-12b)

## Next

**HARD STOP.** Do not start RMAP-08, RMAP-11b, RMAP-B04, or RMAP-TAX.

## Git

| Item | SHA |
|------|-----|
| Implementation/test closeout | `184b71d2e70b776f52064d4dded6d8c3c0358f60` |
| Docs | recorded in ChatGPT final report after docs commit |

Do not chase HEAD with SHA-only follow-up commits.
