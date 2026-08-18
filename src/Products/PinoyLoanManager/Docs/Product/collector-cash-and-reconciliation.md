# Pinoy Loan Manager — Collector Cash and Reconciliation

**Status:** Agreed product direction (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

This document records collector cash accountability as distinct from the loan financial ledger. It is not an accounting-journal or schema specification.

Related: [lending-operating-model.md](lending-operating-model.md), [quick-loan-model.md](quick-loan-model.md), [../Architecture/loan-ledger-and-balance-model.md](../Architecture/loan-ledger-and-balance-model.md).

---

## Two facts, not one balance

Keep a strong conceptual separation:

| World | What it records |
|---|---|
| **A. Loan financial ledger** | Obligation, payments, penalties, balances on the Loan |
| **B. Collector cash accountability** | Physical/operational cash in the collector’s custody |

They may reference the same business transaction but represent different facts.

Example:

Customer pays ₱100.

- **Loan side:** payment is posted against the Loan according to loan rules.
- **Collector cash side:** collector physically received ₱100 cash.

Do **not** combine these into one balance.

---

## Collector daily cash flow

A collector may begin the day with:

- zero cash, **or**
- cashier-issued opening float

Organization policy determines this.

Conceptual equation:

```text
Opening Float
+ Cash Collections
+ Additional Cashier Float
− Approved Loan Disbursements
− Partial Remittances
= Expected Collector Cash
```

At end of day:

```text
Expected Collector Cash
vs
Actual Cash Remitted
= Variance
```

Variance must be:

- recorded
- explained
- authorized / resolved
- auditable

No silent cash balance edits. Corrections follow the financial-history principle in [lending-operating-model.md](lending-operating-model.md) (reversal / adjustment / compensating transaction with actor, time, reason).

Collector must not approve their own cash variance. See [../authorization-matrix.md](../authorization-matrix.md).

---

## Reuse of collected funds

Organization-level configurable policy:

**Allow collector to use collected funds for approved loan disbursement: Yes / No**

**Engineering default: No**

| Setting | Meaning |
|---|---|
| **No** (default) | Collector may disburse only from authorized available float / funds |
| **Yes** | Collected cash may increase available collector cash and may be used for an **authorized** disbursement |

Regardless of configuration:

- every collection is recorded
- every disbursement is recorded
- every cashier-to-collector transfer is recorded
- every collector-to-cashier remittance is recorded
- reconciliation is mandatory

---

## Conceptual cash movements

Documented as business movements. Accounting journal entries are **not** designed here.

| Movement | From | To |
|---|---|---|
| Opening float | Cashier | Collector |
| Additional float | Cashier | Collector |
| Collection | Customer | Collector |
| Field loan disbursement | Collector | Customer |
| Partial remittance | Collector | Cashier |
| End-of-day remittance | Collector | Cashier |
| Office disbursement | Cashier | Customer |
| Office payment | Customer | Cashier |

Office/cashier cash and collector/field cash are the initial operational channels. Bank, e-wallet, and payment gateway are future, not authorized in this package.

---

## Branch scope

Cashier activity, float, disbursement, remittance, and reconciliation may be branch-scoped in multi-branch organizations. Schema is not designed here. See [lending-operating-model.md](lending-operating-model.md).

---

## Explicit non-goals

- Combining loan ledger and collector cash into one balance
- Silent cash edits
- Journal-entry design
- Collector self-approval of variance
- Offline collector cash behavior (open; see [../risks-and-decisions.md](../risks-and-decisions.md))
