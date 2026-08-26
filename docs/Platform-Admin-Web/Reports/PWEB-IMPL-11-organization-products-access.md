# PWEB-IMPL-11 — Organization workspace / Products / Access

**Status:** COMPLETE after validation  
**Branch:** `feat/platform-admin-web-v2`  
**Starting HEAD:** `0c1ecda3c792426c4737a82358bb03b14a90db57`

## Delivered

Read-only Products/Access at `/admin/organizations/:organizationId/products`.

- Workspace navigation: Overview, Branches, People, Products
- `GET /api/v1/platform/admin/organizations/{organizationId}/commercial-summary`
- One row/card per returned `latestEntitlements` record
- Product code always shown; `productDisplayName` only when the summary returns it
- Subscription status, snapshot revision, and generated timestamp when returned
- Empty, loading, error/retry/copy diagnostics, 403 fail-closed
- No fake portfolio totals, no grant/activate/plan-change controls

## Boundary

Platform product-access visibility only. Not POS operations, PLM operations, product configuration, inventory, or device workflow.

## Workspace status

| Area | Record |
|---|---|
| Overview | IMPLEMENTED |
| Branches | IMPLEMENTED — READ ONLY |
| People/Memberships | IMPLEMENTED — READ ONLY |
| Products/Access | IMPLEMENTED — READ ONLY |
| Subscription | NOT STARTED |
| Entitlements | NOT STARTED |
| Billing | NOT STARTED |
| Activity/Audit | NOT STARTED |
| CSRF | BLOCKS_FUTURE_MUTATION |
| Social-auth token-in-URL | BLOCKS_CUTOVER |
| Platform Admin | WEB ONLY |
| PWA | NOT PLANNED |

## Explicitly not changed

Backend APIs, DB/migrations, Blazor Admin, POS, PLM, CORS, cookies, CSRF, PWA.

## Screenshots

`docs/Platform-Admin-Web/Reports/impl-11-organization-products/`

- `01-products-1440x900-light.png`
- `02-products-1440x900-dark.png`
- `03-products-375x812.png`

Visual approval: **AWAITING PRODUCT OWNER + CHATGPT**

## Validation

- `npm run typecheck` / `lint` / `format:check` / `test` (188) / `build` / `test:e2e` (69) / `test:e2e:container` (3)
- Prior package screenshots were not modified
