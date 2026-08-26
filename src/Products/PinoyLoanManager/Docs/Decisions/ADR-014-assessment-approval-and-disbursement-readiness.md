# ADR-014 — Assessment, approval, and disbursement readiness

**Status:** Accepted product policy (PLM-DOC-07); not implemented
**Date:** 2026-08-19

---

## Context

PLM needed Product Owner rules for manual assessment, approval scope, material reapproval, approval expiry, and Disbursement readiness. Prior docs listed assessment criteria and readiness checklist as open.

---

## Decision

1. MVP assessment is **manual**; system presents deterministic organization-scoped facts only. No auto-approval, no opaque AI scoring, no cross-lender or POS data.
2. No per-user monetary approval limits in MVP. Owner/Manager with `plm.loan-requests.approve` approve within Product/Template limits and workflow rules.
3. Approval snapshots all financial terms; rejection retained with reason; approval ≠ disbursement.
4. Material term changes invalidate prior approval; preserve history; require reapproval and borrower acceptance where material.
5. Loan Product/Template may configure Approval Validity; no Platform default.
6. Disbursement readiness checklist documented and auditable.
7. Borrower acknowledgment content before Disbursement documented; not claimed legally sufficient.

Canonical: [../Product/approval-revision-and-disbursement-readiness-policy.md](../Product/approval-revision-and-disbursement-readiness-policy.md).

---

## Consequences

Origination approval and readiness rules are approved for MVP planning.

**Still open:** legal disclosure (PLM-D-00-11), amount-based multi-level approval (future), implementation.
