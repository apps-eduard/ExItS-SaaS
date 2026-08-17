# Product units and inventory behavior

## Why business type does not determine the inventory engine

Grocery, meat shop, bakery, feeds/agri, and other small retail businesses share **one** inventory model:

- one authoritative **base inventory unit** per product
- optional product-specific **buying** and **selling** packages
- usage flags that classify how the item is used

Business templates may **suggest** defaults (for example rice as bulk/kg, flour as ingredient). They must never hard-code separate inventory engines.

## Product behavior model

Authoritative flags on `CatalogProduct`:

| Flag | Meaning |
|------|---------|
| `CanBePurchased` | May appear on purchase orders / stock-in |
| `CanBeSold` | May appear on checkout |
| `CanBeUsedAsIngredient` | Foundation for future recipes (no BOM yet) |
| `IsProduced` | Foundation for future production (no auto production yet) |

`UsagePreset` is a **UI hint** only (`BuyAndSell`, `Bulk`, `Ingredient`, `MadeProduct`, `IngredientAndSellable`).

Plain UI wording:

- How is this item used?
- Buy and sell
- Buy in bulk / sell in smaller amounts
- Ingredient or material
- Made product

## Base inventory unit

`CatalogProduct.UnitOfMeasure` remains the authoritative base unit (Piece, Kilogram, Liter, …).

All stock balances, movements, transfers, stock counts, returns, and lot quantities are in this base unit.

Personal storefront available quantity for **tracked** products is the existing orderable value `OnHand − Reserved` (`InventoryAccount.AvailableQuantity`), not raw OnHand. Untracked products stay orderable and display as `Available` with no quantity. See [product images and storefront availability](product-images-and-storefront-availability.md).

Examples:

- Receive 10 × 50 kg bags → movement **+500 kg**
- Sell 1 × 5 kg pack → movement **−5 kg**
- Sell 0.75 kg meat → movement **−0.75 kg**

Do not maintain separate authoritative pools per package size unless they are separate SKUs.

## Buying units

`CatalogProductUnit` with `Kind = Purchase`:

- product-specific name (Bag, Sack, …)
- `MultiplierToBase` as `decimal` (never float/double)
- 1 Bag = 50 kg is **per product**, never a global rule

## Selling units

`CatalogProductUnit` with `Kind = Sell`:

- independent selling price per option (5 kg pack price is not forced to 5 × 1 kg)
- `AllowsCustomQuantity` for measured sales (meat, loose rice, oil)
- ByWeight products map to custom measured sell in kilograms for backward compatibility

### Canonical example — Rice

| | |
|--|--|
| Base inventory | kg |
| Buy | Sack = 50 kg (purchase unit; cost independent) |
| Sell kg | 1 kg = 1 kg base · **₱55 / kg** |
| Sell Sack | 1 Sack = 50 kg base · **₱2,600 / Sack** |

50 × ₱55 = ₱2,750 is **not** required to equal Sack price. Selling 1 Sack deducts **50 kg** from shared on-hand and charges **₱2,600**.

MAUI checkout:

- one enabled sell unit → tap-to-add (or weight dialog when custom/ByWeight)
- multiple sell units → **Sell as** entry dialog (unit chips, quantity, stock used when conversion ≠ 1, subtotal)
- cart shows entered sell-unit quantity × unit price; optional “from stock” base hint
- offline LocalStore **v9** carries sell units; checkout payload snapshots `SellingUnitId` + `EnteredQuantity`; server recomputes base

## Custom quantity sales

Entered quantity × unit price (per selling option) → line total.  
Inventory uses base quantity = entered × multiplier (or entered when multiplier = 1).

Precision: existing `SaleMoney` / `SaleLine.NormalizeQuantity` rules (`numeric(18,3)` qty, money 2 dp AwayFromZero).

## Historical snapshots

Transactional writes snapshot enough conversion data so later product edits cannot rewrite history:

| Surface | Snapshots |
|---------|-----------|
| Sale line | Selling unit id/name, entered qty, multiplier, base `Quantity`, unit price |
| PO line | Purchase unit id/name, ordered qty (purchase unit), multiplier |
| GRN line | Received qty (purchase unit), multiplier; inventory uses `BaseQuantity` |
| Connected link | Package label, multiplier, optional buyer purchase unit id |

## Purchasing integration

Existing lifecycle unchanged: Draft → Ordered → Goods Receipt → inventory.

Purchase-unit cost is still **operational only** (no new costing method). When a base-unit equivalent is needed: `purchaseUnitCost / multiplier` with money rounding.

Supplier Accept/Decline still **never** adds inventory.

## Connected supplier integration

Extends Phase 1 `BuyerSupplierProductLink` with conversion metadata. Supplier product id and buyer product id remain authoritative. Full supplier catalog stays online/paged; only linked products + conversion metadata project offline.

## Direct local-market Stock In

Adjust Stock remains for corrections and informal stock-in. Optional purchase-unit helper converts to base; owners can still enter base quantity directly. No supplier/PO required.

## Stock count / transfers / lots / returns

- Stock count reconciles **base** on-hand.
- Transfers move **base** quantity (optional display-unit helper may convert on entry).
- Lots store quantity in base units; FEFO allocates base quantity.
- Returns restore the original sale line’s **snapshotted base** quantity.

## Offline / LocalStore

Schema **v9**:

- usage flags on `local_catalog_product`
- sell units in `local_catalog_product_unit`
- linked conversion on `local_linked_supplier_product`

Offline checkout snapshots conversion; server validates/replays independently. No images; no full supplier catalog download.

## Server authority

Clients may compute for UX. Server validates unit ownership/active/kind, multiplier, quantity, and recomputes base quantity for writes.

## Performance

Batch-load units by product id list; indexes on `(organization_id, product_id)` and active kind. Avoid N+1 unit lookups on catalog pages.

## Migration / backward compatibility

Migration `20260814200000_AddPosProductUnitsAndBehavior`:

- usage defaults BuyAndSell
- seed 1:1 Purchase + Sell units for every existing product
- ByWeight → sell unit `AllowsCustomQuantity = true`
- does not rewrite historical stock balances or movements

## Deferred (next phase)

- Recipe / BOM editor
- Production runs
- Automatic ingredient consumption / finished-goods creation
- Yield / waste
- Manufacturing planning
- New inventory costing method
- Arbitrary global package conversions
- Full supplier catalog offline copy

## Examples

**Grocery — Jasmine Rice**  
Base: kg · Buy: Bag (1 = 50 kg) · Sell: 1 kg, 5 kg pack, 50 kg bag, custom kg

**Meat shop — Pork Belly**  
Base: kg · Buy: kg · Sell: custom kg

**Bakery — Flour**  
Ingredient · Base: kg · Buy: Sack (1 = 25 kg)

**Bakery — Pandesal**  
Made product · Base: piece · stock may be adjusted manually until production ships
