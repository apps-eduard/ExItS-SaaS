# Pinoy Loan Manager — Lending Operating Model

**Status:** Agreed product direction (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

This document records currently agreed product direction for Pinoy Loan Manager. It is **not** an implementation specification, schema, or legally validated operating rulebook.

Canonical companions:

- [quick-loan-model.md](quick-loan-model.md)
- [collector-cash-and-reconciliation.md](collector-cash-and-reconciliation.md)
- [penalty-exception-and-waiver-model.md](penalty-exception-and-waiver-model.md)
- [../Architecture/application-surface-model.md](../Architecture/application-surface-model.md)
- [../product-definition.md](../product-definition.md)
- [../risks-and-decisions.md](../risks-and-decisions.md)

---

## Purpose

Pinoy Loan Manager is the independently subscribed ExItS product for **lending operations**. A lending organization originates, disburses, collects, reconciles, and reports loans in this product.

Two origination paths are supported. After approval and disbursement they **must converge into one core Loan model**. Do not design two independent financial engines.

---

## Origination paths

### A. Traditional Loan

```text
Borrower
  → Application
  → Review / Assessment
  → Approval
  → Agreement / Terms
  → Disbursement
  → Active Loan
```

Exact traditional-loan workflow, assessment criteria, agreement artifacts, and approval limits remain **Open / Product Owner Decision Required**. This path exists as a distinct origination flow; it is not specified here.

### B. Quick Loan

```text
Organization creates Quick Loan Template
  → publishes offer
  → eligible linked Personal customer sees offer
  → customer chooses amount
  → calculation displayed
  → customer submits Quick Loan Request
  → organization approves / rejects
  → approved request awaits disbursement
  → office/cashier or collector releases funds
  → Active Loan
```

Detail: [quick-loan-model.md](quick-loan-model.md).

**Approved does not mean Disbursed.** Lifecycle after approval:

```text
Approved → Awaiting Disbursement → Disbursed → Active
```

---

## Shared core Loan model

After approval/disbursement, Traditional Loan and Quick Loan use the **same** operational core for:

- loan ledger
- balances
- payment schedule
- payments
- penalties
- collections
- adjustments / reversals
- settlement
- reporting
- audit / history

Origination UX and template/application artifacts may differ. Posted loan financial facts must not.

Exact ledger architecture, statuses, allocation order, and calculation algorithms remain open. See [../risks-and-decisions.md](../risks-and-decisions.md).

---

## Default organization role presets

Recorded presets (not implemented; not a grant matrix):

| Preset | Responsibility baseline |
|---|---|
| **Owner** | Full organization-level Loan Manager administration; staff/role administration; configuration; Quick Loan templates; approvals; reports; high-risk operational authorization as granted |
| **Manager** | Lending operations; borrower/application review; approvals; collector management; penalty exception/waiver review; reports; operational supervision |
| **Cashier** | Cash custody; collector opening float; additional collector float; office disbursement; receive collector remittance; cash reconciliation; authorized office payment operations |
| **Collector** | Assigned borrowers; field collections; record payments; record collection attempts; record missed-payment reason; approved field disbursement; cash accountability; remittance |

These presets must eventually be backed by **granular product-local grants**. Do **not** hard-code authorization directly to role names. Do **not** copy PinoyBusinessPOS grant sets even where display names overlap.

Separation-of-duty baseline (intent):

- Collector must not approve their own waiver.
- Collector must not approve their own cash variance.
- Collector must not approve their own loan/disbursement authorization.
- Cashier should not normally be the loan approver.
- Owner/Manager may approve according to grants.

Exact grants remain future detailed authorization work (PLM-D-00-06). Matrix: [../authorization-matrix.md](../authorization-matrix.md).

---

## Branch model

Pinoy Loan Manager should support **multi-branch** organizations from the beginning.

Single-branch organizations may operate with one default branch.

Future operational records may be branch-scoped where appropriate, including:

- loans
- collectors
- cashier activity
- float
- disbursement
- remittance
- reconciliation
- reports

Database schema is **not** designed in this package.

---

## Currency and payment channels

| Item | Direction |
|---|---|
| Initial operating currency | PHP |
| MVP product/business scope | May be PHP-only |
| Technical later | Avoid needless assumptions that make future multi-currency impossible |
| Multi-currency implementation | **Not authorized** |

Initial operational payment / disbursement focus:

- Office / Cashier cash
- Collector / Field cash

Future channels (not implemented): bank, e-wallet, payment gateway.

---

## Two money worlds (do not mix)

| Flow | Parties | Owner |
|---|---|---|
| **ExItS Platform Usage Charge** | Organization → ExItS Platform | Platform SaaS billing |
| **Borrower Loan Charges** | Borrower → Lending Organization | Pinoy Loan Manager operational money |

The organization must **not** freely choose how much it owes ExItS. Platform Owner controls Platform pricing.

Future Platform pricing models may include:

- fixed amount per billable loan
- percentage of disbursed amount
- tiered pricing
- monthly subscription
- subscription + usage
- Platform-assigned custom pricing plan

**Preferred/default billable event: LOAN DISBURSED**, not Loan Approved. An approved loan that is never released should normally not create a usage transaction.

Conceptual Platform billing:

```text
Loan is Disbursed
  → Loan Manager emits/records approved commercial usage signal
    through future approved Platform integration
  → Platform records Usage Charge
  → Monthly billing aggregates subscription, usage, adjustments
  → Platform Invoice
  → Organization pays ExItS
```

Exact cross-product commercial-state / usage transport remains dependent on Platform architecture, including **D-P12-03**. Do not invent transport or schema. No direct Loan database writes into Platform billing tables.

---

## Financial history

Posted financial history must be auditable.

Do not silently edit or delete:

- disbursement
- posted payment
- penalty
- waiver
- reversal
- collector cash movement
- remittance

Corrections should eventually use reversal, adjustment, or a compensating/new transaction, with actor, time, reason, and audit history.

Final ledger schema is **not** designed here.

---

## Legal / compliance boundary

This product direction does **not** claim that any interest rate, fee, penalty, collection policy, disclosure, or lending workflow is legally compliant.

External legal/compliance validation is required before Production use.

This package does **not** invent Philippine lending regulations.

---

## Explicit non-goals

- Code, projects, migrations, APIs, UI
- Final interest/amortization/penalty formulas or peso/percent rates
- Accounting journal entries
- Auto-approval of Quick Loans
- Multi-currency or payment-gateway implementation
- Treating Platform Admin as the borrower-loan operations UI
