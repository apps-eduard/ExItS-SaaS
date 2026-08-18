# Pinoy Loan Manager — Personal / Borrower Linking

**Status:** Planning / product-rule baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Optional linking between a PLM Borrower and an ExItS Personal identity. Not a schema, Platform relationship table design, or copy of POS linking.

Related: [borrower-model.md](borrower-model.md), [../Architecture/personal-integration-boundary.md](../Architecture/personal-integration-boundary.md), [../architecture.md](../architecture.md), [../security.md](../security.md).

---

## Accepted rules

ExItS Personal is **Platform-owned** and **product-neutral**.

A Personal identity may separately relate to:

- POS Customer
- PLM Borrower
- future product relationships

POS Customer ≠ PLM Borrower.

A PLM Borrower may remain **unlinked**. Linking is **optional**.

EX ID / QR resolution identifies a Personal identity **only**. Resolution **never** auto-links.

**Explicit Personal consent** is required before an active Personal / Borrower link.

Do **not** copy POS linking tables. Do **not** design generic Platform relationship tables here (PLM-D-00-04). Linking mechanism remains **Open** (PLM-D-00-05).

---

## Conceptual lifecycle

Not a finalized enum:

```text
Unlinked
  → Link Requested
  → Pending Personal Consent
  → Linked
```

Later:

```text
Linked
  → Unlinked / Consent Revoked
```

---

## Link request flow

```text
Organization has Borrower
        ↓
Borrower provides / scans ExItS ID
        ↓
PLM resolves Personal identity through approved Platform contract
        ↓
PLM requests relationship
        ↓
Personal receives consent request
        ↓
Accept / Decline
        ↓
If Accept:
PLM Borrower becomes linked to that Personal identity
```

Decline must **not** delete the Borrower.

No loan history is transferred into Platform.

PLM stores only approved identity / relationship facts required for this product. It must not ingest unrelated Personal activity from POS, another lender, or other products.

---

## Unlinking / revocation

Unlinking must **not**:

- delete Borrower
- delete Loan
- delete payment history
- change contractual financial history

It changes **Personal access / relationship** only.

---

## Open questions (do not invent)

- who may initiate unlink (Borrower / Personal / organization staff / all of these)
- what happens to pending Quick Loan offers after unlink
- what Personal retains visibility of historically
- re-linking rules
- consent-history retention and display

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
