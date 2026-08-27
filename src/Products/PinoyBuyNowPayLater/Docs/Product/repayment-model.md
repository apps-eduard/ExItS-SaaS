# Repayment Model

**Status:** Planning baseline (BNPL-00)  
**Implementation present:** No  
**Related:** BNPL-D-00-17, BNPL-D-00-25, BNPL-D-00-23

## Concepts

| Concept | Notes |
|---|---|
| Repayment identity | Stable id; idempotency key required |
| Amount | Decimal |
| Payment date / posted at | Server authoritative |
| Channel / reference | Cash, GCash, etc. — channels Open (BNPL-D-00-25) |
| Allocation | To installments / principal — policy Open |
| Partial repayment | Supported conceptually |
| Early payoff | Policy Open |
| Reversal / correction | Must not silent-delete; controlled correction |
| Duplicate protection | Idempotency + reconcile |
| Ambiguous outcome | Status query / target-state; no double post |

## Rules

- Repayments allowed only on ACTIVE / OVERDUE (and correction workflows if authorized).  
- Do not implement real payment-provider integration in BNPL-00.  
- Manual recorded repayment is the safe early path.  
- Allocation rules that invent fees/interest are forbidden until BNPL-D-00-15/17 close.

## Independence from POS

Recording a repayment must **not** require Commerce/POS availability (BNPL-D-00-22).
