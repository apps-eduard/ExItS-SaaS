# Returns, Cancellations, and Refunds

**Status:** Planning baseline (BNPL-00)  
**Implementation present:** No  
**Related:** BNPL-D-00-17

## Cross-domain workflow

Example: BNPL financed sale → customer returns item.

| Domain | Owns |
|---|---|
| **Commerce** | Return of goods, inventory restoration where valid, sale return record |
| **BNPL** | Financing adjustment/cancellation, repayment consequences, outstanding balance impact |

## Coordination rules

- Commerce must **not** directly edit BNPL financing database.  
- BNPL must **not** directly edit POS stock tables.  
- Coordination uses approved contracts/events with idempotent handlers.  
- Refund allocation between down payment, repaid installments, and remaining principal is **Open** (BNPL-D-00-17).

## Cancellation before ACTIVE

If financing is CANCELLED before commerce sale, inventory must remain unchanged and no ACTIVE financing exists.

## Cancellation / void after ACTIVE

Requires dual-domain controlled workflow. Prefer compensating transactions over silent deletes. Exact policy Open.
