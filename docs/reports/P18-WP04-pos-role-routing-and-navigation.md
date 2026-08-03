# P18-WP04 — POS Role Routing and Navigation

| Field | Value |
|---|---|
| Status | **Code Complete and Build Verified; Device Validation Pending** |
| Phase | [Phase 18](../phases/phase-18-mobile-personal-organization-and-pos-experience.md) |
| Implementation commit | `4b8b727` |
| Device validation | **Device Validation Blocked** |
| Date | 2026-08-03 |

## 1. Objective

Automatic POS role routing after access is established, with denial paths for missing/inactive role, membership, entitlement, and cross-organization access. Support Start Selling without changing role.

## 2. Scope

NavigationGate + RoleHomeResolver + SellingModeService + access-denied UX. Users must not pick Owner/Manager/Cashier at login.

## 3. Existing functionality reused

- POS `GET /api/v1/pos/permissions/effective`
- Protected shell access policy, sync/reconnect gate
- Operational setup gate for owners/admins (Phase 17)
- AccessDenied page

## 4. Backend / API work completed

- No new POS role endpoints; effective permissions remain authoritative
- Platform product-local role assign/revoke already used for grants that POS middleware maps

## 5. MAUI screens and flows completed

- Landing: Personal (`/personal`) when no org; Org essentials (`/org`) when org without POS access; role home when POS access active
- `/owner`, `/manager`, `/cashier` route by effective role (Owner/Admin → Owner; StoreManager → Manager; Cashier → Cashier)
- No-role / inactive assignment → `/access-denied`
- Start Selling sets selling mode + navigates to `/sales/new`; cancel returns to dashboard without role change
- PosShell home nav resolves to role home; selling-mode banner

## 6. Files / components changed (representative)

- `Application/Auth/RoleRoutingServices.cs`
- `Maui/Services/NavigationGate.cs`
- `Maui/Components/Pages/Dashboards/*`, `Home.razor`, `AccessDenied.razor`, `Layout/PosShell.razor`
- `Maui/Components/Pages/Sales/SaleCheckout.razor` (exit selling mode)

## 7. Authorization and organization-isolation behavior

Effective POS role is API-authoritative. Membership inactive, entitlement denied, revoked role, and cross-org attempts fail closed via Platform/POS authorization and NavigationGate/access-denied paths. Selling mode does not elevate capabilities.

## 8. Tests executed and totals

| Suite | Result |
|---|---|
| RoleHomeResolverTests | Included in MAUI.Tests **73 passed** |
| SellingMode return-route test | Included |

## 9. MAUI build result

**Build Verified**.

## 10. Emulator / device validation result

**Device Validation Blocked**.

## 11. Known limitations

- Denial reason query strings depend on localized keys when supplied
- InventoryStaff / ReportingUser map to access-denied for Phase 18 role homes (not Owner/Manager/Cashier dashboards)

## 12. Deferred items

Dedicated dashboards for non-MVP POS roles; device E2E of denial matrix.

## 13. Current status

Implemented · Tested · Build Verified · Device Validation Blocked

## 14. Commit reference

Implementation: `4b8b727`. Documentation reconciliation: Phase 18 docs tip on `main`.
