# Pinoy Loan Manager — Traditional Application and Assessment Policy

**Status:** Accepted product policy (PLM-DOC-07); not implemented
**Implementation present:** No
**Last updated:** 2026-08-19

Traditional Loan Application minimum fields and manual assessment baseline. Not auto-approval or external credit bureau integration.

**Canonical companions:** [traditional-loan-model.md](traditional-loan-model.md), [loan-application-and-approval.md](loan-application-and-approval.md), [loan-product-configuration.md](loan-product-configuration.md). ADR: [../Decisions/ADR-013-borrower-onboarding-and-application-minimums.md](../Decisions/ADR-013-borrower-onboarding-and-application-minimums.md), [../Decisions/ADR-014-assessment-approval-and-disbursement-readiness.md](../Decisions/ADR-014-assessment-approval-and-disbursement-readiness.md).

---

## Traditional Loan Application minimum

A Traditional Loan Application must include:

- Borrower
- Organization
- Branch
- requested principal
- selected Loan Product
- requested term/frequency
- purpose
- required documents/checklist
- applicant acknowledgments
- origin/channel
- created/submitted timestamps
- application version

Product-configured assessment fields may include:

- income/affordability
- employment/business
- household obligations
- references
- collateral/guarantor **only if** a later approved Loan Product requires them

Do **not** make collateral mandatory for MVP.

---

## Assessment (MVP)

MVP assessment is **manual** and organization-controlled.

The system may present deterministic facts:

- existing active Loans
- Past Due / Matured status
- completed Loan history with that Organization
- current exposure with that Organization
- eligibility checks
- submitted income/reference/document facts
- duplicate warnings

Do **not** use:

- data from another lender
- unrelated POS purchases
- external credit bureau data unless a future approved integration exists
- hidden automated approval
- opaque AI credit scoring

**Auto-approval remains outside MVP.**

---

## Workflow states

Conceptual Traditional flow: Draft → Submitted → Under Review → Approved/Rejected → Awaiting Disbursement → Disbursed → Active.

Cancellation/expiry supported; not deletion. Rejected applications retained with reason.

---

## Honesty gates

| Claim | Allowed? |
|---|---|
| Traditional application minimum and manual assessment approved | Yes |
| Credit bureau or AI scoring in MVP | **No** |
| Legally sufficient underwriting | **No** (PLM-D-00-11 Open) |
