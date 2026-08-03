# P16-WP11 — Organization Product Access aligned to entitlement model

> **Status:** In Progress (validation)  
> **Phase:** Phase 16 — Implementation Complete, Under Validation  
> **Work package:** P16-WP11  
> **Related:** `docs/architecture/product-catalog-entitlement-and-role-model.md`

---

## Root cause

Organization Product access UI exposed low-level technical identifiers and mixed Platform Product, entitlement, and Product role concepts.

Symptoms included:

- raw User ID GUID and Product code inputs for staff Product-role assignment
- internal Product key (`pinoy-business-pos`) treated like a user-facing name
- technical reason codes shown to Organization Owners/Staff
- nav **My Products** pointing at commercial Platform grant tooling
- overloaded Role/Status language that blurred Organization Role vs Product Role

---

## Correction

| Concept | UX / API |
|---|---|
| Platform Product | Catalog-owned; display name on cards (`Pinoy Business POS`) |
| Organization Product Entitlement | Shown as Entitlement status on Enabled Products |
| Organization Product Instance | Provisioning status (`Ready` when entitled path is healthy) |
| Organization Product Role Assignment | Manage Staff Access dropdowns; does not create entitlement |

Permanent rule applied in UI and authorization:

> Entitlement enables the Product for the Organization. Product Role authorizes a person inside that Product.

### Organization UX

- **Enabled Products** / **My Products** → discovery cards (deduped by ProductId/ProductKey)
- **Manage Staff Access** → Select Staff Member, Select Product, Select Product Role, Assign Role
- Approved Product roles only: POS Owner, Store Manager, Cashier, Reporting User
- Friendly denial messages (for example role missing)
- Commercial `/product-access` page restricted to Platform `ManageProductAccess` support use

### DTO separation

`EnabledProductDto` now includes `ProductId`, `ProductKey`, `ProductDisplayName`, `EntitlementStatus`, `ProvisioningStatus`, `OrganizationRole`, `ProductRole`, `CanLaunch`, `DenialReasonCode`, `DenialReasonDisplay`.

---

## Tests

Focused unit/admin guards cover uniqueness, display name, role/entitlement separation, cross-org block, friendly denials, absence of GUID/code inputs in normal Org UI, and Platform permission gating for commercial grant tools.

---

## Manual validation (Local Validation)

API validation against Local Validation (Maria Santos / Carlo Reyes / ABC):

1. Enabled Products returns **Pinoy Business POS** once with `productDisplayName` (not the internal key as title)
2. `entitlementStatus=Enabled`, `provisioningStatus=Ready`, separated `organizationRole` vs `productRole`
3. Without Product role: friendly denial `You do not have a role assigned for this Product.`
4. Maria assigned **POS Owner**; Carlo assigned **Cashier** via product-local-roles (staff dropdown path in UI)
5. Carlo My Products / launch succeeds (`canLaunch=true`)
6. After revoke: entitlement remains **Enabled**; Carlo `canLaunch=false`; launch 403 detail uses the friendly message
7. Carlo Cashier role restored for continued LV use
8. Organization Role remains Staff for Carlo; Product Role is Cashier (separate)

---

## Status

- Phase 16 — Implementation Complete, Under Validation
- P16-WP11 — In Progress
- P16-WP12 — Not Started
