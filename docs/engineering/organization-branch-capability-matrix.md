# Organization + Branch Capability Matrix

**Status:** Authoritative baseline (P28-WP15A)  
**Related:** [client-experience-boundaries](../architecture/client-experience-boundaries.md) | [organization-web-role-and-workflow-matrix](organization-web-role-and-workflow-matrix.md) | [organization-branches-and-fulfillment-locations](organization-branches-and-fulfillment-locations.md) | [authorization-matrix](authorization-matrix.md) | [P28-WP14 workspace selection](../reports/P28-WP14-unified-organization-branch-workspace-selection.md)

## Purpose

Define canonical rules for **who may do what**, **where** (Mobile vs Web), and **under which branch context**, before adding more authorization code.

This document is policy-first. **APIs remain authoritative.** UI hiding is never authorization.

---

## Locked product principles

1. **Workspace = Organization + Branch** (`OrganizationId` + `SelectedBranchId`).
2. **Workspace selection does not grant POS permission.** `CreateSale` / `EnterPos` remain product-local role + device gates.
3. **Organization Owner authority ≠ POS selling permission.** Membership Owner/Administrator does not auto-grant checkout.
4. **Mobile Primary/Main is the gateway to organization governance** on MAUI — not a superuser identity, but the **selected workspace branch** that exposes org-wide management entry points.
5. **Mobile non-primary branch workspace** is primarily **branch configuration + branch operations** — not organization-wide governance surfaces.
6. **Organization Web** is full organization management and **does not require selecting Main** merely to manage the business.
7. **UI hiding is not authorization.** Every row below assumes server enforcement.
8. **Physical/money/stock attestations** require **exact branch context** where the domain record is branch-scoped.
9. **Every meaningful mutation** must have a traceable human/system **actor** (`ActorId` / audit subject).
10. **Financial/transaction history is not deleted.** Use cancel, void, reversal, or status transition per domain rules.
11. **Master/configuration records** use suspend/archive — not hard delete — where the domain supports it.
12. **Critical actions** (when classified below) require **reason + password step-up + audit** once step-up infrastructure exists for that surface. Until then, enforce reason/actor where already implemented; do not claim step-up where absent.

---

## Scope classes

| Scope | Meaning | Examples |
|---|---|---|
| **OrganizationGovernance** | Org-wide policy, membership, subscription, ownership | Staff invite, subscription view, ownership transfer |
| **BranchConfiguration** | Branch master data and readiness | Create branch, hours, fulfillment toggles, archive branch |
| **BranchOperation** | Branch-attested operational state | Sale, shift, stock movement, transfer dispatch/receive at a branch |

**Data owner** follows [organization-branches-and-fulfillment-locations](organization-branches-and-fulfillment-locations.md): org owns catalog/customers/subscription; branch owns overlay inventory, devices, shifts, sale origin branch.

---

## Context model (WP11–WP14 baseline)

```text
User
└ Organization (membership + entitlement)
   └ Branch workspace (SelectedBranchId — management context)
      └ POS operational context (device BranchId / PosDeviceId — selling context)
```

| Context | Session / header | Used for |
|---|---|---|
| Organization | `OrganizationId`, Platform org context | Governance APIs, org-scoped catalog/customers |
| Management branch | `SelectedBranchId`, `X-Pos-Branch-Id` | Branch UI, branch-scoped reads/writes, management stock views |
| Device branch | `AuthSession.BranchId`, device registration | Enter POS, checkout, device-bound money/stock |

Switching workspace runs `SelectWorkspaceAsync` (org then branch). Open cashier shift blocks switching **operational** selected branch to a different id.

---

## Mobile exposure modes

| Mode | Rule |
|---|---|
| **Primary/Main only** | MAUI surfaces OrganizationGovernance entry points only when workspace `SelectedBranchId` is the org **Primary** branch (`IsPrimary`). |
| **Exact selected branch** | Action applies only to the workspace-selected branch (or device-bound branch for POS money paths). |
| **Any permitted branch** | User may act on any Active branch they can access under current resolver (today: owner path = all Active org branches; staff ACL **not implemented**). |
| **Not Mobile** | Web-only or Platform-only for MVP/practical UX. |

**Organization Web:** Owner/Administrator/Manager use centralized nav; branch hierarchy is informational — **no fake Main selection required**.

---

## Authorization dimensions (do not mix)

| Dimension | Source | Notes |
|---|---|---|
| Platform membership role | Platform `OrganizationMember.Role` | Owner, Administrator, Member, … |
| Organization management authority | Bearer / session grant | Org Web + management APIs; ≠ checkout |
| POS product-local role | POS role assignment | Owner ⊇ Manager ⊇ Cashier capabilities |
| UtangCapability / feature codes | POS permission matrix | Authoritative for POS API operations |
| Entitlement / subscription | Commercial gate | Feature-level; fails closed |
| Branch access ACL | `IAccessibleBranchResolver` | Owner path = all Active branches; staff path filters by `organization_membership_branch_assignments` (WP15C) |

