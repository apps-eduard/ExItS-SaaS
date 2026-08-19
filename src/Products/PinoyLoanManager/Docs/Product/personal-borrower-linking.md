# Pinoy Loan Manager — Personal / Borrower Linking

**Status:** Planning / product-rule baseline; lifecycle accepted in PLM-DOC-01
**Implementation present:** No
**Last updated:** 2026-08-19

Optional linking between a PLM Borrower and an ExItS Personal identity. Not a schema, Platform relationship table design, or copy of POS linking.

**Canonical lifecycle, MVP flow, unlink, relink, minimization:** [personal-linking-lifecycle-and-visibility.md](personal-linking-lifecycle-and-visibility.md). Cardinality: [borrower-identity-and-duplicate-policy.md](borrower-identity-and-duplicate-policy.md). ADR: [../Decisions/ADR-002-borrower-personal-cardinality-and-consent.md](../Decisions/ADR-002-borrower-personal-cardinality-and-consent.md).

Related: [borrower-model.md](borrower-model.md), [../Architecture/personal-integration-boundary.md](../Architecture/personal-integration-boundary.md), [../architecture.md](../architecture.md), [../security.md](../security.md).

---

## Accepted rules

ExItS Personal is **Platform-owned** and **product-neutral**.

A Personal identity may separately relate to:

- POS Customer
- PLM Borrower
- future product-specific relationships

POS Customer ≠ PLM Borrower.

A PLM Borrower may remain **unlinked**. Linking is **optional**.

EX ID / QR resolution identifies a Personal identity **only**. Resolution **never** auto-links.

**Explicit Personal consent** is required before an active Personal / Borrower link.

MVP: an authorized organization user (Owner/Manager grants, identifiers open) initiates the request against an **existing** Borrower. Personal self-service claiming is **not** MVP.

Do **not** copy POS linking tables. Do **not** design generic Platform relationship tables here (**PLM-D-00-04** Open). Platform transport/persistence/integration remains **Open** (**PLM-D-00-05**).

---

## Lifecycle and unlink

See [personal-linking-lifecycle-and-visibility.md](personal-linking-lifecycle-and-visibility.md). Unlinking must not delete Borrower, Loan, payments, receipts, or audit history.

---

## Personal visibility (after a permitted link)

Personal may eventually display **authorized** PLM information such as:

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

PLM remains **authoritative**. Personal must **not** query PLM database tables. Detail: [../Architecture/personal-integration-boundary.md](../Architecture/personal-integration-boundary.md).

One lender’s private operational data must not be exposed to another lender.

After unlink, submitted requests and active contractual obligations must not disappear silently. Exact legal retention/visibility remains **PLM-D-00-11**.

---

## Legal / compliance boundary

No consent, identity-resolution, or data-sharing workflow in this document is claimed legally compliant. External qualified legal/compliance review remains required before Production (PLM-D-00-11). This package does not invent Philippine regulations.

---

## Explicit non-goals

- Auto-link from EX ID / QR
- Deleting Borrower on decline or unlink
- Moving Loan history to Platform
- Generic Platform relationship schema
- POS Customer table reuse
- Personal self-claim of a Borrower in MVP
- Automatic Borrower merge
