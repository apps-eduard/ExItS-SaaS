# Pinoy Loan Manager — Notification Model

**Status:** Planning / product-rule baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Potential notifications for Personal customers and organization staff. Not a provider, template, or delivery-SLA specification.

Related: [personal-loan-experience.md](personal-loan-experience.md), [personal-borrower-linking.md](personal-borrower-linking.md), [loan-documents-and-receipts.md](loan-documents-and-receipts.md).

---

## Personal (authorized, after permitted relationship)

Potential notifications:

- relationship request
- Quick Loan offer
- request submitted
- approved
- rejected
- awaiting release
- disbursed
- payment posted
- due reminder
- overdue notification
- penalty / waiver where appropriate
- settlement

Personal must not receive another lender’s private operational data.

---

## Organization staff

Potential notifications:

- pending approvals
- pending disbursements
- collection exceptions
- variance
- reconciliation
- overdue portfolio alerts

Delivery is scoped by grants and assignment. Do not notify Collectors of unrestricted org-wide financial data by default.

---

## Channels

Future channels may include in-app, SMS, email, or other. Do **not** choose SMS/email providers in this package.

Notification failure must not roll back an already posted financial event.

---

## Explicit non-goals

- Provider selection
- Message copy / legal wording
- Delivery guarantees as financial SoR
