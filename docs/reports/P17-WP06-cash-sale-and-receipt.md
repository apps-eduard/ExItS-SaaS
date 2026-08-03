# P17-WP06 — Cash Sale and Receipt

| Field | Value |
|---|---|
| Status | **Complete** (reconciled + receipt enrichment) |
| Phase | [Phase 17](../phases/phase-17-pos-mvp-operational-onboarding-and-first-sale.md) |
| Final Phase 17 commit | See [P17-WP08](P17-WP08-reports-hardening-and-closeout.md) |
| Date | 2026-07-29 |

## Objective

Complete the first cash-sale journey with receipt generation, stock reduction, and duplicate protection.

## Existing functionality reused

- `CheckoutSale`, cart client, sale numbers, cash tender/change, idempotency, stock deduction, sales list/detail.
- ManualGCash and Product-Based Utang remain available but are not Phase 17 gates.

## Implementation summary

- Checkout applies operational setup tax when rate &gt; 0; persists `TaxAmount`.
- `PosSaleDto` enriched with register, store display name, currency, tax mode.
- Sale detail UI shows store, register, cashier id, tax, total, cash, change, and receipt header/address/phone when setup is complete.
- Unique sale/receipt number via existing sale number sequences.
- **Post-validation:** sale DTO enrichment includes receipt header/footer/address/phone from operational setup.

## Files / components changed

- `Sale.cs`, `SaleRecord`, mappers, `SaleUseCases`, `SaleClientDtos`
- `SaleDetail.razor` + localization keys
- Migration column `sales.tax_amount`

## Authorization and isolation behavior

- CreateSale / ViewSales capabilities; org-scoped repositories.
- Duplicate submission: existing idempotency service on checkout.

## Tests executed and results

- Existing sale checkout / stock / idempotency integration tests.
- Tax calculator unit tests.

## Deferred items

- Payment gateway integrations; split tender; printable fiscal invoices; cashier display name directory (actor GUID shown).

## Commit reference

Final Phase 17 commit recorded in P17-WP08.
