# Pinoy Loan Manager — Personal Integration Boundary

**Status:** Planning / architecture baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

How ExItS Personal and Pinoy Loan Manager interact. Not an API specification, schema, or generic Platform relationship design.

Related: [../Product/personal-borrower-linking.md](../Product/personal-borrower-linking.md), [../Product/borrower-model.md](../Product/borrower-model.md), [application-surface-model.md](application-surface-model.md), [../architecture.md](../architecture.md).

---

## Authority

| Concern | Owner |
|---|---|
| Personal identity / account | Platform |
| PLM Borrower | Pinoy Loan Manager |
| Loan operational data (applications, Loans, schedules, payments, receipts) | Pinoy Loan Manager |
| Personal presentation of authorized Loan facts | ExItS Personal (UI only) |

Personal must **not** directly query PLM database tables. Future access uses PLM-authoritative APIs / contracts only. Endpoint design is **not** this package.

Generic Platform cross-product relationship tables remain **OPEN** (PLM-D-00-04). Do not invent them here.

---

## Identity resolution

EX ID / QR resolution identifies a Personal identity through an **approved Platform contract**.

Resolution is **not** a relationship. It never auto-links. Consent: [../Product/personal-borrower-linking.md](../Product/personal-borrower-linking.md).

---

## Data that may cross the boundary

PLM may receive only approved identity / relationship information required for this product (for example: Personal identifier after consent, consent state).

PLM must **not** receive or store unrelated Personal activity from:

- PinoyBusinessPOS
- another lending organization
- other ExItS products

Personal must **not** expose one lender’s private operational data to another lender.

No loan history is transferred into Platform.

---

## Personal display (authorized only)

After a permitted link, Personal may eventually show:

- lender relationships
- available Quick Loan offers
- submitted requests
- approval / rejection status
- active Loans
- payment schedule
- balances
- payment history
- receipts
- notifications

PLM remains the source of truth. Stale Personal cache, if ever used, is not authoritative.

---

## Unlink

Unlink changes Personal access / relationship only. Borrower, Loan, and payment history remain in PLM. See [../Product/personal-borrower-linking.md](../Product/personal-borrower-linking.md).

---

## Explicit non-goals

- Direct Personal → PLM database access
- Designing Platform relationship schema
- Copying POS Customer / linking tables
- Treating Personal as a second loan ledger
