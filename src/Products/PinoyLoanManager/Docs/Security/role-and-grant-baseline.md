# Pinoy Loan Manager — Role and Grant Baseline

**Status:** Accepted product policy (PLM-DOC-05); **PLM-D-00-06 Closed for MVP**; not implemented
**Implementation present:** No
**Policy version:** PLM Authorization Policy v1
**Last updated:** 2026-08-19

Index to finalized MVP role presets, grant catalog, and authorization matrix. Supersedes planning-only grant **intent** from PLM-00-WP05.

**Canonical policy (PLM-DOC-05):**

- [authorization-grant-catalog.md](authorization-grant-catalog.md) — exact grant identifiers
- [default-role-preset-policy.md](default-role-preset-policy.md) — role codes and default assignments
- [resource-scope-and-data-minimization-policy.md](resource-scope-and-data-minimization-policy.md) — scope types and data minimization
- [privileged-access-and-owner-recovery-policy.md](privileged-access-and-owner-recovery-policy.md) — Owner bootstrap, last-Owner protection, recovery boundary
- [../authorization-matrix.md](../authorization-matrix.md) — final MVP preset matrix
- [../Product/workflow-authorization-policy.md](../Product/workflow-authorization-policy.md) — workflow-state guards

ADRs: [ADR-009](Decisions/ADR-009-role-codes-grant-catalog-and-default-presets.md), [ADR-010](Decisions/ADR-010-resource-scope-workflow-security-and-owner-recovery.md), [ADR-008](Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md) (**PLM-D-00-13 Closed**).

---

## Authorization principle

```text
Authenticated Actor
+ Trusted Organization Context
+ Platform Product Access
+ Allowed Commercial State
+ Required Entitlement
+ Active PLM Role Assignment
+ Required PLM Grant
+ Valid Resource Scope
+ Valid Workflow State
+ Domain Invariants
= Authorized Action
```

Deny by default. Server-authoritative only. No client-only authorization. No role-name-only authorization.

---

## Role preset codes

| Code | Display | Focus |
|---|---|---|
| `plm.owner` | Owner | Organization PLM administration |
| `plm.manager` | Manager | Lending operations and supervision |
| `plm.cashier` | Cashier | Office cash custody and execution |
| `plm.collector` | Collector | Assigned field operations |

No implicit hierarchy. Custom roles **not** in MVP. Multiple active assignments allowed; grants union with scope preserved.

---

## Separation of duties (summary)

- Loan approval ≠ cash disbursement
- Loan reversal ≠ physical cash refund
- Collector cannot approve own Loan or high-risk actions
- Cashier cannot approve Loans, own reversals/refunds, or resolve own variance
- Maker/checker when another eligible approver exists (**PLM-D-00-13 Closed**)
- Controlled Owner Override for sole eligible Owner only (`plm.owner-override.execute`)

Detail: [default-role-preset-policy.md](default-role-preset-policy.md), [privileged-access-and-owner-recovery-policy.md](privileged-access-and-owner-recovery-policy.md).

---

## Platform boundary

Platform Owner / Platform Admin do **not** automatically receive PLM operational grants. Commercial-state transport remains **D-P12-03 Open**.

---

## Legal / security boundary

No role or grant policy is claimed legally compliant or production-security certified. **PLM-D-00-11 remains Open.** **R-091 Closed for Phase 13 scope.** Residual MFA/step-up/SSO/email do not reopen R-091.

---

## Explicit non-goals

- Custom roles in MVP
- Wildcard grants
- Schema / API / UI implementation
