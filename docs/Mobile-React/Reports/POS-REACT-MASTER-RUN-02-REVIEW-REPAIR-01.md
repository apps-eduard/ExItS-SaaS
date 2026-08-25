# POS-REACT MASTER RUN 02 — REVIEW REPAIR 01

## Status

**COMPLETE** — four review findings closed in three commits. RMAP-14 React UI remains **not started**.

## Baseline

| Item | Value |
|------|-------|
| Starting HEAD | `a7e1322e` (clean, pushed) |
| Branch | `feat/pos-react-client` |
| Preflight | PASS |

## Findings repaired

### A — Expiry partial return restock (OWNER-APPROVED)

- Added `InventoryLotStockService.RestoreForSaleReturnAsync` (not blind `RestoreSourceAsync`):
  - Original `SaleDeduction` lots for the sale/product
  - Subtract prior `SaleReturnRestock` lot movements from prior returns of the same sale
  - Order by `ExpirationDate ASC` + lot number/id
  - Allocate only up to remaining restorable qty
  - Lot movements: `SaleReturnRestock`, `SourceType=SaleReturn`, `SourceId=saleReturnId`
  - Expired lots may receive restore; remain not sellable
- `SaleReturnStockService`:
  - Aggregate `ReturnToStock` lines by `ProductId`
  - Org account + branch delta (`originalSale.BranchId`; fail closed if branch missing when balances exist)
  - `TracksExpiration` → `RestoreForSaleReturnAsync`
  - `DoNotRestock` → no deltas
  - Idempotent on same return id
  - Historical account restock without lot evidence → `RMAP14_EXPIRY_RETURN_HISTORY_RECONCILIATION_GAP`

### B — Discounted partial refund NET fidelity

- Rewrote `SaleReturnRefundable.ComputeRefundAmount` to cumulative net `LineTotal` allocation (never `UnitPrice`).
- Final slice absorbs remainder via cumulative == `LineTotal`.

### C — Narrow checkout customer lookup

- `GET /api/v1/pos/customers/checkout-search?search=...`
- Authz: `CreateSale` (Cashier allowed); **not** `ViewCustomersAndHistory`
- Nonblank search; `pageSize` ≤ 20; Active only
- Narrow DTO: `customerId`, `displayName`, `mobileNumber?`, `status`
- Full `GET /customers` still requires `ViewCustomersAndHistory`
- React: Cashier Utang uses `searchCheckoutCustomers`; Cashier still denied `/customers` management

### D — Transaction Summary wording

Updated EN (and fil-PH equivalent) to the three-part business/BIR disclaimer across `SalesDocumentWording`, MAUI `PosResources`, React `messages.ts`, and guards/e2e.

## Commits

| # | SHA | Message |
|---|-----|---------|
| 1 | `39247c4e` | `fix(pos): harden partial return inventory and refund contracts` |
| 2 | `a55ca1af` | `fix(pos-react): close checkout customer and summary review findings` |
| 3 | `2364727c` | `docs(pos-react): record master run 02 review repair` |

## RMAP-14 status (unchanged for React)

| Flag | Value |
|------|-------|
| `RMAP14_BACKEND_CONTRACT_REPAIRED` | YES |
| `RMAP14_REACT_UI_NOT_STARTED` | YES |
| RMAP-14 package PASS | **NO** |

Do **not** start RMAP-14 React UI, RMAP-15, or migrations in this repair.

## RMAP-10b note

Owner decision remains **YES** — real browser/PWA PosDevice registration authorized; Development bypass rejected ([POS-REACT-RMAP-10b-browser-pos-device.md](./POS-REACT-RMAP-10b-browser-pos-device.md)).

## Validation (summary)

| Gate | Result |
|------|--------|
| Unit (return refund + lot restore + stock service) | PASS |
| Integration checkout-search Cashier vs full list | PASS |
| Maui SalesDocument guards | PASS |
| Vitest customers client | PASS |
| Playwright rmap-11 + rmap-12 (incl. Cashier Utang checkout-search + responsive) | PASS |
| Migrations | **NONE** (HARD STOP honored) |

## Exact next

Restart **RMAP-14 React returns UI only** against the repaired backend contract. Do not start RMAP-15 until RMAP-14 PASS.
