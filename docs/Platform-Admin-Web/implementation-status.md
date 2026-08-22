# Platform Admin Web — Implementation Status

Program: `Platform Admin Web (Modernization)`  
Implementation start: `2026-08-19`  
Authorization: Product Owner authorized implementation through the first visual foundation checkpoint only  
Branch: `feat/platform-admin-web-v2`  
Application path: `src/Platform/ExItS.Platform.Admin.Web/`

## Gates

| Gate | Status | Notes |
|---|---|---|
| A — Documentation approved | PASSED | DOC-01..10 + AMEND-01 merged to main |
| B — Backend/browser readiness | PASSED for foundation | CSRF recorded as `BLOCKS_FUTURE_MUTATION`; OpenAPI recorded as `DEFERRED_TOOLING`; social-login session token in URL recorded as `BLOCKS_CUTOVER` for that flow |
| C — React scaffold | AUTHORIZED in PWEB-IMPL-01 | Feature screens not authorized |
| D–G | NOT AUTHORIZED | No cutover; no old Admin retirement |

## Scope boundaries

- Existing Blazor Admin (`src/Platform/ExItS.Platform.Admin`) remains the active operator console
- No cutover authorization
- No old Admin retirement authorization
- No POS/PLM operational scope
- Implementation authorized only through the first visual foundation checkpoint (Login + design foundation + shell + dashboard), after this scaffold package
- PWEB-IMPL-01 stops after scaffold; Login/Dashboard/shell UI are later packages

## Gate B findings (2026-08-19)

| Area | Classification | Evidence |
|---|---|---|
| `POST /api/v1/platform/auth/login` | READY_FOR_FOUNDATION | Cookie session established |
| `POST /api/v1/platform/auth/logout` | READY_FOR_FOUNDATION | Session invalidated; cookie deleted |
| `GET /api/v1/platform/auth/me` | READY_FOR_FOUNDATION | Session validate/renew |
| Account-profile selection | READY_FOR_FOUNDATION | `POST /api/v1/platform/auth/account-profiles/select` |
| Session cookie | READY_FOR_FOUNDATION | HttpOnly, SameSite=Lax, Secure outside Development/Testing |
| CORS + credentials | READY_FOR_FOUNDATION | `AllowCredentials()`; origins from `Cors:AllowedOrigins` (Vite origin must be added before cross-origin browser calls) |
| CSRF / antiforgery | BLOCKS_FUTURE_MUTATION | No `AddAntiforgery`/`UseAntiforgery` in Platform API; cookie-authenticated mutations are not yet confirmed CSRF-safe |
| problem+json | READY_FOR_FOUNDATION | `AddProblemDetails()` + pipeline exception handler |
| `X-Correlation-Id` | READY_FOR_FOUNDATION | Request/response correlation in `PlatformSecurityPipeline` |
| External authentication | READY_FOR_FOUNDATION for later screens | Google/Facebook challenge/complete exist when configured |
| Session token in external-login return URL | BLOCKS_CUTOVER | `ExternalAuthEndpoints` appends `sessionToken=` to `returnUrl`; reusable session credentials must not appear in URLs |
| Development/Testing test-user picker | READY_FOR_FOUNDATION | Visible only when Vite development/test/testing **or** explicit runtime `localValidationToolsEnabled` **and** Platform API `/local-validation/enabled` is true. Production remains hidden. |
| Mailpit | PRESERVED | Local Validation SMTP catcher; not changed in this package |
| Development/Testing Test Payments | PRESERVED | Local Validation only; no real payment integration |
| OpenAPI/Swagger | DEFERRED_TOOLING | No Swagger registration; typed clients remain manual |

Mailpit and Development/Testing fake payments remain in place. This package does not add production email or real payment integration.

## Package status

