# Pinoy Loan Manager — Authorization Grant Catalog

**Status:** Accepted product policy (PLM-DOC-05); **PLM-D-00-06 Closed for MVP**; not implemented
**Implementation present:** No
**Policy version:** PLM Authorization Policy v1
**Last updated:** 2026-08-19

Canonical MVP grant identifiers for Pinoy Loan Manager. Not a database schema, API contract, or security-production certification.

**Canonical companions:** [default-role-preset-policy.md](default-role-preset-policy.md), [resource-scope-and-data-minimization-policy.md](resource-scope-and-data-minimization-policy.md), [../authorization-matrix.md](../authorization-matrix.md). ADR: [../Decisions/ADR-009-role-codes-grant-catalog-and-default-presets.md](../Decisions/ADR-009-role-codes-grant-catalog-and-default-presets.md).

---

## Authorization principle

Effective access requires all of:

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

Rules:

- server-authoritative authorization only
- deny by default; absence of a grant means deny
- client UI visibility never grants access
- supplied OrganizationId is never trusted by itself
- resource IDs never bypass tenant/resource checks
- list/search queries must be server-filtered
- workflow state and domain rules are checked after grant/scope checks
- role names alone never authorize actions
- Platform access does not replace product-local authorization

**D-P12-03 remains Open.** Do not choose entitlement transport here.

---

## Identifier format

`plm.<resource>.<action>`

Rules:

- lowercase, dot-separated, stable, product-prefixed, action-specific
- no role names embedded in grant codes
- no wildcard grant in MVP
- no `plm.*`
- no `admin = everything` shortcut

---

## Versioning

**PLM Authorization Policy v1**

- grant identifiers are stable
- deprecated grants remain traceable
- privilege-broadening changes require a new documented policy version
- a product upgrade must not silently broaden existing authority without an explicit migration/acceptance decision
- audit records preserve the policy/grant version used for high-risk actions

---

## Administration

| Grant | Meaning |
|---|---|
| `plm.staff.view` | View PLM staff identities, role assignments, and operational availability within authorized scope |
| `plm.role-assignments.manage` | Assign, modify, suspend, or remove Manager, Cashier, and Collector presets |
| `plm.owner-assignments.manage` | Assign or remove Owner preset, subject to last-Owner and maker/checker rules |
| `plm.configuration.view` | View PLM organization/branch operational configuration |
| `plm.configuration.manage` | Modify PLM operational configuration |
| `plm.loan-products.view` | View Traditional Loan Products and Quick Loan Templates |
| `plm.loan-products.manage` | Create/update/archive draft Loan Products and Quick Loan Templates |
| `plm.loan-products.publish` | Publish/unpublish approved Loan Products or Quick Loan Templates |

---

## Borrowers and Personal

| Grant | Meaning |
|---|---|
| `plm.borrowers.view` | View Borrower records within authorized scope |
| `plm.borrowers.create` | Create Borrower records |
| `plm.borrowers.update` | Update Borrower records |
| `plm.borrower-documents.view` | View Borrower supporting/underwriting documents within authorized scope |
| `plm.borrower-documents.manage` | Upload/update/remove Borrower documents where policy allows |
| `plm.borrower-groups.manage` | Manage organization-owned Borrower groups |
| `plm.personal-links.request` | Initiate organization-side Personal link request per PLM-DOC-01 |
| `plm.personal-links.suspend` | Suspend/revoke an active Personal link per PLM-DOC-01 |
| `plm.personal-links.correction-request` | Request high-risk Personal identity correction |
| `plm.personal-links.correction-approve` | Approve high-risk Personal identity correction |

No grant allows auto-linking from EX ID / QR resolution alone.

---

## Loan requests and Loans

| Grant | Meaning |
|---|---|
| `plm.loan-requests.view` | View Traditional Loan Applications and Quick Loan Requests |
| `plm.loan-requests.create` | Create draft requests/applications on behalf of the organization |
| `plm.loan-requests.submit` | Submit a request/application for review |
| `plm.loan-requests.review` | Review submitted requests/applications |
| `plm.loan-requests.approve` | Approve a request/application (approval ≠ disbursement) |
| `plm.loan-requests.reject` | Reject a request/application with reason |
| `plm.loan-requests.cancel` | Cancel a request/application before disbursement |
| `plm.loans.view` | View Loan records within authorized scope |
| `plm.loans.view-financials` | View Loan financial balances, schedule, and operational ledger detail |

Rules:

- staff grants do not allow impersonating Personal
- actor may not approve a Loan where they are the Borrower, co-borrower, guarantor, or direct financial beneficiary
- Collector and Cashier presets do not receive approval grants by default

---

## Disbursements

| Grant | Meaning |
|---|---|
| `plm.disbursements.view` | View disbursement readiness and disbursement history |
| `plm.disbursements.authorize` | Authorize release (Approved → Awaiting Disbursement); not cash execution |
| `plm.disbursements.execute-office` | Execute office cash disbursement; requires active Cashier Session |
| `plm.disbursements.execute-field` | Execute field cash disbursement; requires Collector assignment and accountable cash |
| `plm.disbursements.reversal-request` | Request Disbursement Reversal after release |
| `plm.disbursements.reversal-approve` | Approve high-risk Disbursement Reversal |

