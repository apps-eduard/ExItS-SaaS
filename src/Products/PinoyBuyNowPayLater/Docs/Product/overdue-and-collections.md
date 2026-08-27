# Overdue and Collections

**Status:** Planning baseline (BNPL-00)  
**Implementation present:** No  

## Baseline concepts

| Concept | Notes |
|---|---|
| Due | Installment due date reached |
| Overdue | Past due unpaid amount |
| Days overdue | Derived metric |
| Collection queue | Staff worklist |
| Reminders | Notification intent — channels Open |
| Customer communication | Policy + privacy constraints |
| Collector / manual follow-up | May be supported later |
| Promises to pay | Optional future concept |
| Write-off / default | Open — requires policy (see lifecycle) |

## Boundary with PLM

Do **not** automatically import PinoyLoanManager collector architecture, device policies, or remittance models. BNPL collection needs must be evaluated separately. Shared primitives may be considered later without shared operational ownership.

## POS independence

Overdue calculation and collection workflows for existing ACTIVE plans must not require POS inventory/sale APIs.
