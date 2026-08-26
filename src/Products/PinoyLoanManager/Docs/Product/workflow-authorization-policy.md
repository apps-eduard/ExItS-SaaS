# Pinoy Loan Manager — Workflow Authorization Policy

**Status:** Accepted product policy (PLM-DOC-05); not implemented
**Implementation present:** No
**Policy version:** PLM Authorization Policy v1
**Last updated:** 2026-08-19

Workflow-state guards that apply after grant and scope checks. A grant is necessary but never sufficient.

**Canonical companions:** [../Security/authorization-grant-catalog.md](../Security/authorization-grant-catalog.md), [disbursement-and-payment-controls.md](disbursement-and-payment-controls.md), [reversal-refund-and-correction-policy.md](reversal-refund-and-correction-policy.md), [early-settlement-and-principal-prepayment-policy.md](early-settlement-and-principal-prepayment-policy.md), [cash-variance-and-session-close-policy.md](cash-variance-and-session-close-policy.md). ADR: [../Decisions/ADR-010-resource-scope-workflow-security-and-owner-recovery.md](../Decisions/ADR-010-resource-scope-workflow-security-and-owner-recovery.md).

---

## General rule

Every operational action must pass:

1. Platform product access and commercial gate (D-P12-03 contract TBD)
2. Active PLM role assignment with required grant
3. Valid resource scope
4. Valid workflow state and domain invariants
5. Maker/checker or controlled Owner Override where required

Deny by default at any failed step.

---

## Loan approval

Requires:

- `plm.loan-requests.approve`
- Submitted/Under Review state
- valid organization/branch scope
- complete snapshotted terms
- actor is **not** Borrower, co-borrower, guarantor, or direct financial beneficiary
- Loan remains inside configured Loan Product/Template limits

Ordinary Loan approval does **not** require a distinct application-entry user in MVP. An authorized Owner/Manager may review and approve an application entered by another staff member or themselves as operator.

Approval and cash Disbursement remain **separate** actions. Future amount-based approval limits are deferred.

---

## Disbursement authorize

Requires:

- `plm.disbursements.authorize`
- approved Loan
- Awaiting Disbursement
- valid terms
- no prior completed Disbursement

---

## Office disbursement execute

Requires:

- `plm.disbursements.execute-office`
- active Cashier Session
- matching Branch
- valid authorization
- sufficient accountable cash
- borrower verification
- terms cannot be edited during execution

---

## Field disbursement execute

Requires:

- `plm.disbursements.execute-field`
- explicit Collector assignment
- valid authorization
- sufficient Collector cash
- borrower verification

---

## Payment post

Requires:

- `plm.payments.post-office` or `plm.payments.post-field`
- valid Loan
- valid collectible amount
- active Cashier Session for office cash **or** Collector assignment/accountability for field cash

---

## Reversal approve

Requires:

- appropriate reversal-approve grant
- valid original transaction
- unreversed amount
- mandatory reason/evidence
- maker/checker or controlled Owner Override per ADR-008

Payment correction = full reversal + correct repost.

---

## Settlement / prepayment execute

Requires:

- `plm.settlements.execute` or `plm.prepayments.execute`
- valid unexpired quote
- active Cashier Session
- matching Branch
- amount validation
- idempotency protection

Quote issuance requires `plm.settlements.quote` or `plm.prepayments.quote`. Collector cannot execute settlement/prepayment workflow in MVP.

---

## Refund pay

Requires:

- approved Refund Payable
- `plm.refunds.pay`
- active Cashier Session
- recipient verification
- sufficient accountable cash

Refund approval requires `plm.refunds.approve` with maker/checker rules.

---

## Variance resolve

Requires:

- `plm.cash-variances.resolve`
- original variance preserved
- actor **not** resolving own Cashier/Collector variance
- resolution reason/evidence

Nonzero variance cannot be marked balanced.

---

## Personal link and correction

Requires appropriate Personal Link grants per PLM-DOC-01. No auto-link. Personal identity correction is high risk with maker/checker and enhanced audit.

---

## Legal / security boundary

No workflow guard is claimed legally compliant or production-security certified. **PLM-D-00-11 remains Open.**

---

## Explicit non-goals

- Workflow engine implementation
- Amount-based approval limits in MVP
- Statutory disclosure validation
