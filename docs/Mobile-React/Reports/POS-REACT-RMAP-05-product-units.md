# RMAP-05 — Base UOM + SellingMode + product units

## Status

**COMPLETE**

## Baseline

starting SHA: `3441c50e754a2461bdc4ebf62ef7d20ce5420d44` (post RMAP-04 docs)

## Contract review

| Area | Finding |
|------|---------|
| Backend | `UnitOfMeasure`, `SellingMode` (PerItem/ByWeight), `CatalogProductUnit` with `MultiplierToBase`, independent sell prices, shared base pool |
| API | Create/Update `units[]` via `PosCatalogProductUnitInput`; omit units to keep/defaults |
| ByWeight | Requires `Kilogram` base UOM |
| Milligram | Not in enum — excluded |
| Open Sack | Excluded (not a domain workflow) |
| React prior | Create incorrectly used `ByUnit` — fixed to `PerItem` |
| Owner decision | NO |

## Implementation

- Catalog options codes mirror `PosCatalogOptions`
- Product form: base UOM, selling mode, base price, optional purchase/sell package editor
- Independent unit prices preserved (not base × multiplier)
- ByWeight locks base UOM to Kilogram
- Unit replace omits `unitId` (server soft-deactivate + insert)

## Exclusions

- Milligram (RMAP-B02)
- Open Sack
- Today’s Prices (RMAP-06)
- Inventory (RMAP-07)

## Tests

- Vitest: rice-style independent prices; invalid multiplier; box×12
- Playwright: package config; ByWeight lock; viewports
- Regression: RMAP-04 suite

## Next

RMAP-06 — Today’s Prices
