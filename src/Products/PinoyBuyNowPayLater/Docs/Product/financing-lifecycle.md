# Financing Lifecycle

**Status:** Implemented through APPROVED_PENDING_SALE (BNPL-04/05)
**Implementation present:** Yes — `BnplFinancingApplication` + offer installment plan
**Related:** BNPL-D-00-07, BNPL-D-00-22–24

## Implemented state machine (BNPL-04/05)

| State | Meaning | Debt? | Sale? | Inventory? | Repayments? | Collectible installments? |
|---|---|---|---|---|---|---|
| `Draft` | Application started | No | No | No | No | No |
| `PendingEligibility` | Awaiting eligibility | No | No | No | No | No |
| `Offered` | Concrete offer (+ optional explicit plan) | No | No | No | No | No |
| `CustomerAccepted` | Offer + schedule accepted | No | No | No | No | No |
| `ApprovedPendingSale` | Approved; Commerce sale not done | No | No | No | No | No |
| `Declined` | Terminal non-ACTIVE | No | No | No | No | No |
| `Cancelled` | Cancelled before ACTIVE | No | No | No | No | No |

## Transitions (implemented)

| From | To | Command |
|---|---|---|
| Draft | PendingEligibility | Submit |
| PendingEligibility | Offered | CreateOffer (after eligibility approve) |
| PendingEligibility | Declined | DeclineEligibility |
| Offered | Offered | AttachOrReplaceInstallmentPlan (pre-acceptance) |
| Offered | CustomerAccepted | AcceptOffer (requires valid plan) |
| CustomerAccepted | ApprovedPendingSale | Approve (requires accepted locked plan) |
| CustomerAccepted | Declined | DeclineApproval |
| * (non-declined) | Cancelled | Cancel |

## Explicitly NOT implemented

- `ACTIVE` / `PAID` / `OVERDUE` / `DEFAULTED` / `WRITTEN_OFF`
- Any path Draft/Offered/Accepted/ApprovedPendingSale → ACTIVE
- Collectible debt, repayments, settlement, inventory, Commerce sale
- Automatic term/frequency schedule generation (BNPL-D-00-14 OPEN)

ACTIVE requires BNPL-07 after authoritative Commerce sale success.
