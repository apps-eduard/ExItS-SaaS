# UOM, Selling Modes, and Conversions

## CURRENT — controlled UOM enum

`UnitOfMeasure` (`ExItS.PinoyBusinessPOS.Domain.Catalog.UnitOfMeasure`):

Piece, Pack, Box, Bottle, Can, Sachet, Kilogram, Gram, Liter, Milliliter, Meter.

**Milligram:** not in enum. Status: **PROVEN_MISSING** (see unresolved decisions).

## CURRENT — SellingMode

| Mode | Rule | Status |
|------|------|--------|
| `PerItem` | Whole/count selling | PROVEN_CURRENT |
| `ByWeight` | Requires Kilogram base; gram input normalizes to kg | PROVEN_CURRENT |

Precision: `WeightQuantities.CanonicalDecimals = 3`; qty `numeric(18,3)`; money 2 dp AwayFromZero.

Evidence: `SellingModes.EnsureCompatible`, `WeightedSaleQuantityTests`, P23 migrations (`AddPosProductSellingMode`, sale-line snapshots).

## CURRENT — Multi-UOM / package conversion (CRITICAL)

### Proven model

One physical product has:

1. **Base inventory unit** = `CatalogProduct.UnitOfMeasure` (authoritative on-hand unit)
2. Optional **Purchase** and **Sell** `CatalogProductUnit` rows with `MultiplierToBase`
3. **One** `InventoryAccount` on-hand pool in base quantity
4. Independent sell prices per sell unit

Conversion helper: `ProductUnitConversion.ToBaseQuantity` (static helper — not a DB entity).

### Rice example (proven in tests/docs)

| | |
|--|--|
| Base | kg |
| Buy | Sack = 50 kg |
| Sell kg | ₱55 / kg |
| Sell Sack | ₱2,600 / Sack (not forced to 50×55) |
| Receive 10×50kg | +500 kg |
| Sell 1×50kg sack | −50 kg |
| Sell 3.5 kg loose | −3.5 kg |

Evidence:

- Domain `CatalogProductUnit`, table `pos.product_units`
- Migration `AddPosProductUnitsAndBehavior`
- `docs/engineering/product-units-and-inventory-behavior.md`
- `RiceSellUnitCheckoutSemanticsTests`
- Sale/PO/GRN line snapshots (`SellingUnitId`, `MultiplierToBaseSnapshot`, entered qty, base qty)
- Offline LocalStore schema v9 sell units

Status: **PROVEN_CURRENT** for shared-pool multi-package selling.

### Named search terms

| Term | Classification |
|------|----------------|
| `CatalogProductUnit` / `MultiplierToBase` | PROVEN_CURRENT |
| `SellingUnitId` / `PurchaseUnitId` snapshots | PROVEN_CURRENT |
| `BaseUom` / `BaseUnit` column | PROVEN_MISSING (base is `CatalogProduct.UnitOfMeasure`) |
| `InventoryUnit` / `CanonicalUnit` / `AlternateUnit` types | PROVEN_MISSING (concept covered by base UOM + units) |
| `UnitConversion` / `ConversionFactor` / `PackSize` entities | PROVEN_MISSING (use `MultiplierToBase`) |
| `BreakPack` / `BreakBulk` / `Repack` workflows | PROVEN_MISSING |
| `Sack` | Display/package name only — not enum UOM |

Owner decision: explicit “Open Sack” workflow is **not** automatically required if canonical stock semantics suffice — CURRENT shared-pool model already supports that stance.

## OWNER-CONFIRMED alignment

Owner rice/feed/powder package requirements **align with CURRENT** multi-unit model. React must not invent parallel SKUs for package sizes of one physical pool.

## React

Sell-floor catalog read does not yet expose full sell-unit selection / ByWeight entry parity with MAUI. Status: **PROVEN_PARTIAL** / largely **MISSING** for unit UX.
