# P23-WP06 — Sales / inventory decimal quantity propagation

| Field | Value |
|---|---|
| Status | **Implemented** (ByWeight kg quantities through Sale + Inventory; WP07+ not claimed) |
| Phase | [Phase 23](../phases/phase-23-multi-business-entitlements-and-variable-quantity-selling.md) |
| Date | 2026-08-11 |
| Device Verified | **No** |
| Production Ready | **No** |

## Status

WP06 confirms existing `numeric(18,3)` / `decimal` quantity storage and wires **SellingMode-aware** validation plus a small **WeightQuantities** helper so ByWeight sales and inventory use canonical kilograms. Sale lines now snapshot SellingMode for historical fidelity. Offline local inventory deduction uses decimal arithmetic (no SQLite `REAL`). Weighted MAUI entry UX (WP09) and offline server re-price fidelity (WP08) remain deferred.

## Confirmed quantity types

| Store | Column | Type |
|---|---|---|
| PostgreSQL `sale_lines.quantity` | `numeric(18,3)` | already present |
| PostgreSQL `inventory_accounts.on_hand_quantity` | `numeric(18,3)` | already present |
| PostgreSQL `stock_movements.quantity_effect` | `numeric(18,3)` | already present |
| Money (`unit_price`, `line_total`, totals) | `numeric(18,2)` | unchanged |
| LocalStore `on_hand_quantity` | TEXT (invariant decimal string) | unchanged schema; math fixed |

**No widening of quantity columns.** Existing decimal storage is sufficient for core qty.

## Canonical kg rule

ByWeight products (WP05): `Unit = Kilogram`, `SellingPrice` = PHP / kg.

Persisted/sale/inventory quantity = **kilograms** only.

Examples: `1.200`, `0.350`, `0.075`.

## Gram → kg boundary

`WeightQuantities.NormalizeToKilograms(value, WeightInputUnit)`:

| Input | Result |
|---|---|
| `1` kg | `1.000` kg |
| `1.2` kg | `1.200` kg |
| `350` g | `0.350` kg |
| `75` g | `0.075` kg |

- Exact `decimal` division by `1000m` for grams
- Reject ≤0, unsupported units, and >3 decimal places in resulting kg (no silent round)
- Checkout API continues to accept **already-normalized kg** (`CheckoutSaleLineRequest.Quantity`); gram keypad is WP09

## Precision / rounding contract

| Kind | Rule |
|---|---|
| Quantity | ≤ **3** decimal places; over-precision **rejected** |
| Money | **2** dp, `MidpointRounding.AwayFromZero` (`SaleMoney.RoundMoney`) |
| Line total | `RoundMoney(unitPrice × quantity)` |
| Inventory balance | `numeric(18,3)` / decimal; no float |

Examples: `0.350 × 120 = 42.00`; `1.200 × 120 = 144.00`.

## PerItem quantity rule

- **SellingMode authoritative** (not inferred from Unit alone).
- PerItem + countable UOM (Piece/Bottle/Pack/Can/Box/Sachet): **whole** quantities only.
- PerItem + measured UOM (Liter, Kilogram bags, etc.): historical ≤3 dp rule **preserved** (compatibility).
- ByWeight: always ≤3 dp kilograms; unit must remain Kilogram.

## Sale / SaleLine behavior

- Checkout loads live `product.SellingMode` into `SaleLineDraft`.
- Line total uses snapshotted unit price × quantity.
- Mixed cart example: `2×25 + 1.200×120 = 194.00`.
- Client may submit normalized decimal kg directly (no MAUI weight UI in this WP).

## SaleLine snapshot fields

| Field | Status |
|---|---|
| ProductId, Name, Sku, Barcode | existing |
| UnitOfMeasureSnapshot | existing |
| UnitPrice, Quantity, LineTotal | existing |
| **SellingModeSnapshot** | **added** (immutable; default PerItem for historical rows) |

Justified so receipts remain meaningful if the catalog product’s mode later changes. Does **not** fix WP08 offline server re-pricing.

## Inventory behavior

- Sale deduction uses line quantity (kg) and product SellingMode for validation.
- Exact decimal: `50.000 − 1.200 = 48.800`; then `− 0.350 = 48.450`.
- Insufficient-stock remains `OnHand < required` with decimal compare.
- LocalStore deduction: SELECT → `decimal` subtract → TEXT write (removed `CAST AS REAL`).

## Persistence / API / SQLite

- API: `decimal Quantity` end-to-end; `PosSaleLineDto.SellingMode` added.
- JSON uses decimal (not float/double) in domain/contracts.
- Cosmetic trailing zeroes in JSON not required.
- Local receipt snapshot includes optional `SellingMode` (default PerItem).

## Migration impact

Additive only:

- `20260811183000_AddSaleLineSellingModeSnapshot` — `pos.sale_lines.selling_mode_snapshot` NOT NULL default `PerItem` + `ck_sale_lines_selling_mode`

Quantity columns: **no schema migration required**; existing `numeric(18,3)` is sufficient.

## Tests / results (Release)

| Suite | Result |
|---|---|
| `WeightQuantitiesTests` + `WeightedSaleQuantityTests` + `SaleDomainTests` | **57** passed |
| `InventoryAccountDomainTests` + returns + `LocalCashSaleOfflineStoreTests` | **18** passed |
| `AddSaleLineSellingModeSnapshotMigrationTests` | **1** passed |

## Known gaps deferred

- WP07 purchasing / returns / reporting full ByWeight matrix
- WP08 offline server price/qty snapshot fidelity
- WP09 weighted MAUI cart entry (kg/g keypad)
- WP10 Today’s Prices; WP11 onboarding
- Amount→weight; scale hardware

## Files changed (summary)

- Domain: `WeightQuantities`, `SaleMoney`, `SaleLine`, `StockMovement`, `SaleReturnLine`, error codes
- Application: checkout mapping, `PosSaleOptions`, DTOs, inventory deduct
- Infrastructure: SaleLine record/mapper/DbContext + migration
- LocalStore: decimal on-hand deduct; receipt SellingMode
- Maui: pass SellingMode into local cash-sale line snapshot
- Tests + Phase 23 + this report

## Implementation commit hash

`7cef45ff8dd530527b5731b7395470e92d68a16e`
