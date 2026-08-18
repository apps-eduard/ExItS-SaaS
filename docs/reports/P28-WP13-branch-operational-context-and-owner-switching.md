# P28-WP13 — Branch Operational Context and Owner Switching

[Phase 28](../phases/phase-28-customer-ordering-pickup-and-delivery.md) | [Branch architecture](../engineering/organization-branches-and-fulfillment-locations.md) | [WP12](P28-WP12-multi-branch-customer-commerce-hardening.md) | [Portfolio](../portfolio-progress.md)

| Field | Value |
|---|---|
| Status | **Code Complete / Validation Pending** |
| Starting SHA | `506a0750bee9913d439a4ef8773b1f362cdf5554` |
| Feature commit | `ed75c827` |
| Docs commit | *(this report)* |
| Migration | **`20260818223000_AddSaleBranchId`** — nullable `pos.sales.branch_id`; no historical backfill |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |

## Goal

Owner signs in once, sees all organization branches, selects a current branch, manages that branch, and optionally Enter POS. POS sale, inventory, orders, shift, register, and sale attribution use the **same selected branch**.

## Branch context model

Two session fields stay distinct:

| Field | Meaning |
|---|---|
| `AuthSession.BranchId` + `PosDeviceId` | Immutable **device operational binding** (registration branch) |
| `AuthSession.SelectedBranchId` | Owner/operator **current branch** for management and `X-Pos-Branch-Id` |

`AuthSessionBranchContext.GetSelectedBranchId` returns `SelectedBranchId` when set, else device `BranchId`. `PosOrganizationHeaderHandler` sends that value as `X-Pos-Branch-Id`. OrganizationId is never changed by a branch switch.

Secure storage key: `pos.session.selectedBranchId`. Cleared on organization/personal switch and session-key logout. Survives restore / PIN re-entry with the rest of the session.

## Management branch vs POS operational branch

- **Management switch:** Platform `PUT /organizations/{id}/branch-context` validates authenticated org viewer, branch exists, same organization, Active. POS `PUT /api/v1/pos/operational-branch` (capability `ViewCatalog`, not `CreateSale`) applies the open-shift guard.
- **POS operational / money path:** Device remains bound to its registration branch. `AuthorizeForTransactions` receives the selected `BranchId` from the POS authorizer. Device registered to Branch A is denied for Branch B transactions. No silent rebind.

Owner may select Branch B for management while the tablet stays registered to Main. Enter POS on Branch B requires a device registered/authorized for Branch B (`/devices/register`).

## Owner flow

1. Sign in once (no second account).
2. Header: organization name + current branch ▼.
3. Switcher lists org Active branches (inactive shown disabled).
4. Select Branch B → Platform + POS validate → session persists `SelectedBranchId`.
5. Organization screen: Current branch + Switch. Enter POS requires `EnterPos` **and** `CreateSale`. Missing selling role shows a compact `Org_EnterPosRoleRequired` state (no fake permission). Device mismatch shows register-device CTA.

Organization Owner membership sees all Active org branches by default. Owner membership alone still does **not** grant `CreateSale` / `EnterPos`.

## Staff behavior

Branch-scoped staff still use the existing org-viewer + device/session rules. This WP does **not** invent a staff↔branch ACL table. Any member who can view the organization can select any Active branch in that organization for management context. Foreign-org branch IDs return not found. Do not treat this as a finished per-staff assignment model.

## Device rule (V1)

Fixed POS device remains bound to one branch. Management switch is allowed. Enter POS / checkout on another branch requires a device registered for that branch. `AuthorizeForTransactions(expectedBranchId)` is the server gate. Testing hosts still skip device HTTP authorize (existing WebApplicationFactory rule).

## Enter POS

Organization mode → selected branch → selling capability → device matches selected branch → existing register/device requirements → enter that branch’s POS. Compact blocked states when selling role or device branch is missing.

## Shift guard

Cashier shifts remain organization-scoped (no `Shift.BranchId`). An open shift blocks switching **operational** selected branch to a different id (`pos.branch.switch.shift_open` / HTTP 409). Reselecting the same branch is allowed. Close/cancel the open shift first. Management-only UI still goes through this POS guard when the operational client is registered, so an open cashier session cannot silently become Branch B.

## Sale BranchId

New walk-in checkout persists `Sale.BranchId` from the validated `X-Pos-Branch-Id` header (same id used for overlay stock mutation). Checkout also stores OrganizationId, cashier/actor, and existing immutable sale fields. Non-Testing environments require the branch header (`pos.sale.branch_required`). Testing keeps the header optional so existing POS integration suites do not all require a branch.

Stock: `CheckoutSale` passes the header branch into `EnsureAvailableForSale` / `DeductForSale` (WP12 `BranchStockResolver`). Branch B checkout does not consume Main overlay.

### Legacy / backfill

`branch_id` is **nullable**. Historical rows stay null. This WP does **not** invent branch attribution from device history and does **not** backfill single-branch orgs. Unknown provenance is preserved.

## Densified UI

- MAUI header: org title + branch subtitle ▼; bottom sheet, 44px rows, inactive status, EN + fil-PH.
- Org summary: Current branch row + Switch; compact Enter POS blocked copy.
- Sale checkout: `Selling at {branch}`.
- Organization Web: one-line note that walk-in POS branch is selected on mobile. Org Web remains the management center, not checkout.

## Remaining limitations

- Staff↔branch ACL not implemented (org viewers can select any Active org branch).
- Shifts/registers have no BranchId; guard is “any open shift blocks a different selected branch”.
- Devices stay one-branch; no multi-branch mobile device authorization.
- Testing checkout may omit `X-Pos-Branch-Id` (legacy `Sale.BranchId` null).
- Not Device Verified, not Browser Verified, not Production Ready.

## Tests (Release)

| Suite | Filter | Result |
|---|---|---|
| Platform unit | `SelectOrganizationBranchContextTests` + `AuthorizePosDeviceBranchTests` + `OrganizationBranchAndPosDeviceTests` | **13 passed** |
| Platform unit | `BranchFulfillment*` (WP11 regression) | **12 passed** |
| POS unit | `SelectOperationalBranchTests` + `SaleDomainTests` + `BranchStockResolver` + `InventoryTransferUseCaseTests` | **57 passed** |
| MAUI | `BranchOperationalContextGuardTests` + `AuthenticationServiceTests.SelectBranch` + session store + `OrgSummaryUiGuardTests` + `ShellContextIdentityGuardTests` + `SalePageGuardTests` | **22 passed** |
| MAUI | `BranchFulfillmentUiGuardTests` + PIN/device guards | **33 passed** |
| Organization Web | `OrgWebAuthErrorAndBranchesGuardTests` | **5 passed** |

POS sale/inventory **integration** was not used as WP13 proof in this run (catalog create `NotFound` under a concurrent slnx file-lock; WP12 stock isolation remains covered by unit `BranchStockResolver` / `InventoryTransferUseCaseTests`).

## Explicit exclusions

- No new staff↔branch assignment system
- No silent device rebind
- No owner auto-cashier permission
- No historical sale branch backfill
- No Org Web checkout
- No Device / Browser / Production Ready claim
