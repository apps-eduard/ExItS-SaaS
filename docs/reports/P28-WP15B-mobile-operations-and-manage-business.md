# P28-WP15B — Mobile Operational Shell + Main Manage Business Hub

[Phase 28](../phases/phase-28-customer-ordering-pickup-and-delivery.md) | [WP15A baseline](P28-WP15A-capability-client-boundary-baseline.md) | [Capability matrix](../engineering/organization-branch-capability-matrix.md) | [WP14 workspace selection](P28-WP14-unified-organization-branch-workspace-selection.md)

| Field | Value |
|---|---|
| Status | **Code Complete / Build Verified** |
| Starting SHA | `40e4fe2c2b2d26e96ac0feda8723194362df946e` |
| Feature commit | `36647761` |
| Docs commit | `0092ca91` |
| Final SHA | `0092ca9198694edf60da305622bb3ec73ae37835` |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |

## Goal

Clean Mobile operational navigation: separate branch operations from organization governance. Primary/Main Owner/Administrator sees one burger entry **Manage business** → dedicated hub. Non-primary workspaces stay branch-focused. Web governance unchanged.

## Delivered

### Governance gate (Application)

- `IWorkspaceGovernanceGate` / `WorkspaceGovernanceGate` — lazy, branch-keyed cache; `CanAccessManageBusinessAsync()` = Owner/Administrator **and** Primary selected branch.

### Mobile pages

| Route | Purpose |
|---|---|
| `/manage-business` | Organization governance hub (summary + nav rows); gated to Primary + Owner/Admin |
| `/branch-settings` | Local selected-branch configuration gateway |

### Navigation changes (MAUI)

| Surface | Change |
|---|---|
| Burger (`ShellAccountMenu`) | **Manage business** when gate allows; lazy-loaded on menu open |
| `OrgSummary` | Removed Owner `/org/*` essentials nav; **Branch settings** only |
| `MoreHub` | Removed global business section (branches/tax/sales doc); added **Branch settings** |
| `OwnerDashboard` | Removed direct Staff link |
| `Branches` | Back → `/manage-business` (not `/more`) |
| `BranchEdit` | `?return=branch-settings` back link; removed org-wide staff row from setup |

### Density / i18n

- Compact 2.75rem nav rows, 8–12px spacing in `app.css` (`.pos-manage-business__*`, `.pos-branch-settings__*`)
- EN + fil-PH keys for Manage business and Branch settings

### Performance

- Burger does not preload governance datasets until opened
- Manage Business loads branch count + subscription summary only; child pages load their own data

### Web

- No `/manage-business` route; centralized org nav unchanged

## Explicit exclusions

- Staff↔branch ACL (**WP15C** — [report](P28-WP15C-staff-branch-authorization.md))
- POS password step-up for governance mutations
- Live governance activity feed (placeholder only)
- Device/browser validation

## Build / test evidence

| Target | Result |
|---|---|
| Release build — Application | **Succeeded** (0 errors) |
| Release build — Web | **Succeeded** (0 errors) |
| MAUI guard tests (WP15B filter) | **23 passed**, 0 failed |

Tests: `ManageBusinessUiGuardTests` (6), updated `OrgSummaryUiGuardTests`, `MoreHubUiGuardTests`, `OwnerDashboardUiGuardTests`, `BranchFulfillmentUiGuardTests`, `BranchOperationalContextGuardTests`, `ShellContextIdentityGuardTests`.

## Security limitations

- UI hiding is not authorization; APIs remain authoritative
- Gate uses Platform membership Owner/Administrator probe + Primary branch comparison

## Portfolio independence

- PinoyBusinessPOS MAUI/Application only; no cross-product DB or nested foreign product trees

## Docs updated

- [organization-branch-capability-matrix.md](../engineering/organization-branch-capability-matrix.md) — WP15B gap closed
- [client-experience-boundaries.md](../architecture/client-experience-boundaries.md) — Mobile nav tree
- [organization-branches-and-fulfillment-locations.md](../engineering/organization-branches-and-fulfillment-locations.md) — Mobile branch vs governance paths
- [P28-WP14 report](P28-WP14-unified-organization-branch-workspace-selection.md) — cross-ref
- [P28-WP15A report](P28-WP15A-capability-client-boundary-baseline.md) — gap status
- [phase-28](../phases/phase-28-customer-ordering-pickup-and-delivery.md) — WP15B row

## Next work package

Queue continues per Phase 28 plan after WP15B validation sign-off.
