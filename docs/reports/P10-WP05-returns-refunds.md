# P10-WP05 — Returns and Refunds

Phase marker: `P10-WP05-returns-refunds`

## Status

**Complete.** Atomic Completed-only sale returns, refund-method matching, cash-shift impact, Utang credit reduction, optional restock, void/return mutual exclusion, grants, PostgreSQL migration, typed API/MAUI surfaces, and focused tests. **P10-WP06 not started.** R-091 and POS-ROLES remain open.

Feature commit: `58dd6bf`  
Docs commit: `485f23e`

## Delivered capability

| Area | Delivered |
|---|---|
| Return aggregate | `SaleReturn` in `Domain/Returns/`; `RET-YYYYMMDD-NNNNNN`; Completed-only; immutable; org-scoped |
| Return lines | Snapshots from sale lines; refundable qty/amount; duplicate line consolidation; `ReturnToStock` / `DoNotRestock` |
| Refund methods | Cash (open shift + expected-cash reduction); ManualGCash (no physical cash); Utang (`CreditEntry.ReduceForSaleReturn`) |
| Inventory | `StockMovementType.SaleReturnRestock` + `StockMovementSourceType.SaleReturn`; unique index; atomic with create |
| Void gate | `VoidSale` blocked when sale has returns (`ISaleRepository.HasReturnsForSaleAsync`) |
| Cash shift | `CashierShiftExpectedCash` subtracts `cashRefundsOnShift`; shift summary exposes `CashRefundsTotal` |
| Grants | `store-returns-view` / `store-returns-manage`; `ViewReturns` / `ProcessReturn`; Platform `FeatureCode`; default dev grants + matrix |
| Persistence | Migration `AddPosSaleReturns` after `AddPosCashierShifts`; safe Down deletes return movements before narrowing constraints |
| API / client | `/api/v1/pos/sale-returns` list/get/refundable/create (idempotent); `PosSaleReturnClient` |
| MAUI | `/sales/{saleId}/return`; return history on sale detail; EN + fil-PH; online-only; capability-gated |
| Architecture | Returns in dedicated `Returns/` folders; `PosReturnsScopeArchitectureTests`; Sales slice still bans `SaleReturn` names |

## Explicit exclusions

Exchanges, store credit, gift cards, split/different refund methods, unlinked returns, supplier/PO returns, shipping, restocking fees, tax/VAT, promotions recalculation, payment gateways, GCash API verification, manager approval / POS roles, offline returns, **P10-WP06+**. R-091 / POS-ROLES not closed.

## Persistence

Database: `ExItS_PinoyBusinessPOS` · Schema: `pos`  
Migration: `20260731052329_AddPosSaleReturns`

Tables: `sale_returns`, `sale_return_lines`, `sale_return_number_sequences`  
Stock: extended check constraints + `ux_stock_movements_sale_return_source`  
Down migration deletes SaleReturn stock movements before narrowing constraints.

## Build and test evidence

| Suite | Passed | Failed | Skipped |
|---:|---:|---:|---:|
| Full `ExItS.slnx` Release (test projects, `net10.0`, excl. MAUI Android host) | **1045** | **0** | **0** |
| `ExItS.PinoyBusinessPOS.Maui.Tests` (`net10.0` guard tests) | **65** | **0** | **0** |
| **Combined** | **1110** | **0** | **0** |

Prior baseline: **1097 / 0 / 0** (post P10-WP04). Net new tests: **+13**.

Release build of POS API succeeds. MAUI `net10.0-android` Release succeeds after adding `@using ExItS.PinoyBusinessPOS.Application.Returns` to Maui `_Imports.razor` (gap-fix; DTOs were unresolved on Android TFM). R-129 (NU1903) unchanged.

## Security limitations

Development/Testing actor only; no production POS role model. Refund method enforced server-side from original sale tender. Unauthenticated dev APIs remain non-production-secure.

## Portfolio independence

- No `HealthCare/` tree; `git ls-files -- HealthCare/` empty.
- No cross-product DB access; no HealthCare projects in `ExItS.slnx`.

## Risks / open decisions

- R-091 production auth unchanged.
- POS-ROLES operational roles deferred.
- R-109 Android SDK required for full MAUI host compile on CI/dev machines without SDK.
- Utang partial credit reduction uses amount reduction without fabricated repayments; full reduction reverses entry.

## Exact next work package

**P10-WP06 — Advanced Permissions and Reports** — do **not** begin until explicitly authorized.