---

## Capability matrix

Legend:

- **Mobile:** `Primary` | `Exact` | `Any` | `No`
- **Web:** `Yes` | `No` | `Read`
- **Branch access:** whether resolver/ACL must include target branch (staff ACL via WP15C)
- **Device match:** POS device registration branch must equal operational branch
- **Shift:** open shift/register rules
- **Actor:** mutation records acting user id
- **Audit:** durable audit event (Platform audit vs POS actor-on-record)
- **Reason:** explicit reason field required
- **Step-up:** password re-auth required (target policy)
- **Lifecycle:** `update` | `archive` | `void/reversal` | `immutable`

### Organization governance

| Capability | Data owner | Scope | Required role | Branch access | Mobile | Web | Exact branch | Device match | Shift | Actor | Audit | Reason | Step-up | Lifecycle |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| View organization profile | Platform org profile | OrgGov | Owner; Staff view policy | No | Primary | Yes | No | No | No | No | Read | No | No | Read |
| Edit organization profile | Platform org profile | OrgGov | Owner | No | Primary | Yes | No | No | No | Yes | Platform | No | Target | Update |
| View subscription / entitlement | Platform commercial | OrgGov | Owner; Mgr read | No | Primary | Yes (Owner sub) | No | No | No | No | Read | No | No | Read |
| Change plan / billing | Platform commercial | OrgGov | Owner | No | No | Read/defer | No | No | No | Yes | Platform | Yes | Target | Update |
| Staff invite | Platform membership | OrgGov | Owner; Mgr policy | No | Primary | Yes | No | No | No | Yes | Platform | No | Target | Update |
| Staff remove / suspend membership | Platform membership | OrgGov | Owner | No | Primary | Yes | No | No | No | Yes | Platform | Yes | Target | Archive |
| Assign/revoke POS product role | Platform + POS | OrgGov | Owner; Mgr policy | No | Primary | Yes | No | No | No | Yes | Platform + POS assignment record | No | Target | Update |
| Staff branch assignment | Platform | OrgGov | Owner | Per branch | No | Target | No | No | No | Yes | Target | No | Target | Update |
| Ownership transfer request/accept | Platform org | OrgGov | Exact Owner / Personal acceptor | No | Primary | Yes | No | No | No | Yes | Platform | Yes | Target | Update |
| Sales-document Owner education ack | Platform compliance | OrgGov | Exact Owner | No | Primary | Yes | No | No | No | Yes | Platform | No | Target | Immutable ack |
| Tax/compliance profile (when enabled) | Platform compliance | OrgGov | Owner | No | No | Yes | No | No | No | Yes | Platform | No | Target | Update |
| Org notifications inbox | Platform | OrgGov | Owner; Mgr | No | Primary | Yes | No | No | No | Yes | Platform | No | No | Read/update read state |
| Reporting / audit investigation | Mixed | OrgGov | Owner; Mgr; ReportingUser | No | Primary summary | Yes full | No | No | No | Yes | Platform governance + POS actor read | No | No | Read |

### Branch configuration

| Capability | Data owner | Scope | Required role | Branch access | Mobile | Web | Exact branch | Device match | Shift | Actor | Audit | Reason | Step-up | Lifecycle |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Create branch | Platform branch | BranchCfg | Owner; Mgr | Org capacity | Primary | Yes | No | No | No | Yes | Platform | No | Target | Update |
| Edit branch details/address | Platform branch | BranchCfg | Owner; Mgr | Target branch | Exact | Yes | Yes | No | No | Yes | Platform | No | No | Update |
| Change primary branch | Platform branch | BranchCfg | Owner | Target branch | Primary | Yes | Yes | No | No | Yes | Platform | Yes | Target | Update |
| Archive / suspend branch | Platform branch | BranchCfg | Owner | Target branch | Primary | Yes | Yes | No | No | Yes | Platform | Yes | Target | Archive |
| Reactivate branch | Platform branch | BranchCfg | Owner | Target branch | Primary | Yes | Yes | No | No | Yes | Platform | Yes | Target | Update |
| Store hours / timezone | Platform branch | BranchCfg | Owner; Mgr | Target branch | Exact | Yes | Yes | No | No | Yes | Platform | No | No | Update |
| Fulfillment toggles (pickup/delivery/ordering) | Platform branch | BranchCfg | Owner; Mgr | Target branch | Exact | Yes | Yes | No | No | Yes | Platform | No | No | Update |
| Delivery policy / pricing | Platform branch | BranchCfg | Owner; Mgr | Target branch | Exact | Yes | Yes | No | No | Yes | Platform | No | No | Update |
| Online orders pause | Platform branch | BranchCfg | Owner; Mgr | Target branch | Exact | Yes | Yes | No | No | Yes | Platform | Optional reason field exists | No | Update |
| List branches (read) | Platform branch | BranchCfg | Owner; Mgr; staff view | Any accessible | Any | Yes | No | No | No | No | Read | No | No | Read |

