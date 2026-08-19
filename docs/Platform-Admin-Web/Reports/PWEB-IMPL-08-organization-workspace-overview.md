# PWEB-IMPL-08 — Organization workspace + Overview

**Status:** COMPLETE after validation  
**Branch:** `feat/platform-admin-web-v2`  
**Starting HEAD:** `099449d1b57b1ea7bd0fe30672f68c00ee27fe75`

## Delivered

Read-only Organization Workspace at `/admin/organizations/:organizationId`.

- Nested `OrganizationWorkspaceLayout` → `OrganizationOverviewPage` (Overview only; no dead tabs)
- Breadcrumb: Organizations > {Organization Name}
- Organizations list navigates via accessible name links (desktop) and Open (mobile)
- List search/filter query is preserved when returning through the breadcrumb when that state was passed from the list
- Primary identity: `GET /api/v1/platform/organizations/{organizationId}`
- Supplemental Overview: `GET /api/v1/platform/admin/organizations/{organizationId}/commercial-summary`
- Commercial records are shown as returned rows (product code + status). Array length is not treated as a portfolio total. Amounts/plan names/MRR/branch counts are not invented.
- Invalid organizationId fails locally without a malformed API request
- 404: Organization not found + Back to Organizations
- 403: existing fail-closed page-not-found (no payload leakage)
- Supplemental failure keeps the organization page usable (inline error + Retry + Copy diagnostics)
- No Create / Edit / Save / Activate / Suspend / Close / Delete / mutation API calls

## Workspace status

| Area | Record |
|---|---|
| Overview | IMPLEMENTED |
| Branches | NOT STARTED |
| People/Memberships | NOT STARTED |
| Products/Access | NOT STARTED |
| Subscription | NOT STARTED |
| Entitlements | NOT STARTED |
| Billing | NOT STARTED |
| Activity/Audit | NOT STARTED |
| CSRF | BLOCKS_FUTURE_MUTATION |
| Social-auth token-in-URL | BLOCKS_CUTOVER |
| PWA | NOT IN THIS PACKAGE |

## Explicitly not changed

Backend APIs, DB/migrations, Blazor Admin, POS, PLM, CORS, cookies, CSRF, PWA.

## Screenshots

`docs/Platform-Admin-Web/Reports/impl-08-organization-workspace/`

- `01-overview-1440x900-light.png`
- `02-overview-1440x900-dark.png`
- `03-overview-375x812.png`
- `04-overview-from-organizations.png`

Captured from Local Validation on 8095 using real organization data.

Visual approval: **AWAITING PRODUCT OWNER + CHATGPT**

## Validation

- `npm run typecheck` / `lint` / `format:check` / `test` (168) / `build` / `test:e2e` (50) / `test:e2e:container` (3)
- CSRF remains `BLOCKS_FUTURE_MUTATION`
- Social-auth token-in-URL remains `BLOCKS_CUTOVER`
- Backend, DB, Blazor Admin, POS, PLM, and PWA were not changed
