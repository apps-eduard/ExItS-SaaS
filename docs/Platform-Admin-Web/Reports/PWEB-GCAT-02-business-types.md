# PWEB-GCAT-02 — React Global Catalog Business Types Administration

**Status:** COMPLETE

**Starting HEAD:** `8f2b6f0096d93cac0d8ab9fab697c4770dfdd848`  
**IMPLEMENTATION_COMMIT=** *(recorded after commit)*

## Delivered capability

React Platform Admin administration for global merchandise **Business Types** at `/admin/global-catalog/business-types/*`, backed by existing Platform API endpoints under `/api/v1/platform/global-catalog/business-types`.

- Server-side list with search, status filter, sorting, pagination
- Detail, create, and edit routes with React Hook Form + Zod
- Lifecycle status changes (Active / Inactive / Archived) including reactivation from Archived
- Optimistic concurrency via `ExpectedUpdatedAtUtc` on PUT and status POST
- CSRF-protected mutations
- EN + fil-PH i18n under `globalCatalog.businessTypes.*`
- Navigation: Global Catalog → Business Types (first), Categories, Global Products

## Routes

| Route | Screen |
| --- | --- |
| `/admin/global-catalog/business-types` | Business type list |
| `/admin/global-catalog/business-types/new` | Create business type |
| `/admin/global-catalog/business-types/:businessTypeId` | Detail |
| `/admin/global-catalog/business-types/:businessTypeId/edit` | Edit (code read-only) |

Legacy `/admin/catalog/business-types` redirects to the new route.

## API contracts (authoritative backend)

| Method | Path | Permission |
| --- | --- | --- |
| GET | `/api/v1/platform/global-catalog/business-types` | `view_global_catalog` |
| GET | `/api/v1/platform/global-catalog/business-types/{id}` | `view_global_catalog` |
| POST | `/api/v1/platform/global-catalog/business-types` | `manage_global_categories` |
| PUT | `/api/v1/platform/global-catalog/business-types/{id}` | `manage_global_categories` |
| POST | `/api/v1/platform/global-catalog/business-types/{id}/status` | `manage_global_categories` |

No DELETE endpoint (soft lifecycle only).

## Permissions

| Capability | Permission |
| --- | --- |
| Read list/detail | `platform.permission.view_global_catalog` |
| Create/edit/status | `platform.permission.manage_global_categories` |

There is no separate `ManageBusinessTypes` permission (backend uses `manage_global_categories`).

## Fields

| Field | Create | Edit after create |
| --- | --- | --- |
| Code | Yes | **No** (read-only UI; not sent on PUT) |
| Name | Yes | Yes |
| Description | Yes | Yes |
| SortOrder | Yes | Yes |
| IconReference | Yes | Yes (metadata string only; not an image uploader) |

## Lifecycle

Statuses: Active, Inactive, Archived. Archived is **not** terminal — reactivation to Active or Inactive is supported per domain rules.

No hard delete UI (`DELETE_BUTTON=NO`).

## Concurrency

PUT and status POST send `ExpectedUpdatedAtUtc`. On 409, UI shows API conflict detail and refetches detail (does not silently accept stale saves).

## Category / product regression

Active-only business type lookup (`status=Active`) for category/product assignment pickers is unchanged. Inactive/Archived types do not appear in pickers after refetch.

## Build and test evidence

Run from `src/Platform/ExItS.Platform.Admin.Web`:

| Gate | Result |
| --- | --- |
| `npm test` (Vitest) | PASS — 60 files, 383 tests (incl. 27 business-types tests) |
| `npm run typecheck` | PASS |
| `npm run lint` | PASS |
| `npm run build` | PASS |
| `npm run test:e2e -- e2e/global-catalog-business-types.spec.ts` | PASS — 9/9 |
| `npm run test:e2e -- e2e/global-catalog.spec.ts` | PASS — 12/12 (GCAT-01 regression) |
| `npm run test:e2e -- e2e/shell.spec.ts` | PASS — 6/6 (nav regression) |

Vitest uses `features/global-catalog/global-catalog-test-fixtures.ts` (does **not** modify `auth-fixtures.ts`).

Playwright business-types spec covers nav, filters, create CSRF, read-only code on edit, lifecycle, 409 conflict, view-only gate, axe + desktop/tablet overflow.

## Agent 2 conflict guard

- `AGENT_2_DOMAIN_CONFLICT=NO`
- `AGENT_2_KNOWN_SHARED_CONFLICT_PRESERVED=YES` — only additive `messages.ts` keys; `auth-fixtures.ts` untouched
- No PA-COM merge/rebase/cherry-pick

## Explicit exclusions

- Global Catalog Imports / Templates
- SaaS Products / Plans / commercial features
- POS / backend API changes
- New permissions or DELETE endpoints

## Known gaps

- IconReference is a metadata string only (no image upload contract on backend).
- Business type admin uses `manage_global_categories` permission name (backend authoritative; broader than Business Types label).

## Next work package

PWEB-GCAT-03 or adjacent surfaces (templates, imports) per portfolio plan — not authorized in GCAT-02.
