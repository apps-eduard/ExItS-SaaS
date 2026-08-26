# PWEB-IMPL-06A — Local Validation Test User runtime gate

**Status:** COMPLETE (visual approval of PWEB-IMPL-06 still awaiting Product Owner + ChatGPT)  
**Branch:** `feat/platform-admin-web-v2`  
**Starting HEAD:** `53e836d0dbfafce91af8ea786e1a76897d3f36ad`

## Root cause

`areDevelopmentToolsAllowed()` only inspected Vite `import.meta.env.MODE`. The 8095 nginx image is a production-optimized build (`MODE=production`), so `DevelopmentTestUserTools` stayed hidden even when the container was deployed as Local Validation.

This package does **not** rebuild nginx in Vite development mode.

## Runtime flag

`window.__EXITS_PLATFORM_ADMIN_WEB__.localValidationToolsEnabled` is injected by `deploy/docker/platform-admin-web/40-exits-runtime-config.sh` from `LOCAL_VALIDATION_TOOLS_ENABLED`.

| Value | Result |
|---|---|
| missing | false |
| any non-boolean / `"true"` string in the browser object | false |
| boolean `true` | true |
| hostname / port / localhost | never inferred |

Local Validation compose service `admin-web-react` sets `LOCAL_VALIDATION_TOOLS_ENABLED=true`. Production compose does not.

`/config.js` contains only `platformApiBaseUrl` and `localValidationToolsEnabled`. No passwords or secrets.

## Double gate

Test User appears only when **both** are true:

1. Frontend permit: Vite development/test/testing **or** runtime `localValidationToolsEnabled === true`
2. `GET /api/v1/platform/local-validation/enabled` returns JSON `true`, then identities are loaded

Backend false, 404, error, or unreachable: selector hidden (fail closed).

Navigation / DEV destinations remain gated by `areDevelopmentToolsAllowed()` (MODE only) so 8095 does not gain Under Development nav.

## UX

Selector is the existing `DevelopmentTestUserTools` control, labeled **Local Validation** / **Test User — Local Validation** when the runtime flag is active.

Selecting a user fills email/username only. Password stays empty. No auto-submit. Login remains `POST /api/v1/platform/auth/login`.

## Explicitly not changed

Backend Local Validation API, DB/migrations, Blazor Admin, POS, PLM, logout, CSRF, CORS, cookies, Dashboard feature behavior.

## Screenshots

COMPLETE under `docs/Platform-Admin-Web/Reports/impl-06a-local-validation/`:

| File | Surface |
|---|---|
| `01-login-local-validation-1440x900-light.png` | Login with Test User tools |
| `02-login-local-validation-1440x900-dark.png` | Login Dark |
| `03-login-local-validation-375x812-light.png` | Login phone |
| `04-login-after-test-user-selected.png` | Olivia selected; email filled; password empty |
| `05-dashboard-after-test-login.png` | Overview after cookie login |

## Validation

| Check | Result |
|---|---|
| `npm run typecheck` / `lint` / `format:check` | PASS |
| `npm run test` | 141 PASS |
| `npm run build` | PASS |
| `npm run test:e2e` | 32 PASS |
| `npm run test:e2e:container` (8095) | 3 PASS |
| 8090 / 8091 / 8095 | PASS |
| `/config.js` `localValidationToolsEnabled:true` | PASS |
| Production-shaped Playwright (flag false) | selector absent |

## Visual approval

**STILL AWAITING PRODUCT OWNER + CHATGPT** (PWEB-IMPL-06).
