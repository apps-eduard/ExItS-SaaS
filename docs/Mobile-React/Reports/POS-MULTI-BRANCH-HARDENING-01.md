# POS-MULTI-BRANCH-HARDENING-01

| Field | Value |
|-------|-------|
| **TASK_STAGE** | MULTI-BRANCH-HARDENING |
| **PARENT** | POS-PILOT-TO-MULTIBRANCH-MASTER-VALIDATION-01 |
| **START_SHA** | `ad1f9171dbafc1e71c6ca94f2732dfda787b81ce` |
| **SCOPE** | Audit/harden existing multi-branch behavior — not feature expansion |

---

## Coverage map (classification)

| Area | Classification | Notes |
|------|----------------|-------|
| Products | ORG_LEVEL_BY_DESIGN | Catalog org-scoped |
| InventoryAccount | ORG_LEVEL_BY_DESIGN | Model A sellable on-hand |
| InventoryBranchBalance | BRANCH_SCOPED | Overlay; EnsureBalance on outflows |
| Sales / Shifts / Registers | BRANCH_SCOPED | Acting branch headers |
| Stock Use / Waste | BRANCH_SCOPED | BranchBalanceMutation |
| Transfers | BRANCH_SCOPED | Source/dest balances |
| Stock Count | ORG_LEVEL_BY_DESIGN / MIXED | Org on-hand semantics |
| Purchasing / GRN | ORG_LEVEL_BY_DESIGN | `branchId` null receive |
| Supplier Payables | ORG_LEVEL_BY_DESIGN | ADR-023 |
| Expenses | ORG_LEVEL_BY_DESIGN / honest | Report scoping docs |
| Reports | MIXED_INTENTIONAL | Optional branchId; some org-only honest |
| Staff/RBAC | ORG + product role | Branch device bind separate |
| Customer orders | BRANCH_SCOPED when fulfillment branch set | See MB_13 |
| Lots | BRANCH_SCOPED | Exact BranchId list filter |

---

## MB scenario results

| ID | STATUS | Evidence |
|----|--------|----------|
| MB_01 | PASS | Sale/StockUse EnsureBalance on acting branch; Stock Use Postgres + unit |
| MB_02 | PASS | `After_primary_branch_materializes_balance_other_branch_cannot_spend_unallocated` |
| MB_03 | PASS | `Non_primary_does_not_receive_unallocated_org_stock` (BranchBalanceMutationTests) |
| MB_04 | PASS | `PosInventoryTransferApiTests` 4/4 after lot list branch-scope fix |
| MB_05 | PASS | Shift/register branch headers in API tests; architecture |
| MB_06 | PASS | Inventory views org on-hand + branch overlay docs; Stock Use fix |
| MB_07 | PASS | EVIDENCE_REUSED REPORTS-BRANCH-SCOPING + dashboard clarity |
| MB_08 | PASS | Device/branch bind + Cashier context locked; completion SC19 |
| MB_09 | PASS | Stock count org semantics; no false branch claim |
| MB_10 | PASS | Waste/Stock Use EnsureBalance; SC14 completion |
| MB_11 | PASS | Payables org-level ADR-023; no branch invent |
| MB_12 | PASS | EnsureBalance does not double-seed (`Existing_balance_is_not_double_seeded`) |
| MB_13 | PASS / PARTIAL | Customer-order COGS package; fulfillment branch present — classify residual as gap if field reports mismatch |
| MB_14 | PASS | StockUse cross-org isolation test |

---

## Bugs / gaps

| Item | Class | Result |
|------|-------|--------|
| Transfer lot list asserted BranchB lots while GET used BranchA header | TEST_ALIGNMENT | Fixed `ListLotsAsync` to accept branch — production OK |
| Branch-specific financial reporting enhancement | MULTI_BRANCH_GAP | Deferred |
| Branch-specific expense accounting | MULTI_BRANCH_GAP | Deferred |
| Advanced branch staff scheduling | MULTI_BRANCH_GAP | Deferred |

| Metric | Value |
|--------|------:|
| **MULTI_BRANCH_BUGS_FOUND** | 0 production |
| **MULTI_BRANCH_BUGS_FIXED** | 0 production (1 test harness alignment) |
| **MULTI_BRANCH_GAPS** | 3 deferred (above) |

---

## Targeted re-proof executed

| Suite | Result |
|-------|--------|
| BranchBalanceMutationTests | 3/3 PASS |
| PosStockUseApiTests | 8/8 PASS |
| PosInventoryTransferApiTests | 4/4 PASS |

No migration. No large new multi-branch features implemented.
