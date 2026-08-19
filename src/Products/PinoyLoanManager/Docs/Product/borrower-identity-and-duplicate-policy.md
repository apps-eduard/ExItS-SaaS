# Pinoy Loan Manager — Borrower Identity and Duplicate Policy

**Status:** Accepted product-owner rules (PLM-DOC-01). Not implemented.
**Implementation present:** No
**Last updated:** 2026-08-19

Canonical rules for Borrower ownership, cardinality, and duplicate handling. Linking lifecycle: [personal-linking-lifecycle-and-visibility.md](personal-linking-lifecycle-and-visibility.md). ADR: [../Decisions/ADR-002-borrower-personal-cardinality-and-consent.md](../Decisions/ADR-002-borrower-personal-cardinality-and-consent.md).

Do not treat this file as a KYC specification, schema, matching formula, or legal identity policy.

---

## Ownership

Borrower is owned by **Pinoy Loan Manager**.

Borrower is **organization-scoped**. A Borrower belongs to **exactly one** lending Organization.

A Borrower may optionally have an operational **Branch** relationship where appropriate. Schema is not designed.

A Borrower **may exist without**:

- ExItS Personal
- POS Customer
- another product relationship
- a current Loan
- a Quick Loan request

Deleting or unlinking a Personal relationship must **never** delete the Borrower.

POS Customer ≠ PLM Borrower. POS customer status never auto-creates a Loan Borrower. Pinoy Loan Manager never reads POS Customer tables.

---

## Cardinality with ExItS Personal

**Within one Organization:**

- one Borrower may be linked to **at most one** ExItS Personal identity at a time
- one ExItS Personal identity may be linked to **at most one** active Borrower record in that Organization

**Across different Organizations:**

- the same Personal identity may be linked to a **separate** PLM Borrower record for each Organization
- each Organization owns its own Borrower record and operational history
- no cross-organization Borrower record is shared

```text
Personal Juan
├── Borrower at Organization A
├── Borrower at Organization B
└── Borrower at Organization C
```

Those are separate PLM organization relationships.

---

## Duplicate handling

Before creating a Borrower, the future system should search for **possible duplicates within the same Organization**.

Possible matching information **may** include (not a required-field list):

- normalized name
- contact information
- address information
- identity-document references
- linked Personal identity
- organization-specific borrower identifiers

Do **not** finalize required fields or matching formulas.

Accepted rules:

- possible duplicate detection produces a **warning / review candidate**
- the system must **not** automatically merge Borrowers
- similar names alone do not prove duplicate identity
- exact duplicate matching remains **server-authoritative**
- Borrower merge is **not authorized in MVP documentation** yet
- a future merge/correction workflow must preserve financial and audit history

---

## Legal / compliance

No borrower-identity, duplicate, or KYC rule in this document is claimed legally compliant (**PLM-D-00-11**).
