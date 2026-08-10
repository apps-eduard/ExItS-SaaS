# P20 Supplemental — Template-Aware Bulk Import & Organization Catalog Visibility

| Field | Value |
|---|---|
| Status | **Code Complete** |
| Phase | [Phase 20](../phases/phase-20-global-product-catalog-and-business-template-onboarding.md) — **Open** / **Validation Pending** |
| Date | 2026-08-10 |
| Device Verified | **No** (unchanged) |
| Production Ready | **No** |

## 1. Objective

Extend Phase 20 without redesigning catalog ownership:

1. Platform Admin global CSV/XLSX import may optionally link successfully imported/resolved global products into an existing catalog template.
2. Platform Admin may view an Organization’s POS catalog read-only, including provenance.

## 2. Reused vs added architecture

| Concern | Reused | Added |
|---|---|---|
| Global import pipeline | `CatalogImportJob` / `CatalogImportItem`, `CreateCatalogImport`, `ProcessCatalogImportChunk`, Admin imports UI | `TargetTemplateId` on job; confirm destination; idempotent `TryAssignProduct` linking |
| Template composition | `CatalogTemplate` + `catalog_template_products` (references only; no product copy) | Import-time membership link after successful/resolved rows |
| Org catalog data | POS `CatalogProduct` provenance (`PlatformGlobalProductId`, `PlatformTemplateId`, `CatalogSource`, `CatalogImportedAt`) | POS platform-support read API; Platform→POS HttpClient; Admin org catalog page |
| Authz | `ImportGlobalProducts`, `ManageCatalogTemplates`, `ManageOrganizations` | Support API key for Platform→POS reads |

## 3. Import flow — before / after

**Before:** Upload → validate/preview → confirm → background create `GlobalProduct` rows only.

**After:**

```text
Upload → validate/preview
  → Confirm destination:
       (•) Global catalog only
       ( ) Global catalog + template  [requires existing template + ManageCatalogTemplates]
  → Background process:
       create/resolve global products
       if TargetTemplateId set → TryAssignProduct for each CreatedGlobalProductId
       failed rows without product id are not linked
       duplicate membership = no-op (idempotent)
```

Migration: `20260810043542_AddCatalogImportJobTargetTemplateId` (`catalog.catalog_import_jobs.target_template_id`, nullable FK, `ON DELETE SET NULL`).

## 4. Organization catalog visibility

| Layer | Surface |
|---|---|
| POS | `GET /api/v1/pos/platform-support/organizations/{organizationId}/catalog` — support key header; org id from path only; GET-only |
| Platform API | `GET /api/v1/platform/organizations/{organizationId}/catalog` — `ManageOrganizations` |
| Admin UI | `/admin/organizations/{id}/catalog` — read-only table + source breakdown |

**Provenance mapping (display):**

- `PlatformTemplateId` present → `GlobalTemplate`
- else `PlatformGlobalProductId` present → `GlobalCatalog`
- else → `MerchantCreated`

No Platform edits of merchant price/stock/catalog. No cross-database EF relationships.

## 5. Template version behavior

Later Platform template composition changes are **not** auto-pushed into organizations that previously imported a template. Merchant local catalogs remain snapshots.

**Deferred follow-up:** “N new products available” badge / delta discovery for orgs that imported an older template revision (only if contracts stay small/safe).

## 6. Privacy / isolation

- Catalog payload is business/product data; no customer/PHI surfaces on this read path.
- Organization isolation via trusted path `OrganizationId` on POS support API and Platform org lookup before proxy.
- Platform Admin APIs remain Platform-permission gated; Personal/Organization/POS sessions are not Platform Admin actors.

## 7. Tests

- Template-aware import: global-only unchanged; link on success; failed not linked; skipped existing resolved+linked; confirm/process idempotent; duplicate membership no-op; missing template rejected.
- Org catalog: provenance mapping; org-id scoping; read-only client contract; support-key denial; Admin route/permission gates.
- Existing Phase 20 import/template suites remain in scope for regression.

## 8. Explicit exclusions

- Auto-push of template deltas to merchants
- Platform mutation of merchant catalog/price/stock
- Closing Phase 20 / Device Verified
- Redesign of catalog ownership boundaries
