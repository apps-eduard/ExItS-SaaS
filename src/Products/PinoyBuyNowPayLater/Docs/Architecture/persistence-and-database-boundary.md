# Persistence and Database Boundary

**Status:** Customer + financing application persistence (BNPL-03/04)
**Implementation present:** Yes — customers + financing applications/offers/decisions
**Related:** BNPL-D-00-04, BNPL-D-00-12

## Database

| Item | Value | Status |
|---|---|---|
| Logical database name | `ExItS_PinoyBuyNowPayLater` | BNPL-D-00-04 implemented |
| Schema name | `bnpl` | Implemented |
| Connection string name | `BnplDatabase` | Implemented |
| DbContext | `BnplDbContext` | Implemented |
| Migrations | `InitialBnplCustomerFoundation`, `AddBnplFinancingApplicationLifecycle` | Implemented |
| Production auto-migrate | **Forbidden** | No `Database.Migrate()` on API startup |

## Tables

| Table | Package |
|---|---|
| `bnpl.customers` | BNPL-03 |
| `bnpl.financing_applications` | BNPL-04 |
| `bnpl.financing_offers` | BNPL-04 |
| `bnpl.financing_decisions` | BNPL-04 |

No installments / repayments / settlements / inventory / sales tables.

## Isolation

- Customer FK is BNPL-local only (`fk_bnpl_financing_applications_customer`)
- No cross-product FKs or EF navigations to POS/Platform/PLM entities
