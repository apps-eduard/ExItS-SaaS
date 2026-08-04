# P20-WP03 — Platform Admin Catalog Management

| Field | Value |
|---|---|
| Status | **Code Complete** |
| Phase | [Phase 20](../phases/phase-20-global-product-catalog-and-business-template-onboarding.md) — **Open** |
| Specs | [04-platform-admin-management.md](../specs/product-catalog/04-platform-admin-management.md) |
| Commit | _(recorded after push)_ |
| Date | 2026-08-05 |
| Device Verified | **No** |
| Production Ready | **No** |

## 1. Objective

Deliver Ant Design Blazor Platform Admin UI for global merchandise categories and products under `/admin/global-catalog/*`, consuming `/api/v1/platform/global-catalog/*`, without changing commercial SaaS Products/Plans at `/admin/products` and `/admin/plans`.

## 2. Delivered capability

- `PlatformPermissionCodes` mirrors Domain global-catalog permissions (`view_global_catalog`, `manage_global_categories`, `manage_global_products`, plus import/template codes for forward sync)
- Admin HTTP client DTOs + methods for categories/products CRUD and status patch
- Pages:
  - `/admin/global-catalog/categories` — list with search/status/business-type filters, create, detail edit, activate/deactivate/archive
  - `/admin/global-catalog/products` — paged list with search/status/category/barcode filters, create, detail edit, archive/reactivate
- `AdminNav` SubMenu **Product Catalog** gated by `view_global_catalog` (Categories + Products only)
- Localization keys in `AdminResources.resx` and `AdminResources.fil-PH.resx`
- Unit/file-existence guards + API client route tests

## 3. Explicit exclusions

- Imports / Templates Admin nav and pages deferred to **P20-WP04** / **P20-WP05** (no placeholder nav items)
- Product Requests inbox deferred until backend scope exists
- Bulk CSV/XLSX import UI (WP05)
- Business template builder (WP04)
- Merchant/POS import (WP06+)
- Commercial SaaS catalog (`/admin/products`, `/admin/plans`, `/api/v1/platform/catalog/*`) unchanged

## 4. API / UI capability

| Surface | Notes |
|---|---|
| Categories UI | Parent-aware list, concurrency via `ExpectedUpdatedAtUtc` |
| Products UI | Soft lifecycle Draft/Active/Archived; image as reference string |
| Permissions | View gate for pages; manage gates for mutations |
| Nav | Product Catalog separate from Commercial Products/Plans |

## 5. Build / test evidence

| Check | Result |
|---|---|
| Admin project build (Release) | **Succeeded** (0 errors; pre-existing Checkbox obsolete warnings) |
| WP03 + localization + global-catalog API client tests | **11 passed**, 0 failed |
| Pre-existing Admin suite failures | Unrelated (Dashboard `<Statistic`, Payments `FormatMoney`) — not introduced by WP03 |

## 6. Security limitations

- Development-stage Platform auth unchanged; UI permission checks are convenience only — API remains authoritative.
- Not production-ready.

## 7. Risks / open decisions

- Category tree is parent-aware ordered list (indent marker), not a full Ant Tree control.
- Image upload binary storage not in scope; reference string only.
- Templates/Imports nav intentionally omitted until those WPs deliver real pages.

## 8. Files / docs changed

- `src/Platform/ExItS.Platform.Admin/Models/PlatformPermissionCodes.cs`
- `src/Platform/ExItS.Platform.Admin/Models/PlatformDtos.cs`
- `src/Platform/ExItS.Platform.Admin/Services/IPlatformApiClient.cs`
- `src/Platform/ExItS.Platform.Admin/Services/PlatformApiClient.cs`
- `src/Platform/ExItS.Platform.Admin/Components/Pages/GlobalCatalogCategories.razor`
- `src/Platform/ExItS.Platform.Admin/Components/Pages/GlobalCatalogProducts.razor`
- `src/Platform/ExItS.Platform.Admin/Components/Layout/AdminNav.razor`
- `src/Platform/ExItS.Platform.Admin/Components/Shared/AdminStatusTag.razor`
- `src/Platform/ExItS.Platform.Admin/Localization/AdminResources.resx`
- `src/Platform/ExItS.Platform.Admin/Localization/AdminResources.fil-PH.resx`
- `tests/ExItS.Platform.Admin.UnitTests/P20Wp03GlobalCatalogAdminTests.cs`
- `tests/ExItS.Platform.Admin.UnitTests/PlatformApiClientTests.cs`
- Phase progress + this report

## 9. Exact next work package

**P20-WP04** — Business templates (domain + Admin template management; then add Templates nav).
