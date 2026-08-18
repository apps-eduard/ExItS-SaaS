# P28-WP12 — Multi-Branch Customer Commerce Hardening

| Field | Value |
|---|---|
| Status | **Code Complete / Validation Pending** |
| Starting SHA | `66ff06e0ce8fbb891440a0a7a7363db6df11492a` |
| Feature commit | `69111d45` |
| Migration | **No migration** |
| Device Verified | **No** |
| Browser Verified | **No** |
| Production Ready | **No** |

## Goal

Make 1-branch → multi-branch subscription upgrade safe: unlock capacity only. Organization owns master data; branch owns operational state.

## Ownership matrix

| Organization-owned | Branch-owned |
|---|---|
| Customers / Personal↔organization relationship | Inventory on-hand overlay (`InventoryBranchBalance`) |
| Catalog products, categories, UOM, SKU/barcode, images | Lots/expiry (optional `InventoryLot.BranchId`) |
| Current organization selling price | Stock movements, transfers, opening stock |
| Subscription / entitlements | Store hours, location, pickup/delivery, WP11 readiness |
| Supplier/connected relationships where currently org-owned | POS devices, shifts/cash drawers, order/sale origin |

No branch copies of org-owned entities. No branch price overrides in this WP.

## Subscription upgrade

- `CreateBranch` already enforces `MaxBranches`.
- `UpgradeOrganizationSubscription` does **not** create branches, copy products/customers/inventory, assign staff/devices, or enable pickup/delivery.
- Upgrade only increases allowed active branch count.
- Downgrade retains existing branches and history; additional creates that would exceed the new limit are blocked (`PlanChangeImpact`). No automatic archive/delete.

## New branch initialization

Create Branch B:

- Same organization customers and `CatalogProduct` ids immediately.
- Inventory overlay starts **zero** for non-primary (unallocated org stock stays on primary). Main 100 stays 100.
- Stock arrives only through opening stock, receipt, transfer, or other existing movements.
- Staff are **not** auto-assigned. Devices are **not** cloned. Pickup/delivery/customer ordering stay **off**.
- WP11 readiness is evaluated per branch; Main ready does not make Branch B ready.

## Customer relationship

`CustomerLinkRequest` / `LinkedCustomerAppUser` / `BusinessCustomer` are organization-scoped. Invite from Main or Branch B yields Paul ↔ Organization. No Paul-Main / Paul-BranchA rows. Authorization for customer orders remains the active Personal↔seller organization relationship. Staff visibility is unchanged (role + device/session branch); this WP does not invent a new staff↔branch ACL.

## Inventory isolation

`BranchStockResolver`:

- Explicit branch row → that on-hand.
- Missing row: unallocated = org on-hand − other branch rows; unallocated belongs to **primary** only. Non-primary missing row = 0.

Customer-order place/reserve and POS sale checkout (`X-Pos-Branch-Id`) use this resolver. Storefront availability uses `fulfillmentBranchId` (auto-select single eligible or primary when omitted). Aggregate org stock is not presented as Branch B availability.

Walk-in `Sale` has **no** `BranchId` column (no schema change). Overlay mutation uses the session branch header. Electronic payment webhooks without a branch header still consume org reservation; overlay was taken at awaiting-payment reserve when the checkout/create-attempt header was present.

## Transaction attribution

- `CustomerOrder.SellerOrganizationId` + `FulfillmentBranchId` retained.
- Existing Main orders are unchanged when Branch B is created.
- Historical sales are not rewritten.

## UX

Dense MAUI + Organization Web branch setup rows: details, staff (not auto-assigned), devices, inventory (no stock copied + opening stock / transfer CTAs), organization catalog, organization customers, per-branch fulfillment chips. Create-branch copy states catalog/customers stay organization-wide. Personal shop uses fulfillment-branch stock when multiple branches exist.

EN + fil-PH strings added. `CreateBranchRequest.PickupEnabled` default is **false** so omitted JSON does not enable pickup.

## Tests (Release)

| Suite | Filter | Result |
|---|---|---|
| Platform unit | `OrganizationBranchAndPosDeviceTests` + `BranchFulfillment*` | **20 passed** |
| POS unit | `CustomerOrdering` + `BranchStockResolver` + `InventoryTransfer` | **75 passed** |
| MAUI | `BranchFulfillmentUiGuardTests` | **2 passed** |
| Organization Web | `OrgWebAuthErrorAndBranchesGuardTests` | **5 passed** |

## Explicit exclusions

- No `Sale.BranchId` migration.
- No staff↔branch ACL table.
- No branch product clones or regional pricing.
- No automatic inventory row per product on branch create.
- Device / browser / production verification not claimed.

## Next

P28-WP10 E2E validation and Phase 28 closeout.
