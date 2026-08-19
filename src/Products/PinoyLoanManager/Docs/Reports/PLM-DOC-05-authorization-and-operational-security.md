# PLM-DOC-05 — Authorization and Operational Security

**Status:** Documentation package complete (planning only)
**Implementation present:** No
**Last updated:** 2026-08-19
**Branch:** `docs/plm-final-decisions`

Runtime / browser / device / database / production validation: **Not Applicable**.

---

## Scope

Finalize Pinoy Loan Manager MVP authorization contract: role preset codes, grant identifiers, default preset assignments, scope model, workflow guards, data minimization, Owner bootstrap/recovery boundaries, and audit requirements.

Explicitly **out of scope:** code, database, migrations, APIs, UI, solution changes, parked scaffold, POS/Platform implementation, authentication implementation, D-P12-03 transport, custom roles, legal/security-production claims.

---

## Role codes

| Code | Display |
|---|---|
| `plm.owner` | Owner |
| `plm.manager` | Manager |
| `plm.cashier` | Cashier |
| `plm.collector` | Collector |

Custom roles: **not supported in MVP**.

---

## Policy version

**PLM Authorization Policy v1**

Grant format: `plm.<resource>.<action>`. No wildcard. No implicit hierarchy.

Canonical catalog: [../Security/authorization-grant-catalog.md](../Security/authorization-grant-catalog.md).

---

## Default preset summary

| Preset | Scope | Execution highlights |
|---|---|---|
| `plm.owner` | Organization | Full admin; reversal/exception/penalty approval; **no** default office/field execution or refunds.pay |
| `plm.manager` | Org/Branch | Operations/approval; **no** role/config/template manage; **no** cash execution |
| `plm.cashier` | Branch + session | Office disbursement/payment; settlement/prepayment execute; refunds.pay; **no** approval/variance resolve |
| `plm.collector` | Branch + assigned | Field disbursement/payment; **no** settlement/refund/exception approval |

Matrix: [../authorization-matrix.md](../authorization-matrix.md).

---

## Scope model

Organization · Branch · Assigned Work · Own Session/Accountability

Server-side filtering mandatory. Data minimization by role documented.

Canonical: [../Security/resource-scope-and-data-minimization-policy.md](../Security/resource-scope-and-data-minimization-policy.md).

---

## Workflow guards

Grant + scope + workflow state + domain invariants. Documented in [../Product/workflow-authorization-policy.md](../Product/workflow-authorization-policy.md).

---

## Owner controls

- first Owner via future Platform-to-PLM bootstrap (D-P12-03 Open)
- last active Owner **not removable**
- no self-escalation
- emergency Platform recovery = Owner restoration only, audited, not implemented

Canonical: [../Security/privileged-access-and-owner-recovery-policy.md](../Security/privileged-access-and-owner-recovery-policy.md).

---

## High-risk controls

Maker/checker retained (**PLM-D-00-13 Closed**). Controlled Owner Override retained. Future step-up authentication when Platform supports it (**R-091 Open**).

---

## Remaining open dependencies

| ID | Status |
|---|---|
| PLM-D-00-04 | Open |
| PLM-D-00-05 | Open |
| D-P12-03 | Open |
| R-091 | Open |
| PLM-D-00-11 | Open |
| Custom roles | Deferred (future package) |

**PLM-D-00-06 Closed for MVP.** **PLM-D-00-13 Closed.**

---

## No-code / no-database statement

Documentation only. No `.cs`, `.csproj`, `ExItS.slnx`, migrations, database, API, UI, tests, POS, Platform implementation, or parked-scaffold changes.

Implementation remains **paused**. `feat/plm-01-scaffold` remains unmerged.

---

## Git evidence

Recorded in the PLM-DOC-05 commit on `docs/plm-final-decisions`.

---

## Exact next documentation package

**PLM-DOC-06 — Restructuring, Write-Off, Recovery & Collections Closeout**

Do not start PLM-DOC-06 in this package. Implementation remains paused.
