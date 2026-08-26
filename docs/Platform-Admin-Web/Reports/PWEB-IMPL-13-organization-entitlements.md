# PWEB-IMPL-13 — Organization workspace / Entitlements

**Status:** COMPLETE after validation  
**Branch:** `feat/platform-admin-web-v2`  
**Starting HEAD:** `67e7164f2c49360077b4fbbdac872f3b5b33de20`

## Delivered

Read-only Entitlements at `/admin/organizations/:organizationId/entitlements`.

- Workspace navigation exposes only real routes: **Overview**, **Branches**, **People**, **Products**, **Subscription**, **Entitlements**
- Breadcrumb: Organizations > {Organization Name} > Entitlements
- Product codes from `GET /api/v1/platform/admin/organizations/{id}/commercial-summary` `latestEntitlements`
- Snapshot history: `GET /api/v1/platform/organizations/{id}/products/{productCode}/entitlements/snapshots` with `page` and `pageSize`
- URL `?product=` is sanitized against returned product codes before the history call
- Empty product access is truthful and does not invent an org-wide entitlement list
- No snapshot detail route
- No generate, reconcile, or override controls

## Explicitly not changed

Backend APIs, DB/migrations, Blazor Admin, POS, PLM, CORS, cookies, CSRF, PWA.

## Workspace status

| Area | Record |
|---|---|
| Overview | IMPLEMENTED |
| Branches | IMPLEMENTED — READ ONLY |
| People/Memberships | IMPLEMENTED — READ ONLY |
| Products/Access | IMPLEMENTED — READ ONLY |
| Subscription | IMPLEMENTED — READ ONLY |
| Entitlements | IMPLEMENTED — READ ONLY |
| Billing | NOT STARTED |
| Activity/Audit | NOT STARTED |
| CSRF | BLOCKS_FUTURE_MUTATION |
| Social-auth token-in-URL | BLOCKS_CUTOVER |
| Platform Admin | WEB ONLY |
| PWA | NOT PLANNED |

## Screenshots

`docs/Platform-Admin-Web/Reports/impl-13-organization-entitlements/`

- `01-entitlements-1440x900-light.png`
- `02-entitlements-1440x900-dark.png`
- `03-entitlements-375x812.png`
- `04-entitlements-product-selector.png`

Visual approval: **AWAITING PRODUCT OWNER + CHATGPT**

## Validation

- `npm run typecheck` / `lint` / `format:check` / `test` (195) / `build` / `test:e2e` (78) / `test:e2e:container` (3)
- CSRF remains `BLOCKS_FUTURE_MUTATION`
- Social-auth token-in-URL remains `BLOCKS_CUTOVER`
- Prior package screenshots were not modified