| Package | Status | Notes |
|---|---|---|
| PWEB-IMPL-01 | COMPLETE | React + Vite scaffold |
| PWEB-IMPL-02 | COMPLETE | Design system + global preferences foundation |
| PWEB-IMPL-03 | COMPLETE | Sign-In + session bootstrap |
| PWEB-IMPL-03A | COMPLETE | Restrict Development Test User frontend environment gate |
| PWEB-IMPL-04 | COMPLETE | Application shell + navigation foundation |
| PWEB-IMPL-04A | COMPLETE | Global Error Diagnostics + Copy |
| PWEB-IMPL-04B | COMPLETE | Known Route Under-Development State |
| PWEB-IMPL-04B-A | COMPLETE | Production nav implemented-only correction |
| PWEB-IMPL-04C | COMPLETE | Parallel React Local-Validation Container |
| PWEB-IMPL-05 | COMPLETE | Dashboard |
| PWEB-IMPL-06 | AWAITING VISUAL REVIEW | First Visual Checkpoint |
| PWEB-IMPL-06A | COMPLETE | Local Validation Test User runtime gate |
| PWEB-IMPL-06B | VISUAL DIRECTION ACCEPTED | Uniform shadcn structure + Stripe visual standard |
| PWEB-IMPL-06C | PRODUCT OWNER VISUAL APPROVED | Final shell, account, and audit polish |
| PWEB-IMPL-07 | COMPLETE | Organizations list |

## PWEB-IMPL-02 — Design system + global preferences

Status: **COMPLETE**

Implemented foundation (not visually approved; first visual checkpoint is not complete):

