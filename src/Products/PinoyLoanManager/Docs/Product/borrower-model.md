# Pinoy Loan Manager — Borrower Model

**Status:** Planning / product-rule baseline; identity rules accepted in PLM-DOC-01
**Implementation present:** No
**Last updated:** 2026-08-19

Borrower is a **Pinoy Loan Manager–owned** operational entity. This is not a KYC specification, schema, or legally validated identity policy.

**Canonical identity, cardinality, and duplicate rules:** [borrower-identity-and-duplicate-policy.md](borrower-identity-and-duplicate-policy.md). Linking: [personal-borrower-linking.md](personal-borrower-linking.md), [personal-linking-lifecycle-and-visibility.md](personal-linking-lifecycle-and-visibility.md).

Related: [borrower-groups-and-targeting.md](borrower-groups-and-targeting.md), [../Architecture/personal-integration-boundary.md](../Architecture/personal-integration-boundary.md), [../architecture.md](../architecture.md).

---

## Ownership (accepted)

A Borrower record belongs to exactly one lending **organization** in this product. Optional Branch relationship may exist where appropriate.

A Borrower **may exist without** ExItS Personal, POS Customer, another product relationship, a current Loan, or a Quick Loan request.

POS Customer ≠ PLM Borrower. POS customer status never auto-creates a Loan Borrower. Pinoy Loan Manager never reads POS Customer tables.

Deleting or unlinking a Personal relationship must never delete the Borrower.

---

## Possible information categories (not required fields)

Future borrower information **may** include categories such as:

- name
- contact
- address
- identity information
- references
- documents
- organization-specific borrower metadata
- branch relationship
- borrower status

Do **not** finalize required KYC fields in this package. Do **not** claim that any field set is regulatorily sufficient.

Duplicate handling: warning/review candidates only; **no automatic merge** in MVP. Detail: [borrower-identity-and-duplicate-policy.md](borrower-identity-and-duplicate-policy.md).

---

## Branch and status

A Borrower may be associated with a branch in a multi-branch organization. Schema is not designed.

Borrower **status** (active / inactive / blocked / other) remains **OPEN**. Status must not silently delete Loan or payment history.

---

## Visibility and authorization

Organization staff see Borrowers according to product-local grants and resource / branch / assignment **scope**. Collectors do not automatically browse every borrower in every branch. See [../Security/role-and-grant-baseline.md](../Security/role-and-grant-baseline.md).

A lending organization must not see unrelated Personal activity from POS, another lender, or other ExItS products.

---

## Legal / compliance boundary

No borrower-identity, KYC, or data-retention design in this document is claimed legally compliant. External qualified legal/compliance review remains required before Production (PLM-D-00-11). This package does not invent Philippine regulations.

---

## Explicit non-goals

- Required KYC field list
- Duplicate-detection **formula**
- Schema / enum design
- Copying POS Customer tables
- Treating Personal as the Borrower SoR
- Automatic Borrower merge
