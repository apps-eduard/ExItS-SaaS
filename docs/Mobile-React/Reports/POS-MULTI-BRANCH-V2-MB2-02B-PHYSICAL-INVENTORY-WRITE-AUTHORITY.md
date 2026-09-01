# POS-MULTI-BRANCH-V2 MB2-02B — Physical Inventory Write Authority

**Program:** POS-MULTI-BRANCH-COMMERCE-V2  
**Package:** MB2-02B  
**Branch:** `feat/organization`  
**Status:** COMPLETE_VALIDATED_WRITE_AUTHORITY  
**Start SHA:** `7b5fb86a45e59a241dc7d0835be1d5f99e88aa05`  
**Hard stop:** YES — do not start MB2-02C in this package.

**H1 follow-up:** Reservation must not reduce branch OnHand. Mixed-lot `lots.Count == 0` heuristic was unsafe. See [MB2-02B-H1](POS-MULTI-BRANCH-V2-MB2-02B-H1-INVENTORY-RESERVATION-PRIMARY-AND-LOT-HARDENING.md).

---

## Summary

Every physical inventory **write** now updates **organization aggregate** and **correct branch overlay** atomically, with immutable branch provenance on source documents where required. Central coordinator: `BranchInventoryMutationService` + hardened `BranchBalanceMutation` (pre-mutation materialization ordering).

**Core invariant:**

```
PHYSICAL STOCK MUTATION
= ORG AGGREGATE EFFECT + CORRECT BRANCH EFFECT + LOT/MOVEMENT PROVENANCE + AUDITABLE SOURCE + ATOMIC/IDEMPOTENT BEHAVIOR
```

---

## Write model

| Layer | Role |
|-------|------|
| `InventoryAccount` | Organization aggregate / control / reconciliation |
| `InventoryBranchBalance` | Physical operational quantity at one branch |
| `BranchInventoryMutationService` | Central branch overlay coordinator |
| `BranchBalanceMutation` | `EnsureBalance` from **pre-mutation** org on-hand, then apply signed delta |

**Materialization order (locked):** resolve org on-hand → `EnsureBalance` target branch from pre-mutation baseline → apply org delta → apply branch delta. Prevents double-count when primary legacy stock is materialized on first touch.

**Read paths:** remain side-effect free (MB2-02A preserved).

---

## Migration

**Name:** `20260901080801_AddInventoryWriteBranchProvenance`

| Table | Column | New writes | Legacy |
|-------|--------|------------|--------|
| `direct_purchase_receipts` | `receiving_branch_id` | Required | Nullable → Primary-only reversal |
| `goods_receipts` | `receiving_branch_id` | Required | Nullable → Primary-only reversal |
| `stock_counts` | `branch_id` | Required at create | Nullable historical rows |

No stock rewrite. No balance mass backfill. No product duplication.

---

## Document provenance (final)

| Document | Branch field | When assigned | Immutable after post | Legacy null |
|----------|--------------|---------------|----------------------|-------------|
| `DirectPurchaseReceipt` | `ReceivingBranchId` | Create | YES | Primary-only reversal |
| `GoodsReceipt` | `ReceivingBranchId` | Create/receive | YES | Primary-only reversal |
| `StockCount` | `BranchId` | Create draft | YES | Fail if missing on complete |
| `Sale` | `BranchId` | Sale create | YES | Primary-only return/deduct compat |
| `SaleReturn` | via `Sale.BranchId` | Return post | YES | Primary-only |
| `StockUse` | acting branch | Post | YES | — |
| `WasteLoss` | acting branch | Post | YES | — |
| `Production` | acting branch | Post | YES | — |
| `InventoryTransfer` | `SourceBranchId` / `DestinationBranchId` | Create | YES | — |
| `InventoryLot` | `BranchId` | New lot writes | YES | Primary read compat |
| `StockMovement` | `BranchId` | New movement writes | YES | Primary read compat |

---

## Write-path matrix (final)

