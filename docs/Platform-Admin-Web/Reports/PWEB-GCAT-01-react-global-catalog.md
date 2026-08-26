# PWEB-GCAT-01 — React Global Catalog (Categories + Products)

**Status:** COMPLETE

**PWEB-GCAT-01-FIX01:** Browser validation, accessibility, viewport proof, and Agent 2 merge-risk analysis recorded below.

## Delivered capability

React Platform Admin routes under `/admin/global-catalog/*` for global categories and global products backed by `/api/v1/platform/global-catalog`.

- Category list/detail/create/edit with server-side paging, filtering, sorting, parent hierarchy, and business-type scope
- Product list/detail/create/edit with server-side paging, filtering, sorting, business-type scope, lifecycle actions, and image upload/remove (thumb/medium preview)
- Permission gates: `viewGlobalCatalog` to read; `manageGlobalCategories` / `manageGlobalProducts` for mutations
- Unauthorized users see `ShellNotFoundPage`; mutation controls hidden without manage permissions
- Optimistic concurrency via `ExpectedUpdatedAtUtc` with 409 conflict messaging and detail refetch
- CSRF-protected mutations through existing Platform antiforgery bootstrap
- EN + fil-PH i18n under `globalCatalog.*`

## Routes

| Route | Screen |
| --- | --- |
| `/admin/global-catalog/categories` | Category list |
| `/admin/global-catalog/categories/new` | Create category |
| `/admin/global-catalog/categories/:categoryId` | Category detail |
| `/admin/global-catalog/categories/:categoryId/edit` | Edit category |
| `/admin/global-catalog/products` | Product list |
| `/admin/global-catalog/products/new` | Create product |
| `/admin/global-catalog/products/:productId` | Product detail |
| `/admin/global-catalog/products/:productId/edit` | Edit product |

Navigation registry hrefs for `PWEB-NAV-CATEGORIES` and `PWEB-NAV-GLOBAL-PRODUCTS` point to these routes.

## Explicit exclusions

- Business types admin CRUD (read-only lookup only)
- Templates, imports, and other global-catalog endpoints outside categories/products
- Blazor Admin parity for non-GCAT surfaces
- GCAT-02 (imports/templates/business-type CRUD) — not authorized in FIX01

## Build and test evidence

Run from `src/Platform/ExItS.Platform.Admin.Web`:

| Gate | Result |
| --- | --- |
| `npm test` (Vitest) | PASS — 59 files, 356 tests |
| `npm run typecheck` | PASS |
| `npm run lint` | PASS |
| `npm run build` | PASS |
| `npm run test:e2e -- e2e/global-catalog.spec.ts` | PASS — 12/12 |
| `npm run test:e2e -- e2e/shell.spec.ts e2e/under-development.spec.ts` | PASS — 12/12 (nav regression) |

## Playwright browser validation (FIX01)

Spec: `e2e/global-catalog.spec.ts`

| Scenario group | Coverage |
| --- | --- |
| Navigation | Authorized user → Global Catalog nav (`Categories`, `Global Products`) → categories and products routes |
| Permission gate | `viewGlobalCatalog` only (lists, no mutations); `manageGlobalCategories` only; `manageGlobalProducts` only |
| Categories | List data, search, status filter, detail route |
| Products | List data, search, SKU filter, barcode URL filter, detail route |
| CSRF mutations | Category POST + product POST with antiforgery token header |
| 409 conflict | Mocked category PUT 409 → conflict message + detail refetch (stale save not accepted) |
| Image UI | Preview load, PUT multipart upload, DELETE remove |

Mocks only; no destructive changes to Local Validation persistent data.

## Accessibility (Axe)

`@axe-core/playwright` smoke on:

- `/admin/global-catalog/categories` — 0 serious/critical at 1440×900 and 768×1024
- `/admin/global-catalog/products/:productId` — 0 serious/critical at 1440×900 and 768×1024

FIX01 markup fix: `ProductImagePanel` hidden file input now has `aria-label` (axe `label` rule on sr-only file input).

## Responsive viewport proof

Within `global-catalog.spec.ts` accessibility tests:

- Desktop 1440×900 — no horizontal page overflow; filters/table/detail usable
- Tablet 768×1024 — no horizontal page overflow; filters/table/detail usable

Platform Admin remains desktop/tablet-first; phone redesign not required.

## Agent 2 parallel-branch integration risk (non-destructive)

Common ancestor: `92fd5a00ae867cd60a615f98c8f35abae7d55359`

| Branch | Ref inspected | SHA |
| --- | --- | --- |
| GCAT (`feat/platform-admin-global-catalog-react`) | HEAD | `9a1e10baf57f869792940b73b6a81f5c9d680ce1` (+ FIX01 commit) |
| PA-COM-03 (`origin/feat/platform-admin-pa-com-03`) | remote | `f50195c00cb6d542535c9df6e0df9b6fc5e2d909` |
| PA-COM-02 (`origin/feat/platform-admin-pa-com-02`) | remote | `18db549af98b86a3971897c9d2b5a60cc3d6f065` |

**No merge/rebase/cherry-pick performed.** Analysis via `git merge-base`, `git diff --name-only`, and `git merge-tree`.

| Flag | Value |
| --- | --- |
| `AGENT_2_DOMAIN_CONFLICT` | NO — global catalog vs SaaS commercial/plan editing |
| `AGENT_2_SHARED_FILE_OVERLAP` | `messages.ts`, `auth-fixtures.ts` (both PA-COM-02 and PA-COM-03 vs GCAT) |
| `AGENT_2_TEXTUAL_MERGE_CONFLICT` | YES — `git merge-tree` reports `changed in both` with `<<<<<<< .our` markers in both shared files |
| `AGENT_2_SHARED_FILE_RECONCILIATION` | YES — additions are independent (`globalCatalog.*` vs `plans.*` / plan mock hooks); manual merge expected at deliberate integration |

GCAT-only shared edits (no PA-COM overlap): `App.tsx`, `authorization-types.ts`, `platform-permissions.test.ts`, `navigation-registry.ts`, `react-implementation.ts`.

Commercial routes (`/admin/products`, `/admin/plans`), SaaS product/plan features, and Agent 2 permissions unchanged by GCAT.

## Security limitations

Development-stage UI; authorization is enforced client-side for UX gating and server-side on API mutations. Image upload accepts JPEG/PNG/WebP only; preview uses authenticated GET image variants (`thumb`, `medium`).

## Next work package

PWEB-GCAT-02 or adjacent catalog surfaces (business types, templates, imports) per portfolio plan — **not authorized in FIX01** (`GCAT_02_AUTHORIZED=NO`).
