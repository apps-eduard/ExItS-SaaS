# PWEB-GCAT-03 — React Global Catalog Product Import

**Status:** COMPLETE

**Starting HEAD:** `ca135d527dad70ebee917e1ba48b49a8f99debf5`  
**IMPLEMENTATION_COMMIT=`16c7e016c621b12e259dc9c6273ca3b3e8680745`**

## Delivered capability

React Platform Admin administration for global merchandise **product import** at `/admin/global-catalog/imports/*`, backed by existing Platform API endpoints under `/api/v1/platform/global-catalog/products/imports`.

- Server-side import job list with status filter and pagination
- CSV template download from authoritative backend endpoint
- Multipart CSV/XLSX upload with optional idempotency key (upload uses backend `DisableAntiforgery()` — no CSRF on POST upload)
- Import detail with server preview/summary, confirm when `Validated`, and paged errors
- Read-only display of existing `TargetTemplateId` when present on a job (no template selector UI)
- Permission gate: `platform.permission.import_global_products`
- EN + fil-PH i18n under `globalCatalog.imports.*`
- Navigation: Global Catalog → Imports (order 4, after Global Products)

## Routes

| Route | Screen |
| --- | --- |
| `/admin/global-catalog/imports` | Import list + upload panel |
| `/admin/global-catalog/imports/:jobId` | Import detail / preview / confirm / errors |

Legacy `/admin/catalog/imports` redirects to the new route.

## API contracts (authoritative backend)

| Method | Path | Permission | CSRF |
| --- | --- | --- | --- |
| GET | `/api/v1/platform/global-catalog/products/imports` | `import_global_products` | No |
| GET | `/api/v1/platform/global-catalog/products/imports/template.csv` | `import_global_products` | No |
| POST | `/api/v1/platform/global-catalog/products/imports` | `import_global_products` | **No** (`DisableAntiforgery()`) |
| GET | `/api/v1/platform/global-catalog/products/imports/{jobId}` | `import_global_products` | No |
| POST | `/api/v1/platform/global-catalog/products/imports/{jobId}/confirm` | `import_global_products` | Yes |
| GET | `/api/v1/platform/global-catalog/products/imports/{jobId}/errors` | `import_global_products` | No |

Template filename: `exits-global-product-import-template.csv`

## Permissions

| Capability | Permission |
| --- | --- |
| Nav + list/download/upload/view/confirm | `platform.permission.import_global_products` |

Without permission: imports nav hidden; direct route shows not-found shell (fail-closed).

## Import statuses (server values)

`Validated`, `Queued`, `Processing`, `Completed`, `CompletedWithWarnings`, `Failed`

Confirm allowed only when status is `Validated`.

## Upload constraints (backend authoritative)

- Formats: `.csv`, `.xlsx`
- Max file size: 5 MB
- Max rows: 5,000
- Multipart field name: `file`
- Optional idempotency: form field `idempotencyKey` or `Idempotency-Key` header

## Explicit exclusions

- Catalog Template administration UI
- Target template selection on confirm (`TARGET_TEMPLATE_SELECTION=NO`)
- Cancel / retry / delete / rollback import actions (no backend endpoints)
- SaaS Products/Plans/commercial domain
- POS / backend API changes

## Build and test evidence

Run from `src/Platform/ExItS.Platform.Admin.Web`:

| Gate | Result |
| --- | --- |
| `npm test` (Vitest) | 402 passed (61 files; includes 18 import tests) |
| `npm run typecheck` | PASS |
| `npm run lint` | PASS |
| `npm run build` | PASS |
| `npm run test:e2e -- e2e/global-catalog-imports.spec.ts e2e/global-catalog.spec.ts e2e/global-catalog-business-types.spec.ts e2e/shell.spec.ts` | 35 passed |

Vitest uses `features/global-catalog/global-catalog-import-test-fixtures.ts` (does **not** modify `auth-fixtures.ts`).

## Agent conflict guard

- `SAAS_COMMERCIAL_FILES_MODIFIED=NO`
- `AGENT_2_DOMAIN_CONFLICT=NO` — additive `messages.ts` keys only

## Next work package

Catalog Templates admin or adjacent GCAT surfaces per portfolio plan — not authorized in GCAT-03.
