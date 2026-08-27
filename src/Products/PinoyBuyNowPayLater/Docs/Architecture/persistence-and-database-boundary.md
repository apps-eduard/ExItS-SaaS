# Persistence and Database Boundary

**Status:** Planning baseline (BNPL-00)  
**Implementation present:** No  
**Related:** BNPL-D-00-04, BNPL-D-00-12

## Proposed database

| Item | Value | Status |
|---|---|---|
| Logical database name | `ExItS_PinoyBuyNowPayLater` | Proposed only |
| Schema name | — | Open |
| Created in BNPL-00? | **No** | Required |

Sharing an **identity reference** (Guid) is allowed. Sharing **database ownership** is not.

## Isolation verification (documentation)

- BNPL does not read PinoyBusinessPOS DB  
- BNPL does not read PinoyLoanManager DB  
- BNPL does not read PinoyServicePro DB  
- POS / PLM / PSP do not own BNPL financing data  
- Platform does not own BNPL operational financing records  
- OrganizationId / BranchId / ProductId / SaleId cross boundaries only as identifiers/contracts  
- No cross-product foreign keys  
- No cross-product EF navigation properties  

## Migrations

Forbidden until an authorized persistence work package. Do not auto-apply production migrations at API startup (`Migrate()` forbidden on production paths).
