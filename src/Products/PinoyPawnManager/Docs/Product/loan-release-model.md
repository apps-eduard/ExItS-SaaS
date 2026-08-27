# Loan Release Model

> Index: [README.md](README.md)  
> Related: [appraisal-model.md](appraisal-model.md), [pawn-ticket-and-agreement.md](pawn-ticket-and-agreement.md), [pawn-transaction-model.md](pawn-transaction-model.md)  
> Decisions: [../risks-and-decisions.md](../risks-and-decisions.md)

| Field | Value |
|---|---|
| Product | Pinoy Pawn Manager (PPM) |
| Status | PPM-00 planning |
| Implementation | **None** |
| Last updated | 2026-08-27 |
| LEGAL_AUTHORIZATION_CLAIMED | **NO** |

**Loan release** is the financial operation that pays the agreed **principal** to the customer (or authorized payee policy) and helps move machine A to `ACTIVE`. It is distinct from appraisal and from later redemption payout direction.

---

## Appraised value vs principal

| Amount | Role |
|---|---|
| Appraised value | Risk/judgment input — not cash out |
| Principal | Cash (or channel) amount released |
| Policy link | Configurable; **no fixed LTV %** ([PPM-D-00-07](../risks-and-decisions.md)) |

Staff may offer principal below appraisal. Offering above appraisal should be explicitly constrained by org policy when implemented — not assumed legal or wise.

---

## Placement in lifecycle

```text
ACCEPTED
  → custody commitment (item held)
  → loan release (machine C, idempotent)
  → ACTIVE
```

Do not mark `ACTIVE` if:

- Release payment failed or is unknown  
- Item is not under custody control  

Retry-safe release is mandatory intent ([PPM-R-00-05](../risks-and-decisions.md)). Duplicate releases on double-click/network retry must not pay twice.

---

## Release channels (conceptual)

| Channel (planning) | Notes |
|---|---|
| Cash at counter | Common pawnshop pattern; **cash drawer integration Open** ([PPM-D-00-17](../risks-and-decisions.md)) |
| Bank transfer / e-wallet | Future channel; KYC and reconciliation Open |
| Cheque | Unlikely MVP; if ever, clearing rules Open |
| Mixed | Open |

**Cash-management integration** (use POS drawer vs PPM-local cash controls) is **[PPM-D-00-17](../risks-and-decisions.md) Open**. Safe default:

- Record PPM payment/release **facts** in PPM  
- Do **not** copy POS cash-drawer entities without a later ADR  
- Do not treat Platform SaaS billing as pawn cash

---

## Planning concepts for a release operation

| Concept | Intent |
|---|---|
| Release operation id | Idempotency key / unique operation |
| Ticket / transaction link | What is being funded |
| Principal amount + currency | Must match accepted snapshot |
| Channel | Cash / other |
| Payee | Customer on ticket by default |
| Staff actor | Who performed release |
| Timestamp | When completed |
| Status | Pending / completed / failed / void (planning) |
| External reference | Bank ref, drawer txn id if integrated later |

---

## Charges at release

Whether service charges are deducted from principal at release or collected separately is part of **[PPM-D-00-08](../risks-and-decisions.md) Open**. Until decided:

- Snapshot whatever was disclosed on the ticket  
- Do not invent net-vs-gross statutory rules  

---

## Online-only and security

Initial Web/PWA: **ONLINE-ONLY** for loan release. No offline outbox that could queue cash-out while disconnected.

Authorization: only staff with release capability ([../authorization-matrix.md](../authorization-matrix.md)). Dual control for high amounts is Open (no invented ₱ threshold).

---

## What loan release is not

| Not | Why |
|---|---|
| Physical item release to customer | That is redemption custody release |
| POS sale tender | No retail sale |
| PLM disbursement entity reuse | Separate product |
| Proof of legal lending license | **LEGAL_AUTHORIZATION_CLAIMED=NO** |

---

## Exclusions

- No payment gateway implementation in PPM-00  
- No fixed interest deduction schedule  
- No silent auto-release without staff action  
