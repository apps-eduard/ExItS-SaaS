# ADR-010 — Resource scope, workflow security, and Owner recovery

**Status:** Accepted product policy (PLM-DOC-05); not implemented
**Date:** 2026-08-19
**Decisions:** Scope, workflow guards, Owner bootstrap/recovery documented; **PLM-D-00-13 remains Closed**; **D-P12-03 remains Open**

> **Historical note (PLM-DOC-05):** R-091 was Open at package completion. **R-091 is now Closed for Phase 13 scope.** Final status: [PLM-decision-status-summary.md](PLM-decision-status-summary.md).

---

## Context

PLM needed finalized scope types, server-side filtering, data minimization, workflow-state authorization, first-Owner bootstrap direction, last-Owner protection, no self-escalation, Platform support recovery boundary, and audit requirements. Maker/checker and Owner Override were closed in ADR-008 (PLM-D-00-13).

---

## Decision

1. Scope types: Organization, Branch, Assigned Work, Own Session/Accountability. No cross-Organization scope.
2. Server-side resource filtering mandatory; resource ID possession is not authorization; not-found-equivalent behavior where appropriate.
3. Data minimization by role: Owner/Manager full operational view within scope; Cashier office-cash minimum; Collector assigned-field minimum.
4. Workflow-state guards documented in [../Product/workflow-authorization-policy.md](../Product/workflow-authorization-policy.md); grant necessary but not sufficient.
5. First `plm.owner` bootstrap via future Platform-to-PLM contract; Platform Admin does not receive PLM operational access.
6. Last active Owner cannot be removed; Owner assignment is high risk with maker/checker or controlled Owner Override.
7. Role assignment lifecycle with auditable status/history; no self-escalation.
8. Platform emergency Owner recovery is control-plane only, limited to Owner restoration, fully audited; not implemented here.
9. High-risk action catalog and enhanced audit; future step-up authentication when Platform supports it (residual gate; **R-091 Closed for Phase 13 scope**).
10. **PLM-D-00-13 remains Closed.** **D-P12-03** remains Open.

Canonical text: [../Security/resource-scope-and-data-minimization-policy.md](../Security/resource-scope-and-data-minimization-policy.md), [../Security/privileged-access-and-owner-recovery-policy.md](../Security/privileged-access-and-owner-recovery-policy.md).

---

## Consequences

Operational security boundaries are documented without claiming production-security readiness.

**Still open:** D-P12-03, step-up auth mechanism, recovery implementation, PLM-D-00-11.

No implementation is authorized by this ADR.
