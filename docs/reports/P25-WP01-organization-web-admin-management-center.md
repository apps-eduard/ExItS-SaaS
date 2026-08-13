# P25-WP01 — Organization Web Admin Management Center

## 1. Assignment

| Field | Value |
|---|---|
| Phase | 25 |
| Work package | P25-WP01 Organization Web Admin Management Center |
| Status | Code Complete / Ready for Owner Validation |
| Branch | `main` |
| Date | 2026-08-13 |
| Device Verified | **No** |
| Production Ready | **No** |

## 2. Summary

Organization Web Admin is a new DesignSystem Blazor Server app (`ExItS.PinoyBusinessPOS.Web`) for **management / control / reporting**.

**Organization Web Admin is not a POS checkout client.**

Operational selling remains in POS/MAUI. Web sales surfaces are read-only.

## 3. Audit matrix (starting state → action)

| Feature | Backend | MAUI | Web (before) | Action |
|---|---|---|---|---|
| Overview aggregates | Dashboard loaded full sale lists | Partial | Missing | Added bounded `GET /pos/management/overview` |
| Organization profile | Yes | Partial | Missing | Management UI |
| Branches | Yes | Partial | Missing | List/create/edit/archive |
| Staff & invites | Yes | Partial | Missing | List/invite/deactivate/POS roles |
| Roles | Fixed POS roles | Partial | Missing | Assignment + explanation (no RBAC designer) |
| Products / categories | Yes | Yes | Missing | Management UI |
| Global Catalog import | Yes | Yes | Missing | Browse + already-added guard |
| Inventory stock / adjust | Yes | Yes | Missing | Management UI (movements, not overwrite) |
| Transfers | Yes | Yes | Missing | List/create/dispatch/receive |
| Expiration lots | Yes | Partial | Missing | Paged expiring lots |
| Customers | Yes | Yes | Missing | Directory + link/utang summary |
| Devices / registers | Yes | Partial | Missing | Management UI (no PIN secrets) |
| Shifts | Yes | Yes | Missing | Inspection only |
| Sales | Yes | Checkout on MAUI | Missing | **Read-only** history/detail/reports |
| Utang reports | Yes | Yes | Missing | Management report |
| CashCountMode | Yes | Yes | Missing | Settings |
| Subscription | Yes | Partial | Missing | Read-only |
| Notifications | Yes | Partial | Missing | Bell + list |
| Audit activity feed | Platform-admin only | No | No | Not fabricated |

## 4. Reused APIs (not rebuilt)

Platform: organizations, branches, members, invitations, POS devices, notifications, subscription/entitlements, customer-link status.

POS: catalog, catalog import, inventory, lots, transfers, customers/credit, sales **GET**, registers, cashier shifts **GET**, operational setup, reports/dashboard/overview, permissions.

Web does **not** call `CheckoutAsync` / `VoidSaleAsync` / open-shift cashier flows.

## 5. Added this WP

- `ExItS.PinoyBusinessPOS.Web` shell (cookie auth, org hydration, permission-filtered nav, responsive sidebar/drawer).
- Management pages listed in §3.
- Thin POS endpoints: `GET /api/v1/pos/management/overview`, `GET /api/v1/pos/inventory/lots`.
- ApiClient wrappers for overview, expiring lots, branch update/archive, invitations resend/revoke, membership role/reactivate.
- Local Validation port **8093**.
- Tests: Web authorization/no-checkout, architecture (no Infra/AntDesign), overview query service.

## 7b. Test evidence (Release)

| Suite | Passed | Failed | Skipped | Notes |
|---|---:|---:|---:|---|
| ExItS.PinoyBusinessPOS.Web.Tests | 7 | 0 | 0 | Nav permissions + no checkout |
| ExItS.PinoyBusinessPOS.UnitTests | 639 | 0 | 0 | Includes ManagementOverviewQueryServiceTests |
| ExItS.PinoyBusinessPOS.ApiClient.Tests | 48 | 0 | 0 | |
| ExItS.ArchitectureTests (OrgWeb + foundation + LV + repo safety) | 12 | 0 | 0 | New OrgWebAdminArchitectureTests |
| ExItS.PinoyBusinessPOS.Maui.Tests | 379 | 1 | 0 | Failure is `AuthenticationService` containing "Cashier" — pre-existing, not Org Web |
| ExItS.ArchitectureTests (full) | 147 | 4 | 0 | Pre-existing: catalog "checkout" comment, Admin page-header L[], Android NSC wording, SaaSPayment prefix |

## 6. Authorization

- Authenticated browser cookie + Platform session.
- Active organization membership required.
- Navigation uses owner role **or** POS `UtangCapability` from `GET /pos/permissions/effective`.
- Server APIs remain the authority (organization header + capability + tenant isolation).
- Personal linked customers do not receive Org Admin from the link alone.

## 7. Write vs read

**Write (management):** profile, branches, staff/roles, products, inventory adjustments, transfers, devices/registers, operational settings including CashCountMode.

**Read-only operational:** sales, receipts, payments, shift history/variance.

## 8. Migrations

**No.** UI-only plus query endpoints over existing tables.

## 9. Performance / bandwidth

- Dashboard: one SQL aggregate + bounded Platform counts (not 30 list calls).
- Default page size 20; search cancellation on products.
- Reports use server aggregates.
- Compact DTOs; no transaction images.

## 10. Responsive UX

Desktop persistent sidebar; ≤960px drawer navigation. Tables scroll horizontally. Compact metric cards.

## 11. Owner / browser checklist (pending)

LOGIN · OVERVIEW · PRODUCTS · BRANCHES · STAFF · INVENTORY · CUSTOMERS · OPERATIONS · REPORTS (confirm **no checkout**) · SETTINGS · SECURITY (limited staff) · RESPONSIVE (desktop/tablet/mobile browser).

Do not mark Device Verified / Production Ready until the owner completes this list.

## 12. Explicit exclusions

- Web checkout / cart / barcode selling / payment-taking / cashier sale creation
- Custom RBAC designer
- Fabricated org audit event store
- Redis
- Duplicate domain services
- Platform Admin Ant Design reuse for this product UI

## 13. Git

| Commit | Message |
|---|---|
| `8869aec3` | feat(org-web): add organization management web admin |
| `837f6d13` | test(org-web): cover admin authorization, no-checkout, and overview queries |
| `7bff37a2` | docs(org-web): document organization web admin management center |

Starting SHA: `43f5d0f2dd7c92c4903d1947162cf2c1e996b932`

## 14. Next

Owner browser validation of Organization Web on Local Validation (`http://localhost:8093`).
