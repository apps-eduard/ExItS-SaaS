# P20-WP02 — Global Categories and Products Domain

| Field | Value |
|---|---|
| Status | **Code Complete** |
| Phase | [Phase 20](../phases/phase-20-global-product-catalog-and-business-template-onboarding.md) — **Open** |
| Specs | [product-catalog/](../specs/product-catalog/) |
| Commit | `ad93c19` |
| Date | 2026-08-05 |
| Device Verified | **No** |
| Production Ready | **No** |

## 1. Objective

Deliver Platform-owned global merchandise categories and products (domain, application, infrastructure, API) under `/api/v1/platform/global-catalog/*` without changing commercial SaaS catalog behavior at `/api/v1/platform/catalog/*`.

## 2. Delivered capability

- Domain aggregates `GlobalCategory` / `GlobalProduct` with soft lifecycle only (archive; no hard delete)
- Normalization: barcode/SKU uppercase+trim (blank → null); name whitespace collapse; money `decimal(18,2)`
- Permissions: `view_global_catalog`, `manage_global_categories`, `manage_global_products`, `import_global_products`, `manage_catalog_templates`, `publish_catalog_templates` (Admin via `PlatformPermission.All`)
- Audit actions for category/product created/updated/status_changed
- Application use cases: Create/Update/Get/List/SetStatus for categories and products
- Persistence in PostgreSQL schema `catalog` via migration `AddGlobalProductCatalog`
- Unique indexes: filtered barcode, filtered SKU, category name within parent (root + child filters)
- API routes under `/api/v1/platform/global-catalog/categories|products` with pagination and `PlatformAuthz`

## 3. Explicit exclusions

- Admin UI (P20-WP03)
- Templates / publish (P20-WP04)
- Bulk CSV/XLSX import (P20-WP05)
- Merchant discovery and POS import (P20-WP06+)
- Commercial SaaS catalog routes and permissions (`manage_catalog`) unchanged

## 4. Persistence / migrations

- Migration: `20260804211744_AddGlobalProductCatalog`
- Schema: `catalog`
- Tables: `global_categories`, `global_category_business_types`, `global_products`, `global_product_business_types`
- Soft lifecycle only; repositories expose Add/Update (no Remove)

## 5. Build / test evidence

| Check | Result |
|---|---|
| Domain/Application build | Succeeded |
| GlobalCatalog unit tests | **17 passed**, 0 failed |
| PlatformRolePermissionCatalog tests | Passed (Admin holds all new permissions) |

## 6. Security limitations

- Development-stage Platform auth remains as previously established; not production-ready.
- New permissions are Platform Admin operational only; they do not grant POS product-local rights.

## 7. Risks / open decisions

- Category cycle detection for deep parent chains is not enforced beyond self-parent rejection (follow-on if needed).
- Merchant discovery endpoints deferred to later WPs.

## 8. Files / docs changed

- `src/Platform/ExItS.Platform.Domain/GlobalCatalog/**`
- `src/Platform/ExItS.Platform.Application/GlobalCatalog/**`
- `src/Platform/ExItS.Platform.Infrastructure/Persistence/GlobalCatalog/**` + repos + DbContext + migration
- `src/Platform/ExItS.Platform.Api/GlobalCatalog/GlobalCatalogEndpoints.cs` + Program DI/map
- `tests/ExItS.Platform.UnitTests/GlobalCatalog/**`
- Permissions / audit / domain error codes
- This report

## 9. Exact next work package

**P20-WP03** — Platform Admin catalog management (Ant Design Blazor UI for categories/products).
