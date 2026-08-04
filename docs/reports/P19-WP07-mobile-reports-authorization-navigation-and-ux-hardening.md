# P19-WP07 — Mobile Reports, Authorization, Navigation, and UX Hardening

| Field | Value |
|---|---|
| Status | **Code Complete** |
| Phase | [Phase 19](../phases/phase-19-mobile-pos-operations-and-cashier-experience.md) — **Open** |
| Commit | _(filled after commit)_ |
| Production-ready | **No** |
| Device Verified | **No** |
| Date | 2026-08-04 |

## 1. Objective

Capability-gate Reports hub/menus and MoreHub operational navigation so unfinished or unauthorized surfaces are not actively exposed.

## 2. Existing reuse

`PosRoleMatrix.AllowsReport` intent, existing operational report endpoints, MoreHub entry points.

## 3. Delivered

- MoreHub: Inventory/Expenses/Suppliers/Purchasing/Registers/Setup/Shifts/Permissions/Reports gated by matching View* (or ManageOperationalSetup / ViewDashboard) capabilities
- ReportsHub: full menu for ViewReports; Cashier-like ViewShifts-only sees shift summary + cash variance; inventory/purchasing/expenses subsets; empty sections hidden
- OperationalReportPage: per-kind CanAccessKind redirects to `/reports` when denied

## 4. Residuals

- Client approximates role report matrix via capabilities (session does not expose PosRole enum directly)
- Export remains deferred (existing banner)

## 5. Tests

`CustomersReportsNavPageGuardTests` — reports hub/operational/MoreHub gating assertions.

## 6. Authorization

Server remains authoritative; client hides unauthorized nav and report kinds.

## 7. Status

**Code Complete.** Phase 19 remains **Open**. Not Device Verified.
