# Persistence and Database Boundary

**Status:** Planning baseline (PSP-00)  
**Implementation present:** No  
**Related decision:** PSP-D-00-02

## Proposed database

| Item | Value | Status |
|---|---|---|
| Logical database name | `ExItS_PinoyServicePro` | Proposed only |
| Schema name | — | Open / Product Owner Decision Required |
| Created in PSP-00? | **No** | Required |

## Isolation verification (documentation)

- PinoyServicePro does not read PinoyBusinessPOS DB
- PinoyServicePro does not read PinoyLoanManager DB
- PinoyBusinessPOS does not own ServicePro operational data
- PinoyLoanManager does not own ServicePro operational data
- Platform does not own ServicePro operational records
- OrganizationId crosses boundaries only through approved identifiers/contracts
- No cross-product foreign keys
- No direct Platform table reads from this product
- No EAV/dynamic arbitrary schema generation as primary operational model

## Migrations

Migrations are forbidden until an authorized persistence work package. Do not auto-apply production migrations at API startup.
