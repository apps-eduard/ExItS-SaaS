# Customer Model

**Status:** Planning baseline (PSP-00)  
**Implementation present:** No

## Ownership

Customer records for service businesses are **PinoyServicePro–owned** operational data.

| Not the same as | Reason |
|---|---|
| PinoyBusinessPOS Customer | Separate product SoR; no DB reads/FKs |
| PinoyLoanManager Borrower | Separate product SoR |
| ExItS Personal identity | Platform presentation/identity; linking only if later authorized |

## Expected attributes (planning, not schema)

- Name
- Contact information
- Address where applicable
- Organization/branch association rules (open)
- Notes (non-PHI)

Exact required fields vary by template (required vs optional field configuration).

## Isolation and authorization

Customers are tenant-isolated. Access via product-local grants (`customers` area intent). Cross-org concealment applies.

## Personal / customer-facing future

Optional future linking or Personal booking presentation must not auto-merge identities without explicit consent design. See PSP-D-00-05 / PSP-D-00-13. Not in PSP-00 implementation scope.
