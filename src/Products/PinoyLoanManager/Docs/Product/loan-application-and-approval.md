# Pinoy Loan Manager — Loan Application and Approval

**Status:** Planning / product-rule baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Traditional application capture, review, approval, rejection, and term-change rules. Complements [traditional-loan-model.md](traditional-loan-model.md). Quick Loan requests remain in [quick-loan-model.md](quick-loan-model.md).

Related: [loan-product-configuration.md](loan-product-configuration.md), [disbursement-readiness-model.md](disbursement-readiness-model.md), [../Security/role-and-grant-baseline.md](../Security/role-and-grant-baseline.md).

---

## Application capture (not mandatory fields)

An application may eventually capture categories such as:

- borrower
- organization
- branch
- requested amount
- proposed Loan Product
- requested term
- purpose
- supporting documents
- references
- affordability / income information where the organization requires it
- notes
- review data

Do **not** finalize mandatory fields. Do **not** claim KYC or credit-assessment sufficiency.

---

## Approval baseline

**Manual approval** is the planning baseline.

Owner / Manager may approve according to grants. Cashier does **not** normally approve. Collector cannot approve a Loan. No applicant / customer can approve their own application.

Approval must **snapshot** agreed financial / operational terms. Changing the source Loan Product later must not silently change an already approved application. Same snapshot principle as Quick Loan templates.

Rejected applications remain historically visible. A reason should be required. Do **not** convert rejection into deletion. Personal may eventually receive a permitted rejection status / message for a linked Borrower.

---

## Approval changes before disbursement

Do **not** silently edit Approved Loan terms.

If material terms change before disbursement:

require explicit authorized **revision / reapproval** or **cancellation / new approval** according to future policy.

Preserve history. Exact revision workflow remains **OPEN**.

---

## Legal / compliance boundary

No application or approval workflow in this document is claimed legally compliant. External qualified legal/compliance review remains required before Production (PLM-D-00-11). This package does not invent Philippine regulations.

---

## Explicit non-goals

- Auto-approval
- Applicant self-approval
- Silent post-approval term edits
- Final mandatory field list
