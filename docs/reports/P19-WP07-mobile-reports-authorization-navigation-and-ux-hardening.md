# P19-WP07 — Mobile Reports, Authorization, Navigation, and UX Hardening

| Field | Value |
|---|---|
| Status | **Code Complete** (phone scenario **Retest**) |
| Phase | [Phase 19](../phases/phase-19-mobile-pos-operations-and-cashier-experience.md) — **Open** |
| Commit | 2c63530 |
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

## 4. Residuals / Retest notes

- Client approximates role report matrix via capabilities (session does not expose PosRole enum directly)
- Export remains deferred (existing banner)
- **Retest (phone):** Selling Mode must not change POS role; capability gates for Registers/Shifts must remain consistent with merged Local Validation commercial grants so Open Shift is reachable for Owner.

## 5. Tests

`CustomersReportsNavPageGuardTests` — reports hub/operational/MoreHub gating assertions.

## 6. Authorization

Server remains authoritative; client hides unauthorized nav and report kinds. Local Validation merges Dev commercial defaults onto partial Platform entitlement snapshots.

## 7. Status

**Code Complete.** Phase 19 remains **Open**. Related Owner Selling Mode / Open Shift phone scenario marked **Retest**. Not Device Verified.