### Catalog & customers (organization-owned master)

| Capability | Data owner | Scope | Required role | Branch access | Mobile | Web | Exact branch | Device match | Shift | Actor | Audit | Reason | Step-up | Lifecycle |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Catalog product/category CRUD | POS org catalog | OrgGov | POS Owner/Mgr (`ManageCatalog`) | No | Primary | Yes | No | No | No | Yes | Actor on record | No | No | Update/archive |
| Customer master CRUD | POS org customer | OrgGov | POS Owner/Mgr | No | Any | Yes | No | No | No | Yes | Actor on record | No | No | Update |
| Global catalog adoption | POS org catalog | OrgGov | POS Owner/Mgr | No | Any | Yes | No | No | No | Yes | Actor on record | No | No | Update |

Catalog/customer data is **organization-scoped**. Branch workspace does not duplicate master records; branch context affects **operational** views (stock, orders, sales).

### Devices & registers

| Capability | Data owner | Scope | Required role | Branch access | Mobile | Web | Exact branch | Device match | Shift | Actor | Audit | Reason | Step-up | Lifecycle |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Register POS device | Platform device + session | BranchCfg | Owner; Mgr | Registration branch | Exact | Yes | Yes | Yes at register | No | Yes | Platform + device record | No | No | Update |
| Revoke POS device | Platform device | BranchCfg | Owner; Mgr | Device branch | Exact | Yes | Yes | No | No | Yes | Platform | Yes | Target | Archive |
| Register/cash drawer CRUD | POS register | BranchCfg | POS Owner/Mgr | Branch devices | Exact | Yes | Yes | No | No open shift on deactivate | Yes | Actor on record | No | No | Update |

### POS operations (branch-attested)

| Capability | Data owner | Scope | Required role | Branch access | Mobile | Web | Exact branch | Device match | Shift | Actor | Audit | Reason | Step-up | Lifecycle |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Enter POS / selling mode | Session | BranchOp | `CreateSale` capability | Selected + device | Exact | No | Yes | **Yes** | No | Yes | Session | No | No | — |
| Create sale / checkout | POS sale | BranchOp | `CreateSale` | Sale branch | Exact | No | Yes | **Yes** | Open shift + register | Yes | Sale immutable + actor | No | No | **Immutable** |
| Void sale | POS sale | BranchOp | `VoidSale` (Mgr+) | Sale branch | Exact | No | Yes | Policy | No | Yes | Void reason + actor | **Yes** | Target | Void/reversal |
| Sale return / refund | POS return | BranchOp | `ProcessReturn` | Origin branch | Exact | No | Yes | Policy | No | Yes | Actor on record | **Yes** | Target | Void/reversal |
| Open shift | POS shift | BranchOp | Cashier+ | Org scope today | Exact | No | Org-level shift | Device branch | Register active | Yes | Actor on record | No | No | Update |
| Close shift | POS shift | BranchOp | Cashier own / Mgr | Org scope today | Exact | No | — | — | Open shift | Yes | Actor on record | No | No | Update |
| Shift cash movements | POS shift | BranchOp | Cashier+ | Open shift | Exact | No | — | — | Open shift | Yes | Actor on record | Yes | Target | Immutable ledger |
| Stock count session | POS inventory overlay | BranchOp | `ManageInventory` | Target branch | Exact | No | Yes | No | No | Yes | Actor on record | No | No | Update |
| Stock adjustment | POS inventory overlay | BranchOp | `ManageInventory` | Target branch | Exact | No | Yes | No | No | Yes | Movement + actor | **Yes** | Target | Immutable movement |
| Stock transfer create | POS transfer | BranchOp | `ManageInventory` | From branch | Exact | No | Yes (from) | No | No | Yes | Actor on record | No | No | Update |
| Transfer dispatch | POS transfer | BranchOp | `ManageInventory` | From branch | Exact | No | Yes (from) | No | No | Yes | Actor on record | No | No | Update |
| Transfer receive | POS transfer | BranchOp | `ManageInventory` | To branch | Exact | No | Yes (to) | No | No | Yes | Actor on record | No | No | Update |
| Receive stock / purchasing | POS inventory | BranchOp | `ManagePurchasing` | Target branch | Exact | No | Yes | No | No | Yes | Actor on record | No | No | Update |
| Customer order status transition | POS customer order | BranchOp | `ManageCustomerOrders` | Fulfillment branch | Exact | No | Yes (fulfillment) | No | No | Yes | Order history + actor | Optional | Target | Status transition |
| View sales/shifts/inventory reports | POS operational | BranchOp | `ViewReports` / dashboard | Filter branch | Any | Yes | Filter | No | No | No | Read | No | No | Read |

