# P28-WP14 — Unified Organization + Branch Workspace Selection

[Phase 28](../phases/phase-28-customer-ordering-pickup-and-delivery.md) | [WP13](P28-WP13-branch-operational-context-and-owner-switching.md) | [Branch architecture](../engineering/organization-branches-and-fulfillment-locations.md) | [Portfolio](../portfolio-progress.md)

| Field | Value |
|---|---|
| Status | **Code Complete / Validation Pending** |
| Starting SHA | `360f8b01b3fb38ea72abde8026fb186346a94fb3` |
| Feature commit | `f5c4b2fb` |
| Docs commit | `50afc613` |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |

## Goal

Consolidate WP13 organization/branch switching into **one canonical workspace flow**:

**Workspace = Organization + selected management Branch.**

Replace scattered topbar branch switching with `/workspace-select`, login routing through the same logic, and burger menu **Switch workspace**. Preserve WP13 device binding, shift guard, POS vs management separation, and sale/stock branch validation.

## Canonical hierarchy

```
User
└ Organization
   └ Branch (management SelectedBranchId)
      └ Operational POS context (device BranchId / PosDeviceId)
```

| Layer | Meaning |
|---|---|
| Organization context | Which business? |
| Selected management branch | Which location inside that business for management UI and `X-Pos-Branch-Id` |
| POS operational authorization | Can this identity/device **sell** in the selected branch? (separate) |

## Login routing matrix

Resolved by `WorkspaceSelectionService.ResolveRoutingPlanAsync` after authentication:

| Case | Condition | Outcome |
|---|---|---|
| A | 1 accessible org + 1 accessible Active branch | `AutoSelect` → `SelectWorkspaceAsync` → home |
| B | 1 org + 2+ accessible Active branches | `/workspace-select` |
| C | 2+ accessible orgs | `/workspace-select` |
| D | Org(s) but no accessible Active branch | Chooser empty state / setup messaging — no invented branch |
| Personal | 0 eligible orgs | Personal home |

Sign-in does **not** bind organization under the login spinner. Auto-select runs on `/workspace-select` or from Sign-in when plan is `AutoSelect`.

Legacy `/organization-select` redirects to `/workspace-select`.

## SelectWorkspace orchestration

`IAuthenticationService.SelectWorkspaceAsync(organizationId, branchId)`:

1. Different org → `SelectOrganizationAsync` then `SelectBranchAsync`
2. Same org → `SelectBranchAsync` only
3. Server validates membership, org context, branch Active, open-shift guard, foreign-org rejection
4. Persists `OrganizationId` + `SelectedBranchId`, refreshes entitlements; never optimistic UI before server success
5. Clears selling mode; cart discard confirm when non-empty

## UI entry points (one path)

| Surface | Behavior |
|---|---|
| Topbar | Display-only org name + branch subtitle — **not** a switch control |
| Burger menu | **Switch workspace** → `/workspace-select` |
| Sign-in / restore | Routing plan → auto-select or chooser |
| Org summary | Current branch row links to workspace select |
| Org Web | `/workspace-select` accordion; legacy `/select-organization` redirects |

**Removed:** `ShellBranchSwitcher.razor` (topbar bottom sheet).

## Management vs POS

Workspace selection = viewing/managing org + branch. **Enter POS** still requires:

- POS entitlement / `CreateSale`
- Selected branch
- Device registered for selected branch
- Register/shift rules
- Active device state

Device mismatch shows compact blocked copy; no silent rebind.

## State invalidation

| Scope | On org change | On branch-only change |
|---|---|---|
| Identity / auth / user prefs | Keep | Keep |
| Org profile, subscription, entitlements, roles | Refresh | Keep |
| Branch inventory, orders, dashboard filters, register context | Refresh | Invalidate/reload |
| Cart | Confirm discard on switch | Confirm discard on switch |

## Staff ACL limitation

`IAccessibleBranchResolver` abstracts accessible branches. **Only `OwnerAccessibleBranchResolver` is implemented** (Active org branches for access-allowed memberships). Staff↔branch ACL is **not** implemented; future resolver can filter chooser without redesigning navigation.

## Removed / replaced (WP13)

| Retired | Replacement |
|---|---|
| Topbar branch dropdown / `ShellBranchSwitcher` | Display-only subtitle + burger **Switch workspace** |
| Org-select auto-bind page | `/workspace-select` + routing plan |
| Separate org vs branch switch commands | Single **Switch workspace** |
| Duplicate branch list loads in switcher | `WorkspaceSelectionService.ListWorkspacesAsync` |

WP13 operational guards (`SelectBranchAsync`, open shift, device binding, `FromBranchId`/`DeviceBoundBranchId` on operational endpoint) **preserved**.

Capability/client boundaries baseline: [P28-WP15A](../reports/P28-WP15A-capability-client-boundary-baseline.md) | [capability matrix](../engineering/organization-branch-capability-matrix.md).

## Tests (Release, targeted)

| Suite | Filter / scope | Result |
|---|---|---|
| MAUI | `WorkspaceSelectionServiceTests` | **5 passed** |
| MAUI | WP14 guard filter (workspace, branch operational, routing) | **14 passed** |
| MAUI | `SelectBranch_round_trips` + `SelectWorkspace_same_org` | **2 passed** |
| Application + Web | Release build (no Android TFM) | **0 errors** |

Full `ExItS.slnx` Release build fails on this host without Android SDK (MAUI android TFM). Not Device Verified / Browser Verified.

## Explicit exclusions

- No staff↔branch ACL subsystem
- No silent device rebind
- No Org Web POS checkout
- No production-ready claim without device/browser evidence

## Next work package

Device/browser validation of multi-org workspace round-trip and Enter POS device mismatch on physical tablet.
