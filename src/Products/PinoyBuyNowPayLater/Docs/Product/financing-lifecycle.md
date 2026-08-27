# Financing Lifecycle

**Status:** Implemented through APPROVED_PENDING_SALE (BNPL-04)
**Implementation present:** Yes — `BnplFinancingApplication`
**Related:** BNPL-D-00-07, BNPL-D-00-22–24

## Implemented state machine (BNPL-04)

| State | Meaning | Debt? | Sale? | Inventory? | Repayments? |
|---|---|---|---|---|---|
| `Draft` | Application started | No | No | No | No |
| `PendingEligibility` | Awaiting eligibility | No | No | No | No |
| `Offered` | Concrete offer issued | No | No | No | No |
| `CustomerAccepted` | Current offer accepted | No | No | No | No |
| `ApprovedPendingSale` | Approved; Commerce sale not done | No | No | No | No |
| `Declined` | Terminal non-ACTIVE | No | No | No | No |
| `Cancelled` | Cancelled before ACTIVE | No | No | No | No |

## Transitions (implemented)

| From | To | Command |
|---|---|---|
| Draft | PendingEligibility | Submit |
| PendingEligibility | Offered | CreateOffer (after eligibility approve) |
| PendingEligibility | Declined | DeclineEligibility |
| Offered | CustomerAccepted | AcceptOffer |
| CustomerAccepted | ApprovedPendingSale | Approve |
| CustomerAccepted | Declined | DeclineApproval |
| * (non-declined) | Cancelled | Cancel |

## Explicitly NOT implemented

- `ACTIVE` / `PAID` / `OVERDUE` / `DEFAULTED` / `WRITTEN_OFF`
- Any path Draft/Offered/Accepted/ApprovedPendingSale → ACTIVE
- Installments, repayments, settlement, inventory, Commerce sale

ACTIVE requires BNPL-07 after authoritative Commerce sale success.
