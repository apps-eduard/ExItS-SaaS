# Organization Web — role and workflow matrix

**Status:** Engineering remediation (Phase 25 remains Open; this is not P25-WP10 closeout).  
**Related:** [phase-25-organization-web-admin.md](../phases/phase-25-organization-web-admin.md), [client-experience-boundaries.md](../architecture/client-experience-boundaries.md), [authorization-matrix.md](authorization-matrix.md).

## Boundary

Organization Web (`:8093`) is the **business management center**.

| Allowed | Denied |
|---|---|
| Organization Owner | Cashier (POS checkout role) |
| Organization Manager (`OrganizationAdministrator` membership or `StoreManager` POS role) | Ordinary POS-only / device-only identities |
| InventoryStaff / ReportingUser (limited nav) | Personal-only users without org membership |
| | Cart / checkout / barcode selling / payment taking |

Selling remains on PinoyBusinessPOS MAUI.

## Session / identity (runtime)

1. Authenticated Platform session cookie (`.ExItS.OrgWeb.Session`).
2. Selected Organization from Platform session / eligible memberships.
3. Product access Bearer from `IssueToken` (session grant) for POS business APIs.
4. Ambient flow (`OrgWebSessionAmbient`) bridges Blazor circuit → `IHttpClientFactory` handlers so Staging/Production POS APIs receive Bearer introspection — **not** Development-only headers.
5. Platform management APIs (`/api/v1/platform/...`) use **PlatformSession**; product Bearer must not clear that session (see `DevPlatformUserHeaderHandler` preserve rule).

### Development Test User (not Quick Login bypass)

| Rule | Behavior |
|---|---|
| Selector | Development/Testing only |
| Selection | Fills username only |
| Password | Manual entry |
| Auth | Normal login pipeline |
| Routing | Server workspaces: Platform → Org (Owner/Administrator) → Personal; Cashier org workspaces excluded |

See [organization-web-ui-responsive-standard.md](organization-web-ui-responsive-standard.md).

Development Quick Login one-click auto-auth is **removed** from Admin picker and MAUI Sign-In. Legacy `GET /admin/login/as/{key}` may remain for tooling but is not linked from the login UI.

## Role matrix (UI gate + existing server permissions)

| Area | Owner | Manager | Cashier |
|---|---|---|---|
| Overview | Yes | Yes | Denied (host) |
| Business profile | Yes | No (Owner-class only) | Denied |
| Branches | Yes | Yes | Denied |
| Devices / registers | Yes | Yes | Denied |
| Staff / roles | Yes | Yes | Denied |
| Customers | Yes | Yes | Denied |
| Catalog / inventory | Yes | Yes | Denied |
| Sales history / reports | Yes | Yes | Denied |
| Shifts (read/audit) | Yes | Yes | Denied |
| Operational settings | Yes | Yes | Denied |
| Sales documents / Owner education | Exact Owner | No | Denied |
| Ownership transfer | Exact Owner | No | Denied |
| Subscription | Exact Owner | No | Denied |
| POS checkout / CreateSale | **No** (unless separate product-local selling role) | Only if POS role allows | Per Cashier POS role |

Server APIs remain authoritative (`OrganizationManagementAuthority` + `PosRoleMatrix`, Platform membership). Nav hide is not sufficient; Cashier is blocked in `MainLayout` via `CanAccessOrganizationWeb`.

### Effective authority dimensions (do not mix)

| Dimension | Organization Owner | Organization Manager | Cashier |
|---|---|---|---|
| Organization Web management | **FULL** (membership) | Day-to-day subset | **NONE** (host denied) |
| POS management APIs | Full management projection (no automatic checkout) | Manager subset | Denied |
| POS checkout (`CreateSale` / `EnterPos`) | None unless product-local selling role | Only if role allows | Per POS role |
| Platform operator (`view_portfolio`, etc.) | None unless separately Platform staff | None | None |

