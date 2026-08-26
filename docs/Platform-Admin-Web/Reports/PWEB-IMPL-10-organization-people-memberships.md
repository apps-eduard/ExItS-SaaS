# PWEB-IMPL-10 — Organization workspace / People / Memberships

**Status:** COMPLETE after validation  
**Branch:** `feat/platform-admin-web-v2`  
**Starting HEAD:** `c465f35ec1b262a350ba2d73d8152dd70b4032d1`

## Delivered

Read-only People at `/admin/organizations/:organizationId/people`.

- Workspace navigation exposes only real routes: **Overview**, **Branches**, **People**
- Breadcrumb: Organizations > {Organization Name} > People
- Members: `GET /api/v1/platform/organizations/{organizationId}/members` with `status`, `page`, `pageSize`
- Invitations: `GET /api/v1/platform/organizations/{organizationId}/invitations` with `status`, `page`, `pageSize`
- Independent loading, empty, error, retry, copy diagnostics, and 403 fail-closed states
- Invitation mapper omits `acceptToken` even if a payload includes it
- Desktop/tablet `AdminTable`; phone cards
- No search (unsupported)
- No invite, revoke, resend, or role-change controls

## Limitations

The member and invitation list endpoints do not support `search` or `sort`. This package does not invent client-only search.

## Workspace status

| Area | Record |
|---|---|
| Overview | IMPLEMENTED |
| Branches | IMPLEMENTED — READ ONLY |
| People/Memberships | IMPLEMENTED — READ ONLY |
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

`docs/Platform-Admin-Web/Reports/impl-10-organization-people/`

- `01-people-1440x900-light.png`
- `02-people-1440x900-dark.png`
- `03-people-375x812.png`
- `04-invitations.png`

Captured from Local Validation on 8095 using real organization membership data.

Visual approval: **AWAITING PRODUCT OWNER + CHATGPT**

## Validation

- `npm run typecheck` / `lint` / `format:check` / `test` (185) / `build` / `test:e2e` (63) / `test:e2e:container` (3)
- CSRF remains `BLOCKS_FUTURE_MUTATION`
- Social-auth token-in-URL remains `BLOCKS_CUTOVER`
- Prior package screenshots were not modified
