# Pinoy Loan Manager — Approval, Revision, and Disbursement Readiness Policy

**Status:** Accepted product policy (PLM-DOC-07); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

Approval limits, decision snapshot, material change/reapproval, approval expiry, Disbursement readiness checklist, and borrower acknowledgment. Not legal disclosure sufficiency.

**Canonical companions:** [disbursement-readiness-model.md](disbursement-readiness-model.md), [loan-application-and-approval.md](loan-application-and-approval.md), [workflow-authorization-policy.md](workflow-authorization-policy.md). ADR: [../Decisions/ADR-014-assessment-approval-and-disbursement-readiness.md](../Decisions/ADR-014-assessment-approval-and-disbursement-readiness.md).

---

## Approval limit model (MVP)

MVP does **not** implement per-user monetary approval limits.

Owner and Manager with `plm.loan-requests.approve` may approve within:

- approved Loan Product/Template limits
- organization configuration
- valid assessment/checklist
- workflow/domain rules

Organizations requiring amount-based multi-level approval need a **future versioned policy**.

High-risk corrections/reversals remain maker/checker-controlled (ADR-008).

Ordinary Loan approval does not require a distinct application-entry user in MVP.

---

## Approval decision snapshot

Approval must snapshot:

- approved principal
- calculation method
- rate and basis
- fees
- schedule rules
- payment allocation
- penalties
- exception policy
- disbursement channel
- first due date
- maturity
- approval actor/time
- policy versions
- required conditions

Rejection requires a recorded internal reason. Customer-facing rejection wording must avoid exposing confidential internal notes.

Rejected applications are retained, not deleted.

**Approval ≠ Disbursement.**

---

## Material change / reapproval

Material changes after approval invalidate the previous approval.

Material examples:

- principal, rate, calculation method, fee, term, frequency
- first due date, schedule, penalty policy
- disbursement channel
- Borrower identity
- required guarantor/collateral condition

Required behavior:

- preserve original approval
- create revised terms version
- return to review
- obtain new approval
- obtain new borrower acceptance where material terms changed

Do **not** silently edit approved terms.

---

## Non-material changes

Non-material administrative corrections may include internal note, assignment, typo not affecting legal identity or financial terms. They remain audited.

Identity corrections are high-risk (PLM-DOC-05).

---

## Approval expiry

Loan Product/Template may configure **Approval Validity**. No Platform default duration.

When expired before Disbursement:

- approval cannot be executed
- application/request may return to review or expire per policy
- new confirmation/reapproval required
- no Disbursement or usage event created

---

## Disbursement readiness checklist

Before Disbursement require:

- valid current approval
- accepted snapshotted terms
- Borrower verification complete
- required documents complete
- required conditions met
- first due date and schedule generated
- authorized Branch/channel
- assigned Cashier/Collector where applicable
- sufficient accountable cash for cash channel
- no completed Disbursement
- no cancellation/suspension
- idempotency identity
- applicable grant/scope/workflow checks

Checklist result is auditable. See [workflow-authorization-policy.md](workflow-authorization-policy.md).

---

## Borrower acknowledgment before Disbursement

Borrower must be able to review:

- principal, finance charge, fees, deductions, Net Proceeds
- repayment, schedule, first due date, maturity
- penalties, allocation summary
- settlement/prepayment direction
- collection method, release method

Do **not** claim legal disclosure sufficiency (PLM-D-00-11 Open).

---

## Honesty gates

| Claim | Allowed? |
|---|---|
| Approval/reapproval/readiness rules approved | Yes |
| Per-user monetary approval limits in MVP | **No** |
| Legally sufficient disclosure | **No** |
