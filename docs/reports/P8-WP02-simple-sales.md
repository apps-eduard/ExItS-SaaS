# P8-WP02 — Simple Sales

Phase marker: `P8-WP02-simple-sales`

## Status

**Complete with documented risks.** Organization-isolated Cash and ManualGCash retail sales using the P8-WP01 catalog; server-authoritative totals; immutable completed history; explicit void; online-only. **No** inventory deduction, Utang sales, tax/discounts, refunds, split tender, gateways, or offline sale queue. P8-WP03 was not started.

Feature commit: `72a6fa9b1bb6f48610563d01ee10e608e99806e1`

## Delivered capability

| Area | Delivered |
|---|---|
| Domain | `Sale` + `SaleLine` with product snapshots; Completed/Voided; Cash / ManualGCash |
| Checkout | Reload active org products; authoritative price/UOM; qty rules; AwayFromZero 2-dp totals |
| Payments | Cash tender ≥ total with server change; ManualGCash optional reference (manual confirm only) |
| Sale number | `SALE-YYYYMMDD-<sequence>` via `pos.sale_number_sequences` + advisory lock |
| Void | Completed → Voided with reason + actor; no refund/inventory |
| Persistence | Migration `AddPosSimpleSales` (`pos.sales`, `pos.sale_lines`, `pos.sale_number_sequences`) |
| API | `/api/v1/pos/sales` POST/GET/GET{id}/void + checkout idempotency (`sale.checkout`) |
| MAUI | `/sales`, `/sales/new`, `/sales/{saleId}`; in-memory cart; reconnect when offline |
| Features | `store-sales-view`, `store-sales-create`, `store-sales-void` |

## Business rules

- Only active same-org catalog products may be sold.
- Client cart is temporary; no server sale until checkout succeeds.
- Combining duplicate product lines at checkout; server ignores client prices/names.
- Whole qty: Piece, Pack, Box, Bottle, Can, Sachet. ≤3 decimals: Kilogram, Gram, Liter, Milliliter, Meter.
- Monetary rounding: `MidpointRounding.AwayFromZero` to 2 decimal places (credit/repayment convention).
- Exactly one payment method per sale.
- Inactive SKU/barcode reservation remains a catalog concern; sales snapshot identity at checkout.

## Commercial matrix

| State | View | Create | Void |
|---|---:|---:|---:|
| Trialing / Active / GracePeriod | Grant | Grant | Grant |
| PastDue / Cancelled / Expired | Grant | Deny | Deny |
| Suspended / missing / stale / unknown | Deny | Deny | Deny |

## Explicit exclusions

Inventory/stock, suppliers, Utang/customer-credit sales, split/partial payments, cards/gateways/QR/GCash verification, discounts/tax/VAT/fees/tips, refunds/returns/exchanges/line voids, fiscal invoices, offline sales, POS operational roles.

## Online-only

No local sale projections or `sale.*` offline queue handlers. `OfflineOperationTypes.SaleCheckout` exists only for server idempotency headers. Cart clears on logout/org switch; offline UI shows reconnect-required.

## Tests and Android

| Suite | Passed | Failed | Skipped |
|---:|---:|---:|---:|
| Full `ExItS.slnx` Release | **759** | **0** | **0** |

Baseline 684 preserved and exceeded.

Android Release APK:

`src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/bin/Release/net10.0-android/com.exits.pinoybusinesspos-Signed.apk`

Interactive device validation **not** claimed (`adb` unavailable) — **R-109** remains open.

## Risks

| ID | Notes |
|---|---|
| R-109 | No interactive Android checkout validation |
| Dev headers | Org/commercial/actor headers Development/Testing-only |
| Manual GCash | No independent verification (by design) |
| No stock | Sales do not adjust inventory (deferred P8-WP04) |

## Portfolio independence

Root a nested foreign product tree must remain absent/untracked and outside `ExItS.slnx`.

## Documentation and Git

| Field | Value |
|---|---|
| Feature commit | `72a6fa9b1bb6f48610563d01ee10e608e99806e1` |
| Docs hash-record commit | `e5eb8cff75568684cd510897fa37c39e584b9061` |
| Final working tree | clean after push |

## Exact next work package

**P8-WP03 — Product-Based Utang** completed separately; next authorized WP is **P8-WP04 — Basic Inventory**.