| Operation | Org write | Branch write | Lot branch | Movement branch | Source persisted | Idempotent | Status |
|-----------|-----------|--------------|------------|-----------------|------------------|------------|--------|
| Opening / enable tracking | Δ | Δ same branch | YES | YES | Acting branch | Existing | **HARDENED** |
| Add opening stock | + | + acting | YES | YES | Acting branch | Existing | **HARDENED** |
| Adjustment | Δ | Δ acting | YES | YES | Acting branch | movementId | **HARDENED** |
| Stock count complete | +variance | branch → counted | — | YES | Session BranchId | Session | **HARDENED** |
| Direct Purchase | + | + receiving | YES | YES | ReceivingBranchId | idempotencyKey | **HARDENED** |
| PO/GRN receive | + | + receiving | YES | YES | ReceivingBranchId | GRN idempotency | **HARDENED** |
| Purchase reversal | − | − original branch | YES | YES | Receipt BranchId | Void guards | **HARDENED** |
| Sale deduct | − | − Sale.BranchId | YES | YES | Sale.BranchId | Sale idempotency | **ALREADY_CORRECT_VERIFIED** |
| Customer order reserve | reserve | FulfillmentBranchId | YES | YES | FulfillmentBranchId | Existing | **ALREADY_CORRECT_VERIFIED** |
| Sale return | + | + original sale branch | YES | YES | Sale.BranchId | Return guards | **HARDENED** |
| Stock Use | − | − acting | YES | YES | Acting branch | Existing | **ALREADY_CORRECT_VERIFIED** |
| Waste/Loss | − | − acting | YES | YES | Acting branch | Existing | **ALREADY_CORRECT_VERIFIED** |
| Production in/out | Δ | Δ acting | YES | YES | Acting branch | Existing | **ALREADY_CORRECT_VERIFIED** |
| Transfer dispatch | 0 net | source − | YES | YES | Transfer branches | Transfer idempotency | **ALREADY_CORRECT_VERIFIED** |
| Transfer receive | 0 net | dest + | YES | YES | Transfer branches | Transfer idempotency | **ALREADY_CORRECT_VERIFIED** |
| Expiry write-off | − | lot branch | YES | YES | Lot branch | Existing | **ALREADY_CORRECT_VERIFIED** |

---

## Business scenarios (PostgreSQL integration)

| ID | Result |
|----|--------|
| BWRITE-OPEN-01 Main enable 100 | PASS |
| BWRITE-OPEN-02 Remote +20 after Main 100 | PASS |
| BWRITE-ADJ-01 Remote +10 | PASS |
| BWRITE-COUNT-01 Remote count variance | PASS |
| BWRITE-DP-01 Remote direct purchase +30 | PASS |
| BWRITE-LEGACY-01 Primary materialize before +10 | PASS |

MB2-02A read regression (`BranchInventoryReadAuthorityIntegrationTests`): 6/6 PASS.

---

## React UX

- Direct Purchase receive: **Receiving into: {branch}** banner
- Direct Purchase detail: persisted receiving branch shown
- Stock count create: branch-scoped scope note
- Inventory adjustment: **Adjusting stock at: {branch}**
- Direct Purchase draft clears on workspace branch switch with user message
- i18n: en, fil-PH, ceb-PH, ilo-PH, hil-PH

---

## Tests

| Suite | Result |
|-------|--------|
| `BranchInventoryWriteAuthorityIntegrationTests` | 6 passed |
| `BranchInventoryReadAuthorityIntegrationTests` | 6 passed |
| `BranchBalanceMutationTests` + `BranchStockResolverTests` | Passed |
| `DirectPurchaseReceipt*` / `PurchaseReceiving*` / `StockCount*` unit | 46 passed (filter) |
| React DirectPurchaseDetail / InventoryDetail / ReceiveStock | 20 passed |
| React typecheck | Pass |
| POS API Release build | Pass |

---

## Deferred to MB2-02C

- Detailed lot/movement reconciliation reporting UI
- Full BWRITE security matrix dedicated API tests (BWRITE-SEC-*)
- Dedicated PostgreSQL concurrency suite (BWRITE-CONC-*)
- MB2-02D final inventory closure

---

## Next

**NEXT=MB2-02C** — lot/movement reconciliation polish, remaining scenario coverage, React hardening.

**Do not start MB2-02C** without explicit authorization.
