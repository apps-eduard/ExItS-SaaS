# RMAP-14 — Returns / Refunds

## Status

**NOT PASS** — backend contract repaired; React UI not started.

| Flag | Value |
|------|-------|
| `RMAP14_BACKEND_CONTRACT_REPAIRED` | YES |
| `RMAP14_REACT_UI_NOT_STARTED` | YES |
| Former code `RMAP14_EXPIRY_RETURN_CONTRACT_GAP` | **CLEARED** by Master Run 02 Review Repair 01 |

## Baseline

| Item | Value |
|------|-------|
| Hard-stop docs HEAD | `a7e1322e` |
| Repair | [POS-REACT-MASTER-RUN-02-REVIEW-REPAIR-01.md](./POS-REACT-MASTER-RUN-02-REVIEW-REPAIR-01.md) |
| Branch | `feat/pos-react-client` |

## Backend contract (repaired)

### Expiry partial return restock

`SaleReturnStockService` + `InventoryLotStockService.RestoreForSaleReturnAsync`:

- Aggregate `ReturnToStock` by product; org account + branch delta on `originalSale.BranchId`
- For `TracksExpiration`: restore only to original sale-consumed lots, earliest expiration first, net of prior `SaleReturnRestock` lot movements
- Lot movement: `SaleReturnRestock` / `SourceType=SaleReturn` / `SourceId=saleReturnId`
- Historical account restock without lot evidence → fail closed `RMAP14_EXPIRY_RETURN_HISTORY_RECONCILIATION_GAP`
- `DoNotRestock`: no account/branch/lot deltas
- Idempotent on same return id

### Discounted partial refund NET fidelity

`SaleReturnRefundable.ComputeRefundAmount` uses cumulative net `LineTotal` allocation (never `UnitPrice`); final slice absorbs remainder.

### Utang return

`ReduceForSaleReturn` path unchanged — not the former stopper.

## Exclusions / not delivered

- Partial/full return **React** UI
- Refund amount UX
- Inventory restore UX
- Any React return POST
- Lot invent / fake restore
- New DB migration
- RMAP-15

## Exact next

Implement **RMAP-14 React returns UI only** against this contract. Do not start RMAP-15 until RMAP-14 PASS.
