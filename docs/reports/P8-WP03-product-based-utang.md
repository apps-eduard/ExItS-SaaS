# P8-WP03 — Product-Based Utang

Phase marker: `P8-WP03-product-based-utang`

## Status

**Complete with documented risks.** Atomic Product-Based Utang checkout creates one immutable completed sale and one linked remarks-based credit entry in a single transaction. Cash/ManualGCash sales unchanged. **No** inventory deduction, split tender, credit limits, offline Utang, refunds, or P8-WP04+.

Feature commit: `cd58f5c7dc1b9d31497429ef1d025546a0def09c`

## Delivered capability

| Area | Delivered |
|---|---|
| Payment method | `Utang` alongside Cash / ManualGCash |
| Checkout | Active same-org customer + active catalog products; server-authoritative prices/totals |
| Linkage | `sales.customer_id`, `sales.linked_credit_entry_id`, `credit_entries.source_sale_id` |
| Credit | Amount = sale total; remarks `Product sale {SaleNumber}`; optional due date (`Set during Product-Based Utang checkout`) |
| Void | Atomic sale void + linked credit reverse; standalone linked reverse blocked |
| Persistence | Migration `AddProductBasedUtang` (`20260730190056`) |
| API | Extended `POST /api/v1/pos/sales` (+ CustomerId / DueDate); void peeks Utang for ReverseCredit |
| MAUI | `/sales/new` Utang branch; detail/ledger source-sale links; void explains linked reverse |
| Auth | Create: `store-sales-create` + `customer-credit-create`; Void: `store-sales-void` + `ReverseCredit` (`customer-credit-view`) |

## Transaction / linkage design

1. Authorize org + capabilities.
2. Validate active customer and products; reject zero-total Utang.
3. Allocate sale number; create sale with pre-generated `LinkedCreditEntryId`.
4. Create credit with `SourceSaleId` and system remarks; optional due-date audit row.
5. Single DB transaction (reuses ambient idempotency txn when present).

Circular FK avoided: no DB FK from `sales.linked_credit_entry_id` → `credit_entries` (unique index + app enforcement). FK `credit_entries.source_sale_id` → `sales` Restrict.

## Void / reversal policy

- Preferred: linked credit reversals only through Product-Based Utang sale void.
- Standalone reverse of a linked credit → `pos.credit_entry.reversal.requires_sale_void` (409).
- Subsequent repayments making reverse unsafe → `pos.sale.void.blocked_by_subsequent_utang` (409); history preserved.
- No refund or cash/GCash reimbursement transaction.

## Commercial matrix

| State | View | Create Product Utang | Void |
|---|---:|---:|---:|
| Trialing / Active / GracePeriod | Grant | Grant (both caps) | Grant (both caps) |
| PastDue / Cancelled / Expired | Grant | Deny | Deny |
| Suspended / missing / stale / unknown | Deny | Deny | Deny |

Both sales and credit capabilities must pass for create/void.

## Explicit exclusions

Inventory/stock, Cash/GCash+Utang split, deposits/partial at checkout, discounts/tax/VAT/fees/tips, credit limits/approvals/interest/penalties, installments, refunds/returns/exchanges, offline Product-Based Utang, receipt printing/tax invoices, POS operational roles.

## Online-only

No offline Product-Based Utang queue or local sale/credit projection for this path. Cart remains in-memory; reconnect required when offline.

## Tests and Android

| Suite | Passed | Failed | Skipped |
|---:|---:|---:|---:|
| Full `ExItS.slnx` Release (test projects) | **775** | **0** | **0** |

Baseline 759 preserved and exceeded.

Android Release APK:

`src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/bin/Release/net10.0-android/com.exits.pinoybusinesspos-Signed.apk`

Interactive device validation **not** claimed (`adb` unavailable) — **R-109** remains open.

## Risks

| ID | Notes |
|---|---|
| R-109 | No interactive Android Product-Based Utang validation |
| Dev headers | Org/commercial/actor headers Development/Testing-only |
| Manual linkage policy | Standalone reverse of sale-linked credits blocked by design |
| No stock | Sales still do not adjust inventory (deferred P8-WP04) |

## Portfolio independence

Root a nested foreign product tree must remain absent/untracked and outside `ExItS.slnx`.

## Documentation and Git

| Field | Value |
|---|---|
| Feature commit | `cd58f5c7dc1b9d31497429ef1d025546a0def09c` |
| Docs hash-record commit | `65f1ea2f8a1558e9476f1553c85a3bd62ec40c3a` |
| Final working tree | clean after push |

## Exact next work package

**P8-WP04 — Basic Inventory** completed separately; next authorized WP is **P8-WP05 — Expenses**.
