# PWEB-IMPL-06C — Final shell, account, and audit polish

**Status:** PRODUCT OWNER VISUAL APPROVED 
**Branch:** `feat/platform-admin-web-v2`  
**Starting HEAD:** `356cd9319c5ecb815c59ad1213c0a23e420fad92`

## Direction

PWEB-IMPL-06B visual direction is **accepted** as the structural/density standard. This package does not redesign typography, spacing, dashboard composition, or branding.

## Delivered

- Desktop sidebar collapse/expand lives in the top bar immediately before breadcrumbs
- Sidebar header is brand-only (`Ex` / ExItS / Platform Administration)
- Mobile Menu drawer is unchanged and is not combined with the desktop collapse control
- Account trigger uses generated initials (no photos)
- Account menu shows display name, email, and **Sign out**
- Sign out calls `POST /api/v1/platform/auth/logout` with cookies; session/query state cleared only after success (or an already-invalid session)
- Logout network failure reports a diagnostic and does not pretend logout succeeded
- Known audit codes/types have presentation labels; raw values remain in `title` and screen-reader text
- `platform-user:<GUID>` is shown as Platform user plus a compact GUID; the full identifier remains available
- Platform readiness uses the same bordered operational surface as other sections, still at low visual weight

## Explicitly not changed

Backend APIs, DB/migrations, Blazor Admin, POS, PLM, CORS, cookie architecture, CSRF work, dashboard metrics, navigation destinations.

Old stash `stash@{0}` (`wip: sign-out and topbar collapse before PWEB-IMPL-06B`) was **not** applied.

## Screenshots

`docs/Platform-Admin-Web/Reports/impl-06c-final-polish/`

- `01-dashboard-expanded-1440x900.png`
- `02-dashboard-collapsed-1440x900.png`
- `03-account-menu-open.png`
- `04-dashboard-dark.png`
- `05-dashboard-375x812.png`
- `06-audit-polish.png`

Account screenshots contain no password or secret.

## Visual approval

Not claimed. Awaiting Product Owner + ChatGPT.
