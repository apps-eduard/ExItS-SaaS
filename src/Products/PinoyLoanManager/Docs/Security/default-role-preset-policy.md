# Pinoy Loan Manager — Default Role Preset Policy

**Status:** Accepted product policy (PLM-DOC-05); **PLM-D-00-06 Closed for MVP**; not implemented
**Implementation present:** No
**Policy version:** PLM Authorization Policy v1
**Last updated:** 2026-08-19

MVP role preset codes and default grant assignments. Not a role-assignment schema or implementation.

**Canonical companions:** [authorization-grant-catalog.md](authorization-grant-catalog.md), [resource-scope-and-data-minimization-policy.md](resource-scope-and-data-minimization-policy.md), [../authorization-matrix.md](../authorization-matrix.md). ADR: [../Decisions/ADR-009-role-codes-grant-catalog-and-default-presets.md](../Decisions/ADR-009-role-codes-grant-catalog-and-default-presets.md).

---

## Role preset codes

| Code | Display |
|---|---|
| `plm.owner` | Owner |
| `plm.manager` | Manager |
| `plm.cashier` | Cashier |
| `plm.collector` | Collector |

Rules:

- role codes are lowercase and stable
- display labels may be localized later; codes must not change with localization
- role presets are product-owned
- no implicit role hierarchy
- Manager does not inherit Collector
- Owner does not automatically execute Cashier or Collector duties
- one user may receive multiple active role assignments
- effective grants are the union of grants from active assignments, each retaining its assignment scope
- workflow restrictions and maker/checker rules still override combined grants

Custom organization-defined roles are **not** supported in MVP.

---

## Default scope by preset

| Preset | Default scope |
|---|---|
| `plm.owner` | Organization |
| `plm.manager` | Organization or one/multiple Branch scopes, as assigned |
| `plm.cashier` | One or multiple Branch scopes; execution additionally requires Own Cashier Session |
| `plm.collector` | One or multiple Branch scopes; Borrower/Loan access additionally requires Assigned Work; cash requires Own Collector Accountability |

Changing a role’s scope is auditable.

---

## Default `plm.owner` grants

**Administration:** `plm.staff.view`, `plm.role-assignments.manage`, `plm.owner-assignments.manage`, `plm.configuration.view`, `plm.configuration.manage`, `plm.loan-products.view`, `plm.loan-products.manage`, `plm.loan-products.publish`

**Borrower/Personal:** all Borrower, Borrower Document, Borrower Group, and Personal Link grants

**Requests/Loans:** all Loan Request grants; `plm.loans.view`, `plm.loans.view-financials`

**Disbursement:** `plm.disbursements.view`, `plm.disbursements.authorize`, `plm.disbursements.reversal-request`, `plm.disbursements.reversal-approve`

**Not included by default:** `plm.disbursements.execute-office`, `plm.disbursements.execute-field`

**Payment:** `plm.payments.view`, `plm.payments.reversal-request`, `plm.payments.reversal-approve`

**Not included by default:** `plm.payments.post-office`, `plm.payments.post-field`

**Settlement/Prepayment:** `plm.settlements.quote`, `plm.prepayments.quote`

**Not included by default:** `plm.settlements.execute`, `plm.prepayments.execute`

**Refund:** `plm.refunds.request`, `plm.refunds.approve`

**Not included by default:** `plm.refunds.pay`

**Collections/Penalties:** all collection assignment, exception, and penalty review/approval grants

**Cash oversight:** `plm.cash-sessions.view-branch`, `plm.collector-floats.view`, `plm.remittances.view`, `plm.cash-variances.view`, `plm.cash-variances.resolve`

**Reports/Security:** `plm.reports.operational`, `plm.reports.financial`, `plm.audit.view`, `plm.owner-override.execute`

Owner performs Cashier/Collector execution only when separately assigned the corresponding preset.

---

## Default `plm.manager` grants