| Area | Record |
|---|---|
| Design system | Implemented React token foundation (`--exits-*` CSS variables; Light/Dark/System) |
| Language | `en` default / `fil-PH` |
| Theme | System / Light / Dark; default **System** |
| Density | Comfortable / Balanced / Compact; default **Balanced** (React-owned; C# DesignSystem unchanged) |
| Preferences | Non-sensitive UI prefs in `exits.platform-admin-web.ui-preferences.v1` |
| Motion | Restrained durations; `prefers-reduced-motion` honored |

Explicitly not claimed: visual approval, first visual checkpoint, Login, shell, or feature screens.

## PWEB-IMPL-03 — Authentication / Sign-In

Status: **COMPLETE**

| Area | Record |
|---|---|
| Auth client | Typed `login` + `auth/me` (+ unused `logout` contract) with cookies and problem+json |
| Session | Bootstrap via `GET /api/v1/platform/auth/me`; loading / authenticated / unauthenticated / expired |
| Sign In | `/admin/login` with AuthLayout, RHF + Zod, password visibility |
| Return path | Same-origin relative paths only; absolute/external URLs rejected |
| Session expired | `/admin/login?notice=session-expired` |
| External/social login | **DEFERRED** — complete flow appends `sessionToken=` to the return URL (`BLOCKS_CUTOVER`); buttons omitted |
| Development Test User | **COMPLETE** for Development/Testing frontend modes only: fills email/username from `quick-login-identities`; never embeds passwords |
| Logout UI | Not wired (CSRF `BLOCKS_FUTURE_MUTATION`) |

Explicitly not claimed: visual approval, first visual checkpoint, application shell, or Dashboard.

## PWEB-IMPL-03A — Development Test User environment gate

Status: **COMPLETE** (correction to PWEB-IMPL-03; PWEB-IMPL-03 remains COMPLETE)

Development Test User frontend environment gate changed from broad non-production behavior (`MODE !== "production"`) to an explicit Development/Testing allowlist (`development`, `test`, `testing`). Unrecognized modes fail closed. Backend `GET /api/v1/platform/local-validation/enabled` is still required before identities are queried.

## PWEB-IMPL-04 — Application shell + navigation

Status: **COMPLETE**

| Area | Record |
|---|---|
| Application shell | Implemented (`/admin`, sidebar, top bar, breadcrumbs, page header) |
| Navigation foundation | Registry-driven lifecycle + permission-aware UI shaping via `GET /api/v1/platform/authorization/me` |
| Responsive drawer | Implemented below 1024px |
| Preference integration | Language / theme / density in top-bar preferences menu; sidebar collapse persisted |
| Dashboard | **NOT implemented** |
| Global diagnostics | **NOT implemented** |
| Logout | **Not wired** (CSRF `BLOCKS_FUTURE_MUTATION`) |

Explicitly not claimed: visual approval, first visual checkpoint.

## PWEB-IMPL-04A — Global error diagnostics + copy

Status: **COMPLETE**

| Area | Record |
|---|---|
| Diagnostic model | Allowlisted report with client error reference |
| Global diagnostic notice | One persistent notice; Copy Diagnostics is direct |
| React error boundary | Compact fatal state + copy + reload/retry |
| API correlation | Request `X-Correlation-Id` retained; problem `traceId` used as server trace |
| Authorization load failure | Fail-closed + compact diagnostics (no permission dump) |
| Expected credential/session errors | Remain local / login notice UX |
| Dashboard | **NOT implemented** |
| External error service | **NONE** |

Follow-up recorded: **PWEB-IMPL-04B — Known Route Under-Development State** (AVAILABLE destinations that are not yet implemented currently use the shell catch-all not-found page).

Explicitly not claimed: visual approval, first visual checkpoint.

## PWEB-IMPL-04B — Known route under-development state

Status: **COMPLETE**

| Area | Record |
|---|---|
| Canonical navigation lifecycle | Preserved (`AVAILABLE` is not rewritten to `PLANNED_DISABLED` because a React screen is missing) |
| React implementation status | Separate (`IMPLEMENTED` / `UNDER_DEVELOPMENT`); Overview `/admin` is `IMPLEMENTED` |
| Production-shaped navigation | Implemented React destinations only (plus canonical `PLANNED_DISABLED` items that remain planned) |
| Development / test / testing | Authorized unimplemented production destinations appear under Development as non-navigable “Under development” items |
| Direct known routes | `/admin/organizations`, `/admin/users`, `/admin/products`, and other known unimplemented pathnames show Under development (query variants use pathname) |
| Unknown routes | Remain Page not found |
| Authorization | Fail-closed; no flash while loading; unauthorized users do not see privileged under-development details |
| Dashboard | **NOT implemented** |
| Diagnostics | **Unchanged** (under-development is not an error and does not trigger Copy Diagnostics) |
| Visual checkpoint | **Not approved / not claimed** |

Explicitly not claimed: visual approval, first visual checkpoint, Dashboard, Docker, logout.

## PWEB-IMPL-04B-A — Production nav implemented-only correction

Status: **COMPLETE**

| Area | Record |
|---|---|
| Canonical navigation lifecycle | Preserved (`AVAILABLE` / `PLANNED_DISABLED` metadata unchanged) |
| Production-shaped navigation | Implemented React destinations only (`PWEB-NAV-OVERVIEW` at this stage) |
| `PLANNED_DISABLED` presentation | Hidden from normal production/staging/preview/qa/uat/unknown navigation |
| Development / test / testing | Authorized planned items appear under Development as non-navigable “Planned”; under-development items remain non-navigable “Under development” |
| Direct known routes | Unchanged from PWEB-IMPL-04B |
| Dashboard | **Implemented** in PWEB-IMPL-05 |
| Docker | **NOT implemented** |

## PWEB-IMPL-04C — Parallel React local-validation container

Status: **COMPLETE**

| Area | Record |
|---|---|
| Blazor Admin | Unchanged on `localhost:8090` |
| Platform API | Unchanged on `localhost:8091`; CORS allowlist extended with the React origin only |
| React Admin | Production nginx image on `localhost:8095` (`admin-web-react`) |
| Cutover | **NONE** — parallel only |
| Dashboard | **Implemented** in PWEB-IMPL-05 |
| Auth model | Unchanged cookie session + `credentials: include`; API base URL injected at container start |

See `docs/Platform-Admin-Web/Reports/PWEB-IMPL-04C-parallel-react-local-validation-container.md`.

## PWEB-IMPL-05 — Dashboard

Status: **COMPLETE**

| Area | Record |
|---|---|
| Route | `/admin` Overview is a permission-aware control-center dashboard |
| Data | Real Platform API totals/lists only; no demo metrics |
| Widgets | Organizations summary + suspended attention; subscription status totals; unassigned + pending-verification accounts; recent audit; liveness/readiness |
| Navigation actions | None to UNDER_DEVELOPMENT destinations |
| Query policy | Independent TanStack Query widgets; `page=1` with pageSize 1/5/8; missing `totalCount` is not coerced to zero |
| Authorization | Widgets hidden until `authorization/me` is loaded; unauthorized widgets omitted |
| i18n / theme | EN + fil-PH; System / Light / Dark; density tokens unchanged |
| Visual checkpoint | **Not approved / not claimed** — PWEB-IMPL-06 |

See `docs/Platform-Admin-Web/Reports/PWEB-IMPL-05-dashboard.md`.

## PWEB-IMPL-06 — First visual checkpoint

Status: **AWAITING VISUAL REVIEW**

Cursor must **not** mark visual quality APPROVED. Product Owner + ChatGPT review is required.

| Area | Record |
|---|---|
| 8095 integrated auth | **PASS** — cookie session on `http://localhost:8095` against `http://localhost:8091` |
| Runtime `/config.js` | `platformApiBaseUrl` = `http://localhost:8091` |
| CORS | `Access-Control-Allow-Origin: http://localhost:8095` with credentials |
| Login / shell / dashboard polish | Applied for checkpoint screenshots; Gate not complete |
| Screenshots | `docs/Platform-Admin-Web/Reports/impl-06-visual-checkpoint/` |
| Visual status | **AWAITING PRODUCT OWNER + CHATGPT REVIEW** |

See `docs/Platform-Admin-Web/Reports/PWEB-IMPL-06-first-visual-checkpoint.md`.

## PWEB-IMPL-06A — Local Validation Test User runtime gate

Status: **COMPLETE**

Test User tools remain available only in explicit Local Validation / Vite development / test. Real production stays hidden. Visual approval of PWEB-IMPL-06 is still awaiting Product Owner + ChatGPT.

| Area | Record |
|---|---|
| Production Vite `MODE` | Does not hide Local Validation Test User by itself when runtime flag is explicitly true |
| Runtime `/config.js` | `localValidationToolsEnabled` boolean; default/missing = false; never inferred from hostname/port |
| Local Validation 8095 | `LOCAL_VALIDATION_TOOLS_ENABLED=true` on `admin-web-react` |
| Double gate | Frontend permit **and** `GET /api/v1/platform/local-validation/enabled` → true, then identities |
| Password | Never filled, retrieved, displayed, or stored by the selector |
| Login | Still `POST /api/v1/platform/auth/login`; no GUID bypass |
| Screenshots | `docs/Platform-Admin-Web/Reports/impl-06a-local-validation/` |
| Visual approval | **STILL AWAITING PRODUCT OWNER + CHATGPT** (PWEB-IMPL-06) |

See `docs/Platform-Admin-Web/Reports/PWEB-IMPL-06A-local-validation-test-user.md`.

## PWEB-IMPL-06B — Uniform shadcn / Stripe visual system

Status: **VISUAL DIRECTION ACCEPTED**

The 06B visual direction is the permanent Platform Admin standard. PWEB-IMPL-06C is a narrow polish pass on that direction, not a redesign.

See `docs/Platform-Admin-Web/Reports/PWEB-IMPL-06B-uniform-shadcn-stripe-visual-system.md`.

## PWEB-IMPL-06C — Final shell, account, and audit polish

Status: **PRODUCT OWNER VISUAL APPROVED**

| Area | Record |
|---|---|
| Sidebar collapse | Moved to desktop top bar; removed from sidebar header |
| Mobile nav | Existing Menu drawer preserved |
| Account | Generated initials avatar; menu shows name/email; **Sign out** calls `POST /api/v1/platform/auth/logout` |
| Audit | Presentation mapping for known codes; raw values remain in `title` / screen-reader text |
| Actor | `platform-user:<GUID>` shown as Platform user + compact GUID; full value retained |
| Platform readiness | Same bordered operational surface as other sections; still low visual weight |
| Screenshots | `docs/Platform-Admin-Web/Reports/impl-06c-final-polish/` |
| Visual status | **PRODUCT OWNER VISUAL APPROVED** |

See `docs/Platform-Admin-Web/Reports/PWEB-IMPL-06C-final-shell-account-audit-polish.md`.

## PWEB-IMPL-07 — Organizations list

Status: **COMPLETE**

Read-only Organizations list at `/admin/organizations`. No create/edit/delete. CSRF remains `BLOCKS_FUTURE_MUTATION`. Social-auth token-in-URL remains `BLOCKS_CUTOVER`.

See `docs/Platform-Admin-Web/Reports/PWEB-IMPL-07-organizations-list.md`.

## PWEB-IMPL-08 — Organization workspace + Overview

Status: **COMPLETE** after validation

Read-only `/admin/organizations/:organizationId` workspace shell with Overview only. Nested layout is ready for later tabs; no dead tabs are shown. CSRF remains `BLOCKS_FUTURE_MUTATION`. Social-auth token-in-URL remains `BLOCKS_CUTOVER`. PWA is not in this package.

| Area | Record |
|---|---|
| Organization Workspace | Overview **IMPLEMENTED** |
| Branches | **IMPLEMENTED — READ ONLY** |
| People/Memberships | **NOT STARTED** |
| Products/Access | **NOT STARTED** |
| Subscription | **NOT STARTED** |
| Entitlements | **NOT STARTED** |
| Billing | **NOT STARTED** |
| Activity/Audit | **NOT STARTED** |
| CSRF | `BLOCKS_FUTURE_MUTATION` |
| Social-auth token-in-URL | `BLOCKS_CUTOVER` |
| PWA | **NOT IN THIS PACKAGE** |

See `docs/Platform-Admin-Web/Reports/PWEB-IMPL-08-organization-workspace-overview.md`.

## PWEB-IMPL-09 — Organization workspace / Branches

Status: **COMPLETE** after validation

Read-only Branches at `/admin/organizations/:organizationId/branches`. Workspace navigation exposes Overview and Branches only. No branch detail, no mutations, no invented search/pagination. CSRF remains `BLOCKS_FUTURE_MUTATION`. Social-auth token-in-URL remains `BLOCKS_CUTOVER`. Platform Admin is **WEB ONLY**. PWA is **NOT PLANNED**.

| Area | Record |
|---|---|
| Overview | **IMPLEMENTED** |
| Branches | **IMPLEMENTED — READ ONLY** |
| People/Memberships | **NOT STARTED** |
| Products/Access | **NOT STARTED** |
| Subscription | **NOT STARTED** |
| Entitlements | **NOT STARTED** |
| Billing | **NOT STARTED** |
| Activity/Audit | **NOT STARTED** |
| Branch API | Non-paged `GET .../branches` only; no server search/filter/sort in this package |
| CSRF | `BLOCKS_FUTURE_MUTATION` |
| Social-auth token-in-URL | `BLOCKS_CUTOVER` |
| Platform Admin | **WEB ONLY** |
| PWA | **NOT PLANNED** |

See `docs/Platform-Admin-Web/Reports/PWEB-IMPL-09-organization-branches.md`.

## PWEB-IMPL-10 — Organization workspace / People

Status: **COMPLETE** after validation

Read-only People at `/admin/organizations/:organizationId/people` with independent Members and Invitations sections. Workspace navigation exposes Overview, Branches, and People only. No invite/revoke/resend. Invitation accept tokens are never mapped or rendered. CSRF remains `BLOCKS_FUTURE_MUTATION`. Social-auth token-in-URL remains `BLOCKS_CUTOVER`. Platform Admin is **WEB ONLY**. PWA is **NOT PLANNED**.

| Area | Record |
|---|---|
| Overview | **IMPLEMENTED** |
| Branches | **IMPLEMENTED — READ ONLY** |
| People/Memberships | **IMPLEMENTED — READ ONLY** |
| Products/Access | **NOT STARTED** |
| Subscription | **NOT STARTED** |
| Entitlements | **NOT STARTED** |
| Billing | **NOT STARTED** |
| Activity/Audit | **NOT STARTED** |
| Members API | Paged `GET .../members` with `status`, `page`, `pageSize`. No search. |
| Invitations API | Paged `GET .../invitations` with `status`, `page`, `pageSize`. Sanitized; accept tokens omitted. |
| CSRF | `BLOCKS_FUTURE_MUTATION` |
| Social-auth token-in-URL | `BLOCKS_CUTOVER` |
| Platform Admin | **WEB ONLY** |
| PWA | **NOT PLANNED** |

See `docs/Platform-Admin-Web/Reports/PWEB-IMPL-10-organization-people-memberships.md`.

## PWEB-IMPL-11 — Organization workspace / Products

Status: **COMPLETE** after validation

Read-only Products/Access at `/admin/organizations/:organizationId/products` from commercial-summary `latestEntitlements`. No catalog fan-out, no invented totals, no POS/PLM operations. CSRF remains `BLOCKS_FUTURE_MUTATION`. Social-auth token-in-URL remains `BLOCKS_CUTOVER`. Platform Admin is **WEB ONLY**. PWA is **NOT PLANNED**.

| Area | Record |
|---|---|
| Overview | **IMPLEMENTED** |
| Branches | **IMPLEMENTED — READ ONLY** |
| People/Memberships | **IMPLEMENTED — READ ONLY** |
| Products/Access | **IMPLEMENTED — READ ONLY** |
| Subscription | **NOT STARTED** |
| Entitlements | **NOT STARTED** |
| Billing | **NOT STARTED** |
| Activity/Audit | **NOT STARTED** |
| CSRF | `BLOCKS_FUTURE_MUTATION` |
| Social-auth token-in-URL | `BLOCKS_CUTOVER` |
| Platform Admin | **WEB ONLY** |
| PWA | **NOT PLANNED** |

See `docs/Platform-Admin-Web/Reports/PWEB-IMPL-11-organization-products-access.md`.

## PWEB-IMPL-12 — Organization workspace / Subscriptions

Status: **COMPLETE** after validation

Read-only Subscription at `/admin/organizations/:organizationId/subscription` using org-scoped `GET .../subscriptions` with `status`, `search`, `isTrial`, `productCode`, `sortBy`, `sortDesc`, `page`, `pageSize`. No activate/cancel/plan-change. CSRF remains `BLOCKS_FUTURE_MUTATION`. Platform Admin is **WEB ONLY**. PWA is **NOT PLANNED**.

| Area | Record |
|---|---|
| Overview | **IMPLEMENTED** |
| Branches | **IMPLEMENTED — READ ONLY** |
| People/Memberships | **IMPLEMENTED — READ ONLY** |
| Products/Access | **IMPLEMENTED — READ ONLY** |
| Subscription | **IMPLEMENTED — READ ONLY** |
| Entitlements | **NOT STARTED** |
| Billing | **NOT STARTED** |
| Activity/Audit | **NOT STARTED** |
| CSRF | `BLOCKS_FUTURE_MUTATION` |
| Social-auth token-in-URL | `BLOCKS_CUTOVER` |
| Platform Admin | **WEB ONLY** |
| PWA | **NOT PLANNED** |

See `docs/Platform-Admin-Web/Reports/PWEB-IMPL-12-organization-subscriptions.md`.

## PWEB-IMPL-13 — Organization workspace / Entitlements

Status: **COMPLETE** after validation

Read-only Entitlements at `/admin/organizations/:organizationId/entitlements`. Product codes come from already-authorized commercial-summary `latestEntitlements`. Snapshot history is product-scoped only: `GET .../products/{productCode}/entitlements/snapshots?page&pageSize`. URL `product` is sanitized against returned codes before the history call. No snapshot detail route. No override mutations. CSRF remains `BLOCKS_FUTURE_MUTATION`. Platform Admin is **WEB ONLY**. PWA is **NOT PLANNED**.

| Area | Record |
|---|---|
| Overview | **IMPLEMENTED** |
| Branches | **IMPLEMENTED — READ ONLY** |
| People/Memberships | **IMPLEMENTED — READ ONLY** |
| Products/Access | **IMPLEMENTED — READ ONLY** |
| Subscription | **IMPLEMENTED — READ ONLY** |
| Entitlements | **IMPLEMENTED — READ ONLY** |
| Billing | **NOT STARTED** |
| Activity/Audit | **NOT STARTED** |
| CSRF | `BLOCKS_FUTURE_MUTATION` |
| Social-auth token-in-URL | `BLOCKS_CUTOVER` |
| Platform Admin | **WEB ONLY** |
| PWA | **NOT PLANNED** |

See `docs/Platform-Admin-Web/Reports/PWEB-IMPL-13-organization-entitlements.md`.

## PWEB-IMPL-14 — Organization workspace / Billing

Status: **COMPLETE** after validation

Read-only Billing at `/admin/organizations/:organizationId/billing` using org-scoped `GET .../payments` with `status`, `page`, `pageSize`. Platform SaaS money only — not POS sales/tenders/cash or PLM loan/collections. No Record/Confirm/Reject/Void. 403 fail-closes without leaking amounts. CSRF remains `BLOCKS_FUTURE_MUTATION`. Platform Admin is **WEB ONLY**. PWA is **NOT PLANNED**.

| Area | Record |
|---|---|
| Overview | **IMPLEMENTED** |
| Branches | **IMPLEMENTED — READ ONLY** |
| People/Memberships | **IMPLEMENTED — READ ONLY** |
| Products/Access | **IMPLEMENTED — READ ONLY** |
| Subscription | **IMPLEMENTED — READ ONLY** |
| Entitlements | **IMPLEMENTED — READ ONLY** |
| Billing | **IMPLEMENTED — READ ONLY** |
| Activity/Audit | **NOT STARTED** |
| CSRF | `BLOCKS_FUTURE_MUTATION` |
| Social-auth token-in-URL | `BLOCKS_CUTOVER` |
| Platform Admin | **WEB ONLY** |
| PWA | **NOT PLANNED** |

See `docs/Platform-Admin-Web/Reports/PWEB-IMPL-14-organization-billing.md`.

## PWEB-IMPL-14A — Organization workspace read-only polish

Status: **COMPLETE** after validation

Polish pass for read-only organization workspace: shared subscription status presentation (Cancelled/Expired i18n, consistent tones), entitlement grant feature codes with Enabled/Disabled states, and regression/screenshot closeout. No backend changes. No new mutations. Activity/Audit not started.

| Area | Record |
|---|---|
| Overview | **IMPLEMENTED** |
| Branches | **IMPLEMENTED — READ ONLY** |
| People/Memberships | **IMPLEMENTED — READ ONLY** |
| Products/Access | **IMPLEMENTED — READ ONLY** |
| Subscription | **IMPLEMENTED — READ ONLY** |
| Entitlements | **IMPLEMENTED — READ ONLY** |
| Billing | **IMPLEMENTED — READ ONLY** |
| Activity/Audit | **NOT STARTED** |
| CSRF | `BLOCKS_FUTURE_MUTATION` |
| Social-auth token-in-URL | `BLOCKS_CUTOVER` |
| Platform Admin | **WEB ONLY** |
| PWA | **NOT PLANNED** |

See `docs/Platform-Admin-Web/Reports/PWEB-IMPL-14A-organization-readonly-polish.md`.

## PWEB-IMPL-14B — Entitlements compact grants disclosure

Status: **COMPLETE**

See `docs/Platform-Admin-Web/Reports/PWEB-IMPL-14B-entitlements-compact-grants.md`.

## PWEB-IMPL-14C — Full Platform Admin navigation blueprint

Status: **COMPLETE**

See `docs/Platform-Admin-Web/Reports/PWEB-IMPL-14C-navigation-blueprint.md`.

## PWEB-IMPL-14D — Product-filtered Organizations

Status: **COMPLETE** after PWEB-IMPL-14D-R1

Canonical UI/URL `/admin/organizations?product=<code>` maps to server `productCode` after catalog sanitation. Server-authoritative product filtering is available via organization-level Platform subscriptions.

See `docs/Platform-Admin-Web/Reports/PWEB-IMPL-14D-product-filtered-organizations.md` and `docs/Platform-Admin-Web/Reports/PWEB-IMPL-14D-R1-product-organizations-server-filter.md`.

## PWEB-IMPL-15 — Organization Activity / Audit

Status: **COMPLETE** after validation

Read-only organization workspace Activity / Audit tab backed by
`GET /api/v1/platform/organizations/{organizationId}/audit` with supported
server filters only. Reuses dashboard audit presentation helpers. No mutations,
no export, no global Audit Log implementation, no backend changes.

| Area | Record |
|---|---|
| Overview | **IMPLEMENTED** |
| Branches | **IMPLEMENTED — READ ONLY** |
| People/Memberships | **IMPLEMENTED — READ ONLY** |
| Products/Access | **IMPLEMENTED — READ ONLY** |
| Subscription | **IMPLEMENTED — READ ONLY** |
| Entitlements | **IMPLEMENTED — READ ONLY** |
| Billing | **IMPLEMENTED — READ ONLY** |
| Activity/Audit | **IMPLEMENTED — READ ONLY** |
| CSRF | `BLOCKS_FUTURE_MUTATION` |
| Social-auth token-in-URL | `BLOCKS_CUTOVER` |
| Platform Admin | **WEB ONLY** |
| PWA | **NOT PLANNED** |

See `docs/Platform-Admin-Web/Reports/PWEB-IMPL-15-organization-activity-audit.md`.

## PWEB-IMPL-16 — Platform Users Directory

Status: **COMPLETE** after validation

Read-only `/admin/users` directory with URL views mapped to actual API `directory` values (`PlatformStaff`, `Organization`, `Personal`, `Unassigned`). Needs Review uses `directory=Unassigned`, not a fake account status. No mutation controls.

See `docs/Platform-Admin-Web/Reports/PWEB-IMPL-16-platform-users-directory.md`.

## Queue

| Package | Status |
|---|---|
| PWEB-IMPL-05 — Dashboard | COMPLETE |
| PWEB-IMPL-06 — First Visual Checkpoint | AWAITING VISUAL REVIEW |
| PWEB-IMPL-06A — Local Validation Test User | COMPLETE |
| PWEB-IMPL-06B — Uniform visual system | VISUAL DIRECTION ACCEPTED |
| PWEB-IMPL-06C — Final polish | PRODUCT OWNER VISUAL APPROVED |
| PWEB-IMPL-07 — Organizations list | COMPLETE |
| PWEB-IMPL-08 — Organization workspace Overview | COMPLETE |
| PWEB-IMPL-09 — Organization workspace Branches | COMPLETE |
| PWEB-IMPL-10 — Organization workspace People | COMPLETE |
| PWEB-IMPL-11 — Organization workspace Products | COMPLETE |
| PWEB-IMPL-12 — Organization workspace Subscriptions | COMPLETE |
| PWEB-IMPL-13 — Organization workspace Entitlements | COMPLETE |
| PWEB-IMPL-14 — Organization workspace Billing | COMPLETE |
| PWEB-IMPL-14A — Organization read-only polish | COMPLETE |
| PWEB-IMPL-14B — Entitlements compact grants | COMPLETE |
| PWEB-IMPL-14C — Navigation blueprint | COMPLETE |
| PWEB-IMPL-14D — Product Organizations | COMPLETE |
| PWEB-IMPL-16 — Platform Users Directory | COMPLETE |
| PWEB-IMPL-17 — Platform User Detail | COMPLETE |
| PWEB-IMPL-18 — Product Catalog | COMPLETE |
| PWEB-IMPL-19 — Product Detail + Plans | COMPLETE |
| PWEB-IMPL-20 — CSRF Mutation Foundation | COMPLETE |
| CSRF compatibility gate | COMPLETE (`06e5cc1c`) |
| PWEB-IMPL-21 … PWEB-IMPL-30 | **DOCUMENTED — NOT STARTED** (see `pweb-21-30-continuation-plan.md`) |

Next authorized implementation package: **PWEB-IMPL-21** (requires explicit start; documentation alone does not authorize implementation).

### Commercial / subscription overlay (2026-08-22)

Read-only commercial inspection (PWEB-18/19 catalog + PWEB-11…14 org commercial tabs) is **COMPLETE**. Commercial mutation **foundation** (PA-COM-01) is **COMPLETE** on `feat/platform-admin-pa-com-01` — typed clients + hooks + tests; **no** lifecycle UI.

- [commercial-subscription-implementation-plan.md](./commercial-subscription-implementation-plan.md) — PA-COM-01 COMPLETE; PA-COM-02…08 not authorized
- [Reports/PLATFORM-WEB-COMMERCIAL-READINESS-AUDIT-01.md](./Reports/PLATFORM-WEB-COMMERCIAL-READINESS-AUDIT-01.md)
- [Reports/PLATFORM-WEB-PA-COM-01-commercial-mutation-foundation.md](./Reports/PLATFORM-WEB-PA-COM-01-commercial-mutation-foundation.md)

`PA_COM_04_AUTHORIZED=NO`. Full Platform Admin → POS commercial E2E is **NOT READY**. PWEB-IMPL-21 remains the identity/governance next package if that track is started first; commercial E2E does not require 21–23 when the operator is already Platform Administrator.

Production Ready: **NO**
Cutover Authorized: **NO**
Create Organization in Platform Admin: **PROHIBITED**
Create Product in Platform Admin: **PROHIBITED**
`PLM_PWA_CSRF_COMPAT_REVIEW_REQUIRED`: **YES**
`POS_REACT_CSRF_COMPAT_REVIEW_REQUIRED`: **YES**
Social-auth URL blocker: **OPEN** (`BLOCKS_CUTOVER`)
