# RMAP-14 — Returns / Refunds

## Status

**PASS** — React returns / refunds UI delivered against the repaired backend contract.

| Flag | Value |
|------|-------|
| `RMAP14_BACKEND_CONTRACT_REPAIRED` | YES |
| `RMAP14_BACKEND_READY_FOR_REACT_RESTART` | YES |
| `BACKEND_READY` | YES |
| `REACT_UI_STARTED` / `RMAP14_REACT_UI_NOT_STARTED` | **YES** / NO |
| `RMAP14_RETURN_CONCURRENCY_GAP` | **CLOSED** |
| `RMAP14_RETURN_VOID_RACE_GAP` | **CLOSED** |
| `RMAP_14_FINAL` | **APPROVED** |
| `RMAP_15_AUTHORIZED` | **NO** |
| `RMAP_B01_AUTHORIZED` | NO |
| `RMAP_12B_AUTHORIZED` | NO |
| `RMAP_B04_AUTHORIZED` | NO |
| `RMAP_TAX_AUTHORIZED` | NO |
| `PRODUCTION_CUTOVER` | NO |

## Baseline

| Item | Value |
|------|-------|
| Starting HEAD | `85dba1e81e7b8e8c30ff3077cceffd2cc521cfe3` |
| Branch | `feat/pos-react-client` |
| Run ID | `POS-REACT-RMAP-14-FINAL` |
| Repair 01 | [POS-REACT-MASTER-RUN-02-REVIEW-REPAIR-01.md](./POS-REACT-MASTER-RUN-02-REVIEW-REPAIR-01.md) |
| Repair 02 | [POS-REACT-MASTER-RUN-02-REVIEW-REPAIR-02.md](./POS-REACT-MASTER-RUN-02-REVIEW-REPAIR-02.md) |

## Delivered React capability

- Typed client: `pos-sale-returns-client.ts` — refundable GET, list/get returns, POST create with optional `returnId`
- Optional **Estimated refund** helper from refundable NET fields only (cumulative proportional); POST `totalRefundAmount` always wins
- Capabilities: `canViewReturns` (includes Cashier), `canProcessReturn` (Owner/Manager only)
- Guards: `RequireViewReturns`, `RequireProcessReturn`
- Routes: `/returns`, `/returns/sale/:saleId`, `/returns/:returnId`
- Role home **Returns** link (ViewReturns)
- Transaction Summary **Return items** CTA when `canProcessReturn` and sale not voided
- UX: search by transaction number, quantity steppers / ByWeight decimals, Return all, Put back in stock / Do not return to stock, required reason, confirmation, success wording for Cash / GCash / Utang
- Stale/409: refresh refundable, clear over-max qty, user re-confirms (no silent clamp)
- Cash no open shift: friendly block (not treated as stale concurrency)
- No lot selection; no Invoice labels; Cashier cannot ProcessReturn
- i18n `returns.*` en + fil-PH

## Tests

| Suite | Result |
|-------|--------|
| Vitest (client, caps, estimate, quantity, guards) | PASS |
| Playwright `rmap-14-returns-refunds.spec.ts` matrix A–N + responsive 375/768/1024/1440 | PASS |
| Regression rmap-11, 11b, 12, 13 | PASS |
| format:check / typecheck / lint / build | PASS |

## Backend contract (unchanged)

- `GET /api/v1/pos/sale-returns/refundable/{saleId}`
- `POST /api/v1/pos/sale-returns` `{ saleId, reason, lines[{saleLineId,quantity,restockDisposition,lineReason?}], notes?, returnId? }`
- `RestockDisposition`: `ReturnToStock` \| `DoNotRestock`
- List/get returns; ProcessReturn Owner/Manager; ViewReturns includes Cashier
- Device header via `pos-http`; concurrency gaps remain CLOSED from Review Repair 02

## Exclusions / not delivered

- RMAP-15+
- RMAP-B01 / RMAP-12b / RMAP-B04 / RMAP-TAX
- Offline returns
- Lot invent / fake restore / provider GCash
- New DB migration / backend redesign
- Invoice / Official Receipt labeling

## Exact next

**HARD STOP.** Product Owner + ChatGPT review. Do **not** start RMAP-15 until authorized (`RMAP_15_AUTHORIZED=NO`).