**Invariant:** Platform `OrganizationOwner` membership ≠ automatic POS Cashier/Owner checkout role. Token issue may set `OrganizationManagementAuthority=true` for Owner/Administrator **from membership alone** (commercial entitlement is a separate paid-feature gate) when product-local role is missing — Org Web Bearer binds; checkout remains denied until a product-local selling role is assigned.

### Session grant / Bearer (Owner fix)

1. `IssueToken` (session grant) allows Organization Owner/Administrator even when `ProductLocalRoleMissing` (entitlement not required for core management tokens).
2. Introspection returns `organizationManagementAuthority` + `membershipRole` without inventing `MappedPosRoleCode=Owner`.
3. POS `PosRoleAuth` honors management authority for management capabilities; denies `CreateSale` / `EnterPos`.
4. Org Web hydrator binds Bearer from successful session grant — **not** from admin `/access/evaluate`.
5. Tokens persist on `OrgWebCircuitSession` and are re-applied via `CreateInboundActivityHandler` so Blazor Server AsyncLocal loss does not drop PlatformSession/Bearer on page calls.
6. See [runtime remediation](../reports/P25-org-web-runtime-owner-auth-and-icon-nav-remediation.md).

## Navigation map (implemented)

- Overview  
- Business → profile, branches, devices, registers  
- People → staff, roles, customers  
- Catalog → products, categories, global catalog  
- Inventory → stock, transfers, expiration  
- Sales → history, sales report, business credit  
- Operations → shifts, cash/shift report, inventory report  
- Settings → operational settings, sales documents, ownership, subscription, alerts  

## Deferred (APIs exist / thin UI)

| Item | Reason |
|---|---|
| Suppliers / purchase orders / goods receipt pages | Suppliers list + Connected ExItS connect + Incoming/Sent connection requests shipped (`/suppliers`, `/suppliers/requests`, `/suppliers/connect`); PO/goods-receipt Org Web pages still deferred |
| Expenses UI | Same |
| Sale returns management UI | Read history only today |
| Billing / plan change | Subscription is read-only |
| Full mobile drawer parity | Desktop sider is primary; drawer still reduced |

## Multi-org

Switch organization recalculates membership + POS role + capabilities. Owner in Org A / Manager in Org B / Cashier in Org C must resolve independently per selected OrganizationId.

## Privacy

No Personal Utang, no other Organizations, no device secrets, no Platform reviewer notes on Org Web. Management data stays Organization-scoped.

## Root cause (user-facing Staging / Branches errors)

1. `Development-stage organization, actor, and commercial headers are unavailable outside Development/Testing` — POS `PosOrganizationScope` when Bearer org/actor was missing on Staging. Fixed via ambient Bearer + organization binding.

2. `Actor 'development-operator:unauthenticated' does not hold permission 'platform.permission.view_portfolio'` on Branches — `DevPlatformUserHeaderHandler` cleared `Authorization: PlatformSession` when the scheme was not Bearer, leaving Platform APIs unauthenticated. Fixed by preserving PlatformSession and not attaching product Bearer to `/api/v1/platform/*` from Org Web outer auth handler. Membership-based `EnsureCanViewOrganizationAsync` remains sufficient; Org users are **not** granted `ViewPortfolio`.

3. Organization Owner authenticated and routed to Org Web but Overview/business APIs returned unauthorized — session hydrator required `/access/evaluate` Allowed and `IssueToken` refused product entry without a product-local role, so Bearer stayed null and POS fell through to Development-stage headers. Fixed by **Organization management authority**: Owner/Administrator receive session-grant Bearer and POS management capabilities without automatic `CreateSale`/`EnterPos`.

4. After the first management-authority fix, Local Validation still failed because Blazor Server lost `AsyncLocal` ambient PlatformSession/Bearer between hydrate and page HttpClient calls (`IHttpClientFactory` handlers are not circuit-scoped). Fixed by `OrgWebCircuitSession` + `CreateInboundActivityHandler` re-apply. Commercial entitlement is **not** required for core management token qualification. See [P25-org-web-runtime-owner-auth-and-icon-nav-remediation.md](../reports/P25-org-web-runtime-owner-auth-and-icon-nav-remediation.md).