# P23-WP10 — Today's Prices (fast current-price update)

| Field | Value |
|---|---|
| Status | **Implemented** (current catalog SellingPrice bulk update; WP11+ not claimed) |
| Phase | [Phase 23](../phases/phase-23-multi-business-entitlements-and-variable-quantity-selling.md) |
| Date | 2026-08-11 |
| Device Verified | **No** |
| Production Ready | **No** |

## Status

WP10 adds an online-only **Today's Prices** workflow so owners/managers can update multiple current catalog selling prices quickly without opening Edit Product one-by-one. Historical sale lines remain immutable via existing WP06/WP08 snapshots.

## Current-state audit (before change)

| Layer | Existing path |
|---|---|
| MAUI | `CatalogProductEdit` → full `UpdatePosCatalogProductRequest` PUT |
| API | `PUT /api/v1/pos/catalog/products/{id}` + `ManageCatalog` |
| Domain | `CatalogProduct.UpdateDetails` + `NormalizeSellingPrice` (zero allowed; negative / >2dp rejected) |
| Auth | Commercial feature `store-catalog-manage`; role matrix Owner/Admin/Manager yes, Cashier no |
| Audit | No dedicated POS product-price audit writer (same as full product update) |
| Local cache | Edit page did not upsert LocalStore; checkout/sync refresh later |

Reuse: keep `NormalizeSellingPrice` / concurrency / ManageCatalog. Avoid N× full PUT (must resend name/SKU/barcode).

## UX

Route: `/catalog/todays-prices` (Products → **Today's Prices** tile).

Rows:

- Name
- Current price (`₱120 / kg` for ByWeight; `₱25` for PerItem)
- Editable new price (`CurrencyInput`)
- Changed rows highlighted; Apply / Reset

Search + category filter reuse catalog list patterns.

## Authorization

| Actor | Allowed |
|---|---|
| Owner / Admin | Yes (`ManageCatalog`) |
| Store Manager | Yes (`ManageCatalog`) |
| Cashier | No (ViewCatalog only) |
| Cross-org product id | Not found / fail closed |

Server: `POST .../products/prices` requires `UtangCapability.ManageCatalog`. UI hide alone is insufficient.

## PerItem vs ByWeight price semantics

| Mode | Catalog `SellingPrice` meaning | Display |
|---|---|---|
| PerItem | Price per selling unit | `₱27` |
| ByWeight | PHP per **canonical kilogram** | `₱135 / kg` |

No gram-priced catalog storage.

## Bulk update behavior

`POST /api/v1/pos/catalog/products/prices`

Request items: `ProductId`, `SellingPrice`, optional `ExpectedUpdatedAtUtc`.

**Partial success** (chosen for stall UX): per-item results; one SaveChanges for successful mutations.

| Case | Behavior |
|---|---|
| Unchanged price | Success, `Changed=false`, no `UpdatedAtUtc` bump |
| Duplicate ProductId in request | Fail item (`pos.catalog.price_bulk_duplicate`) |
| Missing / cross-org | Fail item (`pos.product.not_found`) |
| Negative / >2 dp | Fail item (existing domain codes) |
| Zero | Allowed (existing policy) |
| Empty request | HTTP 400 (`pos.catalog.price_bulk_empty`) |

Domain helper: `CatalogProduct.UpdateSellingPrice` (price only).

## Online-required behavior

- Route `/catalog/todays-prices` + action `catalog.manage` → OnlineRequired
- Client does **not** queue price edits in the sale outbox
- Offline / timeout / unavailable → clear reconnect messaging; drafts are not treated as saved

## Local catalog refresh

On successful item updates: `ILocalSellingCatalogStore.UpsertProductsAsync` with returned DTOs so new sales see the new price without waiting for a full sync.

## Historical snapshot safety

Changing Today's Prices updates **current** `CatalogProduct.SellingPrice` only.

Existing `SaleLine.UnitPrice` / line totals / receipts / returns / historical reports stay on snapshots.

WP08 offline v2 queued sales with `UnitPriceSnapshot` still sync at the snapshot price after live catalog changes.

Example: sale 1.200 kg @ ₱120 → ₱144 remains after catalog moves to ₱150; new sale @ ₱150 → ₱180.

## Audit behavior

Same as existing catalog product update: no new POS price-audit subsystem. Attribution remains organization-scoped API actor + commercial grants. Dedicated price-history deferred.

## Tests / counts (Release)

| Suite | Result |
|---|---|
| Unit (`CatalogDomainTests` UpdateSellingPrice + role matrix ManageCatalog + WP08 fidelity with UpdateSellingPrice) | included in 62 passed filter |
| Maui (`CatalogTodaysPricesUiTests` + CatalogPageGuard updates) | **10** passed |
| Integration (`PosCatalogTodaysPricesApiTests`) | **2** passed |

## Migration / schema impact

**None.** Current `SellingPrice` column only.

## Deferred

- WP11 onboarding / multi-BT UX
- Copy-yesterday / price-history subsystem
- Amount→weight; scale hardware
- Dedicated POS product-price audit stream

## Implementation commit hash

_(filled after commit)_
