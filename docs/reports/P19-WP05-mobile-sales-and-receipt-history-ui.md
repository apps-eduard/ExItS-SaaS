# P19-WP05 — Mobile Sales and Receipt History UI

| Field | Value |
|---|---|
| Status | **Code Complete** |
| Phase | [Phase 19](../phases/phase-19-mobile-pos-operations-and-cashier-experience.md) — **Open** |
| Commit | _(filled after commit)_ |
| Production-ready | **No** |
| Device Verified | **No** |
| Date | 2026-08-04 |

## 1. Objective

Complete sales history list/detail and post-sale receipt surface with next-sale CTA.

## 2. Existing reuse

`IPosSaleClient` list/get, existing SalesList/SaleDetail filters and void/return gates.

## 3. Delivered

- Sales list with filters, paging, offline/error/retry (existing, retained)
- Sale detail Receipt action for completed sales when ViewGenerateReceipt or ViewSales
- New SaleReceipt page: header, lines, totals, tender/change, next sale / back

## 4. Residuals

- Dedicated thermal printer integration out of scope
- Receipt reprint uses same GetAsync sale payload

## 5. Tests

`SalesCashierPageGuardTests` — receipt route and detail receipt link.

## 6. Authorization

ViewSales for receipt/history; CreateSale for new sale CTA.

## 7. Status

**Code Complete.** Phase 19 remains **Open**. Not Device Verified.
