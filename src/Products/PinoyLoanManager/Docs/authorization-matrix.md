# Pinoy Loan Manager — Authorization Matrix

> Template: P12-WP03. Foundation: [exits-product-foundation-reference.md](../../../../docs/Product-Foundation/exits-product-foundation-reference.md)
> Platform access ≠ product operational permission.

| Field | Value |
|---|---|
| Product | Pinoy Loan Manager / `pinoy-loan-manager` (**Closed**, PLM-D-00-01) |
| Policy version | **PLM Authorization Policy v1** |
| Status | **Accepted MVP preset matrix (PLM-DOC-05)**; **PLM-D-00-06 Closed for MVP** |
| Implementation present | No |

Canonical grant definitions: [Security/authorization-grant-catalog.md](Security/authorization-grant-catalog.md). Default preset policy: [Security/default-role-preset-policy.md](Security/default-role-preset-policy.md). Workflow guards: [Product/workflow-authorization-policy.md](Product/workflow-authorization-policy.md). Maker/checker: [Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md](Decisions/ADR-008-reversals-refunds-variance-and-accounting-boundary.md) (**PLM-D-00-13 Closed**).

Custom roles are **not** supported in MVP.

---

## Authorization formula

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

Deny by default. Server-authoritative only. No client-only authorization. No role-name-only authorization. No implicit role hierarchy.

**D-P12-03:** commercial-state transport remains **Open**. Fail closed on unknown/denied commercial state for write authority.

---

## Role preset codes

| Code | Display | Default scope |
|---|---|---|
| `plm.owner` | Owner | Organization |
| `plm.manager` | Manager | Organization or assigned Branch(es) |
| `plm.cashier` | Cashier | Branch + Own Cashier Session for execution |
| `plm.collector` | Collector | Branch + Assigned Work + Own Collector Accountability |

Multiple active assignments per user are allowed. Effective grants = union of active assignments, each retaining its scope. High-risk maker/checker and Owner Override still apply.

---

## Matrix legend

| Mark | Meaning |
|---|---|
| **Allow** | Included in default preset (still requires grant + scope + workflow + domain checks at runtime) |
| **Deny** | Not included in default preset |
| **Scoped** | Included only within the preset’s scope (Branch / Assigned Work / Own Session as noted) |

No **Open** cells in this MVP matrix.

---

## Administration

| Grant | `plm.owner` | `plm.manager` | `plm.cashier` | `plm.collector` |
|---|---|---|---|---|
| `plm.staff.view` | Allow | Allow | Allow | Deny |
| `plm.role-assignments.manage` | Allow | Deny | Deny | Deny |
| `plm.owner-assignments.manage` | Allow | Deny | Deny | Deny |
| `plm.configuration.view` | Allow | Allow | Deny | Deny |
| `plm.configuration.manage` | Allow | Deny | Deny | Deny |
| `plm.loan-products.view` | Allow | Allow | Deny | Deny |
| `plm.loan-products.manage` | Allow | Deny | Deny | Deny |
| `plm.loan-products.publish` | Allow | Deny | Deny | Deny |

---

## Borrowers and Personal

| Grant | `plm.owner` | `plm.manager` | `plm.cashier` | `plm.collector` |
|---|---|---|---|---|
| `plm.borrowers.view` | Allow | Allow | Scoped | Scoped |
| `plm.borrowers.create` | Allow | Allow | Deny | Deny |
| `plm.borrowers.update` | Allow | Allow | Deny | Deny |
| `plm.borrower-documents.view` | Allow | Allow | Deny | Deny |
| `plm.borrower-documents.manage` | Allow | Allow | Deny | Deny |
| `plm.borrower-groups.manage` | Allow | Allow | Deny | Deny |
| `plm.personal-links.request` | Allow | Allow | Deny | Deny |
| `plm.personal-links.suspend` | Allow | Allow | Deny | Deny |
| `plm.personal-links.correction-request` | Allow | Allow | Deny | Deny |
| `plm.personal-links.correction-approve` | Allow | Allow | Deny | Deny |

