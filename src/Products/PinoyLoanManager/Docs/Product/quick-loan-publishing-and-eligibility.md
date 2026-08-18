# Pinoy Loan Manager — Quick Loan Publishing and Eligibility

**Status:** Planning / product-rule baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Publishing and eligibility for Quick Loan Templates. Complements [quick-loan-model.md](quick-loan-model.md); does not replace template/snapshot rules.

Related: [borrower-groups-and-targeting.md](borrower-groups-and-targeting.md), [personal-borrower-linking.md](personal-borrower-linking.md), [borrower-model.md](borrower-model.md).

---

## Publishing ≠ Loan

Publishing a Quick Loan Template does **not** create a Loan.

Publishing creates **customer visibility / availability** of an offer.

A submitted request still requires **manual organization approval** as the initial / default behavior. Do **not** implement auto-approval.

Template snapshot rules remain as in [quick-loan-model.md](quick-loan-model.md).

---

## Publishing audiences

A template may be published to:

- All Eligible Linked Borrowers of **that organization**
- Borrower Group
- Selected Borrower(s)

Never interpret “all” as all ExItS users globally.

A POS Customer alone is never a publishing audience.

Personal publishing of an offer to a customer requires a **linked** Personal / Borrower relationship plus eligibility. Unlinked Borrowers may still be served in Organization Web according to grants; they do not see Personal offers.

---

## Eligibility ≠ approval

| Question | Layer |
|---|---|
| May this borrower **see / request** this offer? | Eligibility |
| Will the organization **approve** this actual request? | Approval |

Passing eligibility does **not** approve a Loan.

---

## Potential configurable eligibility

Organization / template policy **may** eventually include:

- linked Personal required for Personal publishing
- maximum concurrent active Quick Loans
- overdue status
- completed Loan history
- borrower group
- current exposure
- branch
- other future policy

Do **not** invent default peso exposure limits or overdue formulas.

**Accepted engineering baseline:** default maximum active Quick Loans = **1 per borrower per organization**, configurable by approved organization / template policy.

---

## Privacy

A lending organization must not see unrelated Personal activity from POS, another lender, or other ExItS products.

Personal must not expose one lender’s private operational data (including unpublished templates or other borrowers’ offers) to another lender.

---

## Legal / compliance boundary

No publishing, eligibility, or targeting rule in this document is claimed legally compliant. External qualified legal/compliance review remains required before Production (PLM-D-00-11). This package does not invent Philippine regulations.

---

## Explicit non-goals

- Global ExItS-user broadcast
- Auto-approval
- Built-in mandatory borrower groups
- Dynamic eligibility-rule engine implementation
