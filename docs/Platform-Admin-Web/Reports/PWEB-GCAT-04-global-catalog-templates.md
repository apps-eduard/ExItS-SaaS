# PWEB-GCAT-04 — React Global Catalog Templates Administration

**Status:** COMPLETE

**Branch:** `feat/platform-admin-global-catalog-templates` (from `5f458de7`)

## Delivered capability

React Platform Admin administration for global **Catalog Templates** at `/admin/global-catalog/templates/*`, backed by existing Platform API endpoints under `/api/v1/platform/global-catalog/templates`.

- Server-side list with search, status filter, primary business type filter, sorting, pagination
- Detail, create, and edit routes with React Hook Form + Zod
- Lifecycle actions: publish (Draft + ≥1 product), unpublish (Published → Draft), archive (Draft/Published → Archived; no reactivate)
- Product composition: assigned table, available-product browse/assign/remove, featured/first-batch flags, reorder via up/down
- Optimistic concurrency via `ExpectedUpdatedAtUtc` on PUT and lifecycle/composition mutations
- CSRF-protected mutations via `globalCatalogMutationRequest`
- EN + fil-PH i18n under `globalCatalog.templates.*`
- Navigation: Global Catalog → Templates (order 5); legacy `/admin/catalog/templates` redirect

## Routes

| Route | Screen |
| --- | --- |
| `/admin/global-catalog/templates` | Template list |
| `/admin/global-catalog/templates/new` | Create template |
| `/admin/global-catalog/templates/:templateId` | Detail + composition |
| `/admin/global-catalog/templates/:templateId/edit` | Edit metadata |

Legacy `/admin/catalog/templates` redirects to `/admin/global-catalog/templates`.

## API contracts (authoritative backend — unchanged)

| Method | Path | Permission |
| --- | --- | --- |
| GET | `/api/v1/platform/global-catalog/templates` | `view_global_catalog` |
| GET | `/api/v1/platform/global-catalog/templates/{id}` | `view_global_catalog` |
| POST | `/api/v1/platform/global-catalog/templates` | `manage_catalog_templates` |
| PUT | `/api/v1/platform/global-catalog/templates/{id}` | `manage_catalog_templates` |
| POST | `.../publish\|unpublish\|archive` | `publish_catalog_templates` |
| POST/PATCH/DELETE/PUT | `.../products*` | `manage_catalog_templates` |
| GET | `.../available-products` | `view_global_catalog` |

## Permissions (React)

| Capability | Permission |
| --- | --- |
| Read list/detail/available products | `platform.permission.view_global_catalog` |
| Create/edit/composition | `platform.permission.manage_catalog_templates` |
| Publish/unpublish/archive | `platform.permission.publish_catalog_templates` |

View-only users (`viewGlobalCatalog` without manage/publish) see metadata only; archived templates are read-only with no fake reactivate.

## Lifecycle

Statuses: Draft, Published, Archived.

- **Publish:** Draft only; requires ≥1 assigned product
- **Unpublish:** Published → Draft (idempotent if already Draft)
- **Archive:** Draft or Published → Archived (terminal; no reactivate UI)

## Concurrency

PUT and mutation endpoints send `ExpectedUpdatedAtUtc` where supported. On 409, UI shows API conflict detail and refetches detail (Business Types pattern).

## Build and test evidence

Run from `src/Platform/ExItS.Platform.Admin.Web`:

| Gate | Result |
| --- | --- |
| `npm test` (Vitest) | PASS — 63 files, 415 tests (incl. 12 template tests) |
| `npm run typecheck` | PASS |
| `npm run lint` | PASS |
| `npm run build` | PASS |
| `npm run test:e2e -- e2e/global-catalog-templates.spec.ts e2e/global-catalog-imports.spec.ts e2e/global-catalog-business-types.spec.ts e2e/global-catalog.spec.ts e2e/shell.spec.ts` | PASS — 43/43 |

Vitest uses `features/global-catalog/global-catalog-template-test-fixtures.ts` (does **not** modify `auth-fixtures.ts`).

## Explicit exclusions

- Backend API changes
- `TargetTemplateId` on imports
- SaaS Products / Plans / commercial / POS / MAUI
- Fake template operations not backed by API

## Next work package

Per portfolio plan after GCAT-04 sign-off.
