# Organization Branches and Fulfillment Locations

An `OrganizationBranch` is an organization-owned operating location. It remains in the Platform database and is referenced by product operational records only by identifier; products must not query the Platform database directly.

## Location model

- Structured postal address: two address lines, city/municipality, region, postal code, and country code.
- Optional WGS84 latitude and longitude.
- `PickupEnabled` indicates operator intent to offer customer pickup (requires `CustomerOrderingEnabled`).
- `DeliveryEnabled` indicates operator intent to offer local delivery (default **off**; requires readiness + explicit enablement).
- `CustomerOrderingEnabled` is opt-in online ordering for the branch.
- `OnlineOrdersPaused` is a merchant override that blocks new online orders without affecting walk-in POS or in-flight orders.
- Branch operating hours (Mon–Sun) and optional branch timezone override support server-authoritative open/closed evaluation.

Effective pickup requires Active branch, customer ordering enabled, readiness, and operational hours (when configured).

Effective delivery requires Active branch, customer ordering enabled, delivery enabled, readiness (address, hours, phone, coordinates, complete delivery policy, delivery entitlement), and operational hours.

Coordinates identify the fulfillment origin. They are not a customer address and must not be inferred from free-form address text.

## Fulfillment readiness (P28-WP11)

Server evaluator separates entitlement (`CanUse*`), merchant intent (`*Enabled`), setup completeness (`*Ready`), and live operability (`*Operational`). Enablement APIs reject incomplete setup. See [P28-WP11 report](../reports/P28-WP11-organization-setup-and-branch-fulfillment-readiness.md).

## Ownership (P28-WP12)

Organization owns master/shared data. Branch owns operational state. Creating a second branch never clones catalog, customers, inventory, staff, or devices.

| Organization-owned | Branch-owned |
|---|---|
| Customers / Personal↔organization relationship | Inventory on-hand / reserved overlay |
| Catalog products, categories, UOM, SKU/barcode, images | Lots/expiry location |
| Current organization selling price | Stock movements and transfers |
| Subscription / entitlements | Store hours, location, pickup/delivery, readiness |
| Supplier/connected relationships (where currently org-owned) | POS devices, shifts/cash drawers, transaction origin |

Subscription upgrade only unlocks `MaxBranches` capacity. It does not create branches or enable fulfillment. Downgrade retains existing branches and history; new creates that would exceed the limit are blocked.

New branch inventory starts zero/unallocated to the primary. Stock arrives only through opening stock, receipt, transfer, or other existing movements. Storefront availability uses the selected fulfillment branch (`fulfillmentBranchId`). Customer orders reserve/consume that branch overlay; sales use `X-Pos-Branch-Id` for overlay mutation **and** persist `Sale.BranchId` on new checkouts (nullable; historical rows remain null — see [P28-WP13](../reports/P28-WP13-branch-operational-context-and-owner-switching.md)). `CustomerOrder` keeps `SellerOrganizationId` + `FulfillmentBranchId`.

Staff are not auto-assigned to a new branch. Devices stay bound to the registration branch. Main readiness never satisfies Branch B.

Owner/Administrator may select any Active organization branch as **management context** (`SelectedBranchId`) without rebinding the POS device and without gaining `CreateSale` / `EnterPos`. Enter POS on another branch requires a device registered for that branch. An open cashier shift blocks switching the selected operational branch.

**Workspace selection (P28-WP14):** one `/workspace-select` flow chooses Organization + Branch together via `SelectWorkspaceAsync`. Burger menu **Switch workspace** is the only switch entry point; the topbar shows org + branch display-only. See [P28-WP14](../reports/P28-WP14-unified-organization-branch-workspace-selection.md).

**Capability boundaries (P28-WP15A):** organization governance vs branch configuration vs branch operations, Mobile Primary vs exact-branch exposure, audit/step-up baseline — [organization-branch-capability-matrix.md](organization-branch-capability-matrix.md).

See [P28-WP12 report](../reports/P28-WP12-multi-branch-customer-commerce-hardening.md) and [P28-WP13 branch operational context](../reports/P28-WP13-branch-operational-context-and-owner-switching.md).

## Management surfaces

MAUI provides a dense branch list (tappable cards) and a progressive editor: compact setup/readiness rows, then expandable Details, Address & location, Operating hours, and Fulfillment. Delivery pricing is disclosed when configuring delivery. Organization Web uses the same hierarchy with a wider two-column form grid and a sticky Save bar. See [P28 branch-edit UX densification](../reports/P28-branch-edit-ux-densification.md).

**Mobile navigation (P28-WP15B):**

- Global branch list/create/edit for owners: **Manage business → Branches** (`/manage-business` → `/organization/branches`), Primary/Main workspace + Owner/Admin gate only.
- Local branch configuration at any workspace: **Branch settings** (`/branch-settings`) → branch editor with `?return=branch-settings`.
- Operational More/Org summary must not expose global branch management or org-wide governance clutter.

Branch capacity remains entitlement-controlled. Primary branches cannot be treated as disposable, and archived branches cannot fulfill new orders.

## Boundaries

Customer ordering (Phase 28 Stage B) consumes branch fulfillment capabilities and delivery policy. Personal linked-merchant storefront/cart UX is delivered for authenticated active links (`CustomerOrder`); courier marketplace, routing, and customer-order payment rails remain separate residuals. POS remains free of PHI and no cross-product database access or foreign keys are introduced.
