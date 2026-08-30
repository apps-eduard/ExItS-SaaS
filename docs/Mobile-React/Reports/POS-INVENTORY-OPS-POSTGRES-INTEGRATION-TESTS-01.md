# POS-INVENTORY-OPS-POSTGRES-INTEGRATION-TESTS-01

**Status:** COMPLETE  
**Branch:** `feat/organization`  
**TASK:** POS-INVENTORY-OPS-POSTGRES-INTEGRATION-TESTS-01  
**START_SHA:** `a10a8fd1f3c323555071c04ce09611bfc78ee2cf`  
**FEATURE_SHA:** `722e9634f57137dda9d5832f84f71de607b760f8`  

## TEST_INFRASTRUCTURE_REUSED

- `PosPostgreSqlFixture` / `PosPostgreSqlCollection` (`postgres:18` Testcontainers)
- Nested `WebApplicationFactory<Program>` pattern (`Testing` + `ConnectionStrings:PosDatabase`)
- Org isolation via fresh `Guid.NewGuid()` per test (no truncate)
- Shared helpers: `PosInventoryOpsIntegrationSupport` (mirrors `PosShiftIntegrationSupport` style)
- Commercial grant headers + POS role assignment patterns from `PosInventoryApiTests` / `PosPermissionApiTests`
- Idempotency headers pattern from `PosSaleApiTests` / transfer tests

## POSTGRES_CONTAINER_MODEL

Single shared PostgreSQL 18 container per collection; migrations applied once at fixture startup.

## DATABASE_RESET_MODEL

No per-test truncate. Isolation by unique organization IDs.

## STOCK_USE_COVERAGE

`PosStockUseApiTests` (5 facts):

- Create decreases on-hand; movement type `StockUse`; reason/notes/reference; unit/line cost from OpeningStock; list/get history
- Branch-scoped create requires branch balance (seed via adjustment) and persists `BranchId`
- Unknown cost remains null; profitability `stockUseKnownCost` separate from `KnownCogs`
- Cross-org fail-closed; insufficient stock → 409; view-only commercial + ReportingUser denied; HTTP idempotency replay (201 then 200, no double decrement)
- Foreign org product + branch attribution fail-closed

## WASTE_LOSS_COVERAGE

`PosWasteLossApiTests` (4 facts):

- Expired reason + explicit lot; cost Complete; on-hand/movement; list history
- Unknown cost → Unavailable / null totals; profitability `wasteLossKnownCost` separate from COGS
- Cross-org; insufficient stock 409; permission gates; idempotent replay
- Foreign org product fail-closed

## PRODUCTION_COVERAGE

`PosProductionApiTests` (3 facts):

- Definition + atomic run: materials decrease, output increase, consumption/output movements, Complete cost + `OutputBaseUnitCost`; ProductionOutput feeds subsequent Stock Use cost; not counted as Sale/StockUse/Waste
- Insufficient materials → 409 with no inventory change; Partial cost when some materials lack acquisition cost; single output movement
- Cross-org; permission gates; idempotent replay; no nested BOM / no FIFO assumptions

## COSTING_MODEL_VALIDATED

Latest acquisition unit cost by movement timestamp from OpeningStock / PurchaseReceipt / DirectPurchaseReceipt / ProductionOutput (via `GetLatestAcquisitionUnitCostAsync`). Not FIFO / weighted average / selling price.

## UNKNOWN_COST_POLICY

Null snapshots remain null; Waste/Production use Complete / Partial / Unavailable; never coerce to zero.

## CROSS_ORG_GUARD

**PASS** — mutations against another org’s product fail; source org on-hand unchanged.

## INVALID_BRANCH_FAILS_CLOSED

**PASS** — foreign-org product under caller org/branch fails closed.  
Note: optional `BranchId` requires existing branch balance (opening stock is org-level; branch stock is seeded via adjustment). Invalid/empty branch balance yields insufficient branch stock (409), not silent org-wide consume.

## PERMISSION_GUARD

**PASS** — `store-inventory-view` only → 403; ReportingUser (no ManageInventory) → 403; ManageInventory path succeeds for Owner/default Testing grants.

## STOCK_USE_IDEMPOTENCY

**HTTP headers** `Idempotency-Key` + `X-Pos-Payload-Hash` (+ optional operation type `inventory.stock_use`); body `idempotencyKey` / `stockUseId` also supported. Replay → 200, same id, no second movement.

## WASTE_LOSS_IDEMPOTENCY

Same model; operation type `inventory.waste_loss`.

## PRODUCTION_IDEMPOTENCY

Same model for **runs** (`inventory.production_run`). Definition CRUD is not idempotent-keyed.

## REPORTING_SEPARATION_VALIDATED

**PASS** — after Stock Use / Waste posts, `GET /api/v1/pos/reports/profitability` shows `stockUseKnownCost` / `wasteLossKnownCost` with `KnownCogs` = 0 and `CompletedSaleCount` = 0 when no sales.

## DOUBLE_COUNTING_GUARD

**PASS** — production consumption movements are not `SaleDeduction` / `StockUse` / `WasteLoss`; profitability keeps waste/stock-use aggregates separate from sale COGS.

## PRODUCTION_CODE_CHANGE_REQUIRED

**NO**

## MIGRATION

**N/A**

## POSTGRES_INTEGRATION_TESTS

| Suite | Count | Result |
|-------|-------|--------|
| PosStockUseApiTests | 5 | PASS |
| PosWasteLossApiTests | 4 | PASS |
| PosProductionApiTests | 3 | PASS |
| **Total** | **12** | **PASS** |

## BACKEND_REGRESSION_TESTS

`ExItS.PinoyBusinessPOS.UnitTests.Inventory` → **177 passed / 0 failed**

## REACT

REACT_CHANGE_REQUIRED=NO  

| Check | Result |
|-------|--------|
| REACT_FULL_TEST_COUNT | 1305 |
| REACT_FULL_PASS | 1305 |
| REACT_FULL_FAIL | 0 |
| TYPECHECK | PASS |
| LINT | PASS (0 errors; pre-existing warnings) |
| BUILD | PASS |

## NEXT

`POS-PURCHASE-RECEIPT-REVERSAL-01`

Inventory ops (Stock Use / Waste / Production) now have PostgreSQL API coverage. Next high pilot-value correctness gap is safe purchase/GRN receipt reversal — still separate from Supplier Payables and Customer Utang.
