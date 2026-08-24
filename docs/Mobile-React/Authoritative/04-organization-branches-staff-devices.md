# Organization, Branches, Staff, and Devices

## Organization profile and ownership

| Concern | Authority | Status | Evidence |
|---------|-----------|--------|----------|
| Organization aggregate | Platform | PROVEN_CURRENT | `organizations`, `public_organization_id` (`ORG######`) |
| Business QR / public org identity | Platform | PROVEN_CURRENT | public identity APIs + MAUI |
| Ownership | Platform membership Owner | PROVEN_CURRENT | Start a Business; ownership transfer migrations |
| Ownership transfer | Platform | PROVEN_CURRENT | `AddOrganizationOwnershipTransfers` + use cases |
| Compliance profile | Platform (org) | PROVEN_CURRENT | Phase 26 foundation; transfer preserves profile |
| Multi-org ownership | Same Personal identity, multiple Owner memberships | PROVEN_CURRENT | P25 |

## Staff

See [02-identity-personal-organization-lifecycle.md](02-identity-personal-organization-lifecycle.md) for the **entity-level** CURRENT vs OWNER-CONFIRMED staff identity model.

| Topic | CURRENT |
|-------|---------|
| Staff principal | Separate org-scoped `PlatformUser` + real login `local@ORG######` |
| Personal accept invite | Authenticated Personal creates **new** staff principal + `LinkedPersonalUserId` |
| No Personal | Anonymous accept unchanged; link absent |
| Person-link | Correlation only; not authorization |
| Marker | `ORGANIZATION_STAFF_EXISTING_PERSON_LINK_CONTRACT_MISSING` **RESOLVED** (RMAP-B00) |
| Backend | PROVEN_CURRENT after RMAP-B00 |

MAUI surfaces: `/org/staff`, `/org/staff/invite`, `/org/staff/assign` (implement CURRENT model).

React surfaces (Manage staff): `/org/staff`, `/org/staff/invite`, `/org/staff/assign` — product-local Owner/Manager/Cashier via Platform `product-local-roles`; invite/suspend/remove membership. Gate: Organization Owner (`RequireInviteStaff`). Does **not** include Org Web custom role catalogs or MAUI `/permissions` hub (POS DB assignments). See [POS-REACT-MANAGE-STAFF-AND-ADMIN-UX-01.md](../Reports/POS-REACT-MANAGE-STAFF-AND-ADMIN-UX-01.md).

| Topic | React status |
|-------|--------------|
| Staff list + invite | PROVEN_CURRENT (`OrgStaffPage`, `OrgStaffInvitePage`) |
| Assign/revoke product-local POS roles | PROVEN_CURRENT (`OrgStaffAssignPage` + `product-local-roles-client`) |
| Suspend / remove membership | PROVEN_CURRENT |
| Branch assignments per member | MISSING (MAUI only) |
| POS `/permissions` hub | MISSING (deferred; not mobile essentials) |

## Branches

**Branch ≠ Register ≠ Device.**

| Attribute | Current | Status | Evidence |
|-----------|---------|--------|----------|
| Branch identity | `OrganizationBranch` (Platform) | PROVEN_CURRENT | Domain + Branch APIs |
| Main branch | `CreateMainBranch` on Start a Business; `EnsureMainBranchExists` | PROVEN_CURRENT | `StartBusinessUseCases` |
| Org ownership | Branch belongs to organization | PROVEN_CURRENT | |
| Address | Update branch DTO/UI | PROVEN_CURRENT | MAUI `BranchEdit.razor` |
| Coordinates | Lat/lng on branch | PROVEN_CURRENT | BranchEdit + delivery calculator |
| Phone | Branch contact fields | PROVEN_CURRENT | Branch update |
| Operating hours | `GET/PUT .../operating-hours` | PROVEN_CURRENT | `BranchAndDeviceEndpoints.cs` |
| Activation/status | Branch lifecycle + suspension-related governance | PROVEN_CURRENT / PARTIAL by feature | migrations include branch suspension |
| POS operational branch | POS session `BranchId`; `PUT /api/v1/pos/operational-branch` | PROVEN_CURRENT | |
| Multi-branch inventory | Branch balances / transfers | PROVEN_CURRENT | POS advanced inventory |

