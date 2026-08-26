# ADR-011 — Restructuring, refinancing, and hardship

**Status:** Accepted product policy (PLM-DOC-06); not implemented
**Date:** 2026-08-19
**Decisions:** Restructuring product rules accepted; **PLM-D-00-08 Closed for MVP Product business/calculation policy** (restructuring portion); Refinancing deferred

---

## Context

PLM needed Product Owner rules for restructuring under the same Loan identity, component treatment, hardship linkage, and separation from refinancing. Prior docs listed restructuring as open under PLM-D-00-08.

---

## Decision

1. Restructuring is supported for Active, Past Due, and Matured Past Due Loans; not for Settled, fully reversed, or Written-Off Loans (unless future recovery policy allows).
2. MVP restructuring stays under the **same Loan identity** and creates a Restructuring Agreement, revised terms snapshot, new schedule version, effective date, and audit event. Original history is preserved.
3. Outstanding Principal remains Principal. Penalties, fees, and unpaid interest are **not** capitalized into Principal. They may be retained, rescheduled, waived, or reversed per authorized policy. No interest on penalties or fees.
4. Future finance charge on restructured Principal uses PLM-DOC-02 approved methods only.
5. Restructuring Policy may configure eligibility, DPD bounds, restructure count, term/frequency, method, rate range, documentation, and penalty/fee treatment — **no default numeric limits**.
6. One active restructuring request per Loan at a time.
7. **Refinancing** (new Loan, possible new Disbursement, explicit replacement) is **deferred beyond MVP**. Do not conflate with restructuring.
8. Restructuring approval requires maker/checker for financial-term changes when another eligible approver exists (ADR-008 / PLM-D-00-13 Closed).

Canonical text: [../Product/restructuring-and-hardship-policy.md](../Product/restructuring-and-hardship-policy.md).

---

## Consequences

Restructuring has an approved MVP product contract. Refinancing remains a future explicit decision.

**Still open:** persistence schema, external GL, legal/compliance (PLM-D-00-11).

No restructuring engine or schema is authorized by this ADR.
