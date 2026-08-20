# RMAP-06 — Today's Prices

## Status

**COMPLETE**

## Baseline

starting SHA: `ae614cab6cc7ca43d3eff1d829d3840e7ba3606a` (post RMAP-05 docs)

## Contract review

| Area | Finding |
|------|---------|
| Endpoint | `POST /api/v1/pos/catalog/products/prices` — ManageCatalog; partial success 200 |
| Concurrency | `expectedUpdatedAtUtc` **required** (fail-closed) |
| Scope | Product base selling price + primary Sell unit only; other unit prices independent |
| Cashier override | Excluded (RMAP-B01 / RMAP-12b) |
| Owner decision | NO |

## Implementation

- `/catalog/todays-prices` bulk editor with dirty tracking, sticky save, per-row conflict feedback
- Uses required concurrency tokens from list/load

## Next

RMAP-07 — Inventory tracking

## Validation closeout (RESUME 05 REVIEW REPAIR)

**Status:** COMPLETE — validation closeout complete

**Repair baseline:** `d4d81886a5c7159ab39f57d80cc31a1d61833bea`

**Shared implementation commit (process deviation — not rewritten):**
- `RMAP_06_07_SHARED_IMPLEMENTATION_COMMIT=YES`
- Implementation: `d3e4e3da32cbd562c6973bcad18480742ed9d64b`
- Shared docs: `4688709ab774f3cefdfa669fa8c5b4fe67641dbc`
- `HISTORY_REWRITE_USED=NO`

**Validation repair SHA:** `cb91145b0aa3140f7eb47c853998288aec40a66a`

### Application defects closed by validation

| Defect | Evidence | Fix |
|--------|----------|-----|
| Partial failure invalidated catalog and cleared failed dirty rows | Playwright partial-failure case | Skip `invalidateQueries` when `failedCount > 0` |
| Catalog create `UpdateAsync` missed staged EF entities (`pos.product.not_found`) | `PosCatalogTodaysPricesApiTests` / `PosInventoryApiTests` create helpers | Prefer change-tracker Local in `CatalogProductRepository.UpdateAsync` |

### Backend contract

| Suite | Result |
|-------|--------|
| `PosCatalogTodaysPricesApiTests` | Passed 3 / Failed 0 / Skipped 0 |
| UOM regression (`CheckoutSaleLineConversionTests`, `RiceSellUnitCheckoutSemanticsTests`, `ProductUnitConversion*`, `WeightedSaleQuantity*`) | Passed 31 / Failed 0 / Skipped 0 |

### React gates

| Gate | Result |
|------|--------|
| Vitest | 32 files / 116 tests passed |
| typecheck | PASS |
| lint | 0 errors (8 pre-existing fast-refresh warnings) |
| Prettier (touched) | PASS |
| build | PASS |

### Playwright (`e2e/rmap-06-todays-prices.spec.ts`)

Passed **9** / Failed **0**

Functional: dirty-only submit + concurrency token; partial failure; stale conflict; cashier denied; OrgAdmin alone denied.

Responsive matrix (overflow, dirty, sticky save, touch ≥~44px):

| Viewport | Result |
|----------|--------|
| 375×812 | PASS |
| 768×1024 | PASS |
| 1024×768 | PASS |
| 1440×900 | PASS |

### Flags

- `RMAP_06_PASS=YES`
- `RMAP_06_RESPONSIVE_MATRIX_PROVEN=YES`
- `RMAP_06_BACKEND_CONTRACT_REVALIDATED=YES`
- Cashier price override still OUT OF SCOPE (RMAP-B01)
- `RMAP_B03_DISCOUNT_STARTED=NO`
- `RMAP_08_STARTED=NO`

### Next

HARD STOP — send report to ChatGPT. Do not start RMAP-08 or RMAP-B03.