API root: `/api/v1/platform/organizations/{organizationId}/branches`

## Fulfillment and delivery configuration

Dependency chain (owner-confirmed and current):

```text
Organization
  → Branch
    → Location + Hours + Fulfillment configuration
      → Pickup/Delivery readiness
        → Customer Ordering
```

| Capability | Status | Evidence |
|------------|--------|----------|
| Pickup enabled | PROVEN_CURRENT | fulfillment-settings |
| Delivery enabled | PROVEN_CURRENT | fulfillment-settings / delivery-policy |
| Fulfillment readiness | PROVEN_CURRENT | `.../fulfillment-readiness` |
| Service radius / distance | PROVEN_CURRENT | Platform `HaversineDeliveryDistanceCalculator`; POS `StraightLineDeliveryDistance` |
| Delivery fee preview | PROVEN_CURRENT | `.../delivery-fee-preview` |
| Entitlement/feature gates | PROVEN_PARTIAL | capacity/feature codes interact with readiness — verify per feature code in Platform entitlements |
| MAUI configuration | PROVEN_CURRENT | `BranchEdit.razor` fulfillment panel |
| React configuration | MISSING | no branch admin UI |

Note: Platform and POS Haversine Earth-radius constants differ slightly (`6371.0088` vs `6371.0`). Functionally aligned; treat as audit note, not a React blocker.

## Devices

| Concern | Authority | Status |
|---------|-----------|--------|
| Registered POS device | Platform `pos-devices` | PROVEN_CURRENT |
| Active-only customer list | `GET .../pos-devices` → Active only | PROVEN_CURRENT |
| Soft revoke + history | Status Revoked retained; `GET .../pos-devices/history` | PROVEN_CURRENT |
| Registration token / recovery | Platform (MAUI customer UX still uses create/redeem) | PROVEN_CURRENT |
| React direct register | `POST .../pos-devices/register` — no React registration-code UX | PROVEN_CURRENT |
| Login ≠ device slot | Auth does not register or consume capacity | PROVEN_CURRENT |
| POS sales execution gate | Platform authorize + POS `IPosDeviceTransactionAuthorizer` | PROVEN_CURRENT |
| Device on POS session | POS session carries `PosDeviceId` | PROVEN_CURRENT |
| Lost/revoked device | Platform revoke + POS authorization fail-closed | PROVEN_CURRENT |
| Offline grant / PIN binding | Device-bound offline operating grant | PROVEN_CURRENT (MAUI LocalStore) |
| Browser/PWA durable install id + register/authorize | Platform + React | PROVEN_CURRENT (RMAP-10b + simplification) |

MAUI: `/devices/register` (redeem), `/organization/devices` (create code) — compatibility retained.
React: `/devices/register` (Register this device), `/org/devices` (active-only management).

## Registers and shifts (POS)

Documented in [POS/registers-devices-and-shifts.md](POS/registers-devices-and-shifts.md). Registers are POS stations, not branches or devices.

## Notifications

Unified organization notifications foundations exist (P25). Status: **PROVEN_CURRENT** in Platform/POS notification surfaces used by MAUI; **MISSING** in React.

## Organization settings relevant to sales documents

Compliance / sales-document capability is organization-scoped on Platform. Sale engine remains one POS Sale engine; current issued document kind is Transaction Summary (see sales-documents POS doc). TaxDocument issuance remains unavailable unless source changes.

## React implications

Organization/branch/device context parity is a foundation package **before** sell-floor checkout parity. React binds workspace/branch for sell routes (`WorkspaceProvider`, `NoAccessibleBranchPage`) and, as of RMAP-10b, registers/authorizes browser PosDevices without inventing terminals. Branch admin configuration UI remains MISSING.
