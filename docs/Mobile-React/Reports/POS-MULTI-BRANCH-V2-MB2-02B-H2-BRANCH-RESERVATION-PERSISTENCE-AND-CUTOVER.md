# POS-MULTI-BRANCH-V2 MB2-02B-H2 — Branch Reservation Persistence and Cutover

**Program:** POS-MULTI-BRANCH-COMMERCE-V2  
**Package:** MB2-02B-H2  
**Branch:** `feat/organization`  
**Status:** COMPLETE_VALIDATED (H3 exact-projection correction applied)
**Start SHA:** `3331f735e70e8afce82c17a270c65e5b6e4a844c`

---

## H3 follow-up (stale projection correction)

Independent review found H2 reconciler/migration updated only branch balances in the active aggregate. Stale non-zero `ReservedQuantity` could survive with zero active reservations. **H3** (`20260901143000_ExactProjectBranchInventoryReservations` + reconciler rewrite) sets every in-scope balance to `COALESCE(active aggregate, 0)`. See [MB2-02B-H3 report](POS-MULTI-BRANCH-V2-MB2-02B-H3-EXACT-RESERVATION-PROJECTION-AND-WRITE-AUTHORITY-CLOSURE.md).

---

## P0 defect (post-H1 review)

`InventoryBranchBalanceRepository.UpsertAsync` INSERT path used `ToRecord` (includes `ReservedQuantity`), but the **existing-row UPDATE** path set only `OnHandQuantity` and `UpdatedAtUtc`. Reservations appeared correct in-memory and failed after `SaveChanges` + fresh `DbContext`.

**Fix:** `InventoryTransferEntityMapper.ApplyToRecord` persists `OnHandQuantity`, `ReservedQuantity`, and `UpdatedAtUtc` on UPDATE.

---

## Cutover audit (Strategy A — deterministic)

| Source | Active state | Branch authority |
|--------|--------------|------------------|
| Sale | `StockReservationState = Reserved` | `Sale.BranchId` (required; null → fail closed) |
| CustomerOrder | `StockReservationState = Reserved` | `FulfillmentBranchId` |

**Order → sale:** `CustomerOrderStockService.ConsumeOnCompleteAsync` marks the order **Consumed** and writes `CustomerOrderDeduction`. It does not leave the order Reserved while creating a Sale Reserved for the same hold. Cutover counts `Reserved` documents only → no double-count.

**Tracked products only** (same as reserve path).

---

## Cutover implementation

1. **EF migration** `20260901133000_ReconcileBranchInventoryReservations`  
   - Reconstructs `reserved_quantity` from active documents  
   - Fails on unresolved sale branch / missing balance / over-reserve / org≠sum(docs)  
   - Does not change OnHand, ProductId, movements, or sale/order status  
   - **Down:** `SET reserved_quantity = 0` (H1 default); OnHand unchanged  
   - Does **not** rewrite the pushed H1 column migration

2. **C#** `IBranchInventoryReservationCutover` / `BranchInventoryReservationCutover`  
   - Same rules for tests and org-scoped re-runs  
   - Optional `organizationId` filter (migration remains global)

---

## Formulae

- `BranchReserved(org, branch, product)` = Σ line qty of active Reserved Sales/Orders for that branch/product (tracked)  
- `OrgReserved(product)` must equal Σ branch-attributable active doc qty for that product  
- Never assign unknown branch to Main  
- Never invent OnHand / zero-seed missing balances for reservations

---

## Concurrency note

`inventory_branch_balances` has **no** xmin token (unlike reorder settings). Protection remains product reservation locks, transactions, and DB check constraints (`reserved >= 0`, `reserved <= on_hand`).

---

## Tests

PostgreSQL/Testcontainers: repository round-trips, SaleStockService / CustomerOrderStockService restart E2E, cutover scenarios, migration apply/rollback/reapply + fail-closed.

**NEXT=MB2_02C** — HARD STOP.
