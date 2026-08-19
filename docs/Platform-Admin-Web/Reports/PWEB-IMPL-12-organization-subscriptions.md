# PWEB-IMPL-12 — Organization workspace / Subscriptions

**Status:** COMPLETE after validation  
**Branch:** `feat/platform-admin-web-v2`  
**Starting HEAD:** `335c6113c54bbc9fc37dfbb8f5f75fd9b4285703`

## Delivered

Read-only Subscription at `/admin/organizations/:organizationId/subscription`.

- Workspace navigation exposes only real routes: **Overview**, **Branches**, **People**, **Products**, **Subscription**
- Breadcrumb: Organizations > {Organization Name} > Subscription
- `GET /api/v1/platform/organizations/{organizationId}/subscriptions`
- Server `status`, `search`, `isTrial`, `productCode`, `sortBy`, `sortDesc`, `page`, `pageSize`
- URL query state
- Product/plan/status/trial/period from returned DTO fields
- Agreed price is mapped out and not displayed
- `planId` is not offered (no catalog picker)
- Empty, zero-result, reset, error/retry, 403 fail-closed
- No activate/suspend/cancel/plan-change controls

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
| Entitlements | NOT STARTED |
| Billing | NOT STARTED |
| Activity/Audit | NOT STARTED |
| CSRF | BLOCKS_FUTURE_MUTATION |
| Social-auth token-in-URL | BLOCKS_CUTOVER |
| Platform Admin | WEB ONLY |
| PWA | NOT PLANNED |

## Screenshots

`docs/Platform-Admin-Web/Reports/impl-12-organization-subscriptions/`

- `01-subscriptions-1440x900-light.png`
- `02-subscriptions-1440x900-dark.png`
- `03-subscriptions-375x812.png`
- `04-subscriptions-filtered.png`

Visual approval: **AWAITING PRODUCT OWNER + CHATGPT**

## Validation

- `npm run typecheck` / `lint` / `format:check` / `test` (191) / `build` / `test:e2e` (72) / `test:e2e:container` (3)
- CSRF remains `BLOCKS_FUTURE_MUTATION`
- Social-auth token-in-URL remains `BLOCKS_CUTOVER`
- Prior package screenshots were not modified
