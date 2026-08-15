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

Development Quick Login may still use Dev helpers on Development/Testing hosts. Normal authenticated runtime must not require `X-Dev-Platform-User-Id` / commercial Dev headers.

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

Server APIs remain authoritative (`PosRoleMatrix`, Platform membership). Nav hide is not sufficient; Cashier is blocked in `MainLayout` via `CanAccessOrganizationWeb`.

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
| Suppliers / purchase orders / goods receipt pages | Clients registered; dedicated Org Web pages not shipped in this pass |
| Expenses UI | Same |
| Sale returns management UI | Read history only today |
| Billing / plan change | Subscription is read-only |
| Full mobile drawer parity | Desktop sider is primary; drawer still reduced |

## Multi-org

Switch organization recalculates membership + POS role + capabilities. Owner in Org A / Manager in Org B / Cashier in Org C must resolve independently per selected OrganizationId.

## Privacy

No Personal Utang, no other Organizations, no device secrets, no Platform reviewer notes on Org Web. Management data stays Organization-scoped.

## Root cause (user-facing Staging error)

`Development-stage organization, actor, and commercial headers are unavailable outside Development/Testing` came from POS `PosOrganizationScope` when Bearer org/actor was missing on Staging. Org Web HttpClient handlers could not see circuit `AccessToken`, so business calls fell through to Dev-header path and failed. Fixed via ambient Bearer + organization binding.
