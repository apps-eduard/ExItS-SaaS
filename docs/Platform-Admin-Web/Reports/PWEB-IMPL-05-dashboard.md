# PWEB-IMPL-05 — Platform Admin Dashboard

**Status:** COMPLETE  
**Branch:** `feat/platform-admin-web-v2`  
**Predecessor:** PWEB-IMPL-04C (`bfeea4450112686505448a8fc6f333caae5c9a0c`)

## Delivered widgets

All widgets use existing Platform API list/health contracts. Counts use paged `totalCount` with `page=1` and a small `pageSize`. No unbounded list download is used to compute cards. No mutations. No “View” actions to UNDER_DEVELOPMENT routes.

| Widget | Backing requests | Authorization (UI) | Notes |
|---|---|---|---|
| Organizations summary | `GET /api/v1/platform/organizations` (`pageSize=1`, plus `status=Active` / `Closed`; Suspended reused from attention query `pageSize=5`) | `ViewPortfolio` or `ManageOrganizations` | Status distribution from server filters |
| Organizations needing attention | `GET /api/v1/platform/organizations?status=Suspended&page=1&pageSize=5` | same | Suspended is the API attention state (no separate needs-review org status) |
| Subscriptions | `GET /api/v1/platform/subscriptions` totals for all, `Trialing`, `Active`, `PastDue`, `GracePeriod` (`pageSize=1`) | `ManageSubscriptions` | Server list auth is `ViewPortfolio`; UI follows the dashboard spec |
| Accounts needing review | `GET /api/v1/platform/users?directory=Unassigned&pageSize=5` and `status=PendingVerification&pageSize=1` | `ManagePlatformUsers` | There is no `NeedsReview` account status. Unassigned matches Blazor Needs Review; pending verification is the incomplete-activation queue |
| Recent Platform activity | `GET /api/v1/platform/audit?page=1&pageSize=8` | `ViewAuditRecords` | Newest-first repository order; bounded page |
| Platform readiness | `GET /health` and `GET /health/ready` (text health-check bodies) | Existing `isPlatformAdministrator` UI flag | HTTP 503 + `Unhealthy` is shown as reported data, not coerced to healthy |

## Intentionally omitted

| Candidate | Reason |
|---|---|
| `GET /api/v1/platform/admin/portfolio-summary` | Mixes org/subscription/payment counts in one payload; failed counts are stored as `0` plus a `failures` list — unsafe for permission-shaped widgets and for “real zero” |
| Payment / entitlement / product KPIs | Not on the dashboard widget list; would require extra aggregation or unbounded reads |
| POS / PLM operational metrics | Out of Platform Admin Web scope |
| Click-through to Organizations, Users, Subscriptions, Audit, Health screens | Those React routes remain UNDER_DEVELOPMENT (04B / 04B-A preserved) |
| Charts | Status totals are clearer as compact counts |
| Fabricated healthy/demo values | Forbidden |

## Permission / query behavior

- Authorization loading: generic skeletons only (no privileged widget titles or data).
- Unauthorized widgets: not rendered.
- Independent widget queries: one failure shows inline Retry; other widgets continue. Widget errors do not open global Copy Diagnostics.
- Missing `totalCount` is treated as invalid (error + Retry), not as zero.

## 8095 integrated auth

No valid `deploy/docker/.env.local-validation` was available in this worktree or other local Desktop/ExItS copies. Secrets were not invented.

| Check | Result |
|---|---|
| 8090 `/admin/login` | Reachable HTTP 200 (existing Blazor Admin; unchanged) |
| 8091 `/health` | Reachable HTTP 200 |
| 8095 `/admin/login` | Not running in this package |
| Cookie login from 8095 | **INTEGRATED_8095_AUTH_NOT_REPROVED** |

PWEB-IMPL-06 must not receive visual approval until real 8095 integrated login is validated.

## Explicitly not claimed

- PWEB-IMPL-06 visual approval
- Backend/DB/POS/PLM changes
- Logout, CSRF, social login, PWA, cutover
- Docker/port topology changes
