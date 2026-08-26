# PWEB-IMPL-14 — Organization workspace / Billing

**Status:** COMPLETE after validation  
**Branch:** `feat/platform-admin-web-v2`  
**Starting HEAD:** `5e8de4e7c3c70e242d546a7ecced85c9e33099b6`

## Delivered

Read-only Billing at `/admin/organizations/:organizationId/billing`.

- Workspace navigation exposes only real routes: **Overview**, **Branches**, **People**, **Products**, **Subscription**, **Entitlements**, **Billing**
- No Activity/Audit tab
- Breadcrumb: Organizations > {Organization Name} > Billing
- `GET /api/v1/platform/organizations/{organizationId}/payments` with `status`, `page`, `pageSize`
- URL query state
- Platform SaaS payment fields only (product, amount, currency, method, status, paid date)
- 403 fail-closes without leaking amounts
- Empty, filtered empty, error/retry
- No Record, Confirm, Reject, or Void controls

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
| Billing | IMPLEMENTED — READ ONLY |
| Activity/Audit | NOT STARTED |
| CSRF | BLOCKS_FUTURE_MUTATION |
| Social-auth token-in-URL | BLOCKS_CUTOVER |
| Platform Admin | WEB ONLY |
| PWA | NOT PLANNED |

## Screenshots

`docs/Platform-Admin-Web/Reports/impl-14-organization-billing/`

- `01-billing-1440x900-light.png`
- `02-billing-1440x900-dark.png`
- `03-billing-375x812.png`
- `04-billing-filtered.png`

Visual approval: **AWAITING PRODUCT OWNER + CHATGPT**

## Validation

- `npm run typecheck` / `lint` / `format:check` / `test` (198) / `build` / `test:e2e` (83) / `test:e2e:container` (3)
- CSRF remains `BLOCKS_FUTURE_MUTATION`
- Social-auth token-in-URL remains `BLOCKS_CUTOVER`
- Prior package screenshots were not modified
