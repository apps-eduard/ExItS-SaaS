# PLATFORM-WEB-PA-COM-02 — SaaS Product Lifecycle

## Summary

PA-COM-02 delivers React Platform Admin lifecycle management for **existing** Platform SaaS catalog products (not Global Merchandise SKU catalog).

| Item | Value |
|---|---|
| Starting HEAD | `f50195c00cb6d542535c9df6e0df9b6fc5e2d909` |
| Final HEAD | `c560b291` |

## API contracts (verified from `CatalogEndpoints.cs`)

All product mutations use **product ID (GUID)**, not product code.

| Operation | Method | Path | Permission |
|---|---|---|---|
| Rename | `PATCH` | `/api/v1/platform/catalog/products/{id}/rename` | `platform.permission.manage_catalog` |
| Activate | `POST` | `/api/v1/platform/catalog/products/{id}/activate` | `platform.permission.manage_catalog` |
| Deactivate | `POST` | `/api/v1/platform/catalog/products/{id}/deactivate` | `platform.permission.manage_catalog` |
| Retire | `POST` | `/api/v1/platform/catalog/products/{id}/retire` | `platform.permission.manage_catalog` |
| Read detail | `GET` | `/api/v1/platform/catalog/products/{id}` | `platform.permission.view_portfolio` |
| Read list | `GET` | `/api/v1/platform/catalog/products` | `platform.permission.view_portfolio` |

Rename body: `{ displayName, expectedUpdatedAtUtc? }`

## React screens

| Route | Component | Scope |
|---|---|---|
| `/admin/products` | `ProductsPage` | SaaS product list (unchanged structure) |
| `/admin/products/:productId` | `ProductDetailPage` + `ProductLifecycleOperator` | Identity, rename, lifecycle |
| `/admin/plans/:planId` | `PlanDetailPage` + `PlanCommercialOperator` | **PA-COM-03 — not modified** |

## Lifecycle rules (server-authoritative)

From `Product.cs` domain transitions:

| Status | Allowed outbound |
|---|---|
| Active | Deactivate, Retire |
| Inactive | Activate, Retire |
| Retired | **None (terminal)** |

UI mirrors these rules; invalid transitions return domain/409 errors from API.

## Explicit exclusions

- **No Create Product UI** — runtime `POST /catalog/products` is Testing-only
- **No product code mutation**
- **No Unretire**
- **No Global Catalog changes** (`features/global-catalog/*`, `api/global-catalog/*` untouched)
- **No POS changes**

## Known backend gaps

None discovered for PA-COM-02 scope. Product lifecycle HTTP routes exist and match UI wiring.

## Tests

- Vitest: `ProductDetailPage.test.tsx`, existing suite regression
- Playwright: `e2e/product-lifecycle.spec.ts`, `e2e/products-plans.spec.ts`, `e2e/plan-commercial.spec.ts`

## Agent conflict check

| Agent | Modified | Notes |
|---|---|---|
| Agent 3 (Global Catalog) | **NO** | No `global-catalog` paths touched |
| Agent 1 (POS) | **NO** | No `PinoyBusinessPOS` paths touched |

Shared files changed: `use-commercial-mutations.ts`, `commercial-backend-gaps.ts`, `commercial-query-keys.ts` (already shared with PA-COM-03), `auth-fixtures.ts`, `messages.ts`.

## PA-COM-03 regression

Plan commercial editor (`Save commercial package`) verified in Vitest after product detail changes.
