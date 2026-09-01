# POS-MULTI-BRANCH-V2 MB2-02B-H1 — Inventory Reservation, Primary, and Lot Hardening

**Program:** POS-MULTI-BRANCH-COMMERCE-V2  
**Package:** MB2-02B-H1  
**Branch:** `feat/organization`  
**Status:** COMPLETE_VALIDATED  
**Start SHA:** `1e012d63ea51aa2058e9b2c468c0d18c1a493d2b`

---

## Defects reproduced (pre-fix)

| ID | Finding | Confirmed |
|----|---------|-----------|
| H1-DEFECT-RESERVATION | `SaleStockService.ReserveForAwaitingPaymentAsync` / `CustomerOrderStockService.ReserveForAcceptAsync` applied signed **OnHand** deltas via `EnsureBalance` + `Apply` | YES |
| H1-DEFECT-UNKNOWN-PRIMARY-WRITE | `BranchStockResolver.EnsureBalance` seeded missing rows at **0** when `primaryBranchId` was null | YES |
| H1-DEFECT-PRIMARY-NPLUS1 | `BranchBalanceMutation.ApplyAsync` called `GetPrimaryBranchIdAsync` per product/line | YES |
| H1-DEFECT-MIXED-LEGACY-LOTS | Primary lot reads used `lots.Count == 0` before concatenating `ListOnHandAsync(..., branchId: null)`, which returns **all** lots (not null-only) and hid legitimate coexistence | YES |

Previous 02B double-count: concatenating Main lots with `branchId: null` (unfiltered) listed the same branch-scoped lots twice. Rule is **not** `count == 0`; it is **union by lot id** of branch-scoped + `BranchId IS NULL` rows, Primary only.

---

## Reservation model

| Field | Meaning |
|-------|---------|
| `InventoryAccount.OnHandQuantity` | Organization physical on-hand |
| `InventoryAccount.ReservedQuantity` | Organization reservation |
| `InventoryBranchBalance.OnHandQuantity` | Physical on-hand at one branch |
| `InventoryBranchBalance.ReservedQuantity` | Branch reservation (new) |
| Branch available | `OnHand − Reserved` |

Schema: `20260901120000_AddBranchInventoryReservations` adds `reserved_quantity` default 0 (no stock rewrite, no balance mass backfill, no fabricated historical reservations).

**H2 follow-up:** existing-row Upsert now persists `ReservedQuantity`. Cutover reconstruction is `20260901133000_ReconcileBranchInventoryReservations` + `IBranchInventoryReservationCutover` — see [MB2-02B-H2 report](POS-MULTI-BRANCH-V2-MB2-02B-H2-BRANCH-RESERVATION-PERSISTENCE-AND-CUTOVER.md).

Authority: `Sale.BranchId` / `CustomerOrder.FulfillmentBranchId`. Current workspace is not the physical consume branch after a switch.

Lifecycle:

- Immediate paid sale: OnHand − at sale branch; no persistent reservation required.
- Reserve: org + branch Reserved += qty; OnHand unchanged; no SaleDeduction movement.
- Pay/consume: org `ConsumeReservation`; branch `ConsumeReservation`; one physical movement.
- Cancel reserved: Reserved − only.
- Expiry reserved: lots consumed at consume/payment, not at reserve.

---

## Unknown Primary writes

| Target row | Primary | Behavior |
|------------|---------|----------|
| Explicit balance exists | unknown | Mutate that row |
| Missing | known | `EnsureBalance` as before |
| Missing | unknown | Fail `pos.inventory.primary_unavailable`; no account/balance/lot/movement |

---

## Primary lookup

Callers pass `primaryBranchId` into `BranchBalanceMutation`. `PosOrganizationBranchDirectory.GetPrimaryBranchIdAsync` is request-cached. Direct Purchase 30-line unit test: lookup count ≤ 1.

---

## Legacy lots (Option B — compatibility union)

Primary effective lots = branch-scoped ∪ remaining null-branch lots, dedup by `InventoryLot.Id`. Secondary never includes null lots. Unknown Primary never includes null lots. `AdoptOrgLevelLotsForBranchAsync` is no longer invoked on lot list GET (that was a write-on-read).

---

## Deferred

PostgreSQL concurrent reservation races (H1-CONC-*) remain covered by existing product reservation locks; dedicated dual-connection oversell proofs stay available for 02C if desired. Organization Inventory UI remains 02C.

**NEXT=MB2_02C** — HARD STOP.
