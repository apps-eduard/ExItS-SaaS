# P23-WP07 — Purchasing / returns / reporting weighted quantity propagation

| Field | Value |
|---|---|
| Status | **Implemented** (ByWeight kg through purchasing, returns, adjustments, reporting DTOs; WP08+ not claimed) |
| Phase | [Phase 23](../phases/phase-23-multi-business-entitlements-and-variable-quantity-selling.md) |
| Date | 2026-08-11 |
| Device Verified | **No** |
| Production Ready | **No** |

## Status

WP07 audits and wires **SellingMode-aware** quantity normalization (reuse `SaleLine.NormalizeQuantity` / WP05–WP06 helpers) through purchasing/receiving, sale returns restock, inventory adjustments/stock counts, and report/refundable DTOs so ByWeight quantities stay canonical **kilograms** (≤3 dp) with money at 2 dp. No second weight-conversion system. No WP07 schema migration.

## Purchasing / stock receiving

- Purchase submit snapshots accept optional `SellingMode` (`PurchaseOrderLineSnapshotInput`).
- Ordered/received qty normalized via `PurchaseOrderLine.NormalizeQuantity` → `SaleLine.NormalizeQuantity`.
- ByWeight requires Kilogram (`SellingModes.EnsureCompatible`); cost is **per kg**; line total = RoundMoney(qty × unit cost).
- Goods receipt / `PurchaseStockService` pass product `SellingMode` into receipt lines and `StockMovement.PurchaseReceipt`.
- API boundary remains **canonical kg** (no new gram-entry UI).

Example: Tomato `10.500` kg @ PHP 80/kg → line total PHP `840.00`; receive `25.500` kg increases on-hand exactly.

## Cost-per-kg semantics

For ByWeight: `UnitPurchaseCost` / cost price = **PHP per canonical kg**. Inventory movement stores kg only (no grams persisted). No FIFO/LIFO/WA redesign.

## Returns

- `SaleReturnLine.Create` already normalizes return qty with `saleLine.SellingModeSnapshot` and refunds with historical `saleLine.UnitPrice`.
- Partial weighted returns allowed; cannot exceed sold qty (including repeated partials).
- `SaleReturnStockService` prefers **sale line SellingModeSnapshot** for restock movements (falls back to live product mode only when line unavailable).
- `PosRefundableSaleLineDto` exposes `SellingMode` from the sale-line snapshot.

Example: sold `1.200` kg @ PHP 120/kg; return `0.350` kg → refund PHP `42.00`; inventory `+0.350` kg.

## Historical snapshot behavior

Returns and refundable DTOs use immutable sale-line fields (`Quantity`, `UnitOfMeasureSnapshot`, `SellingModeSnapshot`, `UnitPrice`). Live catalog mode/price changes after the sale do not alter historical return interpretation. Receipts/history continue to distinguish PerItem pcs vs ByWeight kg via those snapshots (WP06).

## Inventory adjustments

- Manual ±, opening, variance, stock-count paths pass product `SellingMode` into `StockMovement` factories so ByWeight rejects whole-number coercion and over-precision consistently.
- Auth/audit rules unchanged.

Examples: `+2.350` kg; waste `-0.425` kg; `0.001` kg valid.

## Reporting aggregation / display

- Product sales rows keep **decimal** quantity sums (no int cast).
- `ReportProductSalesRowDto` and `PosSalesByProductRowDto` expose `UnitOfMeasure` + `SellingMode` from sale-line snapshots for UI display (`2 pcs` vs `1.200 kg`).
- Money totals remain 2 dp. Canonical report qty remains **kg** (UI may later show grams; data stays kg).

Soft gap: return-only product rows (no sale in range) default `SellingMode` to `PerItem` because return lines do not persist SellingMode; sale-first rows keep snapshot mode.

## Edge cases covered

- `0.001` kg purchase/return/adjustment
- >3 dp rejected
- PerItem whole-qty regression
- return > sold / multi-partial cap
- historical snapshot vs later catalog change
- ByWeight + non-Kg unit rejected on purchase submit

## Migration / schema impact

**No WP07 schema migration required.** WP06 already established decimal qty storage and `SaleLine.SellingModeSnapshot`.

## Tests / results (Release)

| Suite | Result |
|---|---|
| `WeightedOperationsTests` | **11** passed |
| Related Purchase / SaleReturn / Inventory / Report filters | **78** passed (includes the 11) |
| Maui.Tests Return/Purchase/Report/Inventory filter | **15** passed |
| POS Api + IntegrationTests projects | Build succeeded |

## Known gaps deferred (WP08+)

- **WP08:** offline outbox / server sync must preserve immutable Quantity, Unit, SellingMode, UnitPriceSnapshot (no live-catalog re-price). — **done** ([P23-WP08](P23-WP08-offline-sale-snapshot-fidelity.md))
- WP09 weighted MAUI checkout UX (kg/g keypad) — **done** ([P23-WP09](P23-WP09-weighted-sale-maui-ux.md))
- WP10 Today’s Prices; WP11 onboarding
- Persisted SellingMode on return lines (optional; soft report gap only)
- Scale hardware; recipe inventory; restaurant workflows

## Files changed (summary)

- Domain: Purchasing (PO/receipt lines), Inventory (`StockMovement`, account/count), reuse SaleLine normalize
- Application: Purchase / Inventory / Advanced Inventory / SaleReturn stock + DTOs / Reporting
- Tests: `tests/.../Operations/WeightedOperationsTests.cs`
- Docs: this report + Phase 23 WP07 status

## Implementation commit hash

`8d3cb6c0d3ba355f9c8828b8ad07998147957f33`