Cashier/Collector **Scoped** = Branch / Assigned Work only. No auto-link grant exists.

---

## Loan requests and Loans

| Grant | `plm.owner` | `plm.manager` | `plm.cashier` | `plm.collector` |
|---|---|---|---|---|
| `plm.loan-requests.view` | Allow | Allow | Deny | Deny |
| `plm.loan-requests.create` | Allow | Allow | Deny | Deny |
| `plm.loan-requests.submit` | Allow | Allow | Deny | Deny |
| `plm.loan-requests.review` | Allow | Allow | Deny | Deny |
| `plm.loan-requests.approve` | Allow | Allow | Deny | Deny |
| `plm.loan-requests.reject` | Allow | Allow | Deny | Deny |
| `plm.loan-requests.cancel` | Allow | Allow | Deny | Deny |
| `plm.loans.view` | Allow | Allow | Scoped | Scoped |
| `plm.loans.view-financials` | Allow | Allow | Scoped | Scoped |

Actor may not approve where they are Borrower, co-borrower, guarantor, or direct beneficiary (domain invariant, all presets).

---

## Disbursements

| Grant | `plm.owner` | `plm.manager` | `plm.cashier` | `plm.collector` |
|---|---|---|---|---|
| `plm.disbursements.view` | Allow | Allow | Scoped | Scoped |
| `plm.disbursements.authorize` | Allow | Allow | Deny | Deny |
| `plm.disbursements.execute-office` | Deny | Deny | Scoped | Deny |
| `plm.disbursements.execute-field` | Deny | Deny | Deny | Scoped |
| `plm.disbursements.reversal-request` | Allow | Allow | Scoped | Scoped |
| `plm.disbursements.reversal-approve` | Allow | Allow | Deny | Deny |

Authorization ≠ execution. Owner executes office/field disbursement only when also assigned Cashier/Collector preset.

---

## Payments

| Grant | `plm.owner` | `plm.manager` | `plm.cashier` | `plm.collector` |
|---|---|---|---|---|
| `plm.payments.view` | Allow | Allow | Scoped | Scoped |
| `plm.payments.post-office` | Deny | Deny | Scoped | Deny |
| `plm.payments.post-field` | Deny | Deny | Deny | Scoped |
| `plm.payments.reversal-request` | Allow | Allow | Scoped | Scoped |
| `plm.payments.reversal-approve` | Allow | Allow | Deny | Deny |

---

## Settlement and prepayment

| Grant | `plm.owner` | `plm.manager` | `plm.cashier` | `plm.collector` |
|---|---|---|---|---|
| `plm.settlements.quote` | Allow | Allow | Deny | Deny |
| `plm.settlements.execute` | Deny | Deny | Scoped | Deny |
| `plm.prepayments.quote` | Allow | Allow | Deny | Deny |
| `plm.prepayments.execute` | Deny | Deny | Scoped | Deny |

Formal settlement/prepayment execution is Office/Cashier only in MVP.

---

## Refunds

| Grant | `plm.owner` | `plm.manager` | `plm.cashier` | `plm.collector` |
|---|---|---|---|---|
| `plm.refunds.request` | Allow | Allow | Allow | Deny |
| `plm.refunds.approve` | Allow | Allow | Deny | Deny |
| `plm.refunds.pay` | Deny | Deny | Scoped | Deny |

---

## Collections

| Grant | `plm.owner` | `plm.manager` | `plm.cashier` | `plm.collector` |
|---|---|---|---|---|
| `plm.collections.view-assigned` | Deny | Deny | Deny | Scoped |
| `plm.collections.record-attempt` | Deny | Deny | Deny | Scoped |
| `plm.collection-assignments.manage` | Allow | Allow | Deny | Deny |
| `plm.collection-exceptions.request` | Deny | Allow | Deny | Scoped |
| `plm.collection-exceptions.approve` | Allow | Allow | Deny | Deny |
| `plm.collection-exceptions.declare` | Allow | Allow | Deny | Deny |

