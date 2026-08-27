# Inventory Boundary

**Status:** Planning baseline (BNPL-00)  
**Implementation present:** No  
**Related:** BNPL-D-00-05, BNPL-D-00-06, BNPL-D-00-24

## Permanent rule

```text
Same Organization
+ Same Branch
+ Same Product
= Same authoritative inventory
```

Authoritative inventory owner: **Commerce / POS**.

BNPL **does not** own inventory and **must not** maintain a parallel stock ledger.

## How BNPL obtains stock facts

| Need | Mechanism |
|---|---|
| Product details | Commerce catalog contract |
| Current price (display / offer) | Commerce price contract (snapshot at activation) |
| Branch availability | Commerce availability contract |
| Final stock validation | Inside Commerce sale finalization |

## Concurrency

UI availability is informational. Concurrent cashiers can change stock before finalize. Finalize must fail closed on insufficient stock. BNPL then cancels/expires `APPROVED_PENDING_SALE` safely.

## Example

Initial Branch A iPhone 17 qty = 10 → POS cash sale −1 → 9 → BNPL sees 9 → BNPL financed qty 2 succeeds → 7 → POS sees 7.