Authorization and execution are separate. No grant permits silent deletion.

---

## Payments

| Grant | Meaning |
|---|---|
| `plm.payments.view` | View payment history and allocation detail |
| `plm.payments.post-office` | Post office cash payment; requires active Cashier Session |
| `plm.payments.post-field` | Post field cash payment; requires Collector assignment/accountability |
| `plm.payments.reversal-request` | Request Payment Reversal |
| `plm.payments.reversal-approve` | Approve high-risk Payment Reversal |

Payment correction = full reversal + correct repost. No grant permits direct mutation of posted history.

---

## Settlement and prepayment

| Grant | Meaning |
|---|---|
| `plm.settlements.quote` | Issue Settlement Quote for full early settlement |
| `plm.settlements.execute` | Execute settlement payment against a valid quote; Office/Cashier only in MVP |
| `plm.prepayments.quote` | Issue principal-prepayment quote/calculation |
| `plm.prepayments.execute` | Execute principal prepayment against valid quote; Office/Cashier only in MVP |

Formal early settlement and principal prepayment workflows are Office/Cashier only in MVP. Ordinary Collector field payment that satisfies scheduled balance remains a normal field Payment, not a Settlement Quote workflow.

---

## Refunds

| Grant | Meaning |
|---|---|
| `plm.refunds.request` | Create/request Refund Payable from an approved source |
| `plm.refunds.approve` | Approve Refund Payable for payment |
| `plm.refunds.pay` | Pay approved Refund Payable as physical cash; Office/Cashier only in MVP |

Requester cannot approve their own refund when another eligible approver exists. Cashier cannot approve their own refund.

---

## Collections

| Grant | Meaning |
|---|---|
| `plm.collections.view-assigned` | View assigned collection/disbursement work only |
| `plm.collections.record-attempt` | Record factual visit/contact/payment-attempt outcomes |
| `plm.collection-assignments.manage` | Assign/reassign collectors, routes, Borrowers, Loans, and field work |
| `plm.collection-exceptions.request` | Request review of a borrower/collector-specific exception |
| `plm.collection-exceptions.approve` | Approve/reject an exception request |
| `plm.collection-exceptions.declare` | Declare scoped organization/branch/area collection suspension |

---

## Penalties

| Grant | Meaning |
|---|---|
| `plm.penalties.view` | View assessed penalties within authorized scope |
| `plm.penalties.waiver-request` | Request penalty waiver |
| `plm.penalties.waiver-approve` | Approve penalty waiver |
| `plm.penalties.reversal-request` | Request penalty reversal |
| `plm.penalties.reversal-approve` | Approve high-risk penalty reversal |

Waiver and reversal remain distinct. Grants do not permit changing the Loan penalty policy retrospectively.

---

## Cash operations

| Grant | Meaning |
|---|---|
| `plm.cash-sessions.open` | Open Cashier Session |
| `plm.cash-sessions.view-own` | View own active/closed Cashier Session |
| `plm.cash-sessions.view-branch` | View branch Cashier Sessions within scope |
| `plm.cash-sessions.close` | Close Cashier Session (balanced or with authorized variance) |
| `plm.collector-floats.issue` | Issue opening/additional Collector float from Cashier |
| `plm.collector-floats.receive` | Receive issued float as the receiving Collector |
| `plm.collector-floats.view` | View Collector float history within scope |
| `plm.remittances.view` | View remittance records within scope |
| `plm.remittances.submit` | Submit Collector remittance |
| `plm.remittances.receive` | Receive Collector remittance at Cashier |
| `plm.remittances.reconcile` | Reconcile received remittance |
| `plm.cash-variances.view` | View cash variance records |
| `plm.cash-variances.resolve` | Resolve cash variance via new auditable resolution event |

No grant permits calling a nonzero variance balanced. Cashier cannot resolve own Cashier variance. Collector cannot resolve own variance.

---

## Reports, audit, and override

| Grant | Meaning |
|---|---|
| `plm.reports.operational` | View operational reports within scope |
| `plm.reports.financial` | View financial reports requiring broader authority |
| `plm.audit.view` | View high-risk audit history within scope |
| `plm.owner-override.execute` | Execute controlled Owner Override per ADR-008 |

Audit view does not grant permission to perform the audited action. Only `plm.owner` with this grant may use Owner Override.

---

## Custom roles

Custom organization-defined roles are **not** supported in MVP. Only `plm.owner`, `plm.manager`, `plm.cashier`, and `plm.collector` presets are used.

---

## Legal / security boundary

No grant catalog is claimed legally compliant or production-security certified. **PLM-D-00-11 remains Open.** **R-091 remains Open.** This package does not invent Philippine regulations or authentication mechanisms.

---

## Explicit non-goals

- Wildcard grants
- Implicit role hierarchy
- Custom roles in MVP
- Persistence schema
- API/UI implementation
- D-P12-03 transport design