---

## Penalties

| Grant | `plm.owner` | `plm.manager` | `plm.cashier` | `plm.collector` |
|---|---|---|---|---|
| `plm.penalties.view` | Allow | Allow | Deny | Scoped |
| `plm.penalties.waiver-request` | Deny | Allow | Deny | Scoped |
| `plm.penalties.waiver-approve` | Allow | Allow | Deny | Deny |
| `plm.penalties.reversal-request` | Deny | Allow | Deny | Scoped |
| `plm.penalties.reversal-approve` | Allow | Allow | Deny | Deny |

---

## Cash operations

| Grant | `plm.owner` | `plm.manager` | `plm.cashier` | `plm.collector` |
|---|---|---|---|---|
| `plm.cash-sessions.open` | Deny | Deny | Scoped | Deny |
| `plm.cash-sessions.view-own` | Deny | Deny | Scoped | Deny |
| `plm.cash-sessions.view-branch` | Allow | Allow | Deny | Deny |
| `plm.cash-sessions.close` | Deny | Deny | Scoped | Deny |
| `plm.collector-floats.issue` | Deny | Deny | Scoped | Deny |
| `plm.collector-floats.receive` | Deny | Deny | Deny | Scoped |
| `plm.collector-floats.view` | Allow | Allow | Scoped | Scoped |
| `plm.remittances.view` | Allow | Allow | Scoped | Scoped |
| `plm.remittances.submit` | Deny | Deny | Deny | Scoped |
| `plm.remittances.receive` | Deny | Deny | Scoped | Deny |
| `plm.remittances.reconcile` | Deny | Deny | Scoped | Deny |
| `plm.cash-variances.view` | Allow | Allow | Scoped | Scoped |
| `plm.cash-variances.resolve` | Allow | Allow | Deny | Deny |

Cashier **Scoped** = Branch + active Cashier Session. Collector **Scoped** = own accountability / assigned remittance context. Nonzero variance cannot be marked balanced.

---

## Reports, audit, override

| Grant | `plm.owner` | `plm.manager` | `plm.cashier` | `plm.collector` |
|---|---|---|---|---|
| `plm.reports.operational` | Allow | Allow | Scoped | Scoped |
| `plm.reports.financial` | Allow | Allow | Deny | Deny |
| `plm.audit.view` | Allow | Allow | Deny | Deny |
| `plm.owner-override.execute` | Allow | Deny | Deny | Deny |

---

## Maker/checker and Owner Override

**PLM-D-00-13 Closed.**

High-risk actions (reversal approve, refund approve/pay, variance resolve, Owner assignment, Personal correction approve, Owner Override, future write-off/recovery): requester normally cannot self-approve when another eligible approver exists.

Controlled **Owner Override** (`plm.owner-override.execute`): only when no other eligible approver exists; mandatory reason/evidence; enhanced audit; subsequent-review reporting. Not available to Manager, Cashier-only, or Collector.

Cashier cannot approve own Payment Reversal or Cash Refund. Cashier cannot resolve own Cashier variance. Collector cannot approve own high-risk actions or resolve own variance.

---

## Platform and commercial boundaries

| Concern | Effect |
|---|---|
| Platform Admin / Platform Owner | **Deny** automatic PLM operational grants |
| PinoyBusinessPOS roles | **Deny** PLM operational grants |
| EX ID / QR resolution | **Deny** Personal link without consent workflow |
| Unknown/denied commercial state | Fail closed for write authority (**D-P12-03 Open**) |
| Silent edit/delete posted history | **Deny** all presets |

---

## Explicit non-grants

- Wildcard or `plm.*` grant
- Custom organization roles in MVP
- Role name without matching grant and scope
- Client UI visibility as authorization
- Borrower self-approval of own Loan
- Collector field cash refund in MVP
- Last active Owner removal
