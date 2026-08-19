# Pinoy Loan Manager — Quick Loan Eligibility and Approval Policy

**Status:** Accepted product policy (PLM-DOC-07); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

Quick Loan Request minimum, eligibility vs approval, and Personal/customer submission boundary.

**Canonical companions:** [quick-loan-model.md](quick-loan-model.md), [quick-loan-publishing-and-eligibility.md](quick-loan-publishing-and-eligibility.md), [personal-loan-experience.md](personal-loan-experience.md). ADR: [../Decisions/ADR-013-borrower-onboarding-and-application-minimums.md](../Decisions/ADR-013-borrower-onboarding-and-application-minimums.md), [../Decisions/ADR-014-assessment-approval-and-disbursement-readiness.md](../Decisions/ADR-014-assessment-approval-and-disbursement-readiness.md).

---

## Quick Loan Request minimum

Quick Loan Request requires:

- linked eligible Borrower/Personal relationship (where Personal channel applies)
- published eligible Quick Loan Template
- selected amount within allowed range/increment
- complete displayed calculation
- acceptance/acknowledgment
- request timestamp
- template/version snapshot
- eligibility-result snapshot

**Publishing/eligibility does not equal approval.**

Staff grants do not allow impersonating Personal. Customer/Personal submission remains through customer-authorized flows.

---

## Eligibility vs approval

Eligibility checks are deterministic facts (active loan count, Past Due, template rules). Approval is a separate authorized staff action with `plm.loan-requests.approve`.

Collector and Cashier presets do **not** receive approval grants by default (PLM Authorization Policy v1).

---

## Conflict of interest

Actor may not approve a Loan where they are the Borrower, co-borrower, guarantor, or direct financial beneficiary.

---

## Honesty gates

| Claim | Allowed? |
|---|---|
| Quick Loan request minimum approved | Yes |
| Auto-approval in MVP | **No** |
| Implemented | **No** |
