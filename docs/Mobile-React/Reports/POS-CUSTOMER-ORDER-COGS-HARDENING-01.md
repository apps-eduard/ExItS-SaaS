# POS-CUSTOMER-ORDER-COGS-HARDENING-01

**Status:** COMPLETE  
**Branch:** `feat/organization`  
**TASK:** POS-CUSTOMER-ORDER-COGS-HARDENING-01  
**START_SHA:** `a4416f283f868048db99e757c268e58e6de66907`  
**FEATURE_SHA:** `ea9334be2708efe5cf554cc46dfa54701166b7e6`

## CURRENT_CUSTOMER_ORDER_COST_MODEL

Before this package, completed **Personal Utang** customer orders posted one accounting settlement sale via `CustomerOrderUtangLedgerService` with `InventoryCostResolver.EnrichDraftsWithCostsAsync` and `Sale.RecordCustomerOrderUtangSettlement`. Inventory was consumed earlier via `CustomerOrderStockService` (`StockMovementType.CustomerOrderDeduction`). **Cash** and **ManualGCash** completed orders posted **no** settlement sale, so `SaleLine.UnitCostSnapshot` / profitability aggregates never saw those fulfilled orders.

## ROOT_CAUSE

`CustomerOrderUtangLedgerService.PostOnCompleteIfNeededAsync` returned early unless `PaymentMethod == Utang && PartyType == Personal && Status == Completed`. Cash and ManualGCash completions therefore never entered the canonical Sale/SaleLine COGS snapshot path even though stock was already deducted.

Secondary: delivery-fee settlement lines reused the first product id; batch cost enrichment assigned product acquisition cost to the fee line (COGS inflation).

## FULFILLMENT_TO_SALE_MODEL

```
CompleteCustomerOrder
  → order.Complete()
  → CustomerOrderStockService.ConsumeOnCompleteAsync()   // single inventory consumption
  → CustomerOrderUtangLedgerService.PostOnCompleteIfNeededAsync()
      → if Status != Completed: return
      → idempotent SaleId = CustomerOrderUtangSettlementIds.SaleIdForOrder
      → EnrichOrderLineDraftsWithCostsAsync (inventory lines only)
      → Sale.RecordCustomerOrderSettlement(..., StockReservationState.Consumed)
      → CreditEntry only when Utang + Personal + linked POS customer
```

## AUTHORITATIVE_COST_SOURCE

Existing `InventoryCostResolver` → `GetLatestAcquisitionUnitCostsAsync` (OpeningStock, PurchaseReceipt, DirectPurchaseReceipt, ProductionOutput). Same resolver as POS checkout. Org-scoped acquisition cost (existing model).

## COST_RESOLUTION_POINT

At settlement sale creation inside `CustomerOrderUtangLedgerService`, immediately before `Sale.RecordCustomerOrderSettlement`, using batch enrichment for product lines only.

## UNIT_COST_SNAPSHOT_POLICY

Known authoritative acquisition cost → `SaleLine.UnitCostSnapshot`. Unknown → `null` (never `0`).

## LINE_COST_SNAPSHOT_POLICY

`RoundMoney(UnitCostSnapshot × sold quantity)` when unit cost known; else `null`. Delivery fee lines excluded from enrichment → remain `null`.

## SALE_COST_STATUS_POLICY

`ProductionCostStatuses.FromMaterialCosts` on sale lines → `Complete` / `Partial` / `Unavailable`; `TotalCostSnapshot` sums known line costs only.

## INVENTORY_DEDUCTION_POLICY

Single consumption at order completion via customer-order stock path. Settlement sale uses `SaleStockReservationState.Consumed` (accounting-only).

## DOUBLE_DEDUCTION_GUARD

Settlement sale never calls `DeductForSaleAsync`. `StockReservationState.Consumed` preserves existing guard semantics.

## IDEMPOTENCY_POLICY

Deterministic `SaleIdForOrder`; early return when sale exists; persistence conflict race handled without duplicate sale/credit.

## CASH_POLICY

Completed Cash orders post settlement sale with `SalePaymentMethod.Cash`, tender = total, no credit entry. COGS snapshots identical resolver path.

## GCASH_POLICY

Completed ManualGCash orders post settlement sale with `SalePaymentMethod.ManualGCash`, no tender fields, no credit entry.

## UTANG_POLICY

Unchanged Business Utang charge: Personal + linked customer + credit entry. Sale COGS enriched the same way.

## RETURN_POLICY

Returns continue to use original `SaleLine.UnitCostSnapshot` from the settlement sale (repository aggregate logic unchanged).

## CANCELLATION_POLICY

Non-completed orders do not post settlement sales (no COGS).

## BRANCH_SCOPE

Settlement sale records `branchId` from `order.FulfillmentBranchId`. Cost resolver remains org-scoped (same as direct checkout).

## INVALID_BRANCH_FAILS_CLOSED

Existing order branch validation unchanged at order creation/fulfillment.

## CROSS_ORG_GUARD

Settlement sale uses order `SellerOrganizationId`; resolver queries same org only.

## CUSTOMER_ORDER_COST_QUERY_MODEL

Batch `GetLatestAcquisitionUnitCostsAsync` per settlement (inventory product lines only).

## N_PLUS_ONE

PASS — single batch lookup per multi-line settlement.

## CUSTOMER_COST_VISIBILITY

NONE — customer order DTOs unchanged; existing guard test retained.

## BACKEND_TESTS

- `CustomerOrderUtangLedgerServiceTests` — Cash/GCash post sale without credit; Utang unchanged
- `CustomerOrderSettlementCogsTests` — snapshots, Partial/Unavailable, delivery fee exclusion, idempotency, batch resolver count, profitability/return snapshot usage
- `SaleCostProfitTests` — existing checkout COGS + customer DTO guard

## REACT_TARGETED_TESTS

No React code changes required.

## REACT_FULL_SUITE

TOTAL=1256 PASS=1256 FAIL=0

## TYPECHECK

PASS

## LINT

PASS (0 errors; pre-existing warnings)

## BUILD

PASS

## MIGRATION

N/A — existing `Sale` / `SaleLine` snapshot columns.

## DOC_MOJIBAKE_CLEANUP

Fixed encoding in `POS-INVENTORY-PERMISSION-I18N-POLISH-01.md` (`—`, `≡`, `→`).

## NEXT

POS-ORGANIZATION-REMAINING-GAPS-AUDIT-02
