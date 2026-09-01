# POS-MULTI-BRANCH-V2 MB2-02B-H3 — Exact Reservation Projection and Write Authority Closure

**Program:** POS-MULTI-BRANCH-COMMERCE-V2  
**Package:** MB2-02B-H3  
**Branch:** `feat/organization`  
**Status:** COMPLETE_VALIDATED  
**Start SHA:** `8cd60951a33b07d6918f1860c1b90c96fd792fcc`

---

## P0 defect (post-H2 review)

H2 `BranchInventoryReservationCutover` and migration `20260901133000_ReconcileBranchInventoryReservations` only updated branch balances appearing in the **current active-reservation aggregate**. Stale non-zero `InventoryBranchBalance.ReservedQuantity` survived when no active Reserved Sale/CustomerOrder existed for that branch/product.

**Example before H3:**

| Layer | OnHand | Reserved |
|-------|--------|----------|
| Org | 100 | 0 |
| Main | 100 | 10 (stale) |

Active Reserved Sales = 0. Active Reserved CustomerOrders = 0. H2 reconcile reported success but left Main Reserved = 10.

**H3 invariant (locked):**

```
BranchReserved(org, branch, product)
  = SUM(active authoritative reservation qty for that branch/product)
  OR 0 when no active reservation exists
```

---

## Corrective implementation

### C# reconciler

`BranchInventoryReservationCutover.RunAsync` (write path):

1. Build authoritative active-reservation aggregate (Reserved Sales + CustomerOrders, tracked lines only).
2. Fail closed: unresolved sale `BranchId`, unresolved order `FulfillmentBranchId`, missing balance, over-reserved, org `ReservedQuantity` ≠ doc sum.
3. Load **all** `InventoryBranchBalance` rows in scope (org-filtered or global).
4. For each balance: `desiredReserved = aggregate.Get(key) ?? 0`.
5. Update mismatched rows; single `SaveChanges`. OnHand unchanged.

`AuditAsync(write:false)` reports mismatches via `MismatchedBalanceCount`; no mutations.

### PostgreSQL migration

`20260901143000_ExactProjectBranchInventoryReservations` — data correction only (no schema change):

- Same validations as C# reconciler.
- `UPDATE pos.inventory_branch_balances SET reserved_quantity = COALESCE(active_aggregate, 0)` for **all** rows in scope (not `UPDATE … FROM aggregate` only).
- Atomic transaction; failure rolls back with stale values intact.
- **Down:** documented no-op — stale pre-H3 values are invalid and must not be restored.
- H1/H2 pushed migrations unchanged.

---

## Authoritative active reservation sources (unchanged from H2)

| Source | Active state | Branch authority | Quantity |
|--------|--------------|------------------|----------|
| Sale | `StockReservationState = Reserved` | `Sale.BranchId` (required) | tracked `SaleLine` qty |
| CustomerOrder | `StockReservationState = Reserved` | `FulfillmentBranchId` (required) | tracked `CustomerOrderLine` qty |

Excluded: None, Released, Consumed, Cancelled, Completed, Void.

CustomerOrder → Sale conversion: consumed order not double-counted with reserved sale.

---

## Protected baseline (MB2-02B closure)

| Concept | Definition |
|---------|------------|
| Organization product | One canonical `ProductId` per org |
| `InventoryAccount` | Org aggregate/control projection |
| `InventoryBranchBalance` | Physical branch OnHand + branch Reserved |
| Branch Available | OnHand − Reserved |
| Transfer | Moves physical ownership; org net delta = 0 |
| Sale | Reduces selling branch + org |
| Reservation | Does not reduce physical OnHand until consumed |
| New branch | Zero stock unless legitimate physical event |
| Legacy unallocated | Structural Primary compatibility only |
| Unknown Primary | Fail closed when materialization/provenance ambiguous |

---

## Test evidence (PostgreSQL/Testcontainers)

**H3 scenarios:** H3-STALE-01/02/03, H3-PROJECTION-01, H3-SCOPE-01, H3-IDEMPOTENT-01, H3-ATOMIC-01, H3-LIFECYCLE-01/02, H3-RESTART-01/02, Mica Store lifecycle + stale reconciliation.

**Migration scenarios:** H3-MIGRATION-01/02/03, H3-MIGRATION-FAIL-01.

**H2 regression:** 19 persistence/cutover/migration tests pass (Upsert ReservedQuantity, restart E2E, double-count guard).

**H1 regression:** 16 unit tests (BranchBalanceMutation, lot compatibility, SaleStockService, CustomerOrderStockService).

---

## Mica Store closure scenario

Org Coke 1L: Main 70, Mica A 5, Mica B 10 (org 85). Mica A reserves 4 → org Reserved 4, Mica A Available 1. Cancel → org Reserved 0. Re-reserve 4 and pay → org OnHand 81 (70+1+10). Stale Mica A Reserved=4 with org Reserved=0 cleared to 0 by reconcile.

---

## Deferred P2 (explicit)

- Dedicated PostgreSQL dual-connection concurrent oversell proof.
- Dedicated CustomerOrder complete-after-restart path.

---

## Closure decision

| Package | Status |
|---------|--------|
| MB2-02B-H3 | COMPLETE_VALIDATED |
| MB2-02B-H2 | COMPLETE_VALIDATED (H3 stale-projection correction applied) |
| MB2-02B-H1 | COMPLETE_VALIDATED |
| MB2-02B | COMPLETE_VALIDATED_WRITE_AUTHORITY |

**NEXT=MB2_02C** — **HARD STOP** (do not start without authorization).