### Workspace & session

| Capability | Data owner | Scope | Required role | Branch access | Mobile | Web | Exact branch | Device match | Shift | Actor | Audit | Reason | Step-up | Lifecycle |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Select workspace (org+branch) | Session | — | Active membership | Resolver | Any | Yes | Sets context | No | Blocks if open shift switching away | Yes | Session | No | No | Update |
| Switch organization (Web) | Platform session | OrgGov | Membership | — | No | Yes | No | No | No | Yes | Platform | No | No | Update |

---

## Critical action classification (target)

When POS/org step-up infrastructure is extended beyond Platform lifecycle, classify at minimum:

| Class | Actions | Requirements |
|---|---|---|
| **Critical — financial** | Void sale, sale return, large adjustment, shift cancel with activity | Reason + actor + audit + password step-up |
| **Critical — governance** | Staff remove, ownership transfer, branch archive, primary change, device revoke | Reason + actor + Platform audit + password step-up |
| **Critical — compliance** | Owner education ack, tax capability transitions | Reason/ack text + actor + immutable audit |
| **Standard mutation** | Catalog edit, hours edit, transfer dispatch | Actor on record; reason optional unless domain requires |
| **Immutable operational** | Posted sale, stock movement, repayment | No delete; void/reversal paths only |

**Current implementation notes (honest baseline):**

- POS APIs consistently require `ActorId` via `PosOrganizationScope`.
- **WP15D:** Customer order fulfillment handoffs, stock count draft create, and payment provider finalization now persist actor/system provenance on authoritative records — see [P28-WP15D](../reports/P28-WP15D-operational-actor-traceability.md).
- Void/expense/reversal domains include reason fields in persistence where applicable.
- **Password step-up** is implemented for critical **Platform governance** mutations (server-issued scoped grants bound to user + org + action + target + expiry) covering:
  - branch suspend / archive / reactivate
  - membership suspend / revoke / role-change
  - POS device revoke
- POS void/refund/stock adjustment password step-up remains not generalized beyond Platform lifecycle actions.
- **P28-WP15E:** Platform governance mutations emit append-only `platform.audit_records` events; Organization Web exposes full filtered/paged audit; MAUI Manage business shows a compact recent-activity summary (5–15 rows) with “View full audit on Web”. Separate from POS operational actor-on-record ([WP15D](../reports/P28-WP15D-operational-actor-traceability.md)).

---

## Client boundary summary

| Surface | OrganizationGovernance | BranchConfiguration | BranchOperation (POS) |
|---|---|---|---|
| **MAUI — Primary workspace** | **Manage business** hub (burger) + recent governance activity summary + Web reminder | Branch list/create under Manage business → Branches | Enter POS when role+device allow |
| **MAUI — non-primary workspace** | Hidden — no Manage business / org-wide nav | **Branch settings** → local configure only | Enter POS at selected branch if device matches |
| **Organization Web** | Full management center | All branches | **No checkout** — read operational history only |
| **Platform Admin Web** | Platform scope only | Org branches via Platform APIs | No POS |

---

## Known gaps (documented, not hidden)

| Gap | Status | Follow-up |
|---|---|---|
| MAUI org governance visible regardless of Primary branch | **Implemented (WP15B)** — `IWorkspaceGovernanceGate`, burger Manage business, hub at `/manage-business` | — |
| Staff↔branch ACL | **Implemented (WP15C)** — `organization_membership_branch_assignments`, `IOrganizationBranchAccessService` | See [P28-WP15C](../reports/P28-WP15C-staff-branch-authorization.md) |
| POS password step-up for void/adjustment | Not generalized | Extend when auth infrastructure approved |
| Shifts/registers org-scoped (no `BranchId`) | Documented WP13 limitation | Future branch-scoped shift model if approved |
| Org Web branch selection | Sets org context; branch is hierarchy UX | Parity with MAUI `SelectedBranchId` on Web optional |

---

## References

- Workspace selection: [P28-WP14](../reports/P28-WP14-unified-organization-branch-workspace-selection.md)
- Branch operational context: [P28-WP13](../reports/P28-WP13-branch-operational-context-and-owner-switching.md)
- Branch ownership: [P28-WP12](../reports/P28-WP12-multi-branch-customer-commerce-hardening.md)
- Fulfillment readiness: [P28-WP11](../reports/P28-WP11-organization-setup-and-branch-fulfillment-readiness.md)
