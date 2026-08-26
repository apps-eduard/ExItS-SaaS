# PWEB-IMPL-07 — Organizations list

**Status:** COMPLETE  
**Branch:** `feat/platform-admin-web-v2`  
**Starting HEAD:** `f5cd10b8a2a588e5da45b15c3283d67604cf9e1d`

## Delivered

Read-only `/admin/organizations` using `GET /api/v1/platform/organizations`.

- Organizations navigation is **IMPLEMENTED** (still permission-gated: `ViewPortfolio` or `ManageOrganizations`)
- Server-side search, status, sort (`DisplayName`, `Slug`, `Status`, `CreatedAtUtc`, `UpdatedAtUtc`), and pagination
- URL query state: `search`, `status`, `page`, `sortBy`, `sortDesc`
- Desktop/tablet `AdminTable`; phone cards (name, identifier, status)
- Loading, empty, zero-result + Reset filters, inline error + Retry + Copy diagnostics
- Unauthorized users see the existing fail-closed page-not-found pattern
- No Create / Edit / Delete / row navigation (Organization Workspace is a later package)

## Explicitly not changed

Backend APIs, DB/migrations, Blazor Admin, POS, PLM, CORS, cookies, CSRF.

CSRF remains `BLOCKS_FUTURE_MUTATION`. Social-auth token-in-URL remains `BLOCKS_CUTOVER`.

## Screenshots

`docs/Platform-Admin-Web/Reports/impl-07-organizations/`

- `01-organizations-1440x900-light.png`
- `02-organizations-1440x900-dark.png`
- `03-organizations-375x812.png`
- `04-organizations-filtered.png`

Captured from Local Validation on 8095 using real seeded organizations (no invented rows).

## Validation

- `npm run typecheck` / `lint` / `format:check` / `test` (157) / `build` / `test:e2e` (43) / `test:e2e:container` (3)
- CSRF remains `BLOCKS_FUTURE_MUTATION`
- Social-auth token-in-URL remains `BLOCKS_CUTOVER`
- Backend, DB, Blazor Admin, POS, and PLM were not changed
