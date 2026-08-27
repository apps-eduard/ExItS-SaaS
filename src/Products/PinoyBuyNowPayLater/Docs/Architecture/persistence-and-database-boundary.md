# Persistence and Database Boundary

**Status:** Customer + financing + installment plan persistence (BNPL-03/04/05)
**Implementation present:** Yes — customers, financing applications/offers/decisions, installment plans/items
**Related:** BNPL-D-00-04, BNPL-D-00-12

## Database

| Item | Value | Status |
|---|---|---|
| Logical database name | `ExItS_PinoyBuyNowPayLater` | BNPL-D-00-04 implemented |
| Schema name | `bnpl` | Implemented |
| Connection string name | `BnplDatabase` | Implemented |
| DbContext | `BnplDbContext` | Implemented |
| Migrations | `InitialBnplCustomerFoundation`, `AddBnplFinancingApplicationLifecycle`, `AddBnplInstallmentPlanFoundation` | Implemented |
| Production auto-migrate | **Forbidden** | No `Database.Migrate()` on API startup |

## Tables

| Table | Package |
|---|---|
| `bnpl.customers` | BNPL-03 |
| `bnpl.financing_applications` | BNPL-04 |
| `bnpl.financing_offers` | BNPL-04 |
| `bnpl.financing_decisions` | BNPL-04 |
| `bnpl.installment_plans` | BNPL-05 |
| `bnpl.installment_plan_items` | BNPL-05 |

No repayments / settlements / inventory / sales / collectible-debt tables.

## Isolation

- Customer FK is BNPL-local only (`fk_bnpl_financing_applications_customer`)
- Installment plan FK to offer is BNPL-local only (`fk_bnpl_installment_plans_offer`)
- No cross-product FKs or EF navigations to POS/Platform/PLM entities