**Administration:** `plm.staff.view`, `plm.configuration.view`, `plm.loan-products.view`

**Borrower/Personal:** all Borrower view/create/update and document grants; `plm.borrower-groups.manage`; all Personal Link request/suspend/correction request/approve grants

**Requests/Loans:** all Loan Request grants; `plm.loans.view`, `plm.loans.view-financials`

**Disbursement:** view, authorize, reversal-request, reversal-approve

**Payment:** view, reversal-request, reversal-approve

**Settlement/Prepayment:** quote grants only

**Refund:** request, approve

**Collections/Penalties:** all assignment, exception, and penalty request/approval grants

**Cash oversight:** cash-sessions.view-branch, collector-floats.view, remittances.view, cash-variances.view, cash-variances.resolve

**Reports/Audit:** reports.operational, reports.financial, audit.view

**Not included:** role-assignment grants, configuration.manage, Loan Product manage/publish, office/field cash execution, settlement/prepayment execution, refunds.pay, owner override

---

## Default `plm.cashier` grants

**Scope:** Branch + Own Cashier Session

**Include:** `plm.staff.view`, `plm.borrowers.view`, `plm.loans.view`, `plm.loans.view-financials`, `plm.disbursements.view`, `plm.disbursements.execute-office`, `plm.disbursements.reversal-request`, `plm.payments.view`, `plm.payments.post-office`, `plm.payments.reversal-request`, `plm.settlements.execute`, `plm.prepayments.execute`, `plm.refunds.request`, `plm.refunds.pay`, all Cash Session grants, `plm.collector-floats.issue`, `plm.collector-floats.view`, `plm.remittances.view`, `plm.remittances.receive`, `plm.remittances.reconcile`, `plm.cash-variances.view`, `plm.reports.operational`

**Not included:** Loan approval/rejection, Loan Product/template management, Borrower create/update, Borrower documents, Personal linking, field execution, reversal approval, refund approval, penalty waiver/reversal approval, variance resolution, financial reports, organization-wide audit view, owner override

---

## Default `plm.collector` grants

**Scope:** Branch + Assigned Work + Own Collector Accountability

**Include:** `plm.borrowers.view`, `plm.loans.view`, `plm.loans.view-financials`, `plm.disbursements.view`, `plm.disbursements.execute-field`, `plm.disbursements.reversal-request`, `plm.payments.view`, `plm.payments.post-field`, `plm.payments.reversal-request`, `plm.collections.view-assigned`, `plm.collections.record-attempt`, `plm.collection-exceptions.request`, `plm.penalties.view`, `plm.penalties.waiver-request`, `plm.penalties.reversal-request`, `plm.collector-floats.receive`, `plm.collector-floats.view`, `plm.remittances.view`, `plm.remittances.submit`, `plm.cash-variances.view`, `plm.reports.operational`

**Not included:** organization-wide Borrower browsing, Borrower create/update, Borrower documents, Personal linking, Loan approval/rejection, disbursement authorization, office operations, settlement/prepayment quote or execution, cash refunds, collection assignment management, exception approval/declaration, penalty waiver/reversal approval, Cashier Session operations, float issue, remittance receive/reconcile, variance resolution, financial reports, audit view, owner override

---

## Multiple preset assignments

A user may hold multiple active role presets in the same Organization.

Effective grants = union of grants from active assignments. Each grant retains its assignment scope.

Example: `plm.owner` at Organization scope + `plm.cashier` at Branch A → Owner administration organization-wide; Cashier execution only at Branch A and own active Cashier Session.

Multiple roles must not bypass branch scope, assignment scope, own-session requirements, conflict-of-interest rules, distinct-approver rules, or Owner Override restrictions.

---

## Legal / security boundary

No role preset is claimed legally compliant or production-security certified. **PLM-D-00-11 remains Open.** **R-091 remains Open.**

---

## Explicit non-goals

- Custom roles in MVP
- Implicit hierarchy
- Schema / API / UI implementation
