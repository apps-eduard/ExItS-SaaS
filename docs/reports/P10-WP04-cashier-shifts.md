# P10-WP04 — Cashier Shifts

Phase marker: `P10-WP04-cashier-shifts`

## Status

**Complete.** Cashier shift aggregate, cash movements, sale linkage, expected-cash close with variance, grants, PostgreSQL migration, typed API/MAUI surfaces, and focused tests. **P10-WP05 not started.** R-091 and POS-ROLES remain open.

Feature commit: `4076485`  
Docs commit: `df0a092`

## Delivered capability

| Area | Delivered |
|---|---|
| Shift aggregate | `CashierShift` Open/Closed/Cancelled; `SHIFT-YYYYMMDD-NNNNNN`; one Open per org + actor |
| Cash movements | Immutable CashIn/CashOut on Open shifts; idempotent via movement endpoint + optional client `MovementId` |
| Expected cash | Opening + NetCashSales + CashIn − CashOut; ManualGCash/Utang reported but excluded from physical cash; voided Cash reverses contribution |
| Close | Closing declaration, expected snapshot, variance; terminal Closed/Cancelled |
| Sale integration | Nullable `CashierShiftId` on `Sale`; checkout requires Open shift for org + actor; legacy sales unassigned (no backfill) |
| Grants | `store-shifts-view` / `store-shifts-manage`; `ViewShifts` / `ManageShifts`; Platform `FeatureCode`; default dev grants + matrix tests |
| Persistence | Migration `AddPosCashierShifts` after `EnrichPosStockCountDate`; filtered unique index on open shift |
| API / client | `/api/v1/pos/cashier-shifts` list/open/current/detail/close/cancel/movements/summary; `PosCashierShiftClient` |
| MAUI | `/shifts`, `/shifts/open`, `/shifts/{id}`; checkout gated on open shift; EN + fil-PH; online-only |

## Explicit exclusions

Payroll, accounting journals, bank reconciliation, cash deposits, branch registers, DeviceId/register, tax/fiscal closing, expense↔shift auto-coupling, Draft/Suspended/Reopened states, **P10-WP05+**. Production POS roles (R-091 / POS-ROLES) not closed.

## Persistence

Database: `ExItS_PinoyBusinessPOS` · Schema: `pos`  
Migration: `20260731035548_AddPosCashierShifts`

Tables: `cashier_shifts`, `cashier_shift_movements`, `cashier_shift_number_sequences`  
FK: `sales.cashier_shift_id` nullable, `Restrict`  
Index: `ux_cashier_shifts_org_actor_open` (filtered unique on Open)

Down migration clears shift-linked sale FKs and shift data before dropping constraints.

## Build and test evidence

| Suite | Passed | Failed | Skipped |
|---:|---:|---:|---:|
| Full `ExItS.slnx` Release (test projects, excl. MAUI Android host) | **1032** | **0** | **0** |
| `ExItS.PinoyBusinessPOS.Maui.Tests` (net10.0 guard tests) | **65** | **0** | **0** |
| **Combined** | **1097** | **0** | **0** |

Prior baseline: **1079 / 0 / 0** (post P10-WP03). Net new tests: **+18**.

Release build of POS API succeeds. MAUI `net10.0-android` Release compiles after NumberInput `decimal?` ValueChanged coalesce on shift screens. R-129 (NU1903) unchanged.

## Security limitations

Development/Testing actor only; no production POS role model. Checkout shift resolution is server-authoritative for org + actor. Unauthenticated dev APIs remain non-production-secure.

## Portfolio independence

- No `HealthCare/` tree; `git ls-files -- HealthCare/` empty.
- No cross-product DB access; no HealthCare projects in `ExItS.slnx`.

## Risks / open decisions

- R-091 production auth unchanged.
- POS-ROLES operational roles deferred.
- R-109 Android SDK required for full MAUI host compile on CI/dev machines without SDK.

## Exact next work package

**P10-WP05 — Returns and Refunds** — do **not** begin until explicitly authorized.
