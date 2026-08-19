# Pinoy Loan Manager — Cashier and Collector Control Model

**Status:** Planning / product-rule baseline (documentation only)
**Implementation present:** No
**Last updated:** 2026-08-19

Cashier Session, collector daily cash accountability, float, remittance, and cash availability. Complements [collector-cash-and-reconciliation.md](collector-cash-and-reconciliation.md). Does **not** replace the Loan subledger.

Related: [daily-operational-workflow.md](daily-operational-workflow.md), [disbursement-and-payment-controls.md](disbursement-and-payment-controls.md), [exception-reversal-and-variance-workflow.md](exception-reversal-and-variance-workflow.md), [cash-variance-and-session-close-policy.md](cash-variance-and-session-close-policy.md), [../Architecture/loan-ledger-and-balance-model.md](../Architecture/loan-ledger-and-balance-model.md), [../Architecture/operational-subledger-and-accounting-boundary.md](../Architecture/operational-subledger-and-accounting-boundary.md).

---

## Two ledgers (reaffirm)

Loan subledger answers: what does the borrower owe / pay?  
Collector / cashier cash accountability answers: how much physical cash is this person responsible for?

Never derive collector physical cash solely from Loan balance. Never silently edit expected cash to hide variance.

---

## Cashier Session

Each physical cashier / accountable cash location should have a controlled working session.

Conceptual lifecycle:

```text
Open → Active → Reconciliation → Closed
```

Possible information (not a schema):

- organization
- branch
- cashier
- opened time
- opening cash
- cash movements
- expected cash
- declared / count cash
- variance
- closed time
- status

Cash vault / branch treasury architecture is resolved in [branch-treasury-and-float-acknowledgment-policy.md](branch-treasury-and-float-acknowledgment-policy.md) (PLM-DOC-09): Branch Treasury funds Cashier Session opening cash.

---

## Cashier opening flow

```text
Authorized Cashier
        ↓
Opens Cashier Session
        ↓
Records / confirms Opening Cash
        ↓
Session becomes Active
        ↓
Cashier can perform authorized cash operations
```

Opening cash must **not** be silently editable after financial activity begins. If correction is required, use an auditable correction / adjustment process.

---

## Collector day / cash accountability

```text
Opening Float
+ Additional Float
+ Collections Received
− Loan Disbursements
− Partial Remittances
= Expected Collector Cash
```

The system must always be capable of explaining Expected Collector Cash from **recorded movements**. Do **not** store unexplained manually edited balances as authoritative history.

---

## Issuing collector float

```text
Cashier Active Session
        ↓
Cashier selects Collector
        ↓
Authorized Float Amount
        ↓
Cashier confirms cash handed over
        ↓
Collector confirms / receives according to two-step **Pending Receipt** workflow
        ↓
Cash Movement recorded
```

- Cashier cash accountability **decreases**
- Collector cash accountability **increases**
- Same correlated business event
- **No** Loan balance changes

Exact collector acknowledgement mechanism is resolved: two-step Pending Receipt → Received / Active. Canonical: [branch-treasury-and-float-acknowledgment-policy.md](branch-treasury-and-float-acknowledgment-policy.md).

---

## Additional float

Support additional authorized float during the day (example: collector runs low after approved disbursements).

It must be a **new** cash movement. Do **not** modify Opening Float retroactively.

---

## Collected-funds reuse

Organization policy: **Allow Collected Funds for Disbursement: Yes / No**

**Engineering default: No**

| Setting | Meaning |
|---|---|
| **No** (default) | Collector available disbursement funds come from authorized float / funding |
| **Yes** | Customer cash collections increase Collector accountable cash and may contribute to available physical cash for an **authorized** field disbursement |

Regardless: collection and disbursement remain **separate** financial events. No netting away transaction history.

---

## Partial remittance

Support partial remittance.

Illustrative: Collector has PHP 10,000 accountable cash and returns PHP 5,000 to Cashier during the day.

Record: Collector → Cashier Partial Remittance: PHP 5,000

Collector expected cash decreases. Cashier expected cash increases. Do **not** alter original collections / disbursements. PHP figures are illustrative, not defaults.

---

## End-of-day collector remittance

```text
Collector finishes field work
        ↓
System calculates Expected Collector Cash
        ↓
Collector declares / submits cash
        ↓
Cashier physically counts cash
        ↓
Cashier records Actual Cash Received
        ↓
Compare Expected vs Actual
        ↓
No variance  OR  Variance
```

Cash transfers to Cashier accountability only when receipt / remittance is confirmed according to workflow.

---

## Cashier end-of-day

```text
Cashier receives collector remittances
        ↓
Completes outstanding authorized office activity
        ↓
System calculates Expected Cashier Cash
        ↓
Cash physically counted
        ↓
Actual declared
        ↓
Variance calculated
        ↓
Reconciliation recorded
        ↓
Cashier Session closed as Closed Balanced when variance is zero, or Closed With Variance after authorized review when variance is nonzero. Canonical: [cash-variance-and-session-close-policy.md](cash-variance-and-session-close-policy.md).
```

Cashier expected cash must derive from recorded cash movements. Variance detail: [cash-variance-and-session-close-policy.md](cash-variance-and-session-close-policy.md), [exception-reversal-and-variance-workflow.md](exception-reversal-and-variance-workflow.md).

---

## Cash availability

Do **not** permit an operational cash disbursement that would make an accountable physical cash balance inexplicably negative.

Before cash disbursement, the system should eventually validate sufficient authorized cash according to the applicable cash ledger / session.

If insufficient: **block** final disbursement and require appropriate funding / float movement. Do **not** fake negative cash.

---

## Explicit non-goals

- Combining Loan ledger and cash accountability
- Silent opening-cash edits
- Retroactive rewrite of Opening Float
- Accounting journal design
- Forcing every close to zero

## Legal / compliance boundary

No cash-control workflow in this document is claimed legally compliant. External qualified legal/compliance review remains required before Production (PLM-D-00-11). This package does not invent Philippine regulations.
