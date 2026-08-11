# P23-WP09 — Weighted-sale MAUI cashier UX

| Field | Value |
|---|---|
| Status | **Implemented** (ByWeight kg/g entry on checkout; WP10+ not claimed) |
| Phase | [Phase 23](../phases/phase-23-multi-business-entitlements-and-variable-quantity-selling.md) |
| Date | 2026-08-11 |
| Device Verified | **No** |
| Production Ready | **No** |

## Status

WP09 adds a lightweight MAUI weight-entry dialog for ByWeight products on the cashier sale screen. Cashiers enter kilograms or grams; quantities are normalized to canonical kg via existing `WeightQuantities` / `WeightEntry.TryNormalize`. PerItem +/- stepper behavior is unchanged. Online and offline checkout continue to use the same cart (WP08 snapshot path intact).

## UX behavior

**PerItem:** unchanged — tap adds 1; compact `QuantityStepper` for lines in cart.

**ByWeight:**
1. Tap product (browse or exact barcode/SKU match) → weight dialog (does **not** add 1 kg).
2. Enter amount + choose **g** or **kg** (session remembers last unit on this screen).
3. Add → cart stores canonical kilograms.
4. Edit via cart “Edit weight” or product-row kg button.

Product price shows **₱120 / kg** (not “each”).

## kg/g normalization

| Input | Cart / outbox / server |
|---|---|
| 350 g | 0.350 kg |
| 75 g | 0.075 kg |
| 1.2 kg | 1.200 kg |
| 0.001 kg | 0.001 kg |

Reuse: `WeightQuantities.NormalizeToKilograms` through `Application.Sales.WeightEntry`. Grams are never persisted.

## Cart behavior

Display (canonical kg):

- Tomato · ₱120 / kg × 0.35 kg · ₱42.00
- Coke · ₱25 × 2 Bottle · ₱50.00

Mixed total example: 2×25 + 1.200×120 = **₱194.00**.

ByWeight lines use Edit weight (not integer +/-).

## Validation

Friendly messages for zero/negative, >3 dp after kg normalize, unsupported unit, over max kg. Stock still checked against on-hand before apply.

## PerItem regression

Barcode/SKU exact match and browse tap still add +1 for PerItem. Steppers remain for PerItem only.

## Online / offline parity

Same `SaleCartService` feeds:

- online `ToCheckoutLines()`
- offline `ToCheckoutLines(includePriceSnapshots: true)` (WP08 v2)

Normalized kg is what goes into receipt_json and outbox snapshots.

## Tests / results (Release)

| Suite | Result |
|---|---|
| `WeightEntryTests` + `WeightedSaleCheckoutUiTests` + SalePage/ProductRow/Stock guards | **32** passed |
| `WeightQuantitiesTests` | **8** passed |

## Migration / schema impact

**None.** No PostgreSQL or LocalStore schema changes.

## Known gaps (WP10+)

- WP10 Today’s Prices
- WP11 onboarding / multi-business UX
- Scale hardware; amount→weight entry
- Physical Android device validation (WP14)

## Files changed (summary)

- Application: `WeightEntry` helper
- Maui: `WeightEntryDialog`, `SaleCheckout`, `SaleCartPanel`, `SalesUiOptions`, EN/FIL resources
- Tests: WeightEntry + WeightedSale UI guards; SalePage/ProductRow updates
- Docs: this report + Phase 23

## Implementation commit hash

`a1f45719da4c3ffc9e2f231d71493f45e187740d`
