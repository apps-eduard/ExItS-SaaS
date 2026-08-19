# P28-WP15C — Staff-to-Branch Authorization and Accessible Workspace Enforcement

[Phase 28](../phases/phase-28-customer-ordering-pickup-and-delivery.md) | [WP15A baseline](P28-WP15A-capability-client-boundary-baseline.md) | [WP15B Manage business](P28-WP15B-mobile-operations-and-manage-business.md) | [WP14 workspace selection](P28-WP14-unified-organization-branch-workspace-selection.md) | [Capability matrix](../engineering/organization-branch-capability-matrix.md)

| Field | Value |
| --- | --- |
| Status | **Code Complete / Validation Pending** |
| Depends on | WP15A + WP15B on `origin/main` |
| Closes | WP14 staff↔branch ACL gap |

## Delivered capability

- **Platform model:** `organization_membership_branch_assignments` with unique `(membership_id, branch_id)`.
- **Central resolver:** `IOrganizationBranchAccessService` / `OrganizationBranchAccessService`.
- **Governing roles:** `OrganizationOwner` and `OrganizationAdministrator` inherit all Active branches (no assignment rows).
- **Staff:** `OrganizationMember` requires explicit assignments; at least one Active branch required.
- **Server enforcement:** `ListBranches`, `SelectOrganizationBranchContext`, membership branch-assignment APIs.
- **New staff:** primary Active branch assigned on membership create / invite accept (`AssignPrimaryBranchForNewStaffAsync`).
- **New branches:** no auto-assign for staff after migration.
- **POS client:** branch assignment read/write via Platform API; workspace chooser uses filtered `GetBranchesAsync`.
- **Session restore:** clears `SelectedBranchId` when branch access was revoked server-side.
- **Manage business UX:** MAUI compact chips + bottom sheet; Web staff detail branch matrix.
- **i18n:** EN + fil-PH strings for branch assignment UI.

## Migration / compatibility

**Backfill (one-time):** active `OrganizationMember` memberships receive rows for every **current** Active branch in their organization (`actor_reference = migration:p28-wp15c-backfill`).

**After migration:**

| Actor / event | Branch assignment behavior |
| --- | --- |
| Existing staff (backfill) | All current Active branches |
| New staff invite / membership | Primary Active branch only |
| New branch created | Owner/Admin see automatically; staff **not** auto-assigned |
| Owner / Administrator | No rows; organization-wide access |

This preserves prior effective access for existing staff on existing branches without inventing historical activity attribution.

## Authorization model

```
OrganizationMember + assignment rows → listed branches only
OrganizationOwner / OrganizationAdministrator → all Active branches (null = all in resolver)
POS product role / device binding → unchanged; branch assignment ≠ CreateSale
```

Foreign organization or unassigned branch → `application.branch.access_denied` (403) on branch context selection; filtered out of branch list.

## Explicit exclusions

- Password step-up for assignment changes (future governance package).
- Full audit actor pipeline completion (uses `MembershipBranchAssignmentsUpdated` action + `actor_reference` on rows only).
- POS product-local role changes from branch assignment UI.

## Tests (Release)

| Suite | Filter / scope | Result |
| --- | --- | --- |
| `ExItS.Platform.UnitTests` | `OrganizationBranchAccessServiceTests`, `SelectOrganizationBranchContextTests` | **8 passed** |
| `ExItS.PinoyBusinessPOS.Maui.Tests` | `StaffBranchAuthorizationGuardTests`, `WorkspaceSelectionServiceTests` | **8 passed** |

Guard tests cover MAUI staff branch UI, Web staff detail matrix, session restore revocation path, and resolver wiring.

## Readiness

Server-authoritative staff branch scoping is implemented for Platform list/select and POS workspace flows. Production validation still required for migration apply/rollback on PostgreSQL and end-to-end staff management UX on device/browser.
