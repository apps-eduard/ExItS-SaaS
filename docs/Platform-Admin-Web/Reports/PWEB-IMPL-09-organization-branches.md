# PWEB-IMPL-09 — Organization workspace / Branches

**Status:** COMPLETE after validation  
**Branch:** `feat/platform-admin-web-v2`  
**Starting HEAD:** `d718ec06bd76947fcf26ca220ab6e8844cf926ef`

## Delivered

Read-only Branches at `/admin/organizations/:organizationId/branches`.

- Workspace navigation exposes only real routes: **Overview** and **Branches**
- Breadcrumb: Organizations > {Organization Name} > Branches
- `GET /api/v1/platform/organizations/{organizationId}/branches` with no query parameters
- Desktop/tablet `AdminTable`; phone cards
- Primary vs Branch from `isPrimary` only
- Status uses text plus color (`Active` / `Inactive` / `Archived`)
- Empty, loading, inline error + Retry + Copy diagnostics
- 403 fail-closed page-not-found without payload leakage
- Organization 404 / invalid ID inherited from PWEB-IMPL-08
- No branch detail route, no row links, no mutation controls

## Branch API limitation

The list endpoint returns `IReadOnlyList<OrganizationBranchDto>` with **no** `page`, `pageSize`, `search`, `sort`, or status filter. This package does not invent client-only filtering or fake pagination.

## Explicitly not shown

POS/fulfillment operational fields (delivery fees, pickup/delivery/customer-ordering state, online-order pause, device operations).

## Workspace status

| Area | Record |
|---|---|
| Overview | IMPLEMENTED |
| Branches | IMPLEMENTED — READ ONLY |
| People/Memberships | NOT STARTED |
| Products/Access | NOT STARTED |
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

`docs/Platform-Admin-Web/Reports/impl-09-organization-branches/`

- `01-branches-1440x900-light.png`
- `02-branches-1440x900-dark.png`
- `03-branches-375x812.png`
- `04-workspace-navigation.png`

Captured from Local Validation on 8095 using real organization/branch data.

Visual approval: **AWAITING PRODUCT OWNER + CHATGPT**

## Validation

- `npm run typecheck` / `lint` / `format:check` / `test` (175) / `build` / `test:e2e` (56) / `test:e2e:container` (3)
- CSRF remains `BLOCKS_FUTURE_MUTATION`
- Social-auth token-in-URL remains `BLOCKS_CUTOVER`
- Prior impl-06/07/08 screenshots were not modified
