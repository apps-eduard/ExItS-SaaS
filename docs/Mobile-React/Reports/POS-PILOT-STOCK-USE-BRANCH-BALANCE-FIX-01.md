# POS-PILOT-STOCK-USE-BRANCH-BALANCE-FIX-01

| Field | Value |
|-------|-------|
| **TASK** | POS-PILOT-STOCK-USE-BRANCH-BALANCE-FIX-01 |
| **START_SHA** | `f802cedc1bcf16c78565c7253b656d53c54633d7` |
| **FEATURE_SHA** | `bc625cfb0224248f2f5d695645ac29d2a07c2973` |
| **FINAL_SHA** | `34d6f09323c68814300a5040a233a2b4cb48ec18` |
| **PILOT_SOURCE** | `docs/Mobile-React/Pilot/POS-CONTROLLED-SINGLE-BRANCH-PILOT-01.md` |
| **PILOT_SCENARIO** | 14 |

---

## ROOT_CAUSE

Stock Use / Waste / Production / Adjust **outflows** created a missing `InventoryBranchBalance` at **0**, then applied a negative delta → `Insufficient branch stock`.

Opening Stock, Direct Purchase, and PO Goods Receipt correctly update only org-level `InventoryAccount` (Model A — unallocated pool). Sales already materialize branch balances via `BranchStockResolver.EnsureBalance`. Stock Use did not.

Inventory UI reads `InventoryAccount.OnHandQuantity`, so operators saw stock while branch-scoped Stock Use failed.

| Field | Value |
|-------|-------|
| **INVENTORY_ACCOUNT_ROLE** | Authoritative org sellable on-hand (denormalized from movements) |
| **INVENTORY_BRANCH_BALANCE_ROLE** | Per-branch accountability overlay; unallocated org stock attributed to primary on first outflow |
| **UI_QUANTITY_SOURCE** | Org `InventoryAccount.OnHandQuantity` (unchanged; correct for Model A) |
| **STOCK_USE_QUANTITY_SOURCE** | Org availability check + branch balance via `EnsureBalance` (fixed) |

---

## BRANCH_SEMANTICS

| Field | Value |
|-------|-------|
| **BRANCH_SEMANTICS** | Model A — org receiving/opening stays unallocated; branch overlay seeds from unallocated on primary for outflows |
| **SINGLE_BRANCH_POLICY** | Header / request branch receives unallocated via `EnsureBalance` (Testing: primary null ⇒ any branch may seed; Production: primary from Platform directory) |
| **MULTI_BRANCH_POLICY** | Non-primary without explicit balance gets 0 unallocated; after primary materializes, other branches cannot spend the same pool |

Opening / DP / GRN / Stock Count left org-only (no change to purchasing `branchId=null` / payables org scope).

---

## Flow effects after fix

| Flow | Effect |
|------|--------|
| **OPENING_STOCK_EFFECT** | Still org-only; first Stock Use/Waste/Sale on primary materializes branch qty |
| **DIRECT_PURCHASE_EFFECT** | Still org-only; same lazy materialization |
| **GOODS_RECEIPT_EFFECT** | Unchanged org-only |
| **SALE_EFFECT** | Unchanged (`EnsureBalance`) |
| **WASTE_EFFECT** | Now uses `BranchBalanceMutation` outflows = EnsureBalance |
| **STOCK_USE_EFFECT** | Now uses EnsureBalance for outflows |
| **STOCK_COUNT_EFFECT** | Unchanged org-level |

Inflows (Adjust In, production output, void restore credit) still seed missing rows at **0** then credit — do not steal unallocated onto an arbitrary branch.

---

## EXISTING_DATA_REPAIR_POLICY

**Lazy repair on first outflow** via `BranchStockResolver.EnsureBalance` (same as sales).

- No migration.
- Failed Stock Use never persisted zero balances (Apply threw before Upsert).
- Do not insert Create(0) rows for products that already have org on-hand (would shadow unallocated).

| Field | Value |
|-------|-------|
| **LOT_EXPIRY_POLICY** | Unchanged; lot FEFO consume remains org/lot scoped; no invented lot qty |
| **COSTING_CHANGE** | NONE |

---

## Code change

| Field | Value |
|-------|-------|
| **PRODUCTION_CODE_CHANGE_REQUIRED** | YES |
| **MIGRATION_REQUIRED** | NO |
| **MIGRATION_REASON** | Lazy EnsureBalance; PK already `(org, branch, product)` |

New helper: `BranchBalanceMutation.ApplyAsync` wired into:

- `CreateStockUse` / `VoidStockUse`
- `CreateWasteLoss` / `VoidWasteLoss`
- `CreateProductionRun` / `VoidProductionRun`
- `AdjustInventoryStock`

Optional `IOrganizationBranchDirectory` for primary resolution (DI already registered).

**REACT_CHANGE_REQUIRED:** NO — UI org on-hand is correct under Model A once Stock Use succeeds.

---

## Tests

| Suite | Result |
|-------|--------|
| New unit `BranchBalanceMutationTests` | 3 PASS |
| Stock Use / Waste / Transfer unit (targeted) | PASS |
| Postgres `PosStockUseApiTests` + `PosWasteLossApiTests` + payables | **17 PASS / 0 FAIL** (includes new opening→Stock Use, DP→Stock Use, multi-branch guard, opening→Waste) |
| React full | **1344 / 1344** |
| Typecheck / Lint / Build | PASS / PASS (0 errors) / PASS |

| Field | Value |
|-------|-------|
| **POSTGRES_INTEGRATION_TEST_COUNT** | 17 (targeted coherence + related ops/payables run) |
| **POSTGRES_INTEGRATION_PASS** | 17 |
| **POSTGRES_INTEGRATION_FAIL** | 0 |
| **BACKEND_REGRESSION_TESTS** | StockUse, WasteLoss, BranchBalanceMutation, InventoryTransfer (Same_org), SupplierPayables — PASS |
| **PILOT_SCENARIO_14_RETEST** | PASS (Postgres: `Opening_stock_then_branch_scoped_stock_use_succeeds_without_preseed`) |
| **REACT_TARGETED_TESTS** | N/A (no React change) |
| **REACT_FULL_TEST_COUNT** | 1344 |
| **REACT_FULL_PASS** | 1344 |
| **REACT_FULL_FAIL** | 0 |
| **NEW_TEST_SKIPS** | 0 |
| **NEW_TEST_ONLY** | 0 |
| **TEST_EXCLUSIONS_ADDED** | 0 |

Note: `PosInventoryTransferApiTests.Transfer_preserves_lot_identity...` still fails when listing lots under BranchA after receive to BranchB (pre-existing list scope assertion); unrelated to this outflow seed fix. Transfer unit `Same_org_transfer_full_receive` PASS after inflow/outflow split.

---

## NEXT

| Field | Value |
|-------|-------|
| **NEXT** | POS-PILOT-COMPLETION-VALIDATION-01 |
| **NEXT_WHY** | Scenario 14 coherence fixed with no costing/purchasing model change; remaining pilot gaps are live roles (19) and responsive UX (21) |

FEATURE_SHA / FINAL_SHA recorded after commit/push.
