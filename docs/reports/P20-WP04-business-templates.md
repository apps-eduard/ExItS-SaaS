# P20-WP04 — Business Templates

| Field | Value |
|---|---|
| Status | **Code Complete** |
| Phase | [Phase 20](../phases/phase-20-global-product-catalog-and-business-template-onboarding.md) — **Open** |
| Specs | [product-catalog/](../specs/product-catalog/) |
| Commit | _(recorded after push)_ |
| Date | 2026-08-05 |
| Device Verified | **No** |
| Production Ready | **No** |

## 1. Objective

Deliver Platform-owned business catalog templates (domain, application, persistence, admin + merchant APIs, Admin list/builder UI) without changing commercial SaaS catalog behavior at `/api/v1/platform/catalog/*`.

## 2. Delivered capability

- Domain aggregate `CatalogTemplate` + composition `CatalogTemplateProduct` with soft lifecycle: Draft → Published → (Unpublish→Draft) / Archive
- `SelectionMode` (Curated/Auto/Hybrid), `DefaultBatchSize` (1–500, default 50), featured/first-batch flags, reorder
- Publish requires ≥1 product; archived templates are immutable; published templates remain editable (future imports only — no POS mutation)
- Unique `(CatalogTemplateId, GlobalProductId)` in domain + DB unique index; unique slug
- Application use cases: create/update/publish/unpublish/archive + assign/remove/reorder/flags
- Persistence schema `catalog` via migration `AddCatalogTemplates` (`catalog_templates`, `catalog_template_products`)
- Admin APIs under `/api/v1/platform/global-catalog/templates*`
- Merchant discovery under `/api/v1/catalog/templates*` (authenticated; published only)
- Admin UI `/admin/global-catalog/templates` list + basic builder (merged into WP03 mid-flight Product Catalog nav)
- Permissions: `manage_catalog_templates`, `publish_catalog_templates`, `view_global_catalog` (from WP02)

## 3. Explicit exclusions

- POS import / local snapshot creation (P20-WP06+)
- Entitlement-aware merchant filtering beyond authenticated baseline (deferred to WP06)
- LocalValidation seed of Sari-Sari / Mini Grocery starter template — **deferred** (no global-catalog seed pattern in LocalValidation yet; create via Admin)
- `CatalogTemplateCategory` table (spec mentioned; not required for composition MVP)
- Commercial SaaS catalog routes/permissions unchanged

## 4. Persistence / migrations

- Migration: `20260804222413_AddCatalogTemplates`
- Schema: `catalog`
- Soft lifecycle only; repositories Add/Update (no Remove of templates)
- Composition DELETE removes association only (not the global product)

## 5. Build / test evidence

| Check | Result |
|---|---|
| Platform.Api Release build | Succeeded (0 warnings/errors) |
| Platform.Admin Release build | Succeeded (pre-existing Checkbox obsolete warnings) |
| GlobalCatalog + CatalogTemplate unit tests | **27 passed**, 0 failed |

## 6. Security limitations

- Development-stage Platform auth remains as previously established; not production-ready.
- Merchant discovery requires authentication only; organization entitlement gating deferred.
- Template publish/manage permissions are Platform Admin operational only.

## 7. Risks / open decisions

- Sari-Sari / Mini Grocery starter content should be seeded once LocalValidation (or Admin bootstrap) has a global-catalog seed hook.
- WP03 category/product Admin pages may still be mid-flight locally; this WP only added Templates nav + page and shared client DTOs.

## 8. Files / docs changed

- `src/Platform/ExItS.Platform.Domain/GlobalCatalog/CatalogTemplate*.cs` + enums/rules/errors/audit
- `src/Platform/ExItS.Platform.Application/GlobalCatalog/CatalogTemplateUseCases.cs` + DTOs/repos/error codes
- `src/Platform/ExItS.Platform.Infrastructure` records/mapper/repos/DbContext/migration `AddCatalogTemplates`
- `src/Platform/ExItS.Platform.Api/GlobalCatalog/*` admin + merchant discovery endpoints
- `src/Platform/ExItS.Platform.Admin` templates page, nav, API client, localization
- `tests/ExItS.Platform.UnitTests/GlobalCatalog/CatalogTemplateDomainTests.cs`
- This report + phase progress

## 9. Exact next work package

**P20-WP05** — Platform CSV/XLSX bulk import (or finish P20-WP03 Admin categories/products closeout if still open).
