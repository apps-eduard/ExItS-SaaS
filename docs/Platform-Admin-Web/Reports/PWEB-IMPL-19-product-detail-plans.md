# PWEB-IMPL-19 — Product Detail + Plans / Pricing

**Status:** COMPLETE

**Branch:** `feat/platform-admin-web-v2`

**Starting SHA:** `4897a8dc65af3a9eba09abe90bf508af4221f340`

**Message:** `feat(platform-web): add product detail plans`

## Screens

Read-only routes:

- `/admin/products/:productId` — product detail + product-specific plans
- `/admin/plans` — plans list with server-side filtering/paging
- `/admin/plans/:planId` — plan detail

## API contracts (unchanged backend)

| Route | Endpoint |
| --- | --- |
| Product detail | `GET /api/v1/platform/catalog/products/{id}` |
| Plans list | `GET /api/v1/platform/catalog/plans` |
| Plan detail | `GET /api/v1/platform/catalog/plans/{planId}` |
| Product plans | `GET /api/v1/platform/catalog/products/{productCode}/plans` |

### Plans list query params (server-authoritative)

`productCode`, `status`, `search`, `sortBy`, `sortDesc`, `page`, `pageSize`

### Authorization

`viewPortfolio` (`platform.permission.view_portfolio`). 401/403 fail-closed via `ShellNotFoundPage`.

### Displayed DTO fields

**Product:** `id`, `code`, `displayName`, `status`, `createdAtUtc`, `updatedAtUtc`

**Plan list/detail:** `id`, `productCode`, `code`, `displayName`, `status`, timestamps, `productId`, `productDisplayName`, `planKey`, `description`, limits (`maxBranches`, etc.), feature booleans, `monthlyPrice`, `annualPrice`, `currencyCode`, trial fields — only when returned by API.

No fabricated pricing or status enums. Unknown values use safe raw fallback.

## Mutations

**None.** No create/edit/activate/publish controls.

## Evidence

`docs/Platform-Admin-Web/Reports/impl-19-product-detail-plans/`

## Scope

| Area | State |
| --- | --- |
| Platform Admin React | CHANGED |
| Platform backend | UNCHANGED |
| DB/migrations | NONE |
| Blazor | UNCHANGED |
| POS | UNCHANGED |
| PLM | UNCHANGED |
| PWA | ABSENT |

## T0 baseline (unchanged)

`ApiAuthorizationAuditTests` — 5/12 failures identical on branch and `origin/main` (`PREEXISTING_BASELINE_FAILURE`). Does not waive PWEB-19 package tests.

## Visual approval

**AWAITING PRODUCT OWNER + CHATGPT**
