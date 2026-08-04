# P20-WP06 — Merchant Onboarding and POS Catalog Import

| Field | Value |
|---|---|
| Status | **Code Complete** |
| Phase | [Phase 20](../phases/phase-20-global-product-catalog-and-business-template-onboarding.md) — **Open** |
| Specs | [product-catalog/](../specs/product-catalog/) |
| Commit | `a849635` |
| Date | 2026-08-05 |
| Device Verified | **No** |
| Production Ready | **No** |

## 1. Objective

Deliver POS-owned merchant catalog import: template first-batch / next-batch and selected global products → PostgreSQL-backed background jobs → editable local product snapshots. Organization from trusted auth context. Platform outage must not affect existing selling. Cashier has no import permission by default (`ManageCatalog`).

## 2. Delivered capability

- Extended `CatalogProduct` / `ProductCategory` with Platform external refs and `CatalogSource` (Manual/Template/GlobalSearch/BulkImport); provenance immutable on local edit
- Domain aggregates `CatalogImportJob` + `CatalogImportItemResult` with Queued → Processing → Completed / CompletedWithWarnings / Failed
- Application: `ImportTemplateBatch`, `ImportSelectedProducts`, `CatalogImportQueryService`, `ProcessPosCatalogImportChunk`
- `IPlatformMerchantCatalogClient` + Api HTTP client forwarding bearer to Platform discovery
- Platform merchant discovery extended: `GET /api/v1/catalog/products/search`, `/products/{id}`, `/categories` (Active only)
- POS APIs under `/api/v1/pos/catalog-imports/*` with `PosIdempotencyService` support and `ManageCatalog` / `ViewCatalog` gates
- `PosCatalogImportBackgroundService` (PostgreSQL-backed, no Redis)
- Migration `AddPosCatalogImportMetadata` (`20260804224906`) — products provenance columns default `Manual`; import job/item tables
- Minimal MAUI stub `/catalog/import` (ManageCatalog-gated) + Catalog list link
- Unit tests: duplicate skip, org isolation, snapshot fields, Cashier denial

## 3. Explicit exclusions

- Full MAUI onboarding wizard polish (P20-WP07)
- Opening stock on import (stock remains 0; use existing inventory OpeningStock)
- Platform overwrite of local price/stock/tax/name/category/active (never)
- Cross-database FKs
- Dedicated `ImportCatalogProducts` capability (reuses `ManageCatalog`)
- Multi-instance SKIP LOCKED claim locking
- Redis / Hangfire / Quartz

## 4. Persistence / migrations

- Migration: `20260804224906_AddPosCatalogImportMetadata`
- Schema: `pos`
- Unique filtered index on `(organization_id, platform_global_product_id)`
- Unique optional idempotency key per organization on import jobs
- Existing products backfilled `catalog_source = 'Manual'` via column default

## 5. Build / test evidence

| Check | Result |
|---|---|
| POS Api Release build | Succeeded |
| Platform Api Release build | Succeeded |
| CatalogImport unit tests | **8 passed**, 0 failed |

## 6. Security limitations

- Development-stage POS/Platform auth remains as previously established; not production-ready.
- Import mutations require `ManageCatalog` (Cashier matrix excludes it).
- Platform discovery requires authentication; entitlement-aware filtering still baseline.

## 7. Risks / open decisions

- Concurrent multi-instance workers may race on claim without `FOR UPDATE SKIP LOCKED`.
- Selected-product resolution fetches Platform products one-by-one (acceptable for MVP batch sizes).
- Category name for imported globals may be generic when Platform category name is not snapshotted on template links.

## 8. Files / docs changed

- Domain/Application/Infrastructure/Api catalog-import stack + migration
- Platform `MerchantCatalogDiscoveryEndpoints` products/categories
- MAUI CatalogImport stub + CatalogProductsList link
- Unit tests `CatalogImportJobTests`, `CatalogImportProcessTests`
- This report + phase progress

## 9. Exact next work package

**P20-WP07** — MAUI catalog and cashier integration (template picker, progress UI, post-import review).
