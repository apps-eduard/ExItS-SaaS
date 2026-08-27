# BNPL-01 — Product Scaffold + Platform Registration

| Field | Value |
|---|---|
| Task | BNPL-01 |
| Branch | `feat/bnpl` |
| Status | Complete |
| Date | 2026-08-27 |
| Implementation present | Scaffold only (no financing domain) |

## Delivered

- Product projects: Domain, Application, Infrastructure, Api (+ health)
- Unit tests: assembly smoke, product identity, health endpoint, no financing types
- Architecture isolation tests (no POS/PLM/PSP operational project refs; no EF/migrations)
- Platform `ProductCode.PinoyBuyNowPayLater` = `pinoy-buy-now-pay-later`
- Local Validation catalog: `EnsureBnplLocalValidationCatalog` (Dev/Testing only; zero-price plan; empty grants)
- Seed wiring for ABC Sari-Sari (Maria/Carlo product access) independent of POS/PLM subscriptions
- Solution registration in `ExItS.slnx`

## Explicit exclusions

- No FinancingPlan / Installment / Repayment / Settlement / Application entities
- No DbContext, database, or migrations (BNPL-D-00-04 remains OPEN)
- No commerce/POS integration endpoints
- No BNPL product-local grants/roles (BNPL-D-00-18 remains OPEN)
- No ApiClient / Blazor Web (BNPL client direction is React/PWA; deferred)
- No production pricing or commercial policy claims

## Decision updates

| ID | Status after BNPL-01 |
|---|---|
| BNPL-D-00-01 | Provisionally Approved for Implementation by Product Owner in BNPL-01 |
| BNPL-D-00-02 | Provisionally Approved for Implementation by Product Owner in BNPL-01 |
| BNPL-D-00-03 | Provisionally Approved for Implementation by Product Owner in BNPL-01 |
| BNPL-D-00-04 | **OPEN** (database name) |

## Evidence

- BNPL unit tests PASS
- Architecture BNPL tests PASS
- Platform ProductCode + EnsureBnpl + identity catalog tests PASS

## Next package

**BNPL-02 — Authorization + Organization/Branch Access**
