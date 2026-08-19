# ADR-002 — Borrower / Personal cardinality and consent

**Status:** Accepted product behavior (PLM-DOC-01); Platform mechanism remains open
**Date:** 2026-08-19
**Decisions:** Product rules accepted; **PLM-D-00-04** Open; **PLM-D-00-05** Open; **PLM-D-00-11** Open; **PLM-D-00-13** Open

---

## Context

PLM needed Product Owner rules for who owns a Borrower, how Personal may relate to Borrowers, how linking starts in MVP, and what unlink/relink must not destroy.

Generic Platform relationship schema and commercial/identity **transport** must not be invented here.

---

## Decision

1. Borrower is PLM-owned, organization-scoped, one Organization per Borrower.
2. Within one Organization: at most one active Personal link per Borrower, and at most one active Borrower per Personal identity.
3. Across Organizations: the same Personal identity may have a separate Borrower per Organization; histories are not shared.
4. MVP linking is **organization-initiated** (Owner/Manager grants in future). Personal self-claim is not MVP.
5. EX ID / QR identifies only; explicit Personal consent and organization confirmation are required; no auto-link.
6. Unlink/revoke/suspend never deletes Borrower, Loans, payments, receipts, or audit; it blocks new Personal-delivered offers and Personal-originated requests.
7. Relink requires a new request and new consent. Changing Personal identity on a Borrower is a high-risk correction (PLM-D-00-13 remains open).
8. Duplicate detection warns; auto-merge is not authorized in MVP.

Canonical text: [../Product/borrower-identity-and-duplicate-policy.md](../Product/borrower-identity-and-duplicate-policy.md), [../Product/personal-linking-lifecycle-and-visibility.md](../Product/personal-linking-lifecycle-and-visibility.md).

---

## Consequences

Product behavior is defined enough to stop inventing linking policy.

**Still open**

- PLM-D-00-04 — generic Platform relationship contract/schema
- PLM-D-00-05 — Platform transport, contract, persistence, integration mechanism
- PLM-D-00-11 — legal/privacy/compliance, including Personal visibility after unlink
- PLM-D-00-13 — two-person high-risk approval for identity correction

No API, schema, or implementation is authorized by this ADR.
