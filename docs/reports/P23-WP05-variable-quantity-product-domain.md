# P23-WP05 — Variable-quantity product domain (SellingMode)

| Field | Value |
|---|---|
| Status | **Implemented** (product domain + Platform/POS persistence + import propagation; WP06+ not claimed) |
| Phase | [Phase 23](../phases/phase-23-multi-business-entitlements-and-variable-quantity-selling.md) |
| Date | 2026-08-11 |
| Device Verified | **No** |
| Production Ready | **No** |

## Status

WP05 introduces an explicit product **SellingMode** (`PerItem` | `ByWeight`) on Platform Global Catalog and merchant POS products, with Kilogram as the canonical ByWeight unit. Existing products default safely to **PerItem**. Platform → POS import propagates SellingMode and unit. Cart weight UX, sales/inventory propagation, offline snapshot fidelity, and Today’s Prices remain deferred.

## SellingMode domain definition

| Side | Type | Values |
|---|---|---|
| Platform | `ProductSellingMode` (`ExItS.Platform.Domain.GlobalCatalog`) | `PerItem`, `ByWeight` |
| POS | `SellingMode` (`ExItS.PinoyBusinessPOS.Domain.Catalog`) | `PerItem`, `ByWeight` |

Helpers: `ProductSellingModes` / `SellingModes` (parse, codes, compatibility). Wire format is the enum name string. Business Type does **not** imply selling mode; mode is persisted on the product.

No industry “modes” (Sari-Sari Mode, Vegetable Mode, etc.). One POS; product controls how it is sold.

## PerItem semantics

- Ordinary fixed-count products (Bottle, Piece, Pack, Can, …).
- Preserves all prior product behavior.
- Default for create when SellingMode omitted/blank.
- Migration default for existing rows: `PerItem`.

## ByWeight semantics

- Variable-weight / fresh products.
- **Must** use unit `Kilogram` (Platform `ProductUnit.Kilogram` / POS `UnitOfMeasure.Kilogram`).
- `ByWeight` + non-Kilogram unit is rejected (`InvalidGlobalProductSellingModeUnit` / `InvalidSellingModeUnit`).
- Barcode may be null (fresh goods); GS1 rules unchanged when a barcode is supplied.
- SellingMode is **not** inferred from Unit at runtime.

## Canonical weight-unit decision

| Decision | Choice |
|---|---|
| Canonical inventory / price base | **Kilogram** |
| Separate `BaseUnit` column | **Not added** — existing `Unit` / `UnitOfMeasure` is authoritative |
| Grams as input | Deferred (WP09 UX); normalize later as `350 g → 0.350 kg` |
| Amount→weight / scales | Out of scope |

## Price-per-unit semantics

For ByWeight products, `SellingPrice` means **price per kilogram** (PHP / kg). Example: `SellingPrice = 120`, `Unit = Kilogram` ⇒ PHP 120/kg.

No daily price history / Today’s Prices (WP10). Historical `SaleLine` behavior unchanged (WP08 offline fidelity).

## Product invariants

| Combination | Result |
|---|---|
| PerItem + Bottle/Piece/… | Valid |
| ByWeight + Kilogram | Valid |
| ByWeight + Bottle (or any non-Kg) | Invalid |
| Omitted SellingMode on create | PerItem |
| Omitted SellingMode on update | **Preserves existing** mode |

## Precision (documented for Phase 23)

| Kind | Rule (unchanged infrastructure) |
|---|---|
| Money | `numeric(18,2)`, AwayFromZero |
| Measured qty | existing decimal qty; ≤ 3 dp intended for later sale paths |
| Storage for weight | kilograms |

## Platform schema / API

- Column `catalog.global_products.selling_mode` (`varchar(32)`, NOT NULL, default `PerItem`)
- Checks: `ck_global_products_selling_mode`, `ck_global_products_selling_mode_unit`
- Migration: `20260811170242_AddGlobalProductSellingMode`
- DTOs / create / update / template product enrichment include `SellingMode`
- Admin Global Products UI: SellingMode select

## POS schema / API

- Columns: `pos.products.selling_mode`, `pos.catalog_import_items.selling_mode` (default `PerItem`)
- Checks: `ck_products_selling_mode`, `ck_products_selling_mode_unit`, `ck_catalog_import_items_selling_mode`
- Migration: `20260811170433_AddPosProductSellingMode`
- LocalStore schema v7: `ALTER … ADD COLUMN selling_mode … DEFAULT 'PerItem'`
- Merchant create/update requests accept optional `SellingMode`
- MAUI catalog create/edit/detail expose SellingMode (not cart weight entry)

## CSV behavior

- Optional trailing column **`SellingMode`** (`PerItem` | `ByWeight`, case-insensitive parse).
- Legacy files without the column remain valid ⇒ **PerItem**.
- Blank cell ⇒ PerItem.
- Invalid value rejected cleanly.
- Download template includes `SellingMode` and a ByWeight tomato sample (Kilogram, blank barcode, price per kg).
- Apply path stashes non-PerItem mode via reserved tag `import.sellingmode:` (same pattern as status tags).

## Platform → POS propagation

Import (template batch and selected products) maps Platform `SellingMode` + unit into pending import items and `CatalogProduct.CreateImportedSnapshot`. Mode is not derived from Business Type or category. Provenance unchanged (`PlatformTemplateId` / `PlatformGlobalProductId` / merchant-created).

## Backward compatibility

- Existing merchant/global products → PerItem after migration.
- Existing CSV templates without SellingMode still import.
- PerItem + existing units unchanged.
- No production startup auto-migration.

## Migrations

| DB | Migration |
|---|---|
| Platform | `AddGlobalProductSellingMode` (`20260811170242_…`) |
| POS | `AddPosProductSellingMode` (`20260811170433_…`) |
| LocalStore | schema version **7** additive column |

## Tests / results

Focused suites (Release):

| Suite | Result |
|---|---|
| Platform `ProductSellingModeTests` + CSV schema/row mapper SellingMode cases | **42** passed |
| POS `SellingModeDomainTests` + `CatalogImportJobTests` | **15** passed |
| POS import entitlement (`ImportTemplateBatch` / `ImportSelectedProducts`) | **8** passed |
| Platform migration `AddGlobalProductSellingMode` | **1** passed |
| POS migration `AddPosProductSellingMode` | **2** passed |
| Maui `CatalogProductFormUiTests` | **7** passed |

Admin + POS Infrastructure Release builds: **0 errors**.

## Known gaps / deferred WP06+

- WP06 sales/inventory quantity propagation for ByWeight
- WP07 purchasing/returns/reporting
- WP08 offline sync price/qty snapshot fidelity
- WP09 weighted-sale MAUI cart entry (kg/g)
- WP10 Today’s Prices
- WP11 onboarding multi-BT UX
- Amount-to-weight selling; scale hardware

## Files changed (summary)

- Platform Domain/Application/Infrastructure (+ Admin UI/localization)
- POS Domain/Application/Infrastructure/LocalStore/Api (+ Maui catalog forms)
- Tests (unit + migration integration)
- Phase 23 + this report

## Implementation commit hash

`95bfeca2192beb45d6bfeb3167a7e3f696c85b31`
