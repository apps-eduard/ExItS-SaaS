# Pinoy Loan Manager — Personal Loan Experience

**Status:** Planning / product-rule baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

ExItS Personal remains **one customer hub**. This describes the PLM-backed Loan area only. PLM remains authoritative.

Related: [../Architecture/personal-integration-boundary.md](../Architecture/personal-integration-boundary.md), [personal-borrower-linking.md](personal-borrower-linking.md), [loan-documents-and-receipts.md](loan-documents-and-receipts.md), [notification-model.md](notification-model.md), [../Architecture/application-surface-model.md](../Architecture/application-surface-model.md).

---

## Loan area (future)

May include:

- My Lenders
- Available Quick Loans
- Applications / Requests
- Active Loans
- Loan Details
- Schedule
- Payment History
- Receipts
- Notifications
- Settled Loans

Access requires an authorized Personal / Borrower relationship (or a permitted presentation of a pending consent request). Unlinked history remains in PLM even if Personal access ends.

---

## Separate from Personal peer-to-peer lending

Keep any existing Personal **“I Lent / I Borrowed”** personal-domain feature **separate** if it exists.

Do **not** merge Personal peer-to-peer lending concepts with PLM **organizational** Loans.

Organizational Loans are originated and operated by a subscribed lending organization in Pinoy Loan Manager. They are not Personal-to-Personal loans.

---

## Authority

Personal displays authorized facts. It must not query PLM tables. It is not a second loan ledger.

---

## Explicit non-goals

- A standalone borrower app
- Merging P2P Personal lending with PLM Loans
- Personal as financial SoR
