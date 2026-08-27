# Persistence and Database Boundary

**Status:** Initial persistence implemented (BNPL-03)
**Implementation present:** Yes — customer foundation only
**Related:** BNPL-D-00-04, BNPL-D-00-12

## Database

| Item | Value | Status |
|---|---|---|
| Logical database name | `ExItS_PinoyBuyNowPayLater` | **BNPL-D-00-04 Provisionally Approved / Implemented in BNPL-03** |
| Schema name | `bnpl` | Implemented (product-local schema convention) |
| Connection string name | `BnplDatabase` | Implemented |
| DbContext | `BnplDbContext` | Implemented |
| Initial migration | `InitialBnplCustomerFoundation` | Implemented |
| Production auto-migrate | **Forbidden** | No `Database.Migrate()` on API startup |

## Tables (BNPL-03)

| Table | Purpose |
|---|---|
| `bnpl.customers` | Organization-scoped BNPL customer profiles + optional external id columns |

No financing / installment / repayment / settlement tables.

## Isolation verification

- BNPL does not read PinoyBusinessPOS DB
- BNPL does not read PinoyLoanManager DB
- BNPL does not read PinoyServicePro DB
- No cross-product foreign keys
- No cross-product EF navigation properties
- External identity columns are opaque Guids/strings only
