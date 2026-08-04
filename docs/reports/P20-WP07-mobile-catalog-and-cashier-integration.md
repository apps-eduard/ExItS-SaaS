# P20-WP07 — MAUI Catalog Discovery and Cashier Integration

| Field | Value |
|---|---|
| Status | **Code Complete** |
| Phase | [Phase 20](../phases/phase-20-global-product-catalog-and-business-template-onboarding.md) — **Open** |
| Specs | [07-mobile-and-cashier-experience](../specs/product-catalog/07-mobile-and-cashier-experience.md), [05-merchant-onboarding](../specs/product-catalog/05-merchant-onboarding-and-import.md) |
| Commit | `3ea856c` |
| Date | 2026-08-05 |
| Device Verified | **No** |
| Production Ready | **No** |

## 1. Objective

Deliver ManageCatalog-gated MAUI onboarding and global catalog discovery/import UX on top of WP06 POS import APIs and Platform merchant discovery, while keeping cashier selling on local POS catalog data only.

## 2. Delivered capability

- `IMerchantCatalogDiscoveryClient` / `MerchantCatalogDiscoveryClient` → Platform `/api/v1/catalog/templates|products/search|categories`
- `IPosCatalogImportClient` / `PosCatalogImportClient` → POS `/api/v1/pos/catalog-imports/*`
- `/catalog/import` — choose published template → preview samples → confirm first batch → navigate to job progress
- `/catalog/global` — search, barcode, category filter, multi-select import
- `/catalog/import/jobs/{id}` — poll job progress (queued/processing/completed/warnings/failed)
- `/catalog/import/jobs/{id}/review` — review imported local products; edit link; opening-stock action → existing `/inventory/{id}/adjust`
- Catalog list actions: Browse global catalog + Use business template (ManageCatalog only)
- Cashier `/sales/new`: local name/SKU/barcode lookup, categories, tap-to-add tiles with placeholders; no global import clients
- Localization EN + fil-PH (`Catalog_Import_*`, `Catalog_Global_*`)
- Guard tests: Catalog import/global routes + ManageCatalog; Personal shell excludes catalog import routes; SaleCheckout documents local-only selling

## 3. Explicit exclusions

- Opening stock mutation inside catalog forms (uses existing inventory adjust)
- Platform overwrite of local commercial fields
- Cashier global catalog administration by default
- Lazy remote image CDN pipeline (stable CSS placeholders only)
- Physical-device closeout (P20-WP08)
- Unrelated dirty WIP (compose, AccessTokenUseCases, SignIn, Select, ExpenseCategories, CatalogCategories WIP, SalePageGuardTests dirty, tools/p18-*) left untouched

## 4. Pages / routes

| Route | Page | Gate |
|---|---|---|
| `/catalog/import` | `CatalogImport.razor` | ManageCatalog |
| `/catalog/global` | `CatalogGlobalBrowse.razor` | ManageCatalog |
| `/catalog/import/jobs/{JobId}` | `CatalogImportJob.razor` | ManageCatalog |
| `/catalog/import/jobs/{JobId}/review` | `CatalogImportReview.razor` | ManageCatalog |
| `/sales/new` | `SaleCheckout.razor` (local tiles polish) | CreateSale / local catalog |

## 5. Build / test evidence

| Check | Result |
|---|---|
| ApiClient Release build | Succeeded |
| MAUI Android Release build | Succeeded (0 errors; existing NU1903 SQLite warnings) |
| CatalogPageGuardTests + SalesCashierPageGuardTests + PersonalPageGuardTests | **21 passed**, 0 failed |

## 6. Security limitations

- Development-stage auth unchanged; not production-ready.
- Import/discovery mutations require ManageCatalog (Cashier matrix excludes it).
- Platform discovery requires authentication; entitlement-aware filtering remains baseline.
- Platform unavailability fails discovery/import only — local checkout continues via `IPosCatalogClient`.

## 7. Risks / open decisions

- Template preview resolves sample products one-by-one (acceptable for small first-batch previews).
- Job polling is client-side every 2s while the progress page is open.
- Image URLs from Platform are not fetched yet; placeholders avoid blocking sell/import UX.

## 8. Files / docs changed

- ApiClient discovery + import clients + DI
- Application abstractions + template summary DTO
- MAUI Catalog import/global/job/review pages; CatalogProductsList links; SaleCheckout tiles; app.css placeholders
- PosResources.resx + fil-PH
- Catalog/Sales/Personal guard tests
- This report + phase progress

## 9. Exact next work package

**P20-WP08** — End-to-end validation and user closeout (physical device).
