# Pinoy Loan Manager — Disbursement Readiness Model

**Status:** Planning / product-rule baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Checks that should eventually exist **before** cash or other authorized release. Complements [disbursement-and-payment-controls.md](disbursement-and-payment-controls.md). Does not implement posting.

Related: [loan-application-and-approval.md](loan-application-and-approval.md), [loan-lifecycle-model.md](loan-lifecycle-model.md), [cashier-and-collector-control-model.md](cashier-and-collector-control-model.md).

---

## Approval ≠ disbursement

Approval alone never proves cash was released. Preferred Platform usage-billing event remains **LOAN DISBURSED**. An approved Loan that is never released must not be marked Disbursed and should not generate a usage-charge event merely because approval existed.

---

## Future readiness checks

Before disbursement, the future system should be capable of checking:

- approval still valid
- Loan terms finalized (snapshotted)
- borrower identity / context
- required documents / conditions (policy **OPEN**; not a KYC claim)
- authorized channel (office / field)
- branch
- cash availability for cash release
- user grant and resource / assignment scope
- no already completed disbursement
- idempotency

Do **not** implement these checks in this package.

If cash is insufficient: block final cash disbursement and require funding / float. Do not fake negative cash.

If terms disagree at release: stop and escalate; do not silently edit amounts.

Failed / not-completed disbursement remains in an appropriate **pre-disbursement** state.

---

## Convergence after successful disbursement

Same:

- Loan
- schedule
- subledger
- payment engine
- penalties
- collection
- settlement
- audit
- reports

No duplicate financial core.

---

## Explicit non-goals

- Implementation of the checklist
- Exact document-checklist policy
- Disbursement cancellation / reversal workflow detail (remains **OPEN**)
- Marking Disbursed without release
